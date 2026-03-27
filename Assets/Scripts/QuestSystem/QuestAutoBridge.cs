using UnityEngine;
using Unity.VisualScripting;

public class QuestAutoBridge : MonoBehaviour
{
    private void OnEnable()
    {

        EventBus.OnAnyEvent += HandleGlobalEvent;
    }

    private void OnDisable()
    {
        EventBus.OnAnyEvent -= HandleGlobalEvent;
    }

    private void HandleGlobalEvent(string eventName, EventData data)
    {   
        Debug.Log("Trigger :"+eventName);
        CustomEvent.Trigger(gameObject, eventName, data);
    }
}