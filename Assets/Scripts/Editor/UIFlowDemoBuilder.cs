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
    private const string SceneFolder = "Assets/Scenes";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/Jaeger.unity";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
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

        CreateMainMenuScene(
            mainMenuPanelPath,
            roomPanelPath
        );

        CreateGameplayScene(pausePanelPath);

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

    private static void CreateGameplayScene(string pausePanelPath)
    {
        Scene scene =
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single
            );

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

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
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
