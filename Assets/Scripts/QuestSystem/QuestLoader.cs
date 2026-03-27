using UnityEngine;
using UnityEngine.AddressableAssets;

public class QuestLoader : MonoBehaviour
{
    public void LoadQuest(string key)
    {
        Addressables.LoadAssetAsync<QuestDefinition>(key).Completed += handle =>
        {
            QuestManager.Instance.StartQuest(handle.Result);
        };
    }
}

