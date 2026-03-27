using System.Collections.Generic;
using System.Linq;

public class StepInstance
{
    private StepDefinition def;
    private List<ConditionInstance> conditions;

    public bool IsCompleted => conditions.All(c => c.IsDone);

    public StepInstance(StepDefinition def)
    {
        this.def = def;

        conditions = new List<ConditionInstance>();

        foreach (var c in def.conditions)
        {
            if (c != null)
                conditions.Add(c.CreateInstance());
        }
    }

    public void Start()
    {
        Trigger(def.onStartEvents);

        foreach (var c in conditions)
            c.Register();
    }

    public void Stop()
    {
        foreach (var c in conditions)
            c.Unregister();
    }

    public void Update()
    {
        if (!IsCompleted) return;

        Stop();
        Trigger(def.onCompleteEvents);
    }

    private void Trigger(List<string> events)
    {
        if (events == null) return;

        foreach (var e in events)
            EventBus.Publish(e);
    }
}