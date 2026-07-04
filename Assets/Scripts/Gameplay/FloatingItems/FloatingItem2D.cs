using UnityEngine;

/// <summary>
/// 船外漂浮物。负责漂移、被锚拉回船体，并在抵达后生成玩家可拾取物。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class FloatingItem2D : MonoBehaviour
{
    [Header("物品")]
    [SerializeField]
    private CarryableItemType itemType =
        CarryableItemType.Unknown;

    [Tooltip("抵达锚位后生成的玩家可拾取物 Prefab。")]
    [SerializeField]
    private CarryableItem2D pickupPrefab;

    [Header("漂移")]
    [SerializeField]
    private Vector2 driftVelocity =
        new Vector2(-0.35f, 0f);

    [SerializeField]
    private float angularSpeed = 20f;

    [Min(0f)]
    [SerializeField]
    private float lifetime = 45f;

    [Header("锚拉回")]
    [SerializeField]
    private bool canBeCaughtByAnchor = true;

    [Min(0.01f)]
    [SerializeField]
    private float anchorPullSpeed = 5f;

    [Min(0.01f)]
    [SerializeField]
    private float arrivalDistance = 0.12f;

    private Rigidbody2D itemRigidbody;
    private Collider2D[] colliders;
    private AnchorItemDropPoint2D activeDropPoint;
    private float lifeTimer;
    private bool isBeingPulled;
    private bool hasResolved;

    public CarryableItemType ItemType => itemType;
    public bool CanBeCaughtByAnchor => canBeCaughtByAnchor && !isBeingPulled && !hasResolved;

    private void Reset()
    {
        itemRigidbody = GetComponent<Rigidbody2D>();
        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
    }

    private void Awake()
    {
        itemRigidbody = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);

        itemRigidbody.bodyType = RigidbodyType2D.Kinematic;
        itemRigidbody.gravityScale = 0f;
        itemRigidbody.simulated = true;
    }

    private void FixedUpdate()
    {
        if (hasResolved)
        {
            return;
        }

        if (isBeingPulled)
        {
            UpdateAnchorPull();
            return;
        }

        UpdateDrift();
    }

    private void Update()
    {
        if (lifetime <= 0f ||
            isBeingPulled ||
            hasResolved)
        {
            return;
        }

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public bool TryStartAnchorPull(
        AnchorItemDropPoint2D dropPoint)
    {
        if (!CanBeCaughtByAnchor ||
            dropPoint == null)
        {
            return false;
        }

        activeDropPoint = dropPoint;
        isBeingPulled = true;

        SetColliderEnabled(false);

        if (itemRigidbody != null)
        {
            itemRigidbody.velocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }

        return true;
    }

    private void UpdateDrift()
    {
        Vector2 nextPosition =
            itemRigidbody.position +
            driftVelocity * Time.fixedDeltaTime;

        itemRigidbody.MovePosition(nextPosition);

        if (!Mathf.Approximately(angularSpeed, 0f))
        {
            itemRigidbody.MoveRotation(
                itemRigidbody.rotation +
                angularSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void UpdateAnchorPull()
    {
        if (activeDropPoint == null)
        {
            ResolveWithoutPickup();
            return;
        }

        Vector2 targetPosition =
            activeDropPoint.DropWorldPosition;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                itemRigidbody.position,
                targetPosition,
                anchorPullSpeed * Time.fixedDeltaTime
            );

        itemRigidbody.MovePosition(nextPosition);

        float remainingDistance =
            Vector2.Distance(
                nextPosition,
                targetPosition
            );

        if (remainingDistance <= arrivalDistance)
        {
            SpawnPickupAndDestroy(targetPosition);
        }
    }

    private void SpawnPickupAndDestroy(
        Vector2 worldPosition)
    {
        if (hasResolved)
        {
            return;
        }

        hasResolved = true;

        if (pickupPrefab != null)
        {
            CarryableItem2D pickup =
                Instantiate(
                    pickupPrefab,
                    worldPosition,
                    Quaternion.identity
                );

            pickup.name =
                $"{pickupPrefab.name}_{itemType}_Drop";
        }

        Destroy(gameObject);
    }

    private void ResolveWithoutPickup()
    {
        hasResolved = true;
        Destroy(gameObject);
    }

    private void SetColliderEnabled(
        bool enabledState)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabledState;
            }
        }
    }

    public void Configure(
        CarryableItemType newItemType,
        CarryableItem2D newPickupPrefab,
        Vector2 newDriftVelocity)
    {
        itemType = newItemType;
        pickupPrefab = newPickupPrefab;
        driftVelocity = newDriftVelocity;
    }

    public void SetDriftVelocity(
        Vector2 newDriftVelocity)
    {
        driftVelocity = newDriftVelocity;
    }
}
