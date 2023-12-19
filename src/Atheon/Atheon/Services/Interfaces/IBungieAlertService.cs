using DotNetBungieAPI.Models;

namespace Atheon.Services.Interfaces;

public interface IBungieAlertService
{
    IReadOnlyCollection<GlobalAlert> CurrentAlerts { get; }
}
