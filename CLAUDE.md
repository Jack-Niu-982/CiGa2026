# CiGa2026 项目协作指南

## 项目概览

CiGa2026 是一个 Unity 多人派对游戏项目。核心玩法是 1-4 人合作的 2D 俯视角"船锚飞船"：玩家在飞船内部奔跑，操作四个方向的船锚抓墙转向、抓取漂浮物补充燃料和护盾，让飞船穿过危险航道并抵达终点。

游戏主要使用手柄进行本地同屏游玩。当前版本先验证核心合作手感，暂不展开间谍、隐藏目标、结算指认等扩展玩法。

## 技术环境

- Unity：2022.3.8f1
- 渲染管线：URP 14.0.8
- 输入系统：Input System 1.8.2
- 目标平台：本地多人同屏派对游戏
- 玩家数量：1-4 人，使用手柄接入
- 玩法视角：2D 俯视角

## 重要文档入口

### 核心文档
- [README.md](README.md)：项目概览和环境说明
- [AGENTS.md](AGENTS.md)：协作规则和代码代理工作方式
- [Docs/README.md](Docs/README.md)：完整文档索引

### 玩法设计
- [Docs/Design/CoreGameplay.md](Docs/Design/CoreGameplay.md)：核心玩法设计（必读）
- [Docs/Design/RulesSummary.md](Docs/Design/RulesSummary.md)：已确认规则摘要
- [Docs/Design/FloatingItemTypes.md](Docs/Design/FloatingItemTypes.md)：漂浮物类型设计
- [Docs/Design/PrototypeValidation.md](Docs/Design/PrototypeValidation.md)：第一版原型验证重点

### 技术方案
- [Docs/Technical/UIFlow.md](Docs/Technical/UIFlow.md)：MainMenu 到 Gameplay 的界面循环
- [Docs/Technical/GameplayPlayerSpawning.md](Docs/Technical/GameplayPlayerSpawning.md)：玩家动态生成方案
- [Docs/Technical/FloatingItems.md](Docs/Technical/FloatingItems.md)：船外漂浮物系统
- [Docs/Technical/FloatingItemTypes.md](Docs/Technical/FloatingItemTypes.md)：漂浮物类型和效果系统
- [Docs/Technical/CarryableItemSystem.md](Docs/Technical/CarryableItemSystem.md)：船内可拾取物系统
- [Docs/Technical/StationSystem.md](Docs/Technical/StationSystem.md)：工作站系统（燃料站、修复站）
- [Docs/Technical/InteractionSystem.md](Docs/Technical/InteractionSystem.md)：交互系统设计
- [Docs/Technical/MissionHealthAndSettlement.md](Docs/Technical/MissionHealthAndSettlement.md)：终点、血量与胜负结算系统方案
- [Docs/Technical/OffScreenTargetIndicator.md](Docs/Technical/OffScreenTargetIndicator.md)：屏幕外终点指示箭头使用说明
- [Docs/Technical/UnityMCPWorkflow.md](Docs/Technical/UnityMCPWorkflow.md)：Unity-MCP 工作流程示例和最佳实践
- [Docs/Technical/PhysicsLayerAndCollisionRules.md](Docs/Technical/PhysicsLayerAndCollisionRules.md)：物理层和碰撞规则
- [Docs/Technical/ParallelAgentWorkflow.md](Docs/Technical/ParallelAgentWorkflow.md)：多 Agent 并行开发流程
- [Docs/Technical/PluginAndPackageBaseline.md](Docs/Technical/PluginAndPackageBaseline.md)：插件和包版本基线

## 工作原则

### 开始工作前
1. **查看真实状态**：检查当前仓库真实状态，包括已有目录、文档、脚本和 Unity 配置
2. **使用 Unity-MCP**：涉及 Unity 编辑器状态、控制台错误、场景内容时，优先使用 Unity-MCP 读取现场信息
3. **不凭印象判断**：如果没有实际运行或验证，要明确说明验证边界
4. **只改相关文件**：不要改动与当前任务无关的用户文件
5. **保护用户成果**：已存在的未跟踪文件或未提交改动，默认视为用户工作成果，除非用户明确要求

### 玩家生成约定
- Gameplay 玩家对象必须来自可编辑的玩家 Prefab：`Assets/Prefabs/Gameplay/Player.prefab`
- 房间界面只负责维护玩家槽位、准备状态和设备绑定
- 不在 Gameplay 场景里预摆固定 P1-P4 玩家本体
- 进入 Gameplay 前，`GameFlowController` 把已准备玩家写入 `GameplaySessionStore`
- `GameplayPlayerSpawner` 按会话数据动态生成玩家并绑定对应手柄
- 场景中可以保留出生点、Spawner、父级根节点和流程控制器
- 玩家 Prefab 保留 `PlayerController`、`PlayerOperateInteractor2D`、`KeyboardPlayerInput` 和 `GamepadPlayerInput` 等组件

### 插件与包版本
- 插件和包版本基线记录在 `Docs/Technical/PluginAndPackageBaseline.md`
- `com.unity.inputsystem` 当前保持 `1.8.2`，不要随手回退
- 涉及 Input System、Cinemachine、URP、Unity-MCP 问题时，先核对基线

### Unity 项目约定
- Unity 版本以 `ProjectSettings/ProjectVersion.txt` 为准
- 包版本以 `Packages/manifest.json` 和 Unity Package Manager 实际加载结果为准
- 输入系统使用 `com.unity.inputsystem`
- 本地多人输入围绕 Unity Input System 的设备接入、玩家加入、玩家槽位和手柄状态设计
- 每个主要场景保留自己的 Camera（MainMenu 用菜单 Camera，Gameplay 用玩法 Camera）
- 不做跨场景 Camera 单例
- 可以使用跨场景 Manager，但要考虑关闭 Domain Reload 和 Scene Reload 后的残留风险
- 静态状态、单例实例、事件订阅和缓存引用必须有明确的去重、解绑和重置路径
- 当前不引入 QFramework，采用轻量分层：System 管状态，Controller 管流程，View 只接 Inspector 引用并刷新显示

### UI 制作约定
- UI 面板必须做成可手动编辑的 Prefab（如 `MainMenuPanel`、`RoomPanel`、`PausePanel`）
- 不要在 Runtime 临时拼接整套 UI 层级
- 不要用代码创建完整面板结构
- 美术资源、布局、层级、动画、按钮、文本、图标和引用尽量留在 Prefab 与 Inspector 中维护
- Runtime 脚本只负责状态切换、显示隐藏、按钮事件、手柄房间数据刷新、P1-P4 槽位状态更新等必要逻辑
- 如果某些 Prefab 引用、资源绑定或版面调整不方便自动完成，明确告诉用户在 Unity 编辑器中最终绑定
- **实现新 UI 功能时**：优先通过 Unity-MCP 在场景中创建实例验证，或直接制作 Prefab，而不是只提供脚本让用户手动搭建
- 注意 UI 布局目标：
  - 确认弹窗的按钮行可以使用 `childForceExpandWidth/Height` 让按钮平分伸展
  - 玩家状态栏、背包格等数量可变的卡片组不要使用横向 expand，也不要给子卡片设置 `flexibleWidth`
  - 应固定单卡尺寸并让整组居中排列，避免 1 个玩家时状态框被拉成 4 人宽度

### Unity-MCP 使用约定
- **优先使用 Unity-MCP 工具**：当需要在场景中创建对象、修改组件、制作 Prefab 时，优先使用 Unity-MCP 工具而非仅提供脚本
- **提供完整可用的实例**：实现新功能时，直接在场景中创建可用的实例或 Prefab，避免让用户从零手动搭建
- **场景对象创建流程**：
  1. 使用 `manage_scene` 或相关工具在场景中创建 GameObject
  2. 使用 `manage_component` 添加必要组件
  3. 使用 `set_property` 配置组件参数
  4. 如需创建 Prefab，使用 `manage_asset` 将场景对象保存为 Prefab
- **编辑器工具作为补充**：可以提供编辑器菜单工具方便后续快速创建，但不应该作为唯一的实现方式
- **文档说明**：在技术文档中说明如何手动创建和配置，作为 Unity-MCP 自动创建的备选方案

### 代码风格
- 所有代码注释和文档使用自然流畅的中文
- 表达清楚、具体、好读，避免翻译腔
- 不要过度使用"不是...而是..."这类句式
- 写文档时优先服务后续开发，不写空泛口号
- 只写必要注释（WHY，不是 WHAT）

### 多 Agent 并行开发
- 需要并行推进时，先参考 `Docs/Technical/ParallelAgentWorkflow.md`
- 主对话负责定接口、拆任务、划定允许修改范围和禁止修改范围
- 子 agent 只处理低重叠的局部任务，不直接抢改同一个 Scene、Prefab 或核心脚本
- 主对话最后统一验收、刷新 Unity、检查 Console、检查 Prefab 引用并提交

## 当前阶段重点

当前阶段先验证核心玩法是否成立：

1. 建立 MainMenu、房间界面、Gameplay、PausePanel 的基础循环
2. 实现 1-4 名玩家在飞船内部移动和碰撞
3. 实现四个局部方向锚点的瞄准、发射、命中和收回
4. 实现燃料、护盾、垃圾、蛛网、炸药桶等基础漂浮物
5. 验证燃料压力、护盾压力、抓墙转向和误抓混乱是否足够有趣

**不在第一版范围内**：间谍、隐藏目标、手柄震动、结算指认等扩展规则，除非用户后续明确要求。

## 核心玩法要点

### 游戏目标
团队目标是让飞船在护盾归零前抵达终点。

### 资源系统
- **燃料**：飞船前进资源，燃料不足速度下降，燃料耗尽停止前进
- **护盾**：飞船生命值，碰撞墙面、障碍物或炸药桶时扣除护盾，护盾归零即失败
- **玩家携带上限**：燃料上限 3，护盾上限 3

### 四个锚点
- 飞船有四个局部方向锚点：上、下、左、右
- 锚点跟随船体旋转，不固定对应屏幕方向
- 每个锚点有船内操作装置，玩家必须跑到对应装置并停留操作
- 锚点用途：抓外部墙面改变飞船方向，抓外部漂浮物获得资源或触发干扰
- 参考《黄金矿工》的爪子逻辑：围绕基础方向旋转偏移（暂定 90 度），发射后碰到目标立即抓取并收回

### 漂浮物类型
- **燃料**：进入操作玩家身上，需投入燃料装置
- **护盾**：进入操作玩家身上，需投入护盾装置
- **垃圾**：直接消失，没有收益，浪费抓取机会
- **蛛网**：让命中锚点在一段时间内发射和收回速度变慢
- **炸药桶**：在外部原地爆炸，飞船扣除护盾，不会被拉回船上

### 核心循环
1. 玩家在船内跑到锚点操作装置
2. 操作锚点，瞄准外部墙面或漂浮物
3. 锚发射并命中目标，随后立即收回
4. 命中墙面时，飞船获得一次转向冲量
5. 命中燃料或护盾时，资源进入操作玩家身上
6. 玩家把资源带到船内对应装置并投入
7. 团队用燃料维持前进，用护盾承受失误，用锚点修正航向
8. 飞船抵达终点则胜利，护盾归零则失败

## 文档维护规则

- 根目录 `README.md` 记录项目概览、环境、文档入口和当前阶段
- `Docs/README.md` 作为文档索引，负责告诉后来者该读什么
- 技术方案放在 `Docs/Technical/` 下
- 玩法、关卡、角色、道具等设计内容放入 `Docs/Design/`
- 当前核心玩法文档以 `Docs/Design/CoreGameplay.md` 和 `Docs/Design/RulesSummary.md` 为准
- 每次新增重要系统或流程时，同步更新文档索引

## 常见问题

### 如何添加新功能？
1. 先阅读相关设计文档（`Docs/Design/`）和技术方案（`Docs/Technical/`）
2. 检查是否与当前阶段重点一致
3. 如果涉及多个系统，考虑使用多 Agent 并行开发
4. 实现后更新相关文档索引

### 如何处理 Unity 编译错误？
1. 使用 Unity-MCP 的 `read_console` 工具读取控制台信息
2. 检查 `Docs/Technical/PluginAndPackageBaseline.md` 确认包版本
3. 确认是否是 Domain Reload 或包版本冲突问题
4. 修复后再次用 `read_console` 确认

### 如何测试多人玩法？
1. 从 `MainMenu` 场景开始运行
2. 在房间界面用手柄加入（或用键盘模拟）
3. 确认 P1-P4 槽位状态
4. 满足开始条件后进入 Gameplay
5. 测试玩家移动、锚点操作、资源携带和工作站交互

### 如何查看物理层配置？
参考 `Docs/Technical/PhysicsLayerAndCollisionRules.md`，了解当前物理层和碰撞矩阵设置。

## Git 工作流

- 当前分支：MainMenu
- 主分支：main
- 提交信息使用中文
- 每次提交前检查 Unity Console 是否有错误
- 提交前验证 Prefab 引用是否正确
