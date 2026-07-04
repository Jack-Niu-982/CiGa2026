using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PausePanelView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToMainMenuButton;

    public event Action ResumeClicked;
    public event Action RestartClicked;
    public event Action BackToMainMenuClicked;

    private void OnEnable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(HandleResumeClicked);
        }

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
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(HandleResumeClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveListener(HandleBackToMainMenuClicked);
        }
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandleResumeClicked()
    {
        ResumeClicked?.Invoke();
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

