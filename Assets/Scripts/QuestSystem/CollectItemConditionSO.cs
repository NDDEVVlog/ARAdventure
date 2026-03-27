using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Condition/Collect Item")]
public class CollectItemConditionSO : ConditionDefinition
{
    public string itemId;
    public int amount = 1;

    public override ConditionInstance CreateInstance()
    {
        return new CollectItemConditionInstance(itemId, amount);
    }
}

public class CollectItemConditionInstance : ConditionInstance
{
    private string itemId;
    private int required;
    private int current;

    public CollectItemConditionInstance(string itemId, int amount)
    {
        this.itemId = itemId;
        this.required = amount;
    }

    public override void Register()
    {
        EventBus.Subscribe("ITEM_COLLECTED", OnEvent);
    }

    public override void Unregister()
    {
        EventBus.Unsubscribe("ITEM_COLLECTED", OnEvent);
    }

    private void OnEvent(EventData e)
    {
        if (IsDone) return;

        if (e.Get<string>("itemId") == itemId)
        {
            current++;
            if (current >= required)
                IsDone = true;
        }
    }
}