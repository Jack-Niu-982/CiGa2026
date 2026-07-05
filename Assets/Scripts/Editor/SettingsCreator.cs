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
        CreateDevSettings(folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("所有默认 Settings 已创建在 " + folder);
    }

    [MenuItem("Tools/Settings/Create Player Settings")]
    public static void CreatePlayerSettings()
    {
        CreatePlayerSettings("Assets/Resources/Settings");
    }

    [MenuItem("Tools/Settings/Create Floating Item Settings")]
    public static void CreateFloatingItemSettings()
    {
        CreateFloatingItemSettings("Assets/Resources/Settings");
    }

    [MenuItem("Tools/Settings/Create Anchor Settings")]
    public static void CreateAnchorSettings()
    {
        CreateAnchorSettings("Assets/Resources/Settings");
    }

    [MenuItem("Tools/Settings/Create Gamepad Settings")]
    public static void CreateGamepadSettings()
    {
        CreateGamepadSettings("Assets/Resources/Settings");
    }

    [MenuItem("Tools/Settings/Create Dev Settings")]
    public static void CreateDevSettings()
    {
        CreateDevSettings("Assets/Resources/Settings");
    }

    private static void CreatePlayerSettings(string folder)
    {
        CreateAsset<PlayerSettings>(folder, "PlayerSettings");
    }

    private static void CreateFloatingItemSettings(string folder)
    {
        CreateAsset<FloatingItemSettings>(folder, "FloatingItemSettings");
    }

    private static void CreateAnchorSettings(string folder)
    {
        CreateAsset<AnchorSettings>(folder, "AnchorSettings");
    }

    private static void CreateGamepadSettings(string folder)
    {
        CreateAsset<GamepadSettings>(folder, "GamepadSettings");
    }

    private static void CreateDevSettings(string folder)
    {
        CreateAsset<DevSettings>(folder, "DevSettings");
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
