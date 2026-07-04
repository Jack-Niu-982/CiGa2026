using UnityEngine;

/// <summary>
/// 可被玩家拾取、手持和放下的 2D 物品。
/// 物品自身只负责“被谁拿着”和“显示在玩家手上”，不处理具体玩法效果。
/// </summary>
[DisallowMultipleComponent]
public class CarryableItem2D : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField]
    private CarryableItemType itemType =
        CarryableItemType.Unknown;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private Sprite iconSprite;

    [Header("触发器")]
    [Tooltip("用于检测玩家靠近的 2D 触发器。留空时会自动使用本物体上的 Collider2D。")]
    [SerializeField]
    private Collider2D pickupTrigger;

    [Tooltip("手持时关闭所有 Collider2D，避免同一件物品继续触发拾取检测。")]
    [SerializeField]
    private bool disableAllCollidersWhileHeld = true;

    [Header("物理")]
    [Tooltip("留空时会自动读取本物体上的 Rigidbody2D。手持时会暂停模拟，放下时恢复。")]
    [SerializeField]
    private Rigidbody2D itemRigidbody;

    private Collider2D[] cachedColliders;
    private bool[] cachedColliderEnabledStates;
    private SpriteRenderer[] cachedSpriteRenderers;
    private bool[] cachedSpriteRendererEnabledStates;
    private bool cachedRigidbodySimulated;
    private Transform parentBeforePickup;

    private bool isHeld;
    private PlayerCarryInteractor2D currentHolder;

    public CarryableItemType ItemType => itemType;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? GetDefaultDisplayName(itemType)
            : displayName;
    public Sprite IconSprite => iconSprite;
    public bool IsHeld => isHeld;
    public PlayerCarryInteractor2D CurrentHolder => currentHolder;

    public void Configure(
        CarryableItemType newItemType,
        Collider2D newPickupTrigger,
        Rigidbody2D newItemRigidbody)
    {
        itemType = newItemType;
        pickupTrigger = newPickupTrigger;
        itemRigidbody = newItemRigidbody;

        if (pickupTrigger != null)
        {
            pickupTrigger.isTrigger = true;
        }
    }

    private void Reset()
    {
        pickupTrigger =
            GetComponent<Collider2D>();

        itemRigidbody =
            GetComponent<Rigidbody2D>();

        if (pickupTrigger != null)
        {
            pickupTrigger.isTrigger = true;
        }
    }

    private void Awake()
    {
        CacheComponents();
        EnsureIconSprite();
    }

    private void OnValidate()
    {
        if (pickupTrigger == null)
        {
            pickupTrigger =
                GetComponent<Collider2D>();
        }

        if (itemRigidbody == null)
        {
            itemRigidbody =
                GetComponent<Rigidbody2D>();
        }

        EnsureIconSprite();
    }

    public bool TryPickup(
        PlayerCarryInteractor2D holder)
    {
        if (holder == null ||
            isHeld ||
            !holder.CanPickup(this))
        {
            return false;
        }

        CacheComponents();

        parentBeforePickup =
            transform.parent;

        currentHolder = holder;
        isHeld = true;

        if (itemRigidbody != null)
        {
            cachedRigidbodySimulated =
                itemRigidbody.simulated;

            itemRigidbody.velocity =
                Vector2.zero;

            itemRigidbody.angularVelocity =
                0f;

            itemRigidbody.simulated =
                false;
        }

        ApplyHeldColliderState();
        ApplyHeldVisualState();

        holder.NotifyItemPickedUp(this);

        return true;
    }

    public void Drop(
        Vector2 worldPosition)
    {
        if (!isHeld)
        {
            return;
        }

        PlayerCarryInteractor2D previousHolder =
            currentHolder;

        transform.SetParent(
            parentBeforePickup,
            true
        );

        transform.position =
            worldPosition;

        transform.rotation =
            Quaternion.identity;

        RestorePhysicsState();
        RestoreVisualState();

        currentHolder = null;
        isHeld = false;
        parentBeforePickup = null;

        if (previousHolder != null)
        {
            previousHolder.NotifyHeldItemDropped(this);
        }
    }

    private void CacheComponents()
    {
        if (itemRigidbody == null)
        {
            itemRigidbody =
                GetComponent<Rigidbody2D>();
        }

        if (pickupTrigger == null)
        {
            pickupTrigger =
                GetComponent<Collider2D>();
        }

        cachedColliders =
            GetComponentsInChildren<Collider2D>(
                true
            );

        cachedColliderEnabledStates =
            new bool[cachedColliders.Length];

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            cachedColliderEnabledStates[i] =
                cachedColliders[i] != null &&
                cachedColliders[i].enabled;
        }

        cachedSpriteRenderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        cachedSpriteRendererEnabledStates =
            new bool[cachedSpriteRenderers.Length];

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            cachedSpriteRendererEnabledStates[i] =
                cachedSpriteRenderers[i] != null &&
                cachedSpriteRenderers[i].enabled;
        }
    }

    private void ApplyHeldColliderState()
    {
        if (disableAllCollidersWhileHeld)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled =
                        false;
                }
            }

            return;
        }

        if (pickupTrigger != null)
        {
            pickupTrigger.enabled =
                false;
        }
    }

    private void ApplyHeldVisualState()
    {
        if (cachedSpriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            if (cachedSpriteRenderers[i] != null)
            {
                cachedSpriteRenderers[i].enabled =
                    false;
            }
        }
    }

    private void RestorePhysicsState()
    {
        if (itemRigidbody != null)
        {
            itemRigidbody.simulated =
                cachedRigidbodySimulated;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] == null)
            {
                continue;
            }

            bool shouldEnable =
                i < cachedColliderEnabledStates.Length &&
                cachedColliderEnabledStates[i];

            cachedColliders[i].enabled =
                shouldEnable;
        }
    }

    private void RestoreVisualState()
    {
        if (cachedSpriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            if (cachedSpriteRenderers[i] == null)
            {
                continue;
            }

            bool shouldEnable =
                i < cachedSpriteRendererEnabledStates.Length &&
                cachedSpriteRendererEnabledStates[i];

            cachedSpriteRenderers[i].enabled =
                shouldEnable;
        }
    }

    private static string GetDefaultDisplayName(
        CarryableItemType type)
    {
        switch (type)
        {
            case CarryableItemType.Fuel:
                return "Fuel";

            case CarryableItemType.Shield:
                return "Shield";

            case CarryableItemType.Trash:
                return "Trash";

            default:
                return string.Empty;
        }
    }

    private void EnsureIconSprite()
    {
        if (iconSprite != null)
        {
            return;
        }

        SpriteRenderer spriteRenderer =
            GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            iconSprite =
                spriteRenderer.sprite;
        }
    }
}
