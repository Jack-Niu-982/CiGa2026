using UnityEngine;

/// <summary>
/// 第一版 Gameplay 血量显示。
/// 使用 IMGUI 画在屏幕正中下方，后续正式 UI 可以再接入 UIFlow。
/// </summary>
[DisallowMultipleComponent]
public class SubmarineHealthGui : MonoBehaviour
{
    [Header("血量来源")]
    [SerializeField]
    private SubmarineHealth2D health;

    [Header("血条")]
    [SerializeField]
    private bool showHealthBar = true;

    [SerializeField]
    private float healthBarWidth = 360f;

    [SerializeField]
    private float healthBarHeight = 24f;

    [SerializeField]
    private float healthBarBottomOffset = 42f;

    [SerializeField]
    private Color healthBarBackgroundColor =
        new Color(0.05f, 0.06f, 0.08f, 0.82f);

    [SerializeField]
    private Color healthBarFrameColor =
        new Color(1f, 1f, 1f, 0.85f);

    [SerializeField]
    private Color highHealthColor =
        new Color(0.25f, 0.9f, 0.45f, 1f);

    [SerializeField]
    private Color mediumHealthColor =
        new Color(1f, 0.74f, 0.22f, 1f);

    [SerializeField]
    private Color lowHealthColor =
        new Color(1f, 0.24f, 0.2f, 1f);

    private GUIStyle centeredLabelStyle;

    private void Reset()
    {
        health =
            FindObjectOfType<SubmarineHealth2D>();
    }

    private void Awake()
    {
        if (health == null)
        {
            health =
                FindObjectOfType<SubmarineHealth2D>();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (showHealthBar)
        {
            DrawHealthBar();
        }

    }

    private void DrawHealthBar()
    {
        float width =
            Mathf.Min(
                healthBarWidth,
                Screen.width - 32f
            );

        Rect frameRect =
            new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height -
                    healthBarBottomOffset -
                    healthBarHeight,
                width,
                healthBarHeight
            );

        Rect innerRect =
            new Rect(
                frameRect.x + 2f,
                frameRect.y + 2f,
                frameRect.width - 4f,
                frameRect.height - 4f
            );

        float currentHealth =
            health != null
                ? health.CurrentHealth
                : 0f;

        float maxHealth =
            health != null
                ? health.MaxHealth
                : 1f;

        float healthRatio =
            maxHealth > 0f
                ? Mathf.Clamp01(currentHealth / maxHealth)
                : 0f;

        Rect fillRect =
            new Rect(
                innerRect.x,
                innerRect.y,
                innerRect.width * healthRatio,
                innerRect.height
            );

        Color previousColor =
            GUI.color;

        GUI.color = healthBarFrameColor;
        GUI.DrawTexture(
            frameRect,
            Texture2D.whiteTexture
        );

        GUI.color = healthBarBackgroundColor;
        GUI.DrawTexture(
            innerRect,
            Texture2D.whiteTexture
        );

        GUI.color =
            GetHealthColor(healthRatio);

        GUI.DrawTexture(
            fillRect,
            Texture2D.whiteTexture
        );

        GUI.color = Color.white;
        GUI.Label(
            frameRect,
            $"HP {currentHealth:0}/{maxHealth:0}",
            centeredLabelStyle
        );

        GUI.color =
            previousColor;
    }

    private Color GetHealthColor(
        float healthRatio)
    {
        if (healthRatio <= 0.3f)
        {
            return lowHealthColor;
        }

        if (healthRatio <= 0.6f)
        {
            return mediumHealthColor;
        }

        return highHealthColor;
    }

    private void EnsureStyles()
    {
        if (centeredLabelStyle == null)
        {
            centeredLabelStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment =
                        TextAnchor.MiddleCenter,
                    fontStyle =
                        FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.white
                    }
                };
        }

    }
}
