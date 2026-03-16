using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class ARCameraToggle : MonoBehaviour
{
    [Header("Settings")]
    public bool startWithCameraOff = true; // Check this box in Inspector to start with camera OFF

    [Header("AR Components")]
    public ARCameraManager cameraManager;
    public ARSession arSession;

    [Header("UI Components")]
    public Button toggleButton;
    public TMP_Text buttonText;

    private bool isCameraOn = true;

    void Start()
    {
        // 1. Setup Button Click
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleCameraState);
        }

        // 2. Check if we should start with Camera OFF
        if (startWithCameraOff)
        {
            SetCameraState(false);
        }
        else
        {
            SetCameraState(true);
        }
    }

    // Function to handle the button click
    public void ToggleCameraState()
    {
        SetCameraState(!isCameraOn);
    }

    // The actual logic to turn things on/off
    private void SetCameraState(bool turnOn)
    {
        isCameraOn = turnOn;

        if (cameraManager != null) cameraManager.enabled = isCameraOn;
        if (arSession != null) arSession.enabled = isCameraOn;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (buttonText != null)
        {
            // If Camera is ON, button says "Disable"
            // If Camera is OFF, button says "Enable"
            buttonText.text = isCameraOn ? "Disable Camera" : "Enable Camera";
        }
    }
}