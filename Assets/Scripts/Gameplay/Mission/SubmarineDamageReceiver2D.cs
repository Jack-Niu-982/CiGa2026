using UnityEngine;

/// <summary>
/// 把飞船与危险对象的物理碰撞转换成血量扣除。
/// </summary>
[DisallowMultipleComponent]
public class SubmarineDamageReceiver2D : MonoBehaviour
{
    [Header("血量")]
    [SerializeField]
    private SubmarineHealth2D health;

    [Header("伤害")]
    [SerializeField]
    private float damageOnCollision = 25f;

    [SerializeField]
    private float damageCooldown = 0.75f;

    [SerializeField]
    private float minimumRelativeSpeed = 0.25f;

    [Header("危险来源")]
    [SerializeField]
    private LayerMask hazardLayers;

    [Tooltip("例如 Hazard、Obstacle。空列表表示只按 LayerMask 判断。")]
    [SerializeField]
    private string[] hazardTags;

    [Tooltip("开启后，LayerMask 和 Tag 都没配时也会对任何碰撞扣血。调试时可临时打开。")]
    [SerializeField]
    private bool damageAnyCollisionWhenNoFilter;

    private float lastDamageTime =
        float.NegativeInfinity;

    private void Reset()
    {
        health =
            GetComponent<SubmarineHealth2D>();
    }

    private void Awake()
    {
        if (health == null)
        {
            health =
                GetComponent<SubmarineHealth2D>();
        }
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (collision == null ||
            health == null ||
            health.IsDepleted)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude <
            minimumRelativeSpeed)
        {
            return;
        }

        if (!IsHazard(collision.gameObject))
        {
            return;
        }

        TryApplyDamage(
            damageOnCollision
        );
    }

    public bool TryApplyDamage(float amount)
    {
        if (health == null ||
            amount <= 0f)
        {
            return false;
        }

        if (Time.time - lastDamageTime <
            damageCooldown)
        {
            return false;
        }

        lastDamageTime = Time.time;

        return health.Damage(amount);
    }

    private bool IsHazard(GameObject source)
    {
        if (source == null)
        {
            return false;
        }

        bool hasLayerFilter =
            hazardLayers.value != 0;

        if (hasLayerFilter &&
            (hazardLayers.value &
             (1 << source.layer)) != 0)
        {
            return true;
        }

        bool hasTagFilter =
            hazardTags != null &&
            hazardTags.Length > 0;

        if (hasTagFilter)
        {
            string sourceTag =
                source.tag;

            for (int i = 0; i < hazardTags.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(
                        hazardTags[i]) &&
                    sourceTag == hazardTags[i])
                {
                    return true;
                }
            }
        }

        return
            damageAnyCollisionWhenNoFilter &&
            !hasLayerFilter &&
            !hasTagFilter;
    }
}
