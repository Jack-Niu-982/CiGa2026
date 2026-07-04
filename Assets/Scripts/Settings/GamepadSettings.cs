using UnityEngine;

/// <summary>
/// 手柄输入与震动反馈配置。
/// </summary>
[CreateAssetMenu(
    fileName = "GamepadSettings",
    menuName = "Settings/Gamepad Settings")]
public class GamepadSettings : ScriptableObject
{
    [Header("基础输入")]
    [Tooltip("摇杆死区。")]
    [Range(0f, 0.95f)]
    public float stickDeadZone = 0.2f;

    [Tooltip("垂直输入阈值（触发攀爬/下梯子）。")]
    [Range(0.1f, 1f)]
    public float verticalInputThreshold = 0.6f;

    [Header("锚钩卷线")]
    [Tooltip("卷线摇杆最小幅度。")]
    [Range(0.1f, 0.99f)]
    public float reelStickMinimumMagnitude = 0.65f;

    [Tooltip("最小卷线角速度（度/秒）。")]
    [Min(1f)]
    public float minimumReelAngularSpeed = 45f;

    [Tooltip("全速卷线角速度（度/秒）。")]
    [Min(1f)]
    public float fullReelAngularSpeed = 360f;

    [Tooltip("最小卷线量。")]
    [Range(0.01f, 1f)]
    public float minimumReelAmount = 0.25f;

    [Tooltip("卷线输入宽容时间（秒）。")]
    [Range(0f, 0.3f)]
    public float reelInputGraceTime = 0.08f;

    [Tooltip("最大可接受的角度偏差（度）。")]
    [Range(30f, 180f)]
    public float maximumAcceptedAngleDelta = 120f;

    [Tooltip("卷线量上升速度。")]
    [Min(0.1f)]
    public float reelAmountRiseSpeed = 10f;

    [Tooltip("卷线量下降速度。")]
    [Min(0.1f)]
    public float reelAmountFallSpeed = 14f;

    [Header("震动反馈")]
    [Tooltip("启用锚钩震动反馈。")]
    public bool enableAnchorRumble = true;

    [Tooltip("卷线震动步进角度（度）。")]
    [Range(10f, 180f)]
    public float rumbleStepDegrees = 45f;

    [Tooltip("卷线节拍震动持续时间（秒）。")]
    [Range(0.01f, 0.2f)]
    public float reelTickDuration = 0.045f;

    [Tooltip("卷线节拍低频强度。")]
    [Range(0f, 1f)]
    public float reelTickLowFrequency = 0.42f;

    [Tooltip("卷线节拍高频强度。")]
    [Range(0f, 1f)]
    public float reelTickHighFrequency = 0.22f;

    [Tooltip("连续卷线低频强度。")]
    [Range(0f, 1f)]
    public float continuousReelLowFrequency = 0.18f;

    [Tooltip("连续卷线高频强度。")]
    [Range(0f, 1f)]
    public float continuousReelHighFrequency = 0.07f;

    [Tooltip("发射震动持续时间（秒）。")]
    [Range(0.01f, 0.5f)]
    public float shootRumbleDuration = 0.12f;

    [Tooltip("发射低频强度。")]
    [Range(0f, 1f)]
    public float shootLowFrequency = 0.25f;

    [Tooltip("发射高频强度。")]
    [Range(0f, 1f)]
    public float shootHighFrequency = 0.65f;

    [Tooltip("收回震动持续时间（秒）。")]
    [Range(0.01f, 0.5f)]
    public float retractRumbleDuration = 0.16f;

    [Tooltip("收回低频强度。")]
    [Range(0f, 1f)]
    public float retractLowFrequency = 0.55f;

    [Tooltip("收回高频强度。")]
    [Range(0f, 1f)]
    public float retractHighFrequency = 0.18f;
}
