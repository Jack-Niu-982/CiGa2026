using UnityEngine.InputSystem;

[System.Serializable]
public class RoomPlayerSlot
{
    public int SlotIndex { get; private set; }
    public bool IsOccupied { get; private set; }
    public bool IsReady { get; private set; }
    public int DeviceId { get; private set; }
    public int DeviceIndex { get; private set; }
    public string DeviceName { get; private set; }

    public RoomPlayerSlot(int slotIndex)
    {
        SlotIndex = slotIndex;
        Clear();
    }

    public void Assign(Gamepad gamepad)
    {
        Assign(gamepad, FindGamepadIndex(gamepad));
    }

    public void Assign(
        Gamepad gamepad,
        int deviceIndex)
    {
        IsOccupied = true;
        IsReady = false;
        DeviceId = gamepad != null ? gamepad.deviceId : -1;
        DeviceIndex = deviceIndex;
        DeviceName = gamepad != null ? gamepad.displayName : "Unknown Gamepad";
    }

    public void AssignDebugDevice(
        int deviceId,
        int deviceIndex,
        string deviceName)
    {
        IsOccupied = true;
        IsReady = false;
        DeviceId = deviceId;
        DeviceIndex = deviceIndex;
        DeviceName = deviceName;
    }

    public void CopyFrom(RoomPlayerSlot source)
    {
        if (source == null || !source.IsOccupied)
        {
            Clear();
            return;
        }

        IsOccupied = true;
        IsReady = source.IsReady;
        DeviceId = source.DeviceId;
        DeviceIndex = source.DeviceIndex;
        DeviceName = source.DeviceName;
    }

    public void ToggleReady()
    {
        if (!IsOccupied)
        {
            return;
        }

        IsReady = !IsReady;
    }

    public void Clear()
    {
        IsOccupied = false;
        IsReady = false;
        DeviceId = -1;
        DeviceIndex = -1;
        DeviceName = string.Empty;
    }

    private static int FindGamepadIndex(Gamepad gamepad)
    {
        if (gamepad == null)
        {
            return -1;
        }

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            if (Gamepad.all[i] == gamepad)
            {
                return i;
            }
        }

        return -1;
    }
}
