using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameplaySceneFlowController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Jaeger";

    [Header("Panel Prefab Instance")]
    [SerializeField] private PausePanelView pausePanel;

    [Header("Input")]
    [SerializeField] private bool allowAnyGamepadToPause = true;

    private bool isPaused;

    private void OnEnable()
    {
        if (pausePanel != null)
        {
            pausePanel.ResumeClicked += Resume;
            pausePanel.RestartClicked += Restart;
            pausePanel.BackToMainMenuClicked += BackToMainMenu;
            pausePanel.Show(false);
        }
    }

    private void OnDisable()
    {
        if (pausePanel != null)
        {
            pausePanel.ResumeClicked -= Resume;
            pausePanel.RestartClicked -= Restart;
            pausePanel.BackToMainMenuClicked -= BackToMainMenu;
        }
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Pause();
            return;
        }

        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad == null ||
                !gamepad.startButton.wasPressedThisFrame)
            {
                continue;
            }

            if (!allowAnyGamepadToPause &&
                i != 0)
            {
                continue;
            }

            Pause();
            return;
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.Show(true);
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.Show(false);
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}

