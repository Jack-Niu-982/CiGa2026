using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家身上的拾取与手持交互器。
/// 读取当前玩家自己的 PickUpPressed：空手时拾取最近物品，手持时再次按键放下。
/// </summary>
[DisallowMultipleComponent]
public class PlayerCarryInteractor2D : MonoBehaviour
{
    [Header("玩家输入")]
    [Tooltip("留空时优先读取 PlayerController.CurrentInput。")]
    [SerializeField]
    private PlayerController playerController;

    [Tooltip("没有 PlayerController 或 CurrentInput 时使用的备用输入。")]
    [SerializeField]
    private PlayerInputBase fallbackInput;

    [Tooltip("玩家正在操作锚点或其他设施时，拾取逻辑会暂停，避免拾取动作干扰设施操作。")]
    [SerializeField]
    private PlayerOperateInteractor2D operateInteractor;

    [Header("手持位置")]
    [Tooltip("旧版手持挂点。当前真实物品不会挂到玩家身上，只保留给放下位置和兼容逻辑。")]
    [SerializeField]
    private Transform holdParent;

    [SerializeField]
    private Vector2 holdLocalOffset =
        new Vector2(0.35f, 0.15f);

    [Tooltip("玩家头顶显示当前携带物品图标的 SpriteRenderer。通常是玩家 Prefab 下的 PickupSprite。")]
    [SerializeField]
    private SpriteRenderer pickupSpriteRenderer;

    [SerializeField]
    private PlayerPutInInteractor2D putInInteractor;

    [Header("拾取检测")]
    [Tooltip("拾取检测圆会以玩家 Transform 为圆心，并在玩家碰撞半径外额外增加这段距离。")]
    [Min(0f)]
    [SerializeField]
    private float pickupDetectionPadding = 0.1f;

    [Tooltip("找不到玩家实体碰撞体时使用的备用玩家半径。")]
    [Min(0.01f)]
    [SerializeField]
    private float fallbackPlayerRadius = 0.13f;

    [Header("放下位置")]
    [SerializeField]
    private Vector2 dropLocalOffset =
        new Vector2(0.55f, 0f);

    private readonly Dictionary<CarryableItem2D, int>
        nearbyItems =
            new Dictionary<CarryableItem2D, int>();

    private CarryableItem2D heldItem;
    private Collider2D playerBodyCollider;

    public CarryableItem2D HeldItem => heldItem;
    public bool HasHeldItem => heldItem != null;

    public bool HasPickUpOption =>
        heldItem == null &&
        !IsBusyOperating() &&
        GetClosestPickupItem() != null;

    public event Action<PlayerCarryInteractor2D, CarryableItem2D>
        HeldItemChanged;

    public CarryableItemType HeldItemType =>
        heldItem != null
            ? heldItem.ItemType
            : CarryableItemType.Unknown;

    public Transform HoldParent =>
        holdParent != null
            ? holdParent
            : transform;

    public Vector2 HoldLocalOffset =>
        holdLocalOffset;

    private void Reset()
    {
        playerController =
            GetComponent<PlayerController>();

        fallbackInput =
            GetComponent<PlayerInputBase>();

        operateInteractor =
            GetComponent<PlayerOperateInteractor2D>();

        pickupSpriteRenderer =
            FindPickupSpriteRenderer();
    }

    private void Awake()
    {
        if (playerController == null)
        {
            playerController =
                GetComponent<PlayerController>();
        }

        if (fallbackInput == null)
        {
            fallbackInput =
                GetComponent<PlayerInputBase>();
        }

        if (operateInteractor == null)
        {
            operateInteractor =
                GetComponent<PlayerOperateInteractor2D>();
        }

        if (pickupSpriteRenderer == null)
        {
            pickupSpriteRenderer =
                FindPickupSpriteRenderer();
        }

        if (putInInteractor == null)
        {
            putInInteractor =
                GetComponent<PlayerPutInInteractor2D>();
        }

        ResolvePlayerBodyCollider();

        RefreshPickupSprite();
    }

    private void Update()
    {
        PlayerInputBase currentInput =
            GetCurrentInput();

        bool isBusy =
            IsBusyOperating();

        if (currentInput == null ||
            isBusy ||
            !currentInput.PickUpPressed)
        {
            return;
        }

        if (currentInput.PutInPressed &&
            putInInteractor != null &&
            putInInteractor.HasPutInOption)
        {
            return;
        }

        CarryableItem2D closestItem =
            heldItem == null
                ? GetClosestPickupItem()
                : null;

        if (!(closestItem != null &&
              TryPickupItem(closestItem)) &&
            heldItem != null)
        {
            TryDropHeldItem();
        }
    }

    public bool CanPickup(
        CarryableItem2D item)
    {
        return
            item != null &&
            heldItem == null &&
            heldItem != item &&
            !item.IsHeld;
    }

    public bool HasItemType(
        CarryableItemType itemType)
    {
        return
            heldItem != null &&
            heldItem.ItemType == itemType;
    }

    public bool TryPickupItem(
        CarryableItem2D item)
    {
        if (!CanPickup(item))
        {
            return false;
        }

        return item.TryPickup(this);
    }

    public bool TryDropHeldItem()
    {
        if (heldItem == null)
        {
            return false;
        }

        Vector2 dropPosition =
            transform.TransformPoint(
                dropLocalOffset
            );

        heldItem.Drop(dropPosition);

        return true;
    }

    public bool TryConsumeHeldItem(
        ICarryItemReceiver receiver)
    {
        if (receiver == null ||
            heldItem == null ||
            !receiver.CanReceiveCarryItem(
                this,
                heldItem
            ))
        {
            return false;
        }

        return receiver.TryReceiveCarryItem(
            this,
            heldItem
        );
    }

    internal void NotifyItemPickedUp(
        CarryableItem2D item)
    {
        if (item == null)
        {
            return;
        }

        heldItem = item;
        nearbyItems.Remove(item);
        RefreshPickupSprite();
        HeldItemChanged?.Invoke(this, heldItem);
    }

    internal void NotifyHeldItemDropped(
        CarryableItem2D item)
    {
        if (heldItem == item)
        {
            heldItem = null;
            RefreshPickupSprite();
            HeldItemChanged?.Invoke(this, null);
        }
    }

    private PlayerInputBase GetCurrentInput()
    {
        if (playerController != null &&
            playerController.CurrentInput != null)
        {
            return playerController.CurrentInput;
        }

        return fallbackInput;
    }

    private bool IsBusyOperating()
    {
        if (playerController != null &&
            playerController.IsOperating)
        {
            return true;
        }

        return
            operateInteractor != null &&
            operateInteractor.CurrentOperateController != null;
    }

    private bool TryPickupClosestItem()
    {
        CarryableItem2D closestItem =
            GetClosestPickupItem();

        if (closestItem == null)
        {
            return false;
        }

        return TryPickupItem(
            closestItem
        );
    }

    private CarryableItem2D GetClosestPickupItem()
    {
        CarryableItem2D closestItem =
            null;

        float closestSqrDistance =
            float.MaxValue;

        float detectionRadius =
            GetPickupDetectionRadius();

        Collider2D[] overlaps =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRadius
            );

        HashSet<CarryableItem2D> checkedItems =
            new HashSet<CarryableItem2D>();

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];

            CarryableItem2D item =
                overlap != null
                    ? overlap.GetComponentInParent
                        <CarryableItem2D>()
                    : null;

            if (item == null ||
                item.IsHeld ||
                !checkedItems.Add(item))
            {
                continue;
            }

            float sqrDistance =
                (
                    item.transform.position -
                    transform.position
                ).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance =
                sqrDistance;

            closestItem =
                item;
        }

        return closestItem;
    }

    private float GetPickupDetectionRadius()
    {
        if (playerBodyCollider == null)
        {
            ResolvePlayerBodyCollider();
        }

        float playerRadius =
            fallbackPlayerRadius;

        if (playerBodyCollider != null)
        {
            Vector3 extents =
                playerBodyCollider.bounds.extents;

            playerRadius =
                Mathf.Max(extents.x, extents.y);
        }

        return playerRadius +
               pickupDetectionPadding;
    }

    private void ResolvePlayerBodyCollider()
    {
        Collider2D[] colliders =
            GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider != null &&
                !collider.isTrigger)
            {
                playerBodyCollider = collider;
                return;
            }
        }
    }

    private void RemoveInvalidNearbyItems()
    {
        List<CarryableItem2D> invalidItems =
            null;

        foreach (CarryableItem2D item in nearbyItems.Keys)
        {
            if (item != null &&
                !item.IsHeld)
            {
                continue;
            }

            if (invalidItems == null)
            {
                invalidItems =
                    new List<CarryableItem2D>();
            }

            invalidItems.Add(item);
        }

        if (invalidItems == null)
        {
            return;
        }

        for (int i = 0; i < invalidItems.Count; i++)
        {
            nearbyItems.Remove(
                invalidItems[i]
            );
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        CarryableItem2D item =
            other.GetComponentInParent
                <CarryableItem2D>();

        if (item == null ||
            item.IsHeld)
        {
            return;
        }

        if (!nearbyItems.ContainsKey(item))
        {
            nearbyItems.Add(
                item,
                0
            );
        }

        nearbyItems[item]++;
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        CarryableItem2D item =
            other.GetComponentInParent
                <CarryableItem2D>();

        if (item == null ||
            !nearbyItems.ContainsKey(item))
        {
            return;
        }

        nearbyItems[item]--;

        if (nearbyItems[item] <= 0)
        {
            nearbyItems.Remove(item);
        }
    }

    private void OnDisable()
    {
        if (heldItem != null)
        {
            heldItem.Drop(
                transform.TransformPoint(
                    dropLocalOffset
                )
            );
        }

        nearbyItems.Clear();
        ClearPickupSprite();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            new Color(0.2f, 0.9f, 1f, 0.65f);

        Gizmos.DrawWireSphere(
            transform.position,
            GetPickupDetectionRadius()
        );
    }

    private SpriteRenderer FindPickupSpriteRenderer()
    {
        Transform pickupSpriteTransform =
            transform.Find("PickupSprite");

        if (pickupSpriteTransform == null)
        {
            return null;
        }

        return pickupSpriteTransform
            .GetComponent<SpriteRenderer>();
    }

    private void RefreshPickupSprite()
    {
        if (pickupSpriteRenderer == null)
        {
            return;
        }

        if (heldItem == null ||
            heldItem.IconSprite == null)
        {
            ClearPickupSprite();
            return;
        }

        pickupSpriteRenderer.sprite =
            heldItem.IconSprite;

        pickupSpriteRenderer.enabled =
            true;
    }

    private void ClearPickupSprite()
    {
        if (pickupSpriteRenderer == null)
        {
            return;
        }

        pickupSpriteRenderer.sprite =
            null;

        pickupSpriteRenderer.enabled =
            false;
    }
}
