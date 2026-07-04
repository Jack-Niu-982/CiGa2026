using UnityEngine;

/// <summary>
/// 在船体内部随机位置生成拾取物。
/// 监听 GameplayEventBus 事件，用于调试测试。
/// </summary>
[DisallowMultipleComponent]
public class PickupSpawner : MonoBehaviour
{
    [Header("拾取物 Prefab")]
    [Tooltip("统一的拾取物 Prefab，根据类型动态配置。")]
    [SerializeField]
    private CarryableItem2D pickupPrefab;

    [Header("生成区域")]
    [Tooltip("生成物品的父级 Transform（可选，留空则生成在根级）。")]
    [SerializeField]
    private Transform spawnParent;

    [Tooltip("生成区域的本地坐标中心（相对于 PickupSpawner 自身位置）。")]
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
        if (pickupPrefab == null)
        {
            Debug.LogWarning(
                "[PickupSpawner] 没有配置拾取物 Prefab。"
            );
            return;
        }

        Vector3 worldPosition = GetRandomSpawnPosition();

        CarryableItem2D pickup = Instantiate(
            pickupPrefab,
            worldPosition,
            Quaternion.identity,
            spawnParent
        );

        pickup.name = $"{itemType}Pickup_Spawned";
        pickup.SetItemType(itemType);

        Rigidbody2D pickupRigidbody = pickup.GetComponent<Rigidbody2D>();
        if (pickupRigidbody != null)
        {
            pickupRigidbody.bodyType = RigidbodyType2D.Dynamic;
            pickupRigidbody.simulated = true;
            pickupRigidbody.gravityScale = 0.3f;
            pickupRigidbody.drag = 0.5f;
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

        Vector3 localPosition = spawnCenter + new Vector3(x, y, 0f);
        return transform.TransformPoint(localPosition);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
        {
            return;
        }

        // 将本地坐标转换为世界坐标
        Vector3 worldCenter = transform.TransformPoint(spawnCenter);

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(spawnCenter, spawnSize);
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = Color.yellow;
        Vector3 minHeightPosLocal = spawnCenter + new Vector3(-spawnSize.x * 0.5f, minSpawnHeight, 0f);
        Vector3 minHeightPos = transform.TransformPoint(minHeightPosLocal);
        Vector3 minHeightPosRight = transform.TransformPoint(spawnCenter + new Vector3(spawnSize.x * 0.5f, minSpawnHeight, 0f));
        Gizmos.DrawLine(minHeightPos, minHeightPosRight);

        Gizmos.color = Color.red;
        Vector3 maxHeightPosLeft = transform.TransformPoint(spawnCenter + new Vector3(-spawnSize.x * 0.5f, maxSpawnHeight, 0f));
        Vector3 maxHeightPosRight = transform.TransformPoint(spawnCenter + new Vector3(spawnSize.x * 0.5f, maxSpawnHeight, 0f));
        Gizmos.DrawLine(maxHeightPosLeft, maxHeightPosRight);
    }
}
