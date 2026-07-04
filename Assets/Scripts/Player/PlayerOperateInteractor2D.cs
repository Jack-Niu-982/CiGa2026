using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家设施交互控制器。
///
/// 负责：
/// 1. 读取该玩家自己的交互键；
/// 2. 记录附近可操作设施；
/// 3. 选择最近的设施开始交互；
/// 4. 将交互开始和结束通知给PlayerController。
///
/// 进入交互后，再次按交互键不会退出。
/// 退出操作由PlayerController检测“长按左或右”完成。
/// </summary>
[DisallowMultipleComponent]
public class PlayerOperateInteractor2D : MonoBehaviour
{
    [Header("玩家组件")]
    [Tooltip("通常会自动获取当前物体上的PlayerController。")]
    [SerializeField]
    private PlayerController playerController;

    [Tooltip(
        "留空时优先读取PlayerController中的CurrentInput，" +
        "也可以手动拖入KeyboardPlayerInput或GamepadPlayerInput。"
    )]
    [SerializeField]
    private PlayerInputBase playerInput;

    private readonly HashSet<OperateController>
        nearbyOperateControllers =
            new HashSet<OperateController>();

    private OperateController
        currentOperateController;

    /// <summary>
    /// 当前正在使用的设施。
    /// </summary>
    public OperateController CurrentOperateController =>
        currentOperateController;

    /// <summary>
    /// 该交互器代表的玩家物体。
    /// </summary>
    public GameObject PlayerObject
    {
        get
        {
            if (playerController != null)
            {
                return playerController.gameObject;
            }

            return gameObject;
        }
    }

    private void Reset()
    {
        playerController =
            GetComponent<PlayerController>();

        playerInput =
            GetComponent<PlayerInputBase>();
    }

    private void Awake()
    {
        if (playerController == null)
        {
            playerController =
                GetComponent<PlayerController>();
        }

        if (playerInput == null)
        {
            playerInput =
                GetComponent<PlayerInputBase>();
        }
    }

    private void Update()
    {
        PlayerInputBase currentInput =
            GetCurrentInput();

        if (currentInput == null ||
            !currentInput.InteractPressed)
        {
            return;
        }

        /*
         * 已经处于交互状态：
         * 再按一次交互键，退出当前操作。
         */
        if (currentOperateController != null)
        {
            TryStopCurrentOperate();
            return;
        }

        /*
         * 当前没有交互：
         * 按交互键尝试操作最近的设施。
         */
        HandleInteractPressed();
    }

    private PlayerInputBase GetCurrentInput()
    {
        if (playerController != null &&
            playerController.CurrentInput != null)
        {
            return playerController.CurrentInput;
        }

        return playerInput;
    }

    private void HandleInteractPressed()
    {
        OperateController closestController =
            GetClosestAvailableController();

        if (closestController != null)
        {
            closestController
                .TryStartOperate(this);
        }
    }

    private OperateController
        GetClosestAvailableController()
    {
        nearbyOperateControllers.RemoveWhere(
            controller => controller == null
        );

        OperateController closestController =
            null;

        float closestSqrDistance =
            float.MaxValue;

        foreach (
            OperateController controller
            in nearbyOperateControllers)
        {
            if (controller == null ||
                !controller.CanPlayerStartOperate(this))
            {
                continue;
            }

            float sqrDistance =
                (
                    controller.transform.position -
                    transform.position
                ).sqrMagnitude;

            if (sqrDistance >=
                closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance =
                sqrDistance;

            closestController =
                controller;
        }

        return closestController;
    }

    /// <summary>
    /// 由OperateController在玩家进入范围时调用。
    /// </summary>
    public void RegisterOperateController(
        OperateController controller)
    {
        if (controller == null)
        {
            return;
        }

        nearbyOperateControllers.Add(
            controller
        );
    }

    /// <summary>
    /// 由OperateController在玩家离开范围时调用。
    /// </summary>
    public void UnregisterOperateController(
        OperateController controller)
    {
        if (controller == null)
        {
            return;
        }

        nearbyOperateControllers.Remove(
            controller
        );
    }

    /// <summary>
    /// 由OperateController在操作开始时调用。
    /// </summary>
    public void NotifyOperateStarted(
        OperateController controller)
    {
        currentOperateController =
            controller;

        if (playerController != null)
        {
            playerController
                .EnterOperatingState(controller);
        }
    }

    /// <summary>
    /// 由OperateController在操作结束时调用。
    /// </summary>
    public void NotifyOperateStopped(
        OperateController controller)
    {
        if (currentOperateController !=
            controller)
        {
            return;
        }

        currentOperateController =
            null;

        if (playerController != null)
        {
            playerController
                .ExitOperatingState(controller);
        }
    }

    /// <summary>
    /// 由PlayerController在长按左或右达到时间后调用。
    /// </summary>
    public bool TryStopCurrentOperate()
    {
        if (currentOperateController == null)
        {
            return false;
        }

        return currentOperateController
            .TryStopOperate(this);
    }

    public void SetPlayerInput(
        PlayerInputBase newInput)
    {
        playerInput = newInput;
    }

    private void OnDisable()
    {
        if (currentOperateController != null &&
            currentOperateController
                .CurrentOperatorInteractor == this)
        {
            currentOperateController
                .StopOperate();
        }

        if (playerController != null)
        {
            playerController
                .ExitOperatingState(
                    currentOperateController
                );
        }

        currentOperateController = null;
        nearbyOperateControllers.Clear();
    }
}