using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameplayHudView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button pauseButton;

    public event Action PauseClicked;

    private void OnEnable()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(HandlePauseClicked);
        }
    }

    private void OnDisable()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseClicked);
        }
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandlePauseClicked()
    {
        PauseClicked?.Invoke();
    }
}
