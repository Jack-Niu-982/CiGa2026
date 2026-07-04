using UnityEngine;

/// <summary>
/// 锚发射器操作控制器。
///
/// 玩家交互和IfInUse状态由OperateController负责。
/// 本类只负责启用或禁用锚发射器组件。
/// </summary>
public class AnchorLauncherUseController2D
    : OperateController
{
    [Header("对应的锚发射器")]
    [Tooltip(
        "将这个控制器对应的锚发射器根物体拖到这里。"
    )]
    [SerializeField]
    private GameObject anchorLauncherObject;

    private AnchorLauncher2D[]
        anchorLaunchers;

    private AnchorRotator[]
        shooterRotators;

    private AnchorLaunchDetector2D[]
        anchorLaunchDetectors;

    protected override void Awake()
    {
        FindLauncherComponents();

        base.Awake();
    }

    /// <summary>
    /// 每次IfInUse改变时，由父类自动调用。
    /// </summary>
    protected override void OnOperateStateChanged(
        bool isInUse)
    {
        ApplyUseState(
            isInUse
        );
    }

    /// <summary>
    /// 查找锚发射器及其子物体上的相关组件。
    /// </summary>
    private void FindLauncherComponents()
    {
        if (anchorLauncherObject == null)
        {
            anchorLaunchers = null;
            shooterRotators = null;
            anchorLaunchDetectors = null;

            return;
        }

        anchorLaunchers =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorLauncher2D>(true);

        shooterRotators =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorRotator>(true);

        anchorLaunchDetectors =
            anchorLauncherObject
                .GetComponentsInChildren
                    <AnchorLaunchDetector2D>(true);
    }

    /// <summary>
    /// 手动刷新使用状态。
    /// </summary>
    public void ApplyUseState()
    {
        ApplyUseState(
            IfInUse
        );
    }

    private void ApplyUseState(
        bool isInUse)
    {
        if (anchorLauncherObject == null)
        {
            return;
        }

        if (
            anchorLaunchers == null ||
            shooterRotators == null ||
            anchorLaunchDetectors == null
        )
        {
            FindLauncherComponents();
        }

        SetComponentsEnabled(
            anchorLaunchers,
            isInUse
        );

        SetComponentsEnabled(
            shooterRotators,
            isInUse
        );

        SetComponentsEnabled(
            anchorLaunchDetectors,
            isInUse
        );
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

        for (
            int i = 0;
            i < components.Length;
            i++
        )
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