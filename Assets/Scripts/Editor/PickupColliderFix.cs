using UnityEngine;
using UnityEditor;

/// <summary>
/// 为拾取物 Prefab 添加实体碰撞体，用于物理落地。
/// 当前 Prefab 只有触发器碰撞体，导致拾取物无法与地板和墙壁碰撞。
/// </summary>
public static class PickupColliderFix
{
    [MenuItem("Assets/CiGa2026/Fix Pickup Colliders", false, 100)]
    public static void FixPickupColliders()
    {
        // 获取选中的 Prefab
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogError("[PickupColliderFix] 请先选中要修复的拾取物 Prefab。");
            return;
        }

        int fixedCount = 0;

        foreach (GameObject prefab in selectedObjects)
        {
            if (prefab.GetComponent<CarryableItem2D>() == null)
            {
                Debug.LogWarning($"[PickupColliderFix] {prefab.name} 不是拾取物 Prefab，跳过。");
                continue;
            }

            if (FixSinglePickupCollider(prefab))
            {
                fixedCount++;
            }
        }

        Debug.Log($"[PickupColliderFix] 完成！修复了 {fixedCount} 个拾取物 Prefab。");
    }

    [MenuItem("Assets/CiGa2026/Fix Pickup Colliders", true)]
    public static bool ValidateFixPickupColliders()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static bool FixSinglePickupCollider(GameObject prefab)
    {
        // 查找现有的碰撞体
        PolygonCollider2D[] colliders = prefab.GetComponents<PolygonCollider2D>();

        if (colliders.Length >= 2)
        {
            Debug.Log($"[PickupColliderFix] {prefab.name} 已有 {colliders.Length} 个碰撞体，跳过。");
            return false;
        }

        if (colliders.Length == 0)
        {
            Debug.LogError($"[PickupColliderFix] {prefab.name} 没有碰撞体，无法修复。");
            return false;
        }

        // 现有的碰撞体应该是触发器
        PolygonCollider2D triggerCollider = colliders[0];

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[PickupColliderFix] {prefab.name} 的碰撞体不是触发器，将其设置为触发器。");
            triggerCollider.isTrigger = true;
        }

        // 复制触发器碰撞体的点
        Vector2[] originalPoints = triggerCollider.points;

        // 添加第二个碰撞体作为实体碰撞体
        PolygonCollider2D physicsCollider = prefab.AddComponent<PolygonCollider2D>();
        physicsCollider.isTrigger = false;
        physicsCollider.points = originalPoints;

        // 标记为已修改
        EditorUtility.SetDirty(prefab);

        Debug.Log($"[PickupColliderFix] {prefab.name} 修复完成：添加了实体碰撞体。");
        return true;
    }
}
