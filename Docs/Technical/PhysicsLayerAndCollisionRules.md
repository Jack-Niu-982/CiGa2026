# 物理层与碰撞矩阵约定

这份文档记录 Gameplay 物理层的基础约束。后续任何涉及玩家、飞船、船舱墙体、锚点、漂浮物、危险物和触发器的方案，都必须先检查 Layer 与 Physics2D 碰撞矩阵，再谈脚本逻辑。

## 为什么先看物理层

Unity 2D 里，Collider 是否接触并不只由脚本决定，还由下面几件事共同决定：

- GameObject 所在 Layer。
- `ProjectSettings/Physics2DSettings.asset` 中的 Layer Collision Matrix。
- Collider 是否是 Trigger。
- Rigidbody2D 的 Body Type，以及 Collider 是否附着到对应 Rigidbody2D。

如果这些基础关系没理清，容易出现两类问题：

- 应该挡住玩家的隐藏墙没有挡住。
- 船舱内部墙体和飞船本体互相碰撞，导致船体被自己的内部结构顶开或抖动。

所以技术方案里只写“新增 Collider”是不够的，必须同时写清楚这些 Collider 属于哪一层、和哪些层碰撞、和哪些层不碰撞。

## 当前 Layer 约定

当前项目里已经定义了这些关键层：

- `Player`：玩家角色。
- `Submarine`：飞船本体，挂 Dynamic Rigidbody2D。
- `SubmarineInterior`：船舱内部碰撞代理、内部墙体、操作点和隐藏边界墙。
- `Anchor`：锚。
- `AnchorLauncher`：锚操作/发射点。
- `FloatingItem`：漂浮拾取物。
- `CarryableItem`：船内携带物品（从漂浮物抓回船内后生成的可拾取物）。

## 船舱内部碰撞规则

`Submarine` 和 `SubmarineInterior` 必须在 Physics2D 碰撞矩阵中互相忽略。

原因：

- 飞船本体是外部航行物理对象。
- 船舱内部墙体只应该服务玩家移动和船内交互。
- 如果二者发生碰撞，飞船会被自己的内部墙体顶住，后续调参会变得不可控。

同时，`Player` 和 `SubmarineInterior` 必须保持碰撞。

原因：

- 玩家需要被船舱墙体、隐藏边界和内部结构阻挡。
- 玩家不能直接走出船舱外部。

## 携带物品碰撞规则

`CarryableItem` 层用于锚点拉回船内后生成的可拾取物品。

碰撞配置：
- 与 `SubmarineInterior` 碰撞：物品会停留在船舱地板上，不会穿船而过
- 与 `Floor` 碰撞：物品落在地板上
- 与 `Player` 碰撞（通过 Trigger）：玩家可以拾取物品

每个 CarryableItem Prefab 包含：
- 一个 `PolygonCollider2D`（Trigger）：用于玩家拾取检测
- 一个 `PolygonCollider2D`（实体）：用于物理落地和船舱内部碰撞
- `Rigidbody2D`：初始为 Kinematic，掉落时切换为 Dynamic，重力缩放为 0.3，阻力为 0.5

当前 `Jaeger` 场景中：

- `Submarine` 根节点保持在 `Submarine` 层。
- `Submarine/InsideCircle` 以及其子节点全部放在 `SubmarineInterior` 层。
- `Submarine/InsideCircle/CabinBoundary` 下的隐藏边界墙属于 `SubmarineInterior` 层。

## 实现或排查清单

新增任何 Gameplay Collider 前，先检查：

1. 这个 Collider 是实体阻挡还是 Trigger。
2. 这个 Collider 应该属于哪一个 Layer。
3. 它是否会附着到某个父级 Rigidbody2D。
4. 它是否可能和自己的父级、载具本体或生成器发生自碰撞。
5. Physics2D Layer Collision Matrix 是否已经表达了这些关系。

如果要新增船内墙体：

- 放到 `SubmarineInterior` 层。
- 确认它与 `Player` 层碰撞。
- 确认它与 `Submarine` 层不碰撞。
- 如果是隐藏阻挡，只保留 Collider，不需要 SpriteRenderer。

## 验证方式

用 Unity-MCP 或 Unity 编辑器确认：

- `Physics2D.GetIgnoreLayerCollision(Submarine, SubmarineInterior)` 应为 `true`。
- `Physics2D.GetIgnoreLayerCollision(Player, SubmarineInterior)` 应为 `false`。
- `InsideCircle` 的 Rigidbody2D 附加了船舱内部 Collider。
- 进入 Play Mode 后，玩家不能走出船舱；飞船本体不会被内部隐藏墙顶动。
