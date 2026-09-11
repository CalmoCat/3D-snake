using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Меню, которое не зависит от префабов исходного билда. Оно создаётся из
/// стандартных UGUI-компонентов и открывает одиночную или DUO-сцену.
/// </summary>
public class RuntimeMainMenu : MonoBehaviour
{
    public int singlePlayerSceneIndex = 1;
    public int twoPlayerSceneIndex = 2;

    private Font font;
    private Color background = new Color(0.018f, 0.045f, 0.1f, 1f);
    private Color accent = new Color(0.15f, 0.9f, 0.95f, 1f);

    private void Awake()
    {
        GameSettings.Load();
        GameSettings.IsGameplayStarted = false;
        Time.timeScale = 0f;
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        MainMenu menu = GetComponent<MainMenu>();
        if (menu == null) menu = gameObject.AddComponent<MainMenu>();
        menu.gameSceneBuildIndex = singlePlayerSceneIndex;
        menu.twoPlayerSceneBuildIndex = twoPlayerSceneIndex;
        menu.menuRoot = gameObject;

        BuildMenu(menu);
    }

    private void BuildMenu(MainMenu menu)
    {
        GameObject canvasObject = new GameObject("MenuCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Image backgroundImage = canvasObject.AddComponent<Image>();
        backgroundImage.color = background;
        backgroundImage.raycastTarget = false;
        SetRect(backgroundImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject panel = new GameObject("MainPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        SetRect(panel.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 570f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.025f, 0.075f, 0.16f, 0.96f);

        Text title = CreateText(panel.transform, "NEON RUNNER", 64, TextAnchor.MiddleCenter, accent);
        SetRect(title.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
        Text subtitle = CreateText(panel.transform, "3D SNAKE  ·  ARCADE EDITION", 17, TextAnchor.MiddleCenter, new Color(0.65f, 0.78f, 0.88f));
        SetRect(subtitle.rectTransform, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.73f), Vector2.zero, Vector2.zero);

        Button single = CreateButton(panel.transform, "ОДИНОЧНАЯ ИГРА", new Vector2(0.13f, 0.43f), new Vector2(0.87f, 0.6f), menu.StartGame);
        Button duo = CreateButton(panel.transform, "ИГРА НА ДВОИХ", new Vector2(0.13f, 0.24f), new Vector2(0.87f, 0.41f), menu.StartTwoPlayerGame);
        CreateText(panel.transform, "P1: A / D     P2: ← / →     M: сменить карту", 16, TextAnchor.MiddleCenter, new Color(0.65f, 0.82f, 0.9f));
        Text hint = panel.transform.GetChild(panel.transform.childCount - 1).GetComponent<Text>();
        SetRect(hint.rectTransform, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.17f), Vector2.zero, Vector2.zero);
        Text map = CreateText(panel.transform, ArenaMapLibrary.Get(GameSettings.MapIndex).name + "  ·  четыре карты", 15, TextAnchor.MiddleCenter, new Color(0.55f, 0.75f, 0.8f));
        SetRect(map.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.09f), Vector2.zero, Vector2.zero);

        menu.mainPanel = panel;
        menu.settingsPanel = null;
    }

    private Text CreateText(Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject("Text_" + value);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button_" + label);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.07f, 0.36f, 0.46f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.15f, 0.8f, 0.78f, 1f);
        colors.pressedColor = new Color(0.04f, 0.17f, 0.24f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);
        SetRect(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
        Text text = CreateText(buttonObject.transform, label, 22, TextAnchor.MiddleCenter, Color.white);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
