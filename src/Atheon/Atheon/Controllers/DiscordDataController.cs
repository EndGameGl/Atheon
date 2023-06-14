using Atheon.Controllers.Base;
using Atheon.Models.Api;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscordDataController : ApiResponseControllerBase
{
    private readonly IDiscordClientProvider _discordClientProvider;

    public DiscordDataController(
        ILogger<DiscordDataController> logger,
        IDiscordClientProvider discordClientProvider) : base(logger)
    {
        _discordClientProvider = discordClientProvider;
    }

    [HttpGet("GetDiscordClientId")]
    [Produces(typeof(ApiResponse<string>))]
    public async Task<IActionResult> GetDiscordClientId()
    {
        try
        {
            if (!_discordClientProvider.IsReady)
            {
                return CustomResult<string?>(null, ApiResponseCode.DiscordClientNotReady);
            }

            var client = _discordClientProvider.Client;

            return OkResult(client.CurrentUser.Id.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetDiscordClientId)}");
            return ErrorResult(ex);
        }
    }
}
