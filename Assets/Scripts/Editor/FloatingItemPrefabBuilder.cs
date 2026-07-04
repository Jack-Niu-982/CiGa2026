using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FloatingItemPrefabBuilder
{
    private const string MenuPath = "CiGa2026/Build Floating Item Prefabs";
    private const string PrefabFolder = "Assets/Prefabs/Gameplay/Pickups";
    private const string FloatingPrefabFolder = "Assets/Prefabs/Gameplay/FloatingItems";
    private const string SpriteFolder = PrefabFolder + "/GeneratedSprites";
    private const float PickupRootScale = 0.1f;
    private const int PickupSortingOrder = 30;

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(FloatingPrefabFolder);
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

        BuildFloatingItem(
            FloatingItemType.Fuel,
            "FloatingFuel",
            "FuelPickup",
            new Color(0.22f, 0.9f, 0.42f, 1f),
            new Vector2(0.9f, 0.9f)
        );

        BuildFloatingItem(
            FloatingItemType.Trash,
            "FloatingTrash",
            "TrashPickup",
            new Color(0.62f, 0.62f, 0.56f, 1f),
            new Vector2(0.95f, 0.7f)
        );

        BuildFloatingItem(
            FloatingItemType.Shield,
            "FloatingShield",
            "ShieldPickup",
            new Color(0.28f, 0.72f, 1f, 1f),
            new Vector2(0.9f, 0.9f)
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
        root.transform.localScale =
            new Vector3(
                PickupRootScale,
                PickupRootScale,
                1f
            );

        SpriteRenderer spriteRenderer =
            root.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite =
            CreatePlaceholderSprite(prefabName, placeholderColor);
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = PickupSortingOrder;

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

        SerializedObject serializedCarryable =
            new SerializedObject(carryable);

        serializedCarryable.FindProperty("displayName")
            .stringValue = GetDisplayName(itemType);

        serializedCarryable.FindProperty("iconSprite")
            .objectReferenceValue = spriteRenderer.sprite;

        serializedCarryable.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath =
            $"{PrefabFolder}/{prefabName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void BuildFloatingItem(
        FloatingItemType floatingType,
        string prefabName,
        string pickupPrefabName,
        Color tintColor,
        Vector2 colliderSize)
    {
        CarryableItem2D pickupPrefab =
            AssetDatabase.LoadAssetAtPath<CarryableItem2D>(
                $"{PrefabFolder}/{pickupPrefabName}.prefab"
            );

        if (pickupPrefab == null)
        {
            Debug.LogError(
                $"[FloatingItemPrefabBuilder] Missing pickup prefab: {pickupPrefabName}"
            );

            return;
        }

        GameObject root =
            new GameObject(prefabName);

        int floatingLayer =
            LayerMask.NameToLayer("FloatingItem");

        if (floatingLayer >= 0)
        {
            root.layer = floatingLayer;
        }

        SpriteRenderer spriteRenderer =
            root.AddComponent<SpriteRenderer>();

        SpriteRenderer pickupRenderer =
            pickupPrefab.GetComponent<SpriteRenderer>();

        if (pickupRenderer != null)
        {
            spriteRenderer.sprite =
                pickupRenderer.sprite;
        }

        spriteRenderer.color = tintColor;
        spriteRenderer.sortingOrder = 1;

        BoxCollider2D collider =
            root.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;
        collider.size = colliderSize;

        Rigidbody2D rigidbody =
            root.AddComponent<Rigidbody2D>();

        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.simulated = true;

        FloatingItem2D floatingItem =
            root.AddComponent<FloatingItem2D>();

        floatingItem.Configure(
            floatingType,
            pickupPrefab,
            new Vector2(-0.35f, 0f)
        );

        root.AddComponent<FloatingItemAnchorTarget2D>();

        string prefabPath =
            $"{FloatingPrefabFolder}/{prefabName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static string GetDisplayName(
        CarryableItemType itemType)
    {
        switch (itemType)
        {
            case CarryableItemType.Fuel:
                return "Fuel";

            case CarryableItemType.Shield:
                return "Shield";

            case CarryableItemType.Trash:
                return "Trash";

            default:
                return string.Empty;
        }
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
