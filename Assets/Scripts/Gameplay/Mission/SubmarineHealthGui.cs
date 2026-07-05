using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将船体生命值同步到一个只读 Scrollbar。
/// 组件只更新显示比例，不修改 RectTransform，因此血条位置可在 Inspector 中自由调整。
/// </summary>
[DisallowMultipleComponent]
public class SubmarineHealthGui : MonoBehaviour
{
    [Header("引用")]
    [SerializeField]
    private SubmarineHealth2D health;

    [SerializeField]
    private Scrollbar healthScrollbar;

    private void Reset()
    {
        healthScrollbar = GetComponent<Scrollbar>();
        health = FindObjectOfType<SubmarineHealth2D>();
        ConfigureScrollbar();
        Refresh();
    }

    private void Awake()
    {
        ResolveReferences();
        ConfigureScrollbar();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (health != null)
        {
            health.HealthChanged += HandleHealthChanged;
        }

        ConfigureScrollbar();
        Refresh();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= HandleHealthChanged;
        }
    }

    private void OnValidate()
    {
        if (healthScrollbar == null)
        {
            healthScrollbar = GetComponent<Scrollbar>();
        }

        ConfigureScrollbar();

        if (!Application.isPlaying)
        {
            Refresh();
        }
    }

    private void ResolveReferences()
    {
        if (healthScrollbar == null)
        {
            healthScrollbar = GetComponent<Scrollbar>();
        }

        if (health == null)
        {
            health = FindObjectOfType<SubmarineHealth2D>();
        }
    }

    private void ConfigureScrollbar()
    {
        if (healthScrollbar == null)
        {
            return;
        }

        healthScrollbar.interactable = false;
        healthScrollbar.direction = Scrollbar.Direction.LeftToRight;
        healthScrollbar.numberOfSteps = 0;
        healthScrollbar.value = 0f;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        SetHealthRatio(currentHealth, maxHealth);
    }

    private void Refresh()
    {
        if (health == null)
        {
            SetHealthRatio(0f, 1f);
            return;
        }

        SetHealthRatio(health.CurrentHealth, health.MaxHealth);
    }

    private void SetHealthRatio(float currentHealth, float maxHealth)
    {
        if (healthScrollbar == null)
        {
            return;
        }

        float ratio = maxHealth > 0f
            ? Mathf.Clamp01(currentHealth / maxHealth)
            : 0f;

        // Fill 作为 Scrollbar 内部的显示矩形；size 为 0 时会完全收起。
        healthScrollbar.size = ratio;
    }
}
