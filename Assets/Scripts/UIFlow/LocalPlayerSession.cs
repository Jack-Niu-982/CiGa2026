using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalPlayerSession : MonoBehaviour
{
    private readonly List<RoomPlayerSlot> activeSlots =
        new List<RoomPlayerSlot>(4);

    public IReadOnlyList<RoomPlayerSlot> ActiveSlots => activeSlots;
    public int PlayerCount => activeSlots.Count;
    public int HostDeviceId =>
        activeSlots.Count > 0 ? activeSlots[0].DeviceId : -1;

    public void CaptureFrom(RoomInputManager roomInput)
    {
        activeSlots.Clear();

        if (roomInput == null)
        {
            return;
        }

        IReadOnlyList<RoomPlayerSlot> slots =
            roomInput.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            RoomPlayerSlot source = slots[i];

            if (!source.IsOccupied)
            {
                continue;
            }

            RoomPlayerSlot copy =
                new RoomPlayerSlot(source.SlotIndex);

            copy.CopyFrom(source);
            activeSlots.Add(copy);
        }
    }

    public void Clear()
    {
        activeSlots.Clear();
    }

    public bool IsHostDevice(int deviceId)
    {
        return HostDeviceId == deviceId;
    }
}
