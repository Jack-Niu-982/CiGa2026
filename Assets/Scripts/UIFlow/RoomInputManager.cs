using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class RoomInputManager : MonoBehaviour
{
    public const int MaxPlayers = 4;

    [Header("Room Rules")]
    [SerializeField] private int minimumReadyPlayers = 1;
    [SerializeField] private bool requireAllJoinedPlayersReady = false;

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardAsPlayerOne;

    private readonly List<RoomPlayerSlot> slots =
        new List<RoomPlayerSlot>(MaxPlayers);

    public IReadOnlyList<RoomPlayerSlot> Slots => slots;
    public int JoinedPlayerCount { get; private set; }
    public int ReadyPlayerCount { get; private set; }
    public bool CanStart { get; private set; }

    public event Action SlotsChanged;
    public event Action StartRequested;
    public event Action BackRequested;

    private void Awake()
    {
        EnsureSlots();
        RecalculateState();
    }

    public void ResetRoom()
    {
        EnsureSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Clear();
        }

        RecalculateState();
        SlotsChanged?.Invoke();
    }

    public void TickMainMenuInput()
    {
        if (WasAnyGamepadStartPressed() ||
            WasKeyboardConfirmPressed())
        {
            StartRequested?.Invoke();
        }
    }

    public void TickRoomInput()
    {
        EnsureSlots();
        RemoveDisconnectedGamepads();

        bool changed = false;

        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad == null)
            {
                continue;
            }

            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                changed |= HandleConfirm(gamepad);
            }

            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                changed |= HandleCancel(gamepad);
            }

            if (gamepad.startButton.wasPressedThisFrame &&
                IsFirstJoinedGamepad(gamepad) &&
                CanStart)
            {
                StartRequested?.Invoke();
            }
        }

        if (allowKeyboardAsPlayerOne &&
            Keyboard.current != null)
        {
            changed |= HandleKeyboardDebugInput();
        }

        if (changed)
        {
            RecalculateState();
            SlotsChanged?.Invoke();
        }
    }

    private bool HandleConfirm(Gamepad gamepad)
    {
        RoomPlayerSlot existing =
            FindSlotByDeviceId(gamepad.deviceId);

        if (existing != null)
        {
            existing.ToggleReady();
            return true;
        }

        RoomPlayerSlot emptySlot =
            FindFirstEmptySlot();

        if (emptySlot == null)
        {
            return false;
        }

        emptySlot.Assign(gamepad);
        return true;
    }

    private bool HandleCancel(Gamepad gamepad)
    {
        RoomPlayerSlot existing =
            FindSlotByDeviceId(gamepad.deviceId);

        if (existing == null)
        {
            if (JoinedPlayerCount == 0)
            {
                BackRequested?.Invoke();
            }

            return false;
        }

        existing.Clear();
        return true;
    }

    private bool HandleKeyboardDebugInput()
    {
        Keyboard keyboard = Keyboard.current;
        bool changed = false;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            RoomPlayerSlot firstSlot = slots[0];

            if (!firstSlot.IsOccupied)
            {
                firstSlot.CopyFrom(CreateKeyboardSlot());
            }
            else
            {
                firstSlot.ToggleReady();
            }

            changed = true;
        }

        if (keyboard.enterKey.wasPressedThisFrame &&
            CanStart)
        {
            StartRequested?.Invoke();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (JoinedPlayerCount == 0)
            {
                BackRequested?.Invoke();
            }
            else if (slots[0].IsOccupied &&
                     slots[0].DeviceId == -100)
            {
                slots[0].Clear();
                changed = true;
            }
        }

        return changed;
    }

    private RoomPlayerSlot CreateKeyboardSlot()
    {
        RoomPlayerSlot slot = new RoomPlayerSlot(0);
        slot.AssignDebugDevice(-100, "Keyboard");
        return slot;
    }

    private void RemoveDisconnectedGamepads()
    {
        bool changed = false;

        for (int i = 0; i < slots.Count; i++)
        {
            RoomPlayerSlot slot = slots[i];

            if (!slot.IsOccupied ||
                slot.DeviceId < 0)
            {
                continue;
            }

            if (FindGamepadByDeviceId(slot.DeviceId) != null)
            {
                continue;
            }

            slot.Clear();
            changed = true;
        }

        if (changed)
        {
            RecalculateState();
            SlotsChanged?.Invoke();
        }
    }

    private bool WasAnyGamepadStartPressed()
    {
        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad != null &&
                (gamepad.startButton.wasPressedThisFrame ||
                 gamepad.buttonSouth.wasPressedThisFrame))
            {
                return true;
            }
        }

        return false;
    }

    private bool WasKeyboardConfirmPressed()
    {
        if (!allowKeyboardAsPlayerOne ||
            Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.enterKey.wasPressedThisFrame ||
               Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private void RecalculateState()
    {
        JoinedPlayerCount = 0;
        ReadyPlayerCount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            RoomPlayerSlot slot = slots[i];

            if (!slot.IsOccupied)
            {
                continue;
            }

            JoinedPlayerCount++;

            if (slot.IsReady)
            {
                ReadyPlayerCount++;
            }
        }

        bool enoughReadyPlayers =
            ReadyPlayerCount >= Mathf.Max(1, minimumReadyPlayers);

        bool allJoinedPlayersReady =
            !requireAllJoinedPlayersReady ||
            (JoinedPlayerCount > 0 &&
             ReadyPlayerCount == JoinedPlayerCount);

        CanStart =
            JoinedPlayerCount > 0 &&
            enoughReadyPlayers &&
            allJoinedPlayersReady;
    }

    private bool IsFirstJoinedGamepad(Gamepad gamepad)
    {
        if (gamepad == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            RoomPlayerSlot slot = slots[i];

            if (!slot.IsOccupied)
            {
                continue;
            }

            return slot.DeviceId == gamepad.deviceId;
        }

        return false;
    }

    private RoomPlayerSlot FindSlotByDeviceId(int deviceId)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsOccupied &&
                slots[i].DeviceId == deviceId)
            {
                return slots[i];
            }
        }

        return null;
    }

    private RoomPlayerSlot FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsOccupied)
            {
                return slots[i];
            }
        }

        return null;
    }

    private Gamepad FindGamepadByDeviceId(int deviceId)
    {
        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad != null &&
                gamepad.deviceId == deviceId)
            {
                return gamepad;
            }
        }

        return null;
    }

    private void EnsureSlots()
    {
        while (slots.Count < MaxPlayers)
        {
            slots.Add(new RoomPlayerSlot(slots.Count));
        }
    }
}
