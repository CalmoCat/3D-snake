using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Надёжная привязка настроек: значения и слайдеры больше не расходятся.</summary>
public class UISettingsManager : MonoBehaviour
{
    [Header("Настройки змеи")]
    public Slider snakeSpeedSlider;
    public TMP_Text snakeSpeedValueText;
    public Slider snakeRotationSlider;
    public TMP_Text snakeRotationValueText;

    [Header("Настройки трафика")]
    public Slider carsOnMapSlider;
    public TMP_Text carsOnMapValueText;
    public bool enableUiDiagnostics = true;

    private void Start()
    {
        GameSettings.Load();
        ConfigureSlider(snakeSpeedSlider, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed, GameSettings.SnakeSpeed);
        ConfigureSlider(snakeRotationSlider, GameSettings.MinRotationSpeed, GameSettings.MaxRotationSpeed, GameSettings.SnakeRotationSpeed);
        ConfigureSlider(carsOnMapSlider, GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap, GameSettings.MaxCarsOnMapValue);
        RefreshValueLabels();
        SetSlidersVisible(false);
    }

    public void SetSlidersVisible(bool visible)
    {
        if (snakeSpeedSlider != null) snakeSpeedSlider.gameObject.SetActive(visible);
        if (snakeRotationSlider != null) snakeRotationSlider.gameObject.SetActive(visible);
        if (carsOnMapSlider != null) carsOnMapSlider.gameObject.SetActive(visible);
        if (snakeSpeedValueText != null) snakeSpeedValueText.gameObject.SetActive(visible);
        if (snakeRotationValueText != null) snakeRotationValueText.gameObject.SetActive(visible);
        if (carsOnMapValueText != null) carsOnMapValueText.gameObject.SetActive(visible);
    }

    public void SetSnakeSpeed20() => SetSnakeSpeedPreset(20f);
    public void SetSnakeSpeed30() => SetSnakeSpeedPreset(30f);
    public void SetSnakeSpeed50() => SetSnakeSpeedPreset(50f);
    public void SetRotationSpeed200() => SetRotationSpeedPreset(200f);
    public void SetRotationSpeed300() => SetRotationSpeedPreset(300f);
    public void SetRotationSpeed500() => SetRotationSpeedPreset(500f);
    public void SetCarsOnMap10() => SetCarsOnMapPreset(10);
    public void SetCarsOnMap20() => SetCarsOnMapPreset(20);
    public void SetCarsOnMap40() => SetCarsOnMapPreset(18);

    public void OnSnakeSpeedChanged(float value)
    {
        GameSettings.SnakeSpeed = Mathf.Clamp(value, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed);
        GameSettings.Save();
        RefreshValueLabels();
    }

    public void OnSnakeRotationChanged(float value)
    {
        GameSettings.SnakeRotationSpeed = Mathf.Clamp(value, GameSettings.MinRotationSpeed, GameSettings.MaxRotationSpeed);
        GameSettings.Save();
        RefreshValueLabels();
    }

    public void OnCarsOnMapChanged(float value)
    {
        GameSettings.MaxCarsOnMapValue = Mathf.Clamp(Mathf.RoundToInt(value), GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        GameSettings.Save();
        RefreshValueLabels();
    }

    public void RefreshValueLabels()
    {
        if (snakeSpeedSlider != null) snakeSpeedSlider.SetValueWithoutNotify(GameSettings.SnakeSpeed);
        if (snakeRotationSlider != null) snakeRotationSlider.SetValueWithoutNotify(GameSettings.SnakeRotationSpeed);
        if (carsOnMapSlider != null) carsOnMapSlider.SetValueWithoutNotify(GameSettings.MaxCarsOnMapValue);
        if (snakeSpeedValueText != null) snakeSpeedValueText.text = "Скорость: " + Mathf.RoundToInt(GameSettings.SnakeSpeed);
        if (snakeRotationValueText != null) snakeRotationValueText.text = "Поворот: " + Mathf.RoundToInt(GameSettings.SnakeRotationSpeed);
        if (carsOnMapValueText != null) carsOnMapValueText.text = "Машин: " + GameSettings.MaxCarsOnMapValue;
    }

    public void DiagnoseSettingsPanel(GameObject settingsPanel)
    {
        if (!enableUiDiagnostics || settingsPanel == null) return;
        Debug.Log("[Settings] panel=" + settingsPanel.name + " active=" + settingsPanel.activeInHierarchy);
        Debug.Log("[Settings] speed=" + GameSettings.SnakeSpeed + " rotation=" + GameSettings.SnakeRotationSpeed + " cars=" + GameSettings.MaxCarsOnMapValue);
    }

    private void SetSnakeSpeedPreset(float value)
    {
        GameSettings.SnakeSpeed = Mathf.Clamp(value, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed);
        GameSettings.Save();
        RefreshValueLabels();
    }

    private void SetRotationSpeedPreset(float value)
    {
        GameSettings.SnakeRotationSpeed = Mathf.Clamp(value, GameSettings.MinRotationSpeed, GameSettings.MaxRotationSpeed);
        GameSettings.Save();
        RefreshValueLabels();
    }

    private void SetCarsOnMapPreset(int value)
    {
        GameSettings.MaxCarsOnMapValue = Mathf.Clamp(value, GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        GameSettings.Save();
        RefreshValueLabels();
    }

    private void ConfigureSlider(Slider slider, float min, float max, float value)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = min >= 1f && max <= 40f;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
        slider.onValueChanged.RemoveAllListeners();
        if (slider == snakeSpeedSlider) slider.onValueChanged.AddListener(OnSnakeSpeedChanged);
        else if (slider == snakeRotationSlider) slider.onValueChanged.AddListener(OnSnakeRotationChanged);
        else if (slider == carsOnMapSlider) slider.onValueChanged.AddListener(OnCarsOnMapChanged);
    }
}
