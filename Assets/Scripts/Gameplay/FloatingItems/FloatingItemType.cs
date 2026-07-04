/// <summary>
/// 漂浮物类型。定义船外可被锚钩捕获的所有漂浮物。
/// </summary>
public enum FloatingItemType
{
    Unknown = 0,

    /// <summary>
    /// 燃料 - 勾回后生成可拾取的燃料
    /// </summary>
    Fuel = 1,

    /// <summary>
    /// 炸弹 - 勾回后直接对船体造成伤害
    /// </summary>
    Bomb = 2,

    /// <summary>
    /// 垃圾 - 勾回后生成可拾取的垃圾
    /// </summary>
    Trash = 3,

    /// <summary>
    /// 护盾 - 勾回后生成可拾取的护盾
    /// </summary>
    Shield = 4,

    /// <summary>
    /// 蛛网 - 勾回后禁用对应的锚点若干秒
    /// </summary>
    Web = 5
}
