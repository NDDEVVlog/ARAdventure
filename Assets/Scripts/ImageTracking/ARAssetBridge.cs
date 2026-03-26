using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

[AddComponentMenu("AR Bridge/AR Asset Bridge")]
public class ARAssetBridge : MonoBehaviour
{
    public static ARAssetBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm gọi từ Visual Scripting
    public void LoadAndAttachAsync(string addressableKey, Transform parent)
    {
        if (string.IsNullOrEmpty(addressableKey) || parent == null) return;

        // KIỂM TRA CHẶN: Nếu đã có con rồi thì không load nữa (Sửa lỗi 353 child)
        if (parent.childCount > 0)
        {
            Debug.LogWarning($"[ARAssetBridge] {addressableKey} đã được load trên parent này. Bỏ qua.");
            return;
        }

        StartCoroutine(LoadAndAttachCoroutine(addressableKey, parent));
    }

    private IEnumerator LoadAndAttachCoroutine(string key, Transform parent)
    {
        Debug.Log($"[ARAssetBridge] Đang bắt đầu load: {key}");
        
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;
            if (prefab != null)
            {
                // Instantiate và căn chỉnh
                GameObject instance = Instantiate(prefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            
                Debug.Log($"[ARAssetBridge] SUCCESS: Đã hiển thị {key}. Child count hiện tại: {parent.childCount}");
            }
        }
        else
        {
            Debug.LogError($"[ARAssetBridge] FAILED: Không tìm thấy Key '{key}'. Hãy kiểm tra bảng Addressables và bấm Build Content!");
        }
    }

    // Hàm ẩn hiện (có thể gọi từ Visual Scripting khi Lost/Found)
    public void SetVisibility(GameObject target, bool visible)
    {
        if (target != null) target.SetActive(visible);
    }
}