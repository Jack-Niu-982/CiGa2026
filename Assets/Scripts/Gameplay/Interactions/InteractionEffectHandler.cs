using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 交互效果处理器。根据交互类型执行不同的逻辑。
/// </summary>
public class InteractionEffectHandler : MonoBehaviour
{
    [Header("火炉效果")]
    [Tooltip("烹饪成功后生成的食物 Prefab。")]
    [SerializeField]
    private CarryableItem2D cookedFoodPrefab;

    [Tooltip("烹饪时的粒子效果。")]
    [SerializeField]
    private ParticleSystem cookingParticles;

    [Header("防御站效果")]
    [Tooltip("射击伤害值。")]
    [SerializeField]
    private float defenseDamage = 10f;

    [Tooltip("射击冷却时间（秒）。")]
    [SerializeField]
    private float defenseCooldown = 1f;

    [Header("修理效果")]
    [Tooltip("每次修理恢复的生命值。")]
    [SerializeField]
    private float repairAmount = 20f;

    [Tooltip("修理粒子效果。")]
    [SerializeField]
    private ParticleSystem repairParticles;

    [Header("事件")]
    public UnityEvent<InteractionType> onEffectExecuted;

    private InteractableStation2D station;
    private float lastDefenseTime;

    private void Awake()
    {
        station = GetComponent<InteractableStation2D>();

        if (station != null)
        {
            station.onInteractionSuccess.AddListener(HandleInteractionSuccess);
        }
    }

    private void HandleInteractionSuccess(PlayerController player)
    {
        if (station == null)
        {
            return;
        }

        switch (station.StationType)
        {
            case InteractionType.Stove:
                ExecuteStoveEffect(player);
                break;

            case InteractionType.DefenseStation:
                ExecuteDefenseEffect(player);
                break;

            case InteractionType.RepairStation:
                ExecuteRepairEffect(player);
                break;

            case InteractionType.StorageChest:
                ExecuteStorageEffect(player);
                break;

            case InteractionType.Workbench:
                ExecuteWorkbenchEffect(player);
                break;

            case InteractionType.Helm:
                ExecuteHelmEffect(player);
                break;
        }

        onEffectExecuted?.Invoke(station.StationType);
    }

    private void ExecuteStoveEffect(PlayerController player)
    {
        Debug.Log("火炉：烹饪食物完成！");

        if (cookingParticles != null)
        {
            cookingParticles.Play();
        }

        if (cookedFoodPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            Instantiate(cookedFoodPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void ExecuteDefenseEffect(PlayerController player)
    {
        if (Time.time - lastDefenseTime < defenseCooldown)
        {
            Debug.Log("防御站：冷却中...");
            return;
        }

        lastDefenseTime = Time.time;
        Debug.Log($"防御站：发射！造成 {defenseDamage} 伤害");

        // TODO: 实现射击逻辑
    }

    private void ExecuteRepairEffect(PlayerController player)
    {
        Debug.Log($"修理台：修理完成，恢复 {repairAmount} 生命值");

        if (repairParticles != null)
        {
            repairParticles.Play();
        }

        // TODO: 实现船体修理逻辑
        // 例如：FindObjectOfType<ShipHealth>()?.Heal(repairAmount);
    }

    private void ExecuteStorageEffect(PlayerController player)
    {
        Debug.Log("储物箱：打开储物界面");

        // TODO: 实现储物箱 UI
    }

    private void ExecuteWorkbenchEffect(PlayerController player)
    {
        Debug.Log("工作台：制作物品完成");

        // TODO: 实现制作系统
    }

    private void ExecuteHelmEffect(PlayerController player)
    {
        Debug.Log("舵：控制船只方向");

        // TODO: 实现船只控制
    }
}
