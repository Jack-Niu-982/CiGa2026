using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettlementPanelView : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite winBackground;
    [SerializeField] private Sprite loseBackground;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToMainMenuButton;

    public event Action RestartClicked;
    public event Action BackToMainMenuClicked;

    private void OnEnable()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.AddListener(HandleBackToMainMenuClicked);
        }
    }

    private void OnDisable()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveListener(HandleBackToMainMenuClicked);
        }
    }

    public void Show(MissionSettlementState state)
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite =
                state == MissionSettlementState.Won
                    ? winBackground
                    : loseBackground;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void HandleRestartClicked()
    {
        RestartClicked?.Invoke();
    }

    private void HandleBackToMainMenuClicked()
    {
        BackToMainMenuClicked?.Invoke();
    }
}
