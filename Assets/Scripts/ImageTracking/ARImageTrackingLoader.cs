using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading.Tasks;

public class ARImageTrackingLoader : MonoBehaviour
{
    [SerializeField] private AssetReference referenceImageLibraryAsset; 
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    private XRReferenceImageLibrary runtimeLibrary;
    private AsyncOperationHandle<XRReferenceImageLibrary> libraryHandle;

    private async void Start()
    {
        await LoadAndAssignLibrary();
    }

    private async Task LoadAndAssignLibrary()
    {
        Debug.Log("[ARLoader] Bắt đầu load XRReferenceImageLibrary...");

        if (referenceImageLibraryAsset == null)
        {
            Debug.LogError("[ARLoader] AssetReference ReferenceImageLibrary chưa được gán!");
            return;
        }

        libraryHandle = referenceImageLibraryAsset.LoadAssetAsync<XRReferenceImageLibrary>();
        await libraryHandle.Task;

        if (libraryHandle.Status == AsyncOperationStatus.Succeeded)
        {
            runtimeLibrary = libraryHandle.Result;
            
            Debug.Log($"[ARLoader] Load library thành công! Số ảnh trong library: {runtimeLibrary.count}");

            if (trackedImageManager == null)
            {
                trackedImageManager = GetComponent<ARTrackedImageManager>();
                Debug.LogWarning("[ARLoader] Tự tìm ARTrackedImageManager trên GameObject");
            }

            trackedImageManager.referenceLibrary = runtimeLibrary;

            // Set số ảnh di chuyển (tùy chọn)
            trackedImageManager.requestedMaxNumberOfMovingImages = 2;   // 1 hoặc 2 là đủ cho hầu hết trường hợp

            trackedImageManager.enabled = true;

            Debug.Log("[ARLoader] ĐÃ GÁN referenceLibrary và ENABLED ARTrackedImageManager");
            Debug.Log($"[ARLoader] Requested Max Moving Images: {trackedImageManager.requestedMaxNumberOfMovingImages}");
            Debug.Log($"[ARLoader] Current Max Moving Images: {trackedImageManager.currentMaxNumberOfMovingImages}");
        }
        else
        {
            Debug.LogError($"[ARLoader] Load library thất bại: {libraryHandle.OperationException}");
        }
    }

    private void OnDestroy()
    {
        if (libraryHandle.IsValid())
            Addressables.Release(libraryHandle);
    }
}