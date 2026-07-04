using UnityEngine;

/// <summary>
/// 漂浮物生成区域。第一版使用矩形范围，方便在 Scene 里手动摆放。
/// </summary>
[DisallowMultipleComponent]
public class FloatingItemSpawnZone2D : MonoBehaviour
{
    [SerializeField]
    private Vector2 size =
        new Vector2(2f, 2f);

    [SerializeField]
    private Vector2 driftVelocity =
        new Vector2(-0.35f, 0f);

    public Vector2 DriftVelocity => driftVelocity;

    public Vector2 GetRandomWorldPosition()
    {
        Vector2 local =
            new Vector2(
                Random.Range(-size.x * 0.5f, size.x * 0.5f),
                Random.Range(-size.y * 0.5f, size.y * 0.5f)
            );

        return transform.TransformPoint(local);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);

        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            size
        );

        Gizmos.matrix = oldMatrix;
    }
}
