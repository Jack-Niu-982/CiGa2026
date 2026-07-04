using UnityEngine;

/// <summary>
/// 在船体内部随机位置生成拾取物。
/// 监听 GameplayEventBus 事件，用于调试测试。
/// </summary>
[DisallowMultipleComponent]
public class PickupSpawner : MonoBehaviour
{
    [Header("拾取物 Prefabs")]
    [SerializeField]
    private CarryableItem2D fuelPickupPrefab;

    [SerializeField]
    private CarryableItem2D shieldPickupPrefab;

    [SerializeField]
    private CarryableItem2D trashPickupPrefab;

    [Header("生成区域")]
    [Tooltip("生成物品的父级 Transform（可选，留空则生成在根级）。")]
    [SerializeField]
    private Transform spawnParent;

    [Tooltip("生成区域的世界坐标中心。")]
    [SerializeField]
    private Vector3 spawnCenter = Vector3.zero;

    [Tooltip("生成区域的大小。")]
    [SerializeField]
    private Vector3 spawnSize = new Vector3(8f, 3f, 0f);

    [Header("生成设置")]
    [Tooltip("生成位置的最小高度（避免生成在地板上）。")]
    [SerializeField]
    private float minSpawnHeight = 0.5f;

    [Tooltip("生成位置的最大高度（避免生成在天花板上）。")]
    [SerializeField]
    private float maxSpawnHeight = 2.5f;

    [Tooltip("生成时的初始下落高度偏移。")]
    [SerializeField]
    private float dropHeightOffset = 0.3f;

    [Header("调试")]
    [SerializeField]
    private bool showDebugGizmos = true;

    private void OnEnable()
    {
        GameplayEventBus.SpawnRandomPickupRequested += OnSpawnRandomPickupRequested;
        GameplayEventBus.SpawnAllPickupsRequested += OnSpawnAllPickupsRequested;
    }

    private void OnDisable()
    {
        GameplayEventBus.SpawnRandomPickupRequested -= OnSpawnRandomPickupRequested;
        GameplayEventBus.SpawnAllPickupsRequested -= OnSpawnAllPickupsRequested;
    }

    private void OnSpawnRandomPickupRequested(CarryableItemType itemType)
    {
        SpawnPickup(itemType);
    }

    private void OnSpawnAllPickupsRequested()
    {
        SpawnPickup(CarryableItemType.Fuel);
        SpawnPickup(CarryableItemType.Shield);
        SpawnPickup(CarryableItemType.Trash);
    }

    public void SpawnPickup(CarryableItemType itemType)
    {
        CarryableItem2D prefab = GetPrefabForType(itemType);

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[PickupSpawner] 没有为类型 {itemType} 配置 Prefab。"
            );
            return;
        }

        Vector3 worldPosition = GetRandomSpawnPosition();

        CarryableItem2D pickup = Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity,
            spawnParent
        );

        pickup.name = $"{itemType}Pickup_Spawned";

        Rigidbody2D pickupRigidbody = pickup.GetComponent<Rigidbody2D>();
        if (pickupRigidbody != null)
        {
            pickupRigidbody.bodyType = RigidbodyType2D.Dynamic;
            pickupRigidbody.simulated = true;
            pickupRigidbody.gravityScale = 1f;
            pickupRigidbody.velocity = Vector2.zero;
            pickupRigidbody.angularVelocity = 0f;
        }

        Debug.Log(
            $"[PickupSpawner] 生成 {itemType} 在位置 {worldPosition}"
        );
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(
            -spawnSize.x * 0.5f,
            spawnSize.x * 0.5f
        );

        float y = Random.Range(
            minSpawnHeight,
            maxSpawnHeight
        ) + dropHeightOffset;

        return spawnCenter + new Vector3(x, y, 0f);
    }

    private CarryableItem2D GetPrefabForType(CarryableItemType itemType)
    {
        switch (itemType)
        {
            case CarryableItemType.Fuel:
                return fuelPickupPrefab;

            case CarryableItemType.Shield:
                return shieldPickupPrefab;

            case CarryableItemType.Trash:
                return trashPickupPrefab;

            default:
                return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
        {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(spawnCenter, spawnSize);

        Gizmos.color = Color.yellow;
        Vector3 minHeightPos = spawnCenter + new Vector3(-spawnSize.x * 0.5f, minSpawnHeight, 0f);
        Vector3 maxHeightPosLeft = spawnCenter + new Vector3(-spawnSize.x * 0.5f, maxSpawnHeight, 0f);
        Vector3 maxHeightPosRight = spawnCenter + new Vector3(spawnSize.x * 0.5f, maxSpawnHeight, 0f);

        Gizmos.DrawLine(minHeightPos, minHeightPos + new Vector3(spawnSize.x, 0f, 0f));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(maxHeightPosLeft, maxHeightPosRight);
    }
}
