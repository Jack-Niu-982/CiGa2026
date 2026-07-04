using UnityEngine;

/// <summary>
/// 第一版漂浮物生成器：按固定间隔在若干区域中生成漂浮物。
/// </summary>
[DisallowMultipleComponent]
public class FloatingItemSpawner2D : MonoBehaviour
{
    [System.Serializable]
    private struct SpawnEntry
    {
        public FloatingItem2D prefab;

        [Min(0f)]
        public float weight;
    }

    [SerializeField]
    private FloatingItemSpawnZone2D[] spawnZones;

    [SerializeField]
    private SpawnEntry[] entries;

    [SerializeField]
    private Transform activeItemsRoot;

    [Min(0.1f)]
    [SerializeField]
    private float spawnInterval = 4f;

    [Min(0)]
    [SerializeField]
    private int maxAliveItems = 8;

    [SerializeField]
    private bool spawnOnStart = true;

    private float spawnTimer;

    private void Start()
    {
        if (spawnOnStart)
        {
            TrySpawn();
        }
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
        {
            return;
        }

        spawnTimer = 0f;
        TrySpawn();
    }

    public bool TrySpawn()
    {
        if (GetAliveCount() >= maxAliveItems)
        {
            return false;
        }

        FloatingItemSpawnZone2D zone =
            PickZone();

        FloatingItem2D prefab =
            PickPrefab();

        if (zone == null ||
            prefab == null)
        {
            return false;
        }

        FloatingItem2D item =
            Instantiate(
                prefab,
                zone.GetRandomWorldPosition(),
                Quaternion.identity,
                activeItemsRoot
            );

        item.SetDriftVelocity(
            zone.DriftVelocity
        );

        return true;
    }

    private int GetAliveCount()
    {
        if (activeItemsRoot == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < activeItemsRoot.childCount; i++)
        {
            if (activeItemsRoot.GetChild(i).gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private FloatingItemSpawnZone2D PickZone()
    {
        if (spawnZones == null ||
            spawnZones.Length == 0)
        {
            return null;
        }

        return spawnZones[
            Random.Range(0, spawnZones.Length)
        ];
    }

    private FloatingItem2D PickPrefab()
    {
        if (entries == null ||
            entries.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            totalWeight +=
                Mathf.Max(0f, entries[i].weight);
        }

        if (totalWeight <= 0f)
        {
            return entries[0].prefab;
        }

        float roll =
            Random.Range(0f, totalWeight);

        for (int i = 0; i < entries.Length; i++)
        {
            roll -=
                Mathf.Max(0f, entries[i].weight);

            if (roll <= 0f)
            {
                return entries[i].prefab;
            }
        }

        return entries[entries.Length - 1].prefab;
    }
}
