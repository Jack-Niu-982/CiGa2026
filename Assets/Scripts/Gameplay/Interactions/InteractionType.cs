/// <summary>
/// 可交互站点类型。
/// </summary>
public enum InteractionType
{
    None = 0,

    /// <summary>
    /// 火炉 - 用于烹饪食物
    /// </summary>
    Stove,

    /// <summary>
    /// 防御站 - 用于射击或防御
    /// </summary>
    DefenseStation,

    /// <summary>
    /// 修理台 - 用于修理船体
    /// </summary>
    RepairStation,

    /// <summary>
    /// 储物箱 - 用于存储物品
    /// </summary>
    StorageChest,

    /// <summary>
    /// 锚发射器 - 已有的操作台
    /// </summary>
    AnchorLauncher,

    /// <summary>
    /// 工作台 - 用于制作物品
    /// </summary>
    Workbench,

    /// <summary>
    /// 舵 - 用于控制船只方向
    /// </summary>
    Helm
}
