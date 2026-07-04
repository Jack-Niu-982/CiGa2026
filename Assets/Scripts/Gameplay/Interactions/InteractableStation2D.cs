using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// 可交互站点组件。支持玩家靠近触发交互，显示描边高亮。
/// </summary>
[DisallowMultipleComponent]
public class InteractableStation2D : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("工作站配置。不同工作站应该指向不同的 StationSettings 资产。")]
    [SerializeField]
    private StationSettings stationSettings;

    [Header("检测范围")]
    [Tooltip("玩家进入此范围内可以交互。")]
    [SerializeField]
    private Collider2D interactionTrigger;

    [Header("交互事件")]
    [Tooltip("玩家进入范围时触发。")]
    public UnityEvent<PlayerController> onPlayerEnter;

    [Tooltip("玩家离开范围时触发。")]
    public UnityEvent<PlayerController> onPlayerExit;

    [Tooltip("交互开始时触发。")]
    public UnityEvent<PlayerController> onInteractionStart;

    [Tooltip("交互成功时触发。")]
    public UnityEvent<PlayerController> onInteractionSuccess;

    [Tooltip("交互失败时触发。")]
    public UnityEvent<PlayerController> onInteractionFail;

    private SpriteRenderer[] spriteRenderers;
    private Material[] outlineInstances;
    private PlayerController nearbyPlayer;
    private bool isPlayerNearby;
    private bool isInteracting;
    private Tween outlineTween;

    public StationSettings Settings => GetSettings();
    public InteractionType StationType =>
        Settings != null
            ? Settings.stationType
            : InteractionType.None;
    public InteractionRequirement Requirement =>
        Settings != null
            ? Settings.requirement
            : null;
    public bool IsPlayerNearby => isPlayerNearby;
    public bool IsInteracting => isInteracting;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (interactionTrigger == null)
        {
            interactionTrigger = GetComponent<Collider2D>();
        }

        if (interactionTrigger != null)
        {
            interactionTrigger.isTrigger = true;
        }

        if (Settings != null &&
            Settings.outlineMaterial != null)
        {
            SetupOutlineMaterials();
        }
    }

    private void SetupOutlineMaterials()
    {
        outlineInstances = new Material[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Material instance =
                    new Material(
                        Settings.outlineMaterial
                    );
                outlineInstances[i] = instance;

                Material[] mats = new Material[2];
                mats[0] = spriteRenderers[i].material;
                mats[1] = instance;
                spriteRenderers[i].materials = mats;

                instance.SetColor(
                    "_OutlineColor",
                    Settings.outlineColor
                );
                instance.SetFloat("_OutlineWidth", 0f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerNearby)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            nearbyPlayer = player;
            isPlayerNearby = true;
            ShowOutline();
            onPlayerEnter?.Invoke(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isPlayerNearby)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == nearbyPlayer)
        {
            nearbyPlayer = null;
            isPlayerNearby = false;
            HideOutline();
            onPlayerExit?.Invoke(player);
        }
    }

    public bool TryInteract(PlayerController player)
    {
        if (!isPlayerNearby || isInteracting)
        {
            return false;
        }

        if (!ValidateRequirements(player))
        {
            onInteractionFail?.Invoke(player);
            return false;
        }

        StartInteraction(player);
        return true;
    }

    private bool ValidateRequirements(PlayerController player)
    {
        InteractionRequirement requirement =
            Requirement;

        if (requirement == null)
        {
            return true;
        }

        if (requirement.requiredItemType != CarryableItemType.Unknown)
        {
            PlayerCarryInteractor2D carryInteractor =
                player.GetComponent<PlayerCarryInteractor2D>();

            if (carryInteractor == null)
            {
                return false;
            }

            CarryableItem2D carriedItem = carryInteractor.HeldItem;

            if (carriedItem == null ||
                carriedItem.ItemType != requirement.requiredItemType)
            {
                return false;
            }
        }

        return true;
    }

    private void StartInteraction(PlayerController player)
    {
        isInteracting = true;
        onInteractionStart?.Invoke(player);

        InteractionRequirement requirement =
            Requirement;

        if (requirement == null)
        {
            CompleteInteraction(player, true);
            return;
        }

        if (requirement.consumeItem &&
            requirement.requiredItemType != CarryableItemType.Unknown)
        {
            ConsumePlayerItem(player);
        }

        if (requirement.requiresQTE)
        {
            StartQTE(player);
        }
        else if (requirement.duration > 0f)
        {
            DOVirtual.DelayedCall(requirement.duration, () =>
            {
                CompleteInteraction(player, true);
            });
        }
        else
        {
            CompleteInteraction(player, true);
        }
    }

    private void ConsumePlayerItem(PlayerController player)
    {
        PlayerCarryInteractor2D carryInteractor =
            player.GetComponent<PlayerCarryInteractor2D>();

        if (carryInteractor != null && carryInteractor.HeldItem != null)
        {
            Destroy(carryInteractor.HeldItem.gameObject);
        }
    }

    private void StartQTE(PlayerController player)
    {
        InteractionRequirement requirement =
            Requirement;

        if (requirement == null)
        {
            CompleteInteraction(player, true);
            return;
        }

        Debug.Log($"QTE 开始！在 {requirement.qteTimeWindow} 秒内按 {requirement.qteKey}");
    }

    private void CompleteInteraction(PlayerController player, bool success)
    {
        isInteracting = false;

        if (success)
        {
            onInteractionSuccess?.Invoke(player);
            ExecuteInteractionEffect();
        }
        else
        {
            onInteractionFail?.Invoke(player);
        }
    }

    private void ExecuteInteractionEffect()
    {
        switch (StationType)
        {
            case InteractionType.Stove:
                Debug.Log("烹饪食物完成！");
                break;

            case InteractionType.DefenseStation:
                Debug.Log("防御站激活！");
                break;

            case InteractionType.RepairStation:
                Debug.Log("修理完成！");
                break;

            case InteractionType.FuelStation:
                Debug.Log("燃料投入完成！");
                break;

            case InteractionType.StorageChest:
                Debug.Log("打开储物箱！");
                break;

            case InteractionType.Workbench:
                Debug.Log("制作物品完成！");
                break;

            case InteractionType.Helm:
                Debug.Log("舵已激活！");
                break;
        }
    }

    private void ShowOutline()
    {
        if (outlineInstances == null)
        {
            return;
        }

        if (outlineTween != null && outlineTween.IsActive())
        {
            outlineTween.Kill();
        }

        outlineTween = DOVirtual.Float(
            0f,
            Settings != null ? Settings.outlineWidth : 0f,
            Settings != null ? Settings.outlineFadeInDuration : 0.01f,
            value =>
            {
                foreach (var mat in outlineInstances)
                {
                    if (mat != null)
                    {
                        mat.SetFloat("_OutlineWidth", value);
                    }
                }
            }
        ).SetEase(Ease.OutCubic);
    }

    private void HideOutline()
    {
        if (outlineInstances == null)
        {
            return;
        }

        if (outlineTween != null && outlineTween.IsActive())
        {
            outlineTween.Kill();
        }

        outlineTween = DOVirtual.Float(
            Settings != null ? Settings.outlineWidth : 0f,
            0f,
            Settings != null ? Settings.outlineFadeOutDuration : 0.01f,
            value =>
            {
                foreach (var mat in outlineInstances)
                {
                    if (mat != null)
                    {
                        mat.SetFloat("_OutlineWidth", value);
                    }
                }
            }
        ).SetEase(Ease.InCubic);
    }

    private void OnDestroy()
    {
        if (outlineTween != null && outlineTween.IsActive())
        {
            outlineTween.Kill();
        }

        if (outlineInstances != null)
        {
            foreach (var mat in outlineInstances)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionTrigger != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            float radius =
                Settings != null
                    ? Settings.gizmoRadius
                    : 1f;

            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }

    private StationSettings GetSettings()
    {
        if (stationSettings != null)
        {
            return stationSettings;
        }

        return SettingManager.Station;
    }
}
