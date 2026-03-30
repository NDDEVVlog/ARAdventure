using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class QuestInstance
{
     public string QuestId => def.questId;
    private QuestDefinition def;
    private List<StepInstance> steps;

    private int currentStep = 0;

    public bool IsCompleted { get; private set; }

    public QuestInstance(QuestDefinition def)
    {
        this.def = def;

        steps = new List<StepInstance>();
        foreach (var s in def.steps)
            steps.Add(new StepInstance(s));
    }

    public void Start()
    {
        Trigger(def.onStartEvents);

        if (def.isSequential)
            steps[currentStep].Start();
        else
            steps.ForEach(s => s.Start());
    }

    public void Update()
    {
        if (IsCompleted) return;

        if (def.isSequential)
        {
            var step = steps[currentStep];
            step.Update();

            if (step.IsCompleted)
            {
                currentStep++;
                UnityEngine.Debug.Log("Step completed, moving to next. Current step: " + currentStep);

                if (currentStep >= steps.Count)
                    Complete();
                else
                    steps[currentStep].Start();
            }
        }
        else
        {
            bool allDone = true;

            foreach (var s in steps)
            {
                s.Update();
                if (!s.IsCompleted)
                    allDone = false;
            }

            if (allDone)
                Complete();
        }
    }

    private void Complete()
    {
        IsCompleted = true;
        Trigger(def.onCompleteEvents);
    }

    private void Trigger(List<string> events)
    {
        if (events == null) return;

        foreach (var e in events)
            EventBus.Publish(e);
    }
}