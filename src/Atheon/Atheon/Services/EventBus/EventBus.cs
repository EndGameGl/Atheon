namespace Atheon.Services.EventBus;

public class EventBus<TEventArgs> : IEventBus<TEventArgs>
{
    public static EventBus<TEventArgs> Instance { get; private set; }

    public EventBus()
    {
        Instance = this;
    }

    public event Action<TEventArgs>? Event;

    public void Publish(TEventArgs args)
    {
        Event?.Invoke(args);
    }
}
