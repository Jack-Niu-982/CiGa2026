using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class ChooseCharacterController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string gameplaySceneName = "Jaeger";

    [Header("View")]
    [SerializeField] private ChooseCharacterCardView[] characterCards =
        new ChooseCharacterCardView[RoomInputManager.MaxPlayers];
    [SerializeField] private ChooseCharacterPlayerSlotView[] playerSlots =
        new ChooseCharacterPlayerSlotView[RoomInputManager.MaxPlayers];
    [SerializeField] private TMP_Text statusLabel;

    [Header("Stick Navigation")]
    [SerializeField, Range(0.1f, 1f)]
    private float navigationThreshold = 0.65f;
    [SerializeField, Range(0f, 0.9f)]
    private float navigationReleaseThreshold = 0.30f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip characterChangedClip;
    [SerializeField] private AudioClip playerJoinedClip;

    [SerializeField, Range(0f, 2f)]
    private float audioVolume = 1.1f;

    private readonly List<PlayerSelection> players =
        new List<PlayerSelection>(RoomInputManager.MaxPlayers);

    private string temporaryStatus;
    private float temporaryStatusTimer;

    private sealed class PlayerSelection
    {
        public int DeviceId;
        public string DeviceName;
        public int CharacterIndex;
        public bool IsConfirmed;
        public bool NavigationArmed = true;
    }

    private void Awake()
    {
        ResolveAudioSource();
        ConfigureAudioSource();
    }

    private void Start()
    {
        GameplaySessionStore.Clear();
        RefreshView();
    }

    private void Update()
    {
        RemoveDisconnectedPlayers();

        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad == null)
            {
                continue;
            }

            PlayerSelection player =
                FindPlayer(gamepad.deviceId);

            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                if (player == null)
                {
                    JoinPlayer(gamepad);
                    player = FindPlayer(gamepad.deviceId);
                }
                else
                {
                    ConfirmPlayer(player);
                }
            }

            if (gamepad.buttonEast.wasPressedThisFrame &&
                player != null)
            {
                CancelOrLeave(player);
                continue;
            }

            if (player != null && !player.IsConfirmed)
            {
                HandleNavigation(player, gamepad);
            }

            if (gamepad.buttonNorth.wasPressedThisFrame &&
                players.Count > 0 &&
                players[0].DeviceId == gamepad.deviceId &&
                CanStart())
            {
                EnterGameplay();
                return;
            }
        }

        if (temporaryStatusTimer > 0f)
        {
            temporaryStatusTimer -= Time.unscaledDeltaTime;

            if (temporaryStatusTimer <= 0f)
            {
                temporaryStatus = string.Empty;
                RefreshStatus();
            }
        }
    }

    private void JoinPlayer(Gamepad gamepad)
    {
        if (players.Count >= RoomInputManager.MaxPlayers)
        {
            ShowTemporaryStatus("ROOM IS FULL");
            return;
        }

        PlayerSelection player = new PlayerSelection
        {
            DeviceId = gamepad.deviceId,
            DeviceName = gamepad.displayName,
            CharacterIndex = FindFirstAvailableCharacter()
        };

        players.Add(player);
        PlayOneShot(playerJoinedClip);
        RefreshView();
    }

    private void ConfirmPlayer(PlayerSelection player)
    {
        if (player == null || player.IsConfirmed)
        {
            return;
        }

        if (IsCharacterConfirmedByOther(
                player.CharacterIndex,
                player))
        {
            ShowTemporaryStatus("THAT CHARACTER IS ALREADY TAKEN");
            return;
        }

        player.IsConfirmed = true;
        PlayOneShot(playerJoinedClip);
        RefreshView();
    }

    private void CancelOrLeave(PlayerSelection player)
    {
        if (player.IsConfirmed)
        {
            player.IsConfirmed = false;
        }
        else
        {
            players.Remove(player);
        }

        RefreshView();
    }

    private void HandleNavigation(
        PlayerSelection player,
        Gamepad gamepad)
    {
        Vector2 stick = gamepad.leftStick.ReadValue();
        Vector2 dpad = gamepad.dpad.ReadValue();
        float horizontal =
            Mathf.Abs(dpad.x) > 0.01f
                ? dpad.x
                : stick.x;

        if (Mathf.Abs(horizontal) <= navigationReleaseThreshold)
        {
            player.NavigationArmed = true;
            return;
        }

        if (!player.NavigationArmed ||
            Mathf.Abs(horizontal) < navigationThreshold)
        {
            return;
        }

        player.NavigationArmed = false;
        int direction = horizontal > 0f ? 1 : -1;
        int previousCharacterIndex =
            player.CharacterIndex;

        player.CharacterIndex =
            FindNextAvailableCharacter(
                player.CharacterIndex,
                direction,
                player
            );

        if (player.CharacterIndex !=
            previousCharacterIndex)
        {
            PlayOneShot(characterChangedClip);
        }

        RefreshView();
    }

    private int FindNextAvailableCharacter(
        int currentIndex,
        int direction,
        PlayerSelection player)
    {
        for (int step = 1;
             step <= RoomInputManager.MaxPlayers;
             step++)
        {
            int candidate =
                (currentIndex +
                 direction * step +
                 RoomInputManager.MaxPlayers * 2) %
                RoomInputManager.MaxPlayers;

            if (!IsCharacterConfirmedByOther(candidate, player))
            {
                return candidate;
            }
        }

        return currentIndex;
    }

    private int FindFirstAvailableCharacter()
    {
        for (int i = 0; i < RoomInputManager.MaxPlayers; i++)
        {
            if (!IsCharacterConfirmedByOther(i, null))
            {
                bool hovered = false;

                for (int playerIndex = 0;
                     playerIndex < players.Count;
                     playerIndex++)
                {
                    if (players[playerIndex].CharacterIndex == i)
                    {
                        hovered = true;
                        break;
                    }
                }

                if (!hovered)
                {
                    return i;
                }
            }
        }

        return players.Count % RoomInputManager.MaxPlayers;
    }

    private bool IsCharacterConfirmedByOther(
        int characterIndex,
        PlayerSelection ignoredPlayer)
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerSelection player = players[i];

            if (player != ignoredPlayer &&
                player.IsConfirmed &&
                player.CharacterIndex == characterIndex)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveDisconnectedPlayers()
    {
        bool changed = false;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (FindGamepad(players[i].DeviceId) != null)
            {
                continue;
            }

            players.RemoveAt(i);
            changed = true;
        }

        if (changed)
        {
            RefreshView();
        }
    }

    private void EnterGameplay()
    {
        PlayOneShotAcrossSceneLoad(
            playerJoinedClip
        );

        List<GameplayPlayerAssignment> assignments =
            new List<GameplayPlayerAssignment>(players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            PlayerSelection player = players[i];
            Gamepad gamepad = FindGamepad(player.DeviceId);

            assignments.Add(
                new GameplayPlayerAssignment
                {
                    SlotIndex = i,
                    DeviceId = player.DeviceId,
                    DeviceIndex = FindGamepadIndex(gamepad),
                    DeviceName = player.DeviceName,
                    CharacterIndex = player.CharacterIndex
                }
            );
        }

        GameplaySessionStore.SetAssignments(assignments);
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void RefreshView()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            ChooseCharacterPlayerSlotView slot = playerSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (i >= players.Count)
            {
                slot.Render(i, false, false, string.Empty, 0);
                continue;
            }

            PlayerSelection player = players[i];
            slot.Render(
                i,
                true,
                player.IsConfirmed,
                player.DeviceName,
                player.CharacterIndex
            );
        }

        for (int characterIndex = 0;
             characterIndex < characterCards.Length;
             characterIndex++)
        {
            ChooseCharacterCardView card =
                characterCards[characterIndex];

            if (card == null)
            {
                continue;
            }

            string hoveringPlayers = string.Empty;
            string confirmedPlayer = string.Empty;

            for (int playerIndex = 0;
                 playerIndex < players.Count;
                 playerIndex++)
            {
                PlayerSelection player = players[playerIndex];

                if (player.CharacterIndex != characterIndex)
                {
                    continue;
                }

                string label = $"P{playerIndex + 1}";

                if (player.IsConfirmed)
                {
                    confirmedPlayer = label;
                }
                else
                {
                    hoveringPlayers =
                        string.IsNullOrEmpty(hoveringPlayers)
                            ? label
                            : hoveringPlayers + "  " + label;
                }
            }

            card.Render(hoveringPlayers, confirmedPlayer);
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (statusLabel == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(temporaryStatus))
        {
            statusLabel.text = temporaryStatus;
            return;
        }

        statusLabel.text = CanStart()
            ? "ALL READY  •  P1 PRESS NORTH"
            : "SOUTH: JOIN / CONFIRM   •   LEFT STICK: CHOOSE   •   EAST: BACK";
    }

    private void ShowTemporaryStatus(string message)
    {
        temporaryStatus = message;
        temporaryStatusTimer = 1.5f;
        RefreshStatus();
    }

    private bool CanStart()
    {
        if (players.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsConfirmed)
            {
                return false;
            }
        }

        return true;
    }

    private PlayerSelection FindPlayer(int deviceId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].DeviceId == deviceId)
            {
                return players[i];
            }
        }

        return null;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    private void PlayOneShotAcrossSceneLoad(
        AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        GameObject oneShotObject =
            new GameObject(
                "Character Selection Audio One Shot"
            );

        DontDestroyOnLoad(oneShotObject);

        AudioSource oneShotSource =
            oneShotObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.ignoreListenerPause = true;
        oneShotSource.PlayOneShot(clip, audioVolume);

        Destroy(
            oneShotObject,
            Mathf.Max(
                clip.length + 0.1f,
                2f
            )
        );
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        navigationThreshold =
            Mathf.Clamp(navigationThreshold, 0.1f, 1f);

        navigationReleaseThreshold =
            Mathf.Clamp(navigationReleaseThreshold, 0f, 0.9f);

        audioVolume = Mathf.Clamp(audioVolume, 0f, 2f);
        ResolveAudioSource();
        ConfigureAudioSource();
    }

    private static Gamepad FindGamepad(int deviceId)
    {
        IReadOnlyList<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            if (gamepads[i] != null &&
                gamepads[i].deviceId == deviceId)
            {
                return gamepads[i];
            }
        }

        return null;
    }

    private static int FindGamepadIndex(Gamepad gamepad)
    {
        if (gamepad == null)
        {
            return -1;
        }

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            if (Gamepad.all[i] == gamepad)
            {
                return i;
            }
        }

        return -1;
    }
}
