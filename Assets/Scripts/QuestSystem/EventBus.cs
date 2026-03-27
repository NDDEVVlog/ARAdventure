using System;
using System.Collections.Generic;

public static class EventBus
{
    private static Dictionary<string, Action<EventData>> listeners = new();

    public static Action<string, EventData> OnAnyEvent;

    public static void Subscribe(string eventType, Action<EventData> callback)
    {
        if (!listeners.ContainsKey(eventType))
            listeners[eventType] = delegate { };

        listeners[eventType] += callback;
    }

    public static void Unsubscribe(string eventType, Action<EventData> callback)
    {
        if (listeners.ContainsKey(eventType))
            listeners[eventType] -= callback;
    }

    public static void Publish(string eventType, Dictionary<string, object> payload = null)
    {
        var data = new EventData { type = eventType, payload = payload };

        if (listeners.TryGetValue(eventType, out var cb))
            cb?.Invoke(data);

        OnAnyEvent?.Invoke(eventType, data);
    }
}