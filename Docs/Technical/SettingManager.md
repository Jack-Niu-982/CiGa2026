# SettingManager 配置系统文档

## 概述

SettingManager 是一个全局配置管理系统，将分散在各 MonoBehaviour 中的配置参数集中到 ScriptableObject 中管理。

## 架构设计

- **SettingManager**: 静态类，提供懒加载访问各配置 SO
- **各 Settings SO**: 继承 ScriptableObject，每个负责一类配置
- **资源位置**: `Assets/Resources/Settings/` - 运行时通过 `Resources.Load` 加载
- **兼容性**: 使用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 重置静态引用，兼容 Domain Reload 关闭模式

## 文件结构

```
Assets/Scripts/Settings/
  ├── SettingManager.cs              # 静态入口
  ├── PlayerSettings.cs              # 玩家移动/跳跃/攀爬
  ├── FloatingItemSettings.cs        # 漂浮物生成参数
  ├── AnchorSettings.cs              # 锚钩参数
  ├── GamepadSettings.cs             # 手柄输入/震动
  ├── StationSettings.cs             # 工作站类型、需求、描边和效果数值
  └── DevSettings.cs                 # 调试开关

Assets/Resources/Settings/           # SO 资产实例
  ├── PlayerSettings.asset
  ├── FloatingItemSettings.asset
  ├── AnchorSettings.asset
  ├── GamepadSettings.asset
  ├── StationSettings.asset
  ├── FuelStationSettings.asset
  ├── RepairStationSettings.asset
  └── DevSettings.asset
```

## 使用方法

### 1. 访问配置

```csharp
// 在任何脚本中直接访问
float speed = SettingManager.Player.moveSpeed;
float spawnInterval = SettingManager.FloatingItem.spawnInterval;
bool showDebug = SettingManager.Dev.showAnchorDebugLog;
```

### 2. 空值检查（可选）

```csharp
PlayerSettings settings = SettingManager.Player;
if (settings == null)
{
    // SO 资产不存在，使用 fallback 或报错
    return;
}

velocity.x = horizontalInput * settings.moveSpeed;
```

### 3. 创建 SO 资产

**方法一：Unity Editor 菜单**
1. 在 Unity Editor 中右键 `Assets/Resources/Settings/` 文件夹
2. 选择 `Create > Settings > [配置名称]`
3. 重命名资产为对应名称（如 `PlayerSettings`）

**方法二：通过代码创建（Editor Only）**
```csharp
// 仅在 Editor 中可用
using UnityEditor;

[MenuItem("Tools/Create Default Settings")]
static void CreateDefaultSettings()
{
    PlayerSettings player = ScriptableObject.CreateInstance<PlayerSettings>();
    AssetDatabase.CreateAsset(player, "Assets/Resources/Settings/PlayerSettings.asset");
    
    FloatingItemSettings floatingItem = ScriptableObject.CreateInstance<FloatingItemSettings>();
    AssetDatabase.CreateAsset(floatingItem, "Assets/Resources/Settings/FloatingItemSettings.asset");
    
    // ... 其他配置
    
    AssetDatabase.SaveAssets();
}
```

## 配置 SO 详解

### PlayerSettings

玩家移动、跳跃、攀爬配置。

**字段列表：**
- `moveSpeed` (float, 默认 6): 水平移动速度
- `jumpForce` (float, 默认 12): 跳跃力度
- `groundCheckRadius` (float, 默认 0.08): 地面检测半径
- `climbSpeed` (float, 默认 4): 攀爬速度
- `snapToLadderCenter` (bool, 默认 true): 吸附到梯子中心
- `ladderSnapSpeed` (float, 默认 8): 吸附速度
- `ladderCheckSize` (Vector2, 默认 (0.8, 1.4)): 梯子检测区域大小
- `ladderEndPadding` (float, 默认 0.02): 梯子端点缓冲
- `ladderExitHoldTime` (float, 默认 0.2): 退出梯子需按住时间
- `ladderExitInputThreshold` (float, 默认 0.5): 退出输入阈值
- `ladderLandingCheckSize` (Vector2, 默认 (0.45, 0.12)): 着陆检测大小
- `disableBodyColliderWhileClimbing` (bool, 默认 true): 攀爬时禁用身体碰撞
- `freezeRotation` (bool, 默认 true): 冻结旋转

**使用示例：**
```csharp
// PlayerController.State.cs
velocity.x = horizontalInput * SettingManager.Player.moveSpeed;
velocity.y = SettingManager.Player.jumpForce;
```

### FloatingItemSettings

漂浮物生成配置。

**字段列表：**
- `minSpawnInterval` (float, 默认 3): 最小生成间隔（秒）
- `maxSpawnInterval` (float, 默认 5): 最大生成间隔（秒）
- `maxAliveItems` (int, 默认 8): 最多同时存在数量
- `spawnOnStart` (bool, 默认 true): 启动时立即生成
- `spawnScaleDuration` (float, 默认 0.5): 生成时缩放动画时长（秒）
- `spawnStartScale` (float, 默认 0.3): 生成时初始缩放比例（0-1）
- `spawnFadeDuration` (float, 默认 0.4): 生成时透明度淡入时长（秒）
- `spawnEase` (Ease, 默认 OutBack): 生成动画缓动曲线
- `boatExclusionRadius` (float, 默认 3): 船体周围不生成漂浮物的最小半径
- `minItemDistance` (float, 默认 2): 与其他漂浮物的最小间距
- `maxSpawnAttempts` (int, 默认 10): 生成位置尝试次数
- `velocityBiasStrength` (float, 默认 0.5): 速度偏置强度
- `referenceSpeed` (float, 默认 2): 参考速度
- `deadZoneHalfWidth` (float, 默认 1.5): 中心死区宽度
- `minSideWeight` (float, 默认 0.1): 最小侧边权重
- `defaultDriftVelocity` (Vector2, 默认 (-0.35, 0)): 默认漂移速度
- `defaultAngularSpeed` (float, 默认 20): 默认角速度
- `minLifetime` (float, 默认 8): 最小生命周期（秒）
- `maxLifetime` (float, 默认 12): 最大生命周期（秒）
- `blinkStartTime` (float, 默认 3): 进入闪烁阶段前的剩余时间（秒）
- `blinkCount` (int, 默认 3): 闪烁次数
- `blinkDuration` (float, 默认 0.8): 每次闪烁持续时间（秒）
- `minAlpha` (float, 默认 0.2): 透明度最小值（0-1）
- `maxAlpha` (float, 默认 1): 透明度最大值（0-1）
- `anchorPullSpeed` (float, 默认 5): 锚拉回速度
- `arrivalDistance` (float, 默认 0.12): 到达判定距离

**使用示例：**
```csharp
// FloatingItemSpawner2D.cs - 随机生成间隔
nextSpawnInterval = Random.Range(
    SettingManager.FloatingItem.minSpawnInterval,
    SettingManager.FloatingItem.maxSpawnInterval
);

// FloatingItemSpawner2D.cs - 位置检测
if (IsPositionValid(spawnPosition, settings))
{
    // 检测船体排除区域和其他漂浮物间距
}

// FloatingItem2D.cs - 随机生命周期
lifetime = Random.Range(
    SettingManager.FloatingItem.minLifetime,
    SettingManager.FloatingItem.maxLifetime
);

// FloatingItem2D.cs - 生成动效（DOTween）
transform.DOScale(Vector3.one, settings.spawnScaleDuration)
    .SetEase(settings.spawnEase);
```

**特性说明：**

1. **随机化**：
   - 生成间隔：每次生成后随机选择下一次间隔（minSpawnInterval ~ maxSpawnInterval）
   - 生命周期：每个漂浮物生成时随机 lifetime（minLifetime ~ maxLifetime）

2. **生成动效（DOTween）**：
   - 缩放：从 spawnStartScale 缩放到 1.0，使用 spawnEase 曲线
   - 淡入：透明度从 0 淡入到 1.0
   - 平滑自然的出场效果

3. **位置检测**：
   - 船体排除：距离船体 < boatExclusionRadius 的位置不生成
   - 间距保证：距离任意现有漂浮物 < minItemDistance 的位置不生成
   - 重试机制：最多尝试 maxSpawnAttempts 次寻找合法位置

4. **闪烁效果（DOTween）**：
   - 剩余时间 ≤ blinkStartTime 时开始闪烁
   - 闪烁 blinkCount 次，最后一次停在 minAlpha 透明度后销毁
   - 使用 DOTween Sequence 精确控制动画时序

5. **Gizmos 可视化**（编辑器）：
   - 红色半透明球体：船体排除区域（boatExclusionRadius）
   - 青色线框球体：每个漂浮物的最小间距（minItemDistance）

### AnchorSettings

锚钩发射、绳索、旋转配置。

**字段列表：**
- `maxRopeLength` (float, 默认 15): 最大绳索长度
- `anchorOffset` (float, 默认 0.5): 锚钩发射偏移
- `anchorShootSpeed` (float, 默认 25): 发射速度
- `anchorRetractSpeed` (float, 默认 18): 收回速度
- `wallHitPullDelay` (float, 默认 0.5): 击中墙壁拉动延迟
- `wallPullImpulse` (float, 默认 6): 墙壁拉动冲量
- `inverseWidthByLength` (bool, 默认 true): 绳索宽度随长度反比
- `ropeWidthAtReferenceLength` (float, 默认 0.07): 参考长度下的宽度
- `widthReferenceLength` (float, 默认 5): 宽度参考长度
- `minimumRopeWidth` (float, 默认 0.025): 最小绳索宽度
- `maximumRopeWidth` (float, 默认 0.13): 最大绳索宽度
- `ropeColor` (Color, 默认 白色): 绳索颜色
- `rotationSpeed` (float, 默认 90): 旋转速度
- `maxRotationAngle` (float, 默认 45): 最大旋转角度
- `rotationInputDeadZone` (float, 默认 0.1): 输入死区
- `invertRotationControl` (bool, 默认 false): 反转控制
- `enableWallParticleFeedback` (bool, 默认 true): 启用墙壁粒子
- `wallHitParticleCount` (int, 默认 18): 击中粒子数量
- `wallHitParticleSize` (float, 默认 0.18): 击中粒子大小
- `wallHitParticleColor` (Color): 击中粒子颜色
- `wallRetractParticleCount` (int, 默认 12): 收回粒子数量
- `wallRetractParticleSize` (float, 默认 0.14): 收回粒子大小
- `wallRetractParticleColor` (Color): 收回粒子颜色
- `wallParticleLifetime` (float, 默认 0.42): 粒子生命周期
- `wallParticleOutwardSpeed` (float, 默认 1.8): 粒子向外速度

### GamepadSettings

手柄输入与震动反馈配置。

**字段列表：**
- `stickDeadZone` (float, 默认 0.2): 摇杆死区
- `verticalInputThreshold` (float, 默认 0.6): 垂直输入阈值
- `reelStickMinimumMagnitude` (float, 默认 0.65): 卷线摇杆最小幅度
- `minimumReelAngularSpeed` (float, 默认 45): 最小卷线角速度
- `fullReelAngularSpeed` (float, 默认 360): 全速卷线角速度
- `minimumReelAmount` (float, 默认 0.25): 最小卷线量
- `reelInputGraceTime` (float, 默认 0.08): 输入宽容时间
- `maximumAcceptedAngleDelta` (float, 默认 120): 最大角度偏差
- `reelAmountRiseSpeed` (float, 默认 10): 卷线量上升速度
- `reelAmountFallSpeed` (float, 默认 14): 卷线量下降速度
- `enableAnchorRumble` (bool, 默认 true): 启用震动
- `rumbleStepDegrees` (float, 默认 45): 震动步进角度
- `reelTickDuration` (float, 默认 0.045): 节拍持续时间
- `reelTickLowFrequency` (float, 默认 0.42): 节拍低频
- `reelTickHighFrequency` (float, 默认 0.22): 节拍高频
- `continuousReelLowFrequency` (float, 默认 0.18): 连续低频
- `continuousReelHighFrequency` (float, 默认 0.07): 连续高频
- `shootRumbleDuration` (float, 默认 0.12): 发射震动时长
- `shootLowFrequency` (float, 默认 0.25): 发射低频
- `shootHighFrequency` (float, 默认 0.65): 发射高频
- `retractRumbleDuration` (float, 默认 0.16): 收回震动时长
- `retractLowFrequency` (float, 默认 0.55): 收回低频
- `retractHighFrequency` (float, 默认 0.18): 收回高频

### StationSettings

工作站系统的集中配置。所有工作站的站点类型、交互需求、描边高亮参数和通用效果数值都应放在这里，不应散落在 `InteractableStation2D` 或 `InteractionEffectHandler` 组件上。

**字段列表：**
- `stationType`：工作站类型，如 `FuelStation`、`RepairStation`。
- `requirement`：交互需求，包括需要的携带物、是否消耗、交互时长和 QTE 配置。
- `outlineMaterial`：描边材质，当前使用 `InteractableOutline.mat`。
- `outlineFadeInDuration` / `outlineFadeOutDuration`：描边淡入淡出时间。
- `outlineColor`：描边颜色。
- `outlineWidth`：描边宽度。
- `defenseDamage` / `defenseCooldown`：防御站效果数值。
- `fuelAmount`：燃料站补充量。
- `repairAmount`：修复站恢复量。
- `gizmoRadius`：编辑器中显示交互范围的辅助半径。

**组件边界：**
- `InteractableStation2D` 只保留 `stationSettings`、`interactionTrigger` 和 UnityEvent。
- `InteractionEffectHandler` 只保留粒子、生成物 prefab 和事件引用。
- 新增或调整数值时，优先改 SO，不要在 prefab 组件上新增可配置数值。

### DevSettings

开发调试配置。

**字段列表：**
- `showAnchorDebugLog` (bool, 默认 true): 显示锚钩日志
- `showInputBindingLog` (bool, 默认 true): 显示输入绑定日志
- `showRotationDebugLog` (bool, 默认 true): 显示旋转日志
- `drawDebugRay` (bool, 默认 true): 绘制调试射线
- `debugRayDuration` (float, 默认 0.15): 射线持续时间
- `showGroundCheckGizmos` (bool, 默认 true): 显示地面检测 Gizmos
- `showLadderCheckGizmos` (bool, 默认 true): 显示梯子检测 Gizmos
- `showSpawnZoneGizmos` (bool, 默认 true): 显示生成区域 Gizmos

## 已迁移的组件

### FloatingItemSpawner2D

**迁移字段：**
- ~~`spawnInterval`~~ → `SettingManager.FloatingItem.spawnInterval`
- ~~`maxAliveItems`~~ → `SettingManager.FloatingItem.maxAliveItems`
- ~~`spawnOnStart`~~ → `SettingManager.FloatingItem.spawnOnStart`
- ~~`velocityBiasStrength`~~ → `SettingManager.FloatingItem.velocityBiasStrength`
- ~~`referenceSpeed`~~ → `SettingManager.FloatingItem.referenceSpeed`
- ~~`deadZoneHalfWidth`~~ → `SettingManager.FloatingItem.deadZoneHalfWidth`
- ~~`minSideWeight`~~ → `SettingManager.FloatingItem.minSideWeight`

**保留字段（场景/Prefab 特定）：**
- `spawnZones[]` - 生成区域引用
- `entries[]` - 加权 Prefab 列表
- `activeItemsRoot` - 父物体引用
- `boatRigidbody` - 船体引用

### PlayerController

**迁移字段：**
- ~~`moveSpeed`~~ → `SettingManager.Player.moveSpeed`
- ~~`jumpForce`~~ → `SettingManager.Player.jumpForce`
- ~~`groundCheckRadius`~~ → `SettingManager.Player.groundCheckRadius`
- ~~`climbSpeed`~~ → `SettingManager.Player.climbSpeed`
- ~~`snapToLadderCenter`~~ → `SettingManager.Player.snapToLadderCenter`
- ~~`ladderSnapSpeed`~~ → `SettingManager.Player.ladderSnapSpeed`
- ~~`ladderCheckSize`~~ → `SettingManager.Player.ladderCheckSize`
- ~~`ladderEndPadding`~~ → `SettingManager.Player.ladderEndPadding`
- ~~`ladderExitHoldTime`~~ → `SettingManager.Player.ladderExitHoldTime`
- ~~`ladderExitInputThreshold`~~ → `SettingManager.Player.ladderExitInputThreshold`
- ~~`ladderLandingCheckSize`~~ → `SettingManager.Player.ladderLandingCheckSize`
- ~~`disableBodyColliderWhileClimbing`~~ → `SettingManager.Player.disableBodyColliderWhileClimbing`
- ~~`freezeRotation`~~ → `SettingManager.Player.freezeRotation`

**保留字段（场景/Prefab 特定）：**
- `playerInput` - 输入引用
- `rb`, `bodyCollider`, `animator` - 组件引用
- `groundCheck`, `ladderCheck`, `ladderLandingCheck` - Transform 引用
- `groundLayer`, `ladderLayer`, `submarineInteriorLayer` - LayerMask
- `operateInteractor` - 交互引用
- `moveXParameter` 等 Animator 参数名

## 下一步计划

### 待迁移组件

1. **AnchorLauncher2D** → AnchorSettings
   - 30+ 配置字段（发射、绳索、粒子）
   
2. **GamepadPlayerInput** → GamepadSettings
   - 震动反馈参数（已定义 SO）
   
3. **InteractableStation2D / InteractionEffectHandler** → StationSettings
   - 站点类型、交互需求、描边高亮、修复量、燃料量、防御数值

### 扩展建议

1. **运行时配置切换**
   ```csharp
   // 未来可实现难度分级
   public static void LoadDifficulty(string difficulty)
   {
       string path = $"Settings/{difficulty}/PlayerSettings";
       _player = Resources.Load<PlayerSettings>(path);
   }
   ```

2. **编辑器工具**
   - 创建 Settings Inspector 面板
   - 一键生成默认 SO 资产
   - 配置对比工具

3. **热重载支持**
   ```csharp
   #if UNITY_EDITOR
   // 编辑器中修改 SO 立即生效，无需重新加载
   #endif
   ```

## 常见问题

**Q: SO 资产不存在时会怎样？**  
A: `SettingManager.Player` 等会返回 `null`，代码中已添加空值检查。建议在启动时验证所有 SO 是否存在。

**Q: 能否在运行时修改 SO 的值？**  
A: 可以，但修改不会持久化。如需保存，需要实现存档系统。

**Q: 如何在 Prefab 中覆盖 SO 的值？**  
A: 保留 SerializeField 作为 override，代码中优先读取 SerializeField，为空时 fallback 到 SO。

**Q: 为什么不用 Addressables？**  
A: Resources.Load 足够简单，且项目未使用 Addressables。未来可迁移。

## 相关文件

- `Assets/Scripts/Settings/SettingManager.cs` - 静态入口
- `Assets/Scripts/Settings/*.cs` - 各配置 SO 类
- `Assets/Resources/Settings/*.asset` - SO 资产实例
- `Docs/Technical/CarryableItemSystem.md` - 拾取物系统文档
- `Docs/Technical/FloatingItems.md` - 漂浮物系统文档
