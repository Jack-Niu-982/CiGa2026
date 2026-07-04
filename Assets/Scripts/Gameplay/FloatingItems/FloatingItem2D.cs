using UnityEngine;
using DG.Tweening;

/// <summary>
/// 船外漂浮物。负责漂移、被锚拉回船体，并在抵达后生成玩家可拾取物。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class FloatingItem2D : MonoBehaviour
{
    [Header("物品")]
    [SerializeField]
    private CarryableItemType itemType =
        CarryableItemType.Unknown;

    [Tooltip("抵达锚位后生成的玩家可拾取物 Prefab。")]
    [SerializeField]
    private CarryableItem2D pickupPrefab;

    [Header("漂移")]
    [SerializeField]
    private Vector2 driftVelocity =
        new Vector2(-0.35f, 0f);

    [SerializeField]
    private float angularSpeed = 20f;

    [Header("锚拉回")]
    [SerializeField]
    private bool canBeCaughtByAnchor = true;

    [Min(0.01f)]
    [SerializeField]
    private float anchorPullSpeed = 5f;

    [Min(0.01f)]
    [SerializeField]
    private float arrivalDistance = 0.12f;

    private Rigidbody2D itemRigidbody;
    private Collider2D[] colliders;
    private SpriteRenderer[] spriteRenderers;
    private AnchorItemDropPoint2D activeDropPoint;
    private Transform anchorTransform;
    private float lifeTimer;
    private float lifetime;
    private bool isBeingPulled;
    private bool hasResolved;
    private bool isBlinking;
    private Sequence blinkSequence;

    public CarryableItemType ItemType => itemType;
    public bool CanBeCaughtByAnchor => canBeCaughtByAnchor && !isBeingPulled && !hasResolved;

    private void Reset()
    {
        itemRigidbody = GetComponent<Rigidbody2D>();
        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
    }

    private void Awake()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        itemRigidbody = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
        itemRigidbody.simulated = true;

        if (settings != null)
        {
            lifetime = Random.Range(
                settings.minLifetime,
                settings.maxLifetime
            );

            PlaySpawnAnimation(settings);
        }
        else
        {
            lifetime = 45f;
        }
    }

    private void PlaySpawnAnimation(FloatingItemSettings settings)
    {
        transform.localScale = Vector3.one * settings.spawnStartScale;

        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

        transform.DOScale(Vector3.one, settings.spawnScaleDuration)
            .SetEase(settings.spawnEase);

        DOTween.To(
            () => 0f,
            alpha => SetAlpha(alpha),
            1f,
            settings.spawnFadeDuration
        ).SetEase(Ease.InOutQuad);
    }

    private void FixedUpdate()
    {
        if (hasResolved)
        {
            return;
        }

        if (isBeingPulled)
        {
            UpdateAnchorPull();
            return;
        }

        UpdateDrift();
    }

    private void Update()
    {
        if (lifetime <= 0f ||
            isBeingPulled ||
            hasResolved)
        {
            return;
        }

        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings == null)
        {
            return;
        }

        lifeTimer += Time.deltaTime;

        float remainingTime = lifetime - lifeTimer;

        if (!isBlinking && remainingTime <= settings.blinkStartTime)
        {
            StartBlinking(settings);
        }

        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void StartBlinking(FloatingItemSettings settings)
    {
        isBlinking = true;

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        blinkSequence = DOTween.Sequence();

        for (int i = 0; i < settings.blinkCount; i++)
        {
            blinkSequence.Append(
                DOTween.To(
                    () => spriteRenderers[0].color.a,
                    alpha => SetAlpha(alpha),
                    settings.minAlpha,
                    settings.blinkDuration * 0.5f
                ).SetEase(Ease.InOutSine)
            );

            if (i < settings.blinkCount - 1)
            {
                blinkSequence.Append(
                    DOTween.To(
                        () => spriteRenderers[0].color.a,
                        alpha => SetAlpha(alpha),
                        settings.maxAlpha,
                        settings.blinkDuration * 0.5f
                    ).SetEase(Ease.InOutSine)
                );
            }
        }

        blinkSequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color color = spriteRenderers[i].color;
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }
    }

    private void OnDestroy()
    {
        if (blinkSequence != null && blinkSequence.IsActive())
        {
            blinkSequence.Kill();
        }
    }

    public bool TryStartAnchorPull(
        AnchorItemDropPoint2D dropPoint,
        Transform anchor)
    {
        if (!CanBeCaughtByAnchor ||
            dropPoint == null)
        {
            return false;
        }

        activeDropPoint = dropPoint;
        anchorTransform = anchor;
        isBeingPulled = true;

        SetColliderEnabled(false);

        if (itemRigidbody != null)
        {
            itemRigidbody.velocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }

        return true;
    }

    private void UpdateDrift()
    {
        Vector2 nextPosition =
            itemRigidbody.position +
            driftVelocity * Time.fixedDeltaTime;

        itemRigidbody.MovePosition(nextPosition);

        if (!Mathf.Approximately(angularSpeed, 0f))
        {
            itemRigidbody.MoveRotation(
                itemRigidbody.rotation +
                angularSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void UpdateAnchorPull()
    {
        if (activeDropPoint == null)
        {
            ResolveWithoutPickup();
            return;
        }

        Vector2 targetPosition;

        if (anchorTransform != null)
        {
            targetPosition = anchorTransform.position;
        }
        else
        {
            targetPosition = activeDropPoint.DropWorldPosition;
        }

        Vector2 nextPosition =
            Vector2.MoveTowards(
                itemRigidbody.position,
                targetPosition,
                anchorPullSpeed * Time.fixedDeltaTime
            );

        itemRigidbody.MovePosition(nextPosition);

        if (anchorTransform == null)
        {
            float remainingDistance =
                Vector2.Distance(
                    nextPosition,
                    targetPosition
                );

            if (remainingDistance <= arrivalDistance)
            {
                SpawnPickupAndDestroy(targetPosition);
            }
        }
        else
        {
            float distanceToAnchor =
                Vector2.Distance(
                    nextPosition,
                    (Vector2)anchorTransform.position
                );

            if (distanceToAnchor <= arrivalDistance * 2f)
            {
                float distanceToDropPoint =
                    Vector2.Distance(
                        (Vector2)anchorTransform.position,
                        activeDropPoint.DropWorldPosition
                    );

                if (distanceToDropPoint <= arrivalDistance * 3f)
                {
                    SpawnPickupAndDestroy(activeDropPoint.DropWorldPosition);
                }
            }
        }
    }

    private void SpawnPickupAndDestroy(
        Vector2 worldPosition)
    {
        if (hasResolved)
        {
            return;
        }

        hasResolved = true;

        if (pickupPrefab != null)
        {
            CarryableItem2D pickup =
                Instantiate(
                    pickupPrefab,
                    worldPosition,
                    Quaternion.identity
                );

            Transform pickupParent =
                FindPickupParent();

            if (pickupParent != null)
            {
                pickup.transform.SetParent(
                    pickupParent,
                    true
                );
            }

            pickup.name =
                $"{pickupPrefab.name}_{itemType}_Drop";

            EnablePickupPhysics(pickup);
        }

        Destroy(gameObject);
    }

    private void EnablePickupPhysics(CarryableItem2D pickup)
    {
        if (pickup == null)
        {
            return;
        }

        Rigidbody2D pickupRigidbody =
            pickup.GetComponent<Rigidbody2D>();

        if (pickupRigidbody != null)
        {
            pickupRigidbody.bodyType = RigidbodyType2D.Dynamic;
            pickupRigidbody.simulated = true;
            pickupRigidbody.gravityScale = 1f;
            pickupRigidbody.velocity = Vector2.zero;
            pickupRigidbody.angularVelocity = 0f;
        }
    }

    private Transform FindPickupParent()
    {
        if (activeDropPoint == null)
        {
            return null;
        }

        SubmarineInteriorFollower2D interior =
            activeDropPoint.GetComponentInParent
                <SubmarineInteriorFollower2D>();

        if (interior != null)
        {
            return interior.transform;
        }

        Rigidbody2D parentRigidbody =
            activeDropPoint.GetComponentInParent
                <Rigidbody2D>();

        if (parentRigidbody != null)
        {
            return parentRigidbody.transform;
        }

        return activeDropPoint.transform;
    }

    private void ResolveWithoutPickup()
    {
        hasResolved = true;
        Destroy(gameObject);
    }

    private void SetColliderEnabled(
        bool enabledState)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabledState;
            }
        }
    }

    public void Configure(
        CarryableItemType newItemType,
        CarryableItem2D newPickupPrefab,
        Vector2 newDriftVelocity)
    {
        itemType = newItemType;
        pickupPrefab = newPickupPrefab;
        driftVelocity = newDriftVelocity;
    }

    public void SetDriftVelocity(
        Vector2 newDriftVelocity)
    {
        driftVelocity = newDriftVelocity;
    }
}
