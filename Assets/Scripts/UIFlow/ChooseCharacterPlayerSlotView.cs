using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChooseCharacterPlayerSlotView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text playerLabel;
    [SerializeField] private TMP_Text deviceLabel;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private Color emptyColor =
        new Color(0.08f, 0.12f, 0.20f, 0.86f);
    [SerializeField] private Color joinedColor =
        new Color(0.18f, 0.38f, 0.60f, 0.96f);
    [SerializeField] private Color readyColor =
        new Color(0.18f, 0.62f, 0.38f, 0.96f);

    public void Render(
        int playerIndex,
        bool isJoined,
        bool isConfirmed,
        string deviceName,
        int characterIndex)
    {
        if (playerLabel != null)
        {
            playerLabel.text = $"P{playerIndex + 1}";
        }

        if (deviceLabel != null)
        {
            deviceLabel.text = isJoined
                ? deviceName
                : "PRESS SOUTH";
        }

        if (stateLabel != null)
        {
            stateLabel.text = !isJoined
                ? "WAITING"
                : isConfirmed
                    ? $"CAT {characterIndex + 1} READY"
                    : $"CHOOSING CAT {characterIndex + 1}";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = !isJoined
                ? emptyColor
                : isConfirmed
                    ? readyColor
                    : joinedColor;
        }
    }
}
