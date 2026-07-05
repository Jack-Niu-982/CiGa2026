using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelTerrainColliderBuilder : MonoBehaviour
{
    [SerializeField]
    private string wallLayerName = "Wall";

    [SerializeField]
    private PhysicsMaterial2D physicsMaterial;

    public void Build(LevelTerrainMeshData meshData)
    {
        Clear();

        if (meshData == null ||
            meshData.ColliderPaths == null)
        {
            return;
        }

        int wallLayer = LayerMask.NameToLayer(wallLayerName);

        if (wallLayer >= 0)
        {
            gameObject.layer = wallLayer;
        }

        for (int i = 0; i < meshData.ColliderPaths.Length; i++)
        {
            Vector2[] path = meshData.ColliderPaths[i];

            if (path == null ||
                path.Length < 2)
            {
                continue;
            }

            GameObject child =
                new GameObject($"TerrainCollider_{i:00}");

            child.transform.SetParent(transform, false);

            if (wallLayer >= 0)
            {
                child.layer = wallLayer;
            }

            EdgeCollider2D collider =
                child.AddComponent<EdgeCollider2D>();

            collider.points = path;
            collider.sharedMaterial = physicsMaterial;
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
