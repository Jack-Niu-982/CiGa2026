using System;
using System.Collections.Generic;
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
    public static event Action<CarryableItemType> SpawnRandomPickupRequested;
    public static event Action SpawnAllPickupsRequested;

    private static readonly Dictionary<Type, Delegate> eventHandlers = new Dictionary<Type, Delegate>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticEvents()
    {
        SubmarineDamageRequested = null;
        SubmarineRepairRequested = null;
        SubmarineDamaged = null;
        SpawnRandomPickupRequested = null;
        SpawnAllPickupsRequested = null;
        eventHandlers.Clear();
    }

    /// <summary>
    /// 订阅事件。
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type eventType = typeof(T);
        if (eventHandlers.TryGetValue(eventType, out Delegate existingHandler))
        {
            eventHandlers[eventType] = Delegate.Combine(existingHandler, handler);
        }
        else
        {
            eventHandlers[eventType] = handler;
        }
    }

    /// <summary>
    /// 取消订阅事件。
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type eventType = typeof(T);
        if (eventHandlers.TryGetValue(eventType, out Delegate existingHandler))
        {
            Delegate newHandler = Delegate.Remove(existingHandler, handler);
            if (newHandler == null)
            {
                eventHandlers.Remove(eventType);
            }
            else
            {
                eventHandlers[eventType] = newHandler;
            }
        }
    }

    /// <summary>
    /// 发布事件。
    /// </summary>
    public static void Publish<T>(T eventData) where T : struct
    {
        Type eventType = typeof(T);
        if (eventHandlers.TryGetValue(eventType, out Delegate handler))
        {
            (handler as Action<T>)?.Invoke(eventData);
        }
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

    public static void RequestSpawnRandomPickup(
        CarryableItemType itemType)
    {
        SpawnRandomPickupRequested?.Invoke(itemType);
    }

    public static void RequestSpawnAllPickups()
    {
        SpawnAllPickupsRequested?.Invoke();
    }
}

/// <summary>
/// 一局 Gameplay 胜利时发布的事件。
/// </summary>
public readonly struct MissionVictoryEvent
{
    public MissionVictoryEvent(
        MissionSettlementController source,
        MissionSettlementState state)
    {
        Source = source;
        State = state;
    }

    public MissionSettlementController Source { get; }
    public MissionSettlementState State { get; }
}

/// <summary>
/// 一局 Gameplay 失败时发布的事件。
/// </summary>
public readonly struct MissionFailureEvent
{
    public MissionFailureEvent(
        MissionSettlementController source,
        MissionSettlementState state)
    {
        Source = source;
        State = state;
    }

    public MissionSettlementController Source { get; }
    public MissionSettlementState State { get; }
}
