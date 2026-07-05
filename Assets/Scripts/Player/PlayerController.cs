using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 横版2D玩家控制器。
///
/// 功能：
/// 1. 左右移动。
/// 2. 普通状态按上跳跃。
/// 3. 梯子附近按上或按下进入攀爬。
/// 4. 攀爬时上下移动。
/// 5. 攀爬时长按左或右退出。
/// 6. 攀爬时关闭玩家主体Collider。
/// 7. 到达梯子上下边缘后停止，不会跳动。
/// 8. 攀爬时脚下检测到SubmarineInterior后自动落地。
/// 9. 继承潜艇内部地板的移动与旋转速度。
/// 10. 交互时锁定上下左右移动。
/// 11. 交互时长按左或右退出。
/// 12. 预留Animator参数。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public partial class PlayerController : MonoBehaviour
{
    [Header("输入")]
    [Tooltip("拖入KeyboardPlayerInput或GamepadPlayerInput。")]
    [SerializeField] private PlayerInputBase playerInput;

    [Header("基础组件")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("玩家正常移动使用的主体Collider。")]
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private Animator animator;

    [Tooltip("用于根据行走方向水平翻转玩家外观。")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("跳跃")]
    [Tooltip("放在玩家脚底的空物体。")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("普通状态下可以站立的所有地面图层。")]
    [SerializeField] private LayerMask groundLayer;

    [Header("梯子检测")]
    [Tooltip("放在玩家身体中心的空物体。")]
    [SerializeField] private Transform ladderCheck;

    [Tooltip("只选择Ladder图层。")]
    [SerializeField] private LayerMask ladderLayer;

    [Header("攀爬落地检测")]
    [Tooltip("放在玩家脚底，用于攀爬时检测内部平台。")]
    [SerializeField] private Transform ladderLandingCheck;

    [Tooltip("这里只选择SubmarineInterior图层。")]
    [SerializeField] private LayerMask submarineInteriorLayer;

    [Header("交互状态")]
    [Tooltip("通常会自动获取玩家身上的PlayerOperateInteractor2D。")]
    [SerializeField] private PlayerOperateInteractor2D operateInteractor;

    [Header("Animator参数")]
    [Tooltip("Animator中没有对应参数时会自动忽略。")]
    [SerializeField] private string moveXParameter = "MoveX";

    [SerializeField] private string moveSpeedParameter = "MoveSpeed";
    [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
    [SerializeField] private string groundedParameter = "IsGrounded";
    [SerializeField] private string climbingParameter = "IsClimbing";
    [SerializeField] private string operatingParameter = "IsOperating";
    [SerializeField] private string jumpTriggerParameter = "Jump";

    private readonly HashSet<int> animatorParameterHashes =
        new HashSet<int>();

    private readonly Collider2D[] ladderResults =
        new Collider2D[16];

    private readonly Collider2D[] landingResults =
        new Collider2D[16];

    private Collider2D detectedLadder;
    private Collider2D activeLadder;
    private Collider2D currentGroundCollider;

    private Collider2D ignoredEntryInteriorSurface;
    private OperateController activeOperateController;

    private Vector2 bodyCenterOffset;
    private Vector2 cachedBodyBoundsExtents;

    private float horizontalInput;
    private float verticalInput;
    private float normalGravityScale;
    private float ladderExitTimer;

    private bool isGrounded;
    private bool isClimbing;
    private bool isOperating;
    private bool jumpQueued;

    public bool IsGrounded => isGrounded;
    public bool IsClimbing => isClimbing;
    public bool IsOperating => isOperating;
    public Collider2D CurrentLadder => activeLadder;
    public OperateController CurrentOperateController =>
        activeOperateController;
    public PlayerInputBase CurrentInput => playerInput;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        operateInteractor =
            GetComponent<PlayerOperateInteractor2D>();
    }

    private void Awake()
    {
        PlayerSettings settings =
            SettingManager.Player;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (operateInteractor == null)
        {
            operateInteractor =
                GetComponent<PlayerOperateInteractor2D>();
        }

        normalGravityScale =
            rb.gravityScale;

        if (settings != null && settings.freezeRotation)
        {
            rb.freezeRotation = true;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;

            bodyCenterOffset =
                (Vector2)bodyCollider.bounds.center -
                rb.position;

            cachedBodyBoundsExtents =
                bodyCollider.bounds.extents;
        }

        CacheAnimatorParameters();
    }

    private void Update()
    {
        ReadInput();

        if (!isClimbing &&
            !isOperating)
        {
            UpdateNearbyLadder();
        }
        else if (isOperating)
        {
            detectedLadder = null;
        }

        UpdateGroundedState();
        UpdateMovementState();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isOperating)
        {
            ApplyOperatingMovement();
        }
        else if (isClimbing)
        {
            ApplyClimbingMovement();
        }
        else
        {
            ApplyNormalMovement();
        }
    }

    private void ReadInput()
    {
        if (playerInput == null)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            return;
        }

        horizontalInput =
            playerInput.Horizontal;

        verticalInput =
            playerInput.Vertical;
    }
}
