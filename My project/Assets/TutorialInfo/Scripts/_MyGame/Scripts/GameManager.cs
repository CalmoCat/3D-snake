using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Управляет состоянием забега, очками и жизнями. В старой версии столкновение
/// перезагружало сцену на каждый удар, из-за чего терялись объекты и появлялись
/// повторные события. Теперь игрок получает короткую неуязвимость и продолжает
/// забег с понятным состоянием.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum RunState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    public static GameManager Instance { get; private set; }
    public static int globalScore;
    public static int globalLives = 3;

    [Header("UI: TextMeshPro")]
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public TMP_Text levelText;
    public TMP_Text comboText;
    public TMP_Text powerText;
    public GameObject gameOverPanel;
    public GameObject pausePanel;

    [Header("UI: совместимость со старым Canvas")]
    public Text legacyScoreText;
    public Text legacyLivesText;
    public Text legacyLevelText;
    public Text legacyComboText;
    public Text legacyPowerText;

    [Header("Правила забега")]
    public int startingLives = 3;
    public float damageCooldown = 1.15f;
    public float comboWindow = 3.25f;
    public int scorePerCar = 10;

    public RunState State { get; private set; } = RunState.Menu;
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Level { get; private set; } = 1;
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int CarsEaten { get; private set; }
    public int PickupsCollected { get; private set; }
    public float RunTime { get; private set; }
    public float ShieldTime { get; private set; }
    public float MagnetTime { get; private set; }
    public float FuryTime { get; private set; }
    public float SlowMotionTime { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public float DifficultyProgress { get; private set; }

    public event Action<int> ScoreChanged;
    public event Action<RunState> StateChanged;
    public event Action<string> ToastRequested;

    private float comboTimer;
    private float damageTimer;
    private int nextLevelScore = 100;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameSettings.Load();
        ResolveReferences();
        BeginRun(GameSettings.IsGameplayStarted);
    }

    private void Start()
    {
        ResolveReferences();
        UpdateUI();
        SetPanel(gameOverPanel, State == RunState.GameOver);
        SetPanel(pausePanel, State == RunState.Paused);
    }

    private void Update()
    {
        if (State == RunState.Playing)
        {
            float realDelta = Time.unscaledDeltaTime;
            RunTime += realDelta;
            damageTimer = Mathf.Max(0f, damageTimer - realDelta);
            IsInvulnerable = damageTimer > 0f;
            ShieldTime = Mathf.Max(0f, ShieldTime - realDelta);
            MagnetTime = Mathf.Max(0f, MagnetTime - realDelta);
            FuryTime = Mathf.Max(0f, FuryTime - realDelta);
            SlowMotionTime = Mathf.Max(0f, SlowMotionTime - realDelta);
            if (SlowMotionTime <= 0f && Time.timeScale < 0.99f) Time.timeScale = 1f;

            if (comboTimer > 0f)
            {
                comboTimer -= realDelta;
                if (comboTimer <= 0f) Combo = 0;
            }

            if (SnakeMovement.Active != null)
            {
                DifficultyProgress = Mathf.Clamp01(SnakeMovement.Active.transform.position.magnitude / 250f);
            }
            UpdateUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    public void BeginRun(bool gameplayStarted = true)
    {
        GameSettings.IsGameplayStarted = gameplayStarted;
        Score = 0;
        Lives = Mathf.Max(1, startingLives);
        Level = 1;
        Combo = 0;
        BestCombo = 0;
        CarsEaten = 0;
        PickupsCollected = 0;
        RunTime = 0f;
        ShieldTime = 0f;
        MagnetTime = 0f;
        FuryTime = 0f;
        SlowMotionTime = 0f;
        comboTimer = 0f;
        damageTimer = 0f;
        IsInvulnerable = false;
        nextLevelScore = 100;
        initialized = true;
        globalScore = 0;
        globalLives = Lives;
        SetState(gameplayStarted ? RunState.Playing : RunState.Menu);
        Time.timeScale = gameplayStarted ? 1f : 0f;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0 || State == RunState.GameOver) return;

        int multiplier = Mathf.Max(1, 1 + Mathf.Max(0, Combo - 1) / 3);
        int awarded = Mathf.RoundToInt(amount * multiplier * (FuryTime > 0f ? 2f : 1f));
        Score += awarded;
        globalScore = Score;
        ScoreChanged?.Invoke(Score);

        while (Score >= nextLevelScore)
        {
            Level++;
            nextLevelScore += 100 + Level * 35;
            ToastRequested?.Invoke("Уровень " + Level + "!");
        }
        UpdateUI();
    }

    public void RegisterCarEaten()
    {
        CarsEaten++;
        Combo = Mathf.Clamp(Combo + 1, 1, 99);
        BestCombo = Mathf.Max(BestCombo, Combo);
        comboTimer = comboWindow;
        AddScore(scorePerCar);
        UpdateUI();
    }

    public void CollectPickup(PickupType type, int value = 1)
    {
        if (State != RunState.Playing) return;
        PickupsCollected++;

        switch (type)
        {
            case PickupType.Growth:
                SnakeBody body = FindFirstObjectByType<SnakeBody>();
                if (body != null) body.AddSegments(Mathf.Max(1, value));
                AddScore(25);
                ToastRequested?.Invoke("Рост +" + Mathf.Max(1, value));
                break;
            case PickupType.Shield:
                ShieldTime = Mathf.Max(ShieldTime, 8f + value);
                AddScore(20);
                ToastRequested?.Invoke("Щит активирован");
                break;
            case PickupType.Magnet:
                MagnetTime = Mathf.Max(MagnetTime, 10f + value);
                AddScore(20);
                ToastRequested?.Invoke("Магнит на 10 секунд");
                break;
            case PickupType.Fury:
                FuryTime = Mathf.Max(FuryTime, 8f + value);
                AddScore(30);
                ToastRequested?.Invoke("Ярость: двойные очки");
                break;
            case PickupType.SlowMotion:
                SlowMotionTime = Mathf.Max(SlowMotionTime, 6f + value);
                Time.timeScale = 0.55f;
                AddScore(35);
                ToastRequested?.Invoke("Время замедлено");
                break;
            default:
                AddScore(10);
                break;
        }
        UpdateUI();
    }

    public void TakeDamage()
    {
        if (!initialized || State != RunState.Playing || damageTimer > 0f) return;
        damageTimer = damageCooldown;
        Combo = 0;
        comboTimer = 0f;

        if (ShieldTime > 0f)
        {
            ShieldTime = 0f;
            ToastRequested?.Invoke("Щит поглотил удар");
            SnakeMovement.Active?.RespawnAfterHit(true);
            UpdateUI();
            return;
        }

        Lives = Mathf.Max(0, Lives - 1);
        globalLives = Lives;
        if (Lives == 0)
        {
            GameOver();
            return;
        }

        ToastRequested?.Invoke("Осторожно! Жизни: " + Lives);
        SnakeBody body = FindFirstObjectByType<SnakeBody>();
        if (body != null) body.CutTail(Mathf.Max(2, body.SegmentCount - 2));
        SnakeMovement.Active?.RespawnAfterHit(false);
        UpdateUI();
    }

    public void GameOver()
    {
        if (State == RunState.GameOver) return;
        SetState(RunState.GameOver);
        GameSettings.IsGameplayStarted = false;
        Time.timeScale = 0f;
        SetPanel(gameOverPanel, true);
        UpdateUI();
    }

    public void TogglePause()
    {
        if (State == RunState.GameOver || State == RunState.Menu) return;
        if (State == RunState.Paused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (State != RunState.Playing) return;
        SetState(RunState.Paused);
        Time.timeScale = 0f;
        SetPanel(pausePanel, true);
    }

    public void ResumeGame()
    {
        if (State != RunState.Paused) return;
        SetState(RunState.Playing);
        Time.timeScale = SlowMotionTime > 0f ? 0.55f : 1f;
        SetPanel(pausePanel, false);
    }

    public void PlayAgain()
    {
        GameSettings.IsGameplayStarted = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMenu()
    {
        GameSettings.IsGameplayStarted = false;
        Time.timeScale = 1f;
        globalScore = 0;
        globalLives = startingLives;
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            // DemoScene — единственная сцена автономной версии, поэтому кнопка
            // «В меню» не должна оставлять игрока в пустом остановленном мире.
            PlayAgain();
        }
    }

    // Методы сохранены для уже настроенных Unity-кнопок старой сцены.
    public void FromStartButton() => PlayAgain();
    public void FromMenuButton() => ExitToMenu();

    private void SetState(RunState newState)
    {
        if (State == newState) return;
        State = newState;
        StateChanged?.Invoke(State);
    }

    private void ResolveReferences()
    {
        if (scoreText == null) scoreText = FindSceneText<TMP_Text>("score", "очки");
        if (livesText == null) livesText = FindSceneText<TMP_Text>("lives", "жизни", "life");
        if (levelText == null) levelText = FindSceneText<TMP_Text>("level", "уровень");
        if (comboText == null) comboText = FindSceneText<TMP_Text>("combo", "комбо");
        if (gameOverPanel == null) gameOverPanel = FindSceneObject("gameover", "game over");
        if (pausePanel == null) pausePanel = FindSceneObject("pause", "пауза");
    }

    private T FindSceneText<T>(params string[] parts) where T : Component
    {
        T[] texts = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || !texts[i].gameObject.scene.IsValid()) continue;
            string lower = texts[i].name.ToLowerInvariant();
            for (int p = 0; p < parts.Length; p++) if (lower.Contains(parts[p])) return texts[i];
        }
        return null;
    }

    private GameObject FindSceneObject(params string[] parts)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].gameObject.scene.IsValid()) continue;
            string lower = all[i].name.ToLowerInvariant();
            for (int p = 0; p < parts.Length; p++) if (lower.Contains(parts[p])) return all[i].gameObject;
        }
        return null;
    }

    private void UpdateUI()
    {
        SetText(scoreText, "ОЧКИ  " + Score);
        SetText(livesText, "ЖИЗНИ  " + Lives);
        SetText(levelText, "УР. " + Level);
        SetText(comboText, Combo > 1 ? "КОМБО x" + Combo : "КОМБО —");

        string powers = string.Empty;
        if (ShieldTime > 0f) powers += "ЩИТ " + Mathf.CeilToInt(ShieldTime) + "с  ";
        if (MagnetTime > 0f) powers += "МАГНИТ " + Mathf.CeilToInt(MagnetTime) + "с  ";
        if (FuryTime > 0f) powers += "ЯРОСТЬ " + Mathf.CeilToInt(FuryTime) + "с  ";
        if (SlowMotionTime > 0f) powers += "SLOW " + Mathf.CeilToInt(SlowMotionTime) + "с";
        SetText(powerText, powers);
        SetText(legacyScoreText, "ОЧКИ  " + Score);
        SetText(legacyLivesText, "ЖИЗНИ  " + Lives);
        SetText(legacyLevelText, "УР. " + Level);
        SetText(legacyComboText, Combo > 1 ? "КОМБО x" + Combo : "КОМБО —");
        SetText(legacyPowerText, powers);
        SetPanel(gameOverPanel, State == RunState.GameOver);
        SetPanel(pausePanel, State == RunState.Paused);
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private void SetText(Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
    }
}
