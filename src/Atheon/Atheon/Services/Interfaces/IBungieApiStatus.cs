namespace Atheon.Services.Interfaces;

public interface IBungieApiStatus
{
    public bool IsLive { get; }
    event Func<bool, Task>? StatusChanged;
}
