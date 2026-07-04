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
    [SerializeField] private float bobAmplitude = 0.18f;

    [Tooltip("平滑随机浮动的变化速度。")]
    [Min(0f)]
    [SerializeField] private float bobFrequency = 0.7f;

    [Tooltip("轻微的左右漂移幅度，让运动不只沿一条竖直线。")]
    [Min(0f)]
    [SerializeField] private float horizontalDriftAmplitude = 0.06f;

    [Tooltip("叠加细碎变化的强度。越高越不规则，但仍保持平滑。")]
    [Range(0f, 1f)]
    [SerializeField] private float bobIrregularity = 0.55f;

    [Tooltip("避免多个敌人完全同步浮动。")]
    [SerializeField] private bool randomizeBobPhase = true;

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

    [Header("调试")]
    [SerializeField] private bool showDebugLog;

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

    private void Awake()
    {
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

        enemyRigidbody.MovePosition(targetPosition);

        float angle = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        enemyRigidbody.MoveRotation(angle);
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

    private void OnValidate()
    {
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
        windupLeadTime = Mathf.Max(0f, windupLeadTime);
        audioVolume = Mathf.Clamp01(audioVolume);
    }
}
