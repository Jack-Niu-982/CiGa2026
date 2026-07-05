# CiGa2026

CiGa2026 是一个 Unity 多人派对游戏项目。当前核心玩法方向是 1-4 人合作的 2D 俯视角“船锚飞船”：玩家在飞船内部奔跑，操作四个方向的船锚抓墙转向、抓取漂浮物补充燃料和护盾，让飞船穿过危险航道并抵达终点。

游戏主要使用手柄进行本地同屏游玩。当前版本先验证核心合作手感，暂不展开间谍、隐藏目标、结算指认等扩展玩法。

## 项目环境

- Unity：2022.3.8f1
- 渲染管线：URP 14.0.8
- 输入系统：Input System 1.8.2
- 主要目标：本地多人同屏派对游戏
- 玩家数量：1-4 人，目标流程按 4 个手柄接入设计
- 玩法视角：2D 俯视角

## 文档入口

- [Docs/README.md](Docs/README.md)：项目文档索引
- [Docs/Design/CoreGameplay.md](Docs/Design/CoreGameplay.md)：核心玩法设计
- [Docs/Design/RulesSummary.md](Docs/Design/RulesSummary.md)：已确认规则摘要
- [Docs/Design/PrototypeValidation.md](Docs/Design/PrototypeValidation.md)：第一版原型验证重点
- [Docs/Technical/UIFlow.md](Docs/Technical/UIFlow.md)：MainMenu 到 Gameplay 的界面循环技术方案
- [Docs/Technical/GameplayPlayerSpawning.md](Docs/Technical/GameplayPlayerSpawning.md)：房间玩家到 Gameplay 玩家 Prefab 动态生成方案
- [Docs/Technical/PickupAndFloatingItems.md](Docs/Technical/PickupAndFloatingItems.md)：拾取物与漂浮物 Prefab 生成方案
- [Docs/Technical/FuelAndDepositPoints.md](Docs/Technical/FuelAndDepositPoints.md)：Fuel 数值、HUD 与物品投入点
- [Docs/Technical/ParallelAgentWorkflow.md](Docs/Technical/ParallelAgentWorkflow.md)：多 Agent 并行开发流程和验收规范
- [Docs/Technical/PluginAndPackageBaseline.md](Docs/Technical/PluginAndPackageBaseline.md)：Unity 插件、包版本和关键前置条件记录
- [AGENTS.md](AGENTS.md)：协作和代码代理工作规则

## 当前阶段

当前阶段先验证核心玩法是否成立：

1. 建立 MainMenu、房间界面、Gameplay、PausePanel 的基础循环。
2. 实现 1-4 名玩家在飞船内部移动和碰撞。
3. 实现四个局部方向锚点的瞄准、发射、命中和收回。
4. 实现燃料、护盾、垃圾、蛛网、炸药桶等基础漂浮物。
5. 验证燃料压力、护盾压力、抓墙转向和误抓混乱是否足够有趣。
