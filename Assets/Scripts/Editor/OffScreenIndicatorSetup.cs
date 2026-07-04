using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 屏幕外目标指示器的辅助设置工具
/// </summary>
public static class OffScreenIndicatorSetup
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Off-Screen Target Indicator", false, 10)]
    public static void CreateOffScreenIndicator(MenuCommand menuCommand)
    {
        // 查找或创建 Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建 EventSystem（如果不存在）
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // 创建指示器容器
        GameObject indicatorObj = new GameObject("OffScreenIndicator");
        indicatorObj.transform.SetParent(canvas.transform, false);

        // 添加主控制脚本
        OffScreenTargetIndicator indicator = indicatorObj.AddComponent<OffScreenTargetIndicator>();

        // 创建箭头图标
        GameObject arrowObj = new GameObject("ArrowIcon");
        arrowObj.transform.SetParent(indicatorObj.transform, false);
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(60, 60);
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;

        // 添加 Image 组件
        Image arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = Color.yellow;

        // 尝试创建一个简单的箭头 Sprite（如果没有现成的）
        // 注意：这需要用户后续替换为实际的箭头图标
        arrowImage.sprite = CreateTemporaryArrowSprite();

        // 创建距离文本（可选）
        GameObject textObj = new GameObject("DistanceText");
        textObj.transform.SetParent(arrowObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100, 30);
        textRect.anchoredPosition = new Vector2(0, -40);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Text distanceText = textObj.AddComponent<Text>();
        distanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        distanceText.fontSize = 18;
        distanceText.alignment = TextAnchor.MiddleCenter;
        distanceText.color = Color.white;
        distanceText.text = "0m";

        // 添加 Outline 使文本更清晰
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        // 连接引用
        SerializedObject so = new SerializedObject(indicator);
        so.FindProperty("arrowIcon").objectReferenceValue = arrowRect;
        so.FindProperty("distanceText").objectReferenceValue = distanceText;
        so.FindProperty("showDistance").boolValue = true;
        so.FindProperty("edgeOffset").floatValue = 80f;
        so.ApplyModifiedProperties();

        // 选中创建的对象
        Selection.activeGameObject = indicatorObj;

        // 标记为已修改
        Undo.RegisterCreatedObjectUndo(indicatorObj, "Create Off-Screen Indicator");

        Debug.Log("屏幕外目标指示器已创建。请在 Inspector 中设置 Target 字段为终点对象。");
    }

    private static Sprite CreateTemporaryArrowSprite()
    {
        // 创建一个简单的三角形纹理作为临时箭头
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        // 填充透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 绘制三角形箭头（指向上方）
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 简单的三角形形状
                float centerX = size / 2f;
                float distFromCenter = Mathf.Abs(x - centerX);
                float maxDist = (size - y) * 0.5f;

                if (distFromCenter < maxDist && y > size * 0.2f)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return sprite;
    }

    [MenuItem("GameObject/UI/Find Finish Zone and Setup Indicator", false, 11)]
    public static void AutoSetupIndicatorForFinishZone()
    {
        // 查找场景中的 FinishZone
        FinishZone2D finishZone = Object.FindObjectOfType<FinishZone2D>();
        if (finishZone == null)
        {
            EditorUtility.DisplayDialog(
                "未找到终点",
                "场景中没有找到 FinishZone2D 组件。请先创建终点对象。",
                "确定"
            );
            return;
        }

        // 查找或创建指示器
        OffScreenTargetIndicator indicator = Object.FindObjectOfType<OffScreenTargetIndicator>();
        if (indicator == null)
        {
            // 创建新指示器
            CreateOffScreenIndicator(null);
            indicator = Object.FindObjectOfType<OffScreenTargetIndicator>();
        }

        if (indicator != null)
        {
            // 设置目标为终点
            SerializedObject so = new SerializedObject(indicator);
            so.FindProperty("target").objectReferenceValue = finishZone.transform;
            so.ApplyModifiedProperties();

            EditorUtility.DisplayDialog(
                "设置成功",
                $"已将指示器目标设置为: {finishZone.gameObject.name}",
                "确定"
            );

            Selection.activeGameObject = indicator.gameObject;
        }
    }
#endif
}
