using UnityEngine;

/// <summary>
/// 让船舱内部的 Kinematic 碰撞体跟随飞船本体。
/// 玩家只和这个内部碰撞代理发生接触，玩家重力不会直接压到飞船的 Dynamic Rigidbody2D 上。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SubmarineInteriorFollower2D : MonoBehaviour
{
    [Header("飞船本体")]
    [SerializeField]
    private Rigidbody2D submarineRigidbody;

    [Header("跟随设置")]
    [SerializeField]
    private bool followRotation = true;

    [Tooltip("自动记录内部碰撞代理与飞船之间的初始偏移。")]
    [SerializeField]
    private bool keepInitialOffset = true;

    [Header("舱内拾取物生成")]
    [Tooltip("随机生成区域在舱内代理本地坐标中的中心。")]
    [SerializeField]
    private Vector2 pickupSpawnLocalCenter = Vector2.zero;

    [Tooltip("随机生成区域的本地半径。当前圆形舱室建议保持在 0.42 左右。")]
    [Min(0.01f)]
    [SerializeField]
    private float pickupSpawnLocalRadius = 0.42f;

    [Tooltip("候选点周围需要留出的世界空间半径。")]
    [Min(0.01f)]
    [SerializeField]
    private float pickupSpawnClearance = 0.18f;

    [Min(1)]
    [SerializeField]
    private int pickupSpawnAttempts = 24;

    [Tooltip("留空时自动避开玩家、地板、舱壁、梯子和已有 Pickup。")]
    [SerializeField]
    private LayerMask pickupSpawnBlockingLayers;

    private Rigidbody2D interiorRigidbody;

    private Vector2 localPositionOffset;
    private float rotationOffset;

    private void Awake()
    {
        interiorRigidbody =
            GetComponent<Rigidbody2D>();

        interiorRigidbody.bodyType =
            RigidbodyType2D.Kinematic;

        interiorRigidbody.gravityScale = 0f;
        interiorRigidbody.simulated = true;
        interiorRigidbody.freezeRotation = true;
    }

    private void Start()
    {
        if (submarineRigidbody == null)
        {
            Debug.LogError(
                $"{name} 没有设置飞船 Rigidbody2D。",
                this
            );

            enabled = false;
            return;
        }

        if (keepInitialOffset)
        {
            localPositionOffset =
                submarineRigidbody.transform
                    .InverseTransformPoint(
                        interiorRigidbody.position
                    );

            rotationOffset =
                interiorRigidbody.rotation -
                submarineRigidbody.rotation;
        }
        else
        {
            localPositionOffset = Vector2.zero;
            rotationOffset = 0f;
        }
    }

    private void FixedUpdate()
    {
        FollowSubmarine();
    }

    private void FollowSubmarine()
    {
        if (submarineRigidbody == null)
        {
            return;
        }

        Vector2 targetPosition =
            submarineRigidbody.transform
                .TransformPoint(
                    localPositionOffset
                );

        interiorRigidbody.MovePosition(
            targetPosition
        );

        if (followRotation)
        {
            interiorRigidbody.MoveRotation(
                submarineRigidbody.rotation +
                rotationOffset
            );
        }
    }

    /// <summary>
    /// 获取飞船在指定世界坐标点上的速度，包含平移速度和旋转切向速度。
    /// </summary>
    public Vector2 GetVelocityAtPoint(
        Vector2 worldPoint)
    {
        if (submarineRigidbody == null)
        {
            return Vector2.zero;
        }

        return submarineRigidbody.GetPointVelocity(
            worldPoint
        );
    }

    public bool TryGetRandomEmptyPickupPosition(
        out Vector2 worldPosition)
    {
        int blockingMask = pickupSpawnBlockingLayers.value;

        if (blockingMask == 0)
        {
            blockingMask = LayerMask.GetMask(
                "Player",
                "Floor",
                "Ladder",
                "SubmarineInterior",
                "Pickup"
            );
        }

        int attempts = Mathf.Max(1, pickupSpawnAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector2 localPoint =
                pickupSpawnLocalCenter +
                Random.insideUnitCircle * pickupSpawnLocalRadius;

            Vector2 candidate =
                transform.TransformPoint(localPoint);

            Collider2D blockingCollider =
                Physics2D.OverlapCircle(
                    candidate,
                    pickupSpawnClearance,
                    blockingMask
                );

            if (blockingCollider == null)
            {
                worldPosition = candidate;
                return true;
            }
        }

        worldPosition =
            transform.TransformPoint(pickupSpawnLocalCenter);

        return false;
    }
}
