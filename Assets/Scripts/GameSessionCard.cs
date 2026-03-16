using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class GameSessionCard : MonoBehaviour
{
    [Header("Data Input")]
    public GameSessionData sessionData;

    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI descText;
    public Button actionBtn; 
    public TextMeshProUGUI btnText; 
    public Image progressBar; // Cần 1 Image set type là Filled

    private bool isDownloaded = false;
    private AsyncOperationHandle downloadHandle;

    public void SetupCard(GameSessionData data)
    {
        sessionData = data;
        
        // Điền text từ Data lên UI
        nameText.text = sessionData.sessionName;
        locationText.text = sessionData.location;
        descText.text = sessionData.description;
        
        if (progressBar) progressBar.fillAmount = 0;

        CheckContentStatus();
    }

    private void CheckContentStatus()
    {
        btnText.text = "Checking...";
        actionBtn.interactable = false;

        // Check dung lượng cần tải
        Addressables.GetDownloadSizeAsync(sessionData.sceneAddressableKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                long size = handle.Result;
                if (size > 0)
                {
                    isDownloaded = false;
                    btnText.text = $"Download ({(size / 1048576f):F1} MB)";
                    actionBtn.onClick.AddListener(StartDownload);
                }
                else
                {
                    isDownloaded = true;
                    btnText.text = "Play";
                    actionBtn.onClick.AddListener(PlayGame);
                }
                actionBtn.interactable = true;
            }
        };
    }

    private void StartDownload()
    {
        actionBtn.interactable = false;
        actionBtn.onClick.RemoveAllListeners(); 

        downloadHandle = Addressables.DownloadDependenciesAsync(sessionData.sceneAddressableKey);
        StartCoroutine(TrackProgress());

        downloadHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                isDownloaded = true;
                btnText.text = "Play";
                actionBtn.onClick.AddListener(PlayGame);
                actionBtn.interactable = true;
                if(progressBar) progressBar.gameObject.SetActive(false);
            }
            Addressables.Release(handle); 
        };
    }

    private IEnumerator TrackProgress()
    {
        if (progressBar) progressBar.gameObject.SetActive(true);
        while (!downloadHandle.IsDone)
        {
            float progress = downloadHandle.PercentComplete;
            if (progressBar) progressBar.fillAmount = progress;
            btnText.text = $"Downloading {(progress * 100):F0}%";
            yield return null;
        }
    }

    private void PlayGame()
    {
        if (isDownloaded) Addressables.LoadSceneAsync(sessionData.sceneAddressableKey);
    }
}