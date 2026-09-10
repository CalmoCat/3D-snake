using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Меню без принудительного изменения Canvas и без отключения слайдеров.</summary>
public class MainMenu : MonoBehaviour
{
    [Header("Сцены")]
    public int gameSceneBuildIndex = 1;
    public int twoPlayerSceneBuildIndex = 2;

    [Header("Панели")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject menuRoot;

    private UISettingsManager settingsManager;

    private void Awake()
    {
        if (menuRoot == null) menuRoot = gameObject;
        settingsManager = FindFirstObjectByType<UISettingsManager>();
        EnsureEventSystemExists();
    }

    private void Start()
    {
        if (GameSettings.IsGameplayStarted)
        {
            HideMenu();
            return;
        }
        ShowMainPanel();
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        LoadGameScene(gameSceneBuildIndex);
    }

    public void StartTwoPlayerGame()
    {
        LoadGameScene(twoPlayerSceneBuildIndex);
    }

    private void LoadGameScene(int sceneIndex)
    {
        GameSettings.IsGameplayStarted = true;
        GameSettings.Save();
        Time.timeScale = 1f;
        if (SceneManager.GetActiveScene().buildIndex == sceneIndex)
        {
            HideMenu();
            return;
        }
        SceneManager.LoadScene(sceneIndex);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (settingsManager != null)
        {
            settingsManager.SetSlidersVisible(true);
            settingsManager.RefreshValueLabels();
        }
    }

    public void CloseSettings() => ShowMainPanel();

    public void ShowMainPanel()
    {
        if (menuRoot != null) menuRoot.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsManager != null) settingsManager.SetSlidersVisible(false);
        Time.timeScale = 0f;
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void HideMenu()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuRoot != null && menuRoot != gameObject) menuRoot.SetActive(false);
        if (settingsManager != null) settingsManager.SetSlidersVisible(false);
    }

    public void OnStartButton() => StartGame();
    public void OnTwoPlayerButton() => StartTwoPlayerGame();
    public void OnExitButton() => ExitGame();
    public void OnSettingsButton() => OpenSettings();
    public void OnMenuButton() => ShowMainPanel();

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
