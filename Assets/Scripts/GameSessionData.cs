using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewGameSession", menuName = "Game Data/Game Session")]
public class GameSessionData : ScriptableObject
{
    public string sessionID;
    public string sessionName;
    public string location;
    [TextArea(3, 5)] public string description;
    public string sceneAddressableKey; 
    
    // Lưu Reference tới ảnh thay vì tải sẵn ảnh
    public AssetReferenceT<Sprite> thumbnail; 
}