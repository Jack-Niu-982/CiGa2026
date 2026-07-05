using UnityEngine;

/// <summary>
/// 负责船锚输入和参数配置。
/// 船锚实际飞行、命中墙壁后的延迟拉拽、自动回收和绳子显示，
/// 由常驻的 AnchorRopeRuntime2D 处理。
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
    [SerializeField] private AnchorDirection anchorDirection = AnchorDirection.Up;

    [Header("玩家输入")]
    [Tooltip("当前正在操作该发射器的玩家输入。多人模式下由 AnchorLauncherUseController2D 自动设置。")]
    [SerializeField] private PlayerInputBase currentPlayerInput;

    [Tooltip("仅在场景中只有一个启用的 PlayerInputBase 时自动绑定。多人模式下不会自动选择玩家。")]
    [SerializeField] private bool autoFindPlayerInput = true;

    [Tooltip("一旦交互系统明确绑定过玩家，之后即使解绑，也不会自动抢占其他玩家的输入。")]
    [SerializeField] private bool keepExplicitBindingMode = true;

    [Header("船锚引用")]
    [Tooltip("船锚物体。留空时自动寻找名字中包含 Anchor 或“锚”的子物体。")]
    [SerializeField] private Transform anchorReference;

    [Header("发射检测")]
    [Tooltip("负责检测船锚飞行过程中是否命中墙壁。")]
    [SerializeField] private AnchorLaunchDetector2D launchDetector;

    [Tooltip("最大绳长，同时也是未命中墙壁时的最大飞行距离。")]
    [Min(0.1f)]
    [SerializeField] private float maxRopeLength = 15f;

    [Header("船锚移动")]
    [Tooltip("待机状态下，船锚距离发射器中心的距离。")]
    [Min(0f)]
    [SerializeField] private float anchorOffset = 0.5f;

    [Tooltip("船锚发射速度。")]
    [Min(0f)]
    [SerializeField] private float anchorShootSpeed = 25f;

    [Tooltip("船锚自动收回速度。")]
    [Min(0.01f)]
    [SerializeField] private float anchorRetractSpeed = 18f;

    [Header("燃料消耗")]
    [Tooltip("每次船锚成功发射消耗的 Fuel。")]
    [Min(0f)]
    [SerializeField] private float fuelCostPerShot = 5f;

    [Tooltip("留空时自动从潜艇父级或当前场景寻找 SubmarineFuel2D。")]
    [SerializeField] private SubmarineFuel2D fuelSystem;

    [Header("命中墙壁后的拉拽")]
    [Tooltip("命中墙壁后等待多久，再给潜艇一次朝锚点方向的冲量并自动收锚。")]
    [Min(0f)]
    [SerializeField] private float wallHitPullDelay = 0.5f;

    [Tooltip("施加给潜艇的一次性冲量大小。数值越大，被锚拽动得越明显。")]
    [Min(0f)]
    [SerializeField] private float wallPullImpulse = 6f;

    [Header("墙面粒子反馈")]
    [Tooltip("命中墙面以及从墙面开始回收时，是否播放向外扩散的圆形粒子。")]
    [SerializeField] private bool enableWallParticleFeedback = true;

    [Tooltip("命中墙面时生成的粒子数量。")]
    [Min(0)]
    [SerializeField] private int wallHitParticleCount = 18;

    [Tooltip("命中墙面粒子的直径。")]
    [Min(0.001f)]
    [SerializeField] private float wallHitParticleSize = 0.18f;

    [Tooltip("命中墙面粒子的颜色；Alpha 控制半透明程度。")]
    [SerializeField] private Color wallHitParticleColor =
        new Color(0.55f, 0.9f, 1f, 0.55f);

    [Tooltip("从墙面开始回收时生成的粒子数量。")]
    [Min(0)]
    [SerializeField] private int wallRetractParticleCount = 12;

    [Tooltip("从墙面开始回收时粒子的直径。")]
    [Min(0.001f)]
    [SerializeField] private float wallRetractParticleSize = 0.14f;

    [Tooltip("从墙面开始回收时粒子的颜色；Alpha 控制半透明程度。")]
    [SerializeField] private Color wallRetractParticleColor =
        new Color(0.75f, 0.92f, 1f, 0.42f);

    [Tooltip("粒子存活时间。")]
    [Min(0.01f)]
    [SerializeField] private float wallParticleLifetime = 0.42f;

    [Tooltip("粒子从锚点向外扩散的速度。")]
    [Min(0f)]
    [SerializeField] private float wallParticleOutwardSpeed = 1.8f;

    [Tooltip("粒子的 Sorting Layer。")]
    [SerializeField] private string wallParticleSortingLayerName = "Default";

    [Tooltip("粒子的层内顺序。")]
    [SerializeField] private int wallParticleOrderInLayer = 10;

    [Header("绳子宽度")]
    [SerializeField] private bool inverseWidthByLength = true;
    [Min(0.001f)][SerializeField] private float ropeWidthAtReferenceLength = 0.07f;
    [Min(0.01f)][SerializeField] private float widthReferenceLength = 5f;
    [Min(0.001f)][SerializeField] private float minimumRopeWidth = 0.025f;
    [Min(0.001f)][SerializeField] private float maximumRopeWidth = 0.13f;

    [Header("绳子显示")]
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private Color ropeColor = Color.white;
    [SerializeField] private string ropeSortingLayerName = "Default";
    [SerializeField] private int ropeOrderInLayer;
    [SerializeField] private bool autoConfigureLineRenderer = true;

    [Header("常驻运行组件")]
    [Tooltip("负责绳子和物理逻辑的常驻组件，不要禁用它。")]
    [SerializeField] private AnchorRopeRuntime2D ropeRuntime;

    [Header("调试")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasUsedExplicitPlayerBinding;
    private int externalControlBlockCount;

    public AnchorDirection Direction => anchorDirection;
    public PlayerInputBase CurrentPlayerInput => currentPlayerInput;
    public bool HasPlayerInput => currentPlayerInput != null;
    public bool AreControlsBlocked => externalControlBlockCount > 0;

    public bool IsAnchorActive =>
        ropeRuntime != null &&
        ropeRuntime.IsAnchorActive;

    /// <summary>
    /// 兼容旧接口。
    /// 现在表示船锚命中墙壁后，正在等待那次拉拽。
    /// 不再代表永久固定和持续拉力。
    /// </summary>
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

    internal float FuelCostPerShot =>
        fuelCostPerShot;

    internal float WallHitPullDelay =>
        wallHitPullDelay;

    internal float WallPullImpulse =>
        wallPullImpulse;

    internal bool EnableWallParticleFeedback =>
        enableWallParticleFeedback;

    internal int WallHitParticleCount =>
        wallHitParticleCount;

    internal float WallHitParticleSize =>
        wallHitParticleSize;

    internal Color WallHitParticleColor =>
        wallHitParticleColor;

    internal int WallRetractParticleCount =>
        wallRetractParticleCount;

    internal float WallRetractParticleSize =>
        wallRetractParticleSize;

    internal Color WallRetractParticleColor =>
        wallRetractParticleColor;

    internal float WallParticleLifetime =>
        wallParticleLifetime;

    internal float WallParticleOutwardSpeed =>
        wallParticleOutwardSpeed;

    internal string WallParticleSortingLayerName =>
        wallParticleSortingLayerName;

    internal int WallParticleOrderInLayer =>
        wallParticleOrderInLayer;

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

    public bool IsControlledBy(
        PlayerInputBase playerInput)
    {
        return playerInput != null &&
               currentPlayerInput == playerInput;
    }

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

        if (AreControlsBlocked)
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
            SetCurrentInputFeedback(false);
            return;
        }

        /*
         * 现在已经没有持续收绳功能，
         * 所以第二个参数始终传 false。
         *
         * 这样手柄右摇杆不会再触发持续收绳震动。
         */
        currentPlayerInput.SetAnchorControlFeedback(
            true,
            false
        );

        /*
         * 键盘 Q / 手柄 R1：
         * 发射船锚。
         */
        if (currentPlayerInput.AnchorShootPressed)
        {
            bool shot =
                ropeRuntime.TryShootAnchor();

            if (shot)
            {
                currentPlayerInput
                    .PlayAnchorShootFeedback();
            }
        }

        /*
         * 键盘 R / 手柄 South：
         * 随时取消并回收船锚。
         *
         * 如果正在等待命中后的拉拽，
         * 手动回收会取消那次拉拽。
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
    }

    private void OnDisable()
    {
        /*
         * 这里只停止读取输入和手柄反馈。
         *
         * AnchorRopeRuntime2D 仍会继续完成：
         * 飞行、命中等待、拉拽以及自动回收。
         */
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

        fuelCostPerShot =
            Mathf.Max(0f, fuelCostPerShot);

        wallHitPullDelay =
            Mathf.Max(
                0f,
                wallHitPullDelay
            );

        wallPullImpulse =
            Mathf.Max(
                0f,
                wallPullImpulse
            );

        wallHitParticleCount =
            Mathf.Max(
                0,
                wallHitParticleCount
            );

        wallHitParticleSize =
            Mathf.Max(
                0.001f,
                wallHitParticleSize
            );

        wallRetractParticleCount =
            Mathf.Max(
                0,
                wallRetractParticleCount
            );

        wallRetractParticleSize =
            Mathf.Max(
                0.001f,
                wallRetractParticleSize
            );

        wallParticleLifetime =
            Mathf.Max(
                0.01f,
                wallParticleLifetime
            );

        wallParticleOutwardSpeed =
            Mathf.Max(
                0f,
                wallParticleOutwardSpeed
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
    /// 供其他系统直接调用发射。
    /// </summary>
    public bool TryShootAnchor()
    {
        FindRuntime();

        bool shot =
            ropeRuntime != null &&
            ropeRuntime.TryShootAnchor();

        if (shot &&
            currentPlayerInput != null)
        {
            currentPlayerInput
                .PlayAnchorShootFeedback();
        }

        return shot;
    }

    /// <summary>
    /// 供其他系统直接调用回收。
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
    /// 保留旧接口，避免其他旧脚本报错。
    /// 新玩法中不再产生持续收绳拉力。
    /// </summary>
    public void SetReelHeld(
        bool held)
    {
        FindRuntime();

        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(held);
        }
    }

    /// <summary>
    /// 由 AnchorRopeRuntime2D 在自动回收开始时调用。
    /// </summary>
    internal void NotifyAutomaticRetractStarted()
    {
        if (currentPlayerInput == null ||
            !isActiveAndEnabled)
        {
            return;
        }

        currentPlayerInput
            .PlayAnchorRetractFeedback();
    }

    /// <summary>
    /// 玩家开始操作时绑定准确的玩家输入。
    /// </summary>
    public void SetPlayerInput(
        PlayerInputBase playerInput)
    {
        hasUsedExplicitPlayerBinding = true;

        if (currentPlayerInput == playerInput)
        {
            SetCurrentInputFeedback(
                isActiveAndEnabled
            );

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
    /// 玩家结束操作时清除输入绑定。
    /// </summary>
    public void ClearPlayerInput(
        PlayerInputBase playerInput = null)
    {
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
                false
            );
    }

    private void FindPlayerInput()
    {
        if (currentPlayerInput != null ||
            !autoFindPlayerInput)
        {
            return;
        }

        if (keepExplicitBindingMode &&
            hasUsedExplicitPlayerBinding)
        {
            return;
        }

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
                "不会自动绑定。"
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

        if (fuelSystem == null)
        {
            fuelSystem =
                GetComponentInParent<SubmarineFuel2D>();

            if (fuelSystem == null)
            {
                fuelSystem =
                    FindObjectOfType<SubmarineFuel2D>();
            }
        }
    }

    internal bool TryConsumeFuelForShot()
    {
        if (fuelCostPerShot <= 0f)
        {
            return true;
        }

        if (fuelSystem == null)
        {
            FindReferences();
        }

        if (fuelSystem == null ||
            !fuelSystem.ConsumeFuel(fuelCostPerShot))
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    $"[AnchorLauncher2D] {gameObject.name} Fuel 不足，" +
                    $"发射需要 {fuelCostPerShot:F1}。"
                );
            }

            return false;
        }

        return true;
    }

    private Transform FindAnchorReference()
    {
        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0;
             i < children.Length;
             i++)
        {
            Transform child =
                children[i];

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
            "键盘：Q 发射 / R 回收\n" +
            "手柄：R1 发射 / South 回收"
        );
    }

    public void AddExternalControlBlock()
    {
        externalControlBlockCount++;
        SetCurrentInputFeedback(false);
    }

    public void RemoveExternalControlBlock()
    {
        externalControlBlockCount =
            Mathf.Max(0, externalControlBlockCount - 1);
    }
}
