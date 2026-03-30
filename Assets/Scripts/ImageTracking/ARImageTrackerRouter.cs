using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.VisualScripting;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageTrackerRouter : MonoBehaviour
{
    private ARTrackedImageManager _manager;
    private Dictionary<TrackableId, TrackingState> _lastStates = new Dictionary<TrackableId, TrackingState>();

    private void Awake()
    {
        _manager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (_manager != null)
            _manager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (_manager != null)
            _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // 1. Add mới
        foreach (var img in args.added)
        {
            if (!_lastStates.ContainsKey(img.trackableId))
            {
                _lastStates.Add(img.trackableId, TrackingState.None);
            }
        }

        // 2. Update trạng thái (Dễ tính hơn)
        foreach (var img in args.updated)
        {
            TrackingState currentState = img.trackingState;
            TrackingState previousState = _lastStates.ContainsKey(img.trackableId) ? _lastStates[img.trackableId] : TrackingState.None;

            if (currentState != previousState)
            {
                string name = img.referenceImage.name;

                // CẤP ĐỘ 1: FOUND (Dễ tính - Cứ thấy là báo)
                // Chuyển từ "Không thấy gì" sang "Thấy mờ mờ" hoặc "Thấy rõ"
                if (previousState == TrackingState.None && 
                   (currentState == TrackingState.Limited || currentState == TrackingState.Tracking))
                {
                    Dispatch(img, "found");
                    Debug.Log($"[Router] >>> FOUND (Easy): {name}");
                }

                // CẤP ĐỘ 2: TRACKING (Chính xác cao)
                // Chỉ kích hoạt khi trạng thái đạt mức Tracking chuẩn
                if (currentState == TrackingState.Tracking)
                {
                    Dispatch(img, "tracking");
                    Debug.Log($"[Router] >>> TRACKING (High Quality): {name}");
                }

                // CẤP ĐỘ 3: LOST (Chỉ khi mất hẳn)
                if (currentState == TrackingState.None)
                {
                    Dispatch(img, "lost");
                    Debug.Log($"[Router] >>> LOST: {name}");
                }

                _lastStates[img.trackableId] = currentState;
            }
        }

        // 3. Dọn dẹp

        foreach (var kvp in args.removed)
        {
            // kvp là KeyValuePair<TrackableId, ARTrackedImage>
            // kvp.Key chính là TrackableId
            TrackableId id = kvp.Key; 

            if (_lastStates.ContainsKey(id))
            {
                _lastStates.Remove(id);
                Debug.Log($"[Router] REMOVED: ID {id} đã được dọn dẹp khỏi bộ nhớ");
            }
        }
    }

    private void Dispatch(ARTrackedImage img, string state)
    {
        if (img == null || img.referenceImage == null) return;
        // Gửi Event kèm theo Transform và Hướng của ảnh
        CustomEvent.Trigger(gameObject, $"target.{img.referenceImage.name}.{state}", img.transform, img.transform.up);
    }
}