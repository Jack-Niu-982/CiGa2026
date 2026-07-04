using UnityEngine;

/// <summary>
/// 负责船锚输入和船锚参数配置。
///
/// 该组件可以被 OperateController 启用或禁用。
///
/// 船锚飞行、自动收回、绳子刷新和持续拉力，
/// 全部由 AnchorRopeRuntime2D 常驻处理。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AnchorLaunchDetector2D))]
[RequireComponent(typeof(AnchorRopeRuntime2D))]
public class AnchorLauncher2D : MonoBehaviour
{
    public enum AnchorDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    [Header("船锚方向")]

    [Tooltip("该船锚发射器的基础方向。")]
    [SerializeField]
    private AnchorDirection anchorDirection =
        AnchorDirection.Up;

    [Header("玩家输入")]

    [Tooltip(
        "当前正在操作该发射器的玩家输入。\n" +
        "多人模式下由 AnchorLauncherUseController2D 自动设置。"
    )]
    [SerializeField]
    private PlayerInputBase currentPlayerInput;

    [Tooltip(
        "仅在场景中只有一个启用的 PlayerInputBase 时自动绑定。\n" +
        "多人模式下不会自动选择玩家。"
    )]
    [SerializeField]
    private bool autoFindPlayerInput = true;

    [Tooltip(
        "一旦该发射器被交互系统明确绑定过玩家，" +
        "之后即使解绑，也不会再自动抢占其他玩家的输入。"
    )]
    [SerializeField]
    private bool keepExplicitBindingMode = true;

    [Header("船锚引用")]

    [Tooltip(
        "船锚收回状态下的物体。\n" +
        "留空时会自动寻找名字中包含 Anchor 或“锚”的子物体。"
    )]
    [SerializeField]
    private Transform anchorReference;

    [Header("发射检测")]

    [Tooltip("负责检测船锚飞行过程中是否命中目标。")]
    [SerializeField]
    private AnchorLaunchDetector2D launchDetector;

    [Tooltip(
        "最大绳长，" +
        "同时也是未命中时的最大飞行距离。"
    )]
    [Min(0.1f)]
    [SerializeField]
    private float maxRopeLength = 15f;

    [Header("船锚设置")]

    [Tooltip("待机状态下，船锚距离发射器中心的距离。")]
    [Min(0f)]
    [SerializeField]
    private float anchorOffset = 0.5f;

    [Tooltip("船锚发射速度。")]
    [Min(0f)]
    [SerializeField]
    private float anchorShootSpeed = 25f;

    [Tooltip("船锚收回速度。")]
    [Min(0.01f)]
    [SerializeField]
    private float anchorRetractSpeed = 18f;

    [Header("永久自然拉力")]

    [Tooltip("船锚固定后持续提供的拉力加速度。")]
    [Min(0f)]
    [SerializeField]
    private float passivePullAcceleration = 0.8f;

    [Tooltip(
        "自然拉力允许潜艇达到的" +
        "最大朝向锚点速度。"
    )]
    [Min(0f)]
    [SerializeField]
    private float passiveMaxPullSpeed = 1.5f;

    [Header("玩家主动拉回")]

    [Tooltip(
        "玩家主动收紧绳子时，" +
        "额外增加的拉力加速度。"
    )]
    [Min(0f)]
    [SerializeField]
    private float reelPullAcceleration = 12f;

    [Tooltip(
        "主动拉回时允许达到的" +
        "最大朝锚点速度。"
    )]
    [Min(0f)]
    [SerializeField]
    private float maxReelSpeed = 8f;

    [Tooltip(
        "距离锚点小于该值后，" +
        "停止施加拉力。"
    )]
    [Min(0f)]
    [SerializeField]
    private float stopPullDistance = 0.15f;

    [Header("绳子宽度")]

    [Tooltip("开启后，绳子越长越细。")]
    [SerializeField]
    private bool inverseWidthByLength = true;

    [Tooltip("参考长度下的绳子宽度。")]
    [Min(0.001f)]
    [SerializeField]
    private float ropeWidthAtReferenceLength = 0.07f;

    [Tooltip("用于计算绳子宽度的参考长度。")]
    [Min(0.01f)]
    [SerializeField]
    private float widthReferenceLength = 5f;

    [Tooltip("绳子最小宽度。")]
    [Min(0.001f)]
    [SerializeField]
    private float minimumRopeWidth = 0.025f;

    [Tooltip("绳子最大宽度。")]
    [Min(0.001f)]
    [SerializeField]
    private float maximumRopeWidth = 0.13f;

    [Header("绳子显示")]

    [Tooltip("绳子材质。留空时会自动创建。")]
    [SerializeField]
    private Material ropeMaterial;

    [SerializeField]
    private Color ropeColor = Color.white;

    [SerializeField]
    private string ropeSortingLayerName = "Default";

    [SerializeField]
    private int ropeOrderInLayer;

    [SerializeField]
    private bool autoConfigureLineRenderer = true;

    [Header("常驻运行组件")]

    [Tooltip(
        "负责绳子和物理逻辑的常驻组件，" +
        "不要禁用它。"
    )]
    [SerializeField]
    private AnchorRopeRuntime2D ropeRuntime;

    [Header("调试")]

    [SerializeField]
    private bool showDebugLog = true;

    /// <summary>
    /// 是否已经进入由交互系统明确指定玩家的模式。
    /// </summary>
    private bool hasUsedExplicitPlayerBinding;

    /// <summary>
    /// 船锚基础配置方向。
    /// </summary>
    public AnchorDirection Direction =>
        anchorDirection;

    /// <summary>
    /// 当前正在操作该发射器的玩家输入。
    /// </summary>
    public PlayerInputBase CurrentPlayerInput =>
        currentPlayerInput;

    /// <summary>
    /// 当前是否已经绑定了一个玩家输入。
    /// </summary>
    public bool HasPlayerInput =>
        currentPlayerInput != null;

    /// <summary>
    /// 判断当前发射器是否正由指定玩家控制。
    /// </summary>
    public bool IsControlledBy(
        PlayerInputBase playerInput)
    {
        return playerInput != null &&
               currentPlayerInput == playerInput;
    }

    /// <summary>
    /// 船锚收回状态下的参考物体。
    /// AnchorRotator 会从这里读取。
    /// </summary>
    public Transform AnchorReference
    {
        get
        {
            if (anchorReference == null)
            {
                anchorReference =
                    FindAnchorReference();
            }

            return anchorReference;
        }
    }

    public bool IsAnchorActive =>
        ropeRuntime != null &&
        ropeRuntime.IsAnchorActive;

    public bool IsAnchorAttached =>
        ropeRuntime != null &&
        ropeRuntime.IsAnchorAttached;

    public bool IsAnchorRetracting =>
        ropeRuntime != null &&
        ropeRuntime.IsAnchorRetracting;

    public Vector2 AnchorPoint =>
        ropeRuntime != null
            ? ropeRuntime.AnchorPoint
            : Vector2.zero;

    internal AnchorLaunchDetector2D LaunchDetector
    {
        get
        {
            if (launchDetector == null)
            {
                launchDetector =
                    GetComponent<AnchorLaunchDetector2D>();
            }

            return launchDetector;
        }
    }

    internal float MaxRopeLength =>
        maxRopeLength;

    internal float AnchorOffset =>
        anchorOffset;

    internal float AnchorShootSpeed =>
        anchorShootSpeed;

    internal float AnchorRetractSpeed =>
        anchorRetractSpeed;

    internal float PassivePullAcceleration =>
        passivePullAcceleration;

    internal float PassiveMaxPullSpeed =>
        passiveMaxPullSpeed;

    internal float ReelPullAcceleration =>
        reelPullAcceleration;

    internal float MaxReelSpeed =>
        maxReelSpeed;

    internal float StopPullDistance =>
        stopPullDistance;

    internal bool InverseWidthByLength =>
        inverseWidthByLength;

    internal float RopeWidthAtReferenceLength =>
        ropeWidthAtReferenceLength;

    internal float WidthReferenceLength =>
        widthReferenceLength;

    internal float MinimumRopeWidth =>
        minimumRopeWidth;

    internal float MaximumRopeWidth =>
        maximumRopeWidth;

    internal Material RopeMaterial =>
        ropeMaterial;

    internal Color RopeColor =>
        ropeColor;

    internal string RopeSortingLayerName =>
        ropeSortingLayerName;

    internal int RopeOrderInLayer =>
        ropeOrderInLayer;

    internal bool AutoConfigureLineRenderer =>
        autoConfigureLineRenderer;

    internal bool ShowDebugLog =>
        showDebugLog;

    private void Awake()
    {
        FindReferences();
        FindRuntime();
        FindPlayerInput();

        if (ropeRuntime != null)
        {
            ropeRuntime.Bind(this);
        }
    }

    private void OnEnable()
    {
        FindReferences();
        FindRuntime();
        FindPlayerInput();

        if (ropeRuntime != null)
        {
            ropeRuntime.Bind(this);
        }

        SetCurrentInputFeedback(true);

        LogCurrentInput();
    }

    private void Update()
    {
        if (ropeRuntime == null)
        {
            SetCurrentInputFeedback(false);
            return;
        }

        if (currentPlayerInput == null &&
            autoFindPlayerInput)
        {
            FindPlayerInput();
        }

        if (currentPlayerInput == null ||
            !currentPlayerInput.isActiveAndEnabled)
        {
            ropeRuntime.SetReelHeld(false);

            SetCurrentInputFeedback(false);

            return;
        }

        /*
         * 告诉对应玩家的手柄：
         * 该玩家正在操作船锚。
         *
         * 只有船锚已经固定时，
         * 才允许右摇杆绕圈收绳。
         */
        currentPlayerInput.SetAnchorControlFeedback(
            true,
            ropeRuntime.IsAnchorAttached
        );

        /*
         * Q / R1：发射船锚。
         */
        if (currentPlayerInput.AnchorShootPressed)
        {
            bool successfullyShot =
                ropeRuntime.TryShootAnchor();

            if (successfullyShot)
            {
                currentPlayerInput
                    .PlayAnchorShootFeedback();
            }
        }

        /*
         * R / South：
         * 松开锚点并开始回收船锚。
         */
        if (currentPlayerInput.AnchorRetractPressed)
        {
            bool hadActiveAnchor =
                ropeRuntime.IsAnchorActive;

            ropeRuntime.StartRetractingAnchor();

            if (hadActiveAnchor)
            {
                currentPlayerInput
                    .PlayAnchorRetractFeedback();
            }
        }

        /*
         * 键盘：
         * 按住 E。
         *
         * 手柄：
         * 顺时针持续转动右摇杆。
         */
        ropeRuntime.SetReelHeld(
            ropeRuntime.IsAnchorAttached &&
            currentPlayerInput.AnchorReelHeld
        );
    }

    private void OnDisable()
    {
        /*
         * 玩家退出操作时，
         * 只取消主动拉回和手柄震动。
         *
         * 船锚飞行、自动收回、绳子刷新和自然拉力，
         * 不会因为这个组件被禁用而停止。
         */
        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(false);
        }

        SetCurrentInputFeedback(false);
    }

    private void OnValidate()
    {
        maxRopeLength =
            Mathf.Max(
                0.1f,
                maxRopeLength
            );

        anchorOffset =
            Mathf.Max(
                0f,
                anchorOffset
            );

        anchorShootSpeed =
            Mathf.Max(
                0f,
                anchorShootSpeed
            );

        anchorRetractSpeed =
            Mathf.Max(
                0.01f,
                anchorRetractSpeed
            );

        passivePullAcceleration =
            Mathf.Max(
                0f,
                passivePullAcceleration
            );

        passiveMaxPullSpeed =
            Mathf.Max(
                0f,
                passiveMaxPullSpeed
            );

        reelPullAcceleration =
            Mathf.Max(
                0f,
                reelPullAcceleration
            );

        maxReelSpeed =
            Mathf.Max(
                0f,
                maxReelSpeed
            );

        stopPullDistance =
            Mathf.Max(
                0f,
                stopPullDistance
            );

        ropeWidthAtReferenceLength =
            Mathf.Max(
                0.001f,
                ropeWidthAtReferenceLength
            );

        widthReferenceLength =
            Mathf.Max(
                0.01f,
                widthReferenceLength
            );

        minimumRopeWidth =
            Mathf.Max(
                0.001f,
                minimumRopeWidth
            );

        maximumRopeWidth =
            Mathf.Max(
                minimumRopeWidth,
                maximumRopeWidth
            );

        FindReferences();

        if (ropeRuntime != null)
        {
            ropeRuntime.RefreshVisualConfiguration();
        }
    }

    /// <summary>
    /// 供其他系统直接调用船锚发射。
    /// </summary>
    public bool TryShootAnchor()
    {
        FindRuntime();

        bool successfullyShot =
            ropeRuntime != null &&
            ropeRuntime.TryShootAnchor();

        if (successfullyShot &&
            currentPlayerInput != null)
        {
            currentPlayerInput
                .PlayAnchorShootFeedback();
        }

        return successfullyShot;
    }

    /// <summary>
    /// 供其他系统直接调用船锚回收。
    /// </summary>
    public void StartRetractingAnchor()
    {
        FindRuntime();

        if (ropeRuntime == null)
        {
            return;
        }

        bool hadActiveAnchor =
            ropeRuntime.IsAnchorActive;

        ropeRuntime.StartRetractingAnchor();

        if (hadActiveAnchor &&
            currentPlayerInput != null)
        {
            currentPlayerInput
                .PlayAnchorRetractFeedback();
        }
    }

    /// <summary>
    /// 供其他系统设置是否正在主动拉回。
    /// </summary>
    public void SetReelHeld(bool held)
    {
        FindRuntime();

        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(held);
        }
    }

    /// <summary>
    /// 玩家开始操作时，
    /// 传入准确的玩家输入。
    /// </summary>
    public void SetPlayerInput(
        PlayerInputBase playerInput)
    {
        /*
         * 只要交互系统主动调用过一次，
         * 就说明当前游戏已经开始按玩家所有权分配输入。
         */
        hasUsedExplicitPlayerBinding = true;

        if (currentPlayerInput == playerInput)
        {
            SetCurrentInputFeedback(
                isActiveAndEnabled
            );

            return;
        }

        /*
         * 停止旧玩家手柄的震动。
         */
        if (currentPlayerInput != null)
        {
            currentPlayerInput
                .SetAnchorControlFeedback(
                    false,
                    false
                );
        }

        currentPlayerInput =
            playerInput;

        SetCurrentInputFeedback(
            isActiveAndEnabled
        );

        if (showDebugLog &&
            currentPlayerInput != null)
        {
            Debug.Log(
                $"[AnchorLauncher2D] {gameObject.name} 已绑定输入：" +
                currentPlayerInput.gameObject.name
            );
        }
    }

    /// <summary>
    /// 玩家结束操作时清除输入，
    /// 并停止主动拉回和震动。
    ///
    /// 如果传入 playerInput，
    /// 只有当前绑定的输入与其一致时才会清除。
    /// </summary>
    public void ClearPlayerInput(
        PlayerInputBase playerInput = null)
    {
        /*
         * 防止玩家一退出时，
         * 错误清除已经绑定给玩家二的输入。
         */
        if (playerInput != null &&
            currentPlayerInput != playerInput)
        {
            return;
        }

        if (currentPlayerInput != null)
        {
            currentPlayerInput
                .SetAnchorControlFeedback(
                    false,
                    false
                );
        }

        currentPlayerInput = null;

        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(false);
        }
    }

    private void SetCurrentInputFeedback(
        bool isControllingAnchor)
    {
        if (currentPlayerInput == null)
        {
            return;
        }

        currentPlayerInput
            .SetAnchorControlFeedback(
                isControllingAnchor,
                isControllingAnchor &&
                ropeRuntime != null &&
                ropeRuntime.IsAnchorAttached
            );
    }

    private void FindPlayerInput()
    {
        if (currentPlayerInput != null ||
            !autoFindPlayerInput)
        {
            return;
        }

        /*
         * 一旦交互系统明确绑定过玩家，
         * 解绑后也不能自动抓取场景中的其他玩家。
         */
        if (keepExplicitBindingMode &&
            hasUsedExplicitPlayerBinding)
        {
            return;
        }

        /*
         * 如果发射器本身就在玩家物体或其子物体下，
         * 可以安全地只查找自己的层级。
         */
        currentPlayerInput =
            GetComponent<PlayerInputBase>();

        if (currentPlayerInput == null)
        {
            currentPlayerInput =
                GetComponentInParent<PlayerInputBase>();
        }

        if (currentPlayerInput != null)
        {
            return;
        }

        /*
         * 单人模式兜底：
         *
         * 只有场景中恰好存在一个启用的 PlayerInputBase 时，
         * 才允许自动绑定。
         *
         * 只要检测到两个或更多玩家，
         * 就绝不擅自选择其中一个。
         */
        PlayerInputBase[] activePlayerInputs =
            FindObjectsOfType<PlayerInputBase>();

        if (activePlayerInputs.Length == 1)
        {
            currentPlayerInput =
                activePlayerInputs[0];

            return;
        }

        if (activePlayerInputs.Length > 1 &&
            showDebugLog)
        {
            Debug.Log(
                $"[AnchorLauncher2D] {gameObject.name} 检测到 " +
                $"{activePlayerInputs.Length} 个玩家输入，" +
                "不会自动绑定。等待交互控制器指定玩家。"
            );
        }
    }

    private void FindReferences()
    {
        if (anchorReference == null)
        {
            anchorReference =
                FindAnchorReference();
        }

        if (launchDetector == null)
        {
            launchDetector =
                GetComponent<AnchorLaunchDetector2D>();
        }

        if (ropeRuntime == null)
        {
            ropeRuntime =
                GetComponent<AnchorRopeRuntime2D>();
        }
    }

    private Transform FindAnchorReference()
    {
        Transform[] childTransforms =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0;
             i < childTransforms.Length;
             i++)
        {
            Transform child =
                childTransforms[i];

            if (child == transform)
            {
                continue;
            }

            string lowerName =
                child.name.ToLowerInvariant();

            if (lowerName.Contains("anchor") ||
                child.name.Contains("锚"))
            {
                return child;
            }
        }

        return null;
    }

    private void FindRuntime()
    {
        if (ropeRuntime == null)
        {
            ropeRuntime =
                GetComponent<AnchorRopeRuntime2D>();
        }

        if (ropeRuntime == null)
        {
            Debug.LogError(
                $"[AnchorLauncher2D] {gameObject.name} " +
                "缺少 AnchorRopeRuntime2D。"
            );
        }
    }

    private void LogCurrentInput()
    {
        if (!showDebugLog)
        {
            return;
        }

        string inputName =
            currentPlayerInput != null
                ? currentPlayerInput.gameObject.name
                : "尚未绑定玩家输入";

        Debug.Log(
            $"[AnchorLauncher2D] {gameObject.name} 输入组件已启用。\n" +
            $"方向：{anchorDirection}\n" +
            $"输入来源：{inputName}\n" +
            "键盘：Q 发射 / R 回收 / 按住 E 收绳\n" +
            "手柄：R1 发射 / South 回收 / 顺时针转右摇杆收绳"
        );
    }
}