using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.IO;
using System.Text;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenu : MonoBehaviour
{
    [Header("Сцена игры")]
    public int gameSceneBuildIndex = 1;

    [Header("Панели")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Ссылки на компоненты")]
    public GameObject menuRoot;
    private UISettingsManager uiSettingsManager;
    private bool hasWrittenBuildUiDiagnostic;

    private void Awake()
    {
        ResolveMenuRoot();
        RepairMenuCanvas();
    }

    private void Start()
    {
        EnsureEventSystemExists();
        EnsureCanvasHasValidScale();
        RepairMenuCanvas();
        ResolveMenuRoot();
        FindUISettingsManager();

        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Debug.Log($"[MainMenu] Start: IsGameplayStarted={GameSettings.IsGameplayStarted}, mainPanel={mainPanel}, settingsPanel={settingsPanel}, uiSettingsManager={uiSettingsManager}");

        if (GameSettings.IsGameplayStarted)
        {
            ApplyMenuVisibility(false);
            Time.timeScale = 1f;
            return;
        }

        ApplyMenuVisibility(true);
        Time.timeScale = 0f;
        WriteBuildUiDiagnostic("Start");
    }

    private void LateUpdate()
    {
        if (!GameSettings.IsGameplayStarted && mainPanel != null && mainPanel.activeInHierarchy)
        {
            RepairMenuCanvas();
            ForcePanelVisible(mainPanel);
        }

        if (GameSettings.IsGameplayStarted && mainPanel != null && mainPanel.activeSelf)
        {
            ApplyMenuVisibility(false);
        }
    }

    public void StartGame()
    {
        ResolveMenuRoot();
        GameSettings.IsGameplayStarted = true;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex == gameSceneBuildIndex)
        {
            ApplyMenuVisibility(false);
            Time.timeScale = 1f;
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneBuildIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Игра закрыта");
    }

    public void OpenSettings()
    {
        ShowMainPanel();
    }

    public void ShowMainPanel()
    {
        RepairMenuCanvas();

        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel == null && settingsPanel == null)
        {
            gameObject.SetActive(true);
        }

        EnsureParentHierarchyActive(mainPanel != null ? mainPanel.transform : transform);
        ForcePanelVisible(mainPanel);
        WriteBuildUiDiagnostic("ShowMainPanel");
        
        if (uiSettingsManager != null)
        {
            uiSettingsManager.SetSlidersVisible(true);
        }
    }

    private void EnsureParentHierarchyActive(Transform child)
    {
        if (child == null) return;
        Transform current = child.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    private void ForcePanelVisible(GameObject panel)
    {
        if (panel == null) return;

        panel.transform.SetAsLastSibling();

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Canvas panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas == null)
        {
            panelCanvas = panel.AddComponent<Canvas>();
        }
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 500;

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            panel.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null) continue;
            g.enabled = true;
            g.canvasRenderer.SetAlpha(1f);
            g.canvasRenderer.cullTransparentMesh = false;
            Color c = g.color;
            if (c.a < 0.01f) c.a = 1f;
            g.color = c;
            g.SetMaterialDirty();
            g.SetVerticesDirty();
        }

        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;
            text.enabled = true;
            text.canvasRenderer.SetAlpha(1f);
            text.canvasRenderer.cullTransparentMesh = false;
            text.alpha = 1f;
            text.ForceMeshUpdate(false, false);
        }

        RectTransform[] rects = panel.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform r = rects[i];
            if (r == null) continue;
            if (r.localScale.sqrMagnitude < 0.0001f)
            {
                r.localScale = Vector3.one;
            }
        }
    }

    private void EnsureCanvasHasValidScale()
    {
        Canvas canvas = null;
        if (settingsPanel != null) canvas = settingsPanel.GetComponentInParent<Canvas>();
        if (canvas == null && mainPanel != null) canvas = mainPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform canvasTransform = canvas.transform;
        if (canvasTransform.localScale.sqrMagnitude < 0.0001f)
        {
            canvasTransform.localScale = Vector3.one;
            Debug.LogWarning("[MainMenu] Canvas scale was zero and has been reset to (1,1,1).");
        }
    }

    private void RepairMenuCanvas()
    {
        Canvas canvas = null;
        if (mainPanel != null) canvas = mainPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null && settingsPanel != null) canvas = settingsPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null) canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        canvas.pixelPerfect = false;
        canvas.worldCamera = null;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.localScale = Vector3.one;
            canvasRect.localPosition = Vector3.zero;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void WriteBuildUiDiagnostic(string source)
    {
        if (hasWrittenBuildUiDiagnostic) return;
        hasWrittenBuildUiDiagnostic = true;

        try
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Source={source}");
            builder.AppendLine($"Scene={SceneManager.GetActiveScene().name}");
            builder.AppendLine($"IsGameplayStarted={GameSettings.IsGameplayStarted}");
            builder.AppendLine($"TimeScale={Time.timeScale}");
            builder.AppendLine($"Screen={Screen.width}x{Screen.height}");
            builder.AppendLine($"PersistentDataPath={Application.persistentDataPath}");

            Canvas canvas = null;
            if (mainPanel != null) canvas = mainPanel.GetComponentInParent<Canvas>(true);
            if (canvas == null && settingsPanel != null) canvas = settingsPanel.GetComponentInParent<Canvas>(true);

            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                builder.AppendLine($"Canvas={canvas.name} enabled={canvas.enabled} renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder}");
                if (canvasRect != null)
                {
                    builder.AppendLine($"CanvasRect scale={canvasRect.localScale} size={canvasRect.rect.size} pos={canvasRect.anchoredPosition}");
                }
            }

            if (mainPanel != null)
            {
                builder.AppendLine($"MainPanel activeSelf={mainPanel.activeSelf} activeInHierarchy={mainPanel.activeInHierarchy}");
                Graphic[] graphics = mainPanel.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    Graphic g = graphics[i];
                    if (g == null) continue;
                    builder.AppendLine($"Graphic {g.name} type={g.GetType().Name} enabled={g.enabled} color={g.color} crAlpha={g.canvasRenderer.GetAlpha()}");
                }

                TMP_Text[] texts = mainPanel.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text text = texts[i];
                    if (text == null) continue;
                    builder.AppendLine($"TMP {text.name} text={text.text} alpha={text.alpha} font={text.font?.name} material={(text.fontSharedMaterial != null ? text.fontSharedMaterial.name : "NULL")}");
                }
            }

            string path = Path.Combine(Application.persistentDataPath, "mainmenu_ui_diag.txt");
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            Debug.Log($"[MainMenu] UI diagnostic written to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenu] Failed to write UI diagnostic: {ex}");
        }
    }

    private void HideAllMenuPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel == null && settingsPanel == null)
        {
            gameObject.SetActive(false);
        }
    }

    private void ApplyMenuVisibility(bool visible)
    {
        if (visible)
        {
            ShowMainPanel();
        }
        else
        {
            HideAllMenuPanels();
            if (uiSettingsManager != null)
            {
                uiSettingsManager.SetSlidersVisible(false);
            }
        }
    }

    private void ResolveMenuRoot()
    {
        if (menuRoot != null) return;
        menuRoot = mainPanel != null ? mainPanel : gameObject;
    }

    private void FindUISettingsManager()
    {
        if (uiSettingsManager != null) return;
        uiSettingsManager = FindFirstObjectByType<UISettingsManager>();
        if (uiSettingsManager == null)
        {
            Debug.LogWarning("[MainMenu] UISettingsManager not found in scene!");
        }
    }

    private void EnsureEventSystemExists()
    {
        EventSystem current = EventSystem.current;
        if (current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            current = eventSystemObject.AddComponent<EventSystem>();
        }

        EnsureBestInputModule(current.gameObject);
    }

    private void EnsureBestInputModule(GameObject eventSystemObject)
    {
        StandaloneInputModule legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (legacyModule == null) legacyModule = eventSystemObject.AddComponent<StandaloneInputModule>();
        legacyModule.enabled = true;

        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            Component inputSystemModule = eventSystemObject.GetComponent(inputSystemModuleType);
            if (inputSystemModule != null)
            {
                inputSystemModule.GetType().GetProperty("enabled")?.SetValue(inputSystemModule, false);
            }
        }
    }

    private bool HasValidInputActionsAsset(Component inputSystemModule)
    {
        if (inputSystemModule == null) return false;
        var property = inputSystemModule.GetType().GetProperty("actionsAsset");
        if (property == null) return false;
        object value = property.GetValue(inputSystemModule, null);
        return value != null;
    }

    public void OnStartButton()
    {
        StartGame();
    }

    public void OnExitButton()
    {
        ExitGame();
    }

    public void OnSettingsButton()
    {
        ShowMainPanel();
    }

    public void OnMenuButton()
    {
        ShowMainPanel();
    }
}
