using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Preloaded Quests")]
    public List<QuestDefinition> questDatabase; // 👈 kéo vào đây

    private Dictionary<string, QuestDefinition> questLookup;
    private List<QuestInstance> activeQuests = new();

    private void Awake()
    {
        Instance = this;

        questLookup = new Dictionary<string, QuestDefinition>();

        foreach (var q in questDatabase)
        {
            if (q == null) continue;

            if (!questLookup.ContainsKey(q.questId))
                questLookup.Add(q.questId, q);
            else
                Debug.LogWarning($"Duplicate questId: {q.questId}");
        }
    }

    private void Update()
    {
        foreach (var q in activeQuests)
            q.Update();
    }


    public void StartQuest(string questId)
    {
        if (!questLookup.TryGetValue(questId, out var def))
        {
            Debug.LogError("Quest not found: " + questId);
            return;
        }

        StartQuest(def);
    }

    public void StartQuest(QuestDefinition def)
    {
        if (IsQuestActive(def.questId))
        {
            Debug.LogWarning("Quest already active: " + def.questId);
            return;
        }

        var quest = new QuestInstance(def);
        activeQuests.Add(quest);
        quest.Start();
    }

    public bool IsQuestActive(string questId)
    {
        return activeQuests.Exists(q => q.QuestId == questId);
    }

    public void SendEvent(string type, Dictionary<string, object> payload = null)
    {
        EventBus.Publish(type, payload);
    }
}