using UnityEngine;

public abstract class ConditionDefinition : ScriptableObject
{
    public abstract ConditionInstance CreateInstance();
}


public abstract class ConditionInstance
{
    public bool IsDone { get; protected set; }

    public abstract void Register();
    public abstract void Unregister();
}