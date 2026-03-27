using UnityEngine;
using Unity.VisualScripting;

public class QuestToUVSBridge : MonoBehaviour
{
    public string eventToListen;

    private void OnEnable() {
        EventBus.Subscribe(eventToListen, OnQuestEvent);
    }

    private void OnDisable() {
        EventBus.Unsubscribe(eventToListen, OnQuestEvent);
    }

    private void OnQuestEvent(EventData data) {
        CustomEvent.Trigger(gameObject, eventToListen);
    }
}