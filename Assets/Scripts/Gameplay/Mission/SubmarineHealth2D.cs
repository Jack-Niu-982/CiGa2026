using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 维护飞船的生命值。设计文档里的“护盾”在实现上先作为生命值处理。
/// </summary>
[DisallowMultipleComponent]
public class SubmarineHealth2D : MonoBehaviour, ICarryItemReceiver
{
    [Serializable]
    public class HealthChangedEvent : UnityEvent<float, float>
    {
    }

    [Serializable]
    public class HealthDepletedEvent : UnityEvent<SubmarineHealth2D>
    {
    }

    [Header("血量")]
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float initialHealth = 100f;

    [SerializeField]
    private bool resetToInitialHealthOnAwake = true;

    [Header("护盾拾取物")]
    [SerializeField]
    private float shieldRepairAmount = 25f;

    [Header("事件")]
    [SerializeField]
    private HealthChangedEvent onHealthChanged =
        new HealthChangedEvent();

    [SerializeField]
    private HealthDepletedEvent onDepleted =
        new HealthDepletedEvent();

    private float currentHealth;
    private bool depleted;

    public event Action<float, float> HealthChanged;
    public event Action<SubmarineHealth2D> Depleted;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDepleted => depleted;
    public HealthChangedEvent OnHealthChanged => onHealthChanged;
    public HealthDepletedEvent OnDepleted => onDepleted;

    private void Awake()
    {
        if (resetToInitialHealthOnAwake)
        {
            SetHealth(initialHealth);
        }
        else
        {
            SetHealth(currentHealth);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        initialHealth = Mathf.Clamp(
            initialHealth,
            0f,
            maxHealth
        );

        shieldRepairAmount =
            Mathf.Max(0f, shieldRepairAmount);
    }

    public bool Damage(float amount)
    {
        if (amount <= 0f ||
            depleted)
        {
            return false;
        }

        return SetHealth(
            currentHealth - amount
        );
    }

    public bool Repair(float amount)
    {
        if (amount <= 0f ||
            depleted ||
            currentHealth >= maxHealth)
        {
            return false;
        }

        return SetHealth(
            currentHealth + amount
        );
    }

    public void ResetHealth()
    {
        depleted = false;
        SetHealth(initialHealth);
    }

    public bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        return
            holder != null &&
            item != null &&
            item.ItemType == CarryableItemType.Shield &&
            !depleted &&
            currentHealth < maxHealth;
    }

    public bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        if (!CanReceiveCarryItem(
                holder,
                item))
        {
            return false;
        }

        bool repaired =
            Repair(shieldRepairAmount);

        if (!repaired)
        {
            return false;
        }

        holder.NotifyHeldItemDropped(item);
        Destroy(item.gameObject);

        return true;
    }

    private bool SetHealth(float value)
    {
        float clampedHealth =
            Mathf.Clamp(
                value,
                0f,
                maxHealth
            );

        if (Mathf.Approximately(
                currentHealth,
                clampedHealth) &&
            depleted == (clampedHealth <= 0f))
        {
            return false;
        }

        currentHealth = clampedHealth;

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        onHealthChanged.Invoke(
            currentHealth,
            maxHealth
        );

        if (currentHealth > 0f ||
            depleted)
        {
            return true;
        }

        depleted = true;

        Depleted?.Invoke(this);
        onDepleted.Invoke(this);

        return true;
    }
}
