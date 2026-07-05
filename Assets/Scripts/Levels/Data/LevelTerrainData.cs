using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelTerrainData
{
    public const int CurrentVersion = 1;
    public const byte EmptyDensity = 0;
    public const byte SolidDensity = 255;
    public const byte DefaultSolidThreshold = 128;

    public int version = CurrentVersion;
    public string id = "untitled_level";
    public string displayName = "Untitled Level";
    public int width = 256;
    public int height = 128;
    public int samplesPerUnit = 4;
    public string densitiesBase64;
    public LevelPoseData start;
    public LevelPoseData finish;
    public List<LevelMarkerData> markers =
        new List<LevelMarkerData>();

    [NonSerialized] private byte[] densityCache;

    public int DensityCount =>
        Mathf.Max(0, width) * Mathf.Max(0, height);

    public float CellSize =>
        samplesPerUnit > 0 ? 1f / samplesPerUnit : 1f;

    public Vector2 SizeWorld =>
        new Vector2(width * CellSize, height * CellSize);

    public static LevelTerrainData CreateSolidRectangle(
        string levelId,
        string levelName,
        int width,
        int height,
        int samplesPerUnit)
    {
        LevelTerrainData data = new LevelTerrainData
        {
            version = CurrentVersion,
            id = string.IsNullOrWhiteSpace(levelId)
                ? "untitled_level"
                : levelId,
            displayName = string.IsNullOrWhiteSpace(levelName)
                ? "Untitled Level"
                : levelName,
            width = Mathf.Max(2, width),
            height = Mathf.Max(2, height),
            samplesPerUnit = Mathf.Max(1, samplesPerUnit),
            markers = new List<LevelMarkerData>()
        };

        byte[] densities = new byte[data.DensityCount];

        for (int i = 0; i < densities.Length; i++)
        {
            densities[i] = SolidDensity;
        }

        float centerY =
            data.height * data.CellSize * 0.5f;

        data.start =
            new LevelPoseData(data.CellSize * 8f, centerY, 0f);
        data.finish =
            new LevelPoseData(
                (data.width - 8f) * data.CellSize,
                centerY,
                0f
            );

        data.SetDensities(densities);
        return data;
    }

    public byte[] GetDensities()
    {
        if (densityCache != null &&
            densityCache.Length == DensityCount)
        {
            return densityCache;
        }

        if (string.IsNullOrWhiteSpace(densitiesBase64))
        {
            densityCache = new byte[DensityCount];
            FillDensityCache(SolidDensity);
            return densityCache;
        }

        try
        {
            byte[] decoded =
                Convert.FromBase64String(densitiesBase64);

            densityCache = new byte[DensityCount];
            int count = Mathf.Min(decoded.Length, densityCache.Length);
            Array.Copy(decoded, densityCache, count);

            for (int i = count; i < densityCache.Length; i++)
            {
                densityCache[i] = SolidDensity;
            }
        }
        catch (FormatException)
        {
            densityCache = new byte[DensityCount];
            FillDensityCache(SolidDensity);
        }

        return densityCache;
    }

    public void SetDensities(byte[] densities)
    {
        densityCache = new byte[DensityCount];

        if (densities != null)
        {
            int count = Mathf.Min(densities.Length, densityCache.Length);
            Array.Copy(densities, densityCache, count);

            for (int i = count; i < densityCache.Length; i++)
            {
                densityCache[i] = SolidDensity;
            }
        }
        else
        {
            FillDensityCache(SolidDensity);
        }

        densitiesBase64 =
            Convert.ToBase64String(densityCache);
    }

    public bool TryGetDensity(
        int x,
        int y,
        out byte density)
    {
        if (!IsInside(x, y))
        {
            density = SolidDensity;
            return false;
        }

        density = GetDensities()[ToIndex(x, y)];
        return true;
    }

    public void SetDensity(int x, int y, byte density)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        GetDensities()[ToIndex(x, y)] = density;
        densitiesBase64 =
            Convert.ToBase64String(densityCache);
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 &&
               y >= 0 &&
               x < width &&
               y < height;
    }

    public bool IsSolid(
        int x,
        int y,
        byte threshold = DefaultSolidThreshold)
    {
        if (!TryGetDensity(x, y, out byte density))
        {
            return true;
        }

        return density >= threshold;
    }

    public int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    public Vector2 SampleToWorldCenter(int x, int y)
    {
        return new Vector2(
            (x + 0.5f) * CellSize,
            (y + 0.5f) * CellSize
        );
    }

    public Vector2Int WorldToSample(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / CellSize),
            Mathf.FloorToInt(worldPosition.y / CellSize)
        );
    }

    public void Normalize()
    {
        version = CurrentVersion;
        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);
        samplesPerUnit = Mathf.Max(1, samplesPerUnit);

        if (markers == null)
        {
            markers = new List<LevelMarkerData>();
        }

        SetDensities(GetDensities());
    }

    private void FillDensityCache(byte value)
    {
        for (int i = 0; i < densityCache.Length; i++)
        {
            densityCache[i] = value;
        }

        densitiesBase64 =
            Convert.ToBase64String(densityCache);
    }
}
