using UnityEngine;

/// <summary>
/// 交互需求配置。
/// </summary>
[System.Serializable]
public class InteractionRequirement
{
    [Header("物品需求")]
    [Tooltip("需要的物品类型（None 表示不需要物品）。")]
    public CarryableItemType requiredItemType = CarryableItemType.Unknown;

    [Tooltip("是否消耗物品。")]
    public bool consumeItem = false;

    [Header("交互效果")]
    [Tooltip("交互成功后的效果描述。")]
    [TextArea(2, 4)]
    public string effectDescription = "";

    [Tooltip("交互持续时间（秒，0 表示瞬间完成）。")]
    [Min(0f)]
    public float duration = 0f;

    [Tooltip("是否需要 QTE（快速反应事件）。")]
    public bool requiresQTE = false;

    [Header("QTE 配置")]
    [Tooltip("QTE 时间窗口（秒）。")]
    [Min(0.1f)]
    public float qteTimeWindow = 2f;

    [Tooltip("QTE 需要按的键。")]
    public KeyCode qteKey = KeyCode.Space;

    [Header("音效")]
    [Tooltip("交互开始音效。")]
    public AudioClip interactionStartSound;

    [Tooltip("交互成功音效。")]
    public AudioClip interactionSuccessSound;

    [Tooltip("交互失败音效。")]
    public AudioClip interactionFailSound;
}
