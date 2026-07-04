using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoomPlayerSlotView : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text playerLabel;
    [SerializeField] private TMP_Text deviceLabel;
    [SerializeField] private TMP_Text stateLabel;

    [Header("Visual State")]
    [SerializeField] private Image stateImage;
    [SerializeField] private Color emptyColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color joinedColor = new Color(0.22f, 0.45f, 0.9f, 1f);
    [SerializeField] private Color readyColor = new Color(0.2f, 0.75f, 0.35f, 1f);

    public void Render(RoomPlayerSlot slot)
    {
        if (slot == null)
        {
            RenderEmpty(0);
            return;
        }

        if (playerLabel != null)
        {
            playerLabel.text = $"P{slot.SlotIndex + 1}";
        }

        if (!slot.IsOccupied)
        {
            RenderEmpty(slot.SlotIndex);
            return;
        }

        if (deviceLabel != null)
        {
            deviceLabel.text = string.IsNullOrWhiteSpace(slot.DeviceName)
                ? "Gamepad"
                : slot.DeviceName;
        }

        if (stateLabel != null)
        {
            stateLabel.text = slot.IsReady ? "Ready" : "Joined";
        }

        if (stateImage != null)
        {
            stateImage.color = slot.IsReady ? readyColor : joinedColor;
        }
    }

    private void RenderEmpty(int slotIndex)
    {
        if (playerLabel != null)
        {
            playerLabel.text = $"P{slotIndex + 1}";
        }

        if (deviceLabel != null)
        {
            deviceLabel.text = "No Gamepad";
        }

        if (stateLabel != null)
        {
            stateLabel.text = "Press South";
        }

        if (stateImage != null)
        {
            stateImage.color = emptyColor;
        }
    }
}

