using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LevelFileService
{
    public const string LevelFolderName = "Levels";
    public const string JsonExtension = ".json";

    public static string BuiltInLevelFolder =>
        Path.Combine(Application.streamingAssetsPath, LevelFolderName);

    public static string UserLevelFolder =>
        Path.Combine(Application.persistentDataPath, LevelFolderName);

    public static LevelTerrainData CreateDefaultLevel()
    {
        LevelTerrainData data =
            LevelTerrainData.CreateSolidRectangle(
                "default_level",
                "Default Level",
                256,
                128,
                4
            );

        CarveDebugTunnel(data);
        return data;
    }

    public static bool TryLoadLevel(
        string path,
        out LevelTerrainData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<LevelTerrainData>(json);

            if (data == null)
            {
                return false;
            }

            data.Normalize();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[LevelFileService] Failed to load level: {path}\n{exception.Message}"
            );

            data = null;
            return false;
        }
    }

    public static LevelTerrainData LoadOrCreateDefault()
    {
        string builtInPath =
            Path.Combine(
                BuiltInLevelFolder,
                "default_level" + JsonExtension
            );

        if (TryLoadLevel(builtInPath, out LevelTerrainData data))
        {
            return data;
        }

        return CreateDefaultLevel();
    }

    public static bool SaveUserLevel(
        LevelTerrainData data,
        string fileName,
        out string savedPath)
    {
        savedPath = string.Empty;

        if (data == null)
        {
            return false;
        }

        Directory.CreateDirectory(UserLevelFolder);

        string safeName =
            SanitizeFileName(
                string.IsNullOrWhiteSpace(fileName)
                    ? data.id
                    : fileName
            );

        if (!safeName.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            safeName += JsonExtension;
        }

        savedPath = Path.Combine(UserLevelFolder, safeName);
        return SaveLevel(data, savedPath);
    }

    public static bool SaveBuiltInDefaultLevel(
        LevelTerrainData data,
        out string savedPath)
    {
        savedPath =
            Path.Combine(
                BuiltInLevelFolder,
                "default_level" + JsonExtension
            );

        data.id = "default_level";
        data.displayName = "Default Level";

        bool saved =
            SaveLevel(data, savedPath);

#if UNITY_EDITOR
        if (saved)
        {
            AssetDatabase.ImportAsset("Assets/StreamingAssets/Levels/default_level.json");
        }
#endif

        return saved;
    }

    public static bool SaveLevel(
        LevelTerrainData data,
        string path)
    {
        if (data == null ||
            string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            data.Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[LevelFileService] Failed to save level: {path}\n{exception.Message}"
            );

            return false;
        }
    }

    public static List<string> GetAllLevelPaths()
    {
        List<string> paths = new List<string>();
        AddLevelPaths(BuiltInLevelFolder, paths);
        AddLevelPaths(UserLevelFolder, paths);
        return paths;
    }

    public static bool TryDeleteUserLevel(string path)
    {
        if (!IsUserLevelPath(path) ||
            !File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public static bool IsUserLevelPath(string path)
    {
        string fullPath =
            Path.GetFullPath(path ?? string.Empty)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string userFolder =
            Path.GetFullPath(UserLevelFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.StartsWith(userFolder, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetLevelListLabel(string path)
    {
        string name =
            Path.GetFileNameWithoutExtension(path);

        string source =
            IsUserLevelPath(path) ? "User" : "Built-in";

        return $"{name} [{source}]";
    }

    public static string SanitizeFileName(string fileName)
    {
        string value =
            string.IsNullOrWhiteSpace(fileName)
                ? "untitled_level"
                : fileName.Trim();

        char[] invalid = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalid.Length; i++)
        {
            value = value.Replace(invalid[i], '_');
        }

        return value;
    }

    private static void AddLevelPaths(
        string folder,
        List<string> paths)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            !Directory.Exists(folder))
        {
            return;
        }

        string[] files =
            Directory.GetFiles(
                folder,
                "*" + JsonExtension,
                SearchOption.TopDirectoryOnly
            );

        for (int i = 0; i < files.Length; i++)
        {
            paths.Add(files[i]);
        }
    }

    private static void CarveDebugTunnel(LevelTerrainData data)
    {
        byte[] densities = data.GetDensities();
        float centerY = data.height * 0.5f;
        float radius = data.height * 0.12f;

        for (int y = 1; y < data.height - 1; y++)
        {
            for (int x = 1; x < data.width - 1; x++)
            {
                float wave =
                    Mathf.Sin(x * 0.045f) * data.height * 0.12f;

                float dy = y - centerY - wave;

                if (Mathf.Abs(dy) <= radius)
                {
                    densities[data.ToIndex(x, y)] =
                        LevelTerrainData.EmptyDensity;
                }
            }
        }

        data.SetDensities(densities);
    }
}
