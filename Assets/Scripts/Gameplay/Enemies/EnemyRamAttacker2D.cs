using UnityEngine;

/// <summary>
/// 进入 Gameplay 相机后锁定在潜艇周围，并周期性后撤、冲撞、退回待机距离。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Renderer))]
public class EnemyRamAttacker2D : MonoBehaviour
{
    private const int CurrentFeedbackTuningVersion = 1;

    private enum AttackPhase
    {
        Holding,
        Retreating,
        Charging,
        Returning
    }

    [Header("目标")]
    [SerializeField] private Transform submarineTarget;
    [SerializeField] private Collider2D submarineCollider;
    [SerializeField] private Camera gameplayCamera;

    [Header("距离")]
    [Min(0.1f)]
    [SerializeField] private float holdDistance = 7f;

    [Min(0f)]
    [SerializeField] private float retreatDistance = 2f;

    [Header("攻击节奏")]
    [Min(0.1f)]
    [SerializeField] private float attackInterval = 5f;

    [Min(0.01f)]
    [SerializeField] private float retreatDuration = 0.45f;

    [Min(0.01f)]
    [SerializeField] private float chargeDuration = 0.55f;

    [Min(0.01f)]
    [SerializeField] private float returnDuration = 0.7f;

    [Min(0f)]
    [SerializeField] private float attackDamage = 25f;

    [Header("水中浮动")]
    [Tooltip("待机和移动时上下浮动的世界距离。")]
    [Min(0f)]
    [SerializeField] private float bobAmplitude = 0.55f;

    [Tooltip("平滑随机浮动的变化速度。")]
    [Min(0f)]
    [SerializeField] private float bobFrequency = 0.7f;

    [Tooltip("轻微的左右漂移幅度，让运动不只沿一条竖直线。")]
    [Min(0f)]
    [SerializeField] private float horizontalDriftAmplitude = 0.12f;

    [Tooltip("叠加细碎变化的强度。越高越不规则，但仍保持平滑。")]
    [Range(0f, 1f)]
    [SerializeField] private float bobIrregularity = 0.55f;

    [Tooltip("避免多个敌人完全同步浮动。")]
    [SerializeField] private bool randomizeBobPhase = true;

    [Header("相对位置跟随")]
    [Tooltip("待机时追向潜艇相对位置的速度。数值越小，追赶过程越明显。")]
    [Min(0.1f)]
    [SerializeField] private float relativePositionFollowSpeed = 2.5f;

    [Tooltip("待机时朝向潜艇的旋转速度。")]
    [Min(1f)]
    [SerializeField] private float relativeRotationFollowSpeed = 100f;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("距离本轮后撤还有多少秒时播放前摇。")]
    [Min(0f)]
    [SerializeField] private float windupLeadTime = 0.8f;

    [SerializeField] private AudioClip impactWindupClip;
    [SerializeField] private AudioClip impactRetreatClip;
    [SerializeField] private AudioClip impactChargeClip;
    [SerializeField] private AudioClip impactHitClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("撞击粒子")]
    [SerializeField] private bool enableImpactParticles = true;

    [Tooltip("撞击时向外飞散的红色粒子数量。")]
    [Min(1)]
    [SerializeField] private int impactParticleCount = 44;

    [Tooltip("主体粒子的直径；数值较大，用来强化敌人撞船的冲击感。")]
    [Min(0.01f)]
    [SerializeField] private float impactParticleSize = 1f;

    [Min(0.01f)]
    [SerializeField] private float impactParticleLifetime = 0.55f;

    [Min(0f)]
    [SerializeField] private float impactParticleSpeed = 4.2f;

    [SerializeField] private Color impactParticleColor =
        new Color(1f, 0.03f, 0.015f, 0.95f);

    [Tooltip("中心还会额外生成数颗大粒子，形成明显的红色爆点。")]
    [Min(0f)]
    [SerializeField] private float impactCoreSize = 2.6f;

    [Tooltip("撞击时扩张圆环的最大直径。")]
    [Min(0.1f)]
    [SerializeField] private float impactRingSize = 7f;

    [Tooltip("红色圆环从中心扩张并淡出的时间。")]
    [Min(0.01f)]
    [SerializeField] private float impactRingLifetime = 0.48f;

    [SerializeField] private string impactParticleSortingLayerName = "Default";
    [SerializeField] private int impactParticleOrderInLayer = 50;

    [Header("调试")]
    [SerializeField] private bool showDebugLog;
    [SerializeField, HideInInspector] private int feedbackTuningVersion;

    private Rigidbody2D enemyRigidbody;
    private Collider2D enemyCollider;
    private Renderer enemyRenderer;
    private Vector2 holdDirectionLocal = Vector2.right;
    private AttackPhase phase = AttackPhase.Holding;
    private float phaseTimer;
    private bool activatedByCamera;
    private bool damagedThisAttack;
    private bool windupPlayed;
    private float bobNoiseSeed;
    private ParticleSystem impactParticleSystem;
    private ParticleSystem impactRingParticleSystem;
    private Material runtimeImpactParticleMaterial;
    private Texture2D runtimeImpactParticleTexture;
    private Material runtimeImpactRingMaterial;
    private Texture2D runtimeImpactRingTexture;

    private void Awake()
    {
        ApplyFeedbackTuningMigration();

        enemyRigidbody = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        enemyRenderer = GetComponent<Renderer>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        enemyRigidbody.bodyType = RigidbodyType2D.Kinematic;
        enemyRigidbody.gravityScale = 0f;
        enemyRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        bobNoiseSeed = randomizeBobPhase
            ? Random.Range(-1000f, 1000f)
            : 0f;

        ResolveReferences();
    }

    private void Update()
    {
        if (activatedByCamera)
        {
            return;
        }

        ResolveReferences();

        if (IsInsideGameplayCamera())
        {
            ActivateAroundSubmarine();
        }
    }

    private void FixedUpdate()
    {
        if (!activatedByCamera || submarineTarget == null)
        {
            return;
        }

        phaseTimer += Time.fixedDeltaTime;

        switch (phase)
        {
            case AttackPhase.Holding:
                MoveToDistance(holdDistance);

                if (!windupPlayed &&
                    phaseTimer >= Mathf.Max(0f, attackInterval - windupLeadTime))
                {
                    windupPlayed = true;
                    PlayOneShot(impactWindupClip);
                }

                if (phaseTimer >= attackInterval)
                {
                    SetPhase(AttackPhase.Retreating);
                }
                break;

            case AttackPhase.Retreating:
                MoveToDistance(Mathf.Lerp(
                    holdDistance,
                    holdDistance + retreatDistance,
                    GetPhaseProgress(retreatDuration)));

                if (phaseTimer >= retreatDuration)
                {
                    damagedThisAttack = false;
                    SetPhase(AttackPhase.Charging);
                }
                break;

            case AttackPhase.Charging:
                MoveToDistance(Mathf.Lerp(
                    holdDistance + retreatDistance,
                    GetImpactDistance(),
                    GetPhaseProgress(chargeDuration)));

                if (phaseTimer >= chargeDuration)
                {
                    SetPhase(AttackPhase.Returning);
                }
                break;

            case AttackPhase.Returning:
                MoveToDistance(Mathf.Lerp(
                    GetImpactDistance(),
                    holdDistance,
                    GetPhaseProgress(returnDuration)));

                if (phaseTimer >= returnDuration)
                {
                    SetPhase(AttackPhase.Holding);
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (phase != AttackPhase.Charging ||
            damagedThisAttack ||
            collision == null)
        {
            return;
        }

        SubmarineDamageReceiver2D receiver =
            collision.gameObject.GetComponentInParent<SubmarineDamageReceiver2D>();

        if (receiver == null)
        {
            return;
        }

        receiver.TryApplyDamage(attackDamage);
        damagedThisAttack = true;
        PlayOneShot(impactHitClip);

        Vector2 impactPoint = transform.position;

        if (collision.contactCount > 0)
        {
            impactPoint = collision.GetContact(0).point;
        }

        PlayImpactParticles(impactPoint);
    }

    [ContextMenu("预览红色撞击粒子")]
    private void PreviewImpactParticles()
    {
        PlayImpactParticles(transform.position);
    }

    private void PlayImpactParticles(Vector2 impactPoint)
    {
        if (!enableImpactParticles || impactParticleCount <= 0)
        {
            return;
        }

        EnsureImpactParticleSystem();

        if (impactParticleSystem == null)
        {
            return;
        }

        float angleOffset = Random.Range(0f, 360f);

        for (int i = 0; i < impactParticleCount; i++)
        {
            float angle = angleOffset + 360f * i / impactParticleCount;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));

            ParticleSystem.EmitParams emitParams =
                new ParticleSystem.EmitParams
                {
                    position = new Vector3(
                        impactPoint.x,
                        impactPoint.y,
                        transform.position.z),
                    velocity = direction * impactParticleSpeed *
                        Random.Range(0.65f, 1.25f),
                    startLifetime = impactParticleLifetime *
                        Random.Range(0.8f, 1.2f),
                    startSize = impactParticleSize *
                        Random.Range(0.65f, 1.35f),
                    startColor = impactParticleColor
                };

            impactParticleSystem.Emit(emitParams, 1);
        }

        Color coreColor = impactParticleColor;
        coreColor.a = Mathf.Min(1f, coreColor.a + 0.05f);

        for (int i = 0; i < 5; i++)
        {
            ParticleSystem.EmitParams coreParams =
                new ParticleSystem.EmitParams
                {
                    position = new Vector3(
                        impactPoint.x,
                        impactPoint.y,
                        transform.position.z),
                    velocity = Random.insideUnitCircle *
                        impactParticleSpeed * 0.22f,
                    startLifetime = impactParticleLifetime * 0.65f,
                    startSize = impactCoreSize * Random.Range(0.75f, 1.15f),
                    startColor = coreColor
                };

            impactParticleSystem.Emit(coreParams, 1);
        }

        if (impactRingParticleSystem != null)
        {
            ParticleSystem.EmitParams ringParams =
                new ParticleSystem.EmitParams
                {
                    position = new Vector3(
                        impactPoint.x,
                        impactPoint.y,
                        transform.position.z),
                    velocity = Vector3.zero,
                    startLifetime = impactRingLifetime,
                    startSize = impactRingSize,
                    startColor = impactParticleColor
                };

            impactRingParticleSystem.Emit(ringParams, 1);
        }
    }

    private void EnsureImpactParticleSystem()
    {
        if (impactParticleSystem != null)
        {
            return;
        }

        GameObject particleObject = new GameObject("Enemy Impact Particles");
        particleObject.transform.SetParent(transform, false);

        impactParticleSystem = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = impactParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startLifetime = impactParticleLifetime;
        main.startSize = impactParticleSize;
        main.maxParticles = Mathf.Max(128, impactParticleCount * 4);

        ParticleSystem.EmissionModule emission = impactParticleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = impactParticleSystem.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            impactParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.7f, 0.08f, 0.03f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
            impactParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.12f, 1f),
                new Keyframe(1f, 0f)));

        ParticleSystemRenderer particleRenderer =
            particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = CreateImpactParticleMaterial();
        particleRenderer.sortingLayerName = impactParticleSortingLayerName;
        particleRenderer.sortingOrder = impactParticleOrderInLayer;

        GameObject ringObject = new GameObject("Enemy Impact Ring");
        ringObject.transform.SetParent(transform, false);

        impactRingParticleSystem = ringObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule ringMain = impactRingParticleSystem.main;
        ringMain.loop = false;
        ringMain.playOnAwake = false;
        ringMain.simulationSpace = ParticleSystemSimulationSpace.World;
        ringMain.startSpeed = 0f;
        ringMain.startLifetime = impactRingLifetime;
        ringMain.startSize = impactRingSize;
        ringMain.maxParticles = 8;

        ParticleSystem.EmissionModule ringEmission =
            impactRingParticleSystem.emission;
        ringEmission.enabled = false;

        ParticleSystem.ShapeModule ringShape = impactRingParticleSystem.shape;
        ringShape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule ringColor =
            impactRingParticleSystem.colorOverLifetime;
        ringColor.enabled = true;
        ringColor.color = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.18f, 0.08f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

        ParticleSystem.SizeOverLifetimeModule ringSize =
            impactRingParticleSystem.sizeOverLifetime;
        ringSize.enabled = true;
        ringSize.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.18f),
                new Keyframe(0.18f, 0.72f),
                new Keyframe(1f, 1f)));

        ParticleSystemRenderer ringRenderer =
            ringObject.GetComponent<ParticleSystemRenderer>();
        ringRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        ringRenderer.sharedMaterial = CreateImpactRingMaterial();
        ringRenderer.sortingLayerName = impactParticleSortingLayerName;
        ringRenderer.sortingOrder = impactParticleOrderInLayer + 1;
    }

    private Material CreateImpactParticleMaterial()
    {
        if (runtimeImpactParticleMaterial != null)
        {
            return runtimeImpactParticleMaterial;
        }

        Shader particleShader = Shader.Find("Sprites/Default");

        if (particleShader == null)
        {
            Debug.LogWarning(
                $"[Enemy Ram] {name} 找不到 Sprites/Default Shader，撞击粒子可能无法显示。",
                this);
            return null;
        }

        const int textureSize = 32;
        runtimeImpactParticleTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false)
        {
            name = "Enemy Impact Round Particle Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = Vector2.one * ((textureSize - 1) * 0.5f);
        float radius = textureSize * 0.48f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = 1f - Mathf.SmoothStep(0.68f, 1f, distance);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeImpactParticleTexture.SetPixels(pixels);
        runtimeImpactParticleTexture.Apply(false, true);

        runtimeImpactParticleMaterial = new Material(particleShader)
        {
            name = "Enemy Impact Particle Material",
            mainTexture = runtimeImpactParticleTexture
        };

        return runtimeImpactParticleMaterial;
    }

    private Material CreateImpactRingMaterial()
    {
        if (runtimeImpactRingMaterial != null)
        {
            return runtimeImpactRingMaterial;
        }

        Shader particleShader = Shader.Find("Sprites/Default");

        if (particleShader == null)
        {
            return null;
        }

        const int textureSize = 64;
        runtimeImpactRingTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false)
        {
            name = "Enemy Impact Ring Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = Vector2.one * ((textureSize - 1) * 0.5f);
        float radius = textureSize * 0.43f;
        float ringWidth = textureSize * 0.085f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float distanceFromRing = Mathf.Abs(distance - radius);
                float alpha = 1f - Mathf.SmoothStep(
                    ringWidth * 0.35f,
                    ringWidth,
                    distanceFromRing);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeImpactRingTexture.SetPixels(pixels);
        runtimeImpactRingTexture.Apply(false, true);

        runtimeImpactRingMaterial = new Material(particleShader)
        {
            name = "Enemy Impact Ring Material",
            mainTexture = runtimeImpactRingTexture
        };

        return runtimeImpactRingMaterial;
    }

    private void OnDestroy()
    {
        DestroyRuntimeObject(runtimeImpactParticleMaterial);
        DestroyRuntimeObject(runtimeImpactParticleTexture);
        DestroyRuntimeObject(runtimeImpactRingMaterial);
        DestroyRuntimeObject(runtimeImpactRingTexture);
    }

    private void DestroyRuntimeObject(Object runtimeObject)
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

    private void ResolveReferences()
    {
        if (submarineTarget == null)
        {
            SubmarineHealth2D health = FindObjectOfType<SubmarineHealth2D>();
            submarineTarget = health != null ? health.transform : null;
        }

        if (submarineCollider == null && submarineTarget != null)
        {
            submarineCollider = submarineTarget.GetComponent<Collider2D>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    private bool IsInsideGameplayCamera()
    {
        if (gameplayCamera == null || enemyRenderer == null)
        {
            return false;
        }

        Vector3 viewportPoint =
            gameplayCamera.WorldToViewportPoint(enemyRenderer.bounds.center);

        return viewportPoint.z > 0f &&
               viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
               viewportPoint.y >= 0f && viewportPoint.y <= 1f;
    }

    private void ActivateAroundSubmarine()
    {
        if (submarineTarget == null)
        {
            return;
        }

        Vector2 worldDirection =
            (Vector2)transform.position - (Vector2)submarineTarget.position;

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            worldDirection = Vector2.right;
        }

        float targetAngle = submarineTarget.eulerAngles.z;
        holdDirectionLocal = Rotate(worldDirection.normalized, -targetAngle);
        activatedByCamera = true;
        SetPhase(AttackPhase.Holding);
        MoveToDistance(holdDistance);

        if (showDebugLog)
        {
            Debug.Log($"[Enemy Ram] {name} 进入镜头，开始锁定潜艇。", this);
        }
    }

    private void MoveToDistance(float distance)
    {
        Vector2 direction = GetWorldAttackDirection();
        Vector2 targetPosition =
            (Vector2)submarineTarget.position + direction * distance;

        targetPosition += GetCurrentBobOffset();

        Vector2 nextPosition = targetPosition;

        if (phase == AttackPhase.Holding)
        {
            nextPosition = Vector2.MoveTowards(
                enemyRigidbody.position,
                targetPosition,
                relativePositionFollowSpeed * Time.fixedDeltaTime);
        }

        enemyRigidbody.MovePosition(nextPosition);

        float targetAngle = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        float nextAngle = targetAngle;

        if (phase == AttackPhase.Holding)
        {
            nextAngle = Mathf.MoveTowardsAngle(
                enemyRigidbody.rotation,
                targetAngle,
                relativeRotationFollowSpeed * Time.fixedDeltaTime);
        }

        enemyRigidbody.MoveRotation(nextAngle);
    }

    private Vector2 GetCurrentBobOffset()
    {
        if (bobAmplitude <= 0f || bobFrequency <= 0f)
        {
            return Vector2.zero;
        }

        float bobWeight = 1f;

        if (phase == AttackPhase.Charging)
        {
            // 冲撞末端回到准确攻击线，避免浮动造成擦碰或漏撞。
            bobWeight = 1f - GetPhaseProgress(chargeDuration);
        }

        float time = Time.time * bobFrequency;

        float slowY = SignedPerlin(
            bobNoiseSeed + 11.3f,
            time * 0.52f);

        float mediumY = SignedPerlin(
            bobNoiseSeed + 47.8f,
            time * 1.31f);

        float detailY = SignedPerlin(
            bobNoiseSeed + 103.6f,
            time * 2.73f);

        float detailWeight = bobIrregularity;
        float vertical =
            (slowY + mediumY * 0.55f * detailWeight +
             detailY * 0.25f * detailWeight) /
            (1f + 0.8f * detailWeight);

        float horizontal = SignedPerlin(
            bobNoiseSeed + 211.9f,
            time * 0.67f);

        return new Vector2(
            horizontal * horizontalDriftAmplitude,
            vertical * bobAmplitude) * bobWeight;
    }

    private static float SignedPerlin(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
    }

    private float GetImpactDistance()
    {
        Vector2 direction = GetWorldAttackDirection();
        float submarineRadius = 2f;

        if (submarineCollider != null)
        {
            Vector2 samplePoint =
                (Vector2)submarineTarget.position + direction * 100f;
            Vector2 hullPoint = submarineCollider.ClosestPoint(samplePoint);
            submarineRadius = Vector2.Distance(
                submarineTarget.position,
                hullPoint);
        }

        float enemyRadius = enemyCollider != null
            ? Mathf.Max(enemyCollider.bounds.extents.x, enemyCollider.bounds.extents.y)
            : 0.5f;

        return Mathf.Max(0.1f, submarineRadius + enemyRadius * 0.8f);
    }

    private Vector2 GetWorldAttackDirection()
    {
        return Rotate(holdDirectionLocal, submarineTarget.eulerAngles.z).normalized;
    }

    private float GetPhaseProgress(float duration)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseTimer / duration));
    }

    private void SetPhase(AttackPhase nextPhase)
    {
        phase = nextPhase;
        phaseTimer = 0f;

        switch (nextPhase)
        {
            case AttackPhase.Holding:
                windupPlayed = false;
                break;

            case AttackPhase.Retreating:
                PlayOneShot(impactRetreatClip);
                break;

            case AttackPhase.Charging:
                PlayOneShot(impactChargeClip);
                break;
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    private static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            value.x * cos - value.y * sin,
            value.x * sin + value.y * cos);
    }

    private void ApplyFeedbackTuningMigration()
    {
        if (feedbackTuningVersion >= CurrentFeedbackTuningVersion)
        {
            return;
        }

        bobAmplitude = Mathf.Max(bobAmplitude, 0.55f);
        horizontalDriftAmplitude = Mathf.Max(horizontalDriftAmplitude, 0.12f);
        relativePositionFollowSpeed = 2.5f;
        relativeRotationFollowSpeed = 100f;
        impactParticleCount = Mathf.Max(impactParticleCount, 44);
        impactParticleSize = Mathf.Max(impactParticleSize, 1f);
        impactCoreSize = Mathf.Max(impactCoreSize, 2.6f);
        impactRingSize = Mathf.Max(impactRingSize, 7f);
        impactRingLifetime = Mathf.Max(impactRingLifetime, 0.48f);
        feedbackTuningVersion = CurrentFeedbackTuningVersion;
    }

    private void OnValidate()
    {
        ApplyFeedbackTuningMigration();

        holdDistance = Mathf.Max(0.1f, holdDistance);
        retreatDistance = Mathf.Max(0f, retreatDistance);
        attackInterval = Mathf.Max(0.1f, attackInterval);
        retreatDuration = Mathf.Max(0.01f, retreatDuration);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        returnDuration = Mathf.Max(0.01f, returnDuration);
        attackDamage = Mathf.Max(0f, attackDamage);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
        horizontalDriftAmplitude = Mathf.Max(0f, horizontalDriftAmplitude);
        bobIrregularity = Mathf.Clamp01(bobIrregularity);
        relativePositionFollowSpeed = Mathf.Max(0.1f, relativePositionFollowSpeed);
        relativeRotationFollowSpeed = Mathf.Max(1f, relativeRotationFollowSpeed);
        windupLeadTime = Mathf.Max(0f, windupLeadTime);
        audioVolume = Mathf.Clamp01(audioVolume);
        impactParticleCount = Mathf.Max(1, impactParticleCount);
        impactParticleSize = Mathf.Max(0.01f, impactParticleSize);
        impactParticleLifetime = Mathf.Max(0.01f, impactParticleLifetime);
        impactParticleSpeed = Mathf.Max(0f, impactParticleSpeed);
        impactCoreSize = Mathf.Max(0f, impactCoreSize);
        impactRingSize = Mathf.Max(0.1f, impactRingSize);
        impactRingLifetime = Mathf.Max(0.01f, impactRingLifetime);
    }
}
