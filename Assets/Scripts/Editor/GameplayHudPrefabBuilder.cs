using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayHudPrefabBuilder
{
    private const string MenuPath =
        "CiGa2026/Build Gameplay HUD Prefab";

    private const string PrefabPath =
        "Assets/Prefabs/UI/GameplayHudPanel.prefab";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            Debug.LogError(
                $"[GameplayHudPrefabBuilder] Missing prefab: {PrefabPath}"
            );

            return;
        }

        GameObject root =
            PrefabUtility.LoadPrefabContents(PrefabPath);

        GameplayHudView hudView =
            root.GetComponent<GameplayHudView>();

        if (hudView == null)
        {
            hudView =
                root.AddComponent<GameplayHudView>();
        }

        Button pauseButton =
            root.transform
                .Find("PauseButton")
                ?.GetComponent<Button>();

        GameplayPlayerStatusBarView statusBar =
            EnsureStatusBar(root.transform);

        SerializedObject serializedHud =
            new SerializedObject(hudView);

        serializedHud.FindProperty("pauseButton")
            .objectReferenceValue = pauseButton;

        serializedHud.FindProperty("playerStatusBar")
            .objectReferenceValue = statusBar;

        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(
            root,
            PrefabPath
        );

        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[GameplayHudPrefabBuilder] Gameplay HUD prefab updated."
        );
    }

    private static GameplayPlayerStatusBarView EnsureStatusBar(
        Transform parent)
    {
        Transform existing =
            parent.Find("PlayerStatusBar");

        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject bar =
            CreateUIObject("PlayerStatusBar", parent);

        RectTransform rect =
            bar.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 28f);
        rect.sizeDelta = new Vector2(1120f, 148f);

        HorizontalLayoutGroup layout =
            bar.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameplayPlayerStatusBarView statusBar =
            bar.AddComponent<GameplayPlayerStatusBarView>();

        GameplayPlayerStatusSlotView[] slots =
            new GameplayPlayerStatusSlotView[RoomInputManager.MaxPlayers];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] =
                CreateStatusSlot(bar.transform, i);
        }

        SerializedObject serializedBar =
            new SerializedObject(statusBar);

        SerializedProperty slotViews =
            serializedBar.FindProperty("slotViews");

        slotViews.arraySize = slots.Length;

        for (int i = 0; i < slots.Length; i++)
        {
            slotViews.GetArrayElementAtIndex(i)
                .objectReferenceValue = slots[i];
        }

        serializedBar.ApplyModifiedPropertiesWithoutUndo();

        return statusBar;
    }

    private static GameplayPlayerStatusSlotView CreateStatusSlot(
        Transform parent,
        int index)
    {
        GameObject slot =
            CreateUIObject($"P{index + 1}StatusSlot", parent);

        Image background =
            slot.AddComponent<Image>();

        background.color =
            new Color(0.06f, 0.07f, 0.09f, 0.86f);

        LayoutElement layoutElement =
            slot.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = 260f;
        layoutElement.preferredHeight = 148f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        VerticalLayoutGroup layout =
            slot.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject itemRoot =
            CreateUIObject("HeldItem", slot.transform);

        itemRoot.AddComponent<LayoutElement>()
            .preferredHeight = 42f;

        HorizontalLayoutGroup itemLayout =
            itemRoot.AddComponent<HorizontalLayoutGroup>();

        itemLayout.spacing = 8f;
        itemLayout.childAlignment = TextAnchor.MiddleCenter;
        itemLayout.childControlWidth = true;
        itemLayout.childControlHeight = true;
        itemLayout.childForceExpandWidth = false;
        itemLayout.childForceExpandHeight = true;

        Image itemIcon =
            CreateImage(
                itemRoot.transform,
                "ItemIcon",
                new Color(1f, 1f, 1f, 1f)
            );

        itemIcon.gameObject.AddComponent<LayoutElement>()
            .preferredWidth = 38f;

        TMP_Text itemName =
            CreateText(
                itemRoot.transform,
                "ItemName",
                string.Empty,
                18
            );

        itemName.alignment = TextAlignmentOptions.MidlineLeft;
        itemName.gameObject.AddComponent<LayoutElement>()
            .preferredWidth = 128f;

        GameObject playerRow =
            CreateUIObject("PlayerRow", slot.transform);

        playerRow.AddComponent<LayoutElement>()
            .preferredHeight = 70f;

        HorizontalLayoutGroup playerLayout =
            playerRow.AddComponent<HorizontalLayoutGroup>();

        playerLayout.spacing = 12f;
        playerLayout.childAlignment = TextAnchor.MiddleCenter;
        playerLayout.childControlWidth = true;
        playerLayout.childControlHeight = true;
        playerLayout.childForceExpandWidth = false;
        playerLayout.childForceExpandHeight = true;

        Image portrait =
            CreateImage(
                playerRow.transform,
                "Portrait",
                new Color(0.22f, 0.25f, 0.30f, 1f)
            );

        portrait.gameObject.AddComponent<LayoutElement>()
            .preferredWidth = 62f;

        TMP_Text playerLabel =
            CreateText(
                playerRow.transform,
                "PlayerLabel",
                $"P{index + 1}",
                28
            );

        playerLabel.alignment = TextAlignmentOptions.MidlineLeft;
        playerLabel.gameObject.AddComponent<LayoutElement>()
            .preferredWidth = 72f;

        GameplayPlayerStatusSlotView slotView =
            slot.AddComponent<GameplayPlayerStatusSlotView>();

        SerializedObject serializedSlot =
            new SerializedObject(slotView);

        serializedSlot.FindProperty("playerLabel")
            .objectReferenceValue = playerLabel;

        serializedSlot.FindProperty("portraitImage")
            .objectReferenceValue = portrait;

        serializedSlot.FindProperty("itemRoot")
            .objectReferenceValue = itemRoot;

        serializedSlot.FindProperty("itemIconImage")
            .objectReferenceValue = itemIcon;

        serializedSlot.FindProperty("itemNameLabel")
            .objectReferenceValue = itemName;

        serializedSlot.ApplyModifiedPropertiesWithoutUndo();

        itemRoot.SetActive(false);

        return slotView;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Color color)
    {
        GameObject imageObject =
            CreateUIObject(name, parent);

        Image image =
            imageObject.AddComponent<Image>();

        image.color = color;

        return image;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        int fontSize)
    {
        GameObject textObject =
            CreateUIObject(name, parent);

        TextMeshProUGUI label =
            textObject.AddComponent<TextMeshProUGUI>();

        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;

        return label;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent)
    {
        GameObject gameObject =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        gameObject.transform.SetParent(
            parent,
            false
        );

        return gameObject;
    }
}
