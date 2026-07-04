# 插件与包版本基线

记录日期：2026-07-04

这份文档记录当前 Unity 编辑器实际加载的关键插件和包版本。后续排查编译、输入、相机、UI 或工具链问题时，先对照这里，再决定是否升级或回退包版本。

## Unity 与编辑器连接

- Unity 版本：`2022.3.8f1`
- 项目路径：`E:/UnityProject/CiGa2026`
- 当前平台：`StandaloneWindows64`
- Unity-MCP 当前可用，连接实例为 `CiGa2026@710536a7994b95f4`
- 当前入口场景：`Assets/Scenes/MainMenu.unity`

## 关键包版本

| 包 | 当前实际加载版本 | 来源 | 当前用途 |
| --- | --- | --- | --- |
| `com.coplaydev.unity-mcp` | `10.0.0` | Git | Codex 通过 Unity-MCP 读取编辑器状态、Console、场景和包信息 |
| `com.unity.inputsystem` | `1.8.2` | Registry | 手柄接入、房间加入、玩家输入绑定 |
| `com.unity.cinemachine` | `3.1.7` | Registry | Gameplay 相机与 Cinemachine Brain |
| `com.unity.render-pipelines.universal` | `14.0.8` | BuiltIn | URP、2D 灯光、渲染管线 |
| `com.unity.ugui` | `1.0.0` | BuiltIn | 当前 UIFlow 的 Canvas、Button、EventSystem |
| `com.unity.textmeshpro` | `3.0.6` | Registry | UI 文本 |
| `com.unity.probuilder` | `5.2.4` | Registry | 后续可能用于快速搭建关卡几何 |
| `com.unity.test-framework` | `1.1.33` | Registry | 后续编辑器测试与 PlayMode 测试 |
| `com.singularitygroup.hotreload` | `1.13.19` | Embedded | 热重载插件，实际加载但不在 manifest 依赖列表中 |
| `com.boxqkrtm.ide.cursor` | `2.0.28` | Git | Cursor 编辑器集成 |

## 必须保留的前置条件

`com.unity.inputsystem` 当前必须保持在 `1.8.2` 或兼容版本。此前项目出现过 Cinemachine 3.1.7 编译错误：

```text
CinemachineInputAxisController.cs
error CS1061: 'InputAction' does not contain a definition for 'activeValueType'
```

已确认根因是 `com.unity.cinemachine@3.1.7` 与旧版 `com.unity.inputsystem@1.6.3` 不兼容。当前修复方式是把 Input System 升到 `1.8.2`，并删除旧的 `Library/PackageCache/com.unity.inputsystem@1.6.3` 缓存。

后续不要把 `com.unity.inputsystem` 回退到 `1.6.3`。如果必须改版本，需要同步验证 Cinemachine 编译状态和 Console。

## 当前 UIFlow 依赖

当前完整流转 demo 依赖以下包和插件：

- Unity-MCP：编辑器自动化、场景检查、Console 检查。
- Input System：房间手柄加入、准备、开始和 Gameplay 玩家输入绑定。
- uGUI + TextMeshPro：`MainMenuPanel`、`RoomPanel`、`PausePanel`。
- Cinemachine + URP：`Jaeger` Gameplay 场景相机和 2D 渲染环境。

当前玩家生成方案还依赖：

- `Assets/Prefabs/Gameplay/Player.prefab`
- `GameplaySessionStore`
- `GameplayPlayerSpawner`
- `GamepadPlayerInput.SetGamepadIndex`

## 检查方式

每次遇到编译或工具链问题，建议按这个顺序检查：

1. 用 Unity-MCP 读取 `editor state`，确认当前连接实例是 `CiGa2026`，且编辑器没有在编译或 Play Mode 切换中。
2. 用 Unity-MCP 读取 Console errors。
3. 用 Unity Package Manager 或 Unity-MCP `list_packages` 确认关键包版本。
4. 再检查 `Packages/manifest.json` 和 `Packages/packages-lock.json` 是否与实际加载版本一致。

当前记录时，Unity Console 没有 error。
