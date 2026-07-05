using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameplaySceneFlowController : MonoBehaviour
{
    [Header("Panel Prefab Instance")]
    [SerializeField] private GameplayHudView gameplayHud;
    [SerializeField] private PausePanelView pausePanel;
    [SerializeField] private ConfirmDialogView backToMainMenuConfirmDialog;
    [SerializeField] private SettlementPanelView settlementPanel;

    [Header("Mission")]
    [SerializeField] private MissionSettlementController missionSettlementController;

    [Header("Input")]
    [SerializeField] private bool allowAnyGamepadToPause = true;

    private bool isPaused;
    private bool isSettled;

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

        if (settlementPanel != null)
        {
            settlementPanel.RestartClicked += Restart;
            settlementPanel.BackToMainMenuClicked += BackToMainMenu;
            settlementPanel.Hide();
        }

        if (missionSettlementController != null)
        {
            missionSettlementController.MissionSettled += HandleMissionSettled;
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

        if (settlementPanel != null)
        {
            settlementPanel.RestartClicked -= Restart;
            settlementPanel.BackToMainMenuClicked -= BackToMainMenu;
        }

        if (missionSettlementController != null)
        {
            missionSettlementController.MissionSettled -= HandleMissionSettled;
        }
    }

    private void Update()
    {
        if (isPaused || isSettled)
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
        SceneManager.LoadScene(SettingManager.Scene.gameplay);
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
        SceneManager.LoadScene(SettingManager.Scene.mainMenu);
    }

    private void HideBackToMainMenuConfirmDialog()
    {
        if (backToMainMenuConfirmDialog != null)
        {
            backToMainMenuConfirmDialog.Hide();
        }
    }

    private void HandleMissionSettled(MissionSettlementState state)
    {
        isSettled = true;
        Time.timeScale = 0f;

        if (settlementPanel != null)
        {
            settlementPanel.Show(state);
        }

        if (gameplayHud != null)
        {
            gameplayHud.Show(false);
        }

        if (pausePanel != null)
        {
            pausePanel.Show(false);
        }

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
