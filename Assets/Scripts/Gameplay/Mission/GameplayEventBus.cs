using System;
using UnityEngine;

/// <summary>
/// Gameplay 内部的轻量事件总线。
/// 用于原型期系统之间的松耦合通信，避免调试面板直接引用具体系统。
/// </summary>
public static class GameplayEventBus
{
    public static event Action<float> SubmarineDamageRequested;
    public static event Action<float> SubmarineRepairRequested;
    public static event Action<float> SubmarineDamaged;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticEvents()
    {
        SubmarineDamageRequested = null;
        SubmarineRepairRequested = null;
        SubmarineDamaged = null;
    }

    public static void RequestSubmarineDamage(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SubmarineDamageRequested?.Invoke(amount);
    }

    public static void RequestSubmarineRepair(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SubmarineRepairRequested?.Invoke(amount);
    }

    public static void PublishSubmarineDamaged(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SubmarineDamaged?.Invoke(amount);
    }
}
