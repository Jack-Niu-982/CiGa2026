using UnityEngine;

/// <summary>
/// 开发调试配置。
/// </summary>
[CreateAssetMenu(
    fileName = "DevSettings",
    menuName = "Settings/Dev Settings")]
public class DevSettings : ScriptableObject
{
    [Header("调试日志")]
    [Tooltip("显示锚钩调试日志。")]
    public bool showAnchorDebugLog = true;

    [Tooltip("显示输入绑定日志。")]
    public bool showInputBindingLog = true;

    [Tooltip("显示旋转调试日志。")]
    public bool showRotationDebugLog = true;

    [Header("可视化调试")]
    [Tooltip("绘制调试射线。")]
    public bool drawDebugRay = true;

    [Tooltip("调试射线持续时间（秒）。")]
    [Min(0f)]
    public float debugRayDuration = 0.15f;

    [Header("Gizmos")]
    [Tooltip("显示地面检测 Gizmos。")]
    public bool showGroundCheckGizmos = true;

    [Tooltip("显示梯子检测 Gizmos。")]
    public bool showLadderCheckGizmos = true;

    [Tooltip("显示生成区域 Gizmos。")]
    public bool showSpawnZoneGizmos = true;
}
