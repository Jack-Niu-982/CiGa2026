using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class CarryItemDepositPoint2D : MonoBehaviour,
    ICarryItemReceiver
{
    [Header("投入规则")]
    [SerializeField]
    private CarryableItemType acceptedItemType =
        CarryableItemType.Unknown;

    [Tooltip("每次投入增加的 Health 或 Fuel 数值。")]
    [Min(0f)]
    [SerializeField]
    private float restoreAmount = 25f;

    [Header("数值目标")]
    [SerializeField]
    private SubmarineHealth2D healthTarget;

    [SerializeField]
    private SubmarineFuel2D fuelTarget;

    public CarryableItemType AcceptedItemType =>
        acceptedItemType;

    public float RestoreAmount => restoreAmount;

    private void Awake()
    {
        ResolveTargets();
    }

    private void OnValidate()
    {
        restoreAmount = Mathf.Max(0f, restoreAmount);

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    public bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        if (holder == null ||
            item == null ||
            item.ItemType != acceptedItemType ||
            restoreAmount <= 0f)
        {
            return false;
        }

        ResolveTargets();

        switch (acceptedItemType)
        {
            case CarryableItemType.Shield:
                return
                    healthTarget != null &&
                    !healthTarget.IsDepleted &&
                    healthTarget.CurrentHealth <
                    healthTarget.MaxHealth;

            case CarryableItemType.Fuel:
                return
                    fuelTarget != null &&
                    fuelTarget.CurrentFuel <
                    fuelTarget.MaxFuel;

            default:
                return false;
        }
    }

    public bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        if (!CanReceiveCarryItem(holder, item))
        {
            return false;
        }

        bool changed =
            acceptedItemType == CarryableItemType.Shield
                ? healthTarget.Repair(restoreAmount)
                : fuelTarget.AddFuel(restoreAmount);

        if (!changed)
        {
            return false;
        }

        holder.NotifyHeldItemDropped(item);
        Destroy(item.gameObject);
        return true;
    }

    private void ResolveTargets()
    {
        if (healthTarget == null)
        {
            healthTarget =
                GetComponentInParent<SubmarineHealth2D>();

            if (healthTarget == null)
            {
                healthTarget =
                    FindObjectOfType<SubmarineHealth2D>();
            }
        }

        if (fuelTarget == null)
        {
            fuelTarget =
                GetComponentInParent<SubmarineFuel2D>();

            if (fuelTarget == null)
            {
                fuelTarget =
                    FindObjectOfType<SubmarineFuel2D>();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerPutInInteractor2D player =
            other.GetComponentInParent
                <PlayerPutInInteractor2D>();

        if (player != null)
        {
            player.RegisterDepositPoint(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerPutInInteractor2D player =
            other.GetComponentInParent
                <PlayerPutInInteractor2D>();

        if (player != null)
        {
            player.UnregisterDepositPoint(this);
        }
    }
}
