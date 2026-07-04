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
/// 4. 船锚固定后的永久自然拉力；
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

    public Vector2 AnchorPoint =>
        anchorPoint;

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

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        /*
         * 使用 LateUpdate 更新绳子。
         *
         * 这样即使潜艇和发射器在本帧发生了移动，
         * 绳子也会在所有普通 Update 完成后，
         * 再次严格连接发射器和船锚。
         */
        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        if (
            !initialized ||
            currentState != AnchorState.Attached
        )
        {
            return;
        }

        /*
         * 永久自然拉力。
         *
         * 这一部分位于常驻脚本中，
         * 所以 AnchorLauncher2D 被禁用后依然会运行。
         */
        ApplyPassiveAnchorPull();

        if (reelKeyHeld)
        {
            ApplyActiveReelPull();
        }
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

        detector = settings.LaunchDetector;

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
         * 发射时把船锚解除父子关系。
         *
         * 这样潜艇移动时，船锚不会跟着潜艇移动，
         * 绳子会连接移动中的发射器和世界中的船锚。
         */
        anchorTransform.SetParent(null, true);

        SetAnchorWorldPosition(
            currentAnchorPosition
        );

        lineRenderer.enabled = true;
        currentState = AnchorState.Flying;

        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 开始发射。\n" +
                $"方向：{launchDirection}\n" +
                $"最远飞行点：{flightTargetPoint}"
            );
        }

        /*
         * 速度设置为 0 时，直接瞬间检测整条路径。
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
                HandleFlightHit(instantHit);
                return true;
            }

            currentAnchorPosition =
                flightTargetPoint;

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
    /// 设置玩家是否正在按住主动拉回键。
    /// </summary>
    public void SetReelHeld(bool held)
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
            HandleFlightHit(hit);
            return;
        }

        currentAnchorPosition =
            nextPosition;

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

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void ApplyPassiveAnchorPull()
    {
        ApplyPull(
            settings.PassivePullAcceleration,
            settings.PassiveMaxPullSpeed
        );
    }

    private void ApplyActiveReelPull()
    {
        ApplyPull(
            settings.ReelPullAcceleration,
            settings.MaxReelSpeed
        );
    }

    /// <summary>
    /// 向锚点方向持续施加速度。
    /// </summary>
    private void ApplyPull(
        float acceleration,
        float maximumSpeed
    )
    {
        if (
            acceleration <= 0f ||
            maximumSpeed <= 0f
        )
        {
            return;
        }

        /*
         * 这里使用发射器的位置作为绳子起点。
         *
         * 锚点减去发射器位置，
         * 就是潜艇当前应该被拉动的方向。
         */
        Vector2 toAnchor =
            anchorPoint -
            (Vector2)transform.position;

        float distance =
            toAnchor.magnitude;

        if (
            distance <=
            settings.StopPullDistance ||
            distance <= 0.0001f
        )
        {
            return;
        }

        Vector2 pullDirection =
            toAnchor / distance;

        Vector2 currentVelocity =
            bodyRigidbody.velocity;

        float speedTowardsAnchor =
            Vector2.Dot(
                currentVelocity,
                pullDirection
            );

        if (
            speedTowardsAnchor >=
            maximumSpeed
        )
        {
            return;
        }

        float velocityIncrease =
            acceleration *
            Time.fixedDeltaTime;

        velocityIncrease =
            Mathf.Min(
                velocityIncrease,
                maximumSpeed -
                speedTowardsAnchor
            );

        bodyRigidbody.velocity =
            currentVelocity +
            pullDirection *
            velocityIncrease;
    }

    /// <summary>
    /// 每帧严格刷新绳子的两个端点。
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

        Vector3 ropeStart =
            transform.position;

        Vector3 ropeEnd =
            anchorTransform.position;

        /*
         * 防止两个物体的 Z 不一致，
         * 导致绳子在透视或排序上看起来歪斜。
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

        if (
            settings.InverseWidthByLength
        )
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

        SetAnchorWorldPosition(
            GetAnchorRestWorldPosition(
                direction
            )
        );
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