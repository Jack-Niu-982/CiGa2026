/// <summary>
/// 玩家可以手持的物品类型。
/// 后续交互节点只需要读这个类型，就能判断物品用途。
/// </summary>
public enum CarryableItemType
{
    Unknown = 0,
    Fuel = 1,
    Shield = 2,
    Trash = 3
}
