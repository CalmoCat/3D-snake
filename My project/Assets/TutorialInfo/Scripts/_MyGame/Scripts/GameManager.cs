using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static int globalScore = 0;
    public static int globalLives = 3;

    [Header("UI Ссылки")]
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public GameObject gameOverPanel;
    
    private bool isGameOver = false;
    private bool isProcessingDamage = false;

    private void Awake()
    {
        Instance = this;
        ResolveGameOverPanelReference();
        isGameOver = false;
        isProcessingDamage = false;
        SetGameOverPanelVisible(false);
        if (globalLives <= 0)
        {
            globalLives = 3;
            globalScore = 0;
        }
    }

    void Start()
    {
        Time.timeScale = GameSettings.IsGameplayStarted ? 1f : 0f;
        ResolveUiReferences();
        SetGameOverPanelVisible(false);
        UpdateUI();
    }

    void LateUpdate()
    {
        if (!isGameOver)
        {
            SetGameOverPanelVisible(false);
        }
    }

    public void AddScore(int amount)
    {
        globalScore += amount;
        UpdateUI();
    }

    public void TakeDamage()
    {
        if (isGameOver || isProcessingDamage) return;
        isProcessingDamage = true;
        globalLives--;
        UpdateUI();

        if (globalLives <= 0)
        {
            GameOver();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Очки: " + globalScore;
        if (livesText != null) livesText.text = "Жизни: " + globalLives;
        SetGameOverPanelVisible(isGameOver);
    }

    void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        SetGameOverPanelVisible(true);
    }

    private void SetGameOverPanelVisible(bool isVisible)
    {
        if (gameOverPanel == null) return;
        if (gameOverPanel.activeSelf != isVisible)
        {
            gameOverPanel.SetActive(isVisible);
        }
    }

    private void ResolveGameOverPanelReference()
    {
        if (gameOverPanel != null) return;
        gameOverPanel = FindSceneObjectByName("gameoverpanel");
        if (gameOverPanel == null) gameOverPanel = FindSceneObjectByName("game over panel");
        if (gameOverPanel == null) gameOverPanel = FindSceneObjectByName("gameover");
        if (gameOverPanel == null) gameOverPanel = FindSceneObjectByName("game over");

        if (gameOverPanel == null)
        {
            Debug.LogWarning("GameManager: не назначена Game Over панель (gameOverPanel). Назначьте её в инспекторе.");
        }
    }

    private void ResolveUiReferences()
    {
        ResolveGameOverPanelReference();

        if (scoreText == null)
        {
            scoreText = FindSceneTextByName("score");
            if (scoreText == null) scoreText = FindSceneTextByName("очки");
        }

        if (livesText == null)
        {
            livesText = FindSceneTextByName("lives");
            if (livesText == null) livesText = FindSceneTextByName("life");
            if (livesText == null) livesText = FindSceneTextByName("жизни");
            if (livesText == null) livesText = FindSceneTextByName("жизнь");
        }
    }

    private GameObject FindSceneObjectByName(string namePartLower)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (tr == null) continue;
            if (!tr.gameObject.scene.IsValid()) continue;
            if (tr.hideFlags != HideFlags.None) continue;

            string lowerName = tr.name.ToLowerInvariant();
            if (lowerName.Contains(namePartLower))
            {
                return tr.gameObject;
            }
        }
        return null;
    }

    private TMP_Text FindSceneTextByName(string namePartLower)
    {
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null) continue;
            if (!text.gameObject.scene.IsValid()) continue;
            if (text.hideFlags != HideFlags.None) continue;

            string lowerName = text.name.ToLowerInvariant();
            if (lowerName.Contains(namePartLower))
            {
                return text;
            }
        }
        return null;
    }

    public void PlayAgain()
    {
        GameSettings.IsGameplayStarted = true;
        Time.timeScale = 1f;
        globalScore = 0;
        globalLives = 3;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMenu()
    {
        GameSettings.IsGameplayStarted = false;
        Time.timeScale = 1f;
        globalScore = 0;
        globalLives = 3;
        SceneManager.LoadScene(0);
    }

    public void FromStartButton()
    {
        PlayAgain();
    }

    public void FromMenuButton()
    {
        ExitToMenu();
    }
}