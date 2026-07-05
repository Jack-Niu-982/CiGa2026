using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelRuntimeBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform generatedLevelRoot;
    [SerializeField] private LevelTerrainRenderer2D terrainRenderer;
    [SerializeField] private LevelTerrainColliderBuilder colliderBuilder;
    [SerializeField] private Transform submarineRoot;
    [SerializeField] private Transform gameplayPlayerSpawnRoot;
    [SerializeField] private FinishZone2D finishZone;

    [Header("Options")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool useDefaultLevelWhenMissing = true;
    [SerializeField] private byte solidThreshold =
        LevelTerrainData.DefaultSolidThreshold;

    public static event Action LevelReady;

    public LevelTerrainData CurrentLevel { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        LevelReady = null;
    }

    private void Reset()
    {
        generatedLevelRoot = transform;
        terrainRenderer =
            GetComponentInChildren<LevelTerrainRenderer2D>(true);
        colliderBuilder =
            GetComponentInChildren<LevelTerrainColliderBuilder>(true);
        finishZone = FindObjectOfType<FinishZone2D>();
    }

    private void Start()
    {
        if (!buildOnStart)
        {
            return;
        }

        BuildLevel();
    }

    public bool BuildLevel()
    {
        if (!TryResolveLevel(out LevelTerrainData level))
        {
            Debug.LogWarning(
                "[LevelRuntimeBootstrap] No level selected and default level unavailable.",
                this
            );

            return false;
        }

        CurrentLevel = level;

        EnsureGeneratedHierarchy();

        LevelTerrainMeshData meshData =
            LevelTerrainMarchingSquares.Build(level, solidThreshold);

        if (terrainRenderer != null)
        {
            terrainRenderer.Render(meshData);
        }

        if (colliderBuilder != null)
        {
            colliderBuilder.Build(meshData);
        }

        ApplyStart(level);
        ApplyFinish(level);

        LevelReady?.Invoke();
        return true;
    }

    private bool TryResolveLevel(out LevelTerrainData level)
    {
        if (SelectedLevelStore.TryGetSelectedLevel(out level))
        {
            return true;
        }

        if (!useDefaultLevelWhenMissing)
        {
            level = null;
            return false;
        }

        level = LevelFileService.LoadOrCreateDefault();
        return level != null;
    }

    private void EnsureGeneratedHierarchy()
    {
        if (generatedLevelRoot == null)
        {
            generatedLevelRoot = transform;
        }

        if (terrainRenderer == null)
        {
            Transform visual =
                generatedLevelRoot.Find("TerrainVisual");

            if (visual == null)
            {
                visual = new GameObject("TerrainVisual").transform;
                visual.SetParent(generatedLevelRoot, false);
            }

            terrainRenderer =
                visual.GetComponent<LevelTerrainRenderer2D>();

            if (terrainRenderer == null)
            {
                terrainRenderer =
                    visual.gameObject.AddComponent<LevelTerrainRenderer2D>();
            }
        }

        if (colliderBuilder == null)
        {
            Transform colliderRoot =
                generatedLevelRoot.Find("TerrainColliders");

            if (colliderRoot == null)
            {
                colliderRoot =
                    new GameObject("TerrainColliders").transform;
                colliderRoot.SetParent(generatedLevelRoot, false);
            }

            colliderBuilder =
                colliderRoot.GetComponent<LevelTerrainColliderBuilder>();

            if (colliderBuilder == null)
            {
                colliderBuilder =
                    colliderRoot.gameObject.AddComponent<LevelTerrainColliderBuilder>();
            }
        }
    }

    private void ApplyStart(LevelTerrainData level)
    {
        if (level == null)
        {
            return;
        }

        if (submarineRoot != null)
        {
            submarineRoot.position = level.start.Position;
            submarineRoot.rotation = level.start.Rotation;
        }

        if (gameplayPlayerSpawnRoot == null)
        {
            return;
        }

        Vector3 offset =
            level.start.Position -
            (Vector2)gameplayPlayerSpawnRoot.position;

        gameplayPlayerSpawnRoot.position += offset;
    }

    private void ApplyFinish(LevelTerrainData level)
    {
        if (level == null ||
            finishZone == null)
        {
            return;
        }

        finishZone.transform.position = level.finish.Position;
        finishZone.transform.rotation = level.finish.Rotation;
    }
}
