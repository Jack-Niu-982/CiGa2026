# 工作站系统文档

## 配置规范

工作站配置必须集中到 `StationSettings` ScriptableObject 中，资源放在 `Assets/Resources/Settings/`。组件上只保留场景引用和事件，不再直接填写站点类型、交互需求、描边颜色、描边宽度、燃料量、修复量、防御伤害等数值。

当前已有配置：

- `StationSettings.asset`：默认兜底配置。
- `FuelStationSettings.asset`：燃料站配置，类型为 `FuelStation`，需求物品为 `Fuel`。
- `RepairStationSettings.asset`：修复站配置，类型为 `RepairStation`，需求物品为 `Shield`。

新增工作站时，先创建一份新的 `StationSettings`，再让 prefab 上的 `InteractableStation2D.stationSettings` 指向它。`InteractableStation2D` 负责触发、校验、描边显示和事件派发；`InteractionEffectHandler` 负责执行效果，但效果数值仍然从同一份 `StationSettings` 读取。`Collider2D`、粒子、音效播放源、UnityEvent 这类场景或 prefab 绑定项可以留在组件上。

## 概述

工作站是船内的交互设施，玩家可以在这些设施上投入物品或执行特定操作。当前实现了**燃料站**和**修复站**两个关键工作站。

## 已实现工作站

### 1. 燃料站 (Fuel Receiver)

**组件**：`SubmarineFuelReceiver2D`

**功能**：接收玩家携带的燃料拾取物，补充飞船燃料。

**使用流程**：
1. 玩家拾取燃料（CarryableItemType.Fuel）
2. 靠近燃料站
3. 按交互键投入燃料
4. 飞船燃料增加

**配置参数**：
```csharp
[Header("燃料恢复")]
public float fuelRestoreAmount = 20f;  // 每个燃料恢复量

[Header("视觉反馈")]
public ParticleSystem fuelReceiveParticles;  // 接收粒子效果
public AudioClip fuelReceiveSound;           // 接收音效
```

**实现接口**：`ICarryItemReceiver`
- `CanReceiveCarryItem()` - 只接受 Fuel 类型
- `TryReceiveCarryItem()` - 消耗物品并恢复燃料

**挂载位置**：
- 船内的燃料装置 GameObject
- 需要配置 Collider2D 作为交互触发器

### 2. 修复站 (Repair Station)

**组件**：`InteractionEffectHandler` + `InteractableStation2D`

**功能**：接收玩家携带的护盾/修理包，修复飞船护盾值。

**使用流程**：
1. 玩家拾取护盾（CarryableItemType.Shield）
2. 靠近修复站
3. 按交互键投入护盾
4. 飞船护盾值恢复

**配置参数**：
```csharp
[Header("修理效果")]
public float repairAmount = 20f;            // 修理恢复量
public ParticleSystem repairParticles;      // 修理粒子效果
```

**实现方式**：
```csharp
private void ExecuteRepairEffect(PlayerController player)
{
    // 通过事件总线请求修理
    GameplayEventBus.RequestSubmarineRepair(repairAmount);
}
```

**挂载位置**：
- 船内的修理台 GameObject
- InteractableStation2D.stationType = RepairStation
- InteractionRequirement.requiredItemType = Shield
- InteractionRequirement.consumeItem = true

### 3. 护盾站 (Shield Receiver)

**组件**：`SubmarineHealth2D`（已实现 ICarryItemReceiver）

**功能**：直接接收护盾拾取物，恢复飞船生命值。

**配置参数**：
```csharp
[Header("护盾拾取物")]
public float shieldRepairAmount = 25f;  // 护盾恢复量
```

**实现接口**：`ICarryItemReceiver`
- `CanReceiveCarryItem()` - 只接受 Shield 类型
- `TryReceiveCarryItem()` - 消耗物品并恢复生命值

**注意**：
- SubmarineHealth2D 既是生命值管理器，也是护盾接收器
- 设计文档中的"护盾"在实现上作为生命值处理

## 工作站类型系统

### InteractionType 枚举

```csharp
public enum InteractionType
{
    None = 0,
    Stove,              // 火炉 - 烹饪食物
    DefenseStation,     // 防御站 - 射击防御
    RepairStation,      // 修理台 - 修理船体
    StorageChest,       // 储物箱 - 存储物品
    AnchorLauncher,     // 锚发射器
    Workbench,          // 工作台 - 制作物品
    Helm                // 舵 - 控制船只方向
}
```

### CarryableItemType 枚举

```csharp
public enum CarryableItemType
{
    Unknown = 0,
    Fuel = 1,      // 燃料 - 用于燃料站
    Shield = 2,    // 护盾 - 用于修复站/护盾站
    Trash = 3      // 垃圾 - 用于垃圾桶
}
```

## 实现接口

### ICarryItemReceiver

所有接收玩家携带物的工作站都实现此接口：

```csharp
public interface ICarryItemReceiver
{
    /// <summary>
    /// 检查是否可以接收物品。
    /// </summary>
    bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item);

    /// <summary>
    /// 尝试接收并消耗物品。
    /// </summary>
    bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item);
}
```

### 玩家交互流程

```csharp
// PlayerCarryInteractor2D.cs
public bool TryConsumeHeldItem(ICarryItemReceiver receiver)
{
    if (receiver == null || heldItem == null)
        return false;
    
    if (!receiver.CanReceiveCarryItem(this, heldItem))
        return false;
    
    return receiver.TryReceiveCarryItem(this, heldItem);
}
```

## 事件系统集成

工作站通过 `GameplayEventBus` 与其他系统通信：

### 修复请求

```csharp
// 修理台发送
GameplayEventBus.RequestSubmarineRepair(repairAmount);

// SubmarineHealth2D 监听
private void OnEnable()
{
    GameplayEventBus.SubmarineRepairRequested += HandleSubmarineRepairRequested;
}

private void HandleSubmarineRepairRequested(float amount)
{
    Repair(amount);
}
```

### 燃料请求（待实现）

```csharp
// 燃料站发送（未来）
GameplayEventBus.RequestSubmarineFuel(fuelAmount);

// SubmarineFuel2D 监听（未来）
private void OnEnable()
{
    GameplayEventBus.SubmarineFuelRequested += HandleSubmarineFuelRequested;
}
```

## 配置工作站

### 方法一：使用 InteractableStation2D（推荐用于复杂交互）

适用于需要交互动画、持续时间、QTE 的工作站。

**步骤**：
1. 创建 GameObject（如 "RepairStation"）
2. 添加 `InteractableStation2D` 组件
3. 配置参数：
   ```
   Station Type: RepairStation
   Required Item Type: Shield
   Consume Item: true
   Interaction Trigger: [配置 Collider2D]
   ```
4. 添加 `InteractionEffectHandler` 组件
5. 配置效果参数（repairAmount, particles, etc.）

### 方法二：使用 ICarryItemReceiver（推荐用于简单投入）

适用于简单的"投入物品→立即生效"的工作站。

**步骤**：
1. 创建 GameObject（如 "FuelStation"）
2. 添加 `SubmarineFuelReceiver2D` 组件
3. 添加 `Collider2D`（设置为 Trigger）
4. 配置参数（fuelRestoreAmount, particles, sound）

**玩家交互**：
- 玩家靠近时，PlayerCarryInteractor2D 会自动检测 ICarryItemReceiver
- 按交互键时调用 `TryConsumeHeldItem(receiver)`

## 工作站对比表

| 工作站 | 组件 | 接收物品 | 效果 | 实现方式 |
|--------|------|----------|------|----------|
| 燃料站 | SubmarineFuelReceiver2D | Fuel | 补充燃料 | ICarryItemReceiver |
| 修复站 | InteractionEffectHandler | Shield | 修复护盾 | InteractableStation2D |
| 护盾站 | SubmarineHealth2D | Shield | 恢复生命 | ICarryItemReceiver |

## 扩展工作站

### 添加新工作站类型

1. **创建接收器组件**：
```csharp
public class TrashDisposalStation2D : MonoBehaviour, ICarryItemReceiver
{
    public bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        return item != null && item.ItemType == CarryableItemType.Trash;
    }

    public bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        if (!CanReceiveCarryItem(holder, item))
            return false;

        // 处理垃圾逻辑
        Debug.Log("垃圾已处理");
        
        holder.NotifyHeldItemDropped(item);
        Destroy(item.gameObject);
        return true;
    }
}
```

2. **在 InteractionType 添加新类型**（如果需要）
3. **更新 InteractionEffectHandler**（如果使用 InteractableStation2D）

## 调试与测试

### 测试燃料站

```csharp
// 使用 GameplayDebugPanel 生成燃料拾取物
GameplayEventBus.RequestSpawnRandomPickup(CarryableItemType.Fuel);

// 或在场景中放置 FuelPickup Prefab
```

### 测试修复站

```csharp
// 1. 扣血
GameplayEventBus.RequestSubmarineDamage(30f);

// 2. 生成护盾拾取物
GameplayEventBus.RequestSpawnRandomPickup(CarryableItemType.Shield);

// 3. 拾取并投入修复站
```

### 常见问题

**Q: 玩家靠近工作站但无法交互？**
A: 检查：
1. Collider2D 是否设置为 Trigger
2. InteractionTrigger 是否正确引用
3. RequiredItemType 是否与玩家携带物匹配
4. PlayerInteractionInput 的 detectionRadius 是否足够大

**Q: 物品投入后没有效果？**
A: 检查：
1. `TryReceiveCarryItem` 是否返回 true
2. 事件总线是否正确订阅（OnEnable/OnDisable）
3. SubmarineHealth2D 是否在场景中
4. Console 中是否有错误日志

**Q: 修复站和护盾站有什么区别？**
A:
- **修复站**：通过 InteractableStation2D，需要玩家靠近并按E键，可配置交互时长和动画
- **护盾站**：SubmarineHealth2D 直接实现 ICarryItemReceiver，玩家投入即生效

## 性能考虑

- ICarryItemReceiver 检测通过 PlayerCarryInteractor2D 的 Trigger 碰撞实现
- 每个工作站只在玩家靠近时参与交互检测
- 物品消耗立即销毁 GameObject，避免堆积

## 相关文件

```
Assets/Scripts/Gameplay/Mission/
├── SubmarineFuelReceiver2D.cs     # 燃料站
├── SubmarineHealth2D.cs           # 护盾站（兼生命值管理）
└── GameplayEventBus.cs            # 事件总线

Assets/Scripts/Gameplay/Interactions/
├── InteractableStation2D.cs       # 通用交互站组件
├── InteractionEffectHandler.cs    # 效果处理器（修复站等）
├── InteractionRequirement.cs      # 交互需求配置
└── InteractionType.cs             # 工作站类型枚举

Assets/Scripts/Gameplay/Pickups/
├── ICarryItemReceiver.cs          # 接收器接口
├── PlayerCarryInteractor2D.cs     # 玩家携带系统
├── CarryableItem2D.cs             # 拾取物组件
└── CarryableItemType.cs           # 拾取物类型枚举
```

## 相关文档

- [CarryableItemSystem.md](CarryableItemSystem.md)：船内可拾取物系统
- [FloatingItemTypes.md](FloatingItemTypes.md)：漂浮物类型系统
- [CoreGameplay.md](../Design/CoreGameplay.md)：核心玩法设计
