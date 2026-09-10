using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Самодостаточная стартовая сцена игры. Проект поставлялся только с билдом и
/// скриптами без исходной сцены, поэтому этот bootstrap создаёт арену, игрока,
/// трафик, бонусы, свет и HUD в рантайме — достаточно открыть DemoScene.
/// </summary>
public class RuntimeGameBootstrap : MonoBehaviour
{
    [Header("Арена")]
    public Vector2 arenaSize = new Vector2(56f, 56f);
    public int obstacleCount = 14;
    public bool generateOnStart = true;

    private Transform worldRoot;
    private GameManager gameManager;
    private Font uiFont;

    private readonly Color deepBlue = new Color(0.025f, 0.055f, 0.12f);
    private readonly Color cyan = new Color(0.1f, 0.85f, 0.95f);
    private readonly Color mint = new Color(0.16f, 1f, 0.68f);
    private readonly Color panelBlue = new Color(0.035f, 0.08f, 0.17f, 0.94f);

    private void Awake()
    {
        if (!generateOnStart) return;
        Application.targetFrameRate = 60;
        GameSettings.Load();
        GameSettings.IsGameplayStarted = true;
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateManager();
        CreateLighting();
        CreateArena();
        Transform head = CreatePlayer();
        CreateTraffic();
        CreatePickups();
        CreateCamera(head);
        CreateHud();
    }

    private void CreateManager()
    {
        GameObject managerObject = new GameObject("GameManager");
        gameManager = managerObject.AddComponent<GameManager>();
        gameManager.startingLives = 3;
        gameManager.damageCooldown = 1.05f;
    }

    private void CreateLighting()
    {
        RenderSettings.ambientLight = new Color(0.08f, 0.12f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = deepBlue;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.009f;

        GameObject lightObject = new GameObject("MoonLight");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = new Color(0.62f, 0.8f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        GameObject rimObject = new GameObject("ArenaRimLight");
        Light rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Point;
        rim.range = 40f;
        rim.intensity = 5f;
        rim.color = cyan;
        rimObject.transform.position = new Vector3(0f, 5f, 5f);
    }

    private void CreateArena()
    {
        worldRoot = new GameObject("GeneratedArena").transform;
        CreateCube("ArenaFloor", new Vector3(0f, -0.5f, 0f), new Vector3(arenaSize.x, 1f, arenaSize.y), new Color(0.035f, 0.09f, 0.14f), worldRoot, true);

        float halfX = arenaSize.x * 0.5f;
        float halfZ = arenaSize.y * 0.5f;
        CreateHazardWall("NorthWall", new Vector3(0f, 1.25f, halfZ), new Vector3(arenaSize.x + 2f, 2.5f, 1f));
        CreateHazardWall("SouthWall", new Vector3(0f, 1.25f, -halfZ), new Vector3(arenaSize.x + 2f, 2.5f, 1f));
        CreateHazardWall("EastWall", new Vector3(halfX, 1.25f, 0f), new Vector3(1f, 2.5f, arenaSize.y));
        CreateHazardWall("WestWall", new Vector3(-halfX, 1.25f, 0f), new Vector3(1f, 2.5f, arenaSize.y));

        // Светящиеся полосы разметки дают ощущение дороги даже без текстур.
        for (int i = -5; i <= 5; i++)
        {
            CreateCube("LaneMark", new Vector3(i * 5f, 0.03f, 0f), new Vector3(0.08f, 0.03f, arenaSize.y - 4f), new Color(0.06f, 0.25f, 0.28f), worldRoot, false);
            CreateCube("LaneMark", new Vector3(0f, 0.035f, i * 5f), new Vector3(arenaSize.x - 4f, 0.03f, 0.08f), new Color(0.06f, 0.25f, 0.28f), worldRoot, false);
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            float angle = i * 2.39996f;
            float radius = 11f + (i % 4) * 3.1f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
            if (position.magnitude < 9f) continue;
            GameObject obstacle = CreateCube("Hazard_" + i.ToString("00"), position, new Vector3(1.8f, 2f, 1.8f), new Color(0.65f, 0.16f, 0.28f), worldRoot, true);
            obstacle.AddComponent<ArenaHazard>();
            obstacle.transform.rotation = Quaternion.Euler(0f, i * 19f, 0f);
        }
    }

    private Transform CreatePlayer()
    {
        GameObject snakeRoot = new GameObject("PlayerSnake");
        snakeRoot.transform.position = new Vector3(0f, 0.8f, -5f);
        SnakeBody body = snakeRoot.AddComponent<SnakeBody>();
        body.startSegments = 5;
        body.spacing = 1.35f;
        body.lerpSpeed = 24f;
        body.bodyColor = mint;
        body.tailColor = new Color(0.05f, 0.34f, 0.42f);

        GameObject headObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headObject.name = "SnakeHead";
        headObject.transform.SetParent(snakeRoot.transform);
        headObject.transform.localPosition = Vector3.zero;
        headObject.transform.localScale = new Vector3(1.35f, 1.05f, 1.55f);
        SetMaterial(headObject, mint);
        TrySetTag(headObject, "SnakeHead");
        body.head = headObject.transform;

        Rigidbody rigidbody = headObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        SnakeMovement movement = headObject.AddComponent<SnakeMovement>();
        movement.speed = GameSettings.SnakeSpeed;
        movement.rotationSpeed = GameSettings.SnakeRotationSpeed;
        movement.useMouseSteering = false;

        CreateEye(headObject.transform, new Vector3(-0.28f, 0.28f, 0.46f));
        CreateEye(headObject.transform, new Vector3(0.28f, 0.28f, 0.46f));
        return headObject.transform;
    }

    private void CreateEye(Transform parent, Vector3 localPosition)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "SnakeEye";
        eye.transform.SetParent(parent);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = Vector3.one * 0.2f;
        Collider collider = eye.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        SetMaterial(eye, new Color(0.01f, 0.02f, 0.04f));
    }

    private void CreateTraffic()
    {
        GameObject waypointObject = new GameObject("TrafficRoute");
        waypointObject.transform.SetParent(worldRoot);
        const int waypointCount = 32;
        float radiusX = arenaSize.x * 0.38f;
        float radiusZ = arenaSize.y * 0.38f;
        for (int i = 0; i < waypointCount; i++)
        {
            float angle = i * Mathf.PI * 2f / waypointCount;
            GameObject point = new GameObject("Waypoint_" + i.ToString("00"));
            point.transform.SetParent(waypointObject.transform);
            point.transform.position = new Vector3(Mathf.Cos(angle) * radiusX, 0.75f, Mathf.Sin(angle) * radiusZ);
            point.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
        }

        GameObject trafficObject = new GameObject("TrafficSpawner");
        trafficObject.transform.SetParent(worldRoot);
        CarSpawner spawner = trafficObject.AddComponent<CarSpawner>();
        spawner.waypointSystem = waypointObject.transform;
        spawner.maxCarsOnMap = GameSettings.MaxCarsOnMapValue;
        spawner.baseCarSpeed = 5.2f;
        spawner.speedRandomRange = 1.2f;
        spawner.minDistanceFromHead = 13f;
    }

    private void CreatePickups()
    {
        GameObject pickupObject = new GameObject("PickupSpawner");
        pickupObject.transform.SetParent(worldRoot);
        PickupSpawner spawner = pickupObject.AddComponent<PickupSpawner>();
        spawner.arenaSize = arenaSize - new Vector2(5f, 5f);
        spawner.maxPickups = 11;
    }

    private void CreateCamera(Transform head)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            TrySetTag(cameraObject, "MainCamera");
        }
        camera.fieldOfView = 58f;
        camera.backgroundColor = deepBlue;
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.target = head;
        follow.positionOffset = new Vector3(0f, 15f, -14f);
        follow.rotationOffset = new Vector3(42f, 0f, 0f);
        follow.followSmoothSpeed = 7f;
        follow.rotationSmoothSpeed = 8f;
        camera.transform.position = head.position + follow.positionOffset;
        camera.transform.rotation = Quaternion.Euler(follow.rotationOffset);
    }

    private void CreateHud()
    {
        GameObject canvasObject = new GameObject("RuntimeHUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        RuntimeHUD runtimeHud = canvasObject.AddComponent<RuntimeHUD>();

        GameObject topBar = CreatePanel("TopBar", canvasObject.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 108f), new Vector2(0f, -18f), panelBlue);
        Text score = CreateText("Score", topBar.transform, "ОЧКИ  0", 28, TextAnchor.MiddleLeft, cyan);
        SetAnchors(score.rectTransform, new Vector2(0.035f, 0f), new Vector2(0.23f, 1f), Vector2.zero, Vector2.zero);
        Text lives = CreateText("Lives", topBar.transform, "ЖИЗНИ  3", 28, TextAnchor.MiddleLeft, new Color(1f, 0.45f, 0.55f));
        SetAnchors(lives.rectTransform, new Vector2(0.25f, 0f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
        Text level = CreateText("Level", topBar.transform, "УР. 1", 28, TextAnchor.MiddleCenter, mint);
        SetAnchors(level.rectTransform, new Vector2(0.45f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero);
        Text combo = CreateText("Combo", topBar.transform, "КОМБО —", 25, TextAnchor.MiddleRight, new Color(1f, 0.78f, 0.28f));
        SetAnchors(combo.rectTransform, new Vector2(0.58f, 0f), new Vector2(0.87f, 1f), Vector2.zero, Vector2.zero);
        Text power = CreateText("Power", topBar.transform, string.Empty, 18, TextAnchor.MiddleRight, Color.white);
        SetAnchors(power.rectTransform, new Vector2(0.70f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

        Text title = CreateText("Title", canvasObject.transform, "NEON RUNNER", 24, TextAnchor.MiddleCenter, new Color(0.5f, 0.9f, 1f, 0.55f));
        SetAnchors(title.rectTransform, new Vector2(0.35f, 0.9f), new Vector2(0.65f, 0.97f), Vector2.zero, Vector2.zero);

        Text toast = CreateText("Toast", canvasObject.transform, string.Empty, 26, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(toast.rectTransform, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.19f), Vector2.zero, Vector2.zero);
        Text hint = CreateText("Hint", canvasObject.transform, "A/D или ←/→ — поворот    SPACE — ускорение    P — пауза", 18, TextAnchor.MiddleCenter, new Color(0.55f, 0.75f, 0.85f));
        SetAnchors(hint.rectTransform, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.08f), Vector2.zero, Vector2.zero);

        GameObject boostPanel = CreatePanel("BoostPanel", canvasObject.transform, new Vector2(0.02f, 0.1f), new Vector2(0.22f, 0.135f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.06f, 0.12f, 0.85f));
        GameObject fillObject = new GameObject("BoostFill");
        fillObject.transform.SetParent(boostPanel.transform, false);
        Image boostFill = fillObject.AddComponent<Image>();
        boostFill.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        boostFill.type = Image.Type.Filled;
        boostFill.fillMethod = Image.FillMethod.Horizontal;
        SetAnchors(boostFill.rectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
        Text boostLabel = CreateText("BoostLabel", boostPanel.transform, "BOOST", 14, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(boostLabel.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        GameObject gameOver = CreatePanel("GameOverPanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 330f), Vector2.zero, new Color(0.025f, 0.05f, 0.12f, 0.98f));
        Text overTitle = CreateText("GameOverTitle", gameOver.transform, "ЗАБЕГ ОКОНЧЕН", 38, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.55f));
        SetAnchors(overTitle.rectTransform, new Vector2(0f, 0.68f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
        Text overHint = CreateText("GameOverHint", gameOver.transform, "Город оказался быстрее. Попробуешь ещё раз?", 17, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(overHint.rectTransform, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
        CreateButton("RESTART", gameOver.transform, "ЗАНОВО", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.4f), gameManager.PlayAgain);
        CreateButton("MENU", gameOver.transform, "В МЕНЮ", new Vector2(0.12f, -0.03f), new Vector2(0.88f, 0.11f), gameManager.ExitToMenu);
        gameOver.SetActive(false);

        GameObject pause = CreatePanel("PausePanel", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 280f), Vector2.zero, new Color(0.025f, 0.05f, 0.12f, 0.97f));
        Text pauseTitle = CreateText("PauseTitle", pause.transform, "ПАУЗА", 38, TextAnchor.MiddleCenter, cyan);
        SetAnchors(pauseTitle.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
        CreateButton("Resume", pause.transform, "ПРОДОЛЖИТЬ", new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.48f), gameManager.ResumeGame);
        pause.SetActive(false);

        gameManager.legacyScoreText = score;
        gameManager.legacyLivesText = lives;
        gameManager.legacyLevelText = level;
        gameManager.legacyComboText = combo;
        gameManager.legacyPowerText = power;
        gameManager.gameOverPanel = gameOver;
        gameManager.pausePanel = pause;
        runtimeHud.toastText = toast;
        runtimeHud.hintText = hint;
        runtimeHud.boostFill = boostFill;
    }

    private GameObject CreateHazardWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = CreateCube(name, position, scale, new Color(0.08f, 0.22f, 0.3f), worldRoot, true);
        wall.AddComponent<ArenaHazard>();
        return wall;
    }

    private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Color color, Transform parent, bool collider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        SetMaterial(cube, color);
        if (!collider)
        {
            Collider box = cube.GetComponent<Collider>();
            if (box != null) Destroy(box);
        }
        return cube;
    }

    private void SetMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        SetAnchors(image.rectTransform, anchorMin, anchorMax, size, position);
        return panel;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.35f, 0.45f, 0.95f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.15f, 0.8f, 0.78f, 1f);
        colors.pressedColor = new Color(0.05f, 0.2f, 0.28f, 1f);
        button.colors = colors;
        button.onClick.AddListener(callback);
        SetAnchors(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
        Text text = CreateText("Label", buttonObject.transform, label, 22, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void TrySetTag(GameObject target, string tag)
    {
        try { target.tag = tag; } catch (UnityException) { }
    }
}
