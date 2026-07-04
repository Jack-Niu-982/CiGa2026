using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayPlayerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints =
        new Transform[RoomInputManager.MaxPlayers];

    [SerializeField] private Transform spawnedPlayersRoot;

    [Header("Fallback")]
    [SerializeField] private bool spawnOnePlayerWhenNoSession = true;

    private readonly List<GameObject> spawnedPlayers =
        new List<GameObject>(RoomInputManager.MaxPlayers);

    private void Start()
    {
        SpawnFromCurrentSession();
    }

    public void SpawnFromCurrentSession()
    {
        ClearSpawnedPlayers();
        GameplayPlayerRegistry.Clear();

        if (playerPrefab == null)
        {
            Debug.LogError(
                "[GameplayPlayerSpawner] Missing player prefab.",
                this
            );

            return;
        }

        IReadOnlyList<GameplayPlayerAssignment> assignments =
            GameplaySessionStore.Assignments;

        if (assignments.Count == 0)
        {
            if (spawnOnePlayerWhenNoSession)
            {
                SpawnFallbackPlayer();
            }

            return;
        }

        for (int i = 0; i < assignments.Count; i++)
        {
            SpawnPlayer(assignments[i], i);
        }
    }

    private void SpawnFallbackPlayer()
    {
        GameplayPlayerAssignment assignment =
            new GameplayPlayerAssignment
            {
                SlotIndex = 0,
                DeviceId = -1,
                DeviceIndex = -1,
                DeviceName = "Keyboard"
            };

        GameObject player =
            SpawnPlayer(assignment, 0);

        if (player == null)
        {
            return;
        }

        GamepadPlayerInput gamepadInput =
            player.GetComponent<GamepadPlayerInput>();

        if (gamepadInput != null)
        {
            gamepadInput.enabled = false;
        }

        KeyboardPlayerInput keyboardInput =
            player.GetComponent<KeyboardPlayerInput>();

        if (keyboardInput != null)
        {
            keyboardInput.enabled = true;
            BindPlayerInput(player, keyboardInput);
        }
    }

    private GameObject SpawnPlayer(
        GameplayPlayerAssignment assignment,
        int spawnOrder)
    {
        Transform spawnPoint =
            GetSpawnPoint(spawnOrder);

        Vector3 position =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        Quaternion rotation =
            spawnPoint != null
                ? spawnPoint.rotation
                : Quaternion.identity;

        GameObject player =
            Instantiate(
                playerPrefab,
                position,
                rotation,
                spawnedPlayersRoot
            );

        player.name = $"Player{assignment.SlotIndex + 1}";
        player.SetActive(true);
        spawnedPlayers.Add(player);

        GameplayPlayerIdentity identity =
            player.GetComponent<GameplayPlayerIdentity>();

        if (identity == null)
        {
            identity =
                player.AddComponent<GameplayPlayerIdentity>();
        }

        identity.Configure(assignment.SlotIndex);

        ConfigurePlayerInput(player, assignment);
        GameplayPlayerRegistry.Register(identity);

        return player;
    }

    private void ConfigurePlayerInput(
        GameObject player,
        GameplayPlayerAssignment assignment)
    {
        KeyboardPlayerInput keyboardInput =
            player.GetComponent<KeyboardPlayerInput>();

        if (keyboardInput != null)
        {
            keyboardInput.enabled = assignment.DeviceIndex < 0;
        }

        GamepadPlayerInput gamepadInput =
            player.GetComponent<GamepadPlayerInput>();

        if (gamepadInput == null)
        {
            if (assignment.DeviceIndex >= 0)
            {
                Debug.LogWarning(
                    $"[GameplayPlayerSpawner] {player.name} has no GamepadPlayerInput.",
                    player
                );
            }

            return;
        }

        bool useGamepad =
            assignment.DeviceIndex >= 0;

        gamepadInput.enabled = useGamepad;

        if (useGamepad)
        {
            gamepadInput.SetGamepadIndex(
                assignment.DeviceIndex
            );

            BindPlayerInput(player, gamepadInput);
        }
    }

    private static void BindPlayerInput(
        GameObject player,
        PlayerInputBase input)
    {
        PlayerController controller =
            player.GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.SetPlayerInput(input);
        }

        PlayerOperateInteractor2D interactor =
            player.GetComponent<PlayerOperateInteractor2D>();

        if (interactor != null)
        {
            interactor.SetPlayerInput(input);
        }
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null ||
            index < 0 ||
            index >= spawnPoints.Length)
        {
            return null;
        }

        return spawnPoints[index];
    }

    private void ClearSpawnedPlayers()
    {
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            if (spawnedPlayers[i] != null)
            {
                Destroy(spawnedPlayers[i]);
            }
        }

        spawnedPlayers.Clear();
        GameplayPlayerRegistry.Clear();
    }
}
