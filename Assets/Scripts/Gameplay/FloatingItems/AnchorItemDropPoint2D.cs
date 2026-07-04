using UnityEngine;

/// <summary>
/// 挂在每个锚发射器上，表示该锚拉回漂浮物后生成拾取物的位置。
/// </summary>
[DisallowMultipleComponent]
public class AnchorItemDropPoint2D : MonoBehaviour
{
    [SerializeField]
    private Transform dropPoint;

    [Tooltip("没有显式 DropPoint 子节点时，使用发射器本地坐标偏移。")]
    [SerializeField]
    private Vector2 localDropOffset =
        new Vector2(0f, -0.7f);

    public Vector2 DropWorldPosition
    {
        get
        {
            if (dropPoint != null)
            {
                return dropPoint.position;
            }

            return transform.TransformPoint(
                localDropOffset
            );
        }
    }
}
