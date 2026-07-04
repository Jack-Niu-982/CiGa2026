using UnityEngine;

/// <summary>
/// 燃料接收器。玩家将燃料拾取物投入此组件，补充飞船燃料。
/// 设计文档中"玩家把燃料投入船内燃料装置"对应此组件。
/// </summary>
[DisallowMultipleComponent]
public class SubmarineFuelReceiver2D : MonoBehaviour, ICarryItemReceiver
{
    [Header("燃料恢复")]
    [Tooltip("每个燃料拾取物恢复的燃料量。")]
    [Min(0f)]
    [SerializeField]
    private float fuelRestoreAmount = 20f;

    [Header("视觉反馈")]
    [Tooltip("接收燃料时播放的粒子效果（可选）。")]
    [SerializeField]
    private ParticleSystem fuelReceiveParticles;

    [Tooltip("接收燃料时播放的音效（可选）。")]
    [SerializeField]
    private AudioClip fuelReceiveSound;

    public bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        return
            holder != null &&
            item != null &&
            item.ItemType == CarryableItemType.Fuel;
    }

    public bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item)
    {
        if (!CanReceiveCarryItem(holder, item))
        {
            return false;
        }

        // TODO: 实际补充燃料逻辑
        // 等待 SubmarineFuel2D 或类似组件完成后接入
        Debug.Log($"[FuelReceiver] 接收燃料，恢复 {fuelRestoreAmount} 燃料");

        // 播放视觉/音效反馈
        if (fuelReceiveParticles != null)
        {
            fuelReceiveParticles.Play();
        }

        if (fuelReceiveSound != null)
        {
            AudioSource.PlayClipAtPoint(
                fuelReceiveSound,
                transform.position
            );
        }

        // 通知玩家物品已消耗
        holder.NotifyHeldItemDropped(item);
        Destroy(item.gameObject);

        return true;
    }
}
