# 漂浮物系统方案

本文记录第一版船外漂浮物系统的实现和验收方式。当前目标是先打通这条闭环：

`船外漂浮物生成 -> 锚命中漂浮物 -> 漂浮物被拉向对应锚位 -> 船内生成可拾取物 -> 玩家拾取、搬运、使用`

## 设计边界

第一版只做稳定闭环，不急着做复杂波次和完整数值经济。

已接入：

- 船外漂浮物自动生成。
- 漂浮物按固定方向缓慢漂移和旋转。
- 锚可以命中漂浮物。
- 漂浮物被锚命中后拉向该锚对应的掉落点。
- 抵达掉落点后，生成现有拾取系统使用的 `FuelPickup`、`ShieldPickup`、`TrashPickup`。
- 玩家后续用已有 `PlayerCarryInteractor2D` 拾取、放下、运输。

暂未接入：

- 复杂波次、权重曲线和关卡阶段。
- 锚抓到垃圾、炸药桶、蛛网等特殊效果。
- 燃料节点和护盾节点的完整数值闭环。
- 漂浮物和船体碰撞后的额外效果。

## 运行时脚本

- `FloatingItem2D`
  - 船外漂浮物主体。
  - 控制漂移、旋转、生命周期、被锚拉回。
  - 抵达锚位后实例化对应拾取物。

- `FloatingItemAnchorTarget2D`
  - 锚命中识别入口。
  - 挂在船外漂浮物 Prefab 根节点。

- `AnchorItemDropPoint2D`
  - 挂在每个锚发射器上。
  - 表示该锚拉回漂浮物后，在船内/锚操作点旁边生成拾取物的位置。

- `FloatingItemSpawnZone2D`
  - 矩形生成区域。
  - 每个区域可配置自己的漂移方向。

- `FloatingItemSpawner2D`
  - 按间隔和权重生成船外漂浮物。
  - 当前 `Jaeger` 场景中有 `FloatingItemRuntimeRoot` 承载运行时生成器。

## Prefab

船外漂浮物 Prefab：

- `Assets/Prefabs/Gameplay/FloatingItems/FloatingFuel.prefab`
- `Assets/Prefabs/Gameplay/FloatingItems/FloatingShield.prefab`
- `Assets/Prefabs/Gameplay/FloatingItems/FloatingTrash.prefab`

玩家可拾取物 Prefab：

- `Assets/Prefabs/Gameplay/Pickups/FuelPickup.prefab`
- `Assets/Prefabs/Gameplay/Pickups/ShieldPickup.prefab`
- `Assets/Prefabs/Gameplay/Pickups/TrashPickup.prefab`

船外漂浮物和玩家拾取物分开维护。船外漂浮物负责被锚抓和拉回；玩家拾取物负责被玩家拿起、放下和交给交互节点。

## 场景配置

`Assets/Scenes/Jaeger.unity` 中新增：

- `FloatingItemRuntimeRoot`
  - `FloatingItemSpawner2D`
  - `ActiveFloatingItems`
  - `SpawnZones`
    - `RightSpawnZone`
    - `LeftSpawnZone`
    - `TopSpawnZone`
    - `BottomSpawnZone`

每个锚发射器新增：

- `AnchorItemDropPoint2D`
- `ItemDropPoint` 子物体

当前掉落点大致位于飞船内部靠近对应锚操作点的位置：

- 右锚：`(1.2, 0)`
- 左锚：`(-1.2, 0)`
- 上锚：`(0, 1.2)`
- 下锚：`(0, -1.2)`

## Layer

新增项目 Layer：

- `FloatingItem`

四个 `AnchorLaunchDetector2D` 的 `attachableLayer` 已包含：

- `Wall`
- `FloatingItem`

同时 `allowTriggerTargets = true`，因为船外漂浮物使用 Trigger Collider 供锚射线命中。

## 生成器

菜单入口：

`CiGa2026/Build Floating Item Prefabs`

该菜单会生成或覆盖：

- Pickup Prefab
- FloatingItem Prefab
- 占位 Sprite

后续替换美术时，优先改 Prefab 内的 `SpriteRenderer`，保持根节点上的 `FloatingItem2D`、`FloatingItemAnchorTarget2D`、`Rigidbody2D`、Collider 不变。

## 验收方式

静态验收：

1. 打开三个 `FloatingItems` Prefab。
2. 确认根节点 Layer 是 `FloatingItem`。
3. 确认有 `FloatingItem2D` 和 `FloatingItemAnchorTarget2D`。
4. 确认 Collider 是 Trigger。
5. 确认 `Rigidbody2D` 是 Kinematic，`Gravity Scale = 0`。
6. 确认 `FloatingItem2D.pickupPrefab` 指向对应的 Pickup Prefab。
7. 确认 `Jaeger` 中四个锚的 `AnchorLaunchDetector2D` 包含 `Wall + FloatingItem`。
8. 确认四个锚都有 `AnchorItemDropPoint2D`。

运行验收：

1. 从主菜单进入 Gameplay。
2. 等待生成器生成船外漂浮物。
3. 操作任意锚命中漂浮物。
4. 漂浮物应被拉向对应锚的 `ItemDropPoint`。
5. 抵达后船外漂浮物消失，并在锚操作点旁边生成对应 Pickup。
6. 玩家靠近 Pickup，按交互键拾取。
7. 手持状态再次按交互键可以放下。

当前未执行 Play Mode 验收；本轮只做编辑器静态检查和编译检查。
