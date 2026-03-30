using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using Unity.VisualScripting; // Thêm namespace này để dùng CustomEvent

[AddComponentMenu("AR Bridge/AR Asset Bridge")]
public class ARAssetBridge : MonoBehaviour
{
    public static ARAssetBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Load Asset và kích hoạt một Custom Event khi xong
    /// </summary>
    /// <param name="onCompletedEventName">Tên Event sẽ gọi trong Visual Scripting khi load xong</param>
    public void LoadAndAttachAsync(string addressableKey, Transform parent, Vector3 localPos, Vector3 localRotationEuler, Vector3 localScale, string onCompletedEventName)
    {
        if (string.IsNullOrEmpty(addressableKey) || parent == null) return;

        if (parent.childCount > 0)
        {
            Debug.LogWarning($"[ARAssetBridge] {addressableKey} đã tồn tại. Bỏ qua.");
            // Nếu đã có rồi, vẫn trigger event để Visual Scripting biết mà chạy tiếp animation
            CustomEvent.Trigger(gameObject, onCompletedEventName, parent.GetChild(0).gameObject);
            return;
        }

        StartCoroutine(LoadAndAttachCoroutine(addressableKey, parent, localPos, localRotationEuler, localScale, onCompletedEventName));
    }

    private IEnumerator LoadAndAttachCoroutine(string key, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, string eventName)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject instance = Instantiate(handle.Result, parent);
            
            instance.transform.localPosition = pos;
            instance.transform.localEulerAngles = rot;
            instance.transform.localScale = (scale == Vector3.zero) ? Vector3.one : scale;

            Debug.Log($"[ARAssetBridge] Loaded: {key}");

            // GỬI ĐỐI TƯỢNG VỀ VISUAL SCRIPTING
            // Arg0 chính là GameObject vừa được tạo ra
            if (!string.IsNullOrEmpty(eventName))
            {
                CustomEvent.Trigger(gameObject, eventName, instance);
            }
        }
        else
        {
            Debug.LogError($"[ARAssetBridge] Lỗi load key: {key}");
        }
    }

    // --- CÁC HÀM BỔ TRỢ ---
    public void SetLocalScale(GameObject target, Vector3 scale)
    {
        if (target != null) target.transform.localScale = scale;
    }

    public void SetLocalRotation(GameObject target, Vector3 eulerAngles)
    {
        if (target != null) target.transform.localEulerAngles = eulerAngles;
    }
}