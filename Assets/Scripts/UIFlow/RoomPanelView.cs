using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoomPanelView : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private RoomPlayerSlotView[] slotViews =
        new RoomPlayerSlotView[RoomInputManager.MaxPlayers];

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Text")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text hintLabel;
    [SerializeField] private TMP_Text summaryLabel;

    [Header("Copy")]
    [SerializeField] private string titleText = "Room";
    [SerializeField] private string joinHintText =
        "Press South to join, South again to ready, East to leave.";
    [SerializeField] private string readyHintText =
        "P1 presses Start when the room is ready.";

    public event Action StartClicked;
    public event Action BackClicked;

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
        }
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void Render(RoomInputManager roomInput)
    {
        if (titleLabel != null)
        {
            titleLabel.text = titleText;
        }

        if (hintLabel != null)
        {
            hintLabel.text = joinHintText + "\n" + readyHintText;
        }

        if (roomInput == null)
        {
            RenderEmpty();
            return;
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
            {
                continue;
            }

            RoomPlayerSlot slot =
                i < roomInput.Slots.Count
                    ? roomInput.Slots[i]
                    : null;

            slotViews[i].Render(slot);
        }

        if (summaryLabel != null)
        {
            summaryLabel.text =
                $"{roomInput.JoinedPlayerCount}/4 joined, " +
                $"{roomInput.ReadyPlayerCount} ready";
        }

        if (startButton != null)
        {
            startButton.interactable = roomInput.CanStart;
        }
    }

    private void RenderEmpty()
    {
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
            {
                slotViews[i].Render(null);
            }
        }

        if (summaryLabel != null)
        {
            summaryLabel.text = "0/4 joined";
        }

        if (startButton != null)
        {
            startButton.interactable = false;
        }
    }

    private void HandleStartClicked()
    {
        StartClicked?.Invoke();
    }

    private void HandleBackClicked()
    {
        BackClicked?.Invoke();
    }
}

