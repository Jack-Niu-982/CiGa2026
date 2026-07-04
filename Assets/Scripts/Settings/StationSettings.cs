using UnityEngine;

/// <summary>
/// 工作站的通用配置。组件只保留场景引用和事件，数值统一从这里读取。
/// </summary>
[CreateAssetMenu(
    fileName = "StationSettings",
    menuName = "Settings/Station Settings")]
public class StationSettings : ScriptableObject
{
    [Header("交互类型")]
    public InteractionType stationType = InteractionType.None;

    [Header("交互需求")]
    public InteractionRequirement requirement =
        new InteractionRequirement();

    [Header("描边高亮")]
    public Material outlineMaterial;

    [Min(0.01f)]
    public float outlineFadeInDuration = 0.3f;

    [Min(0.01f)]
    public float outlineFadeOutDuration = 0.2f;

    public Color outlineColor =
        new Color(1f, 0.92f, 0.02f, 1f);

    [Range(0f, 0.1f)]
    public float outlineWidth = 0.02f;

    [Header("通用效果数值")]
    [Min(0f)]
    public float defenseDamage = 10f;

    [Min(0f)]
    public float defenseCooldown = 1f;

    [Min(0f)]
    public float fuelAmount = 20f;

    [Min(0f)]
    public float repairAmount = 20f;

    [Header("编辑器显示")]
    [Min(0f)]
    public float gizmoRadius = 1f;
}
