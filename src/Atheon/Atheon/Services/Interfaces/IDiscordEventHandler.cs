using DotNetBungieAPI.Models;

namespace Atheon.Services.Interfaces;

public interface IDiscordEventHandler
{
    void SubscribeToEvents();
    Task ReportToSystemChannelAsync(string message);
    Task ReportGlobalAlertToSystemChannelAsync(GlobalAlert alert);
}
