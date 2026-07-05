using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelEditorPanelView : MonoBehaviour
{
    [Header("Toolbar")]
    [SerializeField] private Button digButton;
    [SerializeField] private Button fillButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button finishButton;

    [Header("Files")]
    [SerializeField] private Button newButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button playtestButton;
    [SerializeField] private Button backButton;

    [Header("Brush")]
    [SerializeField] private Slider radiusSlider;
    [SerializeField] private Slider hardnessSlider;
    [SerializeField] private TMP_Text radiusLabel;
    [SerializeField] private TMP_Text hardnessLabel;

    [Header("Status")]
    [SerializeField] private TMP_Text toolLabel;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Canvas")]
    [SerializeField] private RawImage mapCanvas;

    public event Action DigClicked;
    public event Action FillClicked;
    public event Action StartClicked;
    public event Action FinishClicked;
    public event Action NewClicked;
    public event Action SaveClicked;
    public event Action LoadClicked;
    public event Action PlaytestClicked;
    public event Action BackClicked;
    public event Action<float> RadiusChanged;
    public event Action<float> HardnessChanged;

    public float RadiusValue =>
        radiusSlider != null ? radiusSlider.value : 4f;

    public float HardnessValue =>
        hardnessSlider != null ? hardnessSlider.value : 0.75f;

    public RawImage MapCanvas => mapCanvas;

    private void Awake()
    {
        EnsureSliderVisuals(radiusSlider, new Color(0.30f, 0.64f, 1f, 1f));
        EnsureSliderVisuals(hardnessSlider, new Color(0.96f, 0.76f, 0.28f, 1f));
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void RenderTool(LevelBrushMode mode)
    {
        if (toolLabel != null)
        {
            toolLabel.text = $"Tool: {mode}";
        }
    }

    public void RenderBrush(float radius, float hardness)
    {
        if (radiusLabel != null)
        {
            radiusLabel.text = $"Radius {radius:0.0}";
        }

        if (hardnessLabel != null)
        {
            hardnessLabel.text = $"Hardness {hardness:0.00}";
        }
    }

    public void SetStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }

    public void SetMapTexture(Texture texture)
    {
        if (mapCanvas != null)
        {
            mapCanvas.texture = texture;
        }
    }

    private void AddListeners()
    {
        if (digButton != null)
        {
            digButton.onClick.AddListener(HandleDigClicked);
        }

        if (fillButton != null)
        {
            fillButton.onClick.AddListener(HandleFillClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (finishButton != null)
        {
            finishButton.onClick.AddListener(HandleFinishClicked);
        }

        if (newButton != null)
        {
            newButton.onClick.AddListener(HandleNewClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(HandleSaveClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(HandleLoadClicked);
        }

        if (playtestButton != null)
        {
            playtestButton.onClick.AddListener(HandlePlaytestClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackClicked);
        }

        if (radiusSlider != null)
        {
            radiusSlider.onValueChanged.AddListener(HandleRadiusChanged);
        }

        if (hardnessSlider != null)
        {
            hardnessSlider.onValueChanged.AddListener(HandleHardnessChanged);
        }
    }

    private void RemoveListeners()
    {
        if (digButton != null)
        {
            digButton.onClick.RemoveListener(HandleDigClicked);
        }

        if (fillButton != null)
        {
            fillButton.onClick.RemoveListener(HandleFillClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }

        if (finishButton != null)
        {
            finishButton.onClick.RemoveListener(HandleFinishClicked);
        }

        if (newButton != null)
        {
            newButton.onClick.RemoveListener(HandleNewClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(HandleSaveClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(HandleLoadClicked);
        }

        if (playtestButton != null)
        {
            playtestButton.onClick.RemoveListener(HandlePlaytestClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
        }

        if (radiusSlider != null)
        {
            radiusSlider.onValueChanged.RemoveListener(HandleRadiusChanged);
        }

        if (hardnessSlider != null)
        {
            hardnessSlider.onValueChanged.RemoveListener(HandleHardnessChanged);
        }
    }

    private void HandleDigClicked() => DigClicked?.Invoke();
    private void HandleFillClicked() => FillClicked?.Invoke();
    private void HandleStartClicked() => StartClicked?.Invoke();
    private void HandleFinishClicked() => FinishClicked?.Invoke();
    private void HandleNewClicked() => NewClicked?.Invoke();
    private void HandleSaveClicked() => SaveClicked?.Invoke();
    private void HandleLoadClicked() => LoadClicked?.Invoke();
    private void HandlePlaytestClicked() => PlaytestClicked?.Invoke();
    private void HandleBackClicked() => BackClicked?.Invoke();
    private void HandleRadiusChanged(float value) => RadiusChanged?.Invoke(value);
    private void HandleHardnessChanged(float value) => HardnessChanged?.Invoke(value);

    private static void EnsureSliderVisuals(Slider slider, Color fillColor)
    {
        if (slider == null)
        {
            return;
        }

        RectTransform sliderRect =
            slider.GetComponent<RectTransform>();

        if (sliderRect == null)
        {
            return;
        }

        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;

        Image rootImage =
            slider.GetComponent<Image>();

        if (rootImage != null)
        {
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = true;
        }

        Image background =
            GetOrCreateImage(sliderRect, "Background");

        background.color = new Color(0.13f, 0.15f, 0.18f, 1f);
        background.raycastTarget = true;
        SetStretchRect(
            background.rectTransform,
            new Vector2(0f, 0.36f),
            new Vector2(1f, 0.64f),
            Vector2.zero,
            Vector2.zero
        );

        RectTransform fillArea =
            GetOrCreateRect(sliderRect, "Fill Area");

        SetStretchRect(
            fillArea,
            new Vector2(0f, 0.36f),
            new Vector2(1f, 0.64f),
            new Vector2(9f, 0f),
            new Vector2(-9f, 0f)
        );

        Image fill =
            GetOrCreateImage(fillArea, "Fill");

        fill.color = fillColor;
        fill.raycastTarget = false;
        SetStretchRect(
            fill.rectTransform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        RectTransform handleArea =
            GetOrCreateRect(sliderRect, "Handle Slide Area");

        SetStretchRect(
            handleArea,
            Vector2.zero,
            Vector2.one,
            new Vector2(9f, 0f),
            new Vector2(-9f, 0f)
        );

        Image handle =
            GetOrCreateImage(handleArea, "Handle");

        handle.color = new Color(0.94f, 0.96f, 1f, 1f);
        handle.raycastTarget = true;
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(18f, 28f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;

        LayoutElement layout =
            slider.GetComponent<LayoutElement>();

        if (layout != null)
        {
            layout.preferredHeight = Mathf.Max(layout.preferredHeight, 34f);
        }
    }

    private static RectTransform GetOrCreateRect(
        RectTransform parent,
        string name)
    {
        Transform existing =
            parent.Find(name);

        if (existing != null &&
            existing.TryGetComponent(out RectTransform rect))
        {
            return rect;
        }

        GameObject gameObject =
            new GameObject(name, typeof(RectTransform));

        gameObject.transform.SetParent(parent, false);

        return gameObject.GetComponent<RectTransform>();
    }

    private static Image GetOrCreateImage(
        RectTransform parent,
        string name)
    {
        RectTransform rect =
            GetOrCreateRect(parent, name);

        if (!rect.TryGetComponent(out CanvasRenderer _))
        {
            rect.gameObject.AddComponent<CanvasRenderer>();
        }

        if (!rect.TryGetComponent(out Image image))
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        return image;
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
}
