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
    [SerializeField] private GameplayHudView gameplayHud;
    [SerializeField] private PausePanelView pausePanel;
    [SerializeField] private ConfirmDialogView backToMainMenuConfirmDialog;

    [Header("Input")]
    [SerializeField] private bool allowAnyGamepadToPause = true;

    private bool isPaused;

    private void OnEnable()
    {
        if (gameplayHud != null)
        {
            gameplayHud.PauseClicked += Pause;
            gameplayHud.Show(true);
        }

        if (pausePanel != null)
        {
            pausePanel.ResumeClicked += Resume;
            pausePanel.RestartClicked += Restart;
            pausePanel.BackToMainMenuClicked += RequestBackToMainMenu;
            pausePanel.Show(false);
        }

        if (backToMainMenuConfirmDialog != null)
        {
            backToMainMenuConfirmDialog.Confirmed += BackToMainMenu;
            backToMainMenuConfirmDialog.Cancelled += HideBackToMainMenuConfirmDialog;
            backToMainMenuConfirmDialog.Hide();
        }
    }

    private void OnDisable()
    {
        if (gameplayHud != null)
        {
            gameplayHud.PauseClicked -= Pause;
        }

        if (pausePanel != null)
        {
            pausePanel.ResumeClicked -= Resume;
            pausePanel.RestartClicked -= Restart;
            pausePanel.BackToMainMenuClicked -= RequestBackToMainMenu;
        }

        if (backToMainMenuConfirmDialog != null)
        {
            backToMainMenuConfirmDialog.Confirmed -= BackToMainMenu;
            backToMainMenuConfirmDialog.Cancelled -= HideBackToMainMenuConfirmDialog;
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

        if (gameplayHud != null)
        {
            gameplayHud.Show(false);
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

        if (backToMainMenuConfirmDialog != null)
        {
            backToMainMenuConfirmDialog.Hide();
        }

        if (gameplayHud != null)
        {
            gameplayHud.Show(true);
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void RequestBackToMainMenu()
    {
        if (backToMainMenuConfirmDialog == null)
        {
            BackToMainMenu();
            return;
        }

        backToMainMenuConfirmDialog.Show(
            "Return to Main Menu?",
            "Current gameplay progress will be discarded."
        );
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HideBackToMainMenuConfirmDialog()
    {
        if (backToMainMenuConfirmDialog != null)
        {
            backToMainMenuConfirmDialog.Hide();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
