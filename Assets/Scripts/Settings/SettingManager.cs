using UnityEngine;

/// <summary>
/// 全局配置管理入口。通过 Resources.Load 懒加载各 Settings SO。
/// 兼容 Domain Reload 关闭模式。
/// </summary>
public static class SettingManager
{
    public static PlayerSettings Player =>
        Get(ref _player, "PlayerSettings");

    public static FloatingItemSettings FloatingItem =>
        Get(ref _floatingItem, "FloatingItemSettings");

    public static AnchorSettings Anchor =>
        Get(ref _anchor, "AnchorSettings");

    public static GamepadSettings Gamepad =>
        Get(ref _gamepad, "GamepadSettings");

    public static DevSettings Dev =>
        Get(ref _dev, "DevSettings");

    public static TerrainSettings Terrain =>
        Get(ref _terrain, "TerrainSettings");

    public static SceneSettings Scene =>
        Get(ref _scene, "SceneSettings");

    private static PlayerSettings _player;
    private static FloatingItemSettings _floatingItem;
    private static AnchorSettings _anchor;
    private static GamepadSettings _gamepad;
    private static DevSettings _dev;
    private static TerrainSettings _terrain;
    private static SceneSettings _scene;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _player = null;
        _floatingItem = null;
        _anchor = null;
        _gamepad = null;
        _dev = null;
        _terrain = null;
        _scene = null;
    }

    private static T Get<T>(ref T cached, string assetName)
        where T : ScriptableObject
    {
        if (cached == null)
        {
            cached = Resources.Load<T>(
                "Settings/" + assetName
            );
        }

        return cached;
    }
}
