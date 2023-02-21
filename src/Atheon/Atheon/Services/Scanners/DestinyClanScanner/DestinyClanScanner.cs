﻿using Atheon.Attributes;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.Entities;
using DotNetBungieAPI.Models;
using Polly;
using DotNetBungieAPI.HashReferences;
using Atheon.Extensions;

namespace Atheon.Services.Scanners.DestinyClanScanner;

public class DestinyClanScanner : EntityScannerBase<DestinyClanScannerInput, DestinyClanScannerContext>
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IUserQueue _userQueue;
    private readonly IDestinyDb _destinyDb;
    private readonly ICommonEvents _commonEvents;
    private readonly IMemoryCache _memoryCache;
    private AsyncPolicy _apiCallPolicy;

    public DestinyClanScanner(
        ILogger<DestinyClanScanner> logger,
        IBungieClientProvider bungieClientProvider,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IUserQueue userQueue,
        IDestinyDb destinyDb,
        ICommonEvents commonEvents,
        IMemoryCache memoryCache) : base(logger)
    {
        Initialize();
        BuildApiCallPolicy();
        _bungieClientProvider = bungieClientProvider;
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _userQueue = userQueue;
        _destinyDb = destinyDb;
        _commonEvents = commonEvents;
        _memoryCache = memoryCache;
    }

    private void BuildApiCallPolicy()
    {
        var timeoutPolicy = Policy
            .TimeoutAsync(TimeSpan.FromSeconds(20));

        var retryPolicy = Policy
            .Handle<Exception>()
            .RetryAsync(3);

        _apiCallPolicy = retryPolicy.WrapAsync(timeoutPolicy);
    }

    [ScanStep(nameof(GetGroupData), 1)]
    public async ValueTask<bool> GetGroupData(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var bungieClient = await _bungieClientProvider.GetClientAsync();

        context.BungieClient = bungieClient;

        context.ClanId = input.ClanId;

        var groupResponse = await _bungieNetApiCallHandler.PerformRequestAndLog(async (handler) =>
        {
            var apiCallResult = await _apiCallPolicy.ExecuteAndCaptureAsync(async (ct) =>
                await context
                    .BungieClient
                    .ApiAccess
                    .GroupV2
                    .GetGroup(input.ClanId, ct),
            cancellationToken);

            if (apiCallResult.Outcome == OutcomeType.Failure)
            {
                handler.LogRequest(new BungieResponse<bool>()
                {
                    ErrorCode = PlatformErrorCodes.ExternalServiceTimeout
                });
            }

            return apiCallResult.Result;
        });

        if (groupResponse?.IsSuccessfulResponseCode is not true)
        {
            return false;
        }

        context.ClanData = groupResponse.Response;

        return true;
    }

    [ScanStep(nameof(GetMembersOfGroup), 2)]
    public async ValueTask<bool> GetMembersOfGroup(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var membersResponse = await _bungieNetApiCallHandler.PerformRequestAndLog(async (handler) =>
        {
            var apiCallResult = await _apiCallPolicy.ExecuteAndCaptureAsync(async (ct) =>
               await context
                   .BungieClient!
                   .ApiAccess
                   .GroupV2
                   .GetMembersOfGroup(
                       input.ClanId,
                       cancellationToken: ct),
                   cancellationToken: cancellationToken);

            if (apiCallResult.Outcome == OutcomeType.Failure)
            {
                _bungieNetApiCallHandler.LogFailure(PlatformErrorCodes.ExternalServiceTimeout);
            }

            return apiCallResult.Result;
        });

        if (membersResponse?.IsSuccessfulResponseCode is not true)
        {
            return false;
        }

        context.Members = membersResponse.Response.Results;
        return true;
    }

    [ScanStep(nameof(LoadRelatedGuildSettings), 3)]
    public async ValueTask<bool> LoadRelatedGuildSettings(
         DestinyClanScannerInput input,
         DestinyClanScannerContext context,
         CancellationToken cancellationToken)
    {
        var guildSettings = await _memoryCache.GetOrAddAsync(
             $"Clan_Guild_Settings_{input.ClanId}",
             async () =>
             {
                 return await _destinyDb.GetAllGuildSettingsForClanAsync(input.ClanId);
             },
             TimeSpan.FromMinutes(1),
             Caching.CacheExpirationType.Absolute);

        context.LinkedGuildSettings = guildSettings;

        return context.LinkedGuildSettings?.Count > 0;
    }

    [ScanStep(nameof(LoadClanDataFromDb), 4)]
    public async ValueTask<bool> LoadClanDataFromDb(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var clanModel = await _destinyDb.GetClanModelAsync(input.ClanId);

        context.DestinyClanDbModel = clanModel;

        return true;
    }

    [ScanStep(nameof(UpdateClanMembers), 5)]
    public async ValueTask<bool> UpdateClanMembers(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        if (!context.DestinyClanDbModel.LastScan.HasValue ||
            (DateTime.UtcNow - context.DestinyClanDbModel.LastScan.Value).TotalMinutes > 10)
        {
            context.MembersToScan = context.Members.ToList();
            await _userQueue.EnqueueAndWaitForSilentUserScans(context, cancellationToken);
        }
        else
        {
            context.MembersToScan = context.Members.Where(x => x.ShouldScanClanMember()).ToList();
            await _userQueue.EnqueueAndWaitForBroadcastedUserScans(context, cancellationToken);
        }
        return true;
    }

    [ScanStep(nameof(UpdateClanData), 6)]
    public async ValueTask<bool> UpdateClanData(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        UpdateClanName(context);
        UpdateClanLevel(context);
        UpdateClanCallsign(context);

        context.DestinyClanDbModel.MembersOnline = context.MembersOnline;
        context.DestinyClanDbModel.MemberCount = context.Members?.Count ?? 0;
        return true;
    }

    [ScanStep(nameof(UpdateOrInsertClanDataInDb), 7, true)]
    public async ValueTask<bool> UpdateOrInsertClanDataInDb(
       DestinyClanScannerInput input,
       DestinyClanScannerContext context,
       CancellationToken cancellationToken)
    {
        if (context.DestinyClanDbModel is null)
            return true;

        context.DestinyClanDbModel.LastScan = DateTime.UtcNow;
        await _destinyDb.UpsertClanModelAsync(context.DestinyClanDbModel);
        return true;
    }

    private void UpdateClanName(DestinyClanScannerContext context)
    {
        if (context.DestinyClanDbModel.ClanName != context.ClanData.Detail.Name)
        {
            foreach (var guildSetting in context.LinkedGuildSettings)
            {
                _commonEvents.ClanBroadcasts.Publish(
                    ClanBroadcastDbModel.ClanNameChanged(
                        guildSetting.GuildId,
                        context.DestinyClanDbModel.ClanId,
                        context.DestinyClanDbModel.ClanName,
                        context.ClanData.Detail.Name));
            }
            context.DestinyClanDbModel.ClanName = context.ClanData.Detail.Name;
        }
    }

    private void UpdateClanLevel(DestinyClanScannerContext context)
    {
        var clanLevel = context.ClanData.Detail.ClanInfo.D2ClanProgressions[DefinitionHashes.Progressions.ClanLevel].Level;
        if (context.DestinyClanDbModel.ClanLevel != clanLevel)
        {
            foreach (var guildSetting in context.LinkedGuildSettings)
            {
                _commonEvents.ClanBroadcasts.Publish(
                    ClanBroadcastDbModel.ClanLevelChanged(
                        guildSetting.GuildId,
                        context.DestinyClanDbModel.ClanId,
                        context.DestinyClanDbModel.ClanLevel,
                        clanLevel));
            }
            context.DestinyClanDbModel.ClanLevel = clanLevel;
        }
    }

    private void UpdateClanCallsign(DestinyClanScannerContext context)
    {
        if (context.DestinyClanDbModel.ClanCallsign != context.ClanData.Detail.ClanInfo.ClanCallSign)
        {
            foreach (var guildSetting in context.LinkedGuildSettings)
            {
                _commonEvents.ClanBroadcasts.Publish(
                    ClanBroadcastDbModel.ClanCallSignChanged(
                        guildSetting.GuildId,
                        context.DestinyClanDbModel.ClanId,
                        context.DestinyClanDbModel.ClanCallsign,
                        context.ClanData.Detail.ClanInfo.ClanCallSign));
            }
            context.DestinyClanDbModel.ClanCallsign = context.ClanData.Detail.ClanInfo.ClanCallSign;
        }
    }
}
