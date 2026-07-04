using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private GameFlowState initialState = GameFlowState.MainMenu;
    [SerializeField] private string gameplaySceneName = "Jaeger";

    [Header("Runtime State")]
    [SerializeField] private RoomInputManager roomInputManager;
    [SerializeField] private LocalPlayerSession localPlayerSession;

    [Header("Panel Prefab Instances")]
    [SerializeField] private MainMenuPanelView mainMenuPanel;
    [SerializeField] private RoomPanelView roomPanel;

    [Header("Options")]
    [SerializeField] private bool quitReturnsToMainMenuInEditor = true;

    public GameFlowState CurrentState { get; private set; }

    private void Reset()
    {
        roomInputManager = GetComponent<RoomInputManager>();
        localPlayerSession = GetComponent<LocalPlayerSession>();
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        EnterState(initialState);
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (roomInputManager == null)
        {
            return;
        }

        if (CurrentState == GameFlowState.MainMenu)
        {
            roomInputManager.TickMainMenuInput();
        }
        else if (CurrentState == GameFlowState.Room)
        {
            roomInputManager.TickRoomInput();
        }
    }

    public void EnterMainMenu()
    {
        if (localPlayerSession != null)
        {
            localPlayerSession.Clear();
        }

        EnterState(GameFlowState.MainMenu);
    }

    public void EnterRoom()
    {
        if (roomInputManager != null)
        {
            roomInputManager.ResetRoom();
        }

        EnterState(GameFlowState.Room);
    }

    public void EnterGameplay()
    {
        if (roomInputManager != null &&
            !roomInputManager.CanStart)
        {
            return;
        }

        if (localPlayerSession != null)
        {
            localPlayerSession.CaptureFrom(roomInputManager);
        }

        GameplaySessionStore.Capture(roomInputManager);

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        if (quitReturnsToMainMenuInEditor)
        {
            EnterMainMenu();
            return;
        }

        Application.Quit();
    }

    private void EnterState(GameFlowState nextState)
    {
        CurrentState = nextState;

        bool showMainMenu = nextState == GameFlowState.MainMenu;
        bool showRoom = nextState == GameFlowState.Room;
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Show(showMainMenu);
        }

        if (roomPanel != null)
        {
            roomPanel.Show(showRoom);
        }

        RefreshRoomPanel();
    }

    private void RefreshRoomPanel()
    {
        if (roomPanel != null)
        {
            roomPanel.Render(roomInputManager);
        }
    }

    private void EnsureReferences()
    {
        if (roomInputManager == null)
        {
            roomInputManager = GetComponent<RoomInputManager>();
        }

        if (localPlayerSession == null)
        {
            localPlayerSession = GetComponent<LocalPlayerSession>();
        }
    }

    private void SubscribeEvents()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.StartClicked += EnterRoom;
            mainMenuPanel.QuitClicked += QuitGame;
        }

        if (roomPanel != null)
        {
            roomPanel.StartClicked += EnterGameplay;
            roomPanel.BackClicked += EnterMainMenu;
        }

        if (roomInputManager != null)
        {
            roomInputManager.SlotsChanged += RefreshRoomPanel;
            roomInputManager.StartRequested += HandleInputStartRequested;
            roomInputManager.BackRequested += HandleInputBackRequested;
        }
    }

    private void UnsubscribeEvents()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.StartClicked -= EnterRoom;
            mainMenuPanel.QuitClicked -= QuitGame;
        }

        if (roomPanel != null)
        {
            roomPanel.StartClicked -= EnterGameplay;
            roomPanel.BackClicked -= EnterMainMenu;
        }

        if (roomInputManager != null)
        {
            roomInputManager.SlotsChanged -= RefreshRoomPanel;
            roomInputManager.StartRequested -= HandleInputStartRequested;
            roomInputManager.BackRequested -= HandleInputBackRequested;
        }
    }

    private void HandleInputStartRequested()
    {
        if (CurrentState == GameFlowState.MainMenu)
        {
            EnterRoom();
        }
        else if (CurrentState == GameFlowState.Room)
        {
            EnterGameplay();
        }
    }

    private void HandleInputBackRequested()
    {
        if (CurrentState == GameFlowState.Room)
        {
            EnterMainMenu();
        }
    }
}
