using UnityEngine;
using DG.Tweening;

/// <summary>
/// 漂浮物生成配置。
/// </summary>
[CreateAssetMenu(
    fileName = "FloatingItemSettings",
    menuName = "Settings/Floating Item Settings")]
public class FloatingItemSettings : ScriptableObject
{
    [Header("生成间隔")]
    [Tooltip("最小生成间隔（秒）。")]
    [Min(0.1f)]
    public float minSpawnInterval = 3f;

    [Tooltip("最大生成间隔（秒）。")]
    [Min(0.1f)]
    public float maxSpawnInterval = 5f;

    [Tooltip("最多同时存在的漂浮物数量。")]
    [Min(0)]
    public int maxAliveItems = 8;

    [Tooltip("启动时立即生成一次。")]
    public bool spawnOnStart = true;

    [Header("生成动效")]
    [Tooltip("生成时的缩放动画持续时间（秒）。")]
    [Min(0.1f)]
    public float spawnScaleDuration = 0.5f;

    [Tooltip("生成时的初始缩放比例。")]
    [Range(0f, 1f)]
    public float spawnStartScale = 0.3f;

    [Tooltip("生成时的透明度淡入持续时间（秒）。")]
    [Min(0.1f)]
    public float spawnFadeDuration = 0.4f;

    [Tooltip("生成动画的缓动曲线。")]
    public Ease spawnEase = Ease.OutBack;

    [Header("生成位置检测")]
    [Tooltip("船体周围不生成漂浮物的最小半径。")]
    [Min(0.5f)]
    public float boatExclusionRadius = 3f;

    [Tooltip("与其他漂浮物的最小间距。")]
    [Min(0.5f)]
    public float minItemDistance = 2f;

    [Tooltip("生成位置尝试次数（避免死循环）。")]
    [Min(1)]
    public int maxSpawnAttempts = 10;

    [Header("方向密度生成")]
    [Tooltip("船速对生成方向的影响强度。0=无偏置，1=最大偏置。")]
    [Range(0f, 1f)]
    public float velocityBiasStrength = 0.5f;

    [Tooltip("速度偏置饱和时对应的船速（units/s）。")]
    [Min(0.1f)]
    public float referenceSpeed = 2f;

    [Tooltip("船体中心左右多宽范围内的漂浮物不计入任何一侧。")]
    [Min(0f)]
    public float deadZoneHalfWidth = 1.5f;

    [Tooltip("任意一侧可获得的最低权重，防止某侧完全不生成。")]
    [Range(0.05f, 0.5f)]
    public float minSideWeight = 0.1f;

    [Header("漂浮物默认参数")]
    [Tooltip("漂浮物默认漂移速度。")]
    public Vector2 defaultDriftVelocity = new Vector2(-0.35f, 0f);

    [Tooltip("漂浮物默认角速度。")]
    public float defaultAngularSpeed = 20f;

    [Tooltip("漂浮物最小生命周期（秒）。")]
    [Min(1f)]
    public float minLifetime = 8f;

    [Tooltip("漂浮物最大生命周期（秒）。")]
    [Min(1f)]
    public float maxLifetime = 12f;

    [Header("生命周期闪烁")]
    [Tooltip("进入闪烁阶段前的剩余时间（秒）。")]
    [Min(0.1f)]
    public float blinkStartTime = 3f;

    [Tooltip("闪烁次数。")]
    [Min(1)]
    public int blinkCount = 3;

    [Tooltip("每次闪烁的持续时间（秒）。")]
    [Min(0.1f)]
    public float blinkDuration = 0.8f;

    [Tooltip("透明度最小值（0-1）。")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;

    [Tooltip("透明度最大值（0-1）。")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Tooltip("锚拉回速度。")]
    [Min(0.01f)]
    public float anchorPullSpeed = 5f;

    [Tooltip("到达距离判定。")]
    [Min(0.01f)]
    public float arrivalDistance = 0.12f;
}
