namespace Atheon.Services.EventBus;

public interface IEventBus<TEventArgs>
{
    public event Action<TEventArgs> Event;

    public void Publish(TEventArgs args);
}
