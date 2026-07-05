using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class LevelTerrainRenderer2D : MonoBehaviour
{
    [SerializeField]
    private Material terrainMaterial;

    [SerializeField]
    private Color fallbackColor =
        new Color(0.12f, 0.17f, 0.21f, 1f);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;

    private void Awake()
    {
        EnsureComponents();
    }

    public void Render(LevelTerrainMeshData meshData)
    {
        EnsureComponents();

        meshFilter.sharedMesh =
            meshData != null ? meshData.Mesh : null;

        if (terrainMaterial != null)
        {
            meshRenderer.sharedMaterial = terrainMaterial;
            return;
        }

        if (runtimeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Generated Terrain Material"
            };

            runtimeMaterial.color = fallbackColor;
        }

        meshRenderer.sharedMaterial = runtimeMaterial;
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }
}
