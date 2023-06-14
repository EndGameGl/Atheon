using Atheon.Attributes;
using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.Entities;
using DotNetBungieAPI.Models;
using Polly;

namespace Atheon.Services.Scanners.DestinyClanScanner;

public class DestinyInitialClanScanner : EntityScannerBase<DestinyClanScannerInput, DestinyClanScannerContext>
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IUserQueue _userQueue;
    private readonly IDestinyDb _destinyDb;
    private readonly ICommonEvents _commonEvents;
    private readonly IGuildDb _guildDb;
    private AsyncPolicy _apiCallPolicy;

    public DestinyInitialClanScanner(
        ILogger<DestinyInitialClanScanner> logger,
        IBungieClientProvider bungieClientProvider,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IUserQueue userQueue,
        IDestinyDb destinyDb,
        ICommonEvents commonEvents,
        IGuildDb guildDb) : base(logger)
    {
        Initialize();
        BuildApiCallPolicy();
        _bungieClientProvider = bungieClientProvider;
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _userQueue = userQueue;
        _destinyDb = destinyDb;
        _commonEvents = commonEvents;
        _guildDb = guildDb;
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
                       context.ClanId,
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

    [ScanStep(nameof(UpdateClanMembers), 3)]
    public async ValueTask<bool> UpdateClanMembers(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        context.MembersToScan = context.Members.ToList();
        await _userQueue.EnqueueAndWaitForSilentUserScans(context, cancellationToken);
        return true;
    }

    [ScanStep(nameof(UpdateOrInsertClanDataInDb), 4, true)]
    public async ValueTask<bool> UpdateOrInsertClanDataInDb(
        DestinyClanScannerInput input,
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var clanData = DestinyClanDbModel.CreateFromApiResponse(context.ClanData);
        clanData.LastScan = DateTime.UtcNow;
        clanData.IsTracking = true;
        await _destinyDb.UpsertClanModelAsync(clanData);
        var guildSettings = await _guildDb.GetAllGuildSettingsForClanAsync(clanData.ClanId);

        foreach (var guildSetting in guildSettings)
        {
            _commonEvents.ClanBroadcasts.Publish(new ClanBroadcastDbModel()
            {
                ClanId = clanData.ClanId,
                Date = DateTime.UtcNow,
                GuildId = guildSetting.GuildId,
                Type = ClanBroadcastType.ClanScanFinished,
                WasAnnounced = false
            });
        }
        return true;
    }
}
