using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 船锚的常驻运行组件。
///
/// 无论玩家是否正在操作发射器，这个脚本都会持续负责：
///
/// 1. 船锚飞行；
/// 2. 船锚自动收回；
/// 3. 绳子始终连接发射器和船锚；
/// 4. 船锚发射后的实时自然拉力；
/// 5. 玩家操作时的额外主动拉力。
///
/// OperateController 不要禁用这个组件。
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AnchorLaunchDetector2D))]
public class AnchorRopeRuntime2D : MonoBehaviour
{
    private enum AnchorState
    {
        Idle,
        Flying,
        Attached,
        Retracting
    }

    private AnchorLauncher2D settings;

    private Transform bodyTransform;
    private Rigidbody2D bodyRigidbody;

    private LineRenderer lineRenderer;
    private AnchorLaunchDetector2D detector;
    private Transform anchorTransform;

    private Vector3 anchorOriginalLocalPosition;
    private Quaternion anchorOriginalLocalRotation;
    private Vector3 anchorOriginalLocalScale;

    private int anchorOriginalSiblingIndex;
    private float anchorWorldZ;

    private AnchorState currentState =
        AnchorState.Idle;

    private Vector2 anchorPoint;
    private Vector2 flightTargetPoint;
    private Vector2 currentAnchorPosition;
    private Vector2 launchDirection;

    private bool reelKeyHeld;
    private bool initialized;

    private Material runtimeLineMaterial;

    public bool IsAnchorActive =>
        currentState != AnchorState.Idle;

    public bool IsAnchorAttached =>
        currentState == AnchorState.Attached;

    public bool IsAnchorRetracting =>
        currentState == AnchorState.Retracting;

    /// <summary>
    /// 船锚当前的实时世界坐标。
    ///
    /// 船锚发射后直接读取船锚 Transform，
    /// 不再只返回命中时保存的位置。
    /// </summary>
    public Vector2 AnchorPoint =>
        anchorTransform != null &&
        currentState != AnchorState.Idle
            ? (Vector2)anchorTransform.position
            : anchorPoint;

    private void Awake()
    {
        Bind(
            GetComponent<AnchorLauncher2D>()
        );
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            Bind(
                GetComponent<AnchorLauncher2D>()
            );
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        UpdateAnchorFlight();
        UpdateAnchorRetract();

        if (currentState == AnchorState.Idle)
        {
            PlaceAnchorAtRestPosition();
        }
    }

    private void FixedUpdate()
    {
        if (
            !initialized ||
            currentState == AnchorState.Idle
        )
        {
            return;
        }

        /*
         * 船锚一旦发射，
         * 每个物理帧都重新读取：
         *
         * 1. 发射器中心位置；
         * 2. 船锚中心位置；
         * 3. 两点之间的实时方向；
         * 4. 两点之间的实时距离。
         *
         * 因此潜艇移动或旋转后，
         * 拉力方向会立即跟着改变。
         */
        ApplyRealtimeRopePull();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        /*
         * 在所有普通 Update 完成后刷新绳子，
         * 保证绳子始终严格连接发射器中心和船锚中心。
         */
        UpdateRopeVisual();
    }

    private void OnDestroy()
    {
        if (runtimeLineMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeLineMaterial);
        }
        else
        {
            DestroyImmediate(runtimeLineMaterial);
        }

        runtimeLineMaterial = null;
    }

    /// <summary>
    /// 绑定保存参数的 AnchorLauncher2D。
    /// </summary>
    public void Bind(
        AnchorLauncher2D launcherSettings
    )
    {
        if (launcherSettings == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 找不到 AnchorLauncher2D。"
            );

            enabled = false;
            return;
        }

        settings = launcherSettings;

        if (!initialized)
        {
            initialized =
                FindComponentsAndInitialize();
        }

        if (initialized)
        {
            RefreshVisualConfiguration();
        }
    }

    /// <summary>
    /// 刷新 LineRenderer 设置。
    /// </summary>
    public void RefreshVisualConfiguration()
    {
        if (
            settings == null ||
            lineRenderer == null
        )
        {
            return;
        }

        if (settings.AutoConfigureLineRenderer)
        {
            ApplyLineRendererVisualSettings();
            AssignLineRendererMaterial();
        }
    }

    /// <summary>
    /// 发射船锚。
    /// </summary>
    public bool TryShootAnchor()
    {
        if (
            !initialized ||
            currentState != AnchorState.Idle
        )
        {
            return false;
        }

        if (
            !TryGetLaunchDirection(
                out Vector2 requestedDirection
            )
        )
        {
            return false;
        }

        Vector2 launcherPosition =
            transform.position;

        Vector2 anchorRestPosition =
            GetAnchorRestWorldPosition(
                requestedDirection
            );

        detector =
            settings.LaunchDetector;

        if (detector == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 缺少 AnchorLaunchDetector2D。"
            );

            return false;
        }

        AnchorLaunchDetector2D.LaunchResult result;

        bool gotLaunchResult =
            detector.TryGetLaunchResult(
                launcherPosition,
                anchorRestPosition,
                requestedDirection,
                settings.MaxRopeLength,
                out result
            );

        if (!gotLaunchResult)
        {
            if (settings.ShowDebugLog)
            {
                Debug.LogWarning(
                    $"[Anchor Launcher] " +
                    $"{gameObject.name} 无法发射：" +
                    $"{result.FailureReason}"
                );
            }

            return false;
        }

        launchDirection =
            result.Direction;

        currentAnchorPosition =
            result.RayOrigin;

        flightTargetPoint =
            result.FlightTargetPoint;

        /*
         * 发射时解除船锚与发射器的父子关系。
         *
         * 这样潜艇移动后，
         * 世界空间中的船锚不会跟着潜艇移动。
         */
        anchorTransform.SetParent(
            null,
            true
        );

        SetAnchorWorldPosition(
            currentAnchorPosition
        );

        anchorPoint =
            currentAnchorPosition;

        lineRenderer.enabled = true;

        currentState =
            AnchorState.Flying;

        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 开始发射。\n" +
                $"方向：{launchDirection}\n" +
                $"最远飞行点：{flightTargetPoint}"
            );
        }

        /*
         * 发射速度为 0 时，
         * 直接瞬间检测整条发射路径。
         */
        if (settings.AnchorShootSpeed <= 0f)
        {
            RaycastHit2D instantHit;

            if (
                detector.TryDetectFlightHit(
                    currentAnchorPosition,
                    flightTargetPoint,
                    out instantHit
                )
            )
            {
                HandleFlightHit(
                    instantHit
                );

                return true;
            }

            currentAnchorPosition =
                flightTargetPoint;

            anchorPoint =
                currentAnchorPosition;

            SetAnchorWorldPosition(
                currentAnchorPosition
            );

            CompleteAnchorFlight();
        }

        return true;
    }

    /// <summary>
    /// 松开固定点并开始收回船锚。
    /// </summary>
    public void StartRetractingAnchor()
    {
        if (
            !initialized ||
            currentState == AnchorState.Idle ||
            currentState == AnchorState.Retracting
        )
        {
            return;
        }

        currentState =
            AnchorState.Retracting;

        reelKeyHeld = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 设置玩家是否正在主动收绳。
    /// </summary>
    public void SetReelHeld(
        bool held
    )
    {
        reelKeyHeld =
            held &&
            currentState == AnchorState.Attached;
    }

    private bool FindComponentsAndInitialize()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        detector =
            settings.LaunchDetector;

        if (lineRenderer == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 缺少 LineRenderer。"
            );

            enabled = false;
            return false;
        }

        if (detector == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 缺少 AnchorLaunchDetector2D。"
            );

            enabled = false;
            return false;
        }

        /*
         * 默认发射器的直接父物体就是潜艇主体。
         */
        bodyTransform =
            transform.parent;

        if (bodyTransform == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 没有父物体。"
            );

            enabled = false;
            return false;
        }

        bodyRigidbody =
            bodyTransform.GetComponent<Rigidbody2D>();

        if (bodyRigidbody == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] 父物体 " +
                $"{bodyTransform.name} 缺少 Rigidbody2D。"
            );

            enabled = false;
            return false;
        }

        anchorTransform =
            FindAnchorChild();

        if (anchorTransform == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 下没有找到船锚子物体。"
            );

            enabled = false;
            return false;
        }

        anchorOriginalLocalPosition =
            anchorTransform.localPosition;

        anchorOriginalLocalRotation =
            anchorTransform.localRotation;

        anchorOriginalLocalScale =
            anchorTransform.localScale;

        anchorOriginalSiblingIndex =
            anchorTransform.GetSiblingIndex();

        anchorWorldZ =
            anchorTransform.position.z;

        SetupLineRenderer();
        PlaceAnchorAtRestPosition();

        return true;
    }

    /// <summary>
    /// 查找作为发射船锚的子物体。
    /// </summary>
    private Transform FindAnchorChild()
    {
        Transform firstChild = null;

        for (
            int i = 0;
            i < transform.childCount;
            i++
        )
        {
            Transform child =
                transform.GetChild(i);

            if (firstChild == null)
            {
                firstChild = child;
            }

            string lowerName =
                child.name.ToLowerInvariant();

            if (
                lowerName.Contains("anchor") ||
                child.name.Contains("锚")
            )
            {
                return child;
            }
        }

        if (
            firstChild != null &&
            settings.ShowDebugLog
        )
        {
            Debug.LogWarning(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 没找到名字包含 Anchor 或 锚 的子物体，" +
                $"将使用第一个子物体 {firstChild.name}。"
            );
        }

        return firstChild;
    }

    private void SetupLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

        RefreshVisualConfiguration();
    }

    private void ApplyLineRendererVisualSettings()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startColor =
            settings.RopeColor;

        lineRenderer.endColor =
            settings.RopeColor;

        lineRenderer.sortingLayerName =
            settings.RopeSortingLayerName;

        lineRenderer.sortingOrder =
            settings.RopeOrderInLayer;

        lineRenderer.alignment =
            LineAlignment.View;
    }

    private void AssignLineRendererMaterial()
    {
        if (settings.RopeMaterial != null)
        {
            lineRenderer.sharedMaterial =
                settings.RopeMaterial;

            return;
        }

        if (runtimeLineMaterial != null)
        {
            if (
                runtimeLineMaterial.HasProperty(
                    "_Color"
                )
            )
            {
                runtimeLineMaterial.SetColor(
                    "_Color",
                    settings.RopeColor
                );
            }

            if (
                runtimeLineMaterial.HasProperty(
                    "_BaseColor"
                )
            )
            {
                runtimeLineMaterial.SetColor(
                    "_BaseColor",
                    settings.RopeColor
                );
            }

            lineRenderer.sharedMaterial =
                runtimeLineMaterial;

            return;
        }

        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/2D/Sprite-Unlit-Default"
                );
        }

        if (shader == null)
        {
            Debug.LogWarning(
                $"[Anchor Rope Runtime] " +
                $"{gameObject.name} 找不到适合绳子的 Shader。"
            );

            return;
        }

        runtimeLineMaterial =
            new Material(shader);

        runtimeLineMaterial.name =
            "AutoGenerated_RopeMaterial";

        if (
            runtimeLineMaterial.HasProperty(
                "_Color"
            )
        )
        {
            runtimeLineMaterial.SetColor(
                "_Color",
                settings.RopeColor
            );
        }

        if (
            runtimeLineMaterial.HasProperty(
                "_BaseColor"
            )
        )
        {
            runtimeLineMaterial.SetColor(
                "_BaseColor",
                settings.RopeColor
            );
        }

        lineRenderer.sharedMaterial =
            runtimeLineMaterial;
    }

    private void UpdateAnchorFlight()
    {
        if (
            currentState != AnchorState.Flying
        )
        {
            return;
        }

        Vector2 previousPosition =
            currentAnchorPosition;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                previousPosition,
                flightTargetPoint,
                settings.AnchorShootSpeed *
                Time.deltaTime
            );

        RaycastHit2D hit;

        if (
            detector.TryDetectFlightHit(
                previousPosition,
                nextPosition,
                out hit
            )
        )
        {
            HandleFlightHit(
                hit
            );

            return;
        }

        currentAnchorPosition =
            nextPosition;

        anchorPoint =
            currentAnchorPosition;

        SetAnchorWorldPosition(
            currentAnchorPosition
        );

        float remainingDistance =
            Vector2.Distance(
                currentAnchorPosition,
                flightTargetPoint
            );

        if (remainingDistance <= 0.001f)
        {
            currentAnchorPosition =
                flightTargetPoint;

            anchorPoint =
                currentAnchorPosition;

            SetAnchorWorldPosition(
                currentAnchorPosition
            );

            CompleteAnchorFlight();
        }
    }

    private void HandleFlightHit(
        RaycastHit2D hit
    )
    {
        anchorPoint =
            hit.point;

        currentAnchorPosition =
            anchorPoint;

        SetAnchorWorldPosition(
            anchorPoint
        );

        AttachAnchor();

        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 命中目标。\n" +
                $"命中物体：{hit.collider.gameObject.name}\n" +
                $"固定点：{hit.point}\n" +
                $"发射方向：{launchDirection}"
            );
        }
    }

    private void CompleteAnchorFlight()
    {
        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} " +
                $"未命中目标，到达最大距离后自动收回。"
            );
        }

        StartRetractingAnchor();
    }

    private void AttachAnchor()
    {
        currentState =
            AnchorState.Attached;

        reelKeyHeld = false;
    }

    private void UpdateAnchorRetract()
    {
        if (
            currentState != AnchorState.Retracting
        )
        {
            return;
        }

        if (
            !TryGetLaunchDirection(
                out Vector2 direction
            )
        )
        {
            return;
        }

        Vector2 targetPosition =
            GetAnchorRestWorldPosition(
                direction
            );

        Vector2 newPosition =
            Vector2.MoveTowards(
                anchorTransform.position,
                targetPosition,
                settings.AnchorRetractSpeed *
                Time.deltaTime
            );

        currentAnchorPosition =
            newPosition;

        anchorPoint =
            newPosition;

        SetAnchorWorldPosition(
            newPosition
        );

        float remainingDistance =
            Vector2.Distance(
                newPosition,
                targetPosition
            );

        if (remainingDistance <= 0.001f)
        {
            FinishAnchorRetract();
        }
    }

    private void FinishAnchorRetract()
    {
        currentState =
            AnchorState.Idle;

        reelKeyHeld = false;

        anchorTransform.SetParent(
            transform,
            true
        );

        anchorTransform.SetSiblingIndex(
            Mathf.Clamp(
                anchorOriginalSiblingIndex,
                0,
                Mathf.Max(
                    0,
                    transform.childCount - 1
                )
            )
        );

        anchorTransform.localRotation =
            anchorOriginalLocalRotation;

        anchorTransform.localScale =
            anchorOriginalLocalScale;

        PlaceAnchorAtRestPosition();

        anchorPoint =
            anchorTransform.position;

        currentAnchorPosition =
            anchorPoint;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 根据船锚中心和发射器中心的实时位置施加拉力。
    ///
    /// 拉力作用在发射器所在的位置，
    /// 因此发射器偏离潜艇重心时可以自然产生旋转。
    /// </summary>
    /// <summary>
    /// 根据船锚中心和发射器中心的实时位置施加拉力。
    ///
    /// 使用 AddForce 将力作用在潜艇质心，
    /// 避免水平拉力因为发射器偏离质心而产生额外旋转，
    /// 进而表现为向上或向下的加速度。
    /// </summary>
    /// <summary>
    /// 根据潜艇 Rigidbody2D 的真实质心和船锚位置，
    /// 持续向船锚方向施加拉力。
    ///
    /// 因为力通过 Rigidbody2D.AddForce 作用于质心，
    /// 所以拉力方向也必须从质心指向船锚，
    /// 不能从发射器位置指向船锚。
    /// </summary>
    private void ApplyRealtimeRopePull()
    {
        if (
            anchorTransform == null ||
            bodyRigidbody == null
        )
        {
            return;
        }

        /*
         * 船锚当前的世界坐标。
         */
        Vector2 anchorCenter =
            anchorTransform.position;

        anchorPoint =
            anchorCenter;

        /*
         * Rigidbody2D 的真实世界质心。
         *
         * 不能使用发射器的 transform.position，
         * 因为最终 AddForce 是作用在 Rigidbody2D 质心上的。
         */
        Vector2 bodyCenter =
            bodyRigidbody.worldCenterOfMass;

        /*
         * 从潜艇质心指向船锚。
         */
        Vector2 bodyToAnchor =
            anchorCenter -
            bodyCenter;

        /*
         * 消除非常微小的浮点误差。
         *
         * 例如理论上完全水平时，
         * Y 可能仍然会出现 0.00001 之类的数值。
         */
        const float axisSnapDistance = 0.02f;

        if (
            Mathf.Abs(bodyToAnchor.y) <=
            axisSnapDistance
        )
        {
            bodyToAnchor.y = 0f;
        }

        if (
            Mathf.Abs(bodyToAnchor.x) <=
            axisSnapDistance
        )
        {
            bodyToAnchor.x = 0f;
        }

        float distanceToAnchor =
            bodyToAnchor.magnitude;

        if (
            distanceToAnchor <=
            settings.StopPullDistance ||
            distanceToAnchor <= 0.0001f
        )
        {
            return;
        }

        Vector2 pullDirection =
            bodyToAnchor /
            distanceToAnchor;

        /*
         * 默认的持续自然拉力。
         */
        float pullAcceleration =
            settings.PassivePullAcceleration;

        float maximumPullSpeed =
            settings.PassiveMaxPullSpeed;

        /*
         * 玩家主动收绳时，
         * 在自然拉力基础上增加主动拉力。
         */
        if (
            currentState == AnchorState.Attached &&
            reelKeyHeld
        )
        {
            pullAcceleration +=
                settings.ReelPullAcceleration;

            maximumPullSpeed =
                Mathf.Max(
                    maximumPullSpeed,
                    settings.MaxReelSpeed
                );
        }

        if (
            pullAcceleration <= 0f ||
            maximumPullSpeed <= 0f
        )
        {
            return;
        }

        /*
         * 获取潜艇整体速度。
         */
        Vector2 currentVelocity =
            bodyRigidbody.velocity;

        /*
         * 计算潜艇目前朝船锚方向的速度。
         */
        float speedTowardsAnchor =
            Vector2.Dot(
                currentVelocity,
                pullDirection
            );

        /*
         * 已经达到允许的最大拉动速度时，
         * 不再继续沿该方向加速。
         */
        if (
            speedTowardsAnchor >=
            maximumPullSpeed
        )
        {
            return;
        }

        float remainingSpeed =
            maximumPullSpeed -
            speedTowardsAnchor;

        float maximumAccelerationThisStep =
            remainingSpeed /
            Mathf.Max(
                Time.fixedDeltaTime,
                0.0001f
            );

        float actualAcceleration =
            Mathf.Min(
                pullAcceleration,
                maximumAccelerationThisStep
            );

        if (actualAcceleration <= 0f)
        {
            return;
        }

        /*
         * F = m × a
         */
        Vector2 pullForce =
            pullDirection *
            actualAcceleration *
            bodyRigidbody.mass;

        /*
         * AddForce 默认作用于 Rigidbody2D 的质心。
         */
        bodyRigidbody.AddForce(
            pullForce,
            ForceMode2D.Force
        );
    }

    /// <summary>
    /// 每帧刷新绳子的两个端点。
    /// </summary>
    private void UpdateRopeVisual()
    {
        if (
            lineRenderer == null ||
            anchorTransform == null
        )
        {
            return;
        }

        if (
            currentState == AnchorState.Idle
        )
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        /*
         * 绳子起点始终是发射器实时中心。
         */
        Vector3 ropeStart =
            transform.position;

        /*
         * 绳子终点始终是船锚实时中心。
         */
        Vector3 ropeEnd =
            anchorTransform.position;

        /*
         * 防止两个物体 Z 坐标不一致，
         * 导致绳子视觉上倾斜或排序异常。
         */
        ropeEnd.z =
            ropeStart.z;

        lineRenderer.SetPosition(
            0,
            ropeStart
        );

        lineRenderer.SetPosition(
            1,
            ropeEnd
        );

        float ropeLength =
            Vector2.Distance(
                ropeStart,
                ropeEnd
            );

        UpdateRopeWidth(
            ropeLength
        );
    }

    private void UpdateRopeWidth(
        float visualLength
    )
    {
        if (lineRenderer == null)
        {
            return;
        }

        float targetWidth =
            settings.RopeWidthAtReferenceLength;

        if (settings.InverseWidthByLength)
        {
            float safeLength =
                Mathf.Max(
                    visualLength,
                    0.05f
                );

            targetWidth =
                settings.RopeWidthAtReferenceLength *
                settings.WidthReferenceLength /
                safeLength;
        }

        targetWidth =
            Mathf.Clamp(
                targetWidth,
                settings.MinimumRopeWidth,
                settings.MaximumRopeWidth
            );

        lineRenderer.startWidth =
            targetWidth;

        lineRenderer.endWidth =
            targetWidth;
    }

    private void PlaceAnchorAtRestPosition()
    {
        if (
            anchorTransform == null ||
            anchorTransform.parent != transform
        )
        {
            return;
        }

        if (
            !TryGetLaunchDirection(
                out Vector2 direction
            )
        )
        {
            return;
        }

        Vector2 restPosition =
            GetAnchorRestWorldPosition(
                direction
            );

        SetAnchorWorldPosition(
            restPosition
        );

        anchorPoint =
            restPosition;

        currentAnchorPosition =
            restPosition;
    }

    private Vector2 GetAnchorRestWorldPosition(
        Vector2 direction
    )
    {
        return
            (Vector2)transform.position +
            direction *
            settings.AnchorOffset;
    }

    /// <summary>
    /// 根据船锚最初相对于发射器的位置计算发射方向。
    /// </summary>
    private bool TryGetLaunchDirection(
        out Vector2 direction
    )
    {
        Vector2 launcherCenter =
            transform.position;

        Vector3 originalAnchorWorldPosition =
            transform.TransformPoint(
                anchorOriginalLocalPosition
            );

        Vector2 rawDirection =
            (Vector2)originalAnchorWorldPosition -
            launcherCenter;

        if (
            rawDirection.sqrMagnitude <=
            0.0001f
        )
        {
            direction = Vector2.zero;

            if (settings.ShowDebugLog)
            {
                Debug.LogWarning(
                    $"[Anchor Launcher] " +
                    $"{gameObject.name} 的发射器中心和船锚初始位置重合。"
                );
            }

            return false;
        }

        direction =
            rawDirection.normalized;

        return true;
    }

    private void SetAnchorWorldPosition(
        Vector2 position
    )
    {
        if (anchorTransform == null)
        {
            return;
        }

        anchorTransform.position =
            new Vector3(
                position.x,
                position.y,
                anchorWorldZ
            );
    }

    private void OnDrawGizmosSelected()
    {
        AnchorLauncher2D previewSettings =
            settings != null
                ? settings
                : GetComponent<AnchorLauncher2D>();

        if (previewSettings == null)
        {
            return;
        }

        Transform previewAnchor = null;

        for (
            int i = 0;
            i < transform.childCount;
            i++
        )
        {
            Transform child =
                transform.GetChild(i);

            string lowerName =
                child.name.ToLowerInvariant();

            if (
                lowerName.Contains("anchor") ||
                child.name.Contains("锚")
            )
            {
                previewAnchor = child;
                break;
            }
        }

        if (
            previewAnchor == null &&
            transform.childCount > 0
        )
        {
            previewAnchor =
                transform.GetChild(0);
        }

        if (previewAnchor == null)
        {
            return;
        }

        Vector2 launcherCenter =
            transform.position;

        Vector2 rawDirection =
            (Vector2)previewAnchor.position -
            launcherCenter;

        if (
            rawDirection.sqrMagnitude <=
            0.0001f
        )
        {
            return;
        }

        Vector2 direction =
            rawDirection.normalized;

        Vector2 restPosition =
            launcherCenter +
            direction *
            previewSettings.AnchorOffset;

        Gizmos.DrawLine(
            launcherCenter,
            restPosition
        );

        Gizmos.DrawWireSphere(
            restPosition,
            0.08f
        );

        Gizmos.DrawLine(
            restPosition,
            restPosition +
            direction *
            previewSettings.MaxRopeLength
        );
    }
}