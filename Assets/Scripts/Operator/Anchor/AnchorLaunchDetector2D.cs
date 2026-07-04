using UnityEngine;

/// <summary>
/// 负责准备船锚的最大飞行距离，并检测船锚飞行过程中实际经过的路径。
/// 不会在发射前提前判断远处是否存在可抓取目标。
/// </summary>
[DisallowMultipleComponent]
public class AnchorLaunchDetector2D : MonoBehaviour
{
    [System.Serializable]
    public struct LaunchResult
    {
        public bool IsValid;
        public Vector2 LauncherPosition;
        public Vector2 RayOrigin;
        public Vector2 Direction;
        public float AvailableTravelDistance;
        public Vector2 FlightTargetPoint;
        public string FailureReason;
    }

    [Header("可固定目标判定")]
    [Tooltip("船锚能够固定到哪些 Layer。通常只勾选 Wall 所在的 Layer。")]
    [SerializeField] private LayerMask attachableLayer;

    [Tooltip("是否允许船锚固定到 Trigger Collider2D。通常建议关闭。")]
    [SerializeField] private bool allowTriggerTargets = false;

    [Header("调试")]
    [Tooltip("显示船锚本帧实际经过的检测线段。绿色代表命中，红色代表未命中。")]
    [SerializeField] private bool drawDebugRay = true;

    [Min(0f)]
    [SerializeField] private float debugRayDuration = 0.15f;

    /// <summary>
    /// 只准备发射数据：
    /// 验证方向与最大绳长，并计算未命中时的极限位置。
    ///
    /// 这里不会检测 Wall，
    /// 也不会提前决定船锚能否抓住目标。
    /// </summary>
    public bool TryGetLaunchResult(
        Vector2 launcherPosition,
        Vector2 anchorRestPosition,
        Vector2 launchDirection,
        float maxRopeLength,
        out LaunchResult result
    )
    {
        result = new LaunchResult
        {
            IsValid = false,
            LauncherPosition = launcherPosition,
            RayOrigin = anchorRestPosition,
            Direction = Vector2.zero,
            AvailableTravelDistance = 0f,
            FlightTargetPoint = anchorRestPosition,
            FailureReason = string.Empty
        };

        if (launchDirection.sqrMagnitude <= 0.0001f)
        {
            result.FailureReason = "发射方向长度为 0。";
            return false;
        }

        if (maxRopeLength <= 0f)
        {
            result.FailureReason = "Max Rope Length 必须大于 0。";
            return false;
        }

        Vector2 direction = launchDirection.normalized;

        float alreadyUsedLength =
            Vector2.Distance(
                launcherPosition,
                anchorRestPosition
            );

        float availableTravelDistance =
            maxRopeLength - alreadyUsedLength;

        result.Direction = direction;
        result.AvailableTravelDistance =
            availableTravelDistance;

        if (availableTravelDistance <= 0f)
        {
            result.FailureReason =
                "船锚待机位置已经达到或超过最大绳长。";

            return false;
        }

        result.IsValid = true;

        result.FlightTargetPoint =
            anchorRestPosition +
            direction * availableTravelDistance;

        return true;
    }

    /// <summary>
    /// 只检测船锚本帧从 previousPosition
    /// 到 nextPosition 实际经过的线段。
    ///
    /// 只有这段路径真正碰到 attachableLayer
    /// 中的物体时才返回 true。
    /// </summary>
    public bool TryDetectFlightHit(
        Vector2 previousPosition,
        Vector2 nextPosition,
        out RaycastHit2D nearestHit
    )
    {
        nearestHit = default(RaycastHit2D);

        Vector2 movement =
            nextPosition - previousPosition;

        float distance = movement.magnitude;

        if (distance <= 0.000001f)
        {
            return false;
        }

        Vector2 direction =
            movement / distance;

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                previousPosition,
                direction,
                distance,
                attachableLayer
            );

        float nearestDistance =
            float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider =
                hits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (
                !allowTriggerTargets &&
                hitCollider.isTrigger
            )
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance =
                    hits[i].distance;

                nearestHit = hits[i];
            }
        }

        bool hitTarget =
            nearestHit.collider != null;

        if (drawDebugRay)
        {
            Vector2 debugEnd =
                hitTarget
                    ? nearestHit.point
                    : nextPosition;

            Debug.DrawLine(
                previousPosition,
                debugEnd,
                hitTarget
                    ? Color.green
                    : Color.red,
                debugRayDuration
            );
        }

        return hitTarget;
    }

    private void OnValidate()
    {
        debugRayDuration =
            Mathf.Max(
                0f,
                debugRayDuration
            );
    }
}