using UnityEngine;
using UnityEngine.UIElements;

public class AdventureUIController : MonoBehaviour
{
    public VisualTreeAsset cardTemplate; // Kéo file CardTemplate.uxml vào đây
    private ScrollView mainScroll;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        mainScroll = root.Q<ScrollView>("MainScroll");
        
        // Test add dữ liệu
        AddCard("Neon District", "Tokyo, Japan", 4.7f);
    }

    public void AddCard(string name, string loc, float rating)
    {
        // 1. Tạo instance từ Template
        VisualElement card = cardTemplate.CloneTree();

        // 2. Tìm các Element bên trong bằng tên (Name) đã đặt ở UXML
        card.Q<Label>("SessionName").text = name;
        card.Q<Label>("Location").text = loc;
        card.Q<Label>("Rating").text = $"★ {rating}";

        // 3. Logic Button
        var btn = card.Q<Button>("ActionButton");
        btn.clicked += () => Debug.Log("Playing: " + name);

        // 4. Add vào ScrollView
        mainScroll.Add(card);
    }
}