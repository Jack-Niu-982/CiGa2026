# 拾取物与漂浮物 Prefab 方案

这份文档记录第一版 Gameplay 漂浮物资产生成方案。当前目标是先把“可以被锚抓住、可以被玩家携带”的资产基础打通，具体燃料补充、护盾恢复、垃圾惩罚等效果先不在这一版里实现。

## 第一批类型

第一批生成三类 Prefab：

- Fuel：燃料漂浮物，第一版只作为可拾取、可携带物体存在。
- Trash：垃圾漂浮物，第一版只作为干扰型可拾取、可携带物体存在。
- Shield：护盾漂浮物，先预留 Prefab 和类型入口，方便后续接入护盾恢复逻辑。

运行时脚本预计由拾取物系统提供：

- `CarryableItem2D`：挂在漂浮物 Prefab 根节点上，保存漂浮物类型和携带状态。
- `PlayerCarryInteractor2D`：挂在玩家侧，负责拾取、放下或交互。
- `CarryableItemType`：类型枚举，建议至少包含 `Fuel`、`Trash`、`Shield`。

## 生成入口

Unity 菜单路径：

`CiGa2026/Build Floating Item Prefabs`

菜单脚本路径：

`Assets/Scripts/Editor/FloatingItemPrefabBuilder.cs`

这个菜单会生成或覆盖下面这些可编辑 Prefab：

- `Assets/Prefabs/Gameplay/Pickups/FuelPickUpInRoom.prefab`
- `Assets/Prefabs/Gameplay/Pickups/TrashPickUpInRoom.prefab`
- `Assets/Prefabs/Gameplay/Pickups/ShieldPickUpInRoom.prefab`

菜单还会在 `Assets/Prefabs/Gameplay/Pickups/GeneratedSprites/` 下生成临时占位 Sprite。占位图只用于让 Prefab 在 Scene 里能被看见，后续应由美术资源替换。

## Prefab 结构

每个 Prefab 根节点预期包含：

- `SpriteRenderer`：显示漂浮物外观。当前使用纯色占位 Sprite，后续直接在 Inspector 替换 `Sprite` 即可。
- `BoxCollider2D`：触发器碰撞体，`Is Trigger` 为开启状态。美术替换后可以手动调整大小。
- `Rigidbody2D`：`Body Type` 为 `Kinematic`，`Gravity Scale` 为 `0`，用于配合触发器和外部漂浮移动系统。
- `CarryableItem2D`：生成器会自动添加，并写入 `Fuel`、`Trash`、`Shield` 类型。

运行菜单前需要先保证拾取物运行时脚本已经通过编译。这样生成出来的 Prefab 会直接带上 `CarryableItem2D` 和正确类型。

## 手动替换美术

后续接入正式美术时，建议按下面步骤处理：

1. 把正式贴图导入 Unity，并确认 Texture Type 为 `Sprite (2D and UI)`。
2. 打开对应 Prefab，把根节点 `SpriteRenderer.Sprite` 替换成正式 Sprite。
3. 根据 Sprite 尺寸调整 `BoxCollider2D.Size` 和 `Offset`。
4. 如果需要动画或子物体表现，可以在 Prefab 内增加子节点，但保持根节点上的 `CarryableItem2D`、`Rigidbody2D`、触发器 Collider 不变。

## 第一版边界

第一版只确认拾取和携带链路：

- 锚或玩家可以识别 `CarryableItem2D`。
- 物体可以被携带或放下。
- Prefab 允许美术和关卡人员继续手动编辑。

## 当前集成状态

- `Assets/Prefabs/Gameplay/Player.prefab` 已挂载 `PlayerCarryInteractor2D`。
- 玩家 Prefab 下已添加 `CarryHoldPoint`，用于手动调整手持物品的位置。
- `PlayerCarryInteractor2D` 会在玩家正在操作锚点或其他设施时暂停响应拾取键，避免和 `PlayerOperateInteractor2D` 抢同一个交互输入。
- `FuelPickUpInRoom`、`TrashPickUpInRoom`、`ShieldPickUpInRoom` 已生成，均包含 `CarryableItem2D`、`SpriteRenderer`、Trigger Collider2D 和 `Rigidbody2D`。
- `FuelPickUpInRoom` 与 `ShieldPickUpInRoom` 使用原 Pickup Idle 动画，`TrashPickUpInRoom` 不使用动画。

下面内容暂不进入这份 Prefab 生成方案：

- Fuel 对飞船燃料数值的实际补充。
- Shield 对飞船护盾数值的实际恢复。
- Trash 的惩罚、污染、误抓反馈或计分。
- 漂浮物刷新器、关卡波次和掉落权重。

这些逻辑后续应该接入 Gameplay 系统层，并在明确数值和流程后补充到设计文档或新的技术文档中。
