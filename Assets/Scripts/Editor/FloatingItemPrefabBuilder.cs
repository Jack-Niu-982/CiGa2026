using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FloatingItemPrefabBuilder
{
    private const string MenuPath = "CiGa2026/Build Floating Item Prefabs";
    private const string PrefabFolder = "Assets/Prefabs/Gameplay/Pickups";
    private const string SpriteFolder = PrefabFolder + "/GeneratedSprites";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(SpriteFolder);

        BuildItem(
            CarryableItemType.Fuel,
            "FuelPickup",
            new Color(0.22f, 0.9f, 0.42f, 1f),
            new Vector2(0.7f, 0.7f)
        );

        BuildItem(
            CarryableItemType.Trash,
            "TrashPickup",
            new Color(0.62f, 0.62f, 0.56f, 1f),
            new Vector2(0.75f, 0.55f)
        );

        BuildItem(
            CarryableItemType.Shield,
            "ShieldPickup",
            new Color(0.28f, 0.72f, 1f, 1f),
            new Vector2(0.72f, 0.72f)
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FloatingItemPrefabBuilder] Floating item prefabs generated.");
    }

    private static void BuildItem(
        CarryableItemType itemType,
        string prefabName,
        Color placeholderColor,
        Vector2 colliderSize)
    {
        GameObject root =
            new GameObject(prefabName);

        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer =
            root.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite =
            CreatePlaceholderSprite(prefabName, placeholderColor);
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 0;

        BoxCollider2D collider =
            root.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        collider.size = colliderSize;

        Rigidbody2D rigidbody =
            root.AddComponent<Rigidbody2D>();

        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.simulated = true;

        CarryableItem2D carryable =
            root.AddComponent<CarryableItem2D>();

        carryable.Configure(
            itemType,
            collider,
            rigidbody
        );

        string prefabPath =
            $"{PrefabFolder}/{prefabName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Sprite CreatePlaceholderSprite(
        string assetName,
        Color color)
    {
        string texturePath =
            $"{SpriteFolder}/{assetName}Placeholder.png";

        Texture2D existingTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (existingTexture == null)
        {
            Texture2D texture =
                new Texture2D(16, 16, TextureFormat.RGBA32, false);

            Color[] pixels =
                Enumerable.Repeat(color, 16 * 16).ToArray();

            texture.SetPixels(pixels);
            texture.Apply();

            System.IO.File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath);

            TextureImporter importer =
                AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent =
            System.IO.Path.GetDirectoryName(path)
                ?.Replace("\\", "/");

        string folderName =
            System.IO.Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
