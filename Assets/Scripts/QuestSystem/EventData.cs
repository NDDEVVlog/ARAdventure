using System.Collections.Generic;

public class EventData
{
    public string type;
    public Dictionary<string, object> payload;

    public T Get<T>(string key)
    {
        if (payload != null && payload.ContainsKey(key))
            return (T)payload[key];
        return default;
    }
}