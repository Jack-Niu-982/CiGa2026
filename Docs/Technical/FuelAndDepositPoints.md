# Fuel 与物品投入点

## 数值系统

- `SubmarineHealth2D` 继续维护 Health。
- `SubmarineFuel2D` 维护 Fuel，默认最大值 100、初始值 50，并通过 `FuelChanged` 同步 UI。
- `SubmarineFuelBar.prefab` 使用只读 Scrollbar 显示 Fuel 比例，运行时由 `SubmarineFuelGui` 刷新。

## 投入点 Prefab

- `Assets/Prefabs/Gameplay/Stations/ShieldPutInPoint.prefab`
  - 只接收 `ShieldPickUpInRoom`。
  - 默认增加 25 Health。
- `Assets/Prefabs/Gameplay/Stations/FuelPutInPoint.prefab`
  - 只接收 `FuelPickUpInRoom`。
  - 默认增加 25 Fuel。

两个投入点都使用 `CarryItemDepositPoint2D`。增加量保存在 Prefab 的 `Restore Amount` 字段中，可直接在 Inspector 调整。只有数值未满、玩家持有正确物品且位于 Trigger 内时，PutIn 才可用。

## 输入与提示

- 键盘默认 `G`。
- 手柄默认 `West`。
- `PlayerPutInInteractor2D` 读取独立的 `PutInPressed`。
- 玩家 `WorldSpaceCanvas` 按当前输入设备显示 `[G]PutIn` 或 `[West]PutIn`。
- PickUp 与 PutIn 默认共用手柄 West；位于有效投入点时优先 PutIn，避免先放下物品。

## 运行闭环

1. 玩家拾取 Fuel 或 Shield。
2. 玩家进入对应投入点 Trigger。
3. 世界空间提示显示 PutIn。
4. 玩家按 PutIn，投入点增加 Prefab 中配置的数值。
5. 手持物被消费，玩家手持显示和 HUD HeldItem 同步清空。

## 船锚消耗

- 每次船锚成功发射默认消耗 5 Fuel。
- 消耗值保存在 `AnchorLauncher.prefab` 的 `Fuel Cost Per Shot` 字段中，可在 Inspector 调整。
- Fuel 不足本次消耗时拒绝发射，不扣除剩余 Fuel。
