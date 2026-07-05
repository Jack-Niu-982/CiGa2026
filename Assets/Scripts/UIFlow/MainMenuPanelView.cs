using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuPanelView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button levelEditorButton;
    [SerializeField] private Button quitButton;

    public event Action StartClicked;
    public event Action LevelEditorClicked;
    public event Action QuitClicked;

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(HandleQuitClicked);
        }

        if (levelEditorButton != null)
        {
            levelEditorButton.onClick.AddListener(HandleLevelEditorClicked);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
        }

        if (levelEditorButton != null)
        {
            levelEditorButton.onClick.RemoveListener(HandleLevelEditorClicked);
        }
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandleStartClicked()
    {
        StartClicked?.Invoke();
    }

    private void HandleLevelEditorClicked()
    {
        LevelEditorClicked?.Invoke();
    }

    private void HandleQuitClicked()
    {
        QuitClicked?.Invoke();
    }
}

