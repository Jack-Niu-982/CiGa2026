using UnityEngine;

/// <summary>
/// 锚钩发射、绳索、旋转配置。
/// </summary>
[CreateAssetMenu(
    fileName = "AnchorSettings",
    menuName = "Settings/Anchor Settings")]
public class AnchorSettings : ScriptableObject
{
    [Header("锚钩行为")]
    [Tooltip("最大绳索长度。")]
    [Min(0.1f)]
    public float maxRopeLength = 15f;

    [Tooltip("锚钩发射偏移距离。")]
    [Min(0f)]
    public float anchorOffset = 0.5f;

    [Tooltip("锚钩发射速度（units/s）。")]
    [Min(0f)]
    public float anchorShootSpeed = 25f;

    [Tooltip("锚钩收回速度（units/s）。")]
    [Min(0.01f)]
    public float anchorRetractSpeed = 18f;

    [Tooltip("击中墙壁后延迟多久开始拉船。")]
    [Min(0f)]
    public float wallHitPullDelay = 0.5f;

    [Tooltip("墙壁拉动冲量。")]
    [Min(0f)]
    public float wallPullImpulse = 6f;

    [Header("绳索显示")]
    [Tooltip("绳索宽度随长度反比缩放。")]
    public bool inverseWidthByLength = true;

    [Tooltip("参考长度下的绳索宽度。")]
    [Min(0.001f)]
    public float ropeWidthAtReferenceLength = 0.07f;

    [Tooltip("宽度参考长度。")]
    [Min(0.01f)]
    public float widthReferenceLength = 5f;

    [Tooltip("最小绳索宽度。")]
    [Min(0.001f)]
    public float minimumRopeWidth = 0.025f;

    [Tooltip("最大绳索宽度。")]
    [Min(0.001f)]
    public float maximumRopeWidth = 0.13f;

    [Tooltip("绳索颜色。")]
    public Color ropeColor = Color.white;

    [Header("锚钩旋转")]
    [Tooltip("旋转速度（度/秒）。")]
    [Min(0f)]
    public float rotationSpeed = 90f;

    [Tooltip("最大旋转角度。")]
    [Range(0f, 180f)]
    public float maxRotationAngle = 45f;

    [Tooltip("输入死区。")]
    [Range(0f, 1f)]
    public float rotationInputDeadZone = 0.1f;

    [Tooltip("反转旋转控制。")]
    public bool invertRotationControl = false;

    [Header("墙壁粒子反馈")]
    [Tooltip("启用墙壁粒子反馈。")]
    public bool enableWallParticleFeedback = true;

    [Tooltip("击中墙壁时的粒子数量。")]
    [Min(0)]
    public int wallHitParticleCount = 18;

    [Tooltip("击中粒子大小。")]
    [Min(0.001f)]
    public float wallHitParticleSize = 0.18f;

    [Tooltip("击中粒子颜色。")]
    public Color wallHitParticleColor = new Color(0.55f, 0.9f, 1f, 0.55f);

    [Tooltip("收回时的粒子数量。")]
    [Min(0)]
    public int wallRetractParticleCount = 12;

    [Tooltip("收回粒子大小。")]
    [Min(0.001f)]
    public float wallRetractParticleSize = 0.14f;

    [Tooltip("收回粒子颜色。")]
    public Color wallRetractParticleColor = new Color(0.75f, 0.92f, 1f, 0.42f);

    [Tooltip("粒子生命周期（秒）。")]
    [Min(0.01f)]
    public float wallParticleLifetime = 0.42f;

    [Tooltip("粒子向外飞出速度。")]
    [Min(0f)]
    public float wallParticleOutwardSpeed = 1.8f;
}
