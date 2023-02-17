using Atheon.Services.DiscordHandlers;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsStorageController : ControllerBase
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly ILogger<SettingsStorageController> _logger;
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IBungieClientProvider _bungieClientProvider;

    public SettingsStorageController(
        ISettingsStorage settingsStorage,
        ILogger<SettingsStorageController> logger,
        IDiscordClientProvider discordClientProvider,
        IBungieClientProvider bungieClientProvider)
    {
        _settingsStorage = settingsStorage;
        _logger = logger;
        _discordClientProvider = discordClientProvider;
        _bungieClientProvider = bungieClientProvider;
    }

    [HttpPost("SetDiscordToken/{reload}")]
    public async Task<IActionResult> SetDiscordTokenAsync([FromBody] string token, bool reload)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.DiscordToken, token);
            if (reload)
            {
                await _discordClientProvider.ForceReloadClientAsync();
            }          
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save discord token");
            return this.BadRequest();
        }
    }

    [HttpPost("SetBungieApiKey")]
    public async Task<IActionResult> SetBungieApiKey([FromBody] string apiKey)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.BungieApiKey, apiKey);
            _bungieClientProvider.SetApiKey(apiKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save discord token");
            return this.BadRequest();
        }
    }

    [HttpPost("SetDestinyManifestPath/{reload}")]
    public async Task<IActionResult> SetDestinyManifestPath([FromBody] string manifestPath, bool reload)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.BungieManifestStoragePath, manifestPath);
            await _bungieClientProvider.SetManifestPath(manifestPath, reload);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save discord token");
            return this.BadRequest();
        }
    }
}
