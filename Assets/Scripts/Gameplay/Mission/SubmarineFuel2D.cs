using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SubmarineFuel2D : MonoBehaviour
{
    [Serializable]
    public class FuelChangedEvent : UnityEvent<float, float>
    {
    }

    [Header("燃料")]
    [Min(1f)]
    [SerializeField]
    private float maxFuel = 100f;

    [SerializeField]
    private float initialFuel = 50f;

    [SerializeField]
    private bool resetToInitialFuelOnAwake = true;

    [Header("事件")]
    [SerializeField]
    private FuelChangedEvent onFuelChanged =
        new FuelChangedEvent();

    private float currentFuel;

    public event Action<float, float> FuelChanged;

    public float MaxFuel => maxFuel;
    public float CurrentFuel => currentFuel;
    public FuelChangedEvent OnFuelChanged => onFuelChanged;

    private void Awake()
    {
        SetFuel(
            resetToInitialFuelOnAwake
                ? initialFuel
                : currentFuel
        );
    }

    private void OnValidate()
    {
        maxFuel = Mathf.Max(1f, maxFuel);
        initialFuel = Mathf.Clamp(
            initialFuel,
            0f,
            maxFuel
        );
    }

    public bool AddFuel(float amount)
    {
        if (amount <= 0f ||
            currentFuel >= maxFuel)
        {
            return false;
        }

        return SetFuel(currentFuel + amount);
    }

    public bool ConsumeFuel(float amount)
    {
        if (amount <= 0f ||
            currentFuel + 0.0001f < amount)
        {
            return false;
        }

        return SetFuel(currentFuel - amount);
    }

    public bool CanConsumeFuel(float amount)
    {
        return
            amount >= 0f &&
            currentFuel + 0.0001f >= amount;
    }

    public void ResetFuel()
    {
        SetFuel(initialFuel);
    }

    private bool SetFuel(float value)
    {
        float clamped = Mathf.Clamp(
            value,
            0f,
            maxFuel
        );

        if (Mathf.Approximately(
                currentFuel,
                clamped
            ))
        {
            return false;
        }

        currentFuel = clamped;
        FuelChanged?.Invoke(currentFuel, maxFuel);
        onFuelChanged.Invoke(currentFuel, maxFuel);
        return true;
    }
}
