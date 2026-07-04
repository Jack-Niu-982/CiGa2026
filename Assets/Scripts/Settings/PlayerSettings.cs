using UnityEngine;

/// <summary>
/// 玩家移动、跳跃、攀爬配置。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerSettings",
    menuName = "Settings/Player Settings")]
public class PlayerSettings : ScriptableObject
{
    [Header("移动")]
    [Tooltip("水平移动速度（units/s）。")]
    [Min(0f)]
    public float moveSpeed = 6f;

    [Tooltip("跳跃力度。")]
    [Min(0f)]
    public float jumpForce = 12f;

    [Header("地面检测")]
    [Tooltip("地面检测半径。")]
    [Min(0.01f)]
    public float groundCheckRadius = 0.08f;

    [Header("攀爬")]
    [Tooltip("攀爬速度（units/s）。")]
    [Min(0f)]
    public float climbSpeed = 4f;

    [Tooltip("吸附到梯子中心。")]
    public bool snapToLadderCenter = true;

    [Tooltip("吸附速度。")]
    [Min(0f)]
    public float ladderSnapSpeed = 8f;

    [Tooltip("梯子检测区域大小。")]
    public Vector2 ladderCheckSize = new Vector2(0.8f, 1.4f);

    [Tooltip("梯子端点留出的缓冲（防止卡在顶部/底部）。")]
    [Min(0f)]
    public float ladderEndPadding = 0.02f;

    [Tooltip("玩家按住退出方向多久后离开梯子。")]
    [Min(0f)]
    public float ladderExitHoldTime = 0.2f;

    [Tooltip("触发梯子退出的输入阈值。")]
    [Range(0f, 1f)]
    public float ladderExitInputThreshold = 0.5f;

    [Tooltip("梯子着陆检测区域大小。")]
    public Vector2 ladderLandingCheckSize = new Vector2(0.45f, 0.12f);

    [Tooltip("攀爬时禁用身体碰撞体。")]
    public bool disableBodyColliderWhileClimbing = true;

    [Header("其他")]
    [Tooltip("冻结旋转。")]
    public bool freezeRotation = true;
}
