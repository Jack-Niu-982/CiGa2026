/// <summary>
/// 未来需要读取或消费玩家手持物的交互节点可以实现这个接口。
/// 当前只定义契约，不接燃料、护盾等具体玩法效果。
/// </summary>
public interface ICarryItemReceiver
{
    bool CanReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item);

    bool TryReceiveCarryItem(
        PlayerCarryInteractor2D holder,
        CarryableItem2D item);
}
