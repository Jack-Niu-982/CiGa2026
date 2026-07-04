using UnityEngine;

/// <summary>
/// 锚发射器操作控制器。
///
/// 玩家交互、当前操作者和 IfInUse 状态
/// 由 OperateController 负责。
///
/// 本类负责：
/// 1. 将当前操作者自己的 PlayerInputBase 绑定到锚发射器；
/// 2. 启用或禁用锚发射器相关组件；
/// 3. 玩家退出时解除输入绑定。
///
/// 每个操作台都只绑定自己的当前操作者，
/// 所以多个玩家同时操作不同发射器时不会串输入。
/// </summary>
public class AnchorLauncherUseController2D
    : OperateController
{
    [Header("对应的锚发射器")]
    [Tooltip(
        "将这个控制器对应的单个锚发射器根物体拖到这里。\n" +
        "不要拖入包含所有发射器的共同父物体。"
    )]
    [SerializeField]
    private GameObject anchorLauncherObject;

    [Header("调试")]
    [SerializeField]
    private bool showInputBindingLog = true;

    private AnchorLauncher2D[]
        anchorLaunchers;

    private AnchorRotator[]
        anchorRotators;

    private AnchorLaunchDetector2D[]
        anchorLaunchDetectors;

    protected override void Awake()
    {
        FindLauncherComponents();

        base.Awake();
    }

    /// <summary>
    /// 每次 IfInUse 改变时，由父类自动调用。
    /// </summary>
    protected override void OnOperateStateChanged(
        bool isInUse)
    {
        if (isInUse)
        {
            /*
             * 必须先绑定正确玩家，
             * 再启用 AnchorLauncher2D。
             *
             * 这样发射器开始读取输入时，
             * 已经知道应该读取哪个玩家。
             */
            BindCurrentOperatorInput();

            ApplyUseState(true);
        }
        else
        {
            /*
             * 先停止读取输入，
             * 再解除玩家绑定。
             */
            ApplyUseState(false);

            ClearLauncherInputBindings();
        }
    }

    /// <summary>
    /// 查找该锚发射器及其子物体上的相关组件。
    /// </summary>
    private void FindLauncherComponents()
    {
        if (anchorLauncherObject == null)
        {
            anchorLaunchers = null;
            anchorRotators = null;
            anchorLaunchDetectors = null;

            return;
        }

        anchorLaunchers =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorLauncher2D>(true);

        anchorRotators =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorRotator>(true);

        anchorLaunchDetectors =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorLaunchDetector2D>(true);
    }

    /// <summary>
    /// 将当前操作者自己的输入绑定到发射器。
    /// </summary>
    private void BindCurrentOperatorInput()
    {
        EnsureLauncherComponents();

        PlayerInputBase operatorInput =
            CurrentOperatorInput;

        if (operatorInput == null)
        {
            Debug.LogWarning(
                $"[AnchorLauncherUseController2D] {gameObject.name} " +
                "进入操作状态，但当前玩家没有可用的 PlayerInputBase。"
            );
        }

        if (anchorLaunchers == null)
        {
            return;
        }

        for (int i = 0;
             i < anchorLaunchers.Length;
             i++)
        {
            AnchorLauncher2D launcher =
                anchorLaunchers[i];

            if (launcher == null)
            {
                continue;
            }

            launcher.SetPlayerInput(
                operatorInput
            );
        }

        if (showInputBindingLog &&
            operatorInput != null)
        {
            string operatorName =
                CurrentOperator != null
                    ? CurrentOperator.name
                    : operatorInput.gameObject.name;

            Debug.Log(
                $"[AnchorLauncherUseController2D] " +
                $"{gameObject.name} 已绑定玩家 {operatorName} 的输入。"
            );
        }
    }

    /// <summary>
    /// 清除该操作台管理的发射器输入绑定。
    /// </summary>
    private void ClearLauncherInputBindings()
    {
        EnsureLauncherComponents();

        if (anchorLaunchers == null)
        {
            return;
        }

        /*
         * 在 OperateController 释放 CurrentOperator 以前，
         * CurrentOperatorInput 仍然是退出玩家自己的输入。
         */
        PlayerInputBase previousOperatorInput =
            CurrentOperatorInput;

        for (int i = 0;
             i < anchorLaunchers.Length;
             i++)
        {
            AnchorLauncher2D launcher =
                anchorLaunchers[i];

            if (launcher == null)
            {
                continue;
            }

            /*
             * 传入退出玩家的输入进行身份检查。
             *
             * 防止旧玩家退出时，
             * 意外清除后来绑定的新玩家。
             */
            launcher.ClearPlayerInput(
                previousOperatorInput
            );
        }
    }

    /// <summary>
    /// 手动刷新使用状态。
    /// </summary>
    public void ApplyUseState()
    {
        OnOperateStateChanged(
            IfInUse
        );
    }

    /// <summary>
    /// 启用或禁用发射器的操作组件。
    /// </summary>
    private void ApplyUseState(
        bool isInUse)
    {
        if (anchorLauncherObject == null)
        {
            return;
        }

        EnsureLauncherComponents();

        SetComponentsEnabled(
            anchorLaunchers,
            isInUse
        );

        SetComponentsEnabled(
            anchorRotators,
            isInUse
        );

        SetComponentsEnabled(
            anchorLaunchDetectors,
            isInUse
        );
    }

    private void EnsureLauncherComponents()
    {
        if (
            anchorLaunchers == null ||
            anchorRotators == null ||
            anchorLaunchDetectors == null
        )
        {
            FindLauncherComponents();
        }
    }

    private void SetComponentsEnabled<T>(
        T[] components,
        bool value)
        where T : Behaviour
    {
        if (components == null)
        {
            return;
        }

        for (int i = 0;
             i < components.Length;
             i++)
        {
            if (components[i] != null)
            {
                components[i].enabled =
                    value;
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        FindLauncherComponents();

        base.OnValidate();
    }
#endif
}