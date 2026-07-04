using UnityEngine;

/// <summary>
/// 玩家交互输入组件。检测附近的可交互点并响应交互按键。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractionInput : MonoBehaviour
{
    [Header("输入")]
    [Tooltip("交互按键。")]
    [SerializeField]
    private KeyCode interactKey = KeyCode.E;

    [Header("检测")]
    [Tooltip("交互检测半径。")]
    [Min(0.1f)]
    [SerializeField]
    private float detectionRadius = 1.5f;

    [Tooltip("可交互物体的图层。")]
    [SerializeField]
    private LayerMask interactableLayer;

    [Header("UI 提示")]
    [Tooltip("显示交互提示的 UI（可选）。")]
    [SerializeField]
    private GameObject interactionPromptUI;

    private PlayerController playerController;
    private InteractableStation2D nearestStation;
    private Collider2D[] detectionResults = new Collider2D[10];

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateNearestStation();

        if (Input.GetKeyDown(interactKey) && nearestStation != null)
        {
            nearestStation.TryInteract(playerController);
        }

        UpdateInteractionPrompt();
    }

    private void UpdateNearestStation()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            detectionRadius,
            detectionResults,
            interactableLayer
        );

        InteractableStation2D closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            InteractableStation2D station =
                detectionResults[i].GetComponent<InteractableStation2D>();

            if (station != null && !station.IsInteracting)
            {
                float distance = Vector2.Distance(
                    transform.position,
                    station.transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = station;
                }
            }
        }

        nearestStation = closest;
    }

    private void UpdateInteractionPrompt()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(nearestStation != null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
