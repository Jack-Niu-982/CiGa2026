using UnityEngine;

public sealed class LevelBrushTool
{
    public float Radius = 4f;
    public float Hardness = 0.75f;
    public float Strength = 1f;
    public float StampSpacingScale = 0.35f;
    public LevelBrushMode Mode = LevelBrushMode.Dig;

    private bool hasLastStamp;
    private Vector2 lastStampWorld;

    public void BeginStroke(
        Vector2 worldPosition,
        LevelTerrainData data)
    {
        hasLastStamp = false;
        Apply(worldPosition, true, data);
    }

    public void ContinueStroke(
        Vector2 worldPosition,
        LevelTerrainData data)
    {
        Apply(worldPosition, false, data);
    }

    public void EndStroke()
    {
        hasLastStamp = false;
    }

    public void Apply(
        Vector2 worldPosition,
        bool force,
        LevelTerrainData data)
    {
        if (data == null)
        {
            return;
        }

        if (!force && hasLastStamp)
        {
            float spacing =
                Mathf.Max(0.05f, Radius * StampSpacingScale);

            float distance =
                Vector2.Distance(lastStampWorld, worldPosition);

            if (distance > spacing)
            {
                int steps = Mathf.CeilToInt(distance / spacing);

                for (int i = 1; i <= steps; i++)
                {
                    Vector2 interpolated =
                        Vector2.Lerp(
                            lastStampWorld,
                            worldPosition,
                            (float)i / steps
                        );

                    Stamp(interpolated, data);
                }

                lastStampWorld = worldPosition;
                hasLastStamp = true;
                return;
            }
        }

        Stamp(worldPosition, data);
        lastStampWorld = worldPosition;
        hasLastStamp = true;
    }

    public void Stamp(Vector2 worldPosition, LevelTerrainData data)
    {
        if (data == null)
        {
            return;
        }

        if (Mode == LevelBrushMode.PlaceStart)
        {
            data.start =
                new LevelPoseData(worldPosition.x, worldPosition.y, 0f);
            return;
        }

        if (Mode == LevelBrushMode.PlaceFinish)
        {
            data.finish =
                new LevelPoseData(worldPosition.x, worldPosition.y, 0f);
            return;
        }

        byte[] densities = data.GetDensities();
        float radius = Mathf.Max(0.01f, Radius);
        float cellSize = data.CellSize;
        Vector2Int center = data.WorldToSample(worldPosition);
        int sampleRadius = Mathf.CeilToInt(radius / cellSize);

        for (int y = center.y - sampleRadius;
             y <= center.y + sampleRadius;
             y++)
        {
            for (int x = center.x - sampleRadius;
                 x <= center.x + sampleRadius;
                 x++)
            {
                if (!data.IsInside(x, y))
                {
                    continue;
                }

                Vector2 sampleWorld = data.SampleToWorldCenter(x, y);
                float distance =
                    Vector2.Distance(sampleWorld, worldPosition);

                if (distance > radius)
                {
                    continue;
                }

                float normalized =
                    Mathf.Clamp01(distance / radius);

                float inner =
                    Mathf.Clamp01(Hardness);

                float falloff =
                    normalized <= inner
                        ? 1f
                        : 1f - Mathf.InverseLerp(inner, 1f, normalized);

                int delta =
                    Mathf.RoundToInt(255f * Strength * falloff);

                int index = data.ToIndex(x, y);
                int current = densities[index];

                current = Mode == LevelBrushMode.Fill
                    ? current + delta
                    : current - delta;

                densities[index] =
                    (byte)Mathf.Clamp(current, 0, 255);
            }
        }

        data.SetDensities(densities);
    }
}
