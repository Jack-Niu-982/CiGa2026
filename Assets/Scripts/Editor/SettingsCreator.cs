using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 编辑器工具：快速创建默认 Settings SO 资产。
/// </summary>
public static class SettingsCreator
{
    [MenuItem("Tools/Settings/Create All Default Settings")]
    public static void CreateAllDefaultSettings()
    {
        string folder = "Assets/Resources/Settings";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Settings");
        }

        CreatePlayerSettings(folder);
        CreateFloatingItemSettings(folder);
        CreateAnchorSettings(folder);
        CreateGamepadSettings(folder);
        CreateStationSettings(folder);
        CreateDevSettings(folder);
        CreateCarryableItemArtSettings(folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("所有默认 Settings 已创建在 " + folder);
    }

    [MenuItem("Tools/Settings/Create Player Settings")]
    public static void CreatePlayerSettingsMenu()
    {
        CreatePlayerSettings();
    }

    public static void CreatePlayerSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<PlayerSettings>(folder, "PlayerSettings");
    }

    [MenuItem("Tools/Settings/Create Floating Item Settings")]
    public static void CreateFloatingItemSettingsMenu()
    {
        CreateFloatingItemSettings();
    }

    public static void CreateFloatingItemSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<FloatingItemSettings>(folder, "FloatingItemSettings");
    }

    [MenuItem("Tools/Settings/Create Anchor Settings")]
    public static void CreateAnchorSettingsMenu()
    {
        CreateAnchorSettings();
    }

    public static void CreateAnchorSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<AnchorSettings>(folder, "AnchorSettings");
    }

    [MenuItem("Tools/Settings/Create Gamepad Settings")]
    public static void CreateGamepadSettingsMenu()
    {
        CreateGamepadSettings();
    }

    public static void CreateGamepadSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<GamepadSettings>(folder, "GamepadSettings");
    }

    [MenuItem("Tools/Settings/Create Station Settings")]
    public static void CreateStationSettingsMenu()
    {
        CreateStationSettings();
    }

    public static void CreateStationSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<StationSettings>(folder, "StationSettings");
    }

    [MenuItem("Tools/Settings/Create Dev Settings")]
    public static void CreateDevSettingsMenu()
    {
        CreateDevSettings();
    }

    public static void CreateDevSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<DevSettings>(folder, "DevSettings");
    }

    [MenuItem("Tools/Settings/Create Carryable Item Art Settings")]
    public static void CreateCarryableItemArtSettingsMenu()
    {
        CreateCarryableItemArtSettings();
    }

    public static void CreateCarryableItemArtSettings(string folder = "Assets/Resources/Settings")
    {
        CreateAsset<CarryableItemArtSettings>(folder, "CarryableItemArtSettings");
    }

    private static void CreateAsset<T>(string folder, string fileName) where T : ScriptableObject
    {
        string path = $"{folder}/{fileName}.asset";

        if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
        {
            Debug.LogWarning($"{fileName} 已存在，跳过创建。");
            return;
        }

        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"已创建 {fileName} 于 {path}");
    }
}
