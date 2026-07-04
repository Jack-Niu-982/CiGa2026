# Prefab 设计规范

本文档定义项目中 Prefab 的设计规范，确保代码和资源的可维护性。

## 核心原则

**单一 Prefab + 枚举配置**：当多个对象本质上是同一种类型，只是视觉或行为上有差异时，应该使用统一的 Prefab，通过枚举控制其变体。

### 优势

1. **统一修改**：修改物理、碰撞体、逻辑等共性配置时，只需修改一个 Prefab
2. **减少冗余**：避免维护多个几乎相同的 Prefab
3. **降低错误**：减少因 Prefab 不同步导致的 bug
4. **易于扩展**：添加新类型只需扩展枚举和 ArtSettings，无需新建 Prefab

### 不适用场景

- 对象的结构完全不同（如玩家 vs 敌人）
- 组件配置差异巨大，无法抽象
- 性能要求极高，需要针对性优化

## 设计模式

### 1. 枚举定义类型

```csharp
/// <summary>
/// 拾取物类型枚举。
/// </summary>
public enum CarryableItemType
{
    Unknown = 0,
    Fuel = 1,
    Shield = 2,
    Trash = 3
}
```

### 2. ArtSettings 分离美术资源

**不要在 Prefab 上直接配置美术资源**，而是通过 ScriptableObject 管理：

```csharp
[CreateAssetMenu(fileName = "CarryableItemArtSettings", menuName = "CiGa2026/Art Settings/Carryable Item")]
public class CarryableItemArtSettings : ScriptableObject
{
    [System.Serializable]
    public class ItemArtConfig
    {
        public CarryableItemType itemType;
        public Sprite sprite;
        public Color color = Color.white;
        public RuntimeAnimatorController animatorController;
        public string displayName;
        public Sprite iconSprite;
    }

    [SerializeField]
    private ItemArtConfig[] configs;

    public ItemArtConfig GetConfig(CarryableItemType itemType)
    {
        // 查找逻辑
    }
}
```

**为什么要分离？**
- Prefab 只包含逻辑和结构
- 美术资源集中管理，易于批量修改
- Runtime 时动态加载，支持换肤等功能
- 美术和程序分工明确

### 3. Prefab 组件设计

```csharp
public class CarryableItem2D : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField]
    private CarryableItemType itemType = CarryableItemType.Unknown;

    [Header("美术资源")]
    [SerializeField]
    private CarryableItemArtSettings artSettings;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Animator animator;

    // Runtime 时根据 itemType 从 artSettings 获取资源
    private void Awake()
    {
        ApplyArtConfig();
    }

    public void SetItemType(CarryableItemType newItemType)
    {
        itemType = newItemType;
        ApplyArtConfig();
    }

    private void ApplyArtConfig()
    {
        if (artSettings == null) return;

        var config = artSettings.GetConfig(itemType);
        if (config == null) return;

        spriteRenderer.sprite = config.sprite;
        spriteRenderer.color = config.color;
        animator.runtimeAnimatorController = config.animatorController;
    }
}
```

### 4. 生成器使用统一 Prefab

```csharp
public class PickupSpawner : MonoBehaviour
{
    [SerializeField]
    private CarryableItem2D pickupPrefab; // 统一 Prefab

    public void SpawnPickup(CarryableItemType itemType)
    {
        CarryableItem2D pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
        pickup.SetItemType(itemType); // 设置类型，自动应用美术资源
        pickup.name = $"{itemType}Pickup_Spawned";
    }
}
```

## 实施示例：拾取物系统

### 旧设计（不推荐）

```
Assets/Prefabs/Gameplay/Pickups/
├── FuelPickup.prefab      ← 独立 Prefab，包含 Sprite、Animator
├── ShieldPickup.prefab    ← 独立 Prefab，包含 Sprite、Animator
└── TrashPickup.prefab     ← 独立 Prefab，包含 Sprite、Animator
```

**问题**：
- 修改碰撞体需要改 3 个 Prefab
- 修改物理参数需要改 3 个 Prefab
- 容易出现配置不一致

### 新设计（推荐）

```
Assets/Prefabs/Gameplay/Pickups/
└── CarryableItemPickup.prefab   ← 统一 Prefab，只包含结构和逻辑

Assets/Settings/Art/
└── CarryableItemArtSettings.asset  ← ScriptableObject，包含所有类型的美术资源
    ├── Fuel: FuelSprite, Color, Animator
    ├── Shield: ShieldSprite, Color, Animator
    └── Trash: TrashSprite, Color, Animator
```

**优势**：
- 修改碰撞体只需改 1 个 Prefab
- 修改美术资源只需改 1 个 ArtSettings
- 添加新类型只需在 ArtSettings 中添加配置

## Prefab 结构规范

### 必需组件

每个统一 Prefab 应包含：

1. **核心脚本**：如 `CarryableItem2D.cs`
   - 包含类型枚举字段
   - 引用 ArtSettings
   - 提供 `SetItemType()` 方法

2. **美术组件**：如 `SpriteRenderer`、`Animator`
   - **不直接配置资源**
   - 由脚本从 ArtSettings 动态赋值

3. **物理组件**：如 `Rigidbody2D`、`Collider2D`
   - 配置共性物理参数
   - 如有差异，通过脚本根据类型调整

### 命名规范

- **Prefab 名称**：使用通用名称，如 `CarryableItemPickup`（不是 `FuelPickup`）
- **ArtSettings 名称**：`{系统名称}ArtSettings`，如 `CarryableItemArtSettings`
- **实例命名**：Runtime 生成时动态设置，如 `FuelPickup_Spawned`

## 其他适用场景

这种模式适用于所有"同类型对象的变体"：

### 1. 漂浮物（FloatingItem）

- 统一 Prefab：`FloatingItem.prefab`
- 枚举：`FloatingItemType { Fuel, Shield, Trash, Bomb, Web }`
- ArtSettings：`FloatingItemArtSettings.asset`

### 2. 敌人（Enemy）

- 统一 Prefab：`Enemy.prefab`
- 枚举：`EnemyType { FastShark, TankWhale, ShooterJellyfish }`
- ArtSettings：`EnemyArtSettings.asset`

### 3. UI 按钮（Button）

- 统一 Prefab：`GameButton.prefab`
- 枚举：`ButtonType { Primary, Secondary, Danger, Success }`
- ArtSettings：`ButtonArtSettings.asset`

### 4. 粒子效果（Particle）

- 统一 Prefab：`ParticleEffect.prefab`
- 枚举：`EffectType { Explosion, Splash, Smoke }`
- ArtSettings：`ParticleEffectArtSettings.asset`

## 迁移指南

### 步骤 1：创建枚举

```csharp
public enum YourItemType
{
    Unknown = 0,
    TypeA = 1,
    TypeB = 2
}
```

### 步骤 2：创建 ArtSettings

```csharp
[CreateAssetMenu(fileName = "YourItemArtSettings", menuName = "CiGa2026/Art Settings/Your Item")]
public class YourItemArtSettings : ScriptableObject
{
    [System.Serializable]
    public class ItemArtConfig
    {
        public YourItemType itemType;
        public Sprite sprite;
        // 其他美术资源
    }

    [SerializeField]
    private ItemArtConfig[] configs;

    public ItemArtConfig GetConfig(YourItemType itemType) { }
}
```

### 步骤 3：修改组件脚本

```csharp
public class YourItem : MonoBehaviour
{
    [SerializeField] private YourItemType itemType;
    [SerializeField] private YourItemArtSettings artSettings;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        ApplyArtConfig();
    }

    public void SetItemType(YourItemType newItemType)
    {
        itemType = newItemType;
        ApplyArtConfig();
    }

    private void ApplyArtConfig()
    {
        var config = artSettings?.GetConfig(itemType);
        if (config != null)
        {
            spriteRenderer.sprite = config.sprite;
        }
    }
}
```

### 步骤 4：创建统一 Prefab

1. 删除旧的多个 Prefab（或保留作为参考）
2. 创建新的统一 Prefab
3. 只配置结构和逻辑组件
4. 引用 ArtSettings

### 步骤 5：创建 ArtSettings Asset

1. 在 Unity 中：`Assets → Create → CiGa2026 → Art Settings → Your Item`
2. 为每个类型配置美术资源
3. 将 ArtSettings 引用到 Prefab

### 步骤 6：更新生成器

```csharp
// 旧代码
[SerializeField] private YourItem typeAPrefab;
[SerializeField] private YourItem typeBPrefab;

// 新代码
[SerializeField] private YourItem itemPrefab; // 统一 Prefab

public void Spawn(YourItemType type)
{
    YourItem item = Instantiate(itemPrefab, position, Quaternion.identity);
    item.SetItemType(type);
}
```

## 验收标准

重构完成后，应满足：

- ✓ 同类型对象只有一个 Prefab
- ✓ Prefab 中没有直接配置 Sprite、Animation 等美术资源
- ✓ 所有美术资源在 ArtSettings ScriptableObject 中
- ✓ 组件脚本提供 `SetItemType()` 方法
- ✓ 生成器使用统一 Prefab + SetItemType
- ✓ 在编辑器中修改 itemType 可以预览对应美术资源（OnValidate）

## 相关文档

- `Assets/Scripts/Gameplay/Pickups/CarryableItem2D.cs` - 拾取物实现示例
- `Assets/Scripts/Gameplay/Pickups/CarryableItemArtSettings.cs` - ArtSettings 实现示例
- `Docs/Technical/CarryableItemSystem.md` - 拾取物系统设计
