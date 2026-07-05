using System.Collections.Generic;
using UnityEngine;

public static class LevelTerrainMesher
{
    private struct EdgeKey
    {
        public readonly Vector2Int A;
        public readonly Vector2Int B;

        public EdgeKey(Vector2Int a, Vector2Int b)
        {
            if (Compare(a, b) <= 0)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + A.GetHashCode();
                hash = hash * 31 + B.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeKey other &&
                   A == other.A &&
                   B == other.B;
        }

        private static int Compare(Vector2Int left, Vector2Int right)
        {
            int xCompare = left.x.CompareTo(right.x);
            return xCompare != 0
                ? xCompare
                : left.y.CompareTo(right.y);
        }
    }

    public static LevelTerrainMeshData Build(
        LevelTerrainData data,
        byte solidThreshold = LevelTerrainData.DefaultSolidThreshold)
    {
        if (data == null)
        {
            return new LevelTerrainMeshData
            {
                Mesh = new Mesh(),
                ColliderPaths = new Vector2[0][]
            };
        }

        data.Normalize();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<EdgeKey, int> edges =
            new Dictionary<EdgeKey, int>();

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!data.IsSolid(x, y, solidThreshold))
                {
                    continue;
                }

                AddSolidCell(
                    data,
                    x,
                    y,
                    vertices,
                    triangles,
                    edges
                );
            }
        }

        Mesh mesh = new Mesh
        {
            name = string.IsNullOrWhiteSpace(data.id)
                ? "Generated Level Terrain"
                : data.id + " Terrain"
        };

        if (vertices.Count > 65535)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return new LevelTerrainMeshData
        {
            Mesh = mesh,
            ColliderPaths = BuildColliderPaths(data, edges)
        };
    }

    private static void AddSolidCell(
        LevelTerrainData data,
        int x,
        int y,
        List<Vector3> vertices,
        List<int> triangles,
        Dictionary<EdgeKey, int> edges)
    {
        float size = data.CellSize;
        float x0 = x * size;
        float y0 = y * size;
        float x1 = x0 + size;
        float y1 = y0 + size;

        int startIndex = vertices.Count;

        vertices.Add(new Vector3(x0, y0, 0f));
        vertices.Add(new Vector3(x1, y0, 0f));
        vertices.Add(new Vector3(x1, y1, 0f));
        vertices.Add(new Vector3(x0, y1, 0f));

        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex);
        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 2);

        AddOrRemoveEdge(
            edges,
            new Vector2Int(x, y),
            new Vector2Int(x + 1, y)
        );
        AddOrRemoveEdge(
            edges,
            new Vector2Int(x + 1, y),
            new Vector2Int(x + 1, y + 1)
        );
        AddOrRemoveEdge(
            edges,
            new Vector2Int(x + 1, y + 1),
            new Vector2Int(x, y + 1)
        );
        AddOrRemoveEdge(
            edges,
            new Vector2Int(x, y + 1),
            new Vector2Int(x, y)
        );
    }

    private static void AddOrRemoveEdge(
        Dictionary<EdgeKey, int> edges,
        Vector2Int a,
        Vector2Int b)
    {
        EdgeKey key = new EdgeKey(a, b);

        if (edges.ContainsKey(key))
        {
            edges.Remove(key);
        }
        else
        {
            edges.Add(key, 1);
        }
    }

    private static Vector2[][] BuildColliderPaths(
        LevelTerrainData data,
        Dictionary<EdgeKey, int> edges)
    {
        Dictionary<Vector2Int, List<Vector2Int>> adjacency =
            new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (EdgeKey edge in edges.Keys)
        {
            AddNeighbor(adjacency, edge.A, edge.B);
            AddNeighbor(adjacency, edge.B, edge.A);
        }

        HashSet<EdgeKey> unused =
            new HashSet<EdgeKey>(edges.Keys);

        List<Vector2[]> paths = new List<Vector2[]>();

        while (unused.Count > 0)
        {
            EdgeKey first = GetFirst(unused);
            List<Vector2Int> points = new List<Vector2Int>
            {
                first.A,
                first.B
            };

            unused.Remove(first);

            ExtendPath(points, adjacency, unused, true);
            ExtendPath(points, adjacency, unused, false);

            if (points.Count < 2)
            {
                continue;
            }

            paths.Add(ToWorldPath(points, data.CellSize));
        }

        return paths.ToArray();
    }

    private static void ExtendPath(
        List<Vector2Int> points,
        Dictionary<Vector2Int, List<Vector2Int>> adjacency,
        HashSet<EdgeKey> unused,
        bool forward)
    {
        while (true)
        {
            Vector2Int end =
                forward
                    ? points[points.Count - 1]
                    : points[0];

            if (!adjacency.TryGetValue(end, out List<Vector2Int> neighbors))
            {
                return;
            }

            bool found = false;

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int candidate = neighbors[i];
                EdgeKey key = new EdgeKey(end, candidate);

                if (!unused.Contains(key))
                {
                    continue;
                }

                unused.Remove(key);

                if (forward)
                {
                    points.Add(candidate);
                }
                else
                {
                    points.Insert(0, candidate);
                }

                found = true;
                break;
            }

            if (!found)
            {
                return;
            }
        }
    }

    private static Vector2[] ToWorldPath(
        List<Vector2Int> points,
        float cellSize)
    {
        Vector2[] path = new Vector2[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            path[i] = new Vector2(
                points[i].x * cellSize,
                points[i].y * cellSize
            );
        }

        return path;
    }

    private static void AddNeighbor(
        Dictionary<Vector2Int, List<Vector2Int>> adjacency,
        Vector2Int from,
        Vector2Int to)
    {
        if (!adjacency.TryGetValue(from, out List<Vector2Int> neighbors))
        {
            neighbors = new List<Vector2Int>(4);
            adjacency.Add(from, neighbors);
        }

        neighbors.Add(to);
    }

    private static EdgeKey GetFirst(HashSet<EdgeKey> edges)
    {
        foreach (EdgeKey edge in edges)
        {
            return edge;
        }

        return default;
    }
}
