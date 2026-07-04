using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 船锚常驻运行组件。
///
/// 无论玩家是否正在操作发射器，本脚本都会继续负责：
/// 1. 船锚飞行；
/// 2. 命中墙壁后等待指定时间；
/// 3. 给潜艇一次朝锚点方向的中心冲量；
/// 4. 自动收回船锚；
/// 5. 绳子始终连接发射器中心和船锚中心。
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
        WaitingWallPull,
        Retracting
    }

    private AnchorLauncher2D settings;

    private Rigidbody2D bodyRigidbody;
    private LineRenderer lineRenderer;
    private AnchorLaunchDetector2D detector;
    private Transform anchorTransform;
    private AnchorAudioFeedback2D audioFeedback;
    private ParticleSystem wallParticleSystem;

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
    private AnchorItemDropPoint2D cachedItemDropPoint;
    private FloatingItemAnchorTarget2D caughtFloatingTarget;

    private float wallPullDelayTimer;
    private bool initialized;

    private Material runtimeLineMaterial;
    private Material runtimeParticleMaterial;
    private Texture2D runtimeParticleTexture;

    public bool IsAnchorActive =>
        currentState != AnchorState.Idle;

    /// <summary>
    /// 兼容旧接口。
    /// 现在表示船锚命中墙壁后正在等待一次性拉拽。
    /// </summary>
    public bool IsAnchorAttached =>
        currentState == AnchorState.WaitingWallPull;

    public bool IsAnchorRetracting =>
        currentState == AnchorState.Retracting;

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
        if (!initialized ||
            currentState != AnchorState.WaitingWallPull)
        {
            return;
        }

        wallPullDelayTimer -=
            Time.fixedDeltaTime;

        if (wallPullDelayTimer <= 0f)
        {
            ExecuteWallPullAndAutomaticRetract();
        }
    }

    private void LateUpdate()
    {
        if (initialized)
        {
            UpdateRopeVisual();
        }
    }

    private void OnDestroy()
    {
        DestroyRuntimeObject(runtimeLineMaterial);
        DestroyRuntimeObject(runtimeParticleMaterial);
        DestroyRuntimeObject(runtimeParticleTexture);

        runtimeLineMaterial = null;
        runtimeParticleMaterial = null;
        runtimeParticleTexture = null;
    }

    /// <summary>
    /// 绑定保存参数的 AnchorLauncher2D。
    /// </summary>
    public void Bind(
        AnchorLauncher2D launcherSettings)
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

        settings =
            launcherSettings;

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
        if (settings == null ||
            lineRenderer == null)
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
        if (!initialized ||
            currentState != AnchorState.Idle)
        {
            return false;
        }

        if (!TryGetLaunchDirection(
                out Vector2 requestedDirection
            ))
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

        bool gotResult =
            detector.TryGetLaunchResult(
                launcherPosition,
                anchorRestPosition,
                requestedDirection,
                settings.MaxRopeLength,
                out AnchorLaunchDetector2D.LaunchResult result
            );

        if (!gotResult)
        {
            if (settings.ShowDebugLog)
            {
                Debug.LogWarning(
                    $"[Anchor Launcher] " +
                    $"{gameObject.name} 无法发射：" +
                    result.FailureReason
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

        wallPullDelayTimer = 0f;

        /*
         * 解除船锚和发射器的父子关系。
         * 避免船锚飞出去后继续跟随潜艇移动。
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

        if (audioFeedback != null)
        {
            audioFeedback.PlayLaunch();
        }

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
         * 直接瞬间检测整条路径。
         */
        if (settings.AnchorShootSpeed <= 0f)
        {
            if (detector.TryDetectFlightHit(
                    currentAnchorPosition,
                    flightTargetPoint,
                    out RaycastHit2D instantHit
                ))
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
    /// 随时取消并开始回收。
    ///
    /// 如果正处于命中后的等待阶段，
    /// 手动回收会取消那次拉拽。
    /// </summary>
    public void StartRetractingAnchor()
    {
        if (!initialized ||
            currentState == AnchorState.Idle ||
            currentState == AnchorState.Retracting)
        {
            return;
        }

        bool wasAttachedToWall =
            currentState == AnchorState.WaitingWallPull;

        wallPullDelayTimer = 0f;

        currentState =
            AnchorState.Retracting;

        if (audioFeedback != null)
        {
            audioFeedback.PlayRetract();
        }

        if (wasAttachedToWall)
        {
            PlayWallParticleBurst(
                anchorPoint,
                settings.WallRetractParticleCount,
                settings.WallRetractParticleSize,
                settings.WallRetractParticleColor
            );
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 保留旧接口，避免其他旧脚本报错。
    /// 新玩法中不再有持续主动收绳。
    /// </summary>
    public void SetReelHeld(
        bool held)
    {
    }

    private bool FindComponentsAndInitialize()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        detector =
            settings.LaunchDetector;

        audioFeedback =
            GetComponent<AnchorAudioFeedback2D>();

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
         * 从发射器向父级查找潜艇 Rigidbody2D。
         *
         * 即使发射器和潜艇之间还有旋转节点，
         * 也可以找到真正的潜艇 Rigidbody2D。
         */
        bodyRigidbody =
            GetComponentInParent<Rigidbody2D>();

        if (bodyRigidbody == null)
        {
            Debug.LogError(
                $"[Anchor Rope Runtime] {gameObject.name} " +
                "向父级没有找到潜艇 Rigidbody2D。"
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

    private Transform FindAnchorChild()
    {
        Transform firstChild = null;

        for (int i = 0;
             i < transform.childCount;
             i++)
        {
            Transform child =
                transform.GetChild(i);

            if (firstChild == null)
            {
                firstChild = child;
            }

            string lowerName =
                child.name.ToLowerInvariant();

            if (lowerName.Contains("anchor") ||
                child.name.Contains("锚"))
            {
                return child;
            }
        }

        if (firstChild != null &&
            settings.ShowDebugLog)
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

        if (runtimeLineMaterial == null)
        {
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
                new Material(shader)
                {
                    name =
                        "AutoGenerated_RopeMaterial"
                };
        }

        ApplyColorToRuntimeMaterial();

        lineRenderer.sharedMaterial =
            runtimeLineMaterial;
    }

    private void ApplyColorToRuntimeMaterial()
    {
        if (runtimeLineMaterial == null)
        {
            return;
        }

        if (runtimeLineMaterial.HasProperty("_Color"))
        {
            runtimeLineMaterial.SetColor(
                "_Color",
                settings.RopeColor
            );
        }

        if (runtimeLineMaterial.HasProperty("_BaseColor"))
        {
            runtimeLineMaterial.SetColor(
                "_BaseColor",
                settings.RopeColor
            );
        }
    }

    private void UpdateAnchorFlight()
    {
        if (currentState != AnchorState.Flying)
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

        if (detector.TryDetectFlightHit(
                previousPosition,
                nextPosition,
                out RaycastHit2D hit
            ))
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
        RaycastHit2D hit)
    {
        anchorPoint =
            hit.point;

        currentAnchorPosition =
            anchorPoint;

        SetAnchorWorldPosition(
            anchorPoint
        );

        if (audioFeedback != null)
        {
            audioFeedback.PlayHit();
        }

        AnchorDestructible2D destructible =
            hit.collider != null
                ? hit.collider.GetComponentInParent<AnchorDestructible2D>()
                : null;

        if (destructible != null &&
            destructible.TryDestroyByAnchor())
        {
            if (settings.ShowDebugLog)
            {
                Debug.Log(
                    $"[Anchor Launcher] {gameObject.name} " +
                    $"击中并清除了 {destructible.gameObject.name}，立即回收。"
                );
            }

            BeginAutomaticRetract();
            return;
        }

        PlayWallParticleBurst(
            anchorPoint,
            settings.WallHitParticleCount,
            settings.WallHitParticleSize,
            settings.WallHitParticleColor
        );

        FloatingItemAnchorTarget2D floatingTarget =
            hit.collider.GetComponentInParent
                <FloatingItemAnchorTarget2D>();

        if (floatingTarget != null &&
            TryCatchFloatingItem(floatingTarget))
        {
            BeginAutomaticRetract();
            return;
        }

        currentState =
            AnchorState.WaitingWallPull;

        wallPullDelayTimer =
            settings.WallHitPullDelay;

        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 命中墙壁。\n" +
                $"命中物体：{hit.collider.gameObject.name}\n" +
                $"命中点：{hit.point}\n" +
                $"将在 {settings.WallHitPullDelay:F2} 秒后拉拽飞船并自动回收。"
            );
        }

        if (wallPullDelayTimer <= 0f)
        {
            ExecuteWallPullAndAutomaticRetract();
        }
    }

    private bool TryCatchFloatingItem(
        FloatingItemAnchorTarget2D floatingTarget)
    {
        if (floatingTarget == null ||
            !floatingTarget.CanBeCaught)
        {
            return false;
        }

        AnchorItemDropPoint2D dropPoint =
            GetItemDropPoint();

        if (dropPoint == null)
        {
            if (settings != null &&
                settings.ShowDebugLog)
            {
                Debug.LogWarning(
                    $"[Anchor Launcher] {gameObject.name} 命中漂浮物，" +
                    "但没有找到 AnchorItemDropPoint2D，无法生成掉落物。"
                );
            }

            return false;
        }

        bool caught =
            floatingTarget.TryCatch(dropPoint);

        if (caught)
        {
            caughtFloatingTarget = floatingTarget;
        }

        if (caught &&
            settings != null &&
            settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 命中漂浮物，" +
                "开始把它拉回对应锚位。"
            );
        }

        return caught;
    }

    private AnchorItemDropPoint2D GetItemDropPoint()
    {
        if (cachedItemDropPoint != null)
        {
            return cachedItemDropPoint;
        }

        cachedItemDropPoint =
            GetComponent<AnchorItemDropPoint2D>();

        if (cachedItemDropPoint == null)
        {
            cachedItemDropPoint =
                GetComponentInChildren<AnchorItemDropPoint2D>(
                    true
                );
        }

        if (cachedItemDropPoint == null)
        {
            cachedItemDropPoint =
                GetComponentInParent<AnchorItemDropPoint2D>();
        }

        return cachedItemDropPoint;
    }


    private void CompleteAnchorFlight()
    {
        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} " +
                "未命中墙壁，到达最大距离后自动收回。"
            );
        }

        BeginAutomaticRetract();
    }

    /// <summary>
    /// 命中等待结束后：
    /// 先给潜艇一次冲量，再开始自动收锚。
    /// </summary>
    private void ExecuteWallPullAndAutomaticRetract()
    {
        if (currentState !=
            AnchorState.WaitingWallPull)
        {
            return;
        }

        ApplyWallPullImpulse();
        BeginAutomaticRetract();
    }

    /// <summary>
    /// 从潜艇 Rigidbody2D 的真实世界质心指向锚点，
    /// 并施加一次性冲量。
    ///
    /// AddForce 默认作用在 Rigidbody2D 质心，
    /// 所以不会因为发射器安装在潜艇边缘而额外制造扭矩。
    /// </summary>
    private void ApplyWallPullImpulse()
    {
        if (bodyRigidbody == null ||
            settings.WallPullImpulse <= 0f)
        {
            return;
        }

        Vector2 bodyCenter =
            bodyRigidbody.worldCenterOfMass;

        Vector2 currentWallAnchorPoint =
            anchorTransform != null
                ? (Vector2)anchorTransform.position
                : anchorPoint;

        /*
         * 从潜艇真实质心指向锚点。
         */
        Vector2 bodyToAnchor =
            currentWallAnchorPoint -
            bodyCenter;

        if (bodyToAnchor.sqrMagnitude <=
            0.000001f)
        {
            return;
        }

        Vector2 pullDirection =
            bodyToAnchor.normalized;

        Vector2 pullImpulse =
            pullDirection *
            settings.WallPullImpulse;

        /*
         * 一次性冲量。
         *
         * 这里不是持续 Force，
         * 因此不会继续把潜艇锁在墙壁上。
         */
        bodyRigidbody.AddForce(
            pullImpulse,
            ForceMode2D.Impulse
        );

        if (settings.ShowDebugLog)
        {
            Debug.Log(
                $"[Anchor Launcher] {gameObject.name} 对潜艇施加一次拉拽冲量。\n" +
                $"潜艇质心：{bodyCenter}\n" +
                $"锚点：{currentWallAnchorPoint}\n" +
                $"方向：{pullDirection}\n" +
                $"冲量：{pullImpulse}"
            );
        }
    }

    private void BeginAutomaticRetract()
    {
        if (currentState == AnchorState.Idle ||
            currentState == AnchorState.Retracting)
        {
            return;
        }

        bool wasAttachedToWall =
            currentState == AnchorState.WaitingWallPull;

        wallPullDelayTimer = 0f;

        currentState =
            AnchorState.Retracting;

        if (audioFeedback != null)
        {
            audioFeedback.PlayRetract();
        }

        if (wasAttachedToWall)
        {
            PlayWallParticleBurst(
                anchorPoint,
                settings.WallRetractParticleCount,
                settings.WallRetractParticleSize,
                settings.WallRetractParticleColor
            );
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        /*
         * 播放对应玩家自己的回收震动。
         */
        settings.NotifyAutomaticRetractStarted();
    }

    private void PlayWallParticleBurst(
        Vector2 worldPosition,
        int particleCount,
        float particleSize,
        Color particleColor)
    {
        if (!settings.EnableWallParticleFeedback ||
            particleCount <= 0)
        {
            return;
        }

        EnsureWallParticleSystem();

        if (wallParticleSystem == null)
        {
            return;
        }

        float angleOffset =
            Random.Range(0f, 360f);

        for (int i = 0;
             i < particleCount;
             i++)
        {
            float angle =
                angleOffset +
                360f * i / particleCount;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

            ParticleSystem.EmitParams emitParams =
                new ParticleSystem.EmitParams
                {
                    position = new Vector3(
                        worldPosition.x,
                        worldPosition.y,
                        anchorWorldZ
                    ),
                    velocity =
                        direction *
                        settings.WallParticleOutwardSpeed *
                        Random.Range(0.8f, 1.2f),
                    startLifetime =
                        settings.WallParticleLifetime,
                    startSize =
                        particleSize *
                        Random.Range(0.85f, 1.15f),
                    startColor =
                        particleColor
                };

            wallParticleSystem.Emit(
                emitParams,
                1
            );
        }
    }

    private void EnsureWallParticleSystem()
    {
        if (wallParticleSystem != null)
        {
            return;
        }

        GameObject particleObject =
            new GameObject("Wall Burst Particles");

        particleObject.transform.SetParent(
            transform,
            false
        );

        wallParticleSystem =
            particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main =
            wallParticleSystem.main;

        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startLifetime =
            settings.WallParticleLifetime;
        main.startSize = 0.1f;
        main.maxParticles =
            Mathf.Max(
                64,
                Mathf.Max(
                    settings.WallHitParticleCount,
                    settings.WallRetractParticleCount
                ) * 4
            );

        ParticleSystem.EmissionModule emission =
            wallParticleSystem.emission;

        emission.enabled = false;

        ParticleSystem.ShapeModule shape =
            wallParticleSystem.shape;

        shape.enabled = false;

        ParticleSystemRenderer particleRenderer =
            particleObject.GetComponent<ParticleSystemRenderer>();

        if (particleRenderer == null)
        {
            particleRenderer =
                particleObject.AddComponent<ParticleSystemRenderer>();
        }

        if (particleRenderer != null)
        {
            particleRenderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial =
                CreateRoundParticleMaterial();
            particleRenderer.sortingLayerName =
                settings.WallParticleSortingLayerName;
            particleRenderer.sortingOrder =
                settings.WallParticleOrderInLayer;
        }
    }

    private Material CreateRoundParticleMaterial()
    {
        if (runtimeParticleMaterial != null)
        {
            return runtimeParticleMaterial;
        }

        Shader particleShader =
            Shader.Find("Sprites/Default");

        if (particleShader == null)
        {
            Debug.LogWarning(
                $"[Anchor Rope Runtime] {gameObject.name} " +
                "找不到 Sprites/Default Shader，墙面粒子可能无法显示。"
            );

            return null;
        }

        const int textureSize = 32;

        runtimeParticleTexture =
            new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false
            )
            {
                name = "Anchor Round Particle Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

        Color[] pixels =
            new Color[textureSize * textureSize];

        Vector2 center =
            new Vector2(
                (textureSize - 1) * 0.5f,
                (textureSize - 1) * 0.5f
            );

        float radius =
            textureSize * 0.48f;

        for (int y = 0;
             y < textureSize;
             y++)
        {
            for (int x = 0;
                 x < textureSize;
                 x++)
            {
                float normalizedDistance =
                    Vector2.Distance(
                        new Vector2(x, y),
                        center
                    ) / radius;

                float alpha =
                    1f - Mathf.SmoothStep(
                        0.78f,
                        1f,
                        normalizedDistance
                    );

                pixels[y * textureSize + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeParticleTexture.SetPixels(pixels);
        runtimeParticleTexture.Apply(false, true);

        runtimeParticleMaterial =
            new Material(particleShader)
            {
                name = "Anchor Round Particle Material",
                mainTexture = runtimeParticleTexture
            };

        return runtimeParticleMaterial;
    }

    private void DestroyRuntimeObject(
        Object runtimeObject)
    {
        if (runtimeObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeObject);
        }
        else
        {
            DestroyImmediate(runtimeObject);
        }
    }

    private void UpdateAnchorRetract()
    {
        if (currentState !=
            AnchorState.Retracting)
        {
            return;
        }

        if (!TryGetLaunchDirection(
                out Vector2 direction
            ))
        {
            return;
        }

        /*
         * 回收目标位置始终根据发射器当前方向实时计算。
         *
         * 潜艇移动或旋转时，
         * 船锚会继续追向发射器现在的位置。
         */
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

        wallPullDelayTimer = 0f;

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

        if (caughtFloatingTarget != null)
        {
            caughtFloatingTarget.OnAnchorRetracted();
            caughtFloatingTarget = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 每帧严格刷新绳子两端的位置。
    /// </summary>
    private void UpdateRopeVisual()
    {
        if (lineRenderer == null ||
            anchorTransform == null)
        {
            return;
        }

        if (currentState == AnchorState.Idle)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        /*
         * 绳子起点始终是发射器当前中心。
         */
        Vector3 ropeStart =
            transform.position;

        /*
         * 绳子终点始终是船锚当前中心。
         */
        Vector3 ropeEnd =
            anchorTransform.position;

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
        float visualLength)
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
        if (anchorTransform == null ||
            anchorTransform.parent != transform)
        {
            return;
        }

        if (!TryGetLaunchDirection(
                out Vector2 direction
            ))
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
        Vector2 direction)
    {
        return
            (Vector2)transform.position +
            direction *
            settings.AnchorOffset;
    }

    /// <summary>
    /// 根据船锚最初相对于发射器的位置计算实际发射方向。
    /// AnchorRotator 转动发射器后，这个方向会随之改变。
    /// </summary>
    private bool TryGetLaunchDirection(
        out Vector2 direction)
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

        if (rawDirection.sqrMagnitude <=
            0.0001f)
        {
            direction =
                Vector2.zero;

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
        Vector2 position)
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

        for (int i = 0;
             i < transform.childCount;
             i++)
        {
            Transform child =
                transform.GetChild(i);

            string lowerName =
                child.name.ToLowerInvariant();

            if (lowerName.Contains("anchor") ||
                child.name.Contains("锚"))
            {
                previewAnchor = child;
                break;
            }
        }

        if (previewAnchor == null &&
            transform.childCount > 0)
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

        if (rawDirection.sqrMagnitude <=
            0.0001f)
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
