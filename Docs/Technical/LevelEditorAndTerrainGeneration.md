# 关卡编辑器与程序轮廓地形方案

## 目标

本方案描述 CiGa2026 的第一版关卡编辑器和运行时地图生成流程。

关卡编辑器从 `MainMenu` 进入，作为主流程旁路功能存在。玩家可以在一块实心长方形地形中，用类似挖掘的画笔挖出道路和隧道，再放置飞船起点、终点和后续扩展要素。保存后的地图由 `Jaeger` 场景读取，并生成真实可见地形、墙体碰撞、起点、终点和玩法 Marker。

当前主流程按下面的关系设计：

```text
MainMenu
-> Room / ChooseCharacter
-> Jaeger
```

关卡编辑器接在 `MainMenu` 的旁路：

```text
MainMenu
-> LevelEditor
-> 保存 / 试玩 / 返回
```

点击试玩时，编辑器先保存当前地图，再写入当前选择地图，然后进入 `ChooseCharacter`。玩家确认角色后进入 `Jaeger`，`Jaeger` 从当前选择地图生成关卡。

## 核心取舍

最终地图不使用可见 Tilemap。

Tilemap 适合快速搭建格子关卡，但本项目需要的是在实心长方形中挖出自然航道。可见地形如果直接用 Tilemap，边缘会天然带四方格感，不符合“挖掘道路和隧道”的目标。

第一版采用程序轮廓地形：

```text
实心长方形密度图
-> 画笔降低局部密度
-> 得到空洞和墙体数据
-> Marching Squares 提取轮廓
-> 生成可见 Mesh
-> 生成 2D 碰撞轮廓
```

Tilemap 可以作为内部调试显示，但不作为最终视觉和碰撞来源。

## 数据模型

关卡保存为 `LevelTerrainData`。它描述地图尺寸、采样精度、密度图、起点、终点和扩展 Marker。

建议目录：

```text
Assets/Scripts/Levels/Data/
  LevelTerrainData.cs
  LevelMarkerData.cs
  LevelMarkerType.cs
```

建议字段：

```csharp
[Serializable]
public sealed class LevelTerrainData
{
    public int version;
    public string id;
    public string displayName;
    public int width;
    public int height;
    public int samplesPerUnit;
    public string densitiesBase64;
    public LevelPoseData start;
    public LevelPoseData finish;
    public List<LevelMarkerData> markers;
}

[Serializable]
public struct LevelPoseData
{
    public float x;
    public float y;
    public float angle;
}

[Serializable]
public sealed class LevelMarkerData
{
    public string id;
    public LevelMarkerType type;
    public float x;
    public float y;
    public float angle;
    public float radius;
}
```

密度图使用 `byte[]`：

- `255` 表示完整实心墙体。
- `0` 表示完全挖空道路。
- 中间值用于软边、自然洞壁和后续视觉细节。

保存时把 `byte[]` 转成 Base64，写入 `densitiesBase64`。这样文件比直接保存数组更短，也方便用 Unity 的 `JsonUtility` 处理。

第一版 JSON 示例：

```json
{
  "version": 1,
  "id": "default_level",
  "displayName": "Default Level",
  "width": 256,
  "height": 128,
  "samplesPerUnit": 4,
  "densitiesBase64": "...",
  "start": { "x": 16, "y": 64, "angle": 0 },
  "finish": { "x": 236, "y": 64, "angle": 0 },
  "markers": []
}
```

## 文件保存

内置地图放在：

```text
Assets/StreamingAssets/Levels/
```

玩家保存地图放在：

```text
Application.persistentDataPath/Levels/
```

建议新增：

```text
Assets/Scripts/Levels/Runtime/
  LevelFileService.cs
  SelectedLevelStore.cs
```

`LevelFileService` 负责：

- 创建默认地图。
- 从 StreamingAssets 读取内置地图。
- 从 persistentDataPath 读取玩家地图。
- 保存玩家地图。
- 生成地图列表。
- 处理重复文件名和非法文件名。

`SelectedLevelStore` 负责：

- 保存当前选择地图路径。
- 保存试玩时的临时地图路径。
- 在关闭 Domain Reload 后提供静态清理路径。

`SelectedLevelStore` 必须使用 `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` 清理静态状态，避免多次从编辑器进入 Play Mode 后复用旧地图。

## 编辑器界面

关卡编辑器是运行时 UI，不是 Unity Editor 扩展。

建议新增：

```text
Assets/Prefabs/UI/LevelEditorPanel.prefab

Assets/Scripts/Levels/EditorRuntime/
  LevelEditorController.cs
  LevelEditorPanelView.cs
  LevelBrushTool.cs
  LevelEditorCameraController.cs
  LevelValidationService.cs
```

UI 结构、布局、按钮、文本、图标和动画继续放在 Prefab 与 Inspector 中维护。Runtime 脚本只负责状态切换、按钮回调、地图列表刷新、工具状态刷新和错误提示。

第一版工具：

- `Dig`：挖空地形。
- `Fill`：填回地形。
- `PlaceStart`：放置飞船起点。
- `PlaceFinish`：放置终点。
- `SelectMarker`：选择、移动或删除 Marker。

第一版按钮：

- 新建地图。
- 加载地图。
- 保存地图。
- 另存为。
- 验证地图。
- 试玩地图。
- 返回 MainMenu。

第一版画笔参数：

- 半径。
- 硬度。
- 强度。
- 边缘噪声。
- 是否显示轮廓预览。

输入方式先以鼠标键盘为主：

- 鼠标左键绘制。
- 鼠标右键临时执行反向操作。
- 鼠标滚轮缩放。
- 中键或空格拖动画布。

手柄编辑可以后续再接，不进入第一版必要范围。

## 画笔挖掘

地图初始时是一整块实心矩形，所有密度值为 `255`。画笔拖动时，按固定间距连续盖章，避免鼠标移动太快导致道路断开。

挖掘逻辑：

```text
采样点到画笔中心的距离越近，密度下降越多。
画笔边缘按 hardness 形成过渡。
noise 用来给边缘增加轻微随机扰动。
密度低于 solidThreshold 的区域视为道路。
```

填回逻辑与挖掘相反，提高局部密度。

建议第一版参数：

```text
solidThreshold = 128
defaultBrushRadius = 4 world units
defaultBrushHardness = 0.75
defaultBrushStrength = 1.0
stampSpacing = brushRadius * 0.35
```

起点和终点必须放在挖空区域。放置时如果点击在实心区域，应给出提示，不自动挖空，避免误改地图。

## 程序轮廓生成

建议新增：

```text
Assets/Scripts/Levels/Runtime/
  LevelTerrainMesher.cs
  LevelTerrainRenderer2D.cs
  LevelTerrainColliderBuilder.cs
  LevelRuntimeBootstrap.cs
```

`LevelTerrainMesher` 使用 Marching Squares 从密度图提取边界，并生成可见 Mesh。Mesh 用于岩体或墙体视觉表现。

`LevelTerrainColliderBuilder` 从同一份密度图提取轮廓，生成 2D 碰撞路径。第一版可以使用多个 `EdgeCollider2D` 表达墙体边界，后续如果需要闭合填充或更复杂碰撞，再评估 `PolygonCollider2D` 或自定义轮廓合并。

碰撞对象统一放到 `Wall` 层。

这样可以保持现有玩法约定：

- 飞船撞到 `Wall` 会受到阻挡或伤害。
- 锚的可命中层继续包含 `Wall`。
- 漂浮物仍使用 `FloatingItem` 层。
- 船内阻挡仍使用 `SubmarineInterior` 层，不参与外部地形。

生成对象建议挂在：

```text
GeneratedLevelRoot
  TerrainVisual
  TerrainColliders
  Markers
  FinishZone
```

## Jaeger 接入

`Jaeger` 场景启动后，先由 `LevelRuntimeBootstrap` 读取地图并生成关卡，再让玩家生成系统使用最终出生点。

推荐启动流程：

```text
LevelRuntimeBootstrap
-> 读取 SelectedLevelStore
-> 没有选择则读取 default_level
-> 生成 GeneratedLevelRoot
-> 生成地形 Mesh
-> 生成 Wall 层碰撞
-> 移动 Submarine 到 start
-> 移动 GameplayPlayerSpawnRoot 或船内出生点参考
-> 移动 FinishZone2D 到 finish
-> GameplayPlayerSpawner 生成玩家
```

需要注意执行顺序。玩家生成必须发生在飞船和船内出生点定位之后，否则玩家可能生成在旧位置。

推荐做法：

- `LevelRuntimeBootstrap` 暴露 `LevelReady` 事件。
- `GameplayPlayerSpawner` 增加可选等待模式，等 `LevelReady` 后再生成。
- 如果场景没有 `LevelRuntimeBootstrap`，`GameplayPlayerSpawner` 保持现有 fallback 行为，方便旧场景调试。

## 起点和终点语义

起点表示飞船进入关卡的位置，不直接表示每个玩家的世界出生点。

当前项目里玩家本体由 `GameplayPlayerSpawner` 动态生成，房间和选角只保存玩家设备与角色。关卡起点应先移动飞船本体，再通过已有船内出生点生成玩家。

终点使用现有 `FinishZone2D` 思路。运行时把终点 Trigger 移动到地图终点位置，并继续通知 `MissionSettlementController.Win()`。

## Marker 扩展

第一版 Marker 只需要保留数据结构，必要时先实现起点和终点。

后续可扩展：

- 漂浮物刷新区。
- 危险物刷新区。
- 固定障碍物。
- 资源提示点。
- 装饰点。
- 教学提示点。

Marker 不应该直接写死成场景物体。运行时由 `LevelRuntimeBootstrap` 根据 Marker 类型实例化对应 Prefab 或配置对应生成器。

## 验证规则

保存和试玩前必须验证地图。第一版至少检查：

1. 起点存在。
2. 终点存在。
3. 起点和终点都在挖空区域。
4. 起点到终点连通。
5. 道路最小宽度满足飞船通行需求。
6. 地图外边缘保持实心，避免飞船飞出有效区域。
7. 生成出的轮廓数量和点数在合理范围内。
8. `Wall` 层存在。
9. `Wall` 层包含在锚点命中检测中。
10. `Wall` 层与飞船碰撞关系符合当前 Physics2D 设计。

连通性检查可以先在密度图上做 BFS。道路宽度检查第一版可以用简化规则：起点到终点路径上的空洞区域，至少能容纳一个配置半径。后续再改成距离场或更精确的通行半径检测。

## 实施顺序

### 第一阶段：数据和默认地图

新增数据结构、保存加载服务和默认实心地图生成。先不做复杂 UI，只要能在代码里创建、保存、读取一张地图。

产出：

- `LevelTerrainData`
- `LevelFileService`
- `SelectedLevelStore`
- `default_level.json`

### 第二阶段：Jaeger 运行时生成

让 `Jaeger` 能从一份测试地图生成可见地形和墙体碰撞。先手写一份带隧道的测试数据，确认飞船和锚能与程序轮廓交互。

产出：

- `LevelRuntimeBootstrap`
- `LevelTerrainMesher`
- `LevelTerrainRenderer2D`
- `LevelTerrainColliderBuilder`

### 第三阶段：运行时关卡编辑器

制作 `LevelEditorPanel.prefab`，支持挖空、填墙、放起点、放终点、保存和加载。

产出：

- `LevelEditorPanelView`
- `LevelEditorController`
- `LevelBrushTool`
- `LevelEditorCameraController`

### 第四阶段：主流程接入

从 `MainMenu` 增加进入关卡编辑器按钮。试玩时进入 `ChooseCharacter`，选角完成后进入 `Jaeger`。

产出：

- `MainMenuPanel` 增加 Level Editor 入口。
- `GameFlowController` 或独立流程控制器增加进入编辑器逻辑。
- `ChooseCharacterController` 保持玩家选择职责，不直接管理地图。

### 第五阶段：验证和提示

补齐地图验证、错误提示、保存前确认、覆盖确认、地图列表刷新。

产出：

- `LevelValidationService`
- 编辑器错误提示 UI
- 保存与试玩前的验证流程

## 第一版验收标准

从 `MainMenu` 进入关卡编辑器后，可以：

1. 新建一张实心长方形地图。
2. 用圆形画笔挖出自然隧道。
3. 用填墙工具修补地形。
4. 放置飞船起点。
5. 放置终点。
6. 保存地图。
7. 加载已保存地图。
8. 点击试玩进入 `ChooseCharacter`。
9. 选角后进入 `Jaeger`。
10. `Jaeger` 按地图数据生成程序轮廓地形。
11. 飞船出现在起点。
12. 锚能抓程序墙体。
13. 飞船能与墙体碰撞。
14. 抵达终点后触发胜利。

## 注意事项

- 不要把关卡编辑器 UI 做成运行时拼装的完整层级，面板仍然要做成 Prefab。
- 不要让 `ChooseCharacterController` 负责地图逻辑，它只负责玩家加入、角色选择和写入玩家会话。
- 不要把起点理解成玩家世界出生点。起点是飞船出生点，玩家仍从飞船内部出生点动态生成。
- 不要让程序墙体使用 `SubmarineInterior` 层。外部地形应使用 `Wall` 层。
- 新增碰撞前先确认 Physics2D 碰撞矩阵，避免飞船、船内墙体和外部地形互相干扰。
- 第一版先保证编辑、保存、加载、运行时生成闭环成立。随机生成、复杂装饰和高级刷怪规则放到后续阶段。
