using Atheon.Controllers.Base;
using Atheon.Models.Api;
using Atheon.Models.Database.Destiny.Guilds;
using Atheon.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuildsController : ApiResponseControllerBase
{
    private readonly IDestinyDb _destinyDb;

    public GuildsController(
        ILogger<GuildsController> logger,
        IDestinyDb destinyDb) : base(logger)
    {
        _destinyDb = destinyDb;
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
}
