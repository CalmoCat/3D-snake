using UnityEngine;

/// <summary>
/// Все настройки запуска хранятся в одном месте. Настройки сохраняются между
/// запусками, но игровой прогресс намеренно сбрасывается при старте нового забега.
/// </summary>
public static class GameSettings
{
    public const float MinSnakeSpeed = 8f;
    public const float MaxSnakeSpeed = 32f;
    public const float MinRotationSpeed = 90f;
    public const float MaxRotationSpeed = 360f;
    public const int MinCarsOnMap = 3;
    public const int MaxCarsOnMap = 18;

    public static float SnakeSpeed = 16f;
    public static float SnakeRotationSpeed = 210f;
    public static int MaxCarsOnMapValue = 8;
    public static bool IsGameplayStarted = false;
    public static bool IsMuted = false;
    public static int SelectedDifficulty = 1;

    private const string SpeedKey = "snake.speed";
    private const string RotationKey = "snake.rotation";
    private const string CarsKey = "snake.cars";
    private const string MuteKey = "snake.mute";
    private const string DifficultyKey = "snake.difficulty";
    private static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        SnakeSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(SpeedKey, SnakeSpeed), MinSnakeSpeed, MaxSnakeSpeed);
        SnakeRotationSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(RotationKey, SnakeRotationSpeed), MinRotationSpeed, MaxRotationSpeed);
        MaxCarsOnMapValue = Mathf.Clamp(PlayerPrefs.GetInt(CarsKey, MaxCarsOnMapValue), MinCarsOnMap, MaxCarsOnMap);
        IsMuted = PlayerPrefs.GetInt(MuteKey, IsMuted ? 1 : 0) == 1;
        SelectedDifficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, SelectedDifficulty), 0, 2);
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(SpeedKey, SnakeSpeed);
        PlayerPrefs.SetFloat(RotationKey, SnakeRotationSpeed);
        PlayerPrefs.SetInt(CarsKey, MaxCarsOnMapValue);
        PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
        PlayerPrefs.SetInt(DifficultyKey, SelectedDifficulty);
        PlayerPrefs.Save();
    }

    public static void ResetSettings()
    {
        SnakeSpeed = 16f;
        SnakeRotationSpeed = 210f;
        MaxCarsOnMapValue = 8;
        IsMuted = false;
        SelectedDifficulty = 1;
        Save();
    }

    public static float DifficultySpeedMultiplier
    {
        get
        {
            switch (SelectedDifficulty)
            {
                case 0: return 0.82f;
                case 2: return 1.22f;
                default: return 1f;
            }
        }
    }

    public static float DifficultyTrafficMultiplier
    {
        get
        {
            switch (SelectedDifficulty)
            {
                case 0: return 0.7f;
                case 2: return 1.35f;
                default: return 1f;
            }
        }
    }
}
