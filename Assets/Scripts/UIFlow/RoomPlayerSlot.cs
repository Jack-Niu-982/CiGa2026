using UnityEngine.InputSystem;

[System.Serializable]
public class RoomPlayerSlot
{
    public int SlotIndex { get; private set; }
    public bool IsOccupied { get; private set; }
    public bool IsReady { get; private set; }
    public int DeviceId { get; private set; }
    public string DeviceName { get; private set; }

    public RoomPlayerSlot(int slotIndex)
    {
        SlotIndex = slotIndex;
        Clear();
    }

    public void Assign(Gamepad gamepad)
    {
        IsOccupied = true;
        IsReady = false;
        DeviceId = gamepad != null ? gamepad.deviceId : -1;
        DeviceName = gamepad != null ? gamepad.displayName : "Unknown Gamepad";
    }

    public void AssignDebugDevice(
        int deviceId,
        string deviceName)
    {
        IsOccupied = true;
        IsReady = false;
        DeviceId = deviceId;
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
        DeviceName = string.Empty;
    }
}
