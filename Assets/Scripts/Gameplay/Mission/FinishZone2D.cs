using UnityEngine;

/// <summary>
/// 终点触发器。目标飞船进入后通知结算系统胜利。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class FinishZone2D : MonoBehaviour
{
    [Header("结算")]
    [SerializeField]
    private MissionSettlementController settlementController;

    [Header("目标飞船")]
    [SerializeField]
    private GameObject targetSubmarine;

    [SerializeField]
    private string fallbackSubmarineTag = "SubMarine";

    private Collider2D finishCollider;

    private void Reset()
    {
        finishCollider =
            GetComponent<Collider2D>();

        finishCollider.isTrigger = true;

        settlementController =
            FindObjectOfType<MissionSettlementController>();
    }

    private void Awake()
    {
        finishCollider =
            GetComponent<Collider2D>();

        finishCollider.isTrigger = true;

        if (settlementController == null)
        {
            settlementController =
                FindObjectOfType<MissionSettlementController>();
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (settlementController == null ||
            other == null ||
            settlementController.IsSettled)
        {
            return;
        }

        if (!IsTargetSubmarine(other))
        {
            return;
        }

        settlementController.Win();
    }

    private bool IsTargetSubmarine(
        Collider2D other)
    {
        GameObject candidate =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

        if (targetSubmarine != null)
        {
            return candidate == targetSubmarine ||
                   other.transform.root.gameObject ==
                   targetSubmarine;
        }

        if (other.GetComponentInParent<SubmarineHealth2D>() !=
            null)
        {
            return true;
        }

        return
            !string.IsNullOrWhiteSpace(fallbackSubmarineTag) &&
            candidate.tag == fallbackSubmarineTag;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color =
            new Color(0.2f, 0.9f, 0.5f, 0.35f);

        Collider2D currentCollider =
            GetComponent<Collider2D>();

        if (currentCollider == null)
        {
            Gizmos.DrawWireCube(
                transform.position,
                Vector3.one
            );

            return;
        }

        Gizmos.DrawWireCube(
            currentCollider.bounds.center,
            currentCollider.bounds.size
        );
    }
}
