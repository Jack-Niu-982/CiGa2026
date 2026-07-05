# 角色选择流程

## 场景与 Prefab

- 角色选择场景：`Assets/Scenes/ChooseCharactor.unity`
- 可编辑 UI Prefab：`Assets/Prefabs/UI/ChooseCharacterPanel.prefab`
- 场景只保留自己的 Camera 和上述 UI Prefab 实例，不依赖其他场景里的 UI 对象。

## 玩家加入与编号

角色选择界面直接读取 Unity Input System 的 `Gamepad.all`。

1. 未加入的手柄按 South 加入。
2. 加入顺序决定玩家编号：第一个是 P1，第二个是 P2，最多到 P4。
3. 未确认时按 East 退出；已确认时按 East 取消确认。
4. 手柄断开后对应玩家会被移除，剩余玩家按原加入顺序重新紧凑排列。

## 角色选择

- 左摇杆或十字键左右切换角色。
- South 确认当前角色。
- 已确认的角色不能被其他玩家再次确认。
- 所有已加入玩家确认后，由 P1 按 North 进入 `Jaeger`。
- 玩家指向或确认某张角色卡时，卡片使用 `Assets/res/anim/cat1` 到 `cat4` 下的 `rotate` 图片播放 UI 逐帧动画。

## Gameplay 数据

`GameplayPlayerAssignment` 同时保存两种编号：

- `SlotIndex`：由手柄按 South 的加入顺序决定，对应 P1-P4。
- `CharacterIndex`：玩家在角色选择界面选择的外貌编号。

进入 Gameplay 前，`ChooseCharacterController` 把结果写入 `GameplaySessionStore`。`GameplayPlayerSpawner` 使用 `SlotIndex` 设置玩家名称和身份，使用 `CharacterIndex` 为 `PlayerAnimatorControllerSelector` 选择对应角色动画。两者互不绑定，因此 P1 可以选择任意一个角色外貌。

`GameplayHudPanel` 的玩家 Portrait 继续读取 `GameplayPlayerIdentity.PortraitSprite`。玩家生成时会根据 `CharacterIndex` 注入对应 Cat 的 `cat编号_rotate_002`，因此 HUD 头像与角色选择结果保持一致。

## 当前操作提示

```text
South：加入 / 确认
左摇杆或十字键左右：选择角色
East：取消确认 / 退出
P1 North：全部确认后进入 Gameplay（Xbox Y / PlayStation △ / Switch X）
```
