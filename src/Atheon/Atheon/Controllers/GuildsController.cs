using Atheon.Controllers.Base;
using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Guilds;
using Atheon.Models.Api;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuildsController : ApiResponseControllerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IGuildDb _guildDb;

    public GuildsController(
        ILogger<GuildsController> logger,
        IDestinyDb destinyDb,
        IDiscordClientProvider discordClientProvider,
        IGuildDb guildDb) : base(logger)
    {
        _destinyDb = destinyDb;
        _discordClientProvider = discordClientProvider;
        _guildDb = guildDb;
    }

    [HttpGet("GuildReferences")]
    [Produces(typeof(ApiResponse<List<GuildReference>>))]
    public async Task<IActionResult> GetAvailableGuilds()
    {
        try
        {
            var guilds = await _guildDb.GetGuildReferencesAsync();
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
            var guildSettings = await _guildDb.GetGuildSettingsAsync(guildId);
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
    public IActionResult GetAvailableGuildTextChannels(ulong guildId)
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

    [HttpPost("{guildId}/Settings/Update")]
    [Produces(typeof(ApiResponse<DiscordGuildSettingsDbModel>))]
    public async Task<IActionResult> UpdateGuildDbModel(
        ulong guildId,
        [FromBody] DiscordGuildSettingsDbModel guildDbModel)
    {
        try
        {
            await _guildDb.UpsertGuildSettingsAsync(guildDbModel);
            var guildSettings = await _guildDb.GetGuildSettingsAsync(guildId);
            return OkResult(guildSettings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error in {nameof(GetAvailableGuilds)}");
            return ErrorResult(ex);
        }
    }
}
