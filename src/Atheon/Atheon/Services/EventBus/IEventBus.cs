namespace Atheon.Services.EventBus;

public interface IEventBus<TEventArgs>
{
    event Action<TEventArgs> Published;

    void Publish(TEventArgs args);
}
