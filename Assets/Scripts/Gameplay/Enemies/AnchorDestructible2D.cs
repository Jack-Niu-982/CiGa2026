using UnityEngine;

/// <summary>
/// 标记可以被飞行中的船锚直接清除的目标。
/// </summary>
[DisallowMultipleComponent]
public class AnchorDestructible2D : MonoBehaviour
{
    [Header("音效")]
    [Tooltip("被船锚击中并删除时播放。")]
    [SerializeField] private AudioClip enemyDeathClip;

    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 1f;

    private bool isBeingDestroyed;

    public bool TryDestroyByAnchor()
    {
        if (isBeingDestroyed)
        {
            return false;
        }

        isBeingDestroyed = true;

        if (enemyDeathClip != null)
        {
            AudioSource.PlayClipAtPoint(
                enemyDeathClip,
                transform.position,
                deathVolume);
        }

        Destroy(gameObject);
        return true;
    }

    private void OnValidate()
    {
        deathVolume = Mathf.Clamp01(deathVolume);
    }
}
