using Atheon.Controllers.Base;
using Atheon.Models.Api;
using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuildsController : ApiResponseControllerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IDiscordClientProvider _discordClientProvider;

    public GuildsController(
        ILogger<GuildsController> logger,
        IDestinyDb destinyDb,
        IDiscordClientProvider discordClientProvider) : base(logger)
    {
        _destinyDb = destinyDb;
        _discordClientProvider = discordClientProvider;
    }

    [HttpGet("GuildReferences")]
    [Produces(typeof(ApiResponse<List<GuildReference>>))]
    public async Task<IActionResult> GetAvailableGuilds()
    {
        try
        {
            var guilds = await _destinyDb.GetGuildReferencesAsync();
            return OkResult(guilds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetAvailableGuilds)}");
            return ErrorResult(ex);
        }
    }

    [HttpGet("{guildId}/Settings")]
    [Produces(typeof(ApiResponse<DiscordGuildSettingsDbModel>))]
    public async Task<IActionResult> GetGuildSettings(ulong guildId)
    {
        try
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(guildId);
            return OkResult(guildSettings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetAvailableGuilds)}");
            return ErrorResult(ex);
        }
    }

    [HttpGet("{guildId}/TextChannels")]
    [Produces(typeof(ApiResponse<List<DiscordChannelReference>>))]
    public async Task<IActionResult> GetAvailableGuildTextChannels(ulong guildId)
    {
        try
        {
            if (!_discordClientProvider.IsReady)
            {
                return CustomResult<string?>(null, ApiResponseCode.DiscordClientNotReady);
            }

            var client = _discordClientProvider.Client;

            var guild = client.GetGuild(guildId);

            var textChannels = guild.TextChannels.Select(x => new DiscordChannelReference()
            {
                ChannelId = x.Id.ToString(),
                ChannelName = $"#{x.Name}"
            }).ToList();

            return OkResult(textChannels);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetAvailableGuilds)}");
            return ErrorResult(ex);
        }
    }

    [HttpPost("Settings/{guildId}/Update")]
    public async Task<IActionResult> UpdateGuildDbModel(
        [FromRoute] ulong guildId,
        [FromBody] DiscordGuildSettingsDbModel guildDbModel)
    {
        try
        {
            await _destinyDb.UpsertGuildSettingsAsync(guildDbModel);
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(guildId);
            return OkResult(guildSettings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetAvailableGuilds)}");
            return ErrorResult(ex);
        }
    }
}
