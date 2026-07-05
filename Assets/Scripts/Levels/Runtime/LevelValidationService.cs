using System.Collections.Generic;
using UnityEngine;

public static class LevelValidationService
{
    public static LevelValidationResult Validate(
        LevelTerrainData data,
        byte solidThreshold = LevelTerrainData.DefaultSolidThreshold)
    {
        LevelValidationResult result =
            new LevelValidationResult();

        if (data == null)
        {
            result.AddError("地图数据为空。");
            return result;
        }

        data.Normalize();

        if (data.width < 2 || data.height < 2)
        {
            result.AddError("地图尺寸太小。");
            return result;
        }

        Vector2Int start =
            data.WorldToSample(data.start.Position);

        Vector2Int finish =
            data.WorldToSample(data.finish.Position);

        ValidatePose("起点", data, start, solidThreshold, result);
        ValidatePose("终点", data, finish, solidThreshold, result);

        if (result.IsValid &&
            !HasPath(data, start, finish, solidThreshold))
        {
            result.AddError("起点到终点之间没有连通道路。");
        }

        if (!IsOuterBorderSolid(data, solidThreshold))
        {
            result.AddWarning("地图外边缘存在空洞，飞船可能离开有效区域。");
        }

        return result;
    }

    private static void ValidatePose(
        string label,
        LevelTerrainData data,
        Vector2Int sample,
        byte solidThreshold,
        LevelValidationResult result)
    {
        if (!data.IsInside(sample.x, sample.y))
        {
            result.AddError($"{label} 不在地图范围内。");
            return;
        }

        if (data.IsSolid(sample.x, sample.y, solidThreshold))
        {
            result.AddError($"{label} 位于实心地形中。");
        }
    }

    private static bool HasPath(
        LevelTerrainData data,
        Vector2Int start,
        Vector2Int finish,
        byte solidThreshold)
    {
        bool[] visited = new bool[data.DensityCount];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[data.ToIndex(start.x, start.y)] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == finish)
            {
                return true;
            }

            TryEnqueue(data, current.x + 1, current.y, solidThreshold, visited, queue);
            TryEnqueue(data, current.x - 1, current.y, solidThreshold, visited, queue);
            TryEnqueue(data, current.x, current.y + 1, solidThreshold, visited, queue);
            TryEnqueue(data, current.x, current.y - 1, solidThreshold, visited, queue);
        }

        return false;
    }

    private static void TryEnqueue(
        LevelTerrainData data,
        int x,
        int y,
        byte solidThreshold,
        bool[] visited,
        Queue<Vector2Int> queue)
    {
        if (!data.IsInside(x, y))
        {
            return;
        }

        int index = data.ToIndex(x, y);

        if (visited[index] ||
            data.IsSolid(x, y, solidThreshold))
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    private static bool IsOuterBorderSolid(
        LevelTerrainData data,
        byte solidThreshold)
    {
        for (int x = 0; x < data.width; x++)
        {
            if (!data.IsSolid(x, 0, solidThreshold) ||
                !data.IsSolid(x, data.height - 1, solidThreshold))
            {
                return false;
            }
        }

        for (int y = 0; y < data.height; y++)
        {
            if (!data.IsSolid(0, y, solidThreshold) ||
                !data.IsSolid(data.width - 1, y, solidThreshold))
            {
                return false;
            }
        }

        return true;
    }
}
