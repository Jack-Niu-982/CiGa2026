using UnityEngine;

/// <summary>
/// 让锚系统可以识别这个漂浮物，并把它交给 FloatingItem2D 拉回船体。
/// </summary>
[DisallowMultipleComponent]
public class FloatingItemAnchorTarget2D : MonoBehaviour
{
    [SerializeField]
    private FloatingItem2D floatingItem;

    public bool CanBeCaught =>
        floatingItem != null &&
        floatingItem.CanBeCaughtByAnchor;

    private void Reset()
    {
        floatingItem =
            GetComponent<FloatingItem2D>();
    }

    private void Awake()
    {
        if (floatingItem == null)
        {
            floatingItem =
                GetComponent<FloatingItem2D>();
        }
    }

    public bool TryCatch(
        AnchorItemDropPoint2D dropPoint,
        Transform anchorTransform)
    {
        if (floatingItem == null)
        {
            return false;
        }

        return floatingItem.TryStartAnchorPull(
            dropPoint,
            anchorTransform
        );
    }
}
