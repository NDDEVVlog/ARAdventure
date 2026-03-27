using System;
using System.Collections.Generic;

[Serializable]
public class StepDefinition
{
    public string stepId;

    public List<ConditionDefinition> conditions;

    public List<string> onStartEvents;
    public List<string> onCompleteEvents;
}