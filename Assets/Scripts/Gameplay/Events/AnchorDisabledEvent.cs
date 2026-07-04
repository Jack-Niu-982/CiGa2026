/// <summary>
/// 锚点被禁用事件。当蛛网捕获锚点时触发。
/// </summary>
public struct AnchorDisabledEvent
{
    /// <summary>
    /// 被禁用的锚点。
    /// </summary>
    public AnchorLauncher2D Anchor;

    /// <summary>
    /// 禁用持续时间（秒）。
    /// </summary>
    public float Duration;
}
