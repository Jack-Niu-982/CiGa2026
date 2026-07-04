using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameplayPlayerRegistry
{
    private static readonly List<GameplayPlayerIdentity> players =
        new List<GameplayPlayerIdentity>(RoomInputManager.MaxPlayers);

    public static IReadOnlyList<GameplayPlayerIdentity> Players =>
        players;

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        players.Clear();
        Changed = null;
    }

    public static void Register(
        GameplayPlayerIdentity player)
    {
        if (player == null ||
            players.Contains(player))
        {
            return;
        }

        players.Add(player);

        players.Sort(
            (left, right) =>
                left.PlayerIndex.CompareTo(right.PlayerIndex)
        );

        Changed?.Invoke();
    }

    public static void Unregister(
        GameplayPlayerIdentity player)
    {
        if (player == null ||
            !players.Remove(player))
        {
            return;
        }

        Changed?.Invoke();
    }

    public static void Clear()
    {
        if (players.Count == 0)
        {
            return;
        }

        players.Clear();
        Changed?.Invoke();
    }
}
