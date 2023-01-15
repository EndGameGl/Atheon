using Atheon.Attributes;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.Entities;
using DotNetBungieAPI.Models.Destiny.Responses;
using DotNetBungieAPI.Models;
using System.Text.Json;
using Atheon.Services.BungieApi;
using Atheon.Extensions;

namespace Atheon.Services.Scanners.DestinyProfileScanner
{
    public class DestinyProfileScanner : EntityScannerBase<DestinyProfileScannerInput, DestinyProfileScannerContext>
    {
        private readonly IBungieClientProvider _bungieClientProvider;
        private readonly BungieNetApiCallLogger _bungieNetApiCallLogger;

        public DestinyProfileScanner(
            ILogger<DestinyProfileScanner> logger,
            IBungieClientProvider bungieClientProvider,
            BungieNetApiCallLogger bungieNetApiCallLogger) : base(logger)
        {
            _bungieClientProvider = bungieClientProvider;
            _bungieNetApiCallLogger = bungieNetApiCallLogger;
        }

        [ScanStep(nameof(CheckIfMemberIsOnline), 1)]
        public ValueTask<bool> CheckIfMemberIsOnline(
            DestinyProfileScannerInput input,
            DestinyProfileScannerContext context,
            CancellationToken cancellationToken)
        {
            if (!input.GroupMember.IsOnline)
                return ValueTask.FromResult(true);

            input.ClanScannerContext.MembersOnline++;
            return ValueTask.FromResult(true);
        }

        [ScanStep(nameof(GetDestinyProfile), 2)]
        public async ValueTask<bool> GetDestinyProfile(
            DestinyProfileScannerInput input,
            DestinyProfileScannerContext context,
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

                context.BungieClient = await _bungieClientProvider.GetClientAsync();

                profileResponse = await context.BungieClient.ApiAccess.Destiny2.GetProfile(
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
                _bungieNetApiCallLogger.LogRequest(new BungieResponse<int>()
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
                _bungieNetApiCallLogger.LogRequest(new BungieResponse<int>()
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
                _bungieNetApiCallLogger.LogTimeout();
                return false;
            }

            _bungieNetApiCallLogger.LogRequest(profileResponse);

            if (!profileResponse.IsSuccessfulResponseCode)
                return false;

            context.DestinyProfileResponse = profileResponse.Response;
            return true;
        }

        [ScanStep(nameof(CheckIfProfileIsPublic), 3)]
        public ValueTask<bool> CheckIfProfileIsPublic(
             DestinyProfileScannerInput input,
             DestinyProfileScannerContext context,
             CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(context.DestinyProfileResponse!.HasPublicRecords());
        }
    }
}
