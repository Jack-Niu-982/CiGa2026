using UnityEngine;

/// <summary>
/// 玩家输入基类。
/// PlayerController只读取统一输入结果，
/// 不需要关心输入来自键盘还是手柄。
/// </summary>
public abstract class PlayerInputBase : MonoBehaviour
{
    /// <summary>
    /// 水平输入，范围为-1到1。
    /// </summary>
    public float Horizontal { get; protected set; }

    /// <summary>
    /// 垂直输入，范围为-1到1。
    /// </summary>
    public float Vertical { get; protected set; }

    /// <summary>
    /// 本帧是否刚刚按下“上”。
    /// </summary>
    public bool UpPressed { get; protected set; }

    /// <summary>
    /// 当前是否持续按住“上”。
    /// </summary>
    public bool UpHeld { get; protected set; }

    /// <summary>
    /// 本帧是否刚刚按下“下”。
    /// </summary>
    public bool DownPressed { get; protected set; }

    /// <summary>
    /// 当前是否持续按住“下”。
    /// </summary>
    public bool DownHeld { get; protected set; }

    /// <summary>
    /// 本帧是否刚刚按下交互键。
    /// </summary>
    public bool InteractPressed { get; protected set; }

    /// <summary>
    /// 当前是否持续按住交互键。
    /// </summary>
    public bool InteractHeld { get; protected set; }

    /// <summary>
    /// 更新玩家输入状态。
    /// </summary>
    protected void SetInputState(
        float horizontal,
        float vertical,
        bool upHeld,
        bool downHeld,
        bool interactHeld)
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

        // 必须先用上一帧的Held状态计算Pressed。
        UpPressed =
            upHeld &&
            !UpHeld;

        DownPressed =
            downHeld &&
            !DownHeld;

        InteractPressed =
            interactHeld &&
            !InteractHeld;

        UpHeld = upHeld;
        DownHeld = downHeld;
        InteractHeld = interactHeld;
    }

    /// <summary>
    /// 保留旧版调用方式。
    /// 其他输入脚本即使没有交互键也不会报错。
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
            false
        );
    }

    /// <summary>
    /// 清空所有输入状态。
    /// </summary>
    protected void ClearInputState()
    {
        SetInputState(
            0f,
            0f,
            false,
            false,
            false
        );
    }

    protected virtual void OnDisable()
    {
        ClearInputState();
    }
}