using UnityEngine;

public static class SelectedLevelStore
{
    private static string selectedLevelPath;
    private static LevelTerrainData temporaryLevel;

    public static bool HasSelectedPath =>
        !string.IsNullOrWhiteSpace(selectedLevelPath);

    public static string SelectedLevelPath =>
        selectedLevelPath;

    public static bool HasTemporaryLevel =>
        temporaryLevel != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        selectedLevelPath = string.Empty;
        temporaryLevel = null;
    }

    public static void SetSelectedPath(string path)
    {
        selectedLevelPath = path;
        temporaryLevel = null;
    }

    public static void SetTemporaryLevel(LevelTerrainData data)
    {
        temporaryLevel = data;
        selectedLevelPath = string.Empty;
    }

    public static bool TryGetSelectedLevel(
        out LevelTerrainData data)
    {
        if (temporaryLevel != null)
        {
            temporaryLevel.Normalize();
            data = temporaryLevel;
            return true;
        }

        if (HasSelectedPath &&
            LevelFileService.TryLoadLevel(selectedLevelPath, out data))
        {
            return true;
        }

        data = null;
        return false;
    }

    public static void Clear()
    {
        selectedLevelPath = string.Empty;
        temporaryLevel = null;
    }
}
