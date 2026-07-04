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
    private FloatingItemType floatingItemType =
        FloatingItemType.Unknown;

    [Tooltip("漂浮物效果配置（炸弹伤害、蛛网禁用时长）。")]
    [SerializeField]
    private FloatingItemEffectData effectData;

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

    public FloatingItemType FloatingType => floatingItemType;
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
            dropPoint == null ||
            anchor == null)
        {
            return false;
        }

        activeDropPoint = dropPoint;
        anchorTransform = anchor;
        isBeingPulled = true;

        // 停止闪烁动画
        if (blinkSequence != null && blinkSequence.IsActive())
        {
            blinkSequence.Kill();
        }

        // 重置透明度
        SetAlpha(1f);

        // 成为锚的子对象，跟随锚一起移动
        transform.SetParent(anchor, true);

        SetColliderEnabled(false);

        if (itemRigidbody != null)
        {
            itemRigidbody.velocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
            itemRigidbody.simulated = false;
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
        // 漂浮物已成为锚的子对象，跟随锚移动
        // 不需要自己移动，等待锚收回完成后调用 TriggerEffect
    }

    /// <summary>
    /// 锚收回完成时调用，触发漂浮物效果并销毁。
    /// </summary>
    public void TriggerEffectOnAnchorReturn()
    {
        if (hasResolved)
        {
            return;
        }

        if (activeDropPoint == null)
        {
            ResolveWithoutPickup();
            return;
        }

        SpawnPickupAndDestroy(activeDropPoint.DropWorldPosition);
    }

    private void SpawnPickupAndDestroy(
        Vector2 worldPosition)
    {
        if (hasResolved)
        {
            return;
        }

        hasResolved = true;

        // 根据漂浮物类型执行不同的效果
        switch (floatingItemType)
        {
            case FloatingItemType.Bomb:
                ApplyBombEffect();
                break;

            case FloatingItemType.Web:
                ApplyWebEffect();
                break;

            case FloatingItemType.Fuel:
            case FloatingItemType.Shield:
            case FloatingItemType.Trash:
                SpawnPickupItem(worldPosition);
                break;
        }

        Destroy(gameObject);
    }

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
        // 从 SettingManager 获取统一的拾取物 Prefab
        CarryableItem2D pickupPrefab = SettingManager.CarryableItemArt?.GetPickupPrefab();

        if (pickupPrefab == null)
        {
            Debug.LogWarning(
                $"[FloatingItem2D] {name} 无法从 SettingManager 获取拾取物 Prefab。",
                this
            );
            return;
        }

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

        // 根据漂浮物类型设置拾取物类型
        CarryableItemType carryableType = ConvertToCarryableType(floatingItemType);
        pickup.name = $"{carryableType}Pickup_FromFloating";
        pickup.SetItemType(carryableType);

        EnablePickupPhysics(pickup);
    }

    private CarryableItemType ConvertToCarryableType(FloatingItemType floatingType)
    {
        switch (floatingType)
        {
            case FloatingItemType.Fuel:
                return CarryableItemType.Fuel;

            case FloatingItemType.Shield:
                return CarryableItemType.Shield;

            case FloatingItemType.Trash:
                return CarryableItemType.Trash;

            default:
                Debug.LogWarning($"[FloatingItem2D] 无法将 {floatingType} 转换为 CarryableItemType。");
                return CarryableItemType.Unknown;
        }
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
            pickupRigidbody.gravityScale = 0.3f;
            pickupRigidbody.drag = 0.5f;
            pickupRigidbody.velocity = Vector2.zero;
            pickupRigidbody.angularVelocity = 0f;
        }

        int carryableLayer = LayerMask.NameToLayer("CarryableItem");
        if (carryableLayer >= 0)
        {
            pickup.gameObject.layer = carryableLayer;
        }
    }

    private Transform FindPickupParent()
    {
        if (activeDropPoint == null)
        {
            return null;
        }

        // 1. 查找名为 "PickupContainer" 的容器
        GameObject pickupContainer =
            GameObject.Find("PickupContainer");

        if (pickupContainer != null)
        {
            return pickupContainer.transform;
        }

        // 2. 查找 SubmarineInterior 层且没有 Rigidbody2D 的父对象
        // 避免将 Dynamic Rigidbody2D 设为 Dynamic Rigidbody2D 的子对象
        Transform parent =
            activeDropPoint.transform;

        int submarineInteriorLayer =
            LayerMask.NameToLayer(
                "SubmarineInterior"
            );

        while (parent != null)
        {
            if (parent.gameObject.layer ==
                    submarineInteriorLayer &&
                parent.GetComponent<Rigidbody2D>() ==
                    null)
            {
                return parent;
            }

            parent = parent.parent;
        }

        // 3. 不设置父对象，避免物理冲突
        return null;
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
        FloatingItemType newFloatingType,
        Vector2 newDriftVelocity)
    {
        floatingItemType = newFloatingType;
        driftVelocity = newDriftVelocity;
    }

    public void SetDriftVelocity(
        Vector2 newDriftVelocity)
    {
        driftVelocity = newDriftVelocity;
    }
}
