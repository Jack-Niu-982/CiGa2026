using UnityEngine;

/// <summary>
/// 给 Rigidbody2D 应用统一的水下物理配置：
/// 无重力、水阻、固定旋转、最大速度以及碰撞材质。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class UnderwaterPhysicsBody2D : MonoBehaviour
{
    [Header("水下物理配置")]
    [Tooltip("拖入创建好的 UnderwaterPhysicsProfile 配置文件")]
    [SerializeField]
    private UnderwaterPhysicsProfile profile;

    private Rigidbody2D rb;

    /// <summary>
    /// 提供给其他脚本读取当前 Rigidbody2D。
    /// </summary>
    public Rigidbody2D Rigidbody
    {
        get
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            return rb;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyProfile();
    }

    private void FixedUpdate()
    {
        LimitSpeed();
    }

    /// <summary>
    /// 把 Profile 中的参数应用到当前 Rigidbody2D。
    /// </summary>
    public void ApplyProfile()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (profile == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " 没有设置 UnderwaterPhysicsProfile。",
                this
            );

            return;
        }

        // 使用动态刚体
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 取消重力
        rb.gravityScale = 0f;

        // 质量
        rb.mass = profile.mass;

        // 旧版 Unity 使用 drag 模拟线性阻力
        rb.drag = profile.linearDamping;

        // 完全禁止物理系统改变物体旋转
        rb.freezeRotation = true;

        // 让运动显示更加平滑
        rb.interpolation = profile.interpolation;

        // 降低高速移动时穿过碰撞体的概率
        rb.collisionDetectionMode = profile.collisionDetection;

        ApplyCollisionMaterial();
    }

    /// <summary>
    /// 把 Profile 中的 Physics Material 2D
    /// 应用到自身和所有子物体的 Collider2D。
    /// </summary>
    private void ApplyCollisionMaterial()
    {
        if (profile == null)
        {
            return;
        }

        if (profile.collisionMaterial == null)
        {
            return;
        }

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D currentCollider in colliders)
        {
            currentCollider.sharedMaterial =
                profile.collisionMaterial;
        }
    }

    /// <summary>
    /// 限制物体的最大移动速度。
    /// </summary>
    private void LimitSpeed()
    {
        if (rb == null)
        {
            return;
        }

        if (profile == null)
        {
            return;
        }

        if (profile.maxSpeed <= 0f)
        {
            return;
        }

        float maxSpeedSquared =
            profile.maxSpeed * profile.maxSpeed;

        if (rb.velocity.sqrMagnitude > maxSpeedSquared)
        {
            rb.velocity =
                rb.velocity.normalized *
                profile.maxSpeed;
        }
    }

    /// <summary>
    /// 施加持续作用的力。
    /// 适合推进器、船锚持续拉动等效果。
    /// </summary>
    public void AddForce(Vector2 force)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.AddForce(
            force,
            ForceMode2D.Force
        );
    }

    /// <summary>
    /// 按指定方向施加持续推力。
    /// direction 不需要提前归一化。
    /// </summary>
    public void AddForce(Vector2 direction, float forceAmount)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        AddForce(
            direction.normalized * forceAmount
        );
    }

    /// <summary>
    /// 施加一次性的瞬间冲击力。
    /// 适合爆炸、撞击或船锚突然绷紧。
    /// </summary>
    public void AddImpulse(Vector2 impulse)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.AddForce(
            impulse,
            ForceMode2D.Impulse
        );
    }

    /// <summary>
    /// 按指定方向施加瞬间冲击力。
    /// </summary>
    public void AddImpulse(
        Vector2 direction,
        float impulseAmount
    )
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        AddImpulse(
            direction.normalized * impulseAmount
        );
    }

    /// <summary>
    /// 立即停止物体的移动。
    /// 适合重新开始关卡或重置位置。
    /// </summary>
    public void StopImmediately()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>
    /// 更换水下物理配置，并立即应用。
    /// </summary>
    public void SetProfile(
        UnderwaterPhysicsProfile newProfile
    )
    {
        profile = newProfile;
        ApplyProfile();
    }

#if UNITY_EDITOR

    /// <summary>
    /// 在 Inspector 中更换 Profile 时，
    /// 自动预览并应用参数。
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        rb = GetComponent<Rigidbody2D>();

        if (rb == null || profile == null)
        {
            return;
        }

        ApplyProfile();
    }

#endif
}