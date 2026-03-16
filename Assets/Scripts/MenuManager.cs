using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameSessionCard cardPrefab;
    public Transform cardContainer; // Kéo 1 UI Panel (có gắn Vertical Layout Group) vào đây
    public GameSessionData[] allLevelsData; // Kéo 4 file Data ở bước 1 vào đây

    void Start()
    {
        foreach (var data in allLevelsData)
        {
            GameSessionCard newCard = Instantiate(cardPrefab, cardContainer);
            newCard.SetupCard(data); // Truyền data vào để setup
        }
    }
}