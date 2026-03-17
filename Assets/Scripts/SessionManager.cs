using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    [SerializeField] private AssetReference cardPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private AssetLabelReference sessionLabel;
    [SerializeField] private Button refreshBtn;

    // Handle lưu trữ danh sách Data hiện tại để giải phóng RAM khi Refresh
    private AsyncOperationHandle<IList<GameSessionData>> currentDataHandle;

    private void Start()
    {
        refreshBtn.onClick.AddListener(OnRefreshClicked);
        CheckAndLoadData();
    }

    private void OnRefreshClicked()
    {
        refreshBtn.interactable = false;
        
        // 1. Xóa toàn bộ Card UI hiện tại
        ClearCurrentCards();

        // 2. GIẢI PHÓNG RAM CŨ: Bắt buộc phải làm để Addressables chịu tải dữ liệu mới
        if (currentDataHandle.IsValid())
        {
            Addressables.Release(currentDataHandle);
        }

        // 3. Bắt đầu check server lại từ đầu
        CheckAndLoadData();
    }

    private void CheckAndLoadData()
    {
        Debug.Log("[Addressables] Checking for Catalog Updates...");
        
        // Kiểm tra xem Server có bản Build Update nào mới không
        Addressables.CheckForCatalogUpdates(false).Completed += checkHandle =>
        {
            if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
            {
                Debug.Log($"[Addressables] Found {checkHandle.Result.Count} update(s). Updating Catalogs...");
                
                // Kéo Catalog mới về
                Addressables.UpdateCatalogs(checkHandle.Result, false).Completed += updateHandle =>
                {
                    Addressables.Release(updateHandle);
                    LoadAllSessions(); // Load data sau khi update catalog
                };
            }
            else
            {
                Debug.Log("[Addressables] No Catalog Updates found. Loading current data...");
                LoadAllSessions();
            }
            
            Addressables.Release(checkHandle); // Giải phóng bộ nhớ của quá trình check
        };
    }

    private void LoadAllSessions()
    {
        // Tải toàn bộ GameSessionData dựa trên Label
        currentDataHandle = Addressables.LoadAssetsAsync<GameSessionData>(sessionLabel, null);
        
        currentDataHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var sessionData in handle.Result)
                {
                    SpawnCard(sessionData);
                }
            }
            else
            {
                // THÊM DÒNG NÀY ĐỂ XEM LỖI LÀ GÌ
                Debug.LogError($"[Addressables] Failed to load Session Data! Error: {handle.OperationException}");
            }
            refreshBtn.interactable = true;
        };
    }

    private void SpawnCard(GameSessionData data)
    {
        cardPrefab.InstantiateAsync(contentParent).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                handle.Result.GetComponent<GameSessionCard>().SetupCard(data);
            }
        };
    }

    private void ClearCurrentCards()
    {
#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = null;
#endif
        // Phải xóa ngược từ dưới lên và dùng ReleaseInstance
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentParent.GetChild(i).gameObject;
            Addressables.ReleaseInstance(child);
        }
    }
}