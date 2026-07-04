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

    [Header("方向密度生成")]
    [Tooltip("船体的 Rigidbody2D，用于获取位置和速度。留空则退化为均匀随机。")]
    [SerializeField]
    private Rigidbody2D boatRigidbody;

    private float spawnTimer;
    private float nextSpawnInterval;

    private void Start()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings != null)
        {
            nextSpawnInterval = Random.Range(
                settings.minSpawnInterval,
                settings.maxSpawnInterval
            );

            if (settings.spawnOnStart)
            {
                TrySpawn();
            }
        }
    }

    private void Update()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings == null)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer < nextSpawnInterval)
        {
            return;
        }

        spawnTimer = 0f;
        nextSpawnInterval = Random.Range(
            settings.minSpawnInterval,
            settings.maxSpawnInterval
        );

        TrySpawn();
    }

    public bool TrySpawn()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings == null)
        {
            return false;
        }

        if (GetAliveCount() >= settings.maxAliveItems)
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

        Vector2 spawnPosition;
        bool foundValidPosition = false;

        for (int attempt = 0; attempt < settings.maxSpawnAttempts; attempt++)
        {
            spawnPosition = zone.GetRandomWorldPosition();

            if (IsPositionValid(spawnPosition, settings))
            {
                FloatingItem2D item =
                    Instantiate(
                        prefab,
                        spawnPosition,
                        Quaternion.identity,
                        activeItemsRoot
                    );

                item.SetDriftVelocity(
                    zone.DriftVelocity
                );

                foundValidPosition = true;
                break;
            }
        }

        return foundValidPosition;
    }

    private bool IsPositionValid(Vector2 position, FloatingItemSettings settings)
    {
        if (boatRigidbody != null)
        {
            float distanceToBoat = Vector2.Distance(
                position,
                boatRigidbody.position
            );

            if (distanceToBoat < settings.boatExclusionRadius)
            {
                return false;
            }
        }

        if (activeItemsRoot == null)
        {
            return true;
        }

        for (int i = 0; i < activeItemsRoot.childCount; i++)
        {
            Transform child = activeItemsRoot.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector2.Distance(
                position,
                child.position
            );

            if (distance < settings.minItemDistance)
            {
                return false;
            }
        }

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

        if (boatRigidbody == null)
        {
            return spawnZones[
                Random.Range(0, spawnZones.Length)
            ];
        }

        return PickZoneWeighted();
    }

    private FloatingItemSpawnZone2D PickZoneWeighted()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings == null)
        {
            return spawnZones[
                Random.Range(0, spawnZones.Length)
            ];
        }

        float boatX = boatRigidbody.position.x;

        CountItemsPerSector(
            boatX,
            settings.deadZoneHalfWidth,
            out int leftCount,
            out int rightCount
        );

        int total = leftCount + rightCount;

        float leftInvDensity;
        float rightInvDensity;

        if (total > 0)
        {
            leftInvDensity = Mathf.Max(
                1f - (float)leftCount / total,
                settings.minSideWeight
            );

            rightInvDensity = Mathf.Max(
                1f - (float)rightCount / total,
                settings.minSideWeight
            );
        }
        else
        {
            leftInvDensity = 1f;
            rightInvDensity = 1f;
        }

        float normalizedVel = Mathf.Clamp(
            boatRigidbody.velocity.x / settings.referenceSpeed,
            -1f,
            1f
        );

        float leftVelBias =
            1f - settings.velocityBiasStrength * normalizedVel;

        float rightVelBias =
            1f + settings.velocityBiasStrength * normalizedVel;

        float totalWeight = 0f;

        for (int i = 0; i < spawnZones.Length; i++)
        {
            totalWeight +=
                ComputeZoneWeight(
                    spawnZones[i],
                    boatX,
                    settings.deadZoneHalfWidth,
                    leftInvDensity,
                    rightInvDensity,
                    leftVelBias,
                    rightVelBias
                );
        }

        if (totalWeight <= 0f)
        {
            return spawnZones[
                Random.Range(0, spawnZones.Length)
            ];
        }

        float roll =
            Random.Range(0f, totalWeight);

        for (int i = 0; i < spawnZones.Length; i++)
        {
            roll -= ComputeZoneWeight(
                spawnZones[i],
                boatX,
                settings.deadZoneHalfWidth,
                leftInvDensity,
                rightInvDensity,
                leftVelBias,
                rightVelBias
            );

            if (roll <= 0f)
            {
                return spawnZones[i];
            }
        }

        return spawnZones[spawnZones.Length - 1];
    }

    private float ComputeZoneWeight(
        FloatingItemSpawnZone2D zone,
        float boatX,
        float deadZoneHalfWidth,
        float leftInvDensity,
        float rightInvDensity,
        float leftVelBias,
        float rightVelBias)
    {
        float dx =
            zone.transform.position.x - boatX;

        if (dx < -deadZoneHalfWidth)
        {
            return leftInvDensity * leftVelBias;
        }

        if (dx > deadZoneHalfWidth)
        {
            return rightInvDensity * rightVelBias;
        }

        return 0.5f *
            (leftInvDensity * leftVelBias +
             rightInvDensity * rightVelBias);
    }

    private void CountItemsPerSector(
        float boatX,
        float deadZoneHalfWidth,
        out int leftCount,
        out int rightCount)
    {
        leftCount = 0;
        rightCount = 0;

        if (activeItemsRoot == null)
        {
            return;
        }

        for (int i = 0; i < activeItemsRoot.childCount; i++)
        {
            GameObject child =
                activeItemsRoot.GetChild(i).gameObject;

            if (!child.activeInHierarchy)
            {
                continue;
            }

            float dx =
                child.transform.position.x - boatX;

            if (dx < -deadZoneHalfWidth)
            {
                leftCount++;
            }
            else if (dx > deadZoneHalfWidth)
            {
                rightCount++;
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        FloatingItemSettings settings =
            SettingManager.FloatingItem;

        if (settings == null || boatRigidbody == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(
            boatRigidbody.position,
            settings.boatExclusionRadius
        );

        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawSphere(
            boatRigidbody.position,
            settings.boatExclusionRadius
        );

        if (activeItemsRoot != null && settings.minItemDistance > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);

            for (int i = 0; i < activeItemsRoot.childCount; i++)
            {
                Transform child = activeItemsRoot.GetChild(i);

                if (child.gameObject.activeInHierarchy)
                {
                    Gizmos.DrawWireSphere(
                        child.position,
                        settings.minItemDistance
                    );
                }
            }
        }
    }
}
