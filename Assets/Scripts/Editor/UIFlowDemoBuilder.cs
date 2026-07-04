using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIFlowDemoBuilder
{
    private const string MenuPath = "CiGa2026/Build UI Flow Demo";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string GameplayPrefabFolder = "Assets/Prefabs/Gameplay";
    private const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Player.prefab";
    private const string SceneFolder = "Assets/Scenes";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/Jaeger.unity";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder(GameplayPrefabFolder);
        EnsureFolder(SceneFolder);

        GameObject mainMenuPanel = CreateMainMenuPanel();
        GameObject roomPanel = CreateRoomPanel();
        GameObject pausePanel = CreatePausePanel();

        string mainMenuPanelPath =
            $"{PrefabFolder}/MainMenuPanel.prefab";
        string roomPanelPath =
            $"{PrefabFolder}/RoomPanel.prefab";
        string pausePanelPath =
            $"{PrefabFolder}/PausePanel.prefab";

        PrefabUtility.SaveAsPrefabAsset(mainMenuPanel, mainMenuPanelPath);
        PrefabUtility.SaveAsPrefabAsset(roomPanel, roomPanelPath);
        PrefabUtility.SaveAsPrefabAsset(pausePanel, pausePanelPath);

        Object.DestroyImmediate(mainMenuPanel);
        Object.DestroyImmediate(roomPanel);
        Object.DestroyImmediate(pausePanel);

        CreateGameplayPlayerPrefab();

        CreateMainMenuScene(
            mainMenuPanelPath,
            roomPanelPath
        );

        CreateGameplayScene(pausePanelPath, PlayerPrefabPath);

        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UIFlowDemoBuilder] UI Flow demo assets generated.");
    }

    private static GameObject CreateMainMenuPanel()
    {
        GameObject panel = CreatePanelRoot("MainMenuPanel");
        VerticalLayoutGroup layout = AddVerticalLayout(panel, 36f);
        layout.childAlignment = TextAnchor.MiddleCenter;

        CreateText(panel.transform, "Title", "ANCHOR SHIP", 64);
        CreateText(panel.transform, "Subtitle", "Local Co-op Party Demo", 28);

        Button startButton =
            CreateButton(panel.transform, "StartButton", "Start");

        Button quitButton =
            CreateButton(panel.transform, "QuitButton", "Quit");

        MainMenuPanelView view =
            panel.AddComponent<MainMenuPanelView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        serializedView.FindProperty("startButton")
            .objectReferenceValue = startButton;
        serializedView.FindProperty("quitButton")
            .objectReferenceValue = quitButton;

        serializedView.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static GameObject CreateRoomPanel()
    {
        GameObject panel = CreatePanelRoot("RoomPanel");
        VerticalLayoutGroup layout = AddVerticalLayout(panel, 22f);
        layout.childAlignment = TextAnchor.MiddleCenter;

        TMP_Text title =
            CreateText(panel.transform, "Title", "ROOM", 54);

        TMP_Text hint =
            CreateText(
                panel.transform,
                "Hint",
                "Press South to join. South again to ready. East to leave.",
                24
            );

        GameObject slotRow =
            CreateUIObject("SlotRow", panel.transform);
        HorizontalLayoutGroup slotLayout =
            slotRow.AddComponent<HorizontalLayoutGroup>();
        slotLayout.spacing = 20f;
        slotLayout.childAlignment = TextAnchor.MiddleCenter;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandWidth = true;
        slotLayout.childForceExpandHeight = false;

        RectTransform slotRowRect =
            slotRow.GetComponent<RectTransform>();
        slotRowRect.sizeDelta = new Vector2(1120f, 210f);

        RoomPlayerSlotView[] slotViews =
            new RoomPlayerSlotView[RoomInputManager.MaxPlayers];

        for (int i = 0; i < slotViews.Length; i++)
        {
            GameObject slot =
                CreateSlotView(slotRow.transform, i);

            slotViews[i] =
                slot.GetComponent<RoomPlayerSlotView>();
        }

        TMP_Text summary =
            CreateText(panel.transform, "Summary", "0/4 joined", 24);

        GameObject buttonRow =
            CreateUIObject("ButtonRow", panel.transform);
        HorizontalLayoutGroup buttonLayout =
            buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 24f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        Button backButton =
            CreateButton(buttonRow.transform, "BackButton", "Back");

        Button startButton =
            CreateButton(buttonRow.transform, "StartButton", "Start");

        RoomPanelView view =
            panel.AddComponent<RoomPanelView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        SerializedProperty slotsProperty =
            serializedView.FindProperty("slotViews");
        slotsProperty.arraySize = slotViews.Length;

        for (int i = 0; i < slotViews.Length; i++)
        {
            slotsProperty.GetArrayElementAtIndex(i)
                .objectReferenceValue = slotViews[i];
        }

        serializedView.FindProperty("startButton")
            .objectReferenceValue = startButton;
        serializedView.FindProperty("backButton")
            .objectReferenceValue = backButton;
        serializedView.FindProperty("titleLabel")
            .objectReferenceValue = title;
        serializedView.FindProperty("hintLabel")
            .objectReferenceValue = hint;
        serializedView.FindProperty("summaryLabel")
            .objectReferenceValue = summary;

        serializedView.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static GameObject CreatePausePanel()
    {
        GameObject panel = CreatePanelRoot("PausePanel");
        VerticalLayoutGroup layout = AddVerticalLayout(panel, 28f);
        layout.childAlignment = TextAnchor.MiddleCenter;

        CreateText(panel.transform, "Title", "PAUSED", 56);

        Button resumeButton =
            CreateButton(panel.transform, "ResumeButton", "Resume");

        Button restartButton =
            CreateButton(panel.transform, "RestartButton", "Restart");

        Button backButton =
            CreateButton(panel.transform, "BackToMainMenuButton", "Main Menu");

        PausePanelView view =
            panel.AddComponent<PausePanelView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        serializedView.FindProperty("resumeButton")
            .objectReferenceValue = resumeButton;
        serializedView.FindProperty("restartButton")
            .objectReferenceValue = restartButton;
        serializedView.FindProperty("backToMainMenuButton")
            .objectReferenceValue = backButton;

        serializedView.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static GameObject CreateSlotView(
        Transform parent,
        int slotIndex)
    {
        GameObject slot =
            CreateUIObject($"P{slotIndex + 1}Slot", parent);

        Image background =
            slot.AddComponent<Image>();
        background.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);

        VerticalLayoutGroup layout =
            slot.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement =
            slot.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 240f;
        layoutElement.preferredHeight = 190f;

        TMP_Text playerLabel =
            CreateText(slot.transform, "PlayerLabel", $"P{slotIndex + 1}", 36);

        TMP_Text deviceLabel =
            CreateText(slot.transform, "DeviceLabel", "No Gamepad", 20);

        TMP_Text stateLabel =
            CreateText(slot.transform, "StateLabel", "Press South", 22);

        GameObject stateSwatch =
            CreateUIObject("StateSwatch", slot.transform);
        Image stateImage =
            stateSwatch.AddComponent<Image>();
        stateImage.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        RectTransform swatchRect =
            stateSwatch.GetComponent<RectTransform>();
        swatchRect.sizeDelta = new Vector2(64f, 10f);

        RoomPlayerSlotView view =
            slot.AddComponent<RoomPlayerSlotView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        serializedView.FindProperty("playerLabel")
            .objectReferenceValue = playerLabel;
        serializedView.FindProperty("deviceLabel")
            .objectReferenceValue = deviceLabel;
        serializedView.FindProperty("stateLabel")
            .objectReferenceValue = stateLabel;
        serializedView.FindProperty("stateImage")
            .objectReferenceValue = stateImage;

        serializedView.ApplyModifiedPropertiesWithoutUndo();

        return slot;
    }

    private static void CreateMainMenuScene(
        string mainMenuPanelPath,
        string roomPanelPath)
    {
        Scene scene =
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

        GameObject root =
            new GameObject("UIFlowRoot");

        CreateMainCamera();

        RoomInputManager roomInput =
            root.AddComponent<RoomInputManager>();
        LocalPlayerSession session =
            root.AddComponent<LocalPlayerSession>();
        GameFlowController flow =
            root.AddComponent<GameFlowController>();

        GameObject canvasObject =
            new GameObject(
                "UIFlowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        MainMenuPanelView mainMenuPanel =
            InstantiatePanel<MainMenuPanelView>(
                mainMenuPanelPath,
                canvasObject.transform
            );

        RoomPanelView roomPanel =
            InstantiatePanel<RoomPanelView>(
                roomPanelPath,
                canvasObject.transform
            );

        roomPanel.gameObject.SetActive(false);

        GameObject eventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        SerializedObject serializedFlow =
            new SerializedObject(flow);

        serializedFlow.FindProperty("roomInputManager")
            .objectReferenceValue = roomInput;
        serializedFlow.FindProperty("localPlayerSession")
            .objectReferenceValue = session;
        serializedFlow.FindProperty("mainMenuPanel")
            .objectReferenceValue = mainMenuPanel;
        serializedFlow.FindProperty("roomPanel")
            .objectReferenceValue = roomPanel;

        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    }

    private static void CreateGameplayPlayerPrefab()
    {
        Scene previousScene =
            EditorSceneManager.GetActiveScene();

        bool restorePreviousScene =
            previousScene.IsValid() &&
            !string.IsNullOrEmpty(previousScene.path) &&
            previousScene.path != GameplayScenePath;

        Scene gameplayScene =
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single
            );

        GameObject source =
            GameObject.Find("Players/Player1") ??
            GameObject.Find("Player1");

        if (source == null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null)
        {
            EnsurePlayerPrefabComponents(PlayerPrefabPath);

            if (restorePreviousScene)
            {
                EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
            }

            return;
        }

        GameObject prefabSource = source;
        bool destroyPrefabSource = false;

        if (prefabSource == null)
        {
            prefabSource = CreateFallbackPlayerPrefabSource();
            destroyPrefabSource = true;
        }

        bool wasActive =
            prefabSource.activeSelf;

        prefabSource.SetActive(true);

        PrefabUtility.SaveAsPrefabAssetAndConnect(
            prefabSource,
            PlayerPrefabPath,
            InteractionMode.AutomatedAction
        );

        prefabSource.SetActive(wasActive);

        if (destroyPrefabSource)
        {
            Object.DestroyImmediate(prefabSource);
        }

        EnsurePlayerPrefabComponents(PlayerPrefabPath);

        EditorSceneManager.SaveScene(gameplayScene);

        if (restorePreviousScene)
        {
            EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
        }
    }

    private static GameObject CreateFallbackPlayerPrefabSource()
    {
        GameObject player =
            new GameObject("PlayerPrefabSource");

        player.transform.position = Vector3.zero;
        player.AddComponent<SpriteRenderer>();
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>();
        player.AddComponent<KeyboardPlayerInput>();
        player.AddComponent<GamepadPlayerInput>();
        player.AddComponent<PlayerOperateInteractor2D>();
        player.AddComponent<PlayerCarryInteractor2D>();
        player.AddComponent<PlayerController>();

        return player;
    }

    private static void EnsurePlayerPrefabComponents(string prefabPath)
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(prefabPath);

        EnsureComponent<SpriteRenderer>(prefabRoot);
        EnsureComponent<Rigidbody2D>(prefabRoot);

        if (prefabRoot.GetComponent<Collider2D>() == null)
        {
            prefabRoot.AddComponent<CircleCollider2D>();
        }

        EnsureComponent<KeyboardPlayerInput>(prefabRoot);
        EnsureComponent<GamepadPlayerInput>(prefabRoot);
        EnsureComponent<PlayerOperateInteractor2D>(prefabRoot);
        EnsureComponent<PlayerCarryInteractor2D>(prefabRoot);
        EnsureComponent<PlayerController>(prefabRoot);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void EnsureComponent<T>(GameObject target)
        where T : Component
    {
        if (target.GetComponent<T>() == null)
        {
            target.AddComponent<T>();
        }
    }

    private static void CreateGameplayScene(
        string pausePanelPath,
        string playerPrefabPath)
    {
        Scene scene =
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single
            );

        RemovePreplacedPlayers();

        GameObject existingRoot =
            GameObject.Find("GameplayUIFlowRoot");

        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        GameObject root =
            new GameObject("GameplayUIFlowRoot");

        GameplaySceneFlowController flow =
            root.AddComponent<GameplaySceneFlowController>();

        GameplayPlayerSpawner spawner =
            root.AddComponent<GameplayPlayerSpawner>();

        GameObject spawnRoot =
            ResetGameObject("GameplayPlayerSpawnRoot");

        GameObject spawnedPlayersRoot =
            ResetGameObject("SpawnedPlayers");

        Transform[] spawnPoints =
            CreateGameplaySpawnPoints(spawnRoot.transform);

        GameObject canvasObject =
            new GameObject(
                "GameplayUIFlowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        PausePanelView pausePanel =
            InstantiatePanel<PausePanelView>(
                pausePanelPath,
                canvasObject.transform
            );

        pausePanel.gameObject.SetActive(false);

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );
        }

        SerializedObject serializedFlow =
            new SerializedObject(flow);

        serializedFlow.FindProperty("pausePanel")
            .objectReferenceValue = pausePanel;

        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedSpawner =
            new SerializedObject(spawner);

        serializedSpawner.FindProperty("playerPrefab")
            .objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);

        serializedSpawner.FindProperty("spawnedPlayersRoot")
            .objectReferenceValue = spawnedPlayersRoot.transform;

        SerializedProperty spawnPointsProperty =
            serializedSpawner.FindProperty("spawnPoints");

        spawnPointsProperty.arraySize = spawnPoints.Length;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPointsProperty.GetArrayElementAtIndex(i)
                .objectReferenceValue = spawnPoints[i];
        }

        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
    }

    private static void RemovePreplacedPlayers()
    {
        GameObject playersRoot =
            GameObject.Find("Players");

        if (playersRoot == null)
        {
            return;
        }

        for (int i = playersRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child =
                playersRoot.transform.GetChild(i);

            if (child.name.StartsWith("Player"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static GameObject ResetGameObject(string name)
    {
        GameObject existing =
            GameObject.Find(name);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        return new GameObject(name);
    }

    private static Transform[] CreateGameplaySpawnPoints(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-1.35f, 0.8f, 0f),
            new Vector3(1.35f, 0.8f, 0f),
            new Vector3(-1.35f, -0.8f, 0f),
            new Vector3(1.35f, -0.8f, 0f)
        };

        Transform[] spawnPoints =
            new Transform[RoomInputManager.MaxPlayers];

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject spawnPoint =
                new GameObject($"P{i + 1}SpawnPoint");

            spawnPoint.transform.SetParent(parent, false);
            spawnPoint.transform.localPosition = positions[i];
            spawnPoint.transform.localRotation = Quaternion.identity;
            spawnPoint.transform.localScale = Vector3.one;

            spawnPoints[i] = spawnPoint.transform;
        }

        return spawnPoints;
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObject =
            new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener)
            );

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position =
            new Vector3(0f, 0f, -10f);

        Camera camera =
            cameraObject.GetComponent<Camera>();

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.04f, 0.055f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    private static T InstantiatePanel<T>(
        string prefabPath,
        Transform parent)
        where T : Component
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        instance.transform.SetParent(parent, false);

        RectTransform rect =
            instance.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return instance.GetComponent<T>();
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes =
            new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
    }

    private static GameObject CreatePanelRoot(string name)
    {
        GameObject panel =
            CreateUIObject(name, null);

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background =
            panel.AddComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

        return panel;
    }

    private static VerticalLayoutGroup AddVerticalLayout(
        GameObject target,
        float spacing)
    {
        VerticalLayoutGroup layout =
            target.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(80, 80, 80, 80);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return layout;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize)
    {
        GameObject textObject =
            CreateUIObject(name, parent);

        TextMeshProUGUI label =
            textObject.AddComponent<TextMeshProUGUI>();

        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableWordWrapping = true;

        LayoutElement layout =
            textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = Mathf.Max(44f, fontSize * 1.4f);

        return label;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string labelText)
    {
        GameObject buttonObject =
            CreateUIObject(name, parent);

        Image image =
            buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.2f, 0.25f, 1f);

        Button button =
            buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout =
            buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 280f;
        layout.preferredHeight = 72f;

        TMP_Text label =
            CreateText(buttonObject.transform, "Label", labelText, 28);

        RectTransform labelRect =
            label.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        LayoutElement labelLayout =
            label.GetComponent<LayoutElement>();

        if (labelLayout != null)
        {
            Object.DestroyImmediate(labelLayout);
        }

        return button;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject gameObject =
            new GameObject(name, typeof(RectTransform));

        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent =
            System.IO.Path.GetDirectoryName(path)
                ?.Replace("\\", "/");

        string folderName =
            System.IO.Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
