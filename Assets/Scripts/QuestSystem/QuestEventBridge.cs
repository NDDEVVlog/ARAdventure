using UnityEngine;
public class QuestEventBridge : MonoBehaviour
{
    public void SendEvent(string eventName)
    {
        EventBus.Publish(eventName);
    }
}