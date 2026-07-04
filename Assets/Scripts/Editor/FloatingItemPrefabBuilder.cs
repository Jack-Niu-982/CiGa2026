using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class FloatingItemPrefabBuilder
{
    private const string MenuPath = "CiGa2026/Build Floating Item Prefabs";
    private const string PrefabFolder = "Assets/Prefabs/Gameplay/Pickups";
    private const string FloatingPrefabFolder = "Assets/Prefabs/Gameplay/FloatingItems";
    private const string ArtFolder = "Assets/res";
    private const string FuelSpritePath = ArtFolder + "/FuelPickupPlaceholder.png";
    private const string TrashSpritePath = ArtFolder + "/floating_crushed_can_trash_icon_v3_20260704070253.png";
    private const string ShieldSpritePath = ArtFolder + "/edited_shield_cat_paw_icon_20260704094010.png";
    private const string FuelIdleFolder = ArtFolder + "/anim/fuel/idle";
    private const string ShieldIdleFolder = ArtFolder + "/anim/shield/idle";
    private const float ArtPixelsPerUnit = 256f;
    private const float IdleAnimationFrameRate = 8f;
    private const float PickupRootScale = 0.1f;
    private const float AnimatedItemScaleMultiplier = 0.7f;
    private const int PickupSortingOrder = 30;
    private const string PickupLayerName = "Pickup";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(FloatingPrefabFolder);
        BuildItem(
            CarryableItemType.Fuel,
            "FuelPickup",
            FuelSpritePath,
            FuelIdleFolder,
            "FuelIdle"
        );

        BuildItem(
            CarryableItemType.Trash,
            "TrashPickup",
            TrashSpritePath,
            null,
            null
        );

        BuildItem(
            CarryableItemType.Shield,
            "ShieldPickup",
            ShieldSpritePath,
            ShieldIdleFolder,
            "ShieldIdle"
        );

        BuildFloatingItem(
            CarryableItemType.Fuel,
            "FloatingFuel",
            "FuelPickup"
        );

        BuildFloatingItem(
            CarryableItemType.Trash,
            "FloatingTrash",
            "TrashPickup"
        );

        BuildFloatingItem(
            CarryableItemType.Shield,
            "FloatingShield",
            "ShieldPickup"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FloatingItemPrefabBuilder] Floating item prefabs generated.");
    }

    private static void BuildItem(
        CarryableItemType itemType,
        string prefabName,
        string spritePath,
        string idleAnimationFolder,
        string idleAnimationName)
    {
        Sprite sprite = PrepareSprite(spritePath);
        RuntimeAnimatorController idleController = null;

        if (!string.IsNullOrEmpty(idleAnimationFolder))
        {
            idleController = PrepareIdleAnimation(
                idleAnimationFolder,
                idleAnimationName,
                out Sprite firstFrame
            );

            if (firstFrame != null)
            {
                sprite = firstFrame;
            }
        }

        if (sprite == null)
        {
            return;
        }

        GameObject root =
            new GameObject(prefabName);

        int pickupLayer = LayerMask.NameToLayer(PickupLayerName);

        if (pickupLayer >= 0)
        {
            root.layer = pickupLayer;
        }

        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale =
            new Vector3(
                PickupRootScale * GetItemScaleMultiplier(itemType),
                PickupRootScale * GetItemScaleMultiplier(itemType),
                1f
            );

        SpriteRenderer spriteRenderer =
            root.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = PickupSortingOrder;

        AddAnimator(root, idleController);

        PolygonCollider2D physicalCollider =
            AddSpriteCollider(root, sprite, false);

        PolygonCollider2D pickupTrigger =
            AddSpriteCollider(root, sprite, true);

        Rigidbody2D rigidbody =
            root.AddComponent<Rigidbody2D>();

        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.simulated = true;
        rigidbody.mass = 0.1f;

        CarryableItem2D carryable =
            root.AddComponent<CarryableItem2D>();

        carryable.Configure(
            itemType,
            pickupTrigger,
            rigidbody
        );

        physicalCollider.isTrigger = false;

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
        CarryableItemType itemType,
        string prefabName,
        string pickupPrefabName)
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

        float itemScale = GetItemScaleMultiplier(floatingType);
        root.transform.localScale =
            new Vector3(itemScale, itemScale, 1f);

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

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 1;

        Animator pickupAnimator =
            pickupPrefab.GetComponent<Animator>();

        AddAnimator(
            root,
            pickupAnimator != null
                ? pickupAnimator.runtimeAnimatorController
                : null
        );

        if (spriteRenderer.sprite == null)
        {
            Debug.LogError(
                $"[FloatingItemPrefabBuilder] Missing sprite on pickup prefab: {pickupPrefabName}"
            );

            UnityEngine.Object.DestroyImmediate(root);
            return;
        }

        PolygonCollider2D collider =
            AddSpriteCollider(root, spriteRenderer.sprite, true);

        Rigidbody2D rigidbody =
            root.AddComponent<Rigidbody2D>();

        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.simulated = true;

        FloatingItem2D floatingItem =
            root.AddComponent<FloatingItem2D>();

        floatingItem.Configure(
            itemType,
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

    private static float GetItemScaleMultiplier(
        CarryableItemType itemType)
    {
        switch (itemType)
        {
            case CarryableItemType.Fuel:
            case CarryableItemType.Shield:
                return AnimatedItemScaleMultiplier;

            default:
                return 1f;
        }
    }

    private static float GetItemScaleMultiplier(
        FloatingItemType itemType)
    {
        switch (itemType)
        {
            case FloatingItemType.Fuel:
            case FloatingItemType.Shield:
                return AnimatedItemScaleMultiplier;

            default:
                return 1f;
        }
    }

    private static Sprite PrepareSprite(string texturePath)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError(
                $"[FloatingItemPrefabBuilder] Missing sprite asset: {texturePath}"
            );

            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ArtPixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static RuntimeAnimatorController PrepareIdleAnimation(
        string frameFolder,
        string animationName,
        out Sprite firstFrame)
    {
        Sprite[] sprites = AssetDatabase
            .FindAssets("t:Texture2D", new[] { frameFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".png"))
            .OrderBy(path => path)
            .Select(PrepareSprite)
            .Where(sprite => sprite != null)
            .ToArray();

        firstFrame = sprites.FirstOrDefault();

        if (sprites.Length == 0)
        {
            Debug.LogError(
                $"[FloatingItemPrefabBuilder] No idle frames found in: {frameFolder}"
            );

            return null;
        }

        string clipPath = $"{frameFolder}/{animationName}.anim";
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.name = animationName;
        clip.frameRate = IdleAnimationFrameRate;

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[sprites.Length + 1];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / IdleAnimationFrameRate,
                value = sprites[i]
            };
        }

        keyframes[sprites.Length] = new ObjectReferenceKeyframe
        {
            time = sprites.Length / IdleAnimationFrameRate,
            value = sprites[0]
        };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes
        );

        AnimationClipSettings clipSettings =
            AnimationUtility.GetAnimationClipSettings(clip);

        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        EditorUtility.SetDirty(clip);

        string controllerPath =
            $"{frameFolder}/{animationName}.controller";

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    controllerPath
                );
        }

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;

        AnimatorState idleState = stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(state => state.name == animationName);

        if (idleState == null)
        {
            idleState = stateMachine.AddState(animationName);
        }

        idleState.motion = clip;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);

        return controller;
    }

    private static void AddAnimator(
        GameObject root,
        RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return;
        }

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
    }

    private static PolygonCollider2D AddSpriteCollider(
        GameObject root,
        Sprite sprite,
        bool isTrigger)
    {
        PolygonCollider2D collider =
            root.AddComponent<PolygonCollider2D>();

        collider.isTrigger = isTrigger;

        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount <= 0)
        {
            return collider;
        }

        collider.pathCount = shapeCount;
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < shapeCount; i++)
        {
            points.Clear();
            sprite.GetPhysicsShape(i, points);
            collider.SetPath(i, points);
        }

        return collider;
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
