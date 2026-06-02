using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UISettingsManager : MonoBehaviour
{
    [Header("Настройки змеи")]
    public Slider snakeSpeedSlider;
    public TMP_Text snakeSpeedValueText;
    public Slider snakeRotationSlider;
    public TMP_Text snakeRotationValueText;

    [Header("Настройки спавна машин")]
    public Slider carsOnMapSlider;
    public TMP_Text carsOnMapValueText;

    [Header("Диагностика UI")]
    public bool enableUiDiagnostics = true;

    private Slider activeDragSlider;

    private void Start()
    {
        InitializeSettingsUi();
    }

    private void LateUpdate()
    {
    }

    private void InitializeSettingsUi()
    {
        if (snakeSpeedSlider != null) snakeSpeedSlider.gameObject.SetActive(false);
        if (snakeRotationSlider != null) snakeRotationSlider.gameObject.SetActive(false);
        if (carsOnMapSlider != null) carsOnMapSlider.gameObject.SetActive(false);

        RefreshValueLabels();
    }

    public void SetSlidersVisible(bool visible)
    {
        if (snakeSpeedSlider != null) snakeSpeedSlider.gameObject.SetActive(false);
        if (snakeRotationSlider != null) snakeRotationSlider.gameObject.SetActive(false);
        if (carsOnMapSlider != null) carsOnMapSlider.gameObject.SetActive(false);

        if (snakeSpeedValueText != null)
        {
            snakeSpeedValueText.gameObject.SetActive(visible);
        }
        if (snakeRotationValueText != null)
        {
            snakeRotationValueText.gameObject.SetActive(visible);
        }
        if (carsOnMapValueText != null)
        {
            carsOnMapValueText.gameObject.SetActive(visible);
        }
    }

    public void SetSnakeSpeed20() => SetSnakeSpeedPreset(20f);
    public void SetSnakeSpeed30() => SetSnakeSpeedPreset(30f);
    public void SetSnakeSpeed50() => SetSnakeSpeedPreset(50f);

    public void SetRotationSpeed200() => SetRotationSpeedPreset(200f);
    public void SetRotationSpeed300() => SetRotationSpeedPreset(300f);
    public void SetRotationSpeed500() => SetRotationSpeedPreset(500f);

    public void SetCarsOnMap10() => SetCarsOnMapPreset(10);
    public void SetCarsOnMap20() => SetCarsOnMapPreset(20);
    public void SetCarsOnMap40() => SetCarsOnMapPreset(40);

    private void SetSnakeSpeedPreset(float value)
    {
        GameSettings.SnakeSpeed = Mathf.Clamp(value, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed);
        RefreshValueLabels();
    }

    private void SetRotationSpeedPreset(float value)
    {
        GameSettings.SnakeRotationSpeed = Mathf.Clamp(value, GameSettings.MinRotationSpeed, GameSettings.MaxRotationSpeed);
        RefreshValueLabels();
    }

    private void SetCarsOnMapPreset(int value)
    {
        GameSettings.MaxCarsOnMapValue = Mathf.Clamp(value, GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        RefreshValueLabels();
    }

    public void OnSnakeSpeedChanged(float value)
    {
        GameSettings.SnakeSpeed = Mathf.Clamp(value, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed);
        RefreshValueLabels();
    }

    public void OnSnakeRotationChanged(float value)
    {
        GameSettings.SnakeRotationSpeed = Mathf.Clamp(value, GameSettings.MinRotationSpeed, GameSettings.MaxRotationSpeed);
        RefreshValueLabels();
    }

    public void OnCarsOnMapChanged(float value)
    {
        GameSettings.MaxCarsOnMapValue = Mathf.Clamp(Mathf.RoundToInt(value), GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        RefreshValueLabels();
    }

    private void RefreshValueLabels()
    {
        if (snakeSpeedValueText != null)
        {
            snakeSpeedValueText.text = $"Скорость змеи: {Mathf.RoundToInt(GameSettings.SnakeSpeed)}";
        }

        if (snakeRotationValueText != null)
        {
            snakeRotationValueText.text = $"Поворот змеи: {Mathf.RoundToInt(GameSettings.SnakeRotationSpeed)}";
        }

        if (carsOnMapValueText != null)
        {
            carsOnMapValueText.text = $"Машин на карте: {GameSettings.MaxCarsOnMapValue}";
        }
    }

    private void EnsureSliderBindings(Slider slider)
    {
        if (slider == null) return;

        RectTransform[] rects = slider.GetComponentsInChildren<RectTransform>(true);

        if (slider.handleRect == null)
        {
            slider.handleRect = FindRectByNamePart(rects, "handle");
        }

        if (slider.fillRect == null)
        {
            slider.fillRect = FindRectByNamePart(rects, "fill");
        }

        if (slider.targetGraphic == null && slider.handleRect != null)
        {
            Graphic handleGraphic = slider.handleRect.GetComponent<Graphic>();
            if (handleGraphic != null)
            {
                slider.targetGraphic = handleGraphic;
            }
        }

        Graphic[] graphics = slider.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null) continue;
            graphics[i].raycastTarget = true;
        }
    }

    private RectTransform FindRectByNamePart(RectTransform[] rects, string namePart)
    {
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null) continue;
            if (rect == (RectTransform)transform) continue;

            string lowerName = rect.name.ToLowerInvariant();
            if (lowerName.Contains(namePart))
            {
                return rect;
            }
        }
        return null;
    }

    private void LogAllSliderDiagnostics()
    {
        if (!enableUiDiagnostics) return;
        LogSliderDiagnostics(snakeSpeedSlider, "snakeSpeedSlider");
        LogSliderDiagnostics(snakeRotationSlider, "snakeRotationSlider");
        LogSliderDiagnostics(carsOnMapSlider, "carsOnMapSlider");
    }

    private void LogSliderDiagnostics(Slider slider, string fieldName)
    {
        if (slider == null)
        {
            Debug.LogWarning($"[UI-DIAG] {fieldName}: ссылка не назначена.");
            return;
        }

        string handleName = slider.handleRect != null ? slider.handleRect.name : "NULL";
        string fillName = slider.fillRect != null ? slider.fillRect.name : "NULL";
        string targetName = slider.targetGraphic != null ? slider.targetGraphic.name : "NULL";
        Debug.Log($"[UI-DIAG] {fieldName}({slider.name}) active={slider.gameObject.activeInHierarchy} interactable={slider.interactable} value={slider.value} min={slider.minValue} max={slider.maxValue} handle={handleName} fill={fillName} targetGraphic={targetName}");
    }

    private void HandleSliderDragFallback()
    {
        if (IsPointerPressedThisFrame())
        {
            activeDragSlider = GetSliderUnderPointer();
            if (activeDragSlider != null)
            {
                SetSliderValueFromPointer(activeDragSlider);
            }
        }

        if (IsPointerHeld() && activeDragSlider != null)
        {
            SetSliderValueFromPointer(activeDragSlider);
        }

        if (IsPointerReleasedThisFrame())
        {
            activeDragSlider = null;
        }
    }

    private Slider GetSliderUnderPointer()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return null;

        PointerEventData pointer = new PointerEventData(es)
        {
            position = GetPointerScreenPosition()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        es.RaycastAll(pointer, results);

        for (int i = 0; i < results.Count; i++)
        {
            Transform current = results[i].gameObject.transform;
            Slider sliderInParents = current.GetComponentInParent<Slider>();
            if (sliderInParents != null && sliderInParents.interactable)
            {
                return sliderInParents;
            }
        }

        return null;
    }

    private void SetSliderValueFromPointer(Slider slider)
    {
        if (slider == null) return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect == null) return;

        Camera uiCamera = GetUiCameraFor(sliderRect);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect, GetPointerScreenPosition(), uiCamera, out Vector2 localPoint))
        {
            return;
        }

        Rect rect = sliderRect.rect;
        float normalized = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float value = Mathf.Lerp(slider.minValue, slider.maxValue, Mathf.Clamp01(normalized));
        slider.value = value;
    }

    private bool IsPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
#endif
        return Input.GetMouseButtonDown(0);
    }

    private bool IsPointerHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
#endif
        return Input.GetMouseButton(0);
    }

    private bool IsPointerReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
#endif
        return Input.GetMouseButtonUp(0);
    }

    private Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    private Camera GetUiCameraFor(RectTransform rectTransform)
    {
        if (rectTransform == null) return null;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
        {
            return canvas.worldCamera;
        }

        return Camera.main;
    }

    public void DiagnoseSettingsPanel(GameObject settingsPanel)
    {
        if (settingsPanel == null || !enableUiDiagnostics) return;

        Debug.Log($"[DIAG] ===== SETTINGS PANEL DIAGNOSTICS =====");

        Debug.Log($"[DIAG] settingsPanel GameObject: {(settingsPanel != null ? settingsPanel.name : "NULL")}");
        Debug.Log($"[DIAG] settingsPanel.activeSelf: {(settingsPanel != null ? settingsPanel.activeSelf.ToString() : "NULL")}");
        Debug.Log($"[DIAG] settingsPanel.activeInHierarchy: {(settingsPanel != null ? settingsPanel.activeInHierarchy.ToString() : "NULL")}");

        if (settingsPanel != null)
        {
            Canvas canvas = settingsPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[DIAG] Canvas found: {canvas.name}, enabled: {canvas.enabled}, renderMode: {canvas.renderMode}");
            }
            else
            {
                Debug.LogWarning($"[DIAG] No Canvas found in parents of settingsPanel!");
            }

            CanvasGroup canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"[DIAG] CanvasGroup found: alpha={canvasGroup.alpha}, blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}");
            }
            else
            {
                Debug.Log($"[DIAG] No CanvasGroup on settingsPanel");
            }

            RectTransform rectTransform = settingsPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"[DIAG] RectTransform: anchoredPosition={rectTransform.anchoredPosition}, sizeDelta={rectTransform.sizeDelta}, scale={rectTransform.localScale}");
            }

            Graphic graphic = settingsPanel.GetComponent<Graphic>();
            if (graphic != null)
            {
                Debug.Log($"[DIAG] Graphic found: color={graphic.color}, raycastTarget={graphic.raycastTarget}");
            }

            Debug.Log($"[DIAG] settingsPanel children count: {settingsPanel.transform.childCount}");
            foreach (Transform child in settingsPanel.transform)
            {
                Debug.Log($"[DIAG] Child: {child.name}, active: {child.gameObject.activeSelf}");
            }
        }
    }
}
