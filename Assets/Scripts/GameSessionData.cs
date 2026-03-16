using UnityEngine;

[CreateAssetMenu(fileName = "NewGameSession", menuName = "Game Data/Game Session")]
public class GameSessionData : ScriptableObject
{
    public string sessionID;
    public string sessionName;
    public string location;
    [TextArea(3, 5)] public string description;

    [Tooltip("Tên Address của Scene trong bảng Addressables Groups")]
    public string sceneAddressableKey; 
}