using UnityEngine;

/// <summary>
/// 玩家输入基类。
/// PlayerController 和可操作设施只读取统一输入结果，
/// 不需要关心输入来自键盘还是手柄。
/// </summary>
public abstract class PlayerInputBase : MonoBehaviour
{
    public float Horizontal { get; protected set; }
    public float Vertical { get; protected set; }

    public bool UpPressed { get; protected set; }
    public bool UpHeld { get; protected set; }

    public bool DownPressed { get; protected set; }
    public bool DownHeld { get; protected set; }

    public bool InteractPressed { get; protected set; }
    public bool InteractHeld { get; protected set; }

    /// <summary>
    /// 本帧是否刚按下船锚发射键。
    /// 键盘默认 Q，手柄默认 R1。
    /// </summary>
    public bool AnchorShootPressed { get; protected set; }

    public bool AnchorShootHeld { get; protected set; }

    /// <summary>
    /// 本帧是否刚按下船锚回收键。
    /// 键盘默认 R，手柄默认 South。
    /// </summary>
    public bool AnchorRetractPressed { get; protected set; }

    public bool AnchorRetractHeld { get; protected set; }

    /// <summary>
    /// 当前是否正在主动收紧绳子。
    ///
    /// 键盘：
    /// 按住 E。
    ///
    /// 手柄：
    /// 持续转动右摇杆。
    /// </summary>
    public bool AnchorReelHeld { get; protected set; }

    /// <summary>
    /// 当前收绳输入强度，范围为 0 到 1。
    ///
    /// 键盘按住 E 时为 1。
    /// 手柄根据右摇杆转动速度计算。
    /// </summary>
    public float AnchorReelAmount { get; protected set; }

    /// <summary>
    /// 更新全部玩家输入状态。
    /// </summary>
    protected void SetInputState(
        float horizontal,
        float vertical,
        bool upHeld,
        bool downHeld,
        bool interactHeld,
        bool anchorShootHeld,
        bool anchorRetractHeld,
        float anchorReelAmount)
    {
        Horizontal = Mathf.Clamp(
            horizontal,
            -1f,
            1f
        );

        Vertical = Mathf.Clamp(
            vertical,
            -1f,
            1f
        );

        UpPressed =
            upHeld &&
            !UpHeld;

        DownPressed =
            downHeld &&
            !DownHeld;

        InteractPressed =
            interactHeld &&
            !InteractHeld;

        AnchorShootPressed =
            anchorShootHeld &&
            !AnchorShootHeld;

        AnchorRetractPressed =
            anchorRetractHeld &&
            !AnchorRetractHeld;

        UpHeld = upHeld;
        DownHeld = downHeld;
        InteractHeld = interactHeld;

        AnchorShootHeld = anchorShootHeld;
        AnchorRetractHeld = anchorRetractHeld;

        AnchorReelAmount =
            Mathf.Clamp01(
                anchorReelAmount
            );

        AnchorReelHeld =
            AnchorReelAmount > 0.01f;
    }

    /// <summary>
    /// 保留 bool 形式，兼容其他已有输入脚本。
    /// </summary>
    protected void SetInputState(
        float horizontal,
        float vertical,
        bool upHeld,
        bool downHeld,
        bool interactHeld,
        bool anchorShootHeld,
        bool anchorRetractHeld,
        bool anchorReelHeld)
    {
        SetInputState(
            horizontal,
            vertical,
            upHeld,
            downHeld,
            interactHeld,
            anchorShootHeld,
            anchorRetractHeld,
            anchorReelHeld ? 1f : 0f
        );
    }

    /// <summary>
    /// 保留旧版五参数调用方式。
    /// </summary>
    protected void SetInputState(
        float horizontal,
        float vertical,
        bool upHeld,
        bool downHeld,
        bool interactHeld)
    {
        SetInputState(
            horizontal,
            vertical,
            upHeld,
            downHeld,
            interactHeld,
            false,
            false,
            0f
        );
    }

    /// <summary>
    /// 保留旧版四参数调用方式。
    /// </summary>
    protected void SetInputState(
        float horizontal,
        float vertical,
        bool upHeld,
        bool downHeld)
    {
        SetInputState(
            horizontal,
            vertical,
            upHeld,
            downHeld,
            false,
            false,
            false,
            0f
        );
    }

    /// <summary>
    /// 由正在使用该输入的船锚通知当前操作状态。
    ///
    /// 键盘输入不需要处理。
    /// 手柄输入会用它限制右摇杆收绳和震动。
    /// </summary>
    public virtual void SetAnchorControlFeedback(
        bool isControllingAnchor,
        bool anchorIsAttached)
    {
    }

    /// <summary>
    /// 船锚成功发射时的反馈。
    /// </summary>
    public virtual void PlayAnchorShootFeedback()
    {
    }

    /// <summary>
    /// 船锚开始回收时的反馈。
    /// </summary>
    public virtual void PlayAnchorRetractFeedback()
    {
    }

    protected void ClearInputState()
    {
        SetInputState(
            0f,
            0f,
            false,
            false,
            false,
            false,
            false,
            0f
        );
    }

    protected virtual void OnDisable()
    {
        ClearInputState();
    }
}