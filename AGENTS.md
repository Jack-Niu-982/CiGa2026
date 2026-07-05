# AGENTS.md

## 项目定位

本项目是 Unity 多人派对游戏 `CiGa2026`。当前核心方向是 1-4 人合作的 2D 俯视角“船锚飞船”：玩家在飞船内部移动，操作四个方向的船锚抓墙转向、抓取漂浮物，维持燃料和护盾，让飞船穿过危险航道抵达终点。

当前优先验证核心合作手感。间谍、隐藏目标、手柄震动、结算指认等扩展规则暂不进入第一版实现范围，除非用户后续明确要求。

## 玩家生成约定

- Gameplay 玩家对象必须来自可编辑的玩家 Prefab，当前路径为 `Assets/Prefabs/Gameplay/Player.prefab`。
- 房间界面只负责维护玩家槽位、准备状态和设备绑定，不在 Gameplay 场景里预摆固定 P1-P4 玩家本体。
- 进入 Gameplay 前，`GameFlowController` 需要把已准备玩家写入 `GameplaySessionStore`；`Jaeger` 场景里的 `GameplayPlayerSpawner` 再按会话数据动态生成玩家并绑定对应手柄。
- 场景中可以保留出生点、Spawner、父级根节点和流程控制器。玩家本体不要作为常驻场景对象维护。
- 玩家 Prefab 需要保留 `PlayerController`、`PlayerOperateInteractor2D`、`KeyboardPlayerInput` 和 `GamepadPlayerInput` 等可检查组件，方便后续手动拖美术、调碰撞体、补动画和绑定引用。

## 插件与包前置条件

- 当前 Unity 插件和包版本基线记录在 `Docs/Technical/PluginAndPackageBaseline.md`。
- 涉及 Input System、Cinemachine、URP、Unity-MCP、Hot Reload、UI 或相机问题时，先核对这份基线，再判断是否需要升级、回退或重新解析包。
- `com.unity.inputsystem` 当前保持 `1.8.2`，不要随手回退到 `1.6.3`，否则可能重新触发 Cinemachine 3.1.7 的 `activeValueType` 编译错误。

## 多 Agent 并行开发

- 后续需要并行推进时，先参考 `Docs/Technical/ParallelAgentWorkflow.md`。
- 主对话负责定接口、拆任务、划定允许修改范围和禁止修改范围。
- 子 agent 只处理低重叠的局部任务，不直接抢改同一个 Scene、Prefab 或核心脚本。
- 主对话最后统一验收、刷新 Unity、检查 Console、检查 Prefab 引用并提交。

## 回复与文档风格

- 所有回复、方案和文档都使用自然流畅的中文。
- 表达要清楚、具体、好读，避免翻译腔。
- 不要过度使用“不是...而是...”这类句式。
- 写文档时优先服务后续开发，不写空泛口号。

## 工作方式

- 开始修改前先看当前仓库真实状态，包括已有目录、文档、脚本和 Unity 配置。
- 涉及 Unity 编辑器状态、控制台错误、场景内容或包版本时，优先使用 Unity-MCP 读取现场信息。
- 当前处于 Gamejam 快速推进阶段，核心功能优先跑通。实现时保留必要防护即可，不要堆过多卫语句把主流程写散；能直接改现场和验证的，就直接推进。
- 不要凭印象判断项目状态；如果没有实际运行或验证，要明确说明验证边界。
- 不要改动与当前任务无关的用户文件。
- 已存在的未跟踪文件或未提交改动，默认视为用户工作成果，除非用户明确要求，否则不要删除、回滚或重写。

## 文档维护

- 根目录 `README.md` 记录项目概览、环境、文档入口和当前阶段。
- `Docs/README.md` 作为文档索引，负责告诉后来者该读什么。
- 技术方案放在 `Docs/Technical/` 下。
- 玩法、关卡、角色、道具等设计内容明确后，再放入 `Docs/Design/`。
- 当前核心玩法文档以 `Docs/Design/CoreGameplay.md` 和 `Docs/Design/RulesSummary.md` 为准。
- 每次新增重要系统或流程时，同步更新文档索引。

## Unity 项目约定

- Unity 版本以 `ProjectSettings/ProjectVersion.txt` 为准。
- 包版本以 `Packages/manifest.json` 和 Unity Package Manager 实际加载结果为准。
- 当前输入系统使用 `com.unity.inputsystem`。
- 本地多人输入优先围绕 Unity Input System 的设备接入、玩家加入、玩家槽位和手柄状态来设计。
- Gameplay 第一版优先验证船内移动、四向锚点、燃料、护盾、漂浮物和船体碰撞，不急着扩展复杂局外规则。
- 每个主要场景保留自己的 Camera。MainMenu 使用菜单 Camera，Gameplay 使用玩法 Camera，不做跨场景 Camera 单例。
- 可以使用跨场景 Manager，但要考虑关闭 Domain Reload 和 Scene Reload 后的残留风险。任何静态状态、单例实例、事件订阅和缓存引用都必须有明确的去重、解绑和重置路径。
- 当前不引入 QFramework。先采用轻量分层：System 管状态，Controller 管流程，View 只接 Inspector 引用并刷新显示。等系统复杂度明显上升后，再评估是否需要框架。

## UI 制作约定

- UI 面板必须做成可手动编辑的 Prefab，例如 `MainMenuPanel`、`RoomPanel`、`PausePanel`。
- 不要在 Runtime 临时拼接整套 UI 层级，不要用代码创建完整面板结构。
- 美术资源、布局、层级、动画、按钮、文本、图标和引用应尽量留在 Prefab 与 Inspector 中维护。
- Runtime 脚本只负责状态切换、显示隐藏、按钮事件、手柄房间数据刷新、P1-P4 槽位状态更新等必要逻辑。
- 如果某些 Prefab 引用、资源绑定或版面调整不方便自动完成，要明确告诉用户由用户在 Unity 编辑器中最终绑定。
- 区分不同 UI 布局目标：确认弹窗的按钮行可以使用 `childForceExpandWidth/Height` 让按钮平分伸展；玩家状态栏、背包格、角色卡等数量可变的卡片组不要使用横向 expand，也不要给子卡片设置 `flexibleWidth`，应固定单卡尺寸并让整组居中排列，避免 1 个玩家时状态框被拉成 4 人宽度。
