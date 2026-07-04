# Gameplay 玩家生成方案

## 目标

Gameplay 里的玩家对象必须根据房间界面的真实加入结果动态生成。场景里不长期维护 P1、P2、P3、P4 玩家本体，玩家本体统一来自可编辑的 Prefab。

当前玩家 Prefab 路径：

- `Assets/Prefabs/Gameplay/Player.prefab`

## 数据流

```text
RoomInputManager
-> RoomPlayerSlot
-> GameplaySessionStore
-> GameplayPlayerSpawner
-> Player.prefab 实例
```

房间界面负责记录最多 4 个玩家槽位，包括玩家编号、设备 ID、设备在 `Gamepad.all` 中的索引、设备名和准备状态。

进入 Gameplay 前，`GameFlowController` 调用 `GameplaySessionStore.Capture(roomInputManager)`，只保存已加入且已准备的玩家。`Jaeger` 场景加载后，`GameplayPlayerSpawner` 读取这份会话数据，按顺序生成玩家。

## 场景结构

`Jaeger` 场景中保留这些对象：

- `GameplayUIFlowRoot`：挂载 `GameplaySceneFlowController` 和 `GameplayPlayerSpawner`。
- `GameplayPlayerSpawnRoot`：放置 `P1SpawnPoint` 到 `P4SpawnPoint`。
- `SpawnedPlayers`：运行时生成玩家的父级，方便检查层级。

`Players` 旧根节点可以暂时保留，但不再放置固定玩家实例。后续若没有其他脚本依赖它，可以再清理。

## Prefab 约定

`Player.prefab` 是后续玩家美术和碰撞调试的主要入口。它需要保留以下组件：

- `PlayerController`
- `PlayerOperateInteractor2D`
- `KeyboardPlayerInput`
- `GamepadPlayerInput`
- `Rigidbody2D`
- 至少一个 `Collider2D`

手柄玩家生成后，`GameplayPlayerSpawner` 会启用 `GamepadPlayerInput`，调用 `SetGamepadIndex` 绑定具体手柄，并把该输入组件写入 `PlayerController` 和 `PlayerOperateInteractor2D`。

没有房间会话时，Spawner 可以生成一个键盘调试玩家，方便编辑器里做静态检查和轻量调试。正式体验完整 UIFlow 时，应从 `MainMenu` 进入房间，再由房间进入 `Jaeger`。

## 关闭 Domain Reload 的注意事项

`GameplaySessionStore` 使用 `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` 清理静态会话数据，降低关闭 Domain Reload 后旧会话残留的风险。

以后如果给玩家生成系统增加缓存、事件订阅或跨场景管理器，也必须提供明确的清理路径，避免下一轮进入 Gameplay 时复用旧引用。
