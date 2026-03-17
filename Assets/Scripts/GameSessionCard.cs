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
    public Image progressBar;
    public Image thumbnailImage;

    private bool isDownloaded = false;
    
    // Lưu các Handle để dọn dẹp RAM khi bị xóa (Refresh)
    private AsyncOperationHandle downloadHandle;
    private AsyncOperationHandle<Sprite> thumbnailHandle;

    public void SetupCard(GameSessionData data)
    {
        sessionData = data;
        nameText.text = sessionData.sessionName;
        locationText.text = sessionData.location;
        descText.text = sessionData.description;
        if (progressBar) progressBar.fillAmount = 0;
        if (progressBar) progressBar.gameObject.SetActive(false);

        LoadThumbnail();
        CheckContentStatus();
    }

    private void LoadThumbnail()
    {
        if (sessionData.thumbnail != null && sessionData.thumbnail.RuntimeKeyIsValid())
        {
            thumbnailHandle = sessionData.thumbnail.LoadAssetAsync<Sprite>();
            thumbnailHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (thumbnailImage != null)
                    {
                        thumbnailImage.sprite = handle.Result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[Addressables] Failed to load thumbnail for {sessionData.sessionName}");
                }
            };
        }
    }

    private void CheckContentStatus()
    {
        btnText.text = "Checking...";
        actionBtn.interactable = false;

        Addressables.GetDownloadSizeAsync(sessionData.sceneAddressableKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                long size = handle.Result;

                if (size > 0)
                {
                    isDownloaded = false;
                    btnText.text = $"Download ({(size / 1048576f):F1} MB)";
                    actionBtn.onClick.RemoveAllListeners();
                    actionBtn.onClick.AddListener(StartDownload);
                }
                else
                {
                    isDownloaded = true;
                    btnText.text = "Play";
                    actionBtn.onClick.RemoveAllListeners();
                    actionBtn.onClick.AddListener(PlayGame);
                }
                actionBtn.interactable = true;
            }
            Addressables.Release(handle);
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
            else
            {
                Debug.LogError($"[Addressables] Download failed for {sessionData.sceneAddressableKey}");
                btnText.text = "Error! Retry";
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(StartDownload);
            }
        };
    }

    private IEnumerator TrackProgress()
    {
        if (progressBar) progressBar.gameObject.SetActive(true);
        while (downloadHandle.IsValid() && !downloadHandle.IsDone)
        {
            float progress = downloadHandle.PercentComplete;
            if (progressBar) progressBar.fillAmount = progress;
            btnText.text = $"Downloading {(progress * 100):F0}%";
            yield return null;
        }
    }

    private void PlayGame()
    {
        if (isDownloaded)
        {
            Addressables.LoadSceneAsync(sessionData.sceneAddressableKey);
        }
    }

    // BẮT BUỘC CÓ: Xóa RAM ảnh cũ và hủy tải xuống nếu người dùng bấm Refresh giữa chừng
    private void OnDestroy()
    {
        if (thumbnailHandle.IsValid())
        {
            Addressables.Release(thumbnailHandle);
        }

        if (downloadHandle.IsValid() && !downloadHandle.IsDone)
        {
            Addressables.Release(downloadHandle);
        }
    }
}