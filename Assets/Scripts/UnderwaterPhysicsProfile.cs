using UnityEngine;

[CreateAssetMenu(
    fileName = "UnderwaterPhysicsProfile",
    menuName = "Physics 2D/Underwater Physics Profile"
)]
public class UnderwaterPhysicsProfile : ScriptableObject
{
    [Header("刚体参数")]
    [Min(0.01f)]
    public float mass = 2f;

    [Tooltip("水阻越大，物体停得越快")]
    [Min(0f)]
    public float linearDamping = 1.2f;

    [Tooltip("最大移动速度")]
    [Min(0f)]
    public float maxSpeed = 6f;

    [Header("碰撞")]
    public PhysicsMaterial2D collisionMaterial;

    [Header("物理设置")]
    public RigidbodyInterpolation2D interpolation =
        RigidbodyInterpolation2D.Interpolate;

    public CollisionDetectionMode2D collisionDetection =
        CollisionDetectionMode2D.Continuous;
}