# 屏幕外终点指示器使用说明

## 功能概述

当终点（FinishZone）在屏幕外时，会在屏幕边缘显示一个箭头，指向终点的方向，帮助玩家找到目标位置。

## 快速设置（推荐）

### 方法一：自动设置（最简单）

1. 确保场景中已有 `FinishZone2D` 组件的终点对象
2. 在 Unity 菜单栏选择：`GameObject > UI > Find Finish Zone and Setup Indicator`
3. 系统会自动创建指示器并连接到终点

### 方法二：手动创建

1. 在 Unity 菜单栏选择：`GameObject > UI > Off-Screen Target Indicator`
2. 在 Inspector 中找到 `OffScreenTargetIndicator` 组件
3. 将终点对象拖拽到 `Target` 字段

## 组件说明

### OffScreenTargetIndicator

这是主要的控制脚本，负责检测目标是否在屏幕外并更新箭头位置。

#### Inspector 参数

**目标设置**
- `Target`：要追踪的目标对象（通常是 FinishZone）

**UI 设置**
- `Arrow Icon`：箭头图标的 RectTransform（自动创建时已连接）
- `Edge Offset`：箭头距离屏幕边缘的偏移量（像素），默认 80

**相机设置**
- `Target Camera`：用于检测的相机，留空则使用主相机

**可选设置**
- `Show Distance`：是否显示距离文本，默认开启
- `Distance Text`：距离文本组件（自动创建时已连接）
- `Distance Unit`：距离单位名称，默认 "m"

## 自定义箭头图标

系统会创建一个临时的白色三角形箭头。建议替换为美术提供的箭头图标：

1. 选中场景中的 `OffScreenIndicator > ArrowIcon` 对象
2. 在 Inspector 中找到 `Image` 组件
3. 将自定义箭头 Sprite 拖拽到 `Source Image` 字段
4. 调整 `Color` 以匹配游戏风格
5. 调整 `RectTransform` 的 `Size` 以设置箭头大小

### 箭头图标要求

- **方向**：箭头应朝上（默认朝向），脚本会自动旋转指向目标
- **格式**：PNG 格式，带透明通道
- **建议尺寸**：64x64 或 128x128 像素
- **Texture Type**：Sprite (2D and UI)

## 高级用法

### 运行时控制

```csharp
// 获取指示器组件
OffScreenTargetIndicator indicator = FindObjectOfType<OffScreenTargetIndicator>();

// 更改目标
indicator.SetTarget(newTargetTransform);

// 手动显示/隐藏
indicator.SetVisible(true);  // 显示
indicator.SetVisible(false); // 隐藏
```

### 多目标指示

如果需要同时指示多个目标（如多个关键点），可以：

1. 复制 `OffScreenIndicator` 对象
2. 为每个指示器设置不同的 `Target`
3. 使用不同颜色或样式的箭头图标区分

### 自定义样式

**修改箭头颜色**
- 选中 `ArrowIcon` 对象
- 调整 `Image` 组件的 `Color` 属性

**修改距离文本样式**
- 选中 `DistanceText` 对象
- 调整 `Text` 组件的字体、大小、颜色等属性

**调整箭头大小**
- 选中 `ArrowIcon` 对象
- 修改 `RectTransform` 的 `Width` 和 `Height`

## 场景集成

### 在 Jaeger 场景中使用

1. 打开 `Assets/Scenes/Jaeger.unity`
2. 使用上述快速设置方法创建指示器
3. 确认指示器的 `Target` 已指向场景中的 `FinishZone` 对象
4. 运行游戏测试

### Canvas 设置

指示器会自动使用场景中的 Canvas。建议 Canvas 设置：

- `Render Mode`: Screen Space - Overlay
- `Canvas Scaler`: Scale With Screen Size
- `Reference Resolution`: 1920x1080 或项目目标分辨率

## 性能优化

- 使用 `LateUpdate` 确保在相机移动后更新位置
- 仅在目标存在且箭头可见时进行计算
- 避免在同一帧内频繁切换目标

## 故障排除

### 箭头不显示

1. 检查 `Target` 是否已设置
2. 确认目标确实在屏幕外
3. 检查 Canvas 是否启用
4. 确认 `ArrowIcon` 对象的 `Image` 组件有 Sprite

### 箭头位置不正确

1. 检查 `Target Camera` 是否正确（通常留空使用主相机）
2. 确认 Canvas 的 `Render Mode` 设置正确
3. 调整 `Edge Offset` 参数

### 距离文本不显示

1. 确认 `Show Distance` 已勾选
2. 检查 `Distance Text` 字段是否已连接
3. 确认文本对象启用

## 与其他系统集成

### 与任务系统集成

可以在任务开始时启用指示器，任务完成时禁用：

```csharp
public class MissionController : MonoBehaviour
{
    [SerializeField]
    private OffScreenTargetIndicator indicator;

    void StartMission()
    {
        indicator.SetVisible(true);
    }

    void CompleteMission()
    {
        indicator.SetVisible(false);
    }
}
```

### 与 UI 流程集成

在 Gameplay 场景加载时自动设置：

```csharp
public class GameplayBootstrap : MonoBehaviour
{
    void Start()
    {
        OffScreenTargetIndicator indicator = FindObjectOfType<OffScreenTargetIndicator>();
        FinishZone2D finishZone = FindObjectOfType<FinishZone2D>();
        
        if (indicator != null && finishZone != null)
        {
            indicator.SetTarget(finishZone.transform);
        }
    }
}
```

## 文件位置

- 主脚本：`Assets/Scripts/Gameplay/Mission/OffScreenTargetIndicator.cs`
- 编辑器工具：`Assets/Scripts/Editor/OffScreenIndicatorSetup.cs`
- 使用文档：`Docs/Technical/OffScreenTargetIndicator.md`
