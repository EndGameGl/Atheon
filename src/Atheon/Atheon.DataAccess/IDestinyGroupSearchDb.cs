using Atheon.DataAccess.Models.GroupSearch;

namespace Atheon.DataAccess;

public interface IDestinyGroupSearchDb
{
    Task InsertGroupAsync(DestinyGroupSearch group);
    Task<DestinyGroupSearch?> GetGroupAsync(ulong channelId, ulong messageId);
    Task UpdateGroupMembersAsync(DestinyGroupSearch group);
    Task CloseGroupAsync(DestinyGroupSearch group);
}
