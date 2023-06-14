using Atheon.Attributes;
using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.Destiny2.Metadata;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.Entities;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Responses;
using System.Text.Json;

namespace Atheon.Services.Scanners.DestinyClanMemberScanner;

public class DestinyClanMemberBroadcastedScanner : EntityScannerBase<DestinyClanMemberScannerInput, DestinyClanMemberScannerContext>
{
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IDestinyDb _destinyDb;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IProfileUpdater[] _profileUpdaters;

    public DestinyClanMemberBroadcastedScanner(
        ILogger<DestinyClanMemberBroadcastedScanner> logger,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IDestinyDb destinyDb,
        IEnumerable<IProfileUpdater> profileUpdaters,
        DestinyDefinitionDataService destinyDefinitionDataService) : base(logger)
    {
        Initialize();
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _destinyDb = destinyDb;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _profileUpdaters = profileUpdaters.OrderBy(x => x.Priority).ToArray();
    }

    [ScanStep(nameof(CheckIfMemberIsOnline), 1)]
    public ValueTask<bool> CheckIfMemberIsOnline(
    DestinyClanMemberScannerInput input,
    DestinyClanMemberScannerContext context,
    CancellationToken cancellationToken)
    {
        if (!input.GroupMember.IsOnline)
            return ValueTask.FromResult(true);

        input.ClanScannerContext.MembersOnline++;
        return ValueTask.FromResult(true);
    }

    [ScanStep(nameof(GetDestinyProfile), 2)]
    public async ValueTask<bool> GetDestinyProfile(
        DestinyClanMemberScannerInput input,
        DestinyClanMemberScannerContext context,
        CancellationToken cancellationToken)
    {
        BungieResponse<DestinyProfileResponse> profileResponse;
        var groupMember = input.GroupMember;
        var destinyUserInfo = input.GroupMember.DestinyUserInfo;

        try
        {
            var timeoutTokenSource = new CancellationTokenSource();
            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(20));
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
                timeoutTokenSource.Token);
            var combinedToken = tokenSource.Token;

            profileResponse = await input.BungieClient.ApiAccess.Destiny2.GetProfile(
                destinyUserInfo.MembershipType,
                destinyUserInfo.MembershipId,
                Destiny2Metadata.GenericProfileComponents,
                cancellationToken: combinedToken);
        }
        catch (JsonException)
        {
            Logger.LogWarning(
                "Failed to read json response for Destiny2.GetProfile api call, Context = {@Context}",
                new
                {
                    groupMember.GroupId,
                    groupMember.DestinyUserInfo.MembershipId,
                    groupMember.DestinyUserInfo.MembershipType
                });
            _bungieNetApiCallHandler.LogRequest(new BungieResponse<int>()
            {
                ErrorCode = PlatformErrorCodes.JsonDeserializationError
            });
            return false;
        }
        catch (IOException)
        {
            Logger.LogWarning(
                "Failed to read web response for Destiny2.GetProfile api call, Context = {@Context}",
                new
                {
                    groupMember.GroupId,
                    groupMember.DestinyUserInfo.MembershipId,
                    groupMember.DestinyUserInfo.MembershipType
                });
            _bungieNetApiCallHandler.LogRequest(new BungieResponse<int>()
            {
                ErrorCode = PlatformErrorCodes.JsonDeserializationError
            });
            return false;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning(
                "Failed to receive web response for Destiny2.GetProfile api call in 20s, Context = {@Context}",
                new
                {
                    groupMember.GroupId,
                    groupMember.DestinyUserInfo.MembershipId,
                    groupMember.DestinyUserInfo.MembershipType
                });
            _bungieNetApiCallHandler.LogTimeout();
            return false;
        }

        _bungieNetApiCallHandler.LogRequest(profileResponse);

        if (!profileResponse.IsSuccessfulResponseCode)
            return false;

        context.DestinyProfileResponse = profileResponse.Response;
        return true;
    }

    [ScanStep(nameof(CheckIfProfileIsPublic), 3)]
    public ValueTask<bool> CheckIfProfileIsPublic(
         DestinyClanMemberScannerInput input,
         DestinyClanMemberScannerContext context,
         CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(context.DestinyProfileResponse!.HasPublicRecords());
    }

    [ScanStep(nameof(LoadOrCreateDbProfile), 4)]
    public async ValueTask<bool> LoadOrCreateDbProfile(
        DestinyClanMemberScannerInput input,
        DestinyClanMemberScannerContext context,
        CancellationToken cancellationToken)
    {
        var profile = await _destinyDb.GetDestinyProfileAsync(input.GroupMember.DestinyUserInfo.MembershipId);

        if (profile is null)
        {
            var titleHashes = await _destinyDefinitionDataService.GetTitleHashesCachedAsync();
            profile = await DestinyProfileDbModel.CreateFromApiResponse(
                input.GroupMember.GroupId,
                context.DestinyProfileResponse!,
                input.BungieClient,
                titleHashes);
            profile.ClanId = input.ClanScannerContext.ClanId;
            profile.LastUpdated = DateTime.UtcNow;
            await _destinyDb.UpsertDestinyProfileAsync(profile);
            return false;
        }
        else
        {
            profile.ClanId = input.GroupMember.GroupId;
        }
        context.ProfileDbModel = profile;
        return true;
    }

    [ScanStep(nameof(UpdateProfile), 5)]
    public async ValueTask<bool> UpdateProfile(
        DestinyClanMemberScannerInput input,
        DestinyClanMemberScannerContext context,
        CancellationToken cancellationToken)
    {
        var shouldUpdatePrimary = context.ProfileDbModel.ResponseMintedTimestamp < context.DestinyProfileResponse.ResponseMintedTimestamp;
        var shouldUpdateSecondary = context.ProfileDbModel.SecondaryComponentsMintedTimestamp < context.DestinyProfileResponse.SecondaryComponentsMintedTimestamp;

        for (int i = 0; i < _profileUpdaters.Length; i++)
        {
            var updater = _profileUpdaters[i];

            if ((!updater.ReliesOnSecondaryComponents && shouldUpdatePrimary) ||
                (updater.ReliesOnSecondaryComponents && shouldUpdateSecondary))
            {
                await updater.Update(input.BungieClient, context.ProfileDbModel!, context.DestinyProfileResponse!, input.ClanScannerContext.LinkedGuildSettings);
            }
        }

        return true;
    }

    [ScanStep(nameof(SaveProfileData), 6)]
    public async ValueTask<bool> SaveProfileData(
        DestinyClanMemberScannerInput input,
        DestinyClanMemberScannerContext context,
        CancellationToken cancellationToken)
    {
        await _destinyDb.UpsertDestinyProfileAsync(context.ProfileDbModel!);
        return true;
    }
}
