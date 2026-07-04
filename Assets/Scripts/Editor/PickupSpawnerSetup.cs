using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动在场景中创建并配置 PickupSpawner。
/// </summary>
public static class PickupSpawnerSetup
{
    [MenuItem("Tools/Setup PickupSpawner in Scene")]
    public static void SetupPickupSpawner()
    {
        // 检查场景中是否已存在 PickupSpawner
        PickupSpawner existing = Object.FindObjectOfType<PickupSpawner>();
        if (existing != null)
        {
            Debug.LogWarning("[PickupSpawnerSetup] 场景中已存在 PickupSpawner，跳过创建。");
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            return;
        }

        // 创建 GameObject
        GameObject spawnerGO = new GameObject("PickupSpawner");
        PickupSpawner spawner = spawnerGO.AddComponent<PickupSpawner>();

        // 加载 Prefab 引用
        string basePath = "Assets/Prefabs/Gameplay/Pickups";
        CarryableItem2D fuelPrefab = AssetDatabase.LoadAssetAtPath<CarryableItem2D>($"{basePath}/FuelPickup.prefab");
        CarryableItem2D shieldPrefab = AssetDatabase.LoadAssetAtPath<CarryableItem2D>($"{basePath}/ShieldPickup.prefab");
        CarryableItem2D trashPrefab = AssetDatabase.LoadAssetAtPath<CarryableItem2D>($"{basePath}/TrashPickup.prefab");

        // 使用 SerializedObject 设置私有字段
        SerializedObject so = new SerializedObject(spawner);

        so.FindProperty("fuelPickupPrefab").objectReferenceValue = fuelPrefab;
        so.FindProperty("shieldPickupPrefab").objectReferenceValue = shieldPrefab;
        so.FindProperty("trashPickupPrefab").objectReferenceValue = trashPrefab;

        so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 1f, 0f);
        so.FindProperty("spawnSize").vector3Value = new Vector3(5f, 2f, 0f);
        so.FindProperty("minSpawnHeight").floatValue = 0.5f;
        so.FindProperty("maxSpawnHeight").floatValue = 2.5f;
        so.FindProperty("showDebugGizmos").boolValue = true;

        so.ApplyModifiedProperties();

        // 标记场景为脏（需要保存）
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // 选中创建的对象
        Selection.activeGameObject = spawnerGO;
        EditorGUIUtility.PingObject(spawnerGO);

        Debug.Log($"[PickupSpawnerSetup] 成功创建 PickupSpawner 并配置 Prefab 引用：\n" +
                  $"- Fuel: {(fuelPrefab != null ? "✓" : "✗")}\n" +
                  $"- Shield: {(shieldPrefab != null ? "✓" : "✗")}\n" +
                  $"- Trash: {(trashPrefab != null ? "✓" : "✗")}\n" +
                  $"请保存场景 (Ctrl+S)。");
    }
}
