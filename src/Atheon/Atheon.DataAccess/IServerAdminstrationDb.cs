using Atheon.DataAccess.Models.Administration;

namespace Atheon.DataAccess;

public interface IServerAdminstrationDb
{
    Task AddServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task RemoveServerAdministratorAsync(ServerBotAdministrator serverBotAdministrator);
    Task<bool> IsServerAdministratorAsync(ulong guildId, ulong userId);
}
