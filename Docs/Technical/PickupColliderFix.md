# 拾取物物理问题 - 最终解决方案

## 问题根源

**拾取物 Prefab 只有一个触发器碰撞体（IsTrigger = true），缺少实体碰撞体。**

Unity 物理规则：触发器碰撞体不会产生物理碰撞，只会触发事件（OnTriggerEnter2D）。

### 当前 Prefab 配置
- FuelPickup.prefab
- ShieldPickup.prefab  
- TrashPickup.prefab

每个 Prefab 只有：
- 1 个 `PolygonCollider2D` (`m_IsTrigger: 1`)
- 1 个 `Rigidbody2D` (Dynamic, GravityScale 0.3, Drag 0.5)

### 导致的现象
- 拾取物受重力影响下落 ✓
- 但触发器碰撞体不会与 Floor 或 SubmarineInterior 层产生物理碰撞 ✗
- 拾取物直接穿透地板和墙壁 ✗
- 掉出船外 ✗

## 正确配置

根据 `Docs/Technical/PhysicsLayerAndCollisionRules.md`：

> 每个 CarryableItem Prefab 包含：
> - 一个 `PolygonCollider2D`（Trigger）：用于玩家拾取检测
> - 一个 `PolygonCollider2D`（实体）：用于物理落地和船舱内部碰撞
> - `Rigidbody2D`：初始为 Kinematic，掉落时切换为 Dynamic

### 需要的配置
每个拾取物 Prefab 应该有：

1. **触发器碰撞体**
   - `PolygonCollider2D`
   - `IsTrigger = true`
   - 用于玩家拾取检测（OnTriggerEnter2D）

2. **实体碰撞体**
   - `PolygonCollider2D`
   - `IsTrigger = false`
   - 用于物理碰撞（与 Floor、SubmarineInterior 碰撞）

3. **Rigidbody2D**
   - Body Type: Dynamic
   - Gravity Scale: 0.3
   - Linear Drag: 0.5

## 修复步骤

### 方法 1：使用编辑器工具（推荐）

1. 在 Unity 中选中三个拾取物 Prefab：
   - `Assets/Prefabs/Gameplay/Pickups/FuelPickup.prefab`
   - `Assets/Prefabs/Gameplay/Pickups/ShieldPickup.prefab`
   - `Assets/Prefabs/Gameplay/Pickups/TrashPickup.prefab`

2. 右键菜单：`Assets → CiGa2026 → Fix Pickup Colliders`

3. 工具会自动为每个 Prefab 添加第二个实体碰撞体

### 方法 2：手动添加

对每个 Prefab：

1. 在 Unity 中打开 Prefab
2. 选中根对象
3. Add Component → Physics 2D → Polygon Collider 2D
4. 确保新添加的碰撞体：
   - Is Trigger: **取消勾选**
   - Points: 与第一个碰撞体相同（自动复制）
5. 保存 Prefab

## 验证

修复后测试：

1. 运行场景
2. 用锚点抓取漂浮物
3. 观察生成的拾取物：
   - ✓ 受重力下落
   - ✓ 停在 Floor 上
   - ✓ 被 SubmarineInterior 层的墙壁阻挡
   - ✓ 不会穿透地板
   - ✓ 不会掉出船外
   - ✓ 玩家仍然可以拾取（触发器检测）

## 相关文件

- `Assets/Scripts/Editor/PickupColliderFix.cs` - 自动修复工具
- `Docs/Technical/PhysicsLayerAndCollisionRules.md` - 物理层规则文档
- `Docs/Technical/PickupPhysicsAnalysis.md` - 父对象问题分析

## 问题回顾

整个问题涉及两个方面：

1. **父对象问题**（已修复）：
   - 拾取物不能作为 Submarine（Dynamic Rigidbody2D）的直接子对象
   - 解决：通过 PickupContainer 或 InsideCircle（无 Rigidbody2D）作为父对象

2. **碰撞体问题**（本次修复）：
   - 拾取物缺少实体碰撞体，无法产生物理碰撞
   - 解决：添加第二个 PolygonCollider2D (IsTrigger = false)

两个问题都需要修复，拾取物才能正常工作。
