# 可交互点系统文档

## 概述

可交互点系统允许在船上放置各种可交互的站点（火炉、防御站、修理台等），玩家靠近时会显示描边高亮，按 E 键进行交互。

## 系统架构

### 核心组件

1. **InteractionType.cs** - 交互类型枚举
   - Stove（火炉）
   - DefenseStation（防御站）
   - RepairStation（修理台）
   - StorageChest（储物箱）
   - Workbench（工作台）
   - Helm（舵）
   - AnchorLauncher（锚发射器）

2. **InteractionRequirement.cs** - 交互需求配置
   - 需要的物品类型
   - 是否消耗物品
   - 交互持续时间
   - 是否需要 QTE
   - 音效配置

3. **InteractableStation2D.cs** - 核心交互组件
   - 碰撞检测
   - 描边显示控制（DOTween 缓动）
   - 交互验证
   - UnityEvent 事件系统

4. **InteractionEffectHandler.cs** - 效果处理器
   - 根据交互类型执行不同逻辑
   - 可扩展的效果系统

5. **PlayerInteractionInput.cs** - 玩家输入组件
   - 检测附近可交互点
   - 响应交互按键（默认 E）
   - 显示 UI 提示

### 视觉反馈

6. **SpriteOutline.shader** - 描边 Shader
   - 采样周围 8 个像素
   - 在透明边缘绘制描边
   - 支持动态宽度调整
   - 支持自定义颜色

## 使用方法

### 1. 创建可交互点预制体

#### 在 Unity Editor 中手动创建：

1. 创建空 GameObject，命名为 "InteractableStation_Stove"
2. 添加组件：
   - `SpriteRenderer` - 显示站点图标
   - `CircleCollider2D` - 设置为 Trigger，半径约 1.0~1.5
   - `InteractableStation2D` - 核心交互组件
   - `InteractionEffectHandler` - 效果处理器

3. 配置 InteractableStation2D：
   ```
   Station Type: Stove
   Interaction Trigger: 拖入 CircleCollider2D
   Outline Material: 拖入 OutlineMaterial
   Outline Fade In Duration: 0.3
   Outline Fade Out Duration: 0.2
   Outline Color: Yellow (1, 1, 0, 1)
   Outline Width: 0.02
   ```

4. 配置 Requirement：
   ```
   Required Item Type: RawFood (如果需要食材)
   Consume Item: true
   Effect Description: "烹饪食物"
   Duration: 2.0 (烹饪时间)
   Requires QTE: false
   ```

5. 配置 Layer：
   - 将 GameObject 的 Layer 设置为 "Interactable"（需要先创建这个 Layer）

6. 保存为 Prefab

### 2. 创建描边材质

1. 在 `Assets/Materials/` 创建新材质
2. 命名为 `OutlineMaterial`
3. Shader 选择 `Custom/SpriteOutline`
4. 配置参数：
   - Outline Color: (1, 1, 0, 1) - 黄色
   - Outline Width: 0.02

### 3. 配置玩家

在玩家 GameObject 上添加 `PlayerInteractionInput` 组件：

```
Interact Key: E
Detection Radius: 1.5
Interactable Layer: Interactable
Interaction Prompt UI: 拖入提示 UI（可选）
```

### 4. 不同类型的配置示例

#### 火炉（Stove）
```
Station Type: Stove
Required Item Type: RawFood
Consume Item: true
Duration: 2.0
Effect: 生成 CookedFood
```

#### 防御站（DefenseStation）
```
Station Type: DefenseStation
Required Item Type: Unknown (不需要物品)
Consume Item: false
Duration: 0
Effect: 发射弹药，造成伤害
```

#### 修理台（RepairStation）
```
Station Type: RepairStation
Required Item Type: RepairKit
Consume Item: true
Duration: 3.0
Effect: 恢复船体生命值
```

#### 储物箱（StorageChest）
```
Station Type: StorageChest
Required Item Type: Unknown
Consume Item: false
Duration: 0
Effect: 打开储物界面
```

## 事件系统

每个 `InteractableStation2D` 提供以下 UnityEvents：

- `onPlayerEnter` - 玩家进入范围
- `onPlayerExit` - 玩家离开范围
- `onInteractionStart` - 交互开始
- `onInteractionSuccess` - 交互成功
- `onInteractionFail` - 交互失败

### 使用示例

在 Inspector 中配置事件：
1. 展开 `On Interaction Success` 事件
2. 点击 `+` 添加监听器
3. 拖入目标对象
4. 选择要调用的方法

或在代码中订阅：
```csharp
InteractableStation2D station = GetComponent<InteractableStation2D>();
station.onInteractionSuccess.AddListener((player) =>
{
    Debug.Log("交互成功！");
});
```

## 扩展功能

### 添加新的交互类型

1. 在 `InteractionType.cs` 枚举中添加新类型
2. 在 `InteractionEffectHandler.cs` 的 `HandleInteractionSuccess` 方法中添加 case
3. 实现对应的效果方法

### 添加 QTE 系统

在 `InteractableStation2D.cs` 的 `StartQTE` 方法中实现：
```csharp
private void StartQTE(PlayerController player)
{
    // 显示 QTE UI
    // 开始倒计时
    // 监听按键输入
    // 成功/失败后调用 CompleteInteraction
}
```

### 自定义描边效果

修改 `SpriteOutline.shader`：
- 调整采样点数量和偏移
- 添加动画效果（如呼吸、闪烁）
- 支持多层描边

## 性能优化

1. **对象池**：频繁生成/销毁的物品使用对象池
2. **LOD**：远距离禁用描边效果
3. **批处理**：使用相同材质的站点可以批处理
4. **事件驱动**：避免每帧 Update 检测，使用 Trigger 事件

## 调试

### Gizmos 可视化

- **绿色线框球**：InteractableStation2D 的交互范围
- **青色线框球**：PlayerInteractionInput 的检测范围

在 Scene 视图中选中对象即可看到 Gizmos。

### Console 日志

启用调试日志可以看到：
- 玩家进入/离开范围
- 交互验证结果
- 效果执行信息

## 已知限制

1. 描边 Shader 目前只支持单个 SpriteRenderer
2. QTE 系统需要额外实现
3. 音效系统需要 AudioSource 组件

## 相关文件

```
Assets/Scripts/Gameplay/Interactions/
  ├── InteractionType.cs
  ├── InteractionRequirement.cs
  ├── InteractableStation2D.cs
  ├── InteractionEffectHandler.cs
  └── (待扩展)

Assets/Scripts/Player/
  └── PlayerInteractionInput.cs

Assets/Shaders/
  └── SpriteOutline.shader

Assets/Materials/
  └── OutlineMaterial.mat

Assets/Prefabs/Interactions/
  ├── InteractableStation_Stove.prefab
  ├── InteractableStation_Defense.prefab
  ├── InteractableStation_Repair.prefab
  └── (其他类型)
```

## 下一步计划

1. 实现完整的 QTE 系统
2. 添加音效支持
3. 实现具体的游戏逻辑（烹饪、修理、防御等）
4. 创建 UI 提示系统
5. 添加教程和提示文本
