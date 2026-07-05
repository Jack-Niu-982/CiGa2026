using System.Collections.Generic;
using UnityEngine;

public static class LevelTerrainMarchingSquares
{
    private struct VertexKey : System.IEquatable<VertexKey>
    {
        public readonly int X;
        public readonly int Y;

        public VertexKey(Vector2 position)
        {
            X = Mathf.RoundToInt(position.x * 10000f);
            Y = Mathf.RoundToInt(position.y * 10000f);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return X * 397 ^ Y;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is VertexKey other && Equals(other);
        }

        public bool Equals(VertexKey other)
        {
            return X == other.X && Y == other.Y;
        }
    }

    private const float DefaultUVScale = 0.25f;

    private static float GetUVScale()
    {
        TerrainSettings settings = SettingManager.Terrain;
        return (settings != null) ? settings.uvScale : DefaultUVScale;
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
        byte[] densities = data.GetDensities();
        float cellSize = data.CellSize;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<VertexKey, int> vertexMap =
            new Dictionary<VertexKey, int>();
        List<Vector2> contourA = new List<Vector2>();
        List<Vector2> contourB = new List<Vector2>();

        for (int y = 0; y < data.height - 1; y++)
        {
            for (int x = 0; x < data.width - 1; x++)
            {
                byte bl = densities[data.ToIndex(x, y)];
                byte br = densities[data.ToIndex(x + 1, y)];
                byte tr = densities[data.ToIndex(x + 1, y + 1)];
                byte tl = densities[data.ToIndex(x, y + 1)];

                ProcessCell(
                    x, y, bl, br, tr, tl,
                    cellSize, solidThreshold,
                    vertices, triangles, vertexMap,
                    contourA, contourB
                );
            }
        }

        Mesh mesh = new Mesh
        {
            name = string.IsNullOrWhiteSpace(data.id)
                ? "Generated Smooth Terrain"
                : data.id + " Smooth Terrain"
        };

        if (vertices.Count > 65535)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        List<Vector2> uvs = new List<Vector2>(vertices.Count);
        float uvScale = GetUVScale();
        for (int i = 0; i < vertices.Count; i++)
        {
            uvs.Add(new Vector2(
                vertices[i].x * uvScale,
                vertices[i].y * uvScale));
        }
        mesh.SetUVs(0, uvs);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        Vector2[][] colliderPaths =
            BuildColliderPaths(contourA, contourB);

        return new LevelTerrainMeshData
        {
            Mesh = mesh,
            ColliderPaths = colliderPaths
        };
    }

    private static void ProcessCell(
        int x, int y,
        byte bl, byte br, byte tr, byte tl,
        float cellSize, byte threshold,
        List<Vector3> vertices,
        List<int> triangles,
        Dictionary<VertexKey, int> vertexMap,
        List<Vector2> contourA,
        List<Vector2> contourB)
    {
        int config = 0;
        if (bl >= threshold) config |= 1;
        if (br >= threshold) config |= 2;
        if (tr >= threshold) config |= 4;
        if (tl >= threshold) config |= 8;

        if (config == 0)
        {
            return;
        }

        float x0 = x * cellSize;
        float y0 = y * cellSize;
        float x1 = (x + 1) * cellSize;
        float y1 = (y + 1) * cellSize;

        Vector2 pBL = new Vector2(x0, y0);
        Vector2 pBR = new Vector2(x1, y0);
        Vector2 pTR = new Vector2(x1, y1);
        Vector2 pTL = new Vector2(x0, y1);

        if (config == 15)
        {
            AddTriangle(vertexMap, vertices, triangles, pBL, pBR, pTR);
            AddTriangle(vertexMap, vertices, triangles, pBL, pTR, pTL);
            return;
        }

        Vector2 eBottom = LerpEdge(pBL, pBR, bl, br, threshold);
        Vector2 eRight = LerpEdge(pBR, pTR, br, tr, threshold);
        Vector2 eTop = LerpEdge(pTL, pTR, tl, tr, threshold);
        Vector2 eLeft = LerpEdge(pBL, pTL, bl, tl, threshold);

        switch (config)
        {
            case 1: // only BL solid
                AddTriangle(vertexMap, vertices, triangles, pBL, eBottom, eLeft);
                AddContourEdge(contourA, contourB, eBottom, eLeft);
                break;

            case 2: // only BR solid
                AddTriangle(vertexMap, vertices, triangles, pBR, eRight, eBottom);
                AddContourEdge(contourA, contourB, eRight, eBottom);
                break;

            case 3: // BL + BR solid
                AddTriangle(vertexMap, vertices, triangles, pBL, pBR, eRight);
                AddTriangle(vertexMap, vertices, triangles, pBL, eRight, eLeft);
                AddContourEdge(contourA, contourB, eRight, eLeft);
                break;

            case 4: // only TR solid
                AddTriangle(vertexMap, vertices, triangles, pTR, eTop, eRight);
                AddContourEdge(contourA, contourB, eTop, eRight);
                break;

            case 5: // BL + TR solid (saddle)
            {
                float center = (bl + br + tr + tl) * 0.25f;
                if (center >= threshold)
                {
                    AddTriangle(vertexMap, vertices, triangles, pBL, eBottom, eLeft);
                    AddTriangle(vertexMap, vertices, triangles, pTR, eTop, eRight);
                    AddTriangle(vertexMap, vertices, triangles, eBottom, eRight, eTop);
                    AddTriangle(vertexMap, vertices, triangles, eBottom, eTop, eLeft);
                    AddContourEdge(contourA, contourB, eBottom, eRight);
                    AddContourEdge(contourA, contourB, eTop, eLeft);
                }
                else
                {
                    AddTriangle(vertexMap, vertices, triangles, pBL, eBottom, eLeft);
                    AddTriangle(vertexMap, vertices, triangles, pTR, eTop, eRight);
                    AddContourEdge(contourA, contourB, eBottom, eLeft);
                    AddContourEdge(contourA, contourB, eTop, eRight);
                }
                break;
            }

            case 6: // BR + TR solid
                AddTriangle(vertexMap, vertices, triangles, pBR, pTR, eTop);
                AddTriangle(vertexMap, vertices, triangles, pBR, eTop, eBottom);
                AddContourEdge(contourA, contourB, eTop, eBottom);
                break;

            case 7: // BL + BR + TR solid
                AddTriangle(vertexMap, vertices, triangles, pBL, pBR, pTR);
                AddTriangle(vertexMap, vertices, triangles, pBL, pTR, eTop);
                AddTriangle(vertexMap, vertices, triangles, pBL, eTop, eLeft);
                AddContourEdge(contourA, contourB, eTop, eLeft);
                break;

            case 8: // only TL solid
                AddTriangle(vertexMap, vertices, triangles, pTL, eLeft, eTop);
                AddContourEdge(contourA, contourB, eLeft, eTop);
                break;

            case 9: // BL + TL solid
                AddTriangle(vertexMap, vertices, triangles, pBL, eBottom, eTop);
                AddTriangle(vertexMap, vertices, triangles, pBL, eTop, pTL);
                AddContourEdge(contourA, contourB, eBottom, eTop);
                break;

            case 10: // BR + TL solid (saddle)
            {
                float center = (bl + br + tr + tl) * 0.25f;
                if (center >= threshold)
                {
                    AddTriangle(vertexMap, vertices, triangles, pBR, eRight, eBottom);
                    AddTriangle(vertexMap, vertices, triangles, pTL, eLeft, eTop);
                    AddTriangle(vertexMap, vertices, triangles, eLeft, eBottom, eRight);
                    AddTriangle(vertexMap, vertices, triangles, eLeft, eRight, eTop);
                    AddContourEdge(contourA, contourB, eLeft, eBottom);
                    AddContourEdge(contourA, contourB, eRight, eTop);
                }
                else
                {
                    AddTriangle(vertexMap, vertices, triangles, pBR, eRight, eBottom);
                    AddTriangle(vertexMap, vertices, triangles, pTL, eLeft, eTop);
                    AddContourEdge(contourA, contourB, eRight, eBottom);
                    AddContourEdge(contourA, contourB, eLeft, eTop);
                }
                break;
            }

            case 11: // BL + BR + TL solid
                AddTriangle(vertexMap, vertices, triangles, pBL, pBR, eRight);
                AddTriangle(vertexMap, vertices, triangles, pBL, eRight, eTop);
                AddTriangle(vertexMap, vertices, triangles, pBL, eTop, pTL);
                AddContourEdge(contourA, contourB, eRight, eTop);
                break;

            case 12: // TR + TL solid
                AddTriangle(vertexMap, vertices, triangles, pTL, eLeft, eRight);
                AddTriangle(vertexMap, vertices, triangles, pTL, eRight, pTR);
                AddContourEdge(contourA, contourB, eLeft, eRight);
                break;

            case 13: // BL + TR + TL solid
                AddTriangle(vertexMap, vertices, triangles, pBL, eBottom, eRight);
                AddTriangle(vertexMap, vertices, triangles, pBL, eRight, pTR);
                AddTriangle(vertexMap, vertices, triangles, pBL, pTR, pTL);
                AddContourEdge(contourA, contourB, eBottom, eRight);
                break;

            case 14: // BR + TR + TL solid
                AddTriangle(vertexMap, vertices, triangles, pBR, pTR, pTL);
                AddTriangle(vertexMap, vertices, triangles, pBR, pTL, eLeft);
                AddTriangle(vertexMap, vertices, triangles, pBR, eLeft, eBottom);
                AddContourEdge(contourA, contourB, eLeft, eBottom);
                break;
        }
    }

    private static Vector2 LerpEdge(
        Vector2 p1, Vector2 p2,
        byte v1, byte v2,
        byte threshold)
    {
        if (Mathf.Abs(v1 - v2) < 1)
        {
            return (p1 + p2) * 0.5f;
        }

        float t = (threshold - (float)v1) / (v2 - (float)v1);
        t = Mathf.Clamp(t, 0.01f, 0.99f);
        return Vector2.Lerp(p1, p2, t);
    }

    private static void AddTriangle(
        Dictionary<VertexKey, int> vertexMap,
        List<Vector3> vertices,
        List<int> triangles,
        Vector2 p1, Vector2 p2, Vector2 p3)
    {
        int i1 = GetOrAddVertex(vertexMap, vertices, p1);
        int i2 = GetOrAddVertex(vertexMap, vertices, p2);
        int i3 = GetOrAddVertex(vertexMap, vertices, p3);

        triangles.Add(i1);
        triangles.Add(i3);
        triangles.Add(i2);
    }

    private static int GetOrAddVertex(
        Dictionary<VertexKey, int> vertexMap,
        List<Vector3> vertices,
        Vector2 position)
    {
        VertexKey key = new VertexKey(position);

        if (vertexMap.TryGetValue(key, out int index))
        {
            return index;
        }

        index = vertices.Count;
        vertices.Add(new Vector3(position.x, position.y, 0f));
        vertexMap.Add(key, index);
        return index;
    }

    private static void AddContourEdge(
        List<Vector2> contourA,
        List<Vector2> contourB,
        Vector2 a, Vector2 b)
    {
        contourA.Add(a);
        contourB.Add(b);
    }

    private static Vector2[][] BuildColliderPaths(
        List<Vector2> contourA,
        List<Vector2> contourB)
    {
        if (contourA.Count == 0)
        {
            return new Vector2[0][];
        }

        Dictionary<VertexKey, List<int>> adjacency =
            new Dictionary<VertexKey, List<int>>();

        for (int i = 0; i < contourA.Count; i++)
        {
            VertexKey keyA = new VertexKey(contourA[i]);
            VertexKey keyB = new VertexKey(contourB[i]);

            AddAdjacency(adjacency, keyA, i);
            AddAdjacency(adjacency, keyB, i);
        }

        bool[] used = new bool[contourA.Count];
        List<Vector2[]> paths = new List<Vector2[]>();

        for (int seed = 0; seed < contourA.Count; seed++)
        {
            if (used[seed])
            {
                continue;
            }

            List<Vector2> path = new List<Vector2>();
            TracePath(
                seed, contourA, contourB,
                adjacency, used, path, true
            );
            TracePath(
                seed, contourA, contourB,
                adjacency, used, path, false
            );

            if (path.Count >= 2)
            {
                paths.Add(path.ToArray());
            }
        }

        return paths.ToArray();
    }

    private static void TracePath(
        int seed,
        List<Vector2> contourA,
        List<Vector2> contourB,
        Dictionary<VertexKey, List<int>> adjacency,
        bool[] used,
        List<Vector2> path,
        bool forward)
    {
        if (!used[seed])
        {
            used[seed] = true;

            if (forward)
            {
                path.Add(contourA[seed]);
                path.Add(contourB[seed]);
            }
            else
            {
                path.Insert(0, contourA[seed]);
            }
        }

        VertexKey endKey = forward
            ? new VertexKey(path[path.Count - 1])
            : new VertexKey(path[0]);

        while (true)
        {
            if (!adjacency.TryGetValue(endKey, out List<int> edgeIndices))
            {
                return;
            }

            bool found = false;

            for (int i = 0; i < edgeIndices.Count; i++)
            {
                int edgeIdx = edgeIndices[i];

                if (used[edgeIdx])
                {
                    continue;
                }

                used[edgeIdx] = true;

                VertexKey keyA = new VertexKey(contourA[edgeIdx]);
                VertexKey keyB = new VertexKey(contourB[edgeIdx]);

                Vector2 next;

                if (keyA.Equals(endKey))
                {
                    next = contourB[edgeIdx];
                }
                else
                {
                    next = contourA[edgeIdx];
                }

                if (forward)
                {
                    path.Add(next);
                }
                else
                {
                    path.Insert(0, next);
                }

                endKey = new VertexKey(next);
                found = true;
                break;
            }

            if (!found)
            {
                return;
            }
        }
    }

    private static void AddAdjacency(
        Dictionary<VertexKey, List<int>> adjacency,
        VertexKey key,
        int edgeIndex)
    {
        if (!adjacency.TryGetValue(key, out List<int> list))
        {
            list = new List<int>(4);
            adjacency.Add(key, list);
        }

        list.Add(edgeIndex);
    }
}
