using Atheon.Controllers.Base;
using Atheon.Models.Api;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsStorageController : ApiResponseControllerBase
{
    private readonly ISettingsStorage _settingsStorage;
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public SettingsStorageController(
        ISettingsStorage settingsStorage,
        ILogger<SettingsStorageController> logger,
        IDiscordClientProvider discordClientProvider,
        IBungieClientProvider bungieClientProvider,
        DestinyDefinitionDataService destinyDefinitionDataService) : base(logger)
    {
        _settingsStorage = settingsStorage;
        _discordClientProvider = discordClientProvider;
        _bungieClientProvider = bungieClientProvider;
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }

    [HttpPost("SetDiscordToken/{reload}")]
    [Produces(typeof(ApiResponse<bool>))]
    public async Task<IActionResult> SetDiscordTokenAsync([FromBody] string token, bool reload)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.DiscordToken, token);
            if (reload)
            {
                await _discordClientProvider.ForceReloadClientAsync();
            }
            return OkResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save discord token");
            return ErrorResult(ex);
        }
    }

    [HttpPost("SetBungieApiKey")]
    [Produces(typeof(ApiResponse<bool>))]
    public async Task<IActionResult> SetBungieApiKey([FromBody] string apiKey)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.BungieApiKey, apiKey);
            _bungieClientProvider.SetApiKey(apiKey);
            return OkResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save discord token");
            return ErrorResult(ex);
        }
    }

    [HttpPost("SetDestinyManifestPath/{reload}")]
    [Produces(typeof(ApiResponse<bool>))]
    public async Task<IActionResult> SetDestinyManifestPath([FromBody] string manifestPath, bool reload)
    {
        try
        {
            await _settingsStorage.SetOption(SettingKeys.BungieManifestStoragePath, manifestPath);
            await _bungieClientProvider.SetManifestPath(manifestPath, reload);
            await _destinyDefinitionDataService.MapLookupTables();
            return OkResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save discord token");
            return ErrorResult(ex);
        }
    }

    [HttpGet("GetDestinyManifestPath")]
    [Produces(typeof(ApiResponse<string>))]
    public async Task<IActionResult> GetDestinyManifestPath()
    {
        try
        {
            var path = await _settingsStorage.GetOption<string>(SettingKeys.BungieManifestStoragePath);
            return OkResult(path);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save discord token");
            return ErrorResult(ex);
        }
    }
}
