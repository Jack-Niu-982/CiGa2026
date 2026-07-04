using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 当目标（终点）在屏幕外时，在屏幕边缘显示指向目标的箭头指示器
/// </summary>
public class OffScreenTargetIndicator : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("要追踪的目标对象（通常是终点）")]
    [SerializeField]
    private Transform target;

    [Header("UI 设置")]
    [Tooltip("箭头图标的 RectTransform")]
    [SerializeField]
    private RectTransform arrowIcon;

    [Tooltip("箭头距离屏幕边缘的偏移量（像素）")]
    [SerializeField]
    private float edgeOffset = 50f;

    [Header("相机设置")]
    [Tooltip("用于检测的相机，留空则使用主相机")]
    [SerializeField]
    private Camera targetCamera;

    [Header("可选设置")]
    [Tooltip("是否显示距离文本")]
    [SerializeField]
    private bool showDistance = false;

    [Tooltip("距离文本组件")]
    [SerializeField]
    private Text distanceText;

    [Tooltip("距离单位名称")]
    [SerializeField]
    private string distanceUnit = "m";

    [Header("调试")]
    [Tooltip("显示调试信息")]
    [SerializeField]
    private bool debugMode = false;

    private Canvas canvas;
    private RectTransform canvasRect;
    private readonly StringBuilder distanceBuilder = new StringBuilder(16);
    private int lastDistance = int.MinValue;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (arrowIcon != null)
        {
            canvas = arrowIcon.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
            }
        }

        // 初始时隐藏箭头
        if (arrowIcon != null)
        {
            arrowIcon.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (target == null || arrowIcon == null || targetCamera == null || canvasRect == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"OffScreenIndicator 缺少引用: target={target != null}, arrow={arrowIcon != null}, camera={targetCamera != null}, canvas={canvasRect != null}");
            }
            return;
        }

        // 将目标世界坐标转换为视口坐标
        Vector3 viewportPos = targetCamera.WorldToViewportPoint(target.position);

        // 检查目标是否在屏幕外
        bool isOffScreen = viewportPos.x < 0 || viewportPos.x > 1 ||
                          viewportPos.y < 0 || viewportPos.y > 1 ||
                          viewportPos.z < 0;

        if (debugMode)
        {
            Debug.Log($"Target viewport pos: {viewportPos}, isOffScreen: {isOffScreen}");
        }

        if (isOffScreen)
        {
            // 显示箭头
            if (!arrowIcon.gameObject.activeSelf)
            {
                arrowIcon.gameObject.SetActive(true);
                if (debugMode) Debug.Log("箭头已显示");
            }

            // 计算箭头位置和旋转
            UpdateArrowPositionAndRotation(viewportPos);

            // 更新距离文本
            if (showDistance && distanceText != null)
            {
                int distance = Mathf.RoundToInt(Vector3.Distance(targetCamera.transform.position, target.position));
                if (distance != lastDistance)
                {
                    lastDistance = distance;
                    distanceBuilder.Clear();
                    distanceBuilder.Append(distance);
                    distanceBuilder.Append(distanceUnit);
                    distanceText.text = distanceBuilder.ToString();
                }
            }
        }
        else
        {
            // 目标在屏幕内，隐藏箭头
            if (arrowIcon.gameObject.activeSelf)
            {
                arrowIcon.gameObject.SetActive(false);
                if (debugMode) Debug.Log("箭头已隐藏");
            }
        }
    }

    private void UpdateArrowPositionAndRotation(Vector3 viewportPos)
    {
        // 将视口坐标限制在屏幕边缘
        Vector3 clampedViewportPos = viewportPos;

        // 处理在相机后方的情况
        if (viewportPos.z < 0)
        {
            // 反转坐标
            clampedViewportPos.x = 1 - clampedViewportPos.x;
            clampedViewportPos.y = 1 - clampedViewportPos.y;
        }

        // 限制到 0-1 范围
        clampedViewportPos.x = Mathf.Clamp01(clampedViewportPos.x);
        clampedViewportPos.y = Mathf.Clamp01(clampedViewportPos.y);

        // 计算从屏幕中心到目标的方向
        Vector2 centerToTarget = new Vector2(
            clampedViewportPos.x - 0.5f,
            clampedViewportPos.y - 0.5f
        );

        // 计算箭头应该在屏幕边缘的位置
        Vector2 screenPosition = CalculateEdgePosition(centerToTarget);

        // 转换为画布坐标（根据 Canvas 的实际大小）
        Vector2 canvasSize = canvasRect.sizeDelta;
        arrowIcon.anchoredPosition = new Vector2(
            (screenPosition.x - 0.5f) * canvasSize.x,
            (screenPosition.y - 0.5f) * canvasSize.y
        );

        // 计算箭头旋转角度（指向目标）
        float angle = Mathf.Atan2(centerToTarget.y, centerToTarget.x) * Mathf.Rad2Deg;
        arrowIcon.localRotation = Quaternion.Euler(0, 0, angle - 90f); // -90 是因为箭头默认朝上
    }

    private Vector2 CalculateEdgePosition(Vector2 direction)
    {
        // 归一化方向
        direction.Normalize();

        // 计算到达屏幕边缘的比例
        float scaleX = Mathf.Abs(direction.x) > 0.001f ? (0.5f - edgeOffset / canvasRect.sizeDelta.x) / Mathf.Abs(direction.x) : float.MaxValue;
        float scaleY = Mathf.Abs(direction.y) > 0.001f ? (0.5f - edgeOffset / canvasRect.sizeDelta.y) / Mathf.Abs(direction.y) : float.MaxValue;

        // 使用较小的比例，确保箭头在屏幕边缘内
        float scale = Mathf.Min(scaleX, scaleY);

        // 计算最终位置（从中心偏移）
        Vector2 edgePosition = new Vector2(
            0.5f + direction.x * scale,
            0.5f + direction.y * scale
        );

        return edgePosition;
    }

    /// <summary>
    /// 设置要追踪的目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 手动显示或隐藏指示器
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (arrowIcon != null)
        {
            arrowIcon.gameObject.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 编辑器中验证设置
        if (arrowIcon != null && canvas == null)
        {
            canvas = arrowIcon.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
            }
        }
    }
#endif
}
