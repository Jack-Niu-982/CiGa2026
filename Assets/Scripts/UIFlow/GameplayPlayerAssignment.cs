[System.Serializable]
public struct GameplayPlayerAssignment
{
    public int SlotIndex;
    public int DeviceId;
    public int DeviceIndex;
    public string DeviceName;
    public int CharacterIndex;

    public bool IsValid => SlotIndex >= 0 && DeviceIndex >= 0;

    public GameplayPlayerAssignment(RoomPlayerSlot slot)
    {
        SlotIndex = slot != null ? slot.SlotIndex : -1;
        DeviceId = slot != null ? slot.DeviceId : -1;
        DeviceIndex = slot != null ? slot.DeviceIndex : -1;
        DeviceName = slot != null ? slot.DeviceName : string.Empty;
        CharacterIndex = slot != null ? slot.SlotIndex : 0;
    }
}

