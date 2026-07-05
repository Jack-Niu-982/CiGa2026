using UnityEngine;

[CreateAssetMenu(
    fileName = "TerrainSettings",
    menuName = "Settings/Terrain Settings")]
public class TerrainSettings : ScriptableObject
{
    [Header("Texture")]
    [Tooltip("地形主纹理。Wrap Mode 需设为 Repeat。")]
    public Texture2D terrainTexture;

    [Tooltip("纹理平铺缩放。值越大纹理越密集。")]
    [Range(0.01f, 2f)]
    public float uvScale = 0.25f;

    [Header("Color")]
    [Tooltip("纹理叠加颜色。白色 = 原色显示。")]
    public Color tint = Color.white;

    [Tooltip("无纹理时的回退颜色。")]
    public Color fallbackColor =
        new Color(0.12f, 0.17f, 0.21f, 1f);

    [Header("Shader")]
    [Tooltip("自定义 Shader 名。留空则使用 Sprites/Default。")]
    public string shaderName = "";
}
