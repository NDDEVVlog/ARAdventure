using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;
using System; // Thêm cái này

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTrackingEventHandler : MonoBehaviour
{
    private ARTrackedImageManager _trackedImageManager;

    [System.Serializable]
    public struct ImageEvent
    {
        public string imageName;
        public UnityEvent onDetected;
        public UnityEvent onLost;
    }

    public List<ImageEvent> imageEvents = new List<ImageEvent>();
    private Dictionary<string, bool> _trackingStatus = new Dictionary<string, bool>();

    void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        // GIẢI PHÁP 1: Nếu 'trackablesChanged' báo lỗi Read-only, hãy dùng lại 'trackedImagesChanged'
        // Trong AR Foundation 6.0, nó báo Obsolete (cảnh báo vàng) nhưng vẫn chạy được (không lỗi đỏ)
        _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // Dùng đúng kiểu dữ liệu ARTrackedImagesChangedEventArgs cho bản 6.0
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 1. Ảnh mới
        foreach (var trackedImage in eventArgs.added)
        {
            if (trackedImage.referenceImage != null)
                TriggerEvent(trackedImage.referenceImage.name, true);
        }

        // 2. Ảnh cập nhật
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.referenceImage != null)
            {
                if (trackedImage.trackingState == TrackingState.Tracking)
                    TriggerEvent(trackedImage.referenceImage.name, true);
                else
                    TriggerEvent(trackedImage.referenceImage.name, false);
            }
        }

        // 3. Ảnh bị mất
        foreach (var trackedImage in eventArgs.removed)
        {
             // Khi bị remove, không truy cập được referenceImage nữa nên ta dùng tên đã lưu
             // (Hàm TriggerEvent sẽ tự xử lý dựa trên name)
        }
    }

    private void TriggerEvent(string name, bool isDetected)
    {
        foreach (var imgEvent in imageEvents)
        {
            if (imgEvent.imageName == name)
            {
                if (!_trackingStatus.ContainsKey(name)) _trackingStatus[name] = false;

                if (isDetected && !_trackingStatus[name])
                {
                    Debug.Log($"[AR] Detected: {name}");
                    imgEvent.onDetected.Invoke();
                    _trackingStatus[name] = true;
                }
                else if (!isDetected && _trackingStatus[name])
                {
                    Debug.Log($"[AR] Lost: {name}");
                    imgEvent.onLost.Invoke();
                    _trackingStatus[name] = false;
                }
            }
        }
    }
}