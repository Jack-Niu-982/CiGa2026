using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameplayPlayerStatusSlotView : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private TMP_Text playerLabel;

    [SerializeField]
    private Image portraitImage;

    [Header("Held Item")]
    [SerializeField]
    private GameObject itemRoot;

    [SerializeField]
    private Image itemIconImage;

    [SerializeField]
    private TMP_Text itemNameLabel;

    public void Render(
        GameplayPlayerIdentity player)
    {
        gameObject.SetActive(player != null);

        if (player == null)
        {
            return;
        }

        if (playerLabel != null)
        {
            playerLabel.text =
                player.PlayerLabel;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite =
                player.PortraitSprite;

            portraitImage.enabled =
                player.PortraitSprite != null;
        }

        PlayerCarryInteractor2D carryInteractor =
            player.GetComponent<PlayerCarryInteractor2D>();

        CarryableItem2D item =
            carryInteractor != null
                ? carryInteractor.HeldItem
                : null;

        bool hasItem =
            item != null;

        if (itemRoot != null)
        {
            itemRoot.SetActive(hasItem);
        }

        if (itemIconImage != null)
        {
            itemIconImage.sprite =
                hasItem ? item.IconSprite : null;

            itemIconImage.enabled =
                hasItem &&
                item.IconSprite != null;
        }

        if (itemNameLabel != null)
        {
            itemNameLabel.text =
                hasItem ? item.DisplayName : string.Empty;
        }
    }
}
