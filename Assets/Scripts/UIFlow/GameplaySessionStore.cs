using System.Collections.Generic;
using UnityEngine;

public static class GameplaySessionStore
{
    private static readonly List<GameplayPlayerAssignment> assignments =
        new List<GameplayPlayerAssignment>(RoomInputManager.MaxPlayers);

    public static IReadOnlyList<GameplayPlayerAssignment> Assignments =>
        assignments;

    public static bool HasSession => assignments.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        assignments.Clear();
    }

    public static void Capture(RoomInputManager roomInput)
    {
        assignments.Clear();

        if (roomInput == null)
        {
            return;
        }

        IReadOnlyList<RoomPlayerSlot> slots =
            roomInput.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            RoomPlayerSlot slot = slots[i];

            if (slot == null ||
                !slot.IsOccupied ||
                !slot.IsReady)
            {
                continue;
            }

            assignments.Add(
                new GameplayPlayerAssignment(slot)
            );
        }
    }

    public static void SetAssignments(
        IReadOnlyList<GameplayPlayerAssignment> newAssignments)
    {
        assignments.Clear();

        if (newAssignments == null)
        {
            return;
        }

        for (int i = 0; i < newAssignments.Count; i++)
        {
            GameplayPlayerAssignment assignment =
                newAssignments[i];

            assignment.CharacterIndex =
                Mathf.Clamp(
                    assignment.CharacterIndex,
                    0,
                    RoomInputManager.MaxPlayers - 1
                );

            assignments.Add(assignment);
        }
    }

    public static void Clear()
    {
        assignments.Clear();
    }
}

