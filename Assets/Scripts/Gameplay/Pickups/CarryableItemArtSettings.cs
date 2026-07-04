using UnityEngine;

/// <summary>
/// 拾取物美术资源配置。
/// Runtime 时根据 CarryableItemType 动态获取 Sprite、动画等资源。
/// </summary>
[CreateAssetMenu(fileName = "CarryableItemArtSettings", menuName = "CiGa2026/Art Settings/Carryable Item")]
public class CarryableItemArtSettings : ScriptableObject
{
    [System.Serializable]
    public class ItemArtConfig
    {
        [Header("类型")]
        public CarryableItemType itemType;

        [Header("视觉资源")]
        public Sprite sprite;
        public Color color = Color.white;
        public RuntimeAnimatorController animatorController;

        [Header("UI 显示")]
        public string displayName;
        public Sprite iconSprite;
    }

    [Header("统一 Prefab")]
    [Tooltip("所有拾取物使用的统一 Prefab，根据类型动态配置。")]
    [SerializeField]
    private CarryableItem2D pickupPrefab;

    [Header("美术配置")]
    [SerializeField]
    private ItemArtConfig[] configs;

    /// <summary>
    /// 获取统一的拾取物 Prefab。
    /// </summary>
    public CarryableItem2D GetPickupPrefab()
    {
        return pickupPrefab;
    }

    /// <summary>
    /// 根据类型获取美术配置。
    /// </summary>
    public ItemArtConfig GetConfig(CarryableItemType itemType)
    {
        foreach (var config in configs)
        {
            if (config.itemType == itemType)
            {
                return config;
            }
        }

        Debug.LogWarning($"[CarryableItemArtSettings] 未找到类型 {itemType} 的美术配置。");
        return null;
    }

    /// <summary>
    /// 获取 Sprite。
    /// </summary>
    public Sprite GetSprite(CarryableItemType itemType)
    {
        var config = GetConfig(itemType);
        return config?.sprite;
    }

    /// <summary>
    /// 获取颜色。
    /// </summary>
    public Color GetColor(CarryableItemType itemType)
    {
        var config = GetConfig(itemType);
        return config != null ? config.color : Color.white;
    }

    /// <summary>
    /// 获取动画控制器。
    /// </summary>
    public RuntimeAnimatorController GetAnimatorController(CarryableItemType itemType)
    {
        var config = GetConfig(itemType);
        return config?.animatorController;
    }

    /// <summary>
    /// 获取显示名称。
    /// </summary>
    public string GetDisplayName(CarryableItemType itemType)
    {
        var config = GetConfig(itemType);
        return config?.displayName ?? itemType.ToString();
    }

    /// <summary>
    /// 获取图标。
    /// </summary>
    public Sprite GetIconSprite(CarryableItemType itemType)
    {
        var config = GetConfig(itemType);
        return config?.iconSprite;
    }
}
