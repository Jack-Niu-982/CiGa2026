using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gameplay 一局的胜负状态机。
/// </summary>
[DisallowMultipleComponent]
public class MissionSettlementController : MonoBehaviour
{
    [Serializable]
    public class MissionSettledEvent
        : UnityEvent<MissionSettlementState>
    {
    }

    [Header("依赖")]
    [SerializeField]
    private SubmarineHealth2D submarineHealth;

    [SerializeField]
    private Rigidbody2D submarineRigidbody;

    [Header("结算表现")]
    [SerializeField]
    private bool stopSubmarineOnSettlement = true;

    [SerializeField]
    private bool pauseTimeOnSettlement;

    [Header("事件")]
    [SerializeField]
    private MissionSettledEvent onMissionSettled =
        new MissionSettledEvent();

    private MissionSettlementState currentState =
        MissionSettlementState.Running;

    private float timeScaleBeforePause = 1f;

    public event Action<MissionSettlementState> MissionSettled;

    public MissionSettlementState CurrentState => currentState;
    public bool IsSettled =>
        currentState != MissionSettlementState.Running;
    public MissionSettledEvent OnMissionSettled =>
        onMissionSettled;

    private void Reset()
    {
        submarineHealth =
            FindObjectOfType<SubmarineHealth2D>();

        if (submarineHealth != null)
        {
            submarineRigidbody =
                submarineHealth.GetComponent<Rigidbody2D>();
        }
    }

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();

        if (submarineHealth != null)
        {
            submarineHealth.Depleted +=
                HandleSubmarineDepleted;
        }
    }

    private void OnDisable()
    {
        if (submarineHealth != null)
        {
            submarineHealth.Depleted -=
                HandleSubmarineDepleted;
        }

        if (pauseTimeOnSettlement &&
            Mathf.Approximately(Time.timeScale, 0f))
        {
            Time.timeScale =
                timeScaleBeforePause;
        }
    }

    public bool Win()
    {
        return Settle(
            MissionSettlementState.Won,
            "飞船抵达终点，行动成功。"
        );
    }

    public bool Fail()
    {
        return Settle(
            MissionSettlementState.Failed,
            "飞船血量归零，行动失败。"
        );
    }

    private void HandleSubmarineDepleted(
        SubmarineHealth2D depletedHealth)
    {
        Fail();
    }

    private bool Settle(
        MissionSettlementState newState,
        string message)
    {
        if (newState == MissionSettlementState.Running ||
            IsSettled)
        {
            return false;
        }

        currentState = newState;

        if (stopSubmarineOnSettlement &&
            submarineRigidbody != null)
        {
            submarineRigidbody.velocity =
                Vector2.zero;

            submarineRigidbody.angularVelocity =
                0f;
        }

        if (pauseTimeOnSettlement)
        {
            timeScaleBeforePause =
                Time.timeScale;

            Time.timeScale = 0f;
        }

        Debug.Log(
            message,
            this
        );

        PublishSettlementResultEvent(currentState);

        MissionSettled?.Invoke(currentState);
        onMissionSettled.Invoke(currentState);

        return true;
    }

    private void PublishSettlementResultEvent(
        MissionSettlementState settledState)
    {
        if (settledState == MissionSettlementState.Won)
        {
            GameplayEventBus.Publish(
                new MissionVictoryEvent(
                    this,
                    settledState
                )
            );

            return;
        }

        if (settledState == MissionSettlementState.Failed)
        {
            GameplayEventBus.Publish(
                new MissionFailureEvent(
                    this,
                    settledState
                )
            );
        }
    }

    private void ResolveDependencies()
    {
        if (submarineHealth == null)
        {
            submarineHealth =
                FindObjectOfType<SubmarineHealth2D>();
        }

        if (submarineRigidbody == null &&
            submarineHealth != null)
        {
            submarineRigidbody =
                submarineHealth.GetComponent<Rigidbody2D>();
        }
    }
}
