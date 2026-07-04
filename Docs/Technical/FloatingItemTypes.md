# 漂浮物类型系统技术文档

## 概述

漂浮物类型系统定义了船外可被锚钩捕获的 5 种漂浮物类型，以及它们被勾回后的不同效果处理机制。

## 核心类型

### FloatingItemType 枚举

定义所有漂浮物类型：

```csharp
public enum FloatingItemType
{
    Unknown = 0,
    Fuel = 1,      // 燃料 - 生成可拾取物
    Bomb = 2,      // 炸弹 - 直接伤害船体
    Trash = 3,     // 垃圾 - 生成可拾取物
    Shield = 4,    // 护盾 - 生成可拾取物
    Web = 5        // 蛛网 - 禁用锚点
}
```

### CarryableItemType 枚举

定义船内可拾取物类型（玩家可搬运）：

```csharp
public enum CarryableItemType
{
    Unknown = 0,
    Fuel = 1,      // 燃料拾取物
    Shield = 2,    // 护盾拾取物
    Trash = 3      // 垃圾拾取物
}
```

**注意**：炸弹和蛛网不生成拾取物，因此不在 CarryableItemType 中。

## 架构设计

### 类型映射关系

```
FloatingItemType → Effect
├── Fuel    → SpawnPickup(CarryableItemType.Fuel)
├── Shield  → SpawnPickup(CarryableItemType.Shield)
├── Trash   → SpawnPickup(CarryableItemType.Trash)
├── Bomb    → DamageSubmarine(damage)
└── Web     → DisableAnchor(duration)
```

### 核心组件

#### 1. FloatingItem2D.cs

漂浮物主组件，负责：
- 漂移运动
- 被锚拉回
- 到达后根据类型执行不同效果

**关键字段**：
```csharp
[SerializeField] private FloatingItemType floatingItemType;
[SerializeField] private CarryableItem2D pickupPrefab;  // 仅 Fuel/Shield/Trash 使用
[SerializeField] private FloatingItemEffectData effectData;  // 炸弹伤害、蛛网时长
```

**核心方法**：
```csharp
// 到达船内后的处理
private void SpawnPickupAndDestroy(Vector2 worldPosition)
{
    switch (floatingItemType)
    {
        case FloatingItemType.Bomb:
            ApplyBombEffect();
            break;
        case FloatingItemType.Web:
            ApplyWebEffect();
            break;
        case FloatingItemType.Fuel:
        case FloatingItemType.Shield:
        case FloatingItemType.Trash:
            SpawnPickupItem(worldPosition);
            break;
    }
    Destroy(gameObject);
}
```

#### 2. FloatingItemEffectData.cs

ScriptableObject 配置，定义效果参数：

```csharp
[CreateAssetMenu(fileName = "FloatingItemEffectData", menuName = "Game/Floating Item Effect Data")]
public class FloatingItemEffectData : ScriptableObject
{
    [Header("炸弹效果")]
    public float bombDamage = 20f;

    [Header("蛛网效果")]
    public float webDisableDuration = 5f;
}
```

**使用方式**：
1. 在 Unity 中创建资产：`Create > Game > Floating Item Effect Data`
2. 配置参数（炸弹伤害、蛛网时长）
3. 在 FloatingItem2D Prefab 中引用

## 效果实现

### 炸弹效果

勾回后直接对船体造成伤害：

```csharp
private void ApplyBombEffect()
{
    if (effectData == null) return;

    // 发送事件通知船体受损
    GameplayEventBus.Publish(new SubmarineHealthChangedEvent
    {
        DamageAmount = effectData.bombDamage,
        Source = "Bomb"
    });
}
```

**集成要点**：
- 船体健康系统需要监听 `SubmarineHealthChangedEvent`
- UI 需要显示伤害反馈（红屏闪烁、震动等）
- 音效：爆炸音效

### 蛛网效果

勾回后禁用对应的锚点：

```csharp
private void ApplyWebEffect()
{
    if (effectData == null || activeDropPoint == null) return;

    AnchorLauncher2D anchor = activeDropPoint.GetComponentInParent<AnchorLauncher2D>();
    if (anchor != null)
    {
        // 发送事件通知锚点被禁用
        GameplayEventBus.Publish(new AnchorDisabledEvent
        {
            Anchor = anchor,
            Duration = effectData.webDisableDuration
        });
    }
}
```

**集成要点**：
- AnchorLauncher2D 需要监听 `AnchorDisabledEvent`
- 添加 `DisableForDuration(float duration)` 方法
- UI 需要显示禁用状态（图标变灰、计时器）
- 音效：蛛网粘住音效

### 生成拾取物效果

生成船内可拾取物：

```csharp
private void SpawnPickupItem(Vector2 worldPosition)
{
    if (pickupPrefab == null) return;

    CarryableItem2D pickup = Instantiate(pickupPrefab, worldPosition, Quaternion.identity);
    
    Transform pickupParent = FindPickupParent();
    if (pickupParent != null)
    {
        pickup.transform.SetParent(pickupParent, true);
    }

    pickup.name = $"{pickupPrefab.name}_{floatingItemType}_Drop";
    EnablePickupPhysics(pickup);
}
```

**集成要点**：
- pickupPrefab 必须配置（对 Fuel/Shield/Trash）
- 拾取物生成在船内的 SubmarineInteriorFollower2D 下
- 拾取物有物理属性，会掉落到地板上

## 事件系统

### SubmarineHealthChangedEvent

```csharp
public struct SubmarineHealthChangedEvent
{
    public float DamageAmount;  // 伤害量（正数为扣血）
    public string Source;       // 伤害来源
}
```

**订阅示例**：
```csharp
void OnEnable()
{
    GameplayEventBus.Subscribe<SubmarineHealthChangedEvent>(OnHealthChanged);
}

void OnHealthChanged(SubmarineHealthChangedEvent evt)
{
    if (evt.Source == "Bomb")
    {
        currentHealth -= evt.DamageAmount;
        // 播放爆炸效果
    }
}
```

### AnchorDisabledEvent

```csharp
public struct AnchorDisabledEvent
{
    public AnchorLauncher2D Anchor;  // 被禁用的锚点
    public float Duration;           // 禁用时长
}
```

**订阅示例**：
```csharp
void OnEnable()
{
    GameplayEventBus.Subscribe<AnchorDisabledEvent>(OnAnchorDisabled);
}

void OnAnchorDisabled(AnchorDisabledEvent evt)
{
    if (evt.Anchor == this)
    {
        StartCoroutine(DisableForDuration(evt.Duration));
    }
}
```

## Prefab 配置

### 漂浮物 Prefab 配置指南

每种漂浮物类型都需要一个独立的 Prefab：

#### Fuel Prefab
```
Components:
- FloatingItem2D
  - Floating Item Type: Fuel
  - Pickup Prefab: [FuelPickup Prefab]
  - Effect Data: [FloatingItemEffectData]
- SpriteRenderer (黄色油桶)
- Collider2D
- Rigidbody2D (Kinematic)
```

#### Bomb Prefab
```
Components:
- FloatingItem2D
  - Floating Item Type: Bomb
  - Pickup Prefab: [留空]
  - Effect Data: [FloatingItemEffectData]
- SpriteRenderer (红色炸弹)
- Collider2D
- Rigidbody2D (Kinematic)
```

#### Web Prefab
```
Components:
- FloatingItem2D
  - Floating Item Type: Web
  - Pickup Prefab: [留空]
  - Effect Data: [FloatingItemEffectData]
- SpriteRenderer (白色蛛网)
- Collider2D
- Rigidbody2D (Kinematic)
```

### 生成器配置

在 FloatingItemSpawner2D 中配置生成权重：

```csharp
Entries:
- Prefab: FuelFloating,     Weight: 3.0
- Prefab: ShieldFloating,   Weight: 2.0
- Prefab: TrashFloating,    Weight: 2.5
- Prefab: BombFloating,     Weight: 1.5
- Prefab: WebFloating,      Weight: 1.0
```

## 工作流程

### 1. 创建效果配置
```
Unity Editor:
1. 右键 Assets 文件夹
2. Create > Game > Floating Item Effect Data
3. 命名为 FloatingItemEffectData
4. 配置：
   - Bomb Damage: 20
   - Web Disable Duration: 5
```

### 2. 创建漂浮物 Prefab
```
为每种类型创建 Prefab：
1. 创建 GameObject
2. 添加 FloatingItem2D
3. 配置 FloatingItemType
4. 引用 FloatingItemEffectData
5. 对于 Fuel/Shield/Trash，引用对应的 PickupPrefab
6. 添加视觉组件（Sprite、动画）
7. 保存为 Prefab
```

### 3. 配置生成器
```
在场景中的 FloatingItemSpawner2D：
1. 添加所有漂浮物 Prefab 到 Entries
2. 配置生成权重
3. 设置生成区域（SpawnZones）
4. 调整生成参数（间隔、最大数量等）
```

### 4. 实现接收系统
```
需要实现的组件：
1. SubmarineHealth（监听 SubmarineHealthChangedEvent）
2. AnchorLauncher2D.DisableForDuration()
3. UI 反馈（爆炸效果、禁用提示）
```

## API 参考

### FloatingItem2D

**公共属性**：
```csharp
public FloatingItemType FloatingType { get; }
public bool CanBeCaughtByAnchor { get; }
```

**公共方法**：
```csharp
public bool TryStartAnchorPull(AnchorItemDropPoint2D dropPoint, Transform anchor)
public void Configure(FloatingItemType newFloatingType, CarryableItem2D newPickupPrefab, Vector2 newDriftVelocity)
public void SetDriftVelocity(Vector2 newDriftVelocity)
```

### FloatingItemEffectData

**公共字段**：
```csharp
public float bombDamage;           // 炸弹伤害值
public float webDisableDuration;   // 蛛网禁用时长
```

## 调试与测试

### 测试清单

- [ ] 燃料勾回后生成燃料拾取物
- [ ] 护盾勾回后生成护盾拾取物
- [ ] 垃圾勾回后生成垃圾拾取物
- [ ] 炸弹勾回后船体扣血
- [ ] 蛛网勾回后锚点禁用
- [ ] 禁用时长可配置且准确
- [ ] 炸弹伤害值可配置且准确
- [ ] 拾取物物理效果正常（掉落、碰撞）
- [ ] 事件正确发送和接收
- [ ] UI 反馈正常显示

### 常见问题

**Q: 炸弹没有造成伤害？**
A: 检查：
1. FloatingItemEffectData 是否配置
2. SubmarineHealth 是否监听 SubmarineHealthChangedEvent
3. bombDamage 值是否大于 0

**Q: 蛛网没有禁用锚点？**
A: 检查：
1. FloatingItemEffectData 是否配置
2. AnchorLauncher2D 是否监听 AnchorDisabledEvent
3. webDisableDuration 值是否大于 0
4. AnchorLauncher2D 是否实现了 DisableForDuration 方法

**Q: 拾取物没有生成？**
A: 检查：
1. 对应漂浮物 Prefab 的 pickupPrefab 字段是否配置
2. pickupPrefab 是否为有效的 CarryableItem2D
3. SubmarineInteriorFollower2D 是否在场景中

## 性能考虑

- FloatingItem2D 数量受 FloatingItemSettings.maxAliveItems 限制
- 事件系统使用 struct 避免 GC 分配
- 生成拾取物时使用对象池可优化性能（未来优化）

## 扩展指南

### 添加新的漂浮物类型

1. 在 FloatingItemType 枚举中添加新类型
2. 在 FloatingItem2D.SpawnPickupAndDestroy() 中添加 case 分支
3. 如果需要新效果，在 FloatingItemEffectData 中添加配置字段
4. 实现对应的效果方法（如 ApplyNewEffect()）
5. 创建对应的 Prefab
6. 更新文档

### 添加新的拾取物类型

1. 在 CarryableItemType 枚举中添加新类型
2. 创建对应的拾取物 Prefab
3. 在漂浮物 Prefab 中引用新的拾取物 Prefab
4. 更新交互站系统以处理新类型

## 相关文件

```
Assets/Scripts/Gameplay/FloatingItems/
├── FloatingItemType.cs              # 漂浮物类型枚举
├── FloatingItem2D.cs                # 漂浮物主组件
├── FloatingItemEffectData.cs        # 效果配置 SO
├── FloatingItemSpawner2D.cs         # 生成器
└── FloatingItemSpawnZone2D.cs       # 生成区域

Assets/Scripts/Gameplay/Pickups/
├── CarryableItemType.cs             # 拾取物类型枚举
├── CarryableItem2D.cs               # 拾取物组件
└── PlayerCarryInteractor2D.cs       # 玩家携带系统

Assets/Scripts/Gameplay/Events/
├── SubmarineHealthChangedEvent.cs   # 船体伤害事件
└── AnchorDisabledEvent.cs           # 锚点禁用事件
```

## 相关文档

- [Design/FloatingItemTypes.md](../Design/FloatingItemTypes.md)：玩法设计文档
- [Technical/FloatingItems.md](FloatingItems.md)：漂浮物生成系统
- [Technical/CarryableItemSystem.md](CarryableItemSystem.md)：拾取物系统
