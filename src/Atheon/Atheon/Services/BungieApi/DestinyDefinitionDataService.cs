using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Service.Abstractions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using IMemoryCache = Atheon.Services.Interfaces.IMemoryCache;
using Atheon.Services.Caching;
using Atheon.Models.Database.Destiny.Tracking;

namespace Atheon.Services.BungieApi;

public class DestinyDefinitionDataService
{
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

}
