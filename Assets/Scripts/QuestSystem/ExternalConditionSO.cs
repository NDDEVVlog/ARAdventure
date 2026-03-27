using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Condition/External")]
public class ExternalConditionSO : ConditionDefinition
{
    public string eventName;

    public override ConditionInstance CreateInstance()
    {
        return new ExternalConditionInstance(eventName);
    }
}

public class ExternalConditionInstance : ConditionInstance
{
    private string eventName;

    public ExternalConditionInstance(string eventName)
    {
        this.eventName = eventName;
    }

    public override void Register()
    {
        EventBus.Subscribe(eventName, OnEvent);
    }

    public override void Unregister()
    {
        EventBus.Unsubscribe(eventName, OnEvent);
    }

    private void OnEvent(EventData e)
    {
        IsDone = true;
    }
}