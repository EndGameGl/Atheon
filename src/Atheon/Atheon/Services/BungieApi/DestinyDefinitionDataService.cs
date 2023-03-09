using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Service.Abstractions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using IMemoryCache = Atheon.Services.Interfaces.IMemoryCache;
using Atheon.Services.Caching;
using Atheon.Models.Database.Destiny.Tracking;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Collections.Generic;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;

namespace Atheon.Services.BungieApi;

public class DestinyDefinitionDataService
{
    private Dictionary<uint, uint> _collectibleToItemMapping;

    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IDestinyDb _destinyDb;

    public DestinyDefinitionDataService(
        IBungieClientProvider bungieClientProvider,
        IMemoryCache memoryCache,
        IDestinyDb destinyDb)
    {
        _bungieClientProvider = bungieClientProvider;
        _memoryCache = memoryCache;
        _destinyDb = destinyDb;
    }

    public async Task MapLookupTables()
    {
        var client = await _bungieClientProvider.GetClientAsync();
        _collectibleToItemMapping = new Dictionary<uint, uint>();

        var items = client.Repository.GetAll<DestinyInventoryItemDefinition>();

        foreach (var item in items)
        {
            if (item.Collectible.HasValidHash)
            {
                _collectibleToItemMapping.Add(item.Collectible.Hash.GetValueOrDefault(), item.Hash);
            }
        }
    }

    public (string CollectibleName, string CollectbleIcon) GetCollectibleDisplayProperties(DestinyCollectibleDefinition collectibleDefinition)
    {
        if (collectibleDefinition.Redacted &&
            _collectibleToItemMapping.TryGetValue(collectibleDefinition.Hash, out uint itemHash) &&
            (new DefinitionHashPointer<DestinyInventoryItemDefinition>(itemHash)).TryGetDefinition(out var item))
        {
            return (item.DisplayProperties.Name, item.DisplayProperties.Icon.AbsolutePath);
        }
        return (collectibleDefinition.DisplayProperties.Name, collectibleDefinition.DisplayProperties.Icon.AbsolutePath);
    }

    public async Task<List<CuratedCollectible>?> GetCuratedCollectiblesCachedAsync()
    {
        return await _memoryCache.GetOrAddAsync(
            $"{nameof(GetCuratedCollectiblesCachedAsync)}",
            async () =>
            {
                return await _destinyDb.GetCuratedCollectiblesAsync();
            },
            TimeSpan.FromSeconds(30),
            CacheExpirationType.Absolute);
    }

    public async Task<List<CuratedRecord>?> GetCuratedRecordsCachedAsync()
    {
        return await _memoryCache.GetOrAddAsync(
            $"{nameof(GetCuratedRecordsCachedAsync)}",
            async () =>
            {
                return await _destinyDb.GetCuratedRecordsAsync();
            },
            TimeSpan.FromSeconds(30),
            CacheExpirationType.Absolute);
    }

    public async Task<List<DestinyRecordDefinition>> GetAllTitleDefinitionsAsync()
    {
        var titles = new List<DestinyRecordDefinition>();
        var client = await _bungieClientProvider.GetClientAsync();
        AddAddTitleRecordsFromPresentationNode(client, titles, DefinitionHashes.PresentationNodes.Titles);
        AddAddTitleRecordsFromPresentationNode(client, titles, DefinitionHashes.PresentationNodes.LegacyTitles);
        return titles;
    }

    public async Task<List<(uint TitleRecordHash, uint? TitleGildRecordHash)>?> GetTitleHashesCachedAsync()
    {
        return await _memoryCache.GetOrAddAsync(
            $"{nameof(GetTitleHashesAsync)}",
            async () =>
            {
                return await GetTitleHashesAsync();
            },
            TimeSpan.FromSeconds(30),
            CacheExpirationType.Absolute);
    }

    private async Task<List<(uint TitleRecordHash, uint? TitleGildRecordHash)>> GetTitleHashesAsync()
    {
        var client = await _bungieClientProvider.GetClientAsync();

        var hashes = new List<(uint TitleRecordHash, uint? TitleGildRecordHash)>();

        AddTitleRecordHashesFromPresentationNode(client, hashes, DefinitionHashes.PresentationNodes.LegacyTitles);
        AddTitleRecordHashesFromPresentationNode(client, hashes, DefinitionHashes.PresentationNodes.Titles);

        if (hashes.Count > 0)
        {
            return hashes;
        }

        throw new Exception("Failed to find hashes for title triumphs!");
    }

    private static void AddTitleRecordHashesFromPresentationNode(
        IBungieClient bungieClient,
        List<(uint TitleRecordHash, uint? TitleGildRecordHash)> hashes,
        uint presentationNodeHash)
    {
        if (!bungieClient.TryGetDefinition<DestinyPresentationNodeDefinition>(
                presentationNodeHash,
                BungieLocales.EN, out var sealsPresentationNodeDefinition))
            return;

        foreach (var nodeSealEntry in sealsPresentationNodeDefinition.Children.PresentationNodes)
        {
            uint titleHash = 0;
            uint? gildingHash = null;

            if (!nodeSealEntry.PresentationNode.TryGetDefinition(out var sealDefinition))
                continue;

            if (sealDefinition.Redacted)
                continue;

            titleHash = sealDefinition.CompletionRecord.Hash.GetValueOrDefault();

            if (!sealDefinition.CompletionRecord.TryGetDefinition(out var sealRecordDefinition))
            {
                hashes.Add((titleHash, gildingHash));
                continue;
            }

            if (sealRecordDefinition.TitleInfo is null)
                continue;

            if (!sealRecordDefinition.TitleInfo.GildingTrackingRecord.HasValidHash)
            {
                hashes.Add((titleHash, gildingHash));
                continue;
            }

            gildingHash = sealRecordDefinition.TitleInfo.GildingTrackingRecord.Hash.GetValueOrDefault();
            hashes.Add((titleHash, gildingHash));
        }
    }

    private static void AddAddTitleRecordsFromPresentationNode(
        IBungieClient bungieClient,
        List<DestinyRecordDefinition> records,
        uint presentationNodeHash)
    {
        if (!bungieClient.TryGetDefinition<DestinyPresentationNodeDefinition>(
                presentationNodeHash,
                BungieLocales.EN, out var sealsPresentationNodeDefinition))
            return;

        foreach (var nodeSealEntry in sealsPresentationNodeDefinition.Children.PresentationNodes)
        {
            if (!nodeSealEntry.PresentationNode.TryGetDefinition(out var sealDefinition))
                continue;

            if (sealDefinition.Redacted)
                continue;

            if (sealDefinition.CompletionRecord.TryGetDefinition(out var sealRecordDefinition))
            {
                records.Add(sealRecordDefinition);
            }
        }
    }

}
