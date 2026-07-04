using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnchorLauncherPrefabSetupUtility
{
    private const string MenuPath =
        "CiGa2026/Setup Anchor Launcher Prefab";

    private const string PrefabFolder =
        "Assets/Prefabs/Gameplay/Anchor";

    private const string PrefabPath =
        PrefabFolder + "/AnchorLauncher.prefab";

    private const int WallAndFloatingItemMask = 8256;

    private struct LauncherSetup
    {
        public string launcherName;
        public string controllerName;
        public AnchorLauncher2D.AnchorDirection direction;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
        public Vector3 anchorLocalPosition;
        public Vector3 anchorLocalEulerAngles;
        public Vector3 dropLocalPosition;
        public Vector3 dropLocalEulerAngles;
    }

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        EnsureFolder(PrefabFolder);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            GameObject source =
                FindSceneObject("ShooterRight");

            if (source == null)
            {
                Debug.LogError(
                    "[AnchorLauncherPrefabSetupUtility] Missing source object: ShooterRight."
                );

                return;
            }

            PrefabUtility.SaveAsPrefabAsset(
                source,
                PrefabPath
            );

            prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        NormalizePrefab(prefab);
        ReplaceSceneLaunchers(prefab);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(
            SceneManager.GetActiveScene()
        );

        Debug.Log(
            "[AnchorLauncherPrefabSetupUtility] Anchor launcher prefab and scene instances are ready."
        );
    }

    private static void NormalizePrefab(
        GameObject prefab)
    {
        GameObject root =
            PrefabUtility.LoadPrefabContents(PrefabPath);

        root.name = "AnchorLauncher";
        root.layer = LayerMask.NameToLayer("AnchorLauncher");
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale =
            new Vector3(0.2f, 0.2f, 0.2f);

        Transform anchor =
            EnsureChild(root.transform, "Anchor");

        anchor.gameObject.layer =
            LayerMask.NameToLayer("Anchor");

        anchor.localPosition = Vector3.up;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        Transform dropPoint =
            EnsureChild(root.transform, "ItemDropPoint");

        dropPoint.localPosition = Vector3.zero;
        dropPoint.localRotation = Quaternion.identity;
        dropPoint.localScale = Vector3.one;

        AnchorLauncher2D launcher =
            root.GetComponent<AnchorLauncher2D>();

        AnchorRotator rotator =
            root.GetComponent<AnchorRotator>();

        AnchorLaunchDetector2D detector =
            root.GetComponent<AnchorLaunchDetector2D>();

        AnchorRopeRuntime2D ropeRuntime =
            root.GetComponent<AnchorRopeRuntime2D>();

        AnchorItemDropPoint2D itemDropPoint =
            root.GetComponent<AnchorItemDropPoint2D>();

        SetObjectField(
            launcher,
            "anchorReference",
            anchor
        );

        SetObjectField(
            launcher,
            "launchDetector",
            detector
        );

        SetObjectField(
            launcher,
            "ropeRuntime",
            ropeRuntime
        );

        SetObjectField(
            launcher,
            "currentPlayerInput",
            null
        );

        SetEnumField(
            launcher,
            "anchorDirection",
            (int)AnchorLauncher2D.AnchorDirection.Up
        );

        SetObjectField(
            rotator,
            "anchorLauncher",
            launcher
        );

        SetObjectField(
            rotator,
            "operateController",
            null
        );

        ConfigureDetector(detector);

        SetObjectField(
            itemDropPoint,
            "dropPoint",
            dropPoint
        );

        PrefabUtility.SaveAsPrefabAsset(
            root,
            PrefabPath
        );

        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ReplaceSceneLaunchers(
        GameObject prefab)
    {
        Transform submarine =
            FindSceneObject("Submarine")?.transform;

        if (submarine == null)
        {
            Debug.LogError(
                "[AnchorLauncherPrefabSetupUtility] Missing scene object: Submarine."
            );

            return;
        }

        LauncherSetup[] setups =
        {
            new LauncherSetup
            {
                launcherName = "ShooterRight",
                controllerName = "RightShooterController",
                direction = AnchorLauncher2D.AnchorDirection.Right,
                localPosition = new Vector3(0.5f, 0f, 0f),
                localEulerAngles = new Vector3(0f, 0f, 90f),
                localScale = new Vector3(0.2f, 0.2f, 0.2f),
                anchorLocalPosition = new Vector3(0f, -1f, 0f),
                anchorLocalEulerAngles = new Vector3(0f, 0f, 180f),
                dropLocalPosition = new Vector3(-0.68749994f, 1.5000004f, 0f),
                dropLocalEulerAngles = new Vector3(0f, 0f, 270f)
            },
            new LauncherSetup
            {
                launcherName = "ShooterLeft",
                controllerName = "LeftShooterController",
                direction = AnchorLauncher2D.AnchorDirection.Left,
                localPosition = new Vector3(-0.5f, 0f, 0f),
                localEulerAngles = new Vector3(0f, 0f, 90f),
                localScale = new Vector3(0.2f, 0.2f, 0.2f),
                anchorLocalPosition = new Vector3(0f, 1f, 0f),
                anchorLocalEulerAngles = Vector3.zero,
                dropLocalPosition = new Vector3(-0.68750024f, -1.5000002f, 0f),
                dropLocalEulerAngles = new Vector3(0f, 0f, 270f)
            },
            new LauncherSetup
            {
                launcherName = "ShooterUp",
                controllerName = "UpShooterController",
                direction = AnchorLauncher2D.AnchorDirection.Up,
                localPosition = new Vector3(0f, 0.5f, 0f),
                localEulerAngles = Vector3.zero,
                localScale = new Vector3(0.2f, 0.2f, 0.2f),
                anchorLocalPosition = new Vector3(0f, 1f, 0f),
                anchorLocalEulerAngles = Vector3.zero,
                dropLocalPosition = new Vector3(0.5625f, -1.5625f, 0f),
                dropLocalEulerAngles = Vector3.zero
            },
            new LauncherSetup
            {
                launcherName = "ShooterDown",
                controllerName = "DownShooterController",
                direction = AnchorLauncher2D.AnchorDirection.Down,
                localPosition = new Vector3(0f, -0.5f, 0f),
                localEulerAngles = Vector3.zero,
                localScale = new Vector3(0.2f, 0.2f, 0.2f),
                anchorLocalPosition = new Vector3(0f, -1f, 0f),
                anchorLocalEulerAngles = new Vector3(0f, 0f, 180f),
                dropLocalPosition = new Vector3(0.5625f, 1.4375f, 0f),
                dropLocalEulerAngles = Vector3.zero
            }
        };

        for (int i = 0; i < setups.Length; i++)
        {
            ReplaceLauncher(
                prefab,
                submarine,
                setups[i],
                i
            );
        }
    }

    private static void ReplaceLauncher(
        GameObject prefab,
        Transform parent,
        LauncherSetup setup,
        int siblingIndex)
    {
        GameObject oldLauncher =
            FindSceneObject(setup.launcherName);

        if (oldLauncher != null)
        {
            siblingIndex =
                oldLauncher.transform.GetSiblingIndex();
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                parent
            ) as GameObject;

        if (instance == null)
        {
            Debug.LogError(
                "[AnchorLauncherPrefabSetupUtility] Failed to instantiate prefab."
            );

            return;
        }

        instance.name = setup.launcherName;
        instance.transform.SetSiblingIndex(siblingIndex);
        instance.transform.localPosition = setup.localPosition;
        instance.transform.localEulerAngles = setup.localEulerAngles;
        instance.transform.localScale = setup.localScale;

        ConfigureLauncherInstance(
            instance,
            setup
        );

        AnchorLauncherUseController2D controller =
            FindSceneObject(setup.controllerName)
                ?.GetComponent<AnchorLauncherUseController2D>();

        if (controller != null)
        {
            SetObjectField(
                controller,
                "anchorLauncherObject",
                instance
            );

            AnchorRotator rotator =
                instance.GetComponent<AnchorRotator>();

            SetObjectField(
                rotator,
                "operateController",
                controller
            );
        }
        else
        {
            Debug.LogWarning(
                $"[AnchorLauncherPrefabSetupUtility] Missing controller: {setup.controllerName}."
            );
        }

        if (oldLauncher != null)
        {
            UnityEngine.Object.DestroyImmediate(oldLauncher);
        }
    }

    private static void ConfigureLauncherInstance(
        GameObject instance,
        LauncherSetup setup)
    {
        Transform anchor =
            instance.transform.Find("Anchor");

        Transform dropPoint =
            instance.transform.Find("ItemDropPoint");

        anchor.localPosition = setup.anchorLocalPosition;
        anchor.localEulerAngles = setup.anchorLocalEulerAngles;
        anchor.localScale = Vector3.one;

        dropPoint.localPosition = setup.dropLocalPosition;
        dropPoint.localEulerAngles = setup.dropLocalEulerAngles;
        dropPoint.localScale =
            new Vector3(1.25f, 1.25f, 1.25f);

        AnchorLauncher2D launcher =
            instance.GetComponent<AnchorLauncher2D>();

        AnchorRotator rotator =
            instance.GetComponent<AnchorRotator>();

        AnchorLaunchDetector2D detector =
            instance.GetComponent<AnchorLaunchDetector2D>();

        AnchorRopeRuntime2D ropeRuntime =
            instance.GetComponent<AnchorRopeRuntime2D>();

        AnchorItemDropPoint2D itemDropPoint =
            instance.GetComponent<AnchorItemDropPoint2D>();

        SetEnumField(
            launcher,
            "anchorDirection",
            (int)setup.direction
        );

        SetObjectField(
            launcher,
            "anchorReference",
            anchor
        );

        SetObjectField(
            launcher,
            "launchDetector",
            detector
        );

        SetObjectField(
            launcher,
            "ropeRuntime",
            ropeRuntime
        );

        SetObjectField(
            launcher,
            "currentPlayerInput",
            null
        );

        SetObjectField(
            rotator,
            "anchorLauncher",
            launcher
        );

        ConfigureDetector(detector);

        SetObjectField(
            itemDropPoint,
            "dropPoint",
            dropPoint
        );

        SetVector2Field(
            itemDropPoint,
            "localDropOffset",
            new Vector2(0f, -0.7f)
        );
    }

    private static void ConfigureDetector(
        AnchorLaunchDetector2D detector)
    {
        SetLayerMaskField(
            detector,
            "attachableLayer",
            WallAndFloatingItemMask
        );

        SetBoolField(
            detector,
            "allowTriggerTargets",
            true
        );

        SetBoolField(
            detector,
            "drawDebugRay",
            true
        );

        SetFloatField(
            detector,
            "debugRayDuration",
            1.5f
        );
    }

    private static Transform EnsureChild(
        Transform parent,
        string name)
    {
        Transform child =
            parent.Find(name);

        if (child == null)
        {
            GameObject childObject =
                new GameObject(name);

            child =
                childObject.transform;

            child.SetParent(
                parent,
                false
            );
        }

        return child;
    }

    private static void EnsureFolder(
        string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent =
            System.IO.Path.GetDirectoryName(folder)
                ?.Replace("\\", "/");

        string name =
            System.IO.Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) &&
            !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(
            parent,
            name
        );
    }

    private static GameObject FindSceneObject(
        string objectName)
    {
        GameObject[] objects =
            UnityEngine.Object.FindObjectsOfType<GameObject>(true);

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name == objectName)
            {
                return objects[i];
            }
        }

        return null;
    }

    private static void SetObjectField(UnityEngine.Object target,
        string fieldName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.objectReferenceValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnumField(UnityEngine.Object target,
        string fieldName,
        int value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.enumValueIndex = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBoolField(UnityEngine.Object target,
        string fieldName,
        bool value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.boolValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloatField(UnityEngine.Object target,
        string fieldName,
        float value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.floatValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2Field(UnityEngine.Object target,
        string fieldName,
        Vector2 value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.vector2Value = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerMaskField(UnityEngine.Object target,
        string fieldName,
        int value)
    {
        SerializedProperty property =
            FindProperty(target, fieldName);

        property.intValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty FindProperty(UnityEngine.Object target,
        string fieldName)
    {
        SerializedObject serializedObject =
            new SerializedObject(target);

        SerializedProperty property =
            serializedObject.FindProperty(fieldName);

        if (property == null)
        {
            throw new MissingFieldException(
                target.GetType().Name,
                fieldName
            );
        }

        return property;
    }
}

