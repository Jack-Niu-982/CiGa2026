/// <summary>
/// 船体生命值变化事件。当炸弹爆炸或其他伤害源影响船体时触发。
/// </summary>
public struct SubmarineHealthChangedEvent
{
    /// <summary>
    /// 伤害量（正数为扣血，负数为恢复）。
    /// </summary>
    public float DamageAmount;

    /// <summary>
    /// 伤害来源（如 "Bomb", "Collision" 等）。
    /// </summary>
    public string Source;
}
