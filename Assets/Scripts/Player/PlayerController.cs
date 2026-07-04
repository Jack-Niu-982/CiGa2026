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
public class PlayerController : MonoBehaviour
{
    [Header("输入")]
    [Tooltip("拖入KeyboardPlayerInput或GamepadPlayerInput。")]
    [SerializeField] private PlayerInputBase playerInput;

    [Header("基础组件")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("玩家正常移动使用的主体Collider。")]
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private Animator animator;

    [Header("水平移动")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 6f;

    [Header("跳跃")]
    [Min(0f)]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("放在玩家脚底的空物体。")]
    [SerializeField] private Transform groundCheck;

    [Min(0.01f)]
    [SerializeField] private float groundCheckRadius = 0.08f;

    [Tooltip("普通状态下可以站立的所有地面图层。")]
    [SerializeField] private LayerMask groundLayer;

    [Header("梯子检测")]
    [Tooltip("放在玩家身体中心的空物体。")]
    [SerializeField] private Transform ladderCheck;

    [SerializeField]
    private Vector2 ladderCheckSize =
        new Vector2(0.8f, 1.4f);

    [Tooltip("只选择Ladder图层。")]
    [SerializeField] private LayerMask ladderLayer;

    [Header("梯子移动")]
    [Min(0f)]
    [SerializeField] private float climbSpeed = 4f;

    [Tooltip("攀爬时是否自动对齐梯子中心。")]
    [SerializeField] private bool snapToLadderCenter = true;

    [Min(0f)]
    [SerializeField] private float ladderSnapSpeed = 8f;

    [Tooltip("玩家与梯子上下边缘之间保留的距离。")]
    [Min(0f)]
    [SerializeField] private float ladderEndPadding = 0.02f;

    [Tooltip("攀爬时长按左或右多久后退出。")]
    [Min(0f)]
    [SerializeField] private float ladderExitHoldTime = 0.2f;

    [Tooltip("水平输入超过该值后开始计算退出时间。")]
    [Range(0f, 1f)]
    [SerializeField] private float ladderExitInputThreshold = 0.5f;

    [Header("攀爬落地检测")]
    [Tooltip("放在玩家脚底，用于攀爬时检测内部平台。")]
    [SerializeField] private Transform ladderLandingCheck;

    [SerializeField]
    private Vector2 ladderLandingCheckSize =
        new Vector2(0.45f, 0.12f);

    [Tooltip("这里只选择SubmarineInterior图层。")]
    [SerializeField] private LayerMask submarineInteriorLayer;

    [Header("攀爬Collider")]
    [Tooltip("进入攀爬后关闭玩家主体Collider。")]
    [SerializeField] private bool disableBodyColliderWhileClimbing = true;

    [Header("交互状态")]
    [Tooltip("通常会自动获取玩家身上的PlayerOperateInteractor2D。")]
    [SerializeField] private PlayerOperateInteractor2D operateInteractor;

    [Header("物理设置")]
    [SerializeField] private bool freezeRotation = true;

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
        operateInteractor =
            GetComponent<PlayerOperateInteractor2D>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (operateInteractor == null)
        {
            operateInteractor =
                GetComponent<PlayerOperateInteractor2D>();
        }

        normalGravityScale =
            rb.gravityScale;

        if (freezeRotation)
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

    #region Ground

    private void UpdateGroundedState()
    {
        if (isClimbing)
        {
            isGrounded = false;
            currentGroundCollider = null;
            return;
        }

        Vector2 checkPosition =
            GetGroundCheckPosition();

        currentGroundCollider =
            Physics2D.OverlapCircle(
                checkPosition,
                groundCheckRadius,
                groundLayer
            );

        isGrounded =
            currentGroundCollider != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null)
        {
            return groundCheck.position;
        }

        if (bodyCollider != null &&
            bodyCollider.enabled)
        {
            return new Vector2(
                bodyCollider.bounds.center.x,
                bodyCollider.bounds.min.y
            );
        }

        return rb.position;
    }

    private bool HasPhysicalGroundContact()
    {
        if (bodyCollider == null ||
            !bodyCollider.enabled ||
            currentGroundCollider == null)
        {
            return false;
        }

        return bodyCollider.IsTouching(
            currentGroundCollider
        );
    }

    #endregion

    #region Ladder Detection

    private void UpdateNearbyLadder()
    {
        Vector2 checkPosition =
            ladderCheck != null
                ? ladderCheck.position
                : transform.position;

        float checkAngle =
            ladderCheck != null
                ? ladderCheck.eulerAngles.z
                : transform.eulerAngles.z;

        int resultCount =
            Physics2D.OverlapBoxNonAlloc(
                checkPosition,
                ladderCheckSize,
                checkAngle,
                ladderResults,
                ladderLayer
            );

        if (resultCount <= 0)
        {
            detectedLadder = null;
            return;
        }

        Collider2D closestLadder = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < resultCount; i++)
        {
            Collider2D ladder =
                ladderResults[i];

            if (ladder == null)
            {
                continue;
            }

            Vector2 closestPoint =
                ladder.ClosestPoint(
                    rb.position
                );

            float distance =
                Vector2.SqrMagnitude(
                    closestPoint -
                    rb.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLadder = ladder;
            }
        }

        detectedLadder = closestLadder;
    }

    #endregion

    #region Input State

    private void UpdateMovementState()
    {
        if (playerInput == null)
        {
            return;
        }

        if (isOperating)
        {
            HandleOperatingStateInput();
        }
        else if (isClimbing)
        {
            HandleClimbingStateInput();
        }
        else
        {
            HandleNormalStateInput();
        }
    }

    private void HandleOperatingStateInput()
    {
        /*
         * 操作台被关闭、删除或强制退出时，
         * 自动恢复玩家正常状态。
         */
        if (activeOperateController == null ||
            !activeOperateController.IfInUse)
        {
            ExitOperatingState(
                activeOperateController
            );

            return;
        }

        /*
         * 交互状态下不在这里处理方向输入。
         *
         * 左右和上下输入仍然可以被锚发射器等设施读取，
         * 但不会用于玩家自身移动。
         *
         * 退出交互由PlayerOperateInteractor2D
         * 检测交互键完成。
         */
    }

    private void HandleNormalStateInput()
    {
        // 唯一新增的主要逻辑：
        // 梯子附近按上或按下都能进入攀爬。
        if (detectedLadder != null &&
            (
                playerInput.UpPressed ||
                playerInput.DownPressed
            ))
        {
            EnterClimbing();
            return;
        }

        // 没有进入梯子时，只有按上才会跳跃。
        if (isGrounded &&
            playerInput.UpPressed)
        {
            QueueJump();
        }
    }

    private void HandleClimbingStateInput()
    {
        if (activeLadder == null)
        {
            ExitClimbing();
            return;
        }

        HandleHorizontalLadderExit();

        if (!isClimbing)
        {
            return;
        }

        bool ignoredSurfaceStillDetected;

        Collider2D landingSurface =
            DetectClosestLandingSurface(
                ignoredEntryInteriorSurface,
                out ignoredSurfaceStillDetected
            );

        if (ignoredEntryInteriorSurface != null)
        {
            if (ignoredSurfaceStillDetected)
            {
                return;
            }

            ignoredEntryInteriorSurface = null;
        }

        if (landingSurface != null)
        {
            LandOnInteriorSurface(
                landingSurface
            );
        }
    }

    private void HandleHorizontalLadderExit()
    {
        if (Mathf.Abs(horizontalInput) >=
            ladderExitInputThreshold)
        {
            ladderExitTimer +=
                Time.deltaTime;

            if (ladderExitTimer >=
                ladderExitHoldTime)
            {
                ExitClimbing();
            }
        }
        else
        {
            ladderExitTimer = 0f;
        }
    }

    #endregion

    #region Normal Movement

    private void ApplyNormalMovement()
    {
        Vector2 platformVelocity =
            Vector2.zero;

        if (isGrounded)
        {
            platformVelocity =
                GetColliderMotionVelocity(
                    currentGroundCollider
                );
        }

        Vector2 velocity =
            rb.velocity;

        velocity.x =
            platformVelocity.x +
            horizontalInput * moveSpeed;

        if (jumpQueued)
        {
            velocity.y =
                platformVelocity.y +
                jumpForce;

            jumpQueued = false;
            isGrounded = false;
            currentGroundCollider = null;

            SetAnimatorTrigger(
                jumpTriggerParameter
            );

            OnJumped();
        }
        else if (
            isGrounded &&
            HasPhysicalGroundContact()
        )
        {
            velocity.y =
                platformVelocity.y;
        }

        rb.velocity = velocity;
    }

    private void QueueJump()
    {
        jumpQueued = true;
    }

    #endregion

    #region Climbing Movement

    private void ApplyClimbingMovement()
    {
        if (activeLadder == null)
        {
            ExitClimbing();
            return;
        }

        if (!TryGetLadderGeometry(
                activeLadder,
                out Vector2 ladderCenter,
                out Vector2 ladderUp,
                out Vector2 ladderRight,
                out float ladderHalfLength))
        {
            rb.velocity =
                GetColliderMotionVelocity(
                    activeLadder
                );

            return;
        }

        Vector2 ladderVelocity =
            GetColliderMotionVelocity(
                activeLadder
            );

        Vector2 playerBodyCenter =
            GetPlayerBodyCenter();

        Vector2 centerDifference =
            playerBodyCenter -
            ladderCenter;

        float currentVerticalDistance =
            Vector2.Dot(
                centerDifference,
                ladderUp
            );

        float currentHorizontalDistance =
            Vector2.Dot(
                centerDifference,
                ladderRight
            );

        float playerHalfExtentOnLadder =
            GetPlayerHalfExtentAlongAxis(
                ladderUp
            );

        float allowedHalfDistance =
            Mathf.Max(
                0f,
                ladderHalfLength -
                playerHalfExtentOnLadder -
                ladderEndPadding
            );

        float wantedClimbSpeed =
            verticalInput *
            climbSpeed;

        float wantedVerticalDistance =
            currentVerticalDistance +
            wantedClimbSpeed *
            Time.fixedDeltaTime;

        float clampedVerticalDistance =
            Mathf.Clamp(
                wantedVerticalDistance,
                -allowedHalfDistance,
                allowedHalfDistance
            );

        float actualClimbSpeed =
            (
                clampedVerticalDistance -
                currentVerticalDistance
            ) /
            Time.fixedDeltaTime;

        float horizontalCorrectionSpeed = 0f;

        if (snapToLadderCenter)
        {
            float nextHorizontalDistance =
                Mathf.MoveTowards(
                    currentHorizontalDistance,
                    0f,
                    ladderSnapSpeed *
                    Time.fixedDeltaTime
                );

            horizontalCorrectionSpeed =
                (
                    nextHorizontalDistance -
                    currentHorizontalDistance
                ) /
                Time.fixedDeltaTime;
        }

        Vector2 relativeClimbVelocity =
            ladderUp *
            actualClimbSpeed +
            ladderRight *
            horizontalCorrectionSpeed;

        rb.velocity =
            ladderVelocity +
            relativeClimbVelocity;
    }

    private Vector2 GetPlayerBodyCenter()
    {
        return rb.position +
               bodyCenterOffset;
    }

    private float GetPlayerHalfExtentAlongAxis(
        Vector2 axis)
    {
        axis = new Vector2(
            Mathf.Abs(axis.x),
            Mathf.Abs(axis.y)
        );

        return
            axis.x *
            cachedBodyBoundsExtents.x +
            axis.y *
            cachedBodyBoundsExtents.y;
    }

    private bool TryGetLadderGeometry(
        Collider2D ladder,
        out Vector2 center,
        out Vector2 up,
        out Vector2 right,
        out float halfLength)
    {
        center =
            ladder.bounds.center;

        up =
            ladder.transform.up.normalized;

        right =
            ladder.transform.right.normalized;

        halfLength = 0f;

        if (ladder is BoxCollider2D boxCollider)
        {
            center =
                boxCollider.transform
                    .TransformPoint(
                        boxCollider.offset
                    );

            float worldHeight =
                Mathf.Abs(
                    boxCollider.size.y *
                    boxCollider.transform
                        .lossyScale.y
                );

            halfLength =
                worldHeight * 0.5f;

            return halfLength > 0f;
        }

        Bounds ladderBounds =
            ladder.bounds;

        halfLength =
            Mathf.Abs(up.x) *
            ladderBounds.extents.x +
            Mathf.Abs(up.y) *
            ladderBounds.extents.y;

        return halfLength > 0f;
    }

    #endregion

    #region Climbing Landing

    private Collider2D DetectClosestLandingSurface(
        Collider2D colliderToIgnore,
        out bool ignoredColliderWasDetected)
    {
        ignoredColliderWasDetected = false;

        Vector2 checkPosition =
            ladderLandingCheck != null
                ? ladderLandingCheck.position
                : GetGroundCheckPosition();

        float checkAngle =
            ladderLandingCheck != null
                ? ladderLandingCheck.eulerAngles.z
                : transform.eulerAngles.z;

        int resultCount =
            Physics2D.OverlapBoxNonAlloc(
                checkPosition,
                ladderLandingCheckSize,
                checkAngle,
                landingResults,
                submarineInteriorLayer
            );

        Collider2D closestSurface = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < resultCount; i++)
        {
            Collider2D surface =
                landingResults[i];

            if (surface == null)
            {
                continue;
            }

            if (surface == colliderToIgnore)
            {
                ignoredColliderWasDetected = true;
                continue;
            }

            Vector2 closestPoint =
                surface.ClosestPoint(
                    checkPosition
                );

            float distance =
                Vector2.SqrMagnitude(
                    closestPoint -
                    checkPosition
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSurface = surface;
            }
        }

        return closestSurface;
    }

    private void LandOnInteriorSurface(
        Collider2D landingSurface)
    {
        if (!isClimbing ||
            landingSurface == null)
        {
            return;
        }

        Vector2 surfaceVelocity =
            GetColliderMotionVelocity(
                landingSurface
            );

        isClimbing = false;
        isGrounded = true;

        jumpQueued = false;
        ladderExitTimer = 0f;

        rb.gravityScale =
            normalGravityScale;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        activeLadder = null;
        detectedLadder = null;
        ignoredEntryInteriorSurface = null;

        currentGroundCollider =
            landingSurface;

        rb.velocity =
            surfaceVelocity;

        OnExitedClimbing();
    }

    #endregion

    #region Climbing State

    private void EnterClimbing()
    {
        if (isClimbing ||
            isOperating ||
            detectedLadder == null)
        {
            return;
        }

        activeLadder =
            detectedLadder;

        isClimbing = true;
        isGrounded = false;

        jumpQueued = false;
        ladderExitTimer = 0f;

        currentGroundCollider = null;

        rb.gravityScale = 0f;

        rb.velocity =
            GetColliderMotionVelocity(
                activeLadder
            );

        if (disableBodyColliderWhileClimbing &&
            bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        bool unused;

        ignoredEntryInteriorSurface =
            DetectClosestLandingSurface(
                null,
                out unused
            );

        OnEnteredClimbing();
    }

    private void ExitClimbing()
    {
        if (!isClimbing)
        {
            return;
        }

        isClimbing = false;
        ladderExitTimer = 0f;

        rb.gravityScale =
            normalGravityScale;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        activeLadder = null;
        detectedLadder = null;
        ignoredEntryInteriorSurface = null;

        OnExitedClimbing();
    }

    #endregion

    #region Operating State

    /// <summary>
    /// 由PlayerOperateInteractor2D在成功开始交互时调用。
    /// </summary>
    public void EnterOperatingState(
        OperateController operateController)
    {
        if (operateController == null)
        {
            return;
        }

        if (isClimbing)
        {
            ExitClimbing();
        }

        activeOperateController =
            operateController;

        isOperating = true;
        jumpQueued = false;

        detectedLadder = null;

        horizontalInput = 0f;
        verticalInput = 0f;

        OnEnteredOperating();
    }

    /// <summary>
    /// 由PlayerOperateInteractor2D在交互结束时调用。
    /// </summary>
    public void ExitOperatingState(
        OperateController operateController)
    {
        if (!isOperating)
        {
            return;
        }

        if (operateController != null &&
            activeOperateController != null &&
            operateController !=
            activeOperateController)
        {
            return;
        }

        isOperating = false;
        activeOperateController = null;

        horizontalInput = 0f;
        verticalInput = 0f;

        OnExitedOperating();
    }

    /// <summary>
    /// 交互状态下不响应任何方向移动。
    /// 仍然继承脚下平台的移动速度，避免潜艇移动时玩家滑开。
    /// </summary>
    private void ApplyOperatingMovement()
    {
        Vector2 velocity =
            rb.velocity;

        if (isGrounded)
        {
            Vector2 platformVelocity =
                GetColliderMotionVelocity(
                    currentGroundCollider
                );

            velocity.x =
                platformVelocity.x;

            if (HasPhysicalGroundContact())
            {
                velocity.y =
                    platformVelocity.y;
            }
        }
        else
        {
            velocity.x = 0f;
        }

        rb.velocity = velocity;
    }

    #endregion

    #region Moving Platform

    private Vector2 GetColliderMotionVelocity(
        Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return Vector2.zero;
        }

        SubmarineInteriorFollower2D follower =
            targetCollider
                .GetComponentInParent
                <SubmarineInteriorFollower2D>();

        if (follower != null)
        {
            return follower.GetVelocityAtPoint(
                rb.position
            );
        }

        Rigidbody2D targetRigidbody =
            targetCollider.attachedRigidbody;

        if (targetRigidbody != null)
        {
            return targetRigidbody
                .GetPointVelocity(
                    rb.position
                );
        }

        return Vector2.zero;
    }

    #endregion

    #region Input Assignment

    public void SetPlayerInput(
        PlayerInputBase newInput)
    {
        playerInput = newInput;
    }

    #endregion

    #region Animator

    private void CacheAnimatorParameters()
    {
        animatorParameterHashes.Clear();

        if (animator == null)
        {
            return;
        }

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters)
        {
            animatorParameterHashes.Add(
                parameter.nameHash
            );
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float animatorHorizontalInput =
            isOperating
                ? 0f
                : horizontalInput;

        SetAnimatorFloat(
            moveXParameter,
            animatorHorizontalInput
        );

        SetAnimatorFloat(
            moveSpeedParameter,
            Mathf.Abs(
                animatorHorizontalInput
            )
        );

        SetAnimatorFloat(
            verticalSpeedParameter,
            rb.velocity.y
        );

        SetAnimatorBool(
            groundedParameter,
            isGrounded &&
            !isClimbing
        );

        SetAnimatorBool(
            climbingParameter,
            isClimbing
        );

        SetAnimatorBool(
            operatingParameter,
            isOperating
        );
    }

    private void SetAnimatorFloat(
        string parameterName,
        float value)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetFloat(
            hash,
            value
        );
    }

    private void SetAnimatorBool(
        string parameterName,
        bool value)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetBool(
            hash,
            value
        );
    }

    private void SetAnimatorTrigger(
        string parameterName)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetTrigger(hash);
    }

    private bool TryGetAnimatorParameterHash(
        string parameterName,
        out int hash)
    {
        hash = 0;

        if (animator == null ||
            string.IsNullOrWhiteSpace(
                parameterName))
        {
            return false;
        }

        hash =
            Animator.StringToHash(
                parameterName
            );

        return animatorParameterHashes.Contains(
            hash
        );
    }

    #endregion

    #region Animation Extension Points

    protected virtual void OnJumped()
    {
    }

    protected virtual void OnEnteredClimbing()
    {
    }

    protected virtual void OnExitedClimbing()
    {
    }

    protected virtual void OnEnteredOperating()
    {
    }

    protected virtual void OnExitedOperating()
    {
    }

    #endregion

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.gravityScale =
                normalGravityScale;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        isClimbing = false;
        isOperating = false;
        isGrounded = false;
        jumpQueued = false;

        activeLadder = null;
        activeOperateController = null;
        detectedLadder = null;
        currentGroundCollider = null;
        ignoredEntryInteriorSurface = null;

        ladderExitTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 groundPosition =
            groundCheck != null
                ? groundCheck.position
                : transform.position;

        Gizmos.DrawWireSphere(
            groundPosition,
            groundCheckRadius
        );

        DrawWireBox(
            ladderCheck != null
                ? ladderCheck.position
                : transform.position,
            ladderCheck != null
                ? ladderCheck.eulerAngles.z
                : transform.eulerAngles.z,
            ladderCheckSize
        );

        DrawWireBox(
            ladderLandingCheck != null
                ? ladderLandingCheck.position
                : groundPosition,
            ladderLandingCheck != null
                ? ladderLandingCheck.eulerAngles.z
                : transform.eulerAngles.z,
            ladderLandingCheckSize
        );
    }

    private void DrawWireBox(
        Vector3 position,
        float angle,
        Vector2 size)
    {
        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                position,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                ),
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            size
        );

        Gizmos.matrix =
            oldMatrix;
    }
}