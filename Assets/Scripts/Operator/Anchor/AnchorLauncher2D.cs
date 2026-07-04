using UnityEngine;

/// <summary>
/// 负责船锚输入和船锚参数配置。
///
/// 该组件可以被 OperateController 启用或禁用。
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

    [Header("船锚方向与按键")]

    [Tooltip("决定该发射器使用哪一组按键。")]
    [SerializeField]
    private AnchorDirection anchorDirection =
        AnchorDirection.Up;

    [Header("船锚引用")]

    [Tooltip(
        "船锚收回状态下的物体。" +
        "留空时会自动寻找名字中包含 Anchor 或“锚”的子物体。"
    )]
    [SerializeField]
    private Transform anchorReference;

    [Header("发射检测")]

    [Tooltip("负责检测船锚飞行过程中是否命中目标。")]
    [SerializeField]
    private AnchorLaunchDetector2D launchDetector;

    [Tooltip("最大绳长，同时也是未命中时的最大飞行距离。")]
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

    [Tooltip("自然拉力允许潜艇达到的最大朝向锚点速度。")]
    [Min(0f)]
    [SerializeField]
    private float passiveMaxPullSpeed = 1.5f;

    [Header("玩家主动拉回")]

    [Tooltip("按住拉回键时额外增加的拉力加速度。")]
    [Min(0f)]
    [SerializeField]
    private float reelPullAcceleration = 12f;

    [Tooltip("主动拉回时允许达到的最大朝锚点速度。")]
    [Min(0f)]
    [SerializeField]
    private float maxReelSpeed = 8f;

    [Tooltip("距离锚点小于该值后停止施加拉力。")]
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

    [Tooltip("负责绳子和物理逻辑的常驻组件，不要禁用它。")]
    [SerializeField]
    private AnchorRopeRuntime2D ropeRuntime;

    [Header("调试")]

    [SerializeField]
    private bool showDebugLog = true;

    /// <summary>
    /// 船锚基础配置方向。
    /// </summary>
    public AnchorDirection Direction =>
        anchorDirection;

    /// <summary>
    /// 船锚收回状态下的参考物体。
    ///
    /// AnchorRotator 会通过这里获取船锚，
    /// 不再需要自己配置 AnchorReference。
    /// </summary>
    /// <summary>
    /// 当前船锚发射器配置的基础方向。
    /// 供 AnchorRotator 等外部组件读取。
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

        if (ropeRuntime != null)
        {
            ropeRuntime.Bind(this);
        }
    }

    private void OnEnable()
    {
        FindReferences();
        FindRuntime();

        if (ropeRuntime != null)
        {
            ropeRuntime.Bind(this);
        }

        LogCurrentControlKeys();
    }

    private void Update()
    {
        if (ropeRuntime == null)
        {
            return;
        }

        GetControlKeys(
            out KeyCode shootKey,
            out KeyCode reelKey,
            out KeyCode releaseKey
        );

        if (Input.GetKeyDown(shootKey))
        {
            ropeRuntime.TryShootAnchor();
        }

        if (Input.GetKeyDown(releaseKey))
        {
            ropeRuntime.StartRetractingAnchor();
        }

        ropeRuntime.SetReelHeld(
            Input.GetKey(reelKey)
        );
    }

    private void OnDisable()
    {
        /*
         * 玩家退出操作时，只取消主动拉回。
         *
         * 船锚飞行、自动收回、绳子刷新和自然拉力，
         * 都不会因为这个组件被禁用而停止。
         */
        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(false);
        }
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
    /// 供手柄或其他输入系统调用发射。
    /// </summary>
    public bool TryShootAnchor()
    {
        FindRuntime();

        return
            ropeRuntime != null &&
            ropeRuntime.TryShootAnchor();
    }

    /// <summary>
    /// 供手柄或其他输入系统调用松开并收回。
    /// </summary>
    public void StartRetractingAnchor()
    {
        FindRuntime();

        if (ropeRuntime != null)
        {
            ropeRuntime.StartRetractingAnchor();
        }
    }

    /// <summary>
    /// 供手柄或其他输入系统设置主动拉回。
    /// </summary>
    public void SetReelHeld(bool held)
    {
        FindRuntime();

        if (ropeRuntime != null)
        {
            ropeRuntime.SetReelHeld(held);
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

        for (int i = 0; i < childTransforms.Length; i++)
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

    private void GetControlKeys(
        out KeyCode shootKey,
        out KeyCode reelKey,
        out KeyCode releaseKey
    )
    {
        switch (anchorDirection)
        {
            case AnchorDirection.Up:
                shootKey = KeyCode.W;
                reelKey = KeyCode.E;
                releaseKey = KeyCode.R;
                break;

            case AnchorDirection.Right:
                shootKey = KeyCode.D;
                reelKey = KeyCode.F;
                releaseKey = KeyCode.G;
                break;

            case AnchorDirection.Down:
                shootKey = KeyCode.S;
                reelKey = KeyCode.X;
                releaseKey = KeyCode.C;
                break;

            case AnchorDirection.Left:
                shootKey = KeyCode.A;
                reelKey = KeyCode.Q;
                releaseKey = KeyCode.Z;
                break;

            default:
                shootKey = KeyCode.W;
                reelKey = KeyCode.E;
                releaseKey = KeyCode.R;
                break;
        }
    }

    private void LogCurrentControlKeys()
    {
        if (!showDebugLog)
        {
            return;
        }

        GetControlKeys(
            out KeyCode shootKey,
            out KeyCode reelKey,
            out KeyCode releaseKey
        );

        Debug.Log(
            $"[AnchorLauncher2D] {gameObject.name} 输入已启用。\n" +
            $"方向：{anchorDirection}\n" +
            $"发射：{shootKey}\n" +
            $"拉回：{reelKey}\n" +
            $"松开：{releaseKey}"
        );
    }
}