# CiGa2026 文档索引

这里记录项目设计、技术方案和协作约定。当前核心玩法已经确定为 1-4 人合作的 2D 俯视角“船锚飞船”，第一版文档重点服务玩法原型实现和手感验证。

## 项目基础

- [../README.md](../README.md)：项目概览、环境和当前阶段
- [../AGENTS.md](../AGENTS.md)：协作规则和代码代理工作方式

## 技术方案

- [Technical/UIFlow.md](Technical/UIFlow.md)：MainMenu、房间界面、Gameplay、PausePanel 的界面循环方案
- [Technical/GameplayPlayerSpawning.md](Technical/GameplayPlayerSpawning.md)：房间玩家到 Gameplay 玩家 Prefab 动态生成方案
- [Technical/PickupAndFloatingItems.md](Technical/PickupAndFloatingItems.md)：拾取物与漂浮物 Prefab 生成方案
- [Technical/FloatingItems.md](Technical/FloatingItems.md)：船外漂浮物生成、锚拉回和拾取物掉落闭环
- [Technical/MissionHealthAndSettlement.md](Technical/MissionHealthAndSettlement.md)：终点、血量与胜负结算系统方案
- [Technical/PhysicsLayerAndCollisionRules.md](Technical/PhysicsLayerAndCollisionRules.md)：Gameplay 物理层、碰撞矩阵和船舱内部阻挡约定
- [Technical/ParallelAgentWorkflow.md](Technical/ParallelAgentWorkflow.md)：多 Agent 并行开发流程和验收规范
- [Technical/PluginAndPackageBaseline.md](Technical/PluginAndPackageBaseline.md)：Unity 插件、包版本和关键前置条件记录

## 玩法设计

- [Design/CoreGameplay.md](Design/CoreGameplay.md)：核心玩法、资源循环、锚点操作和一局流程
- [Design/RulesSummary.md](Design/RulesSummary.md)：已确认规则摘要，方便实现时快速查阅
- [Design/PrototypeValidation.md](Design/PrototypeValidation.md)：第一版原型验证重点和暂缓内容

## 后续建议结构

随着实现推进，可以继续补充下面这些文档：

- `Docs/Design/Players.md`：玩家角色、颜色、出生点和碰撞手感
- `Docs/Design/Levels.md`：航道、墙面、障碍物和终点设计
- `Docs/Design/Items.md`：漂浮物数值、刷新节奏和组合规则
- `Docs/Technical/InputSystem.md`：手柄输入、玩家加入和设备管理
- `Docs/Technical/SceneArchitecture.md`：场景切换、管理器和持久对象
- `Docs/Technical/GameplayArchitecture.md`：飞船、锚点、资源和碰撞系统的实现结构
