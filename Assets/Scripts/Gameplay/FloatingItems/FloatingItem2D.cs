using UnityEngine;

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

    [Min(0f)]
    [SerializeField]
    private float lifetime = 45f;

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
    private AnchorItemDropPoint2D activeDropPoint;
    private float lifeTimer;
    private bool isBeingPulled;
    private bool hasResolved;

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
        itemRigidbody = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);

        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
        itemRigidbody.simulated = true;
<<<<<<< Updated upstream
=======

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
        Vector3 targetScale = transform.localScale;
        transform.localScale = targetScale * settings.spawnStartScale;

        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

        transform.DOScale(targetScale, settings.spawnScaleDuration)
            .SetEase(settings.spawnEase);

        DOTween.To(
            () => 0f,
            alpha => SetAlpha(alpha),
            1f,
            settings.spawnFadeDuration
        ).SetEase(Ease.InOutQuad);
>>>>>>> Stashed changes
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

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public bool TryStartAnchorPull(
        AnchorItemDropPoint2D dropPoint)
    {
        if (!CanBeCaughtByAnchor ||
            dropPoint == null)
        {
            return false;
        }

        activeDropPoint = dropPoint;
        isBeingPulled = true;

        SetColliderEnabled(false);

        if (itemRigidbody != null)
        {
            itemRigidbody.velocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }

        return true;
    }

    public void OnAnchorRetracted()
    {
        if (!isBeingPulled || hasResolved)
        {
            return;
        }

        anchorTransform = null;
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

        Vector2 targetPosition =
            activeDropPoint.DropWorldPosition;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                itemRigidbody.position,
                targetPosition,
                anchorPullSpeed * Time.fixedDeltaTime
            );

        itemRigidbody.MovePosition(nextPosition);

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
        }

        Destroy(gameObject);
    }

<<<<<<< Updated upstream
    private Transform FindPickupParent()
=======
    private void ApplyBombEffect()
    {
        if (effectData == null)
        {
            Debug.LogWarning("FloatingItemEffectData 未配置，炸弹效果无法生效。");
            return;
        }

        // TODO: 对船体造成伤害
        // 需要找到船体的健康组件并应用伤害
        Debug.Log($"炸弹爆炸！造成 {effectData.bombDamage} 点伤害");

        // 发送事件通知船体受损
        GameplayEventBus.Publish(new SubmarineHealthChangedEvent
        {
            DamageAmount = effectData.bombDamage,
            Source = "Bomb"
        });
    }

    private void ApplyWebEffect()
    {
        if (effectData == null)
        {
            Debug.LogWarning("FloatingItemEffectData 未配置，蛛网效果无法生效。");
            return;
        }

        if (activeDropPoint == null)
        {
            return;
        }

        // 找到对应的锚点组件
        AnchorLauncher2D anchor =
            activeDropPoint.GetComponentInParent<AnchorLauncher2D>();

        if (anchor != null)
        {
            // TODO: 在 AnchorLauncher2D 中添加 DisableForDuration 方法
            Debug.Log($"蛛网捕获！锚点将被禁用 {effectData.webDisableDuration} 秒");

            // 发送事件通知锚点被禁用
            GameplayEventBus.Publish(new AnchorDisabledEvent
            {
                Anchor = anchor,
                Duration = effectData.webDisableDuration
            });
        }
    }

    private void SpawnPickupItem(Vector2 worldPosition)
    {
        if (pickupPrefab == null)
        {
            return;
        }

        SubmarineInteriorFollower2D interior =
            FindInteriorFollower();

        Vector2 spawnPosition = worldPosition;

        if (interior != null)
        {
            if (interior.TryGetRandomEmptyPickupPosition(
                    out Vector2 randomPosition
                ))
            {
                spawnPosition = randomPosition;
            }
        }

        CarryableItem2D pickup =
            Instantiate(
                pickupPrefab,
                spawnPosition,
                Quaternion.identity
            );

        pickup.name =
            $"{pickupPrefab.name}_{floatingItemType}_Drop";

        EnablePickupPhysics(pickup);
    }

    private void EnablePickupPhysics(CarryableItem2D pickup)
    {
        if (pickup == null)
        {
            return;
        }

        int pickupLayer =
            LayerMask.NameToLayer("Pickup");

        if (pickupLayer >= 0)
        {
            SetLayerRecursively(
                pickup.gameObject,
                pickupLayer
            );
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

    private SubmarineInteriorFollower2D FindInteriorFollower()
>>>>>>> Stashed changes
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
            return interior;
        }

        Rigidbody2D submarineRigidbody =
            activeDropPoint.GetComponentInParent
                <Rigidbody2D>();

        if (submarineRigidbody != null)
        {
            return submarineRigidbody
                .GetComponentInChildren
                    <SubmarineInteriorFollower2D>(true);
        }

        return null;
    }

    private static void SetLayerRecursively(
        GameObject root,
        int layer)
    {
        root.layer = layer;

        Transform rootTransform = root.transform;

        for (int i = 0; i < rootTransform.childCount; i++)
        {
            SetLayerRecursively(
                rootTransform.GetChild(i).gameObject,
                layer
            );
        }
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
