# 多 Agent 并行开发工作流

本文档记录 CiGa2026 后续使用 Codex 多 agent 并行开发时的推荐流程。目标是提升开发效率，同时避免 Unity 场景、Prefab 和核心脚本互相覆盖。

## 适用场景

适合并行拆分的任务通常满足这些条件：

- 目标已经在主对话里讨论清楚，接口边界比较明确。
- 子任务之间写入文件范围不同，合并冲突风险低。
- 每个子任务能独立完成一部分可验收成果。
- 主对话可以在最后统一编译、检查 Console、检查 Prefab 和文档。

典型例子：

- 一个 agent 写运行时脚本，另一个 agent 写 Editor Builder 和文档。
- 一个 agent 做 UI Prefab 绑定方案，另一个 agent 做输入或数据结构。
- 一个 agent 做玩法系统雏形，另一个 agent 做测试清单、文档和资产生成器。

不适合并行的任务：

- 多个 agent 同时改同一个 Unity Scene。
- 多个 agent 同时改同一个 Prefab。
- 多个 agent 同时改 `PlayerController`、`GameFlowController`、`OperateController` 这类核心脚本。
- 需求还没定清楚，接口和职责随时会变。

## 标准流程

### 1. 主对话先定方案

主对话负责先把需求拆清楚，包括：

- 本轮目标是什么。
- 哪些内容明确不做。
- 关键接口怎么命名。
- 数据流怎么走。
- 允许修改哪些文件。
- 禁止修改哪些文件。
- 是否允许进入 Play Mode。

没有这些前置约定时，不要急着开多个 agent。

### 2. 拆成低重叠任务

每个子 agent 都应该拿到一个清晰、独立的任务包。

任务包需要写明：

- 任务名称。
- 允许写入范围。
- 禁止写入范围。
- 预期交付物。
- 需要遵守的项目规范。
- 验证边界。

示例：

```text
任务：实现拾取物运行时系统

允许修改：
- Assets/Scripts/Gameplay/Pickups/*.cs

禁止修改：
- Assets/Scenes/*
- Assets/Prefabs/*
- Packages/*
- ProjectSettings/*

要求：
- 玩家靠近物品后按 Interact 拾取。
- 手持状态再次按 Interact 放下。
- 不接燃料数值和护盾效果。
- 不进入 Play Mode。
```

### 3. 子 Agent 并行实现

子 agent 执行时需要遵守：

- 不要回滚别人已经做的改动。
- 不要扩大写入范围。
- 不要为了方便临时改场景、Prefab 或 ProjectSettings。
- 新文件使用 UTF-8。
- 如果遇到边界不清的地方，优先保守实现，并在汇报里说明。

每个子 agent 完成后要汇报：

- 改了哪些文件。
- 核心 API 或菜单入口是什么。
- 做了哪些验证。
- 没有验证什么。
- 需要主对话最后补哪些集成点。

### 4. 主对话集中验收

主对话收到子任务后统一做集成，而不是让子 agent 互相补丁。

集中验收至少包括：

- 读新增代码，检查接口命名和职责是否一致。
- 检查是否误改了禁止范围。
- 用 Unity-MCP 做脚本静态校验。
- 刷新 Unity 编译，并读取 Console errors。
- 检查 Prefab 组件、字段和引用。
- 必要时执行 Editor 菜单生成资产。
- 同步更新 README、Docs 索引和相关技术文档。

除非用户明确允许，不进入 Play Mode。

### 5. 最后统一提交

提交前需要：

- `git status --short` 确认工作区范围。
- `git diff --stat` 看改动是否符合本轮任务。
- 确认 Unity Console 没有项目脚本 error。
- 确认当前场景回到适合继续工作的入口，例如 `MainMenu`。

提交信息应该描述本轮集成结果，而不是只写某个子 agent 的子任务。

## Unity 项目特别注意

### Scene 和 Prefab

Unity 的 Scene 和 Prefab 是 YAML 文件，容易产生大范围 diff。并行时尽量让主对话统一改这些文件。

推荐做法：

- 子 agent 可以写 Editor Builder。
- 主对话执行 Builder 生成或更新 Prefab。
- 主对话负责检查 Prefab 组件和序列化字段。

### Runtime 与 Editor 分离

优先把任务拆成：

- Runtime 脚本。
- Editor 生成器。
- 文档。
- Prefab/Scene 绑定。

这样每个方向文件范围更清楚，冲突少。

### 输入和交互优先级

多个系统共用同一个输入键时，主对话必须统一定优先级。

例如本轮拾取系统中：

- `PlayerOperateInteractor2D` 负责锚点和设施操作。
- `PlayerCarryInteractor2D` 负责拾取和放下。
- 玩家正在操作设施时，拾取系统暂停响应 `InteractPressed`，避免抢输入。

### 编码

所有新增脚本和文档都使用 UTF-8。项目根目录已有 `.editorconfig`，用于提醒编辑器保持统一编码。

## 本轮实践记录

第一次并行试运行拆成两条线：

- 运行时拾取/手持系统：`Assets/Scripts/Gameplay/Pickups/`
- 漂浮物 Prefab 生成器与文档：`Assets/Scripts/Editor/FloatingItemPrefabBuilder.cs` 和 `Docs/Technical/PickupAndFloatingItems.md`

主对话最后完成：

- 给 `Player.prefab` 接入 `PlayerCarryInteractor2D` 和 `CarryHoldPoint`。
- 执行 `CiGa2026/Build Floating Item Prefabs`。
- 检查 `FuelPickup`、`TrashPickup`、`ShieldPickup` 的类型、Trigger 和 Rigidbody2D。
- 确认 Unity Console 没有项目脚本 error。
