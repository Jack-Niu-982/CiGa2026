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
}
