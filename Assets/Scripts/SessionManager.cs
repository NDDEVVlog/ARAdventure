using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset cardTemplate;

    [Header("Addressables Settings")]
    [SerializeField] private AssetLabelReference sessionLabel;

    [Header("Icons")]
    public Sprite homeSprite;
    public Sprite achieveSprite;

    private ScrollView mainScrollView;
    private Button refreshBtn;
    private VisualElement loadingOverlay;

    private List<AsyncOperationHandle> loadedAssetHandles = new List<AsyncOperationHandle>();
    private AsyncOperationHandle<IList<IResourceLocation>> locationsHandle;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // --- KHỚP TÊN VỚI UXML ---
        mainScrollView = root.Q<ScrollView>("MainScroll");
        var homeIcon = root.Q<VisualElement>("HomeIcon");
        var achieveIcon = root.Q<VisualElement>("AchieveIcon");
        refreshBtn = root.Q<Button>("RefreshBtn");
        loadingOverlay = root.Q<VisualElement>("LoadingOverlay");

        // Gán Icon
        if (homeIcon != null && homeSprite != null) homeIcon.style.backgroundImage = new StyleBackground(homeSprite);
        if (achieveIcon != null && achieveSprite != null) achieveIcon.style.backgroundImage = new StyleBackground(achieveSprite);
        
        if (refreshBtn != null) refreshBtn.clicked += OnRefreshClicked;

        // Pull to Refresh Logic
        if (mainScrollView != null)
        {
            mainScrollView.RegisterCallback<PointerUpEvent>(evt => {
                if (mainScrollView.verticalScroller.value < -50f || mainScrollView.scrollOffset.y < -50f)
                {
                    OnRefreshClicked();
                }
            });
        }

        CheckAndLoadData();
    }

    private void CheckAndLoadData()
    {
        if (loadingOverlay != null) loadingOverlay.style.display = DisplayStyle.Flex;

        Addressables.CheckForCatalogUpdates(false).Completed += checkHandle =>
        {
            if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
            {
                Addressables.UpdateCatalogs(checkHandle.Result, false).Completed += updateHandle => {
                    LoadAllSessions();
                };
            }
            else
            {
                LoadAllSessions();
            }
        };
    }

    private void OnRefreshClicked()
    {
        if (refreshBtn != null && !refreshBtn.enabledSelf) return;
        if (refreshBtn != null) refreshBtn.SetEnabled(false);
        
        ClearCurrentCards();
        CheckAndLoadData();
    }

    private void LoadAllSessions()
    {
        locationsHandle = Addressables.LoadResourceLocationsAsync(sessionLabel);
        locationsHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var locations = handle.Result;
                if (locations.Count == 0) { FinishLoading(); return; }

                int completedCount = 0;
                foreach (var location in locations)
                {
                    var loadHandle = Addressables.LoadAssetAsync<GameSessionData>(location);
                    loadHandle.Completed += objHandle =>
                    {
                        completedCount++;
                        if (objHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            loadedAssetHandles.Add(objHandle);
                            SpawnCardUI(objHandle.Result);
                        }
                        if (completedCount == locations.Count) FinishLoading();
                    };
                }
            }
            else FinishLoading();
        };
    }

    private void FinishLoading()
    {
        if (refreshBtn != null) refreshBtn.SetEnabled(true);
        if (loadingOverlay != null) loadingOverlay.style.display = DisplayStyle.None;
    }

    private void SpawnCardUI(GameSessionData data)
    {
        if (cardTemplate == null || mainScrollView == null) return;

        VisualElement card = cardTemplate.Instantiate();
        mainScrollView.Add(card);

        // Binding đúng ID trong Card.uxml
        var titleLabel = card.Q<Label>("Title");
        var locLabel = card.Q<Label>("Sub");
        var descLabel = card.Q<Label>("Desc");
        var thumbElement = card.Q<VisualElement>("Image");
        var ratingLabel = card.Q<Label>("Rating");
        var playBtn = card.Q<Button>("PlayBtn");
        var progressContainer = card.Q<VisualElement>("ProgressBar");
        var progressFill = card.Q<VisualElement>("ProgressFill");

        if (titleLabel != null) titleLabel.text = data.sessionName;
        if (locLabel != null) locLabel.text = data.location;
        if (descLabel != null) descLabel.text = data.description;
        if (ratingLabel != null) ratingLabel.text = $"★ {(Random.Range(40, 50) / 10f):F1}";

        if (thumbElement != null && data.thumbnail != null && data.thumbnail.RuntimeKeyIsValid())
        {
            var thumbHandle = data.thumbnail.LoadAssetAsync<Sprite>();
            thumbHandle.Completed += h => {
                if (h.Status == AsyncOperationStatus.Succeeded)
                    thumbElement.style.backgroundImage = new StyleBackground(h.Result);
            };
            loadedAssetHandles.Add(thumbHandle);
        }

        if (playBtn != null) UpdateBtnStatus(playBtn, progressContainer, progressFill, data);
    }

    private void UpdateBtnStatus(Button btn, VisualElement pBar, VisualElement pFill, GameSessionData data)
    {
        Addressables.GetDownloadSizeAsync(data.sceneAddressableKey).Completed += handle =>
        {
            btn.clickable = new Clickable(() => { }); // Reset click
            if (handle.Result > 0)
            {
                btn.text = $"Download ({(handle.Result / 1048576f):F1}MB)";
                btn.clicked += () => StartCoroutine(DownloadSequence(btn, pBar, pFill, data));
            }
            else
            {
                btn.text = "Play Now";
                btn.clicked += () => Addressables.LoadSceneAsync(data.sceneAddressableKey);
            }
        };
    }

    private IEnumerator DownloadSequence(Button btn, VisualElement pBar, VisualElement pFill, GameSessionData data)
    {
        btn.SetEnabled(false);
        if (pBar != null) pBar.style.display = DisplayStyle.Flex;

        var downloadHandle = Addressables.DownloadDependenciesAsync(data.sceneAddressableKey);
        while (!downloadHandle.IsDone)
        {
            float p = downloadHandle.PercentComplete;
            if (pFill != null) pFill.style.width = Length.Percent(p * 100);
            btn.text = $"{(p * 100):F0}%";
            yield return null;
        }

        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            btn.text = "Play Now";
            btn.SetEnabled(true);
            if (pBar != null) pBar.style.display = DisplayStyle.None;
            btn.clickable = new Clickable(() => Addressables.LoadSceneAsync(data.sceneAddressableKey));
        }
    }

    private void ClearCurrentCards()
    {
        mainScrollView?.Clear();
        foreach (var h in loadedAssetHandles) if (h.IsValid()) Addressables.Release(h);
        loadedAssetHandles.Clear();
    }

    private void OnDisable()
    {
        ClearCurrentCards();
        if (locationsHandle.IsValid()) Addressables.Release(locationsHandle);
        if (refreshBtn != null) refreshBtn.clicked -= OnRefreshClicked;
    }
}