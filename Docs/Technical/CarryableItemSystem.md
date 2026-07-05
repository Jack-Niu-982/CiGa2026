# 可拾取物与玩家携带显示系统

这份文档记录船外漂浮物被锚拉回、变成船内可拾取物、玩家拾取显示、放下和消耗的运行时规则。

## 目标

- 船外漂浮物被锚拉回后，随机选择 `Rooms` 下一个子物体，并在其 Trigger 范围内生成可拾取物。
- 生成后的可拾取物必须跟随飞船内部参考系运动，飞船移动或旋转时，物体保持船内相对位置。
- 玩家拾取物品后，不再把真实物体挂在玩家身上。
- 玩家携带状态只通过玩家 Prefab 内的 `PickupSprite` 显示。
- 玩家没有携带物品时，`PickupSprite` 清空并隐藏。

## 运行时流程

### 1. 船外漂浮物

`FloatingItem2D` 负责船外漂浮物。

漂浮物平时使用自己的 Kinematic Rigidbody2D 漂移。被锚命中后：

1. `AnchorRopeRuntime2D` 找到 `FloatingItemAnchorTarget2D`。
2. `FloatingItemAnchorTarget2D` 把锚拉回请求交给 `FloatingItem2D`。
3. `FloatingItem2D` 关闭自身 Collider，移动到锚发射器的 `AnchorItemDropPoint2D.DropWorldPosition`。
4. Fuel/Shield 到达后，从 `Submarine/InsideCircle/Rooms` 的直接子物体中随机选择一个有效 Trigger。
5. 在该 Trigger 内部留边取随机点，生成对应的 `CarryableItem2D` Prefab；Trash 到达后直接删除，不生成舱内物体。

### 2. 船内可拾取物

`FloatingItem2D` 生成 `CarryableItem2D` 后，会寻找 `AnchorItemDropPoint2D` 父级上的 `SubmarineInteriorFollower2D`。

如果找到了船舱内部参考系：

- 新生成的可拾取物会设置为 `SubmarineInteriorFollower2D.transform` 的子物体。
- 设置父级时保持世界坐标不变。
- 后续飞船移动或旋转时，该物体自然跟随船舱内部保持相对位置。

如果没有找到船舱内部参考系，则退回到最近的父级 Rigidbody2D 或 DropPoint 自身。这只是兜底路径，正式 Gameplay 场景里应保证锚发射器位于 `InsideCircle` 船舱内部层级下。

### 3. 玩家拾取

玩家身上的 `PlayerCarryInteractor2D` 负责拾取逻辑。

玩家按独立拾取键时：

- 拾取检测使用以玩家 Transform 为圆心的圆形范围，半径为玩家实体碰撞半径加 `0.1` 世界单位。
- 范围内存在可拾取物时，玩家的 `WorldSpaceCanvas` 会读取 `PlayerController.CurrentInput`：键盘玩家显示 `[E]PickUp`，手柄玩家显示 `[West]PickUp`。配置变化后提示会同步更新。
- 空手时拾取最近的 `CarryableItem2D`。
- 已经携带物品时，如果没有拾取到新物品，则放下当前物品。
- 每个玩家同时只能携带一个物品；手上已有物品时不能拾取第二件，需要先放下或消耗当前物品。

`CarryableItem2D` 被拾取后：

- 记录拾取前的父级。
- 暂停自身 Rigidbody2D。
- 关闭自身 Collider。
- 隐藏自身 SpriteRenderer。
- 不再把真实物体挂到玩家身上。

这样真实物体在携带期间不会参与物理，也不会在玩家身上显示。它只是作为携带数据存在，直到放下或被消耗。

### 4. 玩家手持显示

玩家 Prefab 内有一个 `PickupSprite` 子节点。

`PickupSprite` 挂 `SpriteRenderer`，由 `PlayerCarryInteractor2D` 控制：

- 拾取物品时，读取 `CarryableItem2D.IconSprite` 并显示。
- 放下或消耗物品时，把 Sprite 置空并隐藏。

当前 `PickupSprite` 位于玩家身体右侧的手持位置，由 Prefab 内的本地坐标控制。后续调整位置、缩放或排序，只需要改 `Player.prefab`。

`GameplayHudPanel` 中每个玩家槽位的 `HeldItem/ItemIcon` 同时订阅携带状态：

- 拾取后显示 `CarryableItem2D.IconSprite`。
- 放下或消耗后清空。
- `ItemIcon` 固定为 `38 x 38`，并启用 `Preserve Aspect`，避免物品图标被横向拉伸。

### 5. 放下与消耗

玩家放下物品时：

- `CarryableItem2D` 回到拾取前的父级。
- 按玩家身上的放下偏移设置世界坐标。
- 恢复 Rigidbody2D、Collider 和 SpriteRenderer。
- `PickupSprite` 清空。

如果物品被系统消耗，例如护盾物品投入血量系统：

- 接收系统调用 `PlayerCarryInteractor2D.NotifyHeldItemDropped` 清空携带状态。
- 销毁真实 `CarryableItem2D` 对象。
- `PickupSprite` 随携带状态清空。

## Prefab 约定

### 玩家 Prefab

`Assets/Prefabs/Gameplay/Player.prefab` 需要包含：

- `PlayerCarryInteractor2D`
- `PickupSprite` 子节点
- `PickupSprite` 上的 `SpriteRenderer`
- `WorldSpaceCanvas` 子节点及其 `PlayerWorldActionPrompt`

`PickupSprite` 默认不显示，只有携带物品时才显示对应图标。

### 拾取物 Prefab

拾取物 Prefab 根节点应包含：

- `CarryableItem2D`
- 用于靠近检测的 `Collider2D`
- `Rigidbody2D`
- 至少一个 `SpriteRenderer`

`CarryableItem2D.IconSprite` 可以手动指定。留空时会自动读取子层级中的第一个 `SpriteRenderer.sprite`。

## 关键边界

- 船内可拾取物必须挂在船舱内部参考系下，不能留在世界根节点。
- 玩家携带时不移动真实物体，只显示 `PickupSprite`。
- 真实物体的 Collider 在携带期间必须关闭，避免重复拾取和物理干扰。
- 放下物品时，物体回到原船舱父级并恢复显示和碰撞。

## 验证清单

1. 锚拉回漂浮物后，生成的可拾取物在飞船移动时保持船内相对位置。
2. 玩家拾取后，真实拾取物不显示、不参与碰撞。
3. 玩家进入“玩家半径 + 0.1”的检测范围后，只显示当前输入设备对应的 `[E]PickUp` 或 `[West]PickUp`。
4. 玩家拾取后，手边 `PickupSprite` 和 HUD `HeldItem/ItemIcon` 显示对应物品图标。
5. 玩家放下后，真实拾取物在玩家放下位置恢复显示和碰撞。
6. 消耗物品后，玩家手边和 HUD 中的物品图标清空。
