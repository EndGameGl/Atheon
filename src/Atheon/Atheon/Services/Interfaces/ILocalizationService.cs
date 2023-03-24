using DotNetBungieAPI.Models;

namespace Atheon.Services.Interfaces;

public interface ILocalizationService
{
    ValueTask<BungieLocales> GetGuildLocale(ulong guildId);
}
