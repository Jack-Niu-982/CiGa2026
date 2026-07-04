# PickupSpawner 配置说明

## 问题已修复
- ✅ 移除了对 `SubmarineInteriorFollower2D` 的依赖
- ✅ 简化为直接在世界坐标中生成物品

## 在 Unity 中配置步骤

### 1. 创建 PickupSpawner GameObject
1. 在 Hierarchy 中右键 → `Create Empty`
2. 命名为 `PickupSpawner`

### 2. 添加 PickupSpawner 组件
1. 选中 `PickupSpawner` GameObject
2. 在 Inspector 中点击 `Add Component`
3. 搜索并添加 `PickupSpawner`

### 3. 配置 Prefab 引用
在 Inspector 中配置以下字段：

#### 拾取物 Prefabs
- **Fuel Pickup Prefab**: 拖入 `Assets/Prefabs/Gameplay/Pickups/FuelPickup.prefab`
- **Shield Pickup Prefab**: 拖入 `Assets/Prefabs/Gameplay/Pickups/ShieldPickup.prefab`
- **Trash Pickup Prefab**: 拖入 `Assets/Prefabs/Gameplay/Pickups/TrashPickup.prefab`

#### 生成区域（可选调整）
- **Spawn Parent**: 留空（物品生成在根级）或拖入一个 Transform 作为父级
- **Spawn Center**: `(0, 0, 0)` - 生成区域的世界坐标中心
- **Spawn Size**: `(8, 3, 0)` - 生成区域的大小

#### 生成设置（可选调整）
- **Min Spawn Height**: `0.5` - 最小生成高度
- **Max Spawn Height**: `2.5` - 最大生成高度
- **Drop Height Offset**: `0.3` - 初始下落高度偏移

#### 调试
- **Show Debug Gizmos**: ✅ 勾选（Scene 视图中会显示生成区域）

### 4. 测试
1. 进入 Play 模式
2. 点击 DebugPanel 上的 "生成随机" 或 "生成全部" 按钮
3. 物品会在配置的区域内随机生成并自然下落

## 可视化调试
- 在 Scene 视图中选中 `PickupSpawner` GameObject
- 会看到绿色半透明立方体（生成区域）
- 黄色线表示最小生成高度
- 红色线表示最大生成高度

## 注意事项
- 确保生成区域不要太靠近地面，否则物品会立即掉落到地板
- 如果需要物品生成在特定父级下（如船体内部），设置 `Spawn Parent` 字段
- 生成的物品启用了重力和物理模拟，会自然下落
