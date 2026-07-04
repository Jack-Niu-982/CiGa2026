# 界面循环技术方案

## 目标

本方案描述 CiGa2026 的基础界面循环：

`MainMenu -> RoomPanel -> Gameplay -> PausePanel -> MainMenu`

当前已确认的前提是：游戏为 1-4 人合作的 2D 俯视角多人派对游戏，主要使用手柄进行操作。Gameplay 阶段会进入“船锚飞船”核心玩法：玩家在飞船内部奔跑，操作四向锚点抓墙转向、抓取漂浮物，维持燃料和护盾。

## 界面状态

### MainMenu

主菜单是游戏启动后的入口，负责进入房间界面和退出游戏。

建议包含的基础操作：

- `Start`：进入房间界面。
- `Quit`：退出游戏。

主菜单阶段不分配玩家槽位，只监听全局确认输入。若任意已连接手柄按下确认键，可以触发 `Start`。

### RoomPanel

房间界面用于等待最多 4 个手柄接入，并展示 P1、P2、P3、P4 的准备状态。

建议展示内容：

- P1、P2、P3、P4 四个固定槽位。
- 每个槽位显示状态：未连接、已连接、已准备。
- 每个槽位显示对应手柄编号或设备名。
- 底部显示开始条件，例如“至少 1 名玩家准备后可开始”，具体规则后续可改。

建议交互：

- 手柄按确认键加入并占用一个玩家槽位。
- 已加入玩家再次按确认键切换准备状态。
- 已加入玩家按取消键离开槽位。
- P1 或房主按开始键进入 Gameplay。
- 全部玩家未准备或没有玩家加入时，不允许进入 Gameplay。
- 按返回键回到 MainMenu，并清空房间里的临时玩家槽位。

### Gameplay

Gameplay 是实际游玩场景。第一版 Gameplay 需要承接房间界面生成的玩家会话，并初始化飞船、船内玩家、四个锚点、燃料、护盾、航道和漂浮物。

Gameplay 启动时应接收房间界面确认后的玩家列表，包括：

- 玩家编号：P1-P4。
- 绑定的手柄设备。
- 玩家准备状态。
- 后续可能扩展的角色、颜色或出生点信息。

Gameplay 中需要监听暂停输入。建议只有 P1 或当前房主可以打开 PausePanel，避免 4 个玩家同时抢控制权。暂停时应冻结飞船推进、锚点运动、漂浮物运动和玩家输入，只保留 PausePanel 的 UI 输入。

Gameplay 的胜负出口：

- 飞船在护盾归零前抵达终点，进入胜利结算或临时返回主菜单。
- 飞船护盾归零，进入失败结算或临时返回主菜单。
- 玩家从 PausePanel 选择返回主菜单，直接清理本局状态。

### PausePanel

暂停面板覆盖在 Gameplay 上，用于继续游戏或返回主菜单。

建议包含的操作：

- `Resume`：关闭暂停面板，恢复 Gameplay。
- `Restart`：重新开始当前玩法局，是否需要保留玩家槽位由后续玩法决定。
- `Back To MainMenu`：退出当前局，回到 MainMenu。

返回 MainMenu 时需要清理 Gameplay 的飞船、玩家、锚点、漂浮物、航道进度和临时资源。建议第一版直接清空玩家会话，回到主菜单后重新进入 RoomPanel 再加入。

## 状态机设计

建议用一个轻量的 `GameFlowController` 管理界面循环，避免 UI 面板彼此直接跳转。

核心状态：

```text
MainMenu
Room
Gameplay
Pause
```

推荐职责：

- `GameFlowController`：管理状态切换、场景加载和全局流程。
- `MainMenuController`：处理主菜单按钮和确认输入。
- `RoomController`：管理 4 个玩家槽位、手柄接入和准备状态。
- `GameplayBootstrap`：根据玩家列表初始化 Gameplay。
- `PauseController`：处理暂停菜单、恢复和返回主菜单。
- `LocalPlayerSession`：保存一局游戏中的玩家槽位和设备绑定。
- `GameplayResult`：记录胜利、失败或主动退出，后续接结算界面。

## 推荐流程

### 启动到房间

```text
GameFlowController 进入 MainMenu
MainMenu 显示
玩家选择 Start
GameFlowController 切换到 Room
RoomController 清空旧槽位并开始监听手柄加入
```

### 房间到玩法

```text
手柄确认加入
RoomController 分配 P1-P4 槽位
玩家确认准备
满足开始条件
P1 或房主按开始键
RoomController 生成 LocalPlayerSession
GameFlowController 切换到 Gameplay
GameplayBootstrap 使用 LocalPlayerSession 初始化玩家、飞船、锚点和资源系统
```

### 玩法到暂停

```text
Gameplay 运行中
P1 或房主按暂停键
GameFlowController 切换到 Pause
暂停 Gameplay 输入和时间
PausePanel 显示
```

### 暂停返回主菜单

```text
玩家选择 Back To MainMenu
PauseController 请求返回主菜单
GameFlowController 清理 Gameplay 状态
LocalPlayerSession 失效
切换回 MainMenu
```

## 场景组织建议

第一版采用多场景流转：

- `MainMenu`：主菜单和房间界面。
- `Jaeger`：当前 Gameplay demo 场景。

UI 面板需要做成可编辑 Prefab，再由场景中的流程控制器引用：

- `MainMenuPanel`
- `RoomPanel`
- `GameplayRoot`
- `PausePanel`
- `EventSystem`
- `GameFlowController`

不要在 Runtime 临时拼接整套 UI。代码可以实例化已经制作好的 Prefab，也可以控制 Prefab 的显示隐藏，但面板层级、布局、按钮、文本、图标、美术资源和动画应在 Prefab 与 Inspector 中维护。只有 P1-P4 槽位状态、手柄设备名、准备状态、开始按钮可用状态这类依赖运行时数据的内容，才由脚本动态刷新。

Camera 采用场景本地方案：

- `MainMenu` 场景放自己的 `Main Camera`，用于避免 Game View 出现 “No cameras rendering”，也方便后续添加菜单背景。
- `Jaeger` 场景保留自己的 Gameplay Camera、Cinemachine 和灯光配置。
- 暂不做跨场景 Camera 单例。菜单相机和玩法相机职责不同，分开维护更直观。

跨场景 Manager 的原则：

- 可以存在常驻 Manager，但必须能处理关闭 Domain Reload 和 Scene Reload 后的残留问题。
- 静态字段、单例实例、事件订阅、缓存引用需要有清理和重置路径。
- 可用 `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` 重置静态状态。
- 对于当前 UIFlow，优先使用场景本地流程控制器，减少早期残留风险。

如果 Gameplay 内容复杂后，可以进一步拆出：

- `Boot`：持久管理器和输入系统。
- `MainMenu`：主菜单和房间界面。
- `Gameplay`：玩法场景。

拆 Boot 场景时，`GameFlowController` 和 `LocalPlayerSession` 可以放在 `Boot` 场景中持久存在，但需要按上面的残留风险规则实现。

## 架构选择

当前不建议立刻引入 QFramework。

原因是当前目标仍是玩法原型和 UIFlow demo，系统规模还不需要完整框架。现在更合适的做法是吸收 QFramework 的分层思想，但保持实现轻量：

- `System`：保存房间状态、玩家槽位、会话数据。
- `Controller`：处理 MainMenu、Room、Gameplay、Pause 的流程跳转。
- `View`：只引用 Prefab 里的按钮、文本、图标和槽位，并根据运行时数据刷新显示。

等输入、局内玩法、资源、结算、关卡、音频、存档等模块都变复杂后，再评估是否引入 QFramework 或自建更正式的 Service/Architecture 层。

## 输入系统建议

当前项目使用 Unity Input System 1.8.2。后续实现本地多人时，建议优先明确以下内容：

- 使用 `Gamepad` 设备作为主要输入来源。
- 统一定义 UI 操作：确认、取消、方向、开始、暂停。
- 房间界面维护 4 个固定玩家槽位。
- 玩家加入后，手柄设备与玩家槽位绑定。
- Gameplay 读取玩家槽位，不直接扫描所有手柄。

如果使用 `PlayerInputManager`，需要注意它适合自动加入玩家，但房间界面的 P1-P4 固定槽位、准备状态和重新绑定逻辑仍然建议由 `RoomController` 统一管理。

## UI Prefab 绑定建议

当前已为 UIFlow 生成这些可编辑 Prefab：

- `Assets/Prefabs/UI/MainMenuPanel.prefab`
- `Assets/Prefabs/UI/RoomPanel.prefab`
- `Assets/Prefabs/UI/PausePanel.prefab`

建议每个 Prefab 挂一个轻量 View 脚本，只暴露 Inspector 引用：

- `MainMenuPanelView`：引用 Start、Quit 按钮。
- `RoomPanelView`：引用 4 个 `RoomPlayerSlotView`、返回按钮、开始按钮、提示文本。
- `RoomPlayerSlotView`：引用 P1-P4 标题、设备名、状态文本、准备图标或色块。
- `PausePanelView`：引用 Resume、Restart、Back To MainMenu 按钮。

`GameFlowController` 只引用这些面板实例或 Prefab，不负责创建面板内部结构。美术资源、位置、字体、颜色和动效由 Prefab 调整。

当前已生成 `Assets/Scenes/MainMenu.unity` 作为流转入口，`Assets/Scenes/Jaeger.unity` 作为 Gameplay 场景。Build Settings 中 `MainMenu` 为第 0 个场景，`Jaeger` 为第 1 个场景。运行完整 demo 时从 `MainMenu` 开始。

`MainMenu` 场景中包含：

- `UIFlowRoot`：挂载 `GameFlowController`、`RoomInputManager`、`LocalPlayerSession`。
- `UIFlowCanvas`：作为 `UIFlowRoot` 子物体，放置三个面板 Prefab 实例。
- `Main Camera`：菜单场景本地相机，后续可挂菜单背景、后处理或动态背景。
- `EventSystem`：场景本地对象，不跟随 `UIFlowRoot` 常驻，避免进入 `Jaeger` 后和 Gameplay 场景的 EventSystem 重复。

`Jaeger` 场景中包含：

- Gameplay 原有 Camera 和场景对象。
- `GameplayUIFlowRoot`：挂载 `GameplaySceneFlowController`。
- `GameplayUIFlowCanvas`：放置 `PausePanel` Prefab 实例。
- 场景本地 `EventSystem`。

手柄房间流程：

- MainMenu：任意手柄按 South 或 Start 进入 Room。
- Room：手柄按 South 加入，再按 South 切换准备。
- Room：已加入玩家按 East 离开槽位。
- Room：P1/房主按 Start，且满足开始条件时进入 `Jaeger`。
- Gameplay：P1/房主按 Start 打开 PausePanel。
- PausePanel：按钮负责继续、重开或返回 MainMenu。

## 第一版实现清单

1. 创建 `GameFlowController`，支持 `MainMenu`、`Room`、`Gameplay`、`Pause` 四个状态。
2. 创建 `LocalPlayerSession` 数据结构，保存最多 4 个玩家槽位。
3. 创建 `RoomController`，实现手柄加入、离开、准备和开始条件判断。
4. 制作 `MainMenuPanel`、`RoomPanel`、`PausePanel` 三个 UI Prefab。
5. 创建 `PauseController`，支持继续游戏和返回主菜单。
6. 建立基础输入动作：确认、取消、方向、开始、暂停。
7. 在 `RoomPanel` 中制作 4 个玩家槽位 UI，脚本只刷新 P1-P4 状态。
8. 在 Unity 编辑器中验证 1-4 个手柄接入、准备、进入 Gameplay、暂停、返回主菜单的完整循环。

## 待玩法确认后补充

以下内容后续需要继续补充：

- 胜利、失败后的结算界面是否保留。
- Restart 是否保留上一轮玩家和手柄绑定。
- 手柄热插拔后的恢复策略。
- 局内 UI 的燃料、护盾、玩家携带资源和锚点异常状态展示。
