namespace Atheon.Services.Interfaces;

public interface IDestinyManifestHandler
{
    bool IsUpdating { get; }
    event Func<Task> UpdateStarted;
}
