using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelEditorController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private LevelEditorPanelView panelView;
    [SerializeField] private Camera editorCamera;
    [SerializeField] private Transform terrainPreviewRoot;
    [SerializeField] private LevelTerrainRenderer2D terrainRenderer;
    [SerializeField] private LevelTerrainColliderBuilder colliderBuilder;

    [Header("Map")]
    [SerializeField] private int defaultWidth = 256;
    [SerializeField] private int defaultHeight = 128;
    [SerializeField] private int defaultSamplesPerUnit = 4;
    [SerializeField] private byte solidThreshold =
        LevelTerrainData.DefaultSolidThreshold;

    [Header("Camera")]
    [SerializeField] private float cameraPanSpeed = 16f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 12f;
    [SerializeField] private float maxZoom = 80f;

    [Header("Map View")]
    [SerializeField] private float mapDisplayPixelsPerSample = 6f;
    [SerializeField] private float minMapDisplayPixelsPerSample = 2f;
    [SerializeField] private float maxMapDisplayPixelsPerSample = 16f;
    [SerializeField] private float mapPanSpeed = 760f;
    [SerializeField] private float mapZoomStep = 0.01f;

    private readonly LevelBrushTool brush = new LevelBrushTool();
    private LevelTerrainData currentLevel;
    private bool isPainting;
    private string lastSavedPath;
    private Texture2D mapTexture;
    private Vector2 mapPanOffset;
    private readonly List<string> levelPaths = new List<string>();
    private int selectedLevelIndex = -1;

    public event Action BackRequested;

    private void Awake()
    {
        EnsureReferences();
        CreateNewLevel();
    }

    private void OnEnable()
    {
        SubscribeView();

        if (panelView != null && currentLevel != null)
        {
            panelView.SetLevelName(currentLevel.displayName);
            RefreshLevelList(lastSavedPath);
        }
    }

    private void OnDisable()
    {
        UnsubscribeView();
    }

    private void Update()
    {
        HandleCameraInput();
        HandlePaintInput();
    }

    public void Show(bool visible)
    {
        if (panelView != null)
        {
            panelView.Show(visible);
        }

        gameObject.SetActive(visible);

        if (visible && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void CreateNewLevel()
    {
        currentLevel =
            LevelTerrainData.CreateSolidRectangle(
                "runtime_editor_level",
                "Runtime Editor Level",
                defaultWidth,
                defaultHeight,
                defaultSamplesPerUnit
            );

        brush.Mode = LevelBrushMode.Dig;
        lastSavedPath = string.Empty;
        mapPanOffset = Vector2.zero;
        panelView?.SetLevelName(currentLevel.displayName);
        RefreshLevelList();
        RefreshPreview();
        RefreshView("New solid map created. Dig a route, then place start and finish.");
    }

    public void SaveCurrentLevel()
    {
        if (currentLevel == null)
        {
            SetStatus("No map to save.");
            return;
        }

        ApplyLevelNameFromView();

        if (!LevelFileService.SaveUserLevel(
                currentLevel,
                currentLevel.id,
                out string savedPath))
        {
            SetStatus("Save failed.");
            return;
        }

        lastSavedPath = savedPath;
        SelectedLevelStore.SetSelectedPath(savedPath);
        RefreshLevelList(savedPath);
        SetStatus($"Saved: {savedPath}");
    }

    public void LoadMostRecentLevel()
    {
        if (levelPaths.Count == 0)
        {
            RefreshLevelList();
        }

        if (levelPaths.Count == 0)
        {
            currentLevel = LevelFileService.LoadOrCreateDefault();
            lastSavedPath = string.Empty;
            mapPanOffset = Vector2.zero;
            panelView?.SetLevelName(currentLevel.displayName);
            RefreshPreview();
            RefreshView("No saved maps found. Loaded generated default map.");
            return;
        }

        selectedLevelIndex =
            Mathf.Clamp(selectedLevelIndex, 0, levelPaths.Count - 1);

        LoadLevelAtSelectedIndex();
    }

    public void DeleteSelectedLevel()
    {
        if (selectedLevelIndex < 0 || selectedLevelIndex >= levelPaths.Count)
        {
            SetStatus("No map selected.");
            return;
        }

        string path = levelPaths[selectedLevelIndex];

        if (!LevelFileService.TryDeleteUserLevel(path))
        {
            SetStatus("Only user-saved maps can be deleted.");
            return;
        }

        string deletedPath = path;
        RefreshLevelList();
        SetStatus($"Deleted: {deletedPath}");
    }

    public void SetCurrentLevelAsDefault()
    {
        if (currentLevel == null)
        {
            SetStatus("No map to set as default.");
            return;
        }

        LevelTerrainData defaultCopy =
            JsonUtility.FromJson<LevelTerrainData>(
                JsonUtility.ToJson(currentLevel)
            );

        if (!LevelFileService.SaveBuiltInDefaultLevel(defaultCopy, out string savedPath))
        {
            SetStatus("Set default failed.");
            return;
        }

        RefreshLevelList(savedPath);
        SetStatus($"Default map updated: {savedPath}");
    }

    public void SelectPreviousLevel()
    {
        if (levelPaths.Count == 0)
        {
            RefreshLevelList();
            return;
        }

        selectedLevelIndex =
            (selectedLevelIndex - 1 + levelPaths.Count) % levelPaths.Count;

        RenderSelectedLevel();
    }

    public void SelectNextLevel()
    {
        if (levelPaths.Count == 0)
        {
            RefreshLevelList();
            return;
        }

        selectedLevelIndex =
            (selectedLevelIndex + 1) % levelPaths.Count;

        RenderSelectedLevel();
    }

    public void RefreshLevelList()
    {
        RefreshLevelList(lastSavedPath);
    }

    private void RefreshLevelList(string preferredPath)
    {
        levelPaths.Clear();
        levelPaths.AddRange(LevelFileService.GetAllLevelPaths());

        selectedLevelIndex = levelPaths.IndexOf(preferredPath);

        if (selectedLevelIndex < 0 && levelPaths.Count > 0)
        {
            selectedLevelIndex = 0;
        }

        RenderSelectedLevel();
    }

    private void LoadLevelAtSelectedIndex()
    {
        string path = levelPaths[selectedLevelIndex];

        if (!LevelFileService.TryLoadLevel(path, out LevelTerrainData loaded))
        {
            SetStatus("Load failed.");
            return;
        }

        currentLevel = loaded;
        lastSavedPath = path;
        mapPanOffset = Vector2.zero;
        SelectedLevelStore.SetSelectedPath(path);
        panelView?.SetLevelName(currentLevel.displayName);
        RefreshPreview();
        RenderSelectedLevel();
        RefreshView($"Loaded: {path}");
    }

    public void Playtest()
    {
        if (currentLevel == null)
        {
            SetStatus("No map to playtest.");
            return;
        }

        LevelValidationResult validation =
            LevelValidationService.Validate(
                currentLevel,
                solidThreshold
            );

        if (!validation.IsValid)
        {
            SetStatus(validation.Errors[0]);
            return;
        }

        if (string.IsNullOrWhiteSpace(lastSavedPath))
        {
            SaveCurrentLevel();
        }

        SelectedLevelStore.SetTemporaryLevel(currentLevel);
        SceneManager.LoadScene(SettingManager.Scene.gameplay);
    }

    private void EnsureReferences()
    {
        if (panelView == null)
        {
            panelView = GetComponentInChildren<LevelEditorPanelView>(true);
        }

        if (editorCamera == null)
        {
            editorCamera = Camera.main;
        }

        if (terrainPreviewRoot == null)
        {
            terrainPreviewRoot = transform;
        }

        if (terrainRenderer == null)
        {
            terrainRenderer =
                terrainPreviewRoot.GetComponentInChildren<LevelTerrainRenderer2D>(true);
        }

        if (colliderBuilder == null)
        {
            colliderBuilder =
                terrainPreviewRoot.GetComponentInChildren<LevelTerrainColliderBuilder>(true);
        }
    }

    private void SubscribeView()
    {
        if (panelView == null)
        {
            return;
        }

        panelView.DigClicked += SetDigMode;
        panelView.FillClicked += SetFillMode;
        panelView.StartClicked += SetStartMode;
        panelView.FinishClicked += SetFinishMode;
        panelView.NewClicked += CreateNewLevel;
        panelView.SaveClicked += SaveCurrentLevel;
        panelView.LoadClicked += LoadMostRecentLevel;
        panelView.PreviousLevelClicked += SelectPreviousLevel;
        panelView.NextLevelClicked += SelectNextLevel;
        panelView.RefreshLevelsClicked += RefreshLevelList;
        panelView.DeleteLevelClicked += DeleteSelectedLevel;
        panelView.SetDefaultClicked += SetCurrentLevelAsDefault;
        panelView.PlaytestClicked += Playtest;
        panelView.BackClicked += HideEditor;
        panelView.RadiusChanged += SetBrushRadius;
        panelView.HardnessChanged += SetBrushHardness;
        panelView.LevelNameChanged += SetCurrentLevelName;
    }

    private void UnsubscribeView()
    {
        if (panelView == null)
        {
            return;
        }

        panelView.DigClicked -= SetDigMode;
        panelView.FillClicked -= SetFillMode;
        panelView.StartClicked -= SetStartMode;
        panelView.FinishClicked -= SetFinishMode;
        panelView.NewClicked -= CreateNewLevel;
        panelView.SaveClicked -= SaveCurrentLevel;
        panelView.LoadClicked -= LoadMostRecentLevel;
        panelView.PreviousLevelClicked -= SelectPreviousLevel;
        panelView.NextLevelClicked -= SelectNextLevel;
        panelView.RefreshLevelsClicked -= RefreshLevelList;
        panelView.DeleteLevelClicked -= DeleteSelectedLevel;
        panelView.SetDefaultClicked -= SetCurrentLevelAsDefault;
        panelView.PlaytestClicked -= Playtest;
        panelView.BackClicked -= HideEditor;
        panelView.RadiusChanged -= SetBrushRadius;
        panelView.HardnessChanged -= SetBrushHardness;
        panelView.LevelNameChanged -= SetCurrentLevelName;
    }

    private void HandlePaintInput()
    {
        if (currentLevel == null ||
            Mouse.current == null)
        {
            return;
        }

        if (!TryGetMapPosition(
                Mouse.current.position.ReadValue(),
                out Vector2 worldPosition))
        {
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isPainting = false;
                brush.EndStroke();
            }

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPainting = true;
            brush.BeginStroke(worldPosition, currentLevel);
            RefreshPreview();
        }
        else if (Mouse.current.leftButton.isPressed && isPainting)
        {
            brush.ContinueStroke(worldPosition, currentLevel);
            RefreshPreview();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isPainting = false;
            brush.EndStroke();
        }
    }

    private void HandleCameraInput()
    {
        Vector2 move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                move.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                move.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                move.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                move.y += 1f;
            }
        }

        if (move.sqrMagnitude > 0.01f)
        {
            if (CanControlMapView())
            {
                PanMapView(
                    -move.normalized *
                    mapPanSpeed *
                    Time.unscaledDeltaTime
                );
            }
            else
            {
                PanEditorCamera(move.normalized);
            }
        }

        if (Mouse.current != null)
        {
            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (CanControlMapView())
                {
                    mapDisplayPixelsPerSample =
                        Mathf.Clamp(
                            mapDisplayPixelsPerSample + scroll * mapZoomStep,
                            minMapDisplayPixelsPerSample,
                            maxMapDisplayPixelsPerSample
                        );

                    ApplyMapCanvasLayout();
                }
                else
                {
                    ZoomEditorCamera(scroll);
                }
            }
        }
    }

    private void PanEditorCamera(Vector2 direction)
    {
        if (editorCamera == null)
        {
            return;
        }

        editorCamera.transform.position +=
            (Vector3)(direction *
                      cameraPanSpeed *
                      Time.unscaledDeltaTime);
    }

    private void ZoomEditorCamera(float scroll)
    {
        if (editorCamera == null)
        {
            return;
        }

        editorCamera.orthographicSize =
            Mathf.Clamp(
                editorCamera.orthographicSize -
                scroll * zoomSpeed * Time.unscaledDeltaTime,
                minZoom,
                maxZoom
            );
    }

    private bool CanControlMapView()
    {
        return currentLevel != null &&
               panelView != null &&
               panelView.MapCanvas != null &&
               panelView.MapCanvas.transform.parent is RectTransform;
    }

    private void PanMapView(Vector2 delta)
    {
        mapPanOffset += delta;
        ApplyMapCanvasLayout();
    }

    private void RefreshPreview()
    {
        if (currentLevel == null)
        {
            return;
        }

        RefreshMapTexture();

        LevelTerrainMeshData meshData =
            LevelTerrainMarchingSquares.Build(currentLevel, solidThreshold);

        if (terrainRenderer != null)
        {
            terrainRenderer.Render(meshData);
        }

        if (colliderBuilder != null)
        {
            colliderBuilder.Build(meshData);
        }
    }

    private void RefreshView(string status)
    {
        if (panelView == null)
        {
            return;
        }

        panelView.RenderTool(brush.Mode);
        panelView.RenderBrush(brush.Radius, brush.Hardness);
        RenderSelectedLevel();
        panelView.SetStatus(status);
    }

    private void RenderSelectedLevel()
    {
        if (panelView == null)
        {
            return;
        }

        if (levelPaths.Count == 0)
        {
            panelView.SetSelectedLevelLabel("No saved maps");
            return;
        }

        string label =
            $"{selectedLevelIndex + 1}/{levelPaths.Count} " +
            LevelFileService.GetLevelListLabel(levelPaths[selectedLevelIndex]);

        panelView.SetSelectedLevelLabel(label);
    }

    private void ApplyLevelNameFromView()
    {
        if (panelView != null)
        {
            SetCurrentLevelName(panelView.LevelNameValue);
        }
    }

    private void SetCurrentLevelName(string value)
    {
        if (currentLevel == null)
        {
            return;
        }

        string displayName =
            string.IsNullOrWhiteSpace(value)
                ? "Untitled Level"
                : value.Trim();

        currentLevel.displayName = displayName;
        currentLevel.id = LevelFileService.SanitizeFileName(displayName);
    }

    private void SetStatus(string status)
    {
        if (panelView != null)
        {
            panelView.SetStatus(status);
        }
    }

    private bool TryGetMapPosition(
        Vector2 screenPosition,
        out Vector2 worldPosition)
    {
        worldPosition = Vector2.zero;

        if (panelView == null ||
            panelView.MapCanvas == null)
        {
            return false;
        }

        RectTransform rect =
            panelView.MapCanvas.rectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                screenPosition,
                null,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect localRect = rect.rect;

        if (!localRect.Contains(localPoint))
        {
            return false;
        }

        float normalizedX =
            Mathf.InverseLerp(localRect.xMin, localRect.xMax, localPoint.x);

        float normalizedY =
            Mathf.InverseLerp(localRect.yMin, localRect.yMax, localPoint.y);

        worldPosition = new Vector2(
            normalizedX * currentLevel.SizeWorld.x,
            normalizedY * currentLevel.SizeWorld.y
        );

        return true;
    }

    private void RefreshMapTexture()
    {
        if (currentLevel == null ||
            panelView == null)
        {
            return;
        }

        if (mapTexture == null ||
            mapTexture.width != currentLevel.width ||
            mapTexture.height != currentLevel.height)
        {
            mapTexture =
                new Texture2D(
                    currentLevel.width,
                    currentLevel.height,
                    TextureFormat.RGBA32,
                    false
                );

            mapTexture.filterMode = FilterMode.Point;
            mapTexture.wrapMode = TextureWrapMode.Clamp;
            panelView.SetMapTexture(mapTexture);
        }

        ApplyMapCanvasLayout();

        byte[] densities = currentLevel.GetDensities();
        Color32 solidColor = new Color32(84, 105, 120, 255);
        Color32 emptyColor = new Color32(9, 12, 16, 255);
        Color32 softColor = new Color32(42, 55, 65, 255);

        for (int y = 0; y < currentLevel.height; y++)
        {
            for (int x = 0; x < currentLevel.width; x++)
            {
                byte density = densities[currentLevel.ToIndex(x, y)];
                Color32 color =
                    density >= solidThreshold
                        ? solidColor
                        : density <= 24
                            ? emptyColor
                            : softColor;

                mapTexture.SetPixel(x, y, color);
            }
        }

        DrawMarker(currentLevel.start.Position, new Color32(80, 230, 120, 255), 3);
        DrawMarker(currentLevel.finish.Position, new Color32(255, 210, 70, 255), 3);

        mapTexture.Apply(false);
    }

    private void ApplyMapCanvasLayout()
    {
        if (currentLevel == null ||
            panelView == null ||
            panelView.MapCanvas == null ||
            currentLevel.height <= 0)
        {
            return;
        }

        RawImage canvas =
            panelView.MapCanvas;

        canvas.raycastTarget = true;

        RectTransform rect =
            canvas.rectTransform;

        RectTransform viewport =
            rect.parent as RectTransform;

        if (viewport != null &&
            !viewport.TryGetComponent(out RectMask2D _))
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        AspectRatioFitter fitter =
            canvas.GetComponent<AspectRatioFitter>();

        if (fitter != null)
        {
            Destroy(fitter);
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta =
            new Vector2(currentLevel.width, currentLevel.height) *
            mapDisplayPixelsPerSample;

        mapPanOffset =
            ClampMapPanOffset(mapPanOffset, rect.sizeDelta, viewport);

        rect.anchoredPosition = mapPanOffset;
        rect.localScale = Vector3.one;
    }

    private static Vector2 ClampMapPanOffset(
        Vector2 panOffset,
        Vector2 mapSize,
        RectTransform viewport)
    {
        if (viewport == null)
        {
            return panOffset;
        }

        Vector2 viewportSize =
            viewport.rect.size;

        float maxX =
            Mathf.Max(0f, (mapSize.x - viewportSize.x) * 0.5f);

        float maxY =
            Mathf.Max(0f, (mapSize.y - viewportSize.y) * 0.5f);

        panOffset.x =
            maxX > 0f
                ? Mathf.Clamp(panOffset.x, -maxX, maxX)
                : 0f;

        panOffset.y =
            maxY > 0f
                ? Mathf.Clamp(panOffset.y, -maxY, maxY)
                : 0f;

        return panOffset;
    }

    private void OnDestroy()
    {
        if (mapTexture != null)
        {
            Destroy(mapTexture);
        }
    }

    private void DrawMarker(
        Vector2 worldPosition,
        Color32 color,
        int radius)
    {
        if (mapTexture == null ||
            currentLevel == null)
        {
            return;
        }

        Vector2Int sample =
            currentLevel.WorldToSample(worldPosition);

        for (int y = sample.y - radius; y <= sample.y + radius; y++)
        {
            for (int x = sample.x - radius; x <= sample.x + radius; x++)
            {
                if (!currentLevel.IsInside(x, y))
                {
                    continue;
                }

                int dx = x - sample.x;
                int dy = y - sample.y;

                if (dx * dx + dy * dy > radius * radius)
                {
                    continue;
                }

                mapTexture.SetPixel(x, y, color);
            }
        }
    }

    private void SetDigMode()
    {
        brush.Mode = LevelBrushMode.Dig;
        RefreshView("Dig mode.");
    }

    private void SetFillMode()
    {
        brush.Mode = LevelBrushMode.Fill;
        RefreshView("Fill mode.");
    }

    private void SetStartMode()
    {
        brush.Mode = LevelBrushMode.PlaceStart;
        RefreshView("Click inside a tunnel to place the ship start.");
    }

    private void SetFinishMode()
    {
        brush.Mode = LevelBrushMode.PlaceFinish;
        RefreshView("Click inside a tunnel to place the finish zone.");
    }

    private void SetBrushRadius(float value)
    {
        brush.Radius = Mathf.Max(0.1f, value);
        RefreshView("Brush radius changed.");
    }

    private void SetBrushHardness(float value)
    {
        brush.Hardness = Mathf.Clamp01(value);
        RefreshView("Brush hardness changed.");
    }

    private void HideEditor()
    {
        BackRequested?.Invoke();
    }
}
