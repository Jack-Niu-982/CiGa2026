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
- Fuel 与 Shield 抵达掉落点后，在 `Rooms` 随机子 Trigger 内生成专用的 `FuelPickUpInRoom`、`ShieldPickUpInRoom`。
- Trash 被勾回后直接删除，不在 `Rooms` 内生成 Pickup。
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
  - 表示漂浮物完成回收的判定位置；实际 Pickup 生成点从 `Rooms` 子 Trigger 中随机选择。

漂浮物回收完成以“锚头回到发射器的静止半径内”为准。不要直接比较锚头与 `ItemDropPoint` 的距离，因为两者在四个方向的锚 Prefab 中并不重合。

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

舱内可拾取物 Prefab：

- `Assets/Prefabs/Gameplay/Pickups/FuelPickUpInRoom.prefab`
- `Assets/Prefabs/Gameplay/Pickups/ShieldPickUpInRoom.prefab`
- `Assets/Prefabs/Gameplay/Pickups/TrashPickUpInRoom.prefab`

燃料和护盾沿用各自 Pickup 的 Idle Animator；垃圾保持静态，不挂 Animator。

船外漂浮物和玩家拾取物分开维护。船外漂浮物负责被锚抓和拉回；玩家拾取物负责被玩家拿起、放下和交给交互节点。

场景中的 `FloatingItemSpawner2D > Entries` 为每一种漂浮物提供独立的 `Display Scale`。Spawner 实例化物体后会把该值写给 `FloatingItem2D`；生成动画从 `Display Scale × Spawn Start Scale` 过渡到 `Display Scale`。Prefab 自身的 `FloatingItem2D > Display Scale` 仅在 Entry 数值无效时作为兜底。

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

当前回收判定点大致位于飞船内部靠近对应锚操作点的位置：

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
5. Fuel/Shield 抵达后船外漂浮物消失，并在 `Rooms` 随机子 Trigger 内生成对应 Pickup；Trash 只删除船外对象。
6. 玩家进入“玩家碰撞半径 + 0.1”的圆形范围后，按当前输入设备显示 `[E]PickUp` 或 `[West]PickUp`，按独立拾取键拾取。
7. 拾取后玩家手边和 HUD `HeldItem` 显示物品 Sprite。
8. 手持状态再次按拾取键可以放下。

已完成 Play Mode 运行时验证：随机点采样、范围检测、拾取状态、玩家手持 Sprite 和 HUD 正方形图标均通过。
