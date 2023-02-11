namespace Atheon.Services.EventBus;

public class EventBus<TEventArgs> : IEventBus<TEventArgs>
{
    public static EventBus<TEventArgs> Instance { get; private set; }

    public EventBus()
    {
        Instance = this;
    }

    public event Action<TEventArgs>? Published;

    public void Publish(TEventArgs args)
    {
        Published?.Invoke(args);
    }
}
