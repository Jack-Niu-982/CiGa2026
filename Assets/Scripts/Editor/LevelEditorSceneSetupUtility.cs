using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelEditorSceneSetupUtility
{
    private const string MenuPath = "CiGa2026/Setup Level Editor Flow";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string LevelEditorPanelPath =
        "Assets/Prefabs/UI/LevelEditorPanel.prefab";
    private const string FallbackMainMenuScenePath =
        "Assets/Scenes/MainMenu Jaeger.unity";
    private const string GameplayScenePath =
        "Assets/Scenes/Jaeger.unity";
    private const string DefaultLevelPath =
        "Assets/StreamingAssets/Levels/default_level.json";

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/StreamingAssets");
        EnsureFolder("Assets/StreamingAssets/Levels");

        CreateDefaultLevelAsset();
        CreateLevelEditorPanelPrefab();
        SetupMainMenuScene();
        SetupGameplayScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[LevelEditorSceneSetupUtility] Level editor flow setup complete.");
    }

    [MenuItem("CiGa2026/Validate Generated Level Terrain")]
    public static void ValidateGeneratedTerrain()
    {
        LevelTerrainData data =
            LevelFileService.LoadOrCreateDefault();

        LevelTerrainMeshData meshData =
            LevelTerrainMesher.Build(data);

        LevelValidationResult validation =
            LevelValidationService.Validate(data);

        if (!validation.IsValid)
        {
            Debug.LogError(
                "[LevelEditorSceneSetupUtility] Default level validation failed: " +
                validation.Errors[0]
            );

            return;
        }

        Debug.Log(
            $"[LevelEditorSceneSetupUtility] Generated terrain validated. " +
            $"Vertices={meshData.Mesh.vertexCount}, ColliderPaths={meshData.ColliderPaths.Length}"
        );
    }

    private static void CreateDefaultLevelAsset()
    {
        LevelTerrainData data =
            LevelFileService.CreateDefaultLevel();

        LevelFileService.SaveLevel(data, DefaultLevelPath);
        AssetDatabase.ImportAsset(DefaultLevelPath);
    }

    private static void CreateLevelEditorPanelPrefab()
    {
        GameObject panel = CreatePanelRoot("LevelEditorPanel");

        HorizontalLayoutGroup rootLayout =
            panel.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(28, 28, 28, 28);
        rootLayout.spacing = 18f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;

        GameObject sideBar =
            CreateUIObject("Toolbar", panel.transform);

        Image sideBarBackground =
            sideBar.AddComponent<Image>();
        sideBarBackground.color = new Color(0.07f, 0.08f, 0.10f, 0.96f);

        VerticalLayoutGroup sideLayout =
            sideBar.AddComponent<VerticalLayoutGroup>();
        sideLayout.padding = new RectOffset(18, 18, 18, 18);
        sideLayout.spacing = 10f;
        sideLayout.childControlWidth = true;
        sideLayout.childControlHeight = true;
        sideLayout.childForceExpandWidth = true;
        sideLayout.childForceExpandHeight = false;

        LayoutElement sideLayoutElement =
            sideBar.AddComponent<LayoutElement>();
        sideLayoutElement.preferredWidth = 320f;
        sideLayoutElement.flexibleWidth = 0f;

        CreateText(sideBar.transform, "Title", "LEVEL EDITOR", 30);

        Button digButton =
            CreateButton(sideBar.transform, "DigButton", "Dig");
        Button fillButton =
            CreateButton(sideBar.transform, "FillButton", "Fill");
        Button startButton =
            CreateButton(sideBar.transform, "StartButton", "Start Point");
        Button finishButton =
            CreateButton(sideBar.transform, "FinishButton", "Finish Point");

        TMP_Text radiusLabel =
            CreateText(sideBar.transform, "RadiusLabel", "Radius", 18);
        Slider radiusSlider =
            CreateSlider(sideBar.transform, "RadiusSlider", 1f, 12f, 4f);

        TMP_Text hardnessLabel =
            CreateText(sideBar.transform, "HardnessLabel", "Hardness", 18);
        Slider hardnessSlider =
            CreateSlider(sideBar.transform, "HardnessSlider", 0.05f, 1f, 0.75f);

        Button newButton =
            CreateButton(sideBar.transform, "NewButton", "New");
        Button saveButton =
            CreateButton(sideBar.transform, "SaveButton", "Save");
        Button loadButton =
            CreateButton(sideBar.transform, "LoadButton", "Load Latest");
        Button playtestButton =
            CreateButton(sideBar.transform, "PlaytestButton", "Playtest");
        Button backButton =
            CreateButton(sideBar.transform, "BackButton", "Back");

        TMP_Text toolLabel =
            CreateText(sideBar.transform, "ToolLabel", "Tool: Dig", 18);
        TMP_Text statusLabel =
            CreateText(sideBar.transform, "StatusLabel", "Ready", 16);

        GameObject viewport =
            CreateUIObject("ViewportHint", panel.transform);

        Image viewportBackground =
            viewport.AddComponent<Image>();
        viewportBackground.color = new Color(0.02f, 0.025f, 0.03f, 0.55f);

        viewport.AddComponent<RectMask2D>();

        LayoutElement viewportLayout =
            viewport.AddComponent<LayoutElement>();
        viewportLayout.flexibleWidth = 1f;
        viewportLayout.flexibleHeight = 1f;

        GameObject canvas =
            CreateUIObject("MapCanvas", viewport.transform);

        RawImage mapCanvas =
            canvas.AddComponent<RawImage>();
        mapCanvas.color = Color.white;

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.sizeDelta = new Vector2(1536f, 768f);

        LevelEditorPanelView view =
            panel.AddComponent<LevelEditorPanelView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        serializedView.FindProperty("digButton").objectReferenceValue = digButton;
        serializedView.FindProperty("fillButton").objectReferenceValue = fillButton;
        serializedView.FindProperty("startButton").objectReferenceValue = startButton;
        serializedView.FindProperty("finishButton").objectReferenceValue = finishButton;
        serializedView.FindProperty("newButton").objectReferenceValue = newButton;
        serializedView.FindProperty("saveButton").objectReferenceValue = saveButton;
        serializedView.FindProperty("loadButton").objectReferenceValue = loadButton;
        serializedView.FindProperty("playtestButton").objectReferenceValue = playtestButton;
        serializedView.FindProperty("backButton").objectReferenceValue = backButton;
        serializedView.FindProperty("radiusSlider").objectReferenceValue = radiusSlider;
        serializedView.FindProperty("hardnessSlider").objectReferenceValue = hardnessSlider;
        serializedView.FindProperty("radiusLabel").objectReferenceValue = radiusLabel;
        serializedView.FindProperty("hardnessLabel").objectReferenceValue = hardnessLabel;
        serializedView.FindProperty("toolLabel").objectReferenceValue = toolLabel;
        serializedView.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serializedView.FindProperty("mapCanvas").objectReferenceValue = mapCanvas;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(panel, LevelEditorPanelPath);
        Object.DestroyImmediate(panel);
    }

    private static void SetupMainMenuScene()
    {
        Scene scene =
            EditorSceneManager.OpenScene(GetMainMenuScenePath(), OpenSceneMode.Single);

        GameObject uiFlowRoot =
            GameObject.Find("UIFlowRoot");

        GameObject canvas =
            GameObject.Find("UIFlowRoot/UIFlowCanvas") ??
            GameObject.Find("UIFlowCanvas");

        if (uiFlowRoot == null || canvas == null)
        {
            Debug.LogError("[LevelEditorSceneSetupUtility] MainMenu is missing UIFlowRoot or UIFlowCanvas.");
            return;
        }

        GameFlowController flow =
            uiFlowRoot.GetComponent<GameFlowController>();

        MainMenuPanelView mainMenuPanel =
            uiFlowRoot.GetComponentInChildren<MainMenuPanelView>(true);

        if (mainMenuPanel == null)
        {
            GameObject mainMenuPanelObject =
                GameObject.Find("MainMenuPanel");

            if (mainMenuPanelObject != null)
            {
                mainMenuPanel =
                    mainMenuPanelObject.GetComponent<MainMenuPanelView>();

                if (mainMenuPanel == null)
                {
                    mainMenuPanel =
                        mainMenuPanelObject.AddComponent<MainMenuPanelView>();
                }
            }
        }

        LevelEditorController editor =
            uiFlowRoot.GetComponentInChildren<LevelEditorController>(true);

        if (editor == null)
        {
            GameObject existingEditorRoot =
                GameObject.Find("LevelEditorRoot");

            if (existingEditorRoot != null)
            {
                editor =
                    existingEditorRoot.GetComponent<LevelEditorController>();
            }
        }

        if (editor == null)
        {
            GameObject editorRoot =
                new GameObject("LevelEditorRoot");
            editorRoot.transform.SetParent(uiFlowRoot.transform, false);
            editor = editorRoot.AddComponent<LevelEditorController>();

            GameObject terrainRoot =
                new GameObject("LevelEditorTerrainPreview");
            terrainRoot.transform.SetParent(editorRoot.transform, false);

            GameObject visual =
                new GameObject("TerrainVisual");
            visual.transform.SetParent(terrainRoot.transform, false);
            LevelTerrainRenderer2D renderer =
                visual.AddComponent<LevelTerrainRenderer2D>();

            GameObject colliders =
                new GameObject("TerrainColliders");
            colliders.transform.SetParent(terrainRoot.transform, false);
            LevelTerrainColliderBuilder colliderBuilder =
                colliders.AddComponent<LevelTerrainColliderBuilder>();

            GameObject panelPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LevelEditorPanelPath);

            GameObject panelInstance =
                PrefabUtility.InstantiatePrefab(panelPrefab) as GameObject;
            panelInstance.transform.SetParent(canvas.transform, false);

            RectTransform rect =
                panelInstance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            LevelEditorPanelView panelView =
                panelInstance.GetComponent<LevelEditorPanelView>();

            SerializedObject serializedEditor =
                new SerializedObject(editor);

            serializedEditor.FindProperty("panelView")
                .objectReferenceValue = panelView;
            serializedEditor.FindProperty("editorCamera")
                .objectReferenceValue = Camera.main;
            serializedEditor.FindProperty("terrainPreviewRoot")
                .objectReferenceValue = terrainRoot.transform;
            serializedEditor.FindProperty("terrainRenderer")
                .objectReferenceValue = renderer;
            serializedEditor.FindProperty("colliderBuilder")
                .objectReferenceValue = colliderBuilder;
            serializedEditor.ApplyModifiedPropertiesWithoutUndo();

            editor.Show(false);
        }
        else
        {
            editor.transform.SetParent(uiFlowRoot.transform, false);
            editor.Show(false);
        }

        AddLevelEditorButton(mainMenuPanel);

        SerializedObject serializedFlow =
            new SerializedObject(flow);

        serializedFlow.FindProperty("levelEditor")
            .objectReferenceValue = editor;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupGameplayScene()
    {
        Scene scene =
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

        GameObject bootstrapRoot =
            GameObject.Find("GeneratedLevelRoot");

        if (bootstrapRoot == null)
        {
            bootstrapRoot = new GameObject("GeneratedLevelRoot");
        }

        LevelRuntimeBootstrap bootstrap =
            bootstrapRoot.GetComponent<LevelRuntimeBootstrap>();

        if (bootstrap == null)
        {
            bootstrap = bootstrapRoot.AddComponent<LevelRuntimeBootstrap>();
        }

        LevelTerrainRenderer2D renderer =
            bootstrapRoot.GetComponentInChildren<LevelTerrainRenderer2D>(true);

        if (renderer == null)
        {
            GameObject visual = new GameObject("TerrainVisual");
            visual.transform.SetParent(bootstrapRoot.transform, false);
            renderer = visual.AddComponent<LevelTerrainRenderer2D>();
        }

        LevelTerrainColliderBuilder colliderBuilder =
            bootstrapRoot.GetComponentInChildren<LevelTerrainColliderBuilder>(true);

        if (colliderBuilder == null)
        {
            GameObject colliders = new GameObject("TerrainColliders");
            colliders.transform.SetParent(bootstrapRoot.transform, false);
            colliderBuilder = colliders.AddComponent<LevelTerrainColliderBuilder>();
        }

        Transform submarine =
            FindTransform("Submarine");

        Transform spawnRoot =
            FindTransform("GameplayPlayerSpawnRoot");

        FinishZone2D finishZone =
            Object.FindObjectOfType<FinishZone2D>(true);

        SerializedObject serializedBootstrap =
            new SerializedObject(bootstrap);

        serializedBootstrap.FindProperty("generatedLevelRoot")
            .objectReferenceValue = bootstrapRoot.transform;
        serializedBootstrap.FindProperty("terrainRenderer")
            .objectReferenceValue = renderer;
        serializedBootstrap.FindProperty("colliderBuilder")
            .objectReferenceValue = colliderBuilder;
        serializedBootstrap.FindProperty("submarineRoot")
            .objectReferenceValue = submarine;
        serializedBootstrap.FindProperty("gameplayPlayerSpawnRoot")
            .objectReferenceValue = spawnRoot;
        serializedBootstrap.FindProperty("finishZone")
            .objectReferenceValue = finishZone;
        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

        GameplayPlayerSpawner spawner =
            Object.FindObjectOfType<GameplayPlayerSpawner>(true);

        if (spawner != null)
        {
            SerializedObject serializedSpawner =
                new SerializedObject(spawner);

            serializedSpawner.FindProperty("waitForLevelReady")
                .boolValue = true;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(GetMainMenuScenePath(), OpenSceneMode.Single);
    }

    private static void AddLevelEditorButton(MainMenuPanelView mainMenuPanel)
    {
        if (mainMenuPanel == null)
        {
            return;
        }

        Transform existing =
            mainMenuPanel.transform.Find("LevelEditorButton");

        Button levelEditorButton =
            existing != null
                ? existing.GetComponent<Button>()
                : null;

        if (levelEditorButton == null)
        {
            Button startButton =
                mainMenuPanel.transform.Find("StartButton")?.GetComponent<Button>();

            levelEditorButton =
                CreateButton(
                    mainMenuPanel.transform,
                    "LevelEditorButton",
                    "Level Editor"
                );

            if (startButton != null)
            {
                levelEditorButton.transform.SetSiblingIndex(
                    startButton.transform.GetSiblingIndex() + 1
                );
            }
        }

        LayoutElement levelEditorLayout =
            levelEditorButton.GetComponent<LayoutElement>();

        if (levelEditorLayout == null)
        {
            levelEditorLayout =
                levelEditorButton.gameObject.AddComponent<LayoutElement>();
        }

        levelEditorLayout.preferredWidth = 280f;
        levelEditorLayout.preferredHeight = 72f;

        SerializedObject serializedMenu =
            new SerializedObject(mainMenuPanel);

        Button startButtonForView =
            mainMenuPanel.transform.Find("StartButton")?.GetComponent<Button>();

        Button quitButtonForView =
            mainMenuPanel.transform.Find("QuitButton")?.GetComponent<Button>();

        serializedMenu.FindProperty("startButton")
            .objectReferenceValue = startButtonForView;
        serializedMenu.FindProperty("levelEditorButton")
            .objectReferenceValue = levelEditorButton;
        serializedMenu.FindProperty("quitButton")
            .objectReferenceValue = quitButtonForView;
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindTransform(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
    }

    private static string GetMainMenuScenePath()
    {
        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes;

        if (scenes != null &&
            scenes.Length > 0 &&
            !string.IsNullOrWhiteSpace(scenes[0].path))
        {
            return scenes[0].path;
        }

        return FallbackMainMenuScenePath;
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
        background.color = new Color(0.04f, 0.05f, 0.06f, 0.94f);

        return panel;
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
        layout.preferredHeight = Mathf.Max(32f, fontSize * 1.35f);

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
        image.color = new Color(0.15f, 0.18f, 0.22f, 1f);

        Button button =
            buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout =
            buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 280f;
        layout.preferredHeight = 48f;

        TMP_Text label =
            CreateText(buttonObject.transform, "Label", labelText, 20);

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

    private static Slider CreateSlider(
        Transform parent,
        string name,
        float min,
        float max,
        float value)
    {
        GameObject sliderObject =
            CreateUIObject(name, parent);

        Slider slider =
            sliderObject.AddComponent<Slider>();

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        Image background =
            sliderObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        RectTransform rect =
            sliderObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 24f);

        LayoutElement layout =
            sliderObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 28f;

        CreateSliderVisuals(
            slider,
            name.Contains("Hardness")
                ? new Color(0.96f, 0.76f, 0.28f, 1f)
                : new Color(0.30f, 0.64f, 1f, 1f)
        );

        return slider;
    }

    private static void CreateSliderVisuals(
        Slider slider,
        Color fillColor)
    {
        RectTransform sliderRect =
            slider.GetComponent<RectTransform>();

        Image background =
            CreateSliderImage(sliderRect, "Background");

        background.color = new Color(0.13f, 0.15f, 0.18f, 1f);
        background.raycastTarget = true;
        SetStretchRect(
            background.rectTransform,
            new Vector2(0f, 0.36f),
            new Vector2(1f, 0.64f),
            Vector2.zero,
            Vector2.zero
        );

        GameObject fillAreaObject =
            CreateUIObject("Fill Area", sliderRect);
        RectTransform fillArea =
            fillAreaObject.GetComponent<RectTransform>();

        SetStretchRect(
            fillArea,
            new Vector2(0f, 0.36f),
            new Vector2(1f, 0.64f),
            new Vector2(9f, 0f),
            new Vector2(-9f, 0f)
        );

        Image fill =
            CreateSliderImage(fillArea, "Fill");

        fill.color = fillColor;
        fill.raycastTarget = false;
        SetStretchRect(
            fill.rectTransform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        GameObject handleAreaObject =
            CreateUIObject("Handle Slide Area", sliderRect);
        RectTransform handleArea =
            handleAreaObject.GetComponent<RectTransform>();

        SetStretchRect(
            handleArea,
            Vector2.zero,
            Vector2.one,
            new Vector2(9f, 0f),
            new Vector2(-9f, 0f)
        );

        Image handle =
            CreateSliderImage(handleArea, "Handle");

        handle.color = new Color(0.94f, 0.96f, 1f, 1f);
        handle.raycastTarget = true;

        RectTransform handleRect =
            handle.rectTransform;

        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(18f, 28f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;
    }

    private static Image CreateSliderImage(
        Transform parent,
        string name)
    {
        GameObject imageObject =
            CreateUIObject(name, parent);

        imageObject.AddComponent<CanvasRenderer>();

        return imageObject.AddComponent<Image>();
    }

    private static void SetStretchRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
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
