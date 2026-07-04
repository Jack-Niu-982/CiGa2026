using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有可操作设施的通用父类。
///
/// 负责：
/// 1. 检测进入交互范围的玩家；
/// 2. 管理 IfInUse 和当前操作者；
/// 3. 将玩家同步到指定操作位置；
/// 4. 将操作状态变化通知给子类；
/// 5. 保存当前操作者自己的输入组件。
/// </summary>
public class OperateController : MonoBehaviour
{
    [Header("操作状态")]
    [Tooltip("当前设施是否正在被玩家使用，默认关闭。")]
    public bool IfInUse = false;

    [Header("玩家检测")]
    [Tooltip("能够触发交互的玩家标签。留空则不检查 Tag。")]
    [SerializeField]
    private string playerTag = "Player";

    [Tooltip(
        "开启后，玩家完全离开触发范围时自动退出操作。\n" +
        "如果同步位置在触发范围外，建议关闭。"
    )]
    [SerializeField]
    private bool stopWhenOperatorLeaves = false;

    [Header("玩家位置同步")]
    [Tooltip("是否在开始交互后，把玩家同步到指定位置。")]
    [SerializeField]
    private bool syncOperatorPosition = true;

    [Tooltip(
        "玩家位置同步的参考点。\n" +
        "留空时使用当前 OperateController 物体的位置。"
    )]
    [SerializeField]
    private Transform operatorSyncPoint;

    [Tooltip("玩家相对于同步参考点的位置偏移。")]
    [SerializeField]
    private Vector2 operatorPositionOffset;

    [Tooltip(
        "开启后，Offset 会跟随同步点旋转。\n" +
        "关闭后，Offset 始终按照世界坐标计算。"
    )]
    [SerializeField]
    private bool offsetUsesLocalDirection = true;

    [Tooltip(
        "开启后，交互期间每个物理帧都会同步玩家位置。\n" +
        "适合会移动的潜艇、载具或操作台。"
    )]
    [SerializeField]
    private bool continuouslySyncOperator = true;

    [Tooltip("同步玩家位置时，是否清空玩家当前速度。")]
    [SerializeField]
    private bool clearOperatorVelocity = true;

    /// <summary>
    /// 当前操作者的玩家交互组件。
    /// </summary>
    public PlayerOperateInteractor2D CurrentOperatorInteractor
    {
        get;
        private set;
    }

    /// <summary>
    /// 当前正在操作设施的玩家物体。
    /// </summary>
    public GameObject CurrentOperator
    {
        get
        {
            if (CurrentOperatorInteractor == null)
            {
                return null;
            }

            return CurrentOperatorInteractor.PlayerObject;
        }
    }

    /// <summary>
    /// 当前操作者实际使用的输入组件。
    ///
    /// 每个玩家的 PlayerOperateInteractor2D
    /// 只会返回该玩家自己的输入。
    /// </summary>
    public PlayerInputBase CurrentOperatorInput
    {
        get
        {
            if (CurrentOperatorInteractor == null)
            {
                return null;
            }

            return CurrentOperatorInteractor.CurrentPlayerInput;
        }
    }

    /// <summary>
    /// 当前是否至少有一名玩家位于交互范围内。
    /// </summary>
    public bool HasPlayerInRange =>
        playerColliderCounts.Count > 0;

    /*
     * 玩家可能拥有多个 Collider2D。
     * 记录每个玩家当前有多少个碰撞体位于范围内，
     * 防止单个碰撞体离开时误判为整个玩家离开。
     */
    private readonly Dictionary<PlayerOperateInteractor2D, int>
        playerColliderCounts =
            new Dictionary<PlayerOperateInteractor2D, int>();

    private bool lastAppliedUseState;

    protected virtual void Awake()
    {
        lastAppliedUseState = IfInUse;

        ApplyOperateState();
    }

    protected virtual void Update()
    {
        RemoveDestroyedPlayers();

        /*
         * 允许其他脚本直接修改公开的 IfInUse。
         */
        if (lastAppliedUseState != IfInUse)
        {
            ApplyOperateState();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!IfInUse ||
            !syncOperatorPosition ||
            !continuouslySyncOperator)
        {
            return;
        }

        SyncCurrentOperatorPosition();
    }

    /// <summary>
    /// 判断指定玩家是否可以开始操作。
    /// </summary>
    public virtual bool CanPlayerStartOperate(
        PlayerOperateInteractor2D player)
    {
        if (IfInUse ||
            player == null)
        {
            return false;
        }

        if (!IsPlayerInRange(player))
        {
            return false;
        }

        /*
         * 同一个玩家不能同时操作两个设施。
         */
        if (player.CurrentOperateController != null &&
            player.CurrentOperateController != this)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试让指定玩家开始操作。
    /// </summary>
    public virtual bool TryStartOperate(
        PlayerOperateInteractor2D player)
    {
        if (!CanPlayerStartOperate(player))
        {
            return false;
        }

        /*
         * 必须先记录当前玩家。
         *
         * 之后 SetInUse(true) 调用子类时，
         * 子类才能取得正确的 CurrentOperatorInput。
         */
        CurrentOperatorInteractor = player;

        player.NotifyOperateStarted(this);

        SetInUse(true);

        /*
         * 开始交互时先立刻吸附一次。
         */
        if (syncOperatorPosition)
        {
            SyncCurrentOperatorPosition(true);
        }

        Debug.Log(
            $"{player.PlayerObject.name} 已开始操作 {gameObject.name}"
        );

        return true;
    }

    /// <summary>
    /// 当前操作者尝试结束操作。
    /// </summary>
    public virtual bool TryStopOperate(
        PlayerOperateInteractor2D player)
    {
        if (!IfInUse ||
            player == null ||
            player != CurrentOperatorInteractor)
        {
            return false;
        }

        StopOperate();

        return true;
    }

    /// <summary>
    /// 强制结束当前操作。
    /// </summary>
    public virtual void StopOperate()
    {
        string operatorName =
            CurrentOperator != null
                ? CurrentOperator.name
                : "未知玩家";

        Debug.Log(
            $"{operatorName} 已结束操作 {gameObject.name}"
        );

        SetInUse(false);
    }

    /// <summary>
    /// 由其他脚本直接设置使用状态。
    /// </summary>
    public void SetInUse(bool value)
    {
        IfInUse = value;

        ApplyOperateState();
    }

    /// <summary>
    /// 每次 IfInUse 改变时调用。
    /// 子类可以重写这个方法来启用或禁用自己的组件。
    /// </summary>
    protected virtual void OnOperateStateChanged(
        bool isInUse)
    {
    }

    /// <summary>
    /// 判断指定玩家是否还在交互范围内。
    /// </summary>
    public bool IsPlayerInRange(
        PlayerOperateInteractor2D player)
    {
        if (player == null)
        {
            return false;
        }

        return playerColliderCounts.ContainsKey(player);
    }

    /// <summary>
    /// 获取玩家应该同步到的世界坐标。
    /// </summary>
    public Vector2 GetOperatorSyncPosition()
    {
        Transform syncPoint =
            operatorSyncPoint != null
                ? operatorSyncPoint
                : transform;

        Vector3 offset;

        if (offsetUsesLocalDirection)
        {
            /*
             * Offset 跟随同步点旋转，
             * 但不会受到同步点缩放影响。
             */
            offset = syncPoint.TransformDirection(
                new Vector3(
                    operatorPositionOffset.x,
                    operatorPositionOffset.y,
                    0f
                )
            );
        }
        else
        {
            /*
             * 使用世界坐标 Offset。
             */
            offset = new Vector3(
                operatorPositionOffset.x,
                operatorPositionOffset.y,
                0f
            );
        }

        return (Vector2)(
            syncPoint.position + offset
        );
    }

    /// <summary>
    /// 手动将当前操作者同步到操作位置。
    /// </summary>
    public void SyncCurrentOperatorPosition()
    {
        SyncCurrentOperatorPosition(false);
    }

    /// <summary>
    /// 将当前操作者移动到同步位置。
    /// </summary>
    private void SyncCurrentOperatorPosition(
        bool immediate)
    {
        GameObject playerObject =
            CurrentOperator;

        if (playerObject == null)
        {
            return;
        }

        Vector2 targetPosition =
            GetOperatorSyncPosition();

        Rigidbody2D playerRigidbody =
            playerObject.GetComponent<Rigidbody2D>();

        if (playerRigidbody == null)
        {
            playerRigidbody =
                playerObject.GetComponentInParent<Rigidbody2D>();
        }

        if (playerRigidbody != null)
        {
            if (clearOperatorVelocity)
            {
                playerRigidbody.velocity =
                    Vector2.zero;

                playerRigidbody.angularVelocity =
                    0f;
            }

            /*
             * 用 Rigidbody2D 设置物理位置。
             */
            playerRigidbody.position =
                targetPosition;

            /*
             * 开始操作的这一帧立刻更新视觉位置，
             * 不用等待下一个 FixedUpdate。
             */
            if (immediate)
            {
                Vector3 currentPosition =
                    playerObject.transform.position;

                playerObject.transform.position =
                    new Vector3(
                        targetPosition.x,
                        targetPosition.y,
                        currentPosition.z
                    );
            }
        }
        else
        {
            Vector3 currentPosition =
                playerObject.transform.position;

            playerObject.transform.position =
                new Vector3(
                    targetPosition.x,
                    targetPosition.y,
                    currentPosition.z
                );
        }
    }

    private void ApplyOperateState()
    {
        lastAppliedUseState =
            IfInUse;

        /*
         * 进入使用状态时，当前操作者仍然存在，
         * 子类可以读取 CurrentOperatorInput。
         */
        if (IfInUse)
        {
            OnOperateStateChanged(true);
            return;
        }

        /*
         * 退出时先让子类停止使用设施、清除输入绑定，
         * 再释放当前操作者。
         */
        OnOperateStateChanged(false);

        ReleaseCurrentOperator();
    }

    private void ReleaseCurrentOperator()
    {
        PlayerOperateInteractor2D previousOperator =
            CurrentOperatorInteractor;

        CurrentOperatorInteractor =
            null;

        if (previousOperator != null)
        {
            previousOperator.NotifyOperateStopped(
                this
            );
        }
    }

    protected virtual void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerOperateInteractor2D player =
            FindPlayerInteractor(other);

        if (player == null)
        {
            return;
        }

        if (playerColliderCounts.TryGetValue(
                player,
                out int colliderCount))
        {
            playerColliderCounts[player] =
                colliderCount + 1;

            return;
        }

        playerColliderCounts.Add(
            player,
            1
        );

        player.RegisterOperateController(
            this
        );
    }

    protected virtual void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerOperateInteractor2D player =
            FindPlayerInteractor(other);

        if (player == null ||
            !playerColliderCounts.TryGetValue(
                player,
                out int colliderCount))
        {
            return;
        }

        colliderCount--;

        /*
         * 玩家还有其他 Collider 处于范围内。
         */
        if (colliderCount > 0)
        {
            playerColliderCounts[player] =
                colliderCount;

            return;
        }

        playerColliderCounts.Remove(
            player
        );

        player.UnregisterOperateController(
            this
        );

        if (stopWhenOperatorLeaves &&
            player == CurrentOperatorInteractor)
        {
            StopOperate();
        }
    }

    private PlayerOperateInteractor2D FindPlayerInteractor(
        Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        PlayerOperateInteractor2D player =
            other.GetComponentInParent
                <PlayerOperateInteractor2D>();

        if (player == null)
        {
            return null;
        }

        GameObject playerObject =
            player.PlayerObject;

        if (!string.IsNullOrWhiteSpace(playerTag) &&
            (playerObject == null ||
             !playerObject.CompareTag(playerTag)))
        {
            return null;
        }

        return player;
    }

    private void RemoveDestroyedPlayers()
    {
        if (playerColliderCounts.Count == 0)
        {
            return;
        }

        List<PlayerOperateInteractor2D>
            playersToRemove = null;

        foreach (
            KeyValuePair<PlayerOperateInteractor2D, int> pair
            in playerColliderCounts)
        {
            if (pair.Key != null)
            {
                continue;
            }

            if (playersToRemove == null)
            {
                playersToRemove =
                    new List<PlayerOperateInteractor2D>();
            }

            playersToRemove.Add(
                pair.Key
            );
        }

        if (playersToRemove == null)
        {
            return;
        }

        for (int i = 0;
             i < playersToRemove.Count;
             i++)
        {
            playerColliderCounts.Remove(
                playersToRemove[i]
            );
        }

        if (IfInUse &&
            CurrentOperatorInteractor == null)
        {
            SetInUse(false);
        }
    }

    protected virtual void OnDisable()
    {
        foreach (
            KeyValuePair<PlayerOperateInteractor2D, int> pair
            in playerColliderCounts)
        {
            if (pair.Key != null)
            {
                pair.Key.UnregisterOperateController(
                    this
                );
            }
        }

        playerColliderCounts.Clear();

        IfInUse = false;

        /*
         * 禁用时同样先通知子类关闭，
         * 再释放玩家。
         */
        OnOperateStateChanged(false);

        ReleaseCurrentOperator();

        lastAppliedUseState = false;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
        {
            lastAppliedUseState =
                IfInUse;

            OnOperateStateChanged(
                IfInUse
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!syncOperatorPosition)
        {
            return;
        }

        Vector2 syncPosition =
            GetOperatorSyncPosition();

        Gizmos.DrawWireSphere(
            syncPosition,
            0.15f
        );

        Transform syncPoint =
            operatorSyncPoint != null
                ? operatorSyncPoint
                : transform;

        Gizmos.DrawLine(
            syncPoint.position,
            syncPosition
        );
    }
#endif
}