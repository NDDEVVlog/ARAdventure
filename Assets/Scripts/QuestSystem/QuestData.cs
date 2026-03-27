using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Quest Definition")]
public class QuestDefinition : ScriptableObject
{
    public string questId;
    public bool isSequential = true;

    public List<StepDefinition> steps;

    public List<string> onStartEvents;
    public List<string> onCompleteEvents;
}