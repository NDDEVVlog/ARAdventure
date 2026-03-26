using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.VisualScripting;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageTrackerRouter : MonoBehaviour
{
    private ARTrackedImageManager _manager;
    
    // Dùng TrackableId làm Key để phân biệt từng tấm ảnh vật lý riêng biệt
    private Dictionary<TrackableId, bool> _spawnedIds = new Dictionary<TrackableId, bool>();

    private void Awake()
    {
        _manager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable() => _manager.trackablesChanged.AddListener(OnTrackablesChanged);
    private void OnDisable() => _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // 1. Khi có ảnh mới xuất hiện (Added)
        foreach (var img in args.added)
        {   
            if (!_spawnedIds.ContainsKey(img.trackableId))
                _spawnedIds.Add(img.trackableId, false);
        }

        // 2. Cập nhật trạng thái (Updated)
        foreach (var img in args.updated)
        {   
            TrackableId id = img.trackableId;
            string imageName = img.referenceImage.name;

            if (img.trackingState == TrackingState.Tracking)
            {
                // Nếu ID này chưa được spawn vật thể
                if (_spawnedIds.ContainsKey(id) && _spawnedIds[id] == false)
                {
                    _spawnedIds[id] = true; 
                    Dispatch(img, "found");
                    Debug.Log($"[Router] Đã thấy ảnh '{imageName}' (ID: {id}). Đang tạo vật thể...");
                }
            }
            else if (img.trackingState == TrackingState.Limited)
            {
                // Khi mất dấu một ảnh cụ thể
                Lost(img);
            }
        }

        // 3. Khi ảnh bị xóa (Removed)
        foreach (var removed in args.removed)
        {
            _spawnedIds.Remove(removed.Value.trackableId);
        }
    }

    private void Dispatch(ARTrackedImage img, string state)
    {
        if (img == null || img.referenceImage == null) return;
        
        // Gửi kèm Transform của CHÍNH TẤM ẢNH ĐÓ (Arg. 0)
        // Visual Scripting sẽ dùng Transform này làm Parent để gắn Model vào đúng vị trí
        CustomEvent.Trigger(gameObject, $"target.{img.referenceImage.name}.{state}", img.transform);
    }

    private void Lost(ARTrackedImage img)
    {
        if (img == null || img.referenceImage == null) return;
        CustomEvent.Trigger(gameObject, $"target.{img.referenceImage.name}.lost", img.transform);
    }
}