namespace Atheon.Services.EventBus;

public class EventBus<TEventArgs> : IEventBus<TEventArgs>
{
    private readonly ILogger<EventBus<TEventArgs>> _logger;

    public static EventBus<TEventArgs> Instance { get; private set; }

    public EventBus(ILogger<EventBus<TEventArgs>> logger)
    {
        Instance = this;
        _logger = logger;
    }

    public event Action<TEventArgs>? Published;

    public void Publish(TEventArgs args)
    {
        _logger.LogInformation("Event: {EventName}", typeof(TEventArgs).Name);
        Published?.Invoke(args);
    }
}
