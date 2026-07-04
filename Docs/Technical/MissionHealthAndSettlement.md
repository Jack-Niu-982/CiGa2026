# 终点、血量与结算系统方案

这份文档记录第一版 Gameplay 胜负闭环的实现方式。当前目标是把“护盾归零失败”和“飞船抵达终点胜利”先打通，让核心合作玩法有明确的一局开始和结束。复杂结算面板、评分、重开、关卡选择和指认流程后续再做。

## 目标

- 飞船拥有一份可配置的血量，也就是当前设计文档里的护盾生命值。
- 飞船受到危险碰撞或爆炸影响时扣血。
- 血量降到 0 时，本局行动失败。
- 飞船进入终点触发区域时，本局胜利。
- 胜利或失败后只结算一次，后续触发不再重复覆盖结果。
- 第一版先通过脚本事件和日志确认流程，UI 面板可以后续接入同一套事件。

## 运行时组件

### `SubmarineHealth2D`

挂在 `Submarine` 根节点上，负责维护飞船血量。

核心职责：

- 保存 `maxHealth`、`currentHealth`。
- 提供 `Damage(float amount)` 和 `Repair(float amount)`。
- 血量变化时触发 `OnHealthChanged`。
- 血量归零时触发 `OnDepleted`。
- 可选实现 `ICarryItemReceiver`，让玩家把 `Shield` 类型拾取物投入后恢复血量。

第一版数值建议：

- 最大血量：`100`
- 单次危险碰撞伤害：`25`
- 单个护盾拾取物恢复：`25`

### `SubmarineDamageReceiver2D`

挂在 `Submarine` 或危险碰撞代理上，负责把物理碰撞转成扣血。

核心职责：

- 监听 `OnCollisionEnter2D`。
- 根据 LayerMask 或 Tag 判断是否是危险对象。
- 带一个冷却时间，避免同一次贴边碰撞连续扣血。
- 调用 `SubmarineHealth2D.Damage`。

第一版可以先把危险来源暴露成 Inspector 配置，由关卡人员给墙、障碍物或爆炸对象配置 Layer/Tag。没有配置时不主动扣血，避免误伤船内玩家或内部碰撞代理。

### `FinishZone2D`

挂在终点触发器对象上，负责胜利判定。

核心职责：

- 监听 `OnTriggerEnter2D`。
- 判断进入对象是否是目标飞船。优先使用 Inspector 指定的 `Submarine`，也可以通过 `SubMarine` Tag 兜底。
- 命中后调用 `MissionSettlementController.Win()`。

终点对象需要：

- `Collider2D`，并打开 `Is Trigger`。
- 清晰的场景命名，例如 `FinishZone`。
- 可视化占位表现可以先用 `SpriteRenderer` 或 Gizmos，后续再替换成正式终点美术。

### `MissionSettlementController`

挂在 Gameplay 场景中的独立管理对象上，负责胜负状态机。

核心职责：

- 保存当前状态：`Running`、`Won`、`Failed`。
- 监听 `SubmarineHealth2D.OnDepleted`。
- 被 `FinishZone2D` 调用胜利。
- 胜负只结算一次。
- 结算时触发 `OnMissionSettled`，并输出明确日志。
- 可选冻结飞船速度，避免结算后继续漂移。

第一版不直接切场景，不运行时创建整套 UI。后续的胜利/失败面板应该做成 Prefab，在 Inspector 里引用并监听结算事件。

## 场景接线

`Assets/Scenes/Jaeger.unity` 中建议这样接：

1. 在 `Submarine` 上挂 `SubmarineHealth2D`。
2. 在 `Submarine` 上挂 `SubmarineDamageReceiver2D`，引用同一个 `SubmarineHealth2D`。
3. 新建 `MissionSettlementController` 对象，引用 `SubmarineHealth2D` 和 `Submarine` 的 `Rigidbody2D`。
4. 新建 `FinishZone` 对象，放在航道终点，挂 `BoxCollider2D` 触发器和 `FinishZone2D`。
5. `FinishZone2D` 引用 `MissionSettlementController` 和 `Submarine`。

由于当前有多 Agent 并行修改，场景文件如果已经处于未提交状态，接线时优先使用 Unity-MCP 追加组件和对象，避免手工重写整份 `.unity` 文件。

## 与现有系统的关系

- 不改 `GameplayPlayerSpawner`，玩家仍由房间数据动态生成。
- 不改玩家 Prefab 结构，只复用已有 `PlayerCarryInteractor2D` 和 `ICarryItemReceiver`。
- 不改船锚发射和拾取系统。后续锚命中炸药桶时，可以直接调用 `SubmarineHealth2D.Damage` 或让炸药桶产生危险触发。
- 不改 UIFlow。结算 UI 后续作为单独 Prefab 接入 `MissionSettlementController` 事件。

## 验证边界

本次实现完成后至少做这些检查：

- Unity Console 没有编译错误。
- `Submarine` 上能看到血量组件。
- `FinishZone` 是触发器，并能引用结算控制器。
- 手动让飞船进入终点时，Console 输出胜利日志。
- 手动调用扣血或制造危险碰撞让血量归零时，Console 输出失败日志。

如果没有进入 Play Mode 实测，需要在最终记录中明确说明只完成了静态接线和编译验证。
