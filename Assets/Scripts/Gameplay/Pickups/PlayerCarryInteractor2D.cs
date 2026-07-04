using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家身上的拾取与手持交互器。
/// 读取当前玩家自己的 InteractPressed：空手时拾取最近物品，手持时再次按键放下。
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

    [Tooltip("玩家正在操作锚点或其他设施时，拾取逻辑会暂停，避免同一个交互键抢输入。")]
    [SerializeField]
    private PlayerOperateInteractor2D operateInteractor;

    [Header("手持位置")]
    [Tooltip("物品被拿起后挂到这里。留空时挂到玩家自身。")]
    [SerializeField]
    private Transform holdParent;

    [SerializeField]
    private Vector2 holdLocalOffset =
        new Vector2(0.35f, 0.15f);

    [Header("放下位置")]
    [SerializeField]
    private Vector2 dropLocalOffset =
        new Vector2(0.55f, 0f);

    private readonly Dictionary<CarryableItem2D, int>
        nearbyItems =
            new Dictionary<CarryableItem2D, int>();

    private CarryableItem2D heldItem;

    public CarryableItem2D HeldItem => heldItem;
    public bool HasHeldItem => heldItem != null;

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
    }

    private void Update()
    {
        PlayerInputBase currentInput =
            GetCurrentInput();

        if (currentInput == null ||
            IsBusyOperating() ||
            !currentInput.InteractPressed)
        {
            return;
        }

        if (heldItem != null)
        {
            TryDropHeldItem();
            return;
        }

        TryPickupClosestItem();
    }

    public bool CanPickup(
        CarryableItem2D item)
    {
        return
            item != null &&
            heldItem == null &&
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
    }

    internal void NotifyHeldItemDropped(
        CarryableItem2D item)
    {
        if (heldItem == item)
        {
            heldItem = null;
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
        RemoveInvalidNearbyItems();

        CarryableItem2D closestItem =
            null;

        float closestSqrDistance =
            float.MaxValue;

        foreach (CarryableItem2D item in nearbyItems.Keys)
        {
            if (item == null ||
                item.IsHeld)
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
    }
}
