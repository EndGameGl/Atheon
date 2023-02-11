using Atheon.Attributes;
using Atheon.Extensions;
using Atheon.Services.Scanners.Entities;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Models;
using System.Text.Json;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Atheon.Models.Database.Destiny;

namespace Atheon.Services.Scanners.DestinyClanMemberScanner;

public class DestinyClanMemberSilentScanner : EntityScannerBase<DestinyClanMemberScannerInput, DestinyClanMemberScannerContext>
{
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IDestinyDb _destinyDb;

    public DestinyClanMemberSilentScanner(
        ILogger<DestinyClanMemberSilentScanner> logger,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IDestinyDb destinyDb) : base(logger)
    {
        Initialize();
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _destinyDb = destinyDb;
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

        profile ??= DestinyProfileDbModel.CreateFromApiResponse(context.DestinyProfileResponse!, input.BungieClient);

        context.ProfileDbModel = profile;
        return true;
    }
}
