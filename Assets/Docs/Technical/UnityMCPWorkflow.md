# Unity-MCP 工作流程示例

本文档展示如何使用 Unity-MCP 工具在场景中创建对象、配置组件和制作 Prefab，以屏幕外终点指示器为例。

## 为什么使用 Unity-MCP

当实现新的 Unity 功能时，不应该只提供脚本让用户手动搭建，而应该：

1. **直接创建可用实例**：通过 Unity-MCP 在场景中创建完整的可用对象
2. **制作 Prefab**：将配置好的对象保存为 Prefab 供后续复用
3. **提供编辑器工具**：作为快速创建的辅助手段
4. **补充文档说明**：记录手动创建步骤作为备选方案

这样用户可以直接看到效果，而不是面对一堆脚本和文档说明却不知道如何组装。

## 示例：创建屏幕外终点指示器

### 第一步：检查场景状态

```bash
# 检查当前打开的场景
ReadMcpResourceTool(server="UnityMCP", uri="mcpforunity://editor/state")

# 查找场景中是否已有 Canvas
# 使用 find_gameobjects 或 manage_scene 工具
```

### 第二步：创建 UI Canvas（如果不存在）

使用 Unity-MCP 工具创建 Canvas：

```python
# 伪代码示例，实际使用 MCP 工具
manage_scene(
    action="create",
    objectType="Canvas",
    name="GameplayCanvas"
)

# 配置 Canvas 组件
manage_component(
    target="GameplayCanvas",
    action="add",
    componentType="CanvasScaler"
)
```

### 第三步：创建指示器对象层级

```python
# 创建指示器容器
manage_scene(
    action="create",
    objectType="GameObject",
    name="OffScreenIndicator",
    parent="GameplayCanvas"
)

# 添加主控制脚本
manage_component(
    target="OffScreenIndicator",
    action="add",
    componentType="OffScreenTargetIndicator"
)

# 创建箭头图标子对象
manage_scene(
    action="create",
    objectType="Image",  # UI Image
    name="ArrowIcon",
    parent="OffScreenIndicator"
)

# 创建距离文本子对象
manage_scene(
    action="create",
    objectType="Text",
    name="DistanceText",
    parent="ArrowIcon"
)
```

### 第四步：配置组件参数

```python
# 配置 RectTransform
set_property(
    target="ArrowIcon",
    component="RectTransform",
    property="sizeDelta",
    value={"x": 50, "y": 50}
)

# 配置 Image 颜色
set_property(
    target="ArrowIcon",
    component="Image",
    property="color",
    value={"r": 1, "g": 1, "b": 0, "a": 1}  # 黄色
)

# 连接引用
set_property(
    target="OffScreenIndicator",
    component="OffScreenTargetIndicator",
    property="arrowIcon",
    value="ArrowIcon"  # 引用
)

set_property(
    target="OffScreenIndicator",
    component="OffScreenTargetIndicator",
    property="distanceText",
    value="DistanceText"  # 引用
)
```

### 第五步：连接目标对象

```python
# 查找场景中的终点
find_gameobjects(
    name="FinishZone",
    type="FinishZone2D"
)

# 连接目标引用
set_property(
    target="OffScreenIndicator",
    component="OffScreenTargetIndicator",
    property="target",
    value="FinishZone"  # 引用终点 Transform
)
```

### 第六步：保存为 Prefab

```python
# 将场景对象保存为 Prefab
manage_asset(
    action="create_prefab",
    source="OffScreenIndicator",
    path="Assets/Prefabs/UI/OffScreenTargetIndicator.prefab"
)
```

### 第七步：测试和验证

```python
# 进入 Play Mode 测试
manage_editor(
    action="enter_play_mode"
)

# 检查控制台错误
read_console(logType="Error")

# 退出 Play Mode
manage_editor(
    action="exit_play_mode"
)
```

## 实际工具调用示例

由于 Unity-MCP 的工具名称和参数可能有所不同，以下是参考 MCP 文档后的实际调用方式：

### 查看可用工具

```bash
ListMcpResourcesTool(server="UnityMCP")
```

### 读取编辑器状态

```bash
ReadMcpResourceTool(
    server="UnityMCP",
    uri="mcpforunity://editor/state"
)
```

### 其他常用操作

- 查找 GameObject：使用相关查找工具
- 创建对象：使用场景管理工具
- 添加组件：使用组件管理工具
- 设置属性：使用属性设置工具
- 保存 Prefab：使用资产管理工具

## 补充：提供编辑器菜单工具

即使通过 Unity-MCP 创建了实例，也应该提供编辑器菜单工具方便后续快速创建：

```csharp
// Assets/Scripts/Editor/OffScreenIndicatorSetup.cs
[MenuItem("GameObject/UI/Off-Screen Target Indicator")]
public static void CreateOffScreenIndicator()
{
    // 创建完整的 UI 层级
    // 参考编辑器工具脚本的实现
}

[MenuItem("GameObject/UI/Find Finish Zone and Setup Indicator")]
public static void AutoSetupIndicatorForFinishZone()
{
    // 自动查找终点并设置
}
```

## 文档说明作为备选

在技术文档中补充手动创建步骤，以防 Unity-MCP 不可用或用户偏好手动操作：

```markdown
## 手动创建步骤

1. 右键 Hierarchy，选择 UI > Canvas
2. 在 Canvas 下创建空对象，命名为 OffScreenIndicator
3. 添加 OffScreenTargetIndicator 组件
4. 创建子对象 ArrowIcon，添加 Image 组件
5. 创建孙对象 DistanceText，添加 Text 组件
6. 在 Inspector 中连接引用
7. 拖拽 FinishZone 到 Target 字段
```

## 总结

**推荐工作流程**：
1. ✅ 使用 Unity-MCP 直接在场景中创建可用实例
2. ✅ 保存为 Prefab 供复用
3. ✅ 提供编辑器菜单工具作为快速创建方式
4. ✅ 补充文档说明手动创建步骤

**不推荐的做法**：
1. ❌ 只提供脚本，让用户自己搭建整个对象层级
2. ❌ 只提供文档说明，没有实际可用的示例
3. ❌ 忽略 Unity-MCP 工具，完全依赖用户手动操作

通过这种方式，实现的功能是**开箱即用**的，而不是需要用户花时间理解和组装的半成品。
