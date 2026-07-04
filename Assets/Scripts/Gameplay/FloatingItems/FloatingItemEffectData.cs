using UnityEngine;

/// <summary>
/// 漂浮物效果配置。定义炸弹伤害值和蛛网禁用时长。
/// </summary>
[CreateAssetMenu(fileName = "FloatingItemEffectData", menuName = "Game/Floating Item Effect Data", order = 1)]
public class FloatingItemEffectData : ScriptableObject
{
    [Header("炸弹效果")]
    [Tooltip("炸弹对船体造成的伤害值。")]
    [Min(0f)]
    public float bombDamage = 20f;

    [Header("蛛网效果")]
    [Tooltip("蛛网禁用锚点的持续时间（秒）。")]
    [Min(0f)]
    public float webDisableDuration = 5f;
}
