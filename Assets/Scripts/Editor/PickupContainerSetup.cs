using UnityEngine;
using UnityEditor;

/// <summary>
/// 在 InsideCircle 下创建 PickupContainer 对象。
/// 用于解决拾取物作为 Submarine (Dynamic Rigidbody2D) 子对象导致的物理冲突。
/// </summary>
public static class PickupContainerSetup
{
    [MenuItem("GameObject/CiGa2026/Setup Pickup Container", false, 10)]
    public static void CreatePickupContainer()
    {
        // 1. 查找 InsideCircle
        GameObject insideCircle = GameObject.Find("InsideCircle");

        if (insideCircle == null)
        {
            Debug.LogError("[PickupContainerSetup] 未找到 InsideCircle 对象。请确保在 Jaeger 场景中执行。");
            return;
        }

        // 2. 检查是否已存在 PickupContainer
        Transform existingContainer = insideCircle.transform.Find("PickupContainer");

        if (existingContainer != null)
        {
            Debug.LogWarning("[PickupContainerSetup] PickupContainer 已存在，跳过创建。");
            Selection.activeGameObject = existingContainer.gameObject;
            return;
        }

        // 3. 创建 PickupContainer
        GameObject pickupContainer = new GameObject("PickupContainer");
        pickupContainer.transform.SetParent(insideCircle.transform, false);
        pickupContainer.transform.localPosition = Vector3.zero;
        pickupContainer.transform.localRotation = Quaternion.identity;
        pickupContainer.transform.localScale = Vector3.one;

        // 4. 设置 Layer 为 SubmarineInterior
        int submarineInteriorLayer = LayerMask.NameToLayer("SubmarineInterior");

        if (submarineInteriorLayer >= 0)
        {
            pickupContainer.layer = submarineInteriorLayer;
            Debug.Log($"[PickupContainerSetup] 设置 Layer 为 SubmarineInterior ({submarineInteriorLayer})");
        }
        else
        {
            Debug.LogWarning("[PickupContainerSetup] 未找到 SubmarineInterior Layer，使用 Default。");
        }

        // 5. 标记场景为已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        // 6. 选中新创建的对象
        Selection.activeGameObject = pickupContainer;

        Debug.Log("[PickupContainerSetup] PickupContainer 创建成功！位置：InsideCircle/PickupContainer");
    }

    [MenuItem("GameObject/CiGa2026/Setup Pickup Container", true)]
    public static bool ValidateCreatePickupContainer()
    {
        // 只在 Play Mode 之外可用
        return !Application.isPlaying;
    }
}
