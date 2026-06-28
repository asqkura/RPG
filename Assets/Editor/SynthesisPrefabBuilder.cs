using System.IO;
using System.Linq;
using RPG.MasterData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SynthesisPrefabBuilder
{
    private const string SynthesisRoot = "Assets/UI/Synthesis";
    private const string PrefabFolder = SynthesisRoot + "/Prefabs";
    private const string RowPrefabPath = PrefabFolder + "/SynthesisRecipeRow.prefab";
    private const string ScreenPrefabPath = PrefabFolder + "/SynthesisScreen.prefab";
    private const string ResultScreenPrefabPath = PrefabFolder + "/SynthesisResultScreen.prefab";
    private const string BackgroundSpritePath = SynthesisRoot + "/Backgrounds/SynthesisWorkshopBackground.png";
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";
    private const string HomeScreenPath = "Assets/UI/Home/Prefabs/HomeScreen.prefab";
    private const string ItemDatabasePath = "Assets/MasterData/Test/Databases/TestItemDatabase.asset";
    private const string EquipmentDatabasePath = "Assets/MasterData/Test/Databases/TestEquipmentDatabase.asset";
    private const string RecipeDatabasePath = "Assets/MasterData/Test/Databases/TestSynthesisRecipeDatabase.asset";

    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color AccentColor = new(1f, 0.9f, 0.62f, 1f);

    [MenuItem("Tools/RPG/Build Synthesis UI Prefabs")]
    public static void Build()
    {
        EnsureFolders();
        TestSynthesisDataBuilder.Build();
        var rowPrefab = BuildRowPrefab();
        BuildScreenPrefab(rowPrefab);
        ConfigureHomeSynthesisButton();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Synthesis UI prefabs were generated.");
    }

    private static SynthesisRecipeRowView BuildRowPrefab()
    {
        var root = CreateUIObject("SynthesisRecipeRow", null, new Vector2(690f, 50f));
        var image = root.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        var view = root.AddComponent<SynthesisRecipeRowView>();

        var windowImage = CreateImage("Window", root.transform, new Vector2(690f, 50f), Vector2.zero);
        Stretch(windowImage.rectTransform);
        windowImage.sprite = LoadWindowSprite();
        windowImage.type = Image.Type.Sliced;

        var icon = CreateImage("Icon", root.transform, new Vector2(24f, 24f), new Vector2(40f, 0f));
        Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        icon.preserveAspect = true;

        var nameLabel = CreateText("Name", root.transform, "レシピ名", 24, TextAlignmentOptions.Left, new Vector2(330f, 36f), new Vector2(230f, 0f));
        Anchor(nameLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        var ownedLabel = CreateText("Owned", root.transform, "0", 22, TextAlignmentOptions.Center, new Vector2(70f, 36f), new Vector2(-160f, 0f));
        Anchor(ownedLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
        var costLabel = CreateText("Cost", root.transform, "-", 22, TextAlignmentOptions.Right, new Vector2(112f, 36f), new Vector2(-60f, 0f));
        Anchor(costLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

        var serialized = new SerializedObject(view);
        serialized.FindProperty("windowImage").objectReferenceValue = windowImage;
        serialized.FindProperty("normalWindowSprite").objectReferenceValue = LoadWindowSprite();
        serialized.FindProperty("highlightedWindowSprite").objectReferenceValue = LoadWindowHoverSprite();
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("nameLabel").objectReferenceValue = nameLabel;
        serialized.FindProperty("ownedLabel").objectReferenceValue = ownedLabel;
        serialized.FindProperty("costLabel").objectReferenceValue = costLabel;
        var labels = serialized.FindProperty("labelTexts");
        labels.arraySize = 3;
        labels.GetArrayElementAtIndex(0).objectReferenceValue = nameLabel;
        labels.GetArrayElementAtIndex(1).objectReferenceValue = ownedLabel;
        labels.GetArrayElementAtIndex(2).objectReferenceValue = costLabel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<SynthesisRecipeRowView>();
    }

    private static void BuildScreenPrefab(SynthesisRecipeRowView rowPrefab)
    {
        var root = new GameObject("SynthesisScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SynthesisScreenPreviewController));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.zero;
        rootRect.pivot = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.localScale = Vector3.one;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var background = CreateImage("Background", root.transform, new Vector2(1920f, 1080f), Vector2.zero);
        Stretch(background.rectTransform);
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
        background.color = Color.white;

        var title = CreateText("Title", root.transform, "合成", 46, TextAlignmentOptions.Left, new Vector2(320f, 70f), new Vector2(90f, -60f));
        AnchorTopLeft(title.rectTransform);
        var levelPanel = CreatePanel("SynthesisLevelPanel", root.transform, new Vector2(180f, 60f), new Vector2(420f, -70f));
        CreateText("SynthesisLevelCaption", levelPanel.transform, "合成Lv", 20, TextAlignmentOptions.Left, new Vector2(90f, 28f), new Vector2(22f, -16f));
        var synthesisLevelText = CreateText("SynthesisLevelText", levelPanel.transform, "Lv1", 24, TextAlignmentOptions.Right, new Vector2(70f, 32f), new Vector2(155f, -15f));
        Anchor(synthesisLevelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        var moneyPanel = CreatePanel("MoneyPanel", root.transform, new Vector2(300f, 60f), new Vector2(-250f, -70f));
        AnchorTopRight(moneyPanel.GetComponent<RectTransform>());
        CreateText("MoneyCaption", moneyPanel.transform, "所持金", 20, TextAlignmentOptions.Left, new Vector2(100f, 28f), new Vector2(30f, -16f));
        var moneyText = CreateText("MoneyText", moneyPanel.transform, "0", 24, TextAlignmentOptions.Right, new Vector2(130f, 32f), new Vector2(240f, -15f));
        Anchor(moneyText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        CreateText("MoneyUnit", moneyPanel.transform, "G", 20, TextAlignmentOptions.Center, new Vector2(20f, 24f), new Vector2(-30f, 0f));
        Anchor(moneyPanel.transform.Find("MoneyUnit").GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));

        var categoryPanel = CreatePanel("CategoryPanel", root.transform, new Vector2(280f, 700f), new Vector2(70f, -190f));
        var allButton = CreateButton("AllButton", categoryPanel.transform, "すべて", new Vector2(220f, 58f), new Vector2(30f, -70f));
        var consumableButton = CreateButton("ConsumableButton", categoryPanel.transform, "消耗品", new Vector2(220f, 58f), new Vector2(30f, -145f));
        var weaponButton = CreateButton("WeaponButton", categoryPanel.transform, "武器", new Vector2(220f, 58f), new Vector2(30f, -220f));
        var armorButton = CreateButton("ArmorButton", categoryPanel.transform, "防具", new Vector2(220f, 58f), new Vector2(30f, -295f));
        var accessoryButton = CreateButton("AccessoryButton", categoryPanel.transform, "装飾品", new Vector2(220f, 58f), new Vector2(30f, -370f));

        var listPanel = CreatePanel("RecipeListPanel", root.transform, new Vector2(800f, 700f), new Vector2(350f, -190f));
        var viewport = CreatePanel("Viewport", listPanel.transform, new Vector2(700f, 550f), new Vector2(20f, -110f));
        viewport.AddComponent<RectMask2D>();
        var content = CreateUIObject("Content", viewport.transform, new Vector2(700f, 550f));
        AnchorTopStretch(content.GetComponent<RectTransform>(), 0f, 0f, 0f);
        var scrollRect = listPanel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        CreateText("PanelTitle", listPanel.transform, "合成品", 26, TextAlignmentOptions.Left, new Vector2(180f, 36f), new Vector2(30f, -30f));
        CreateText("ListOwnedHeader", listPanel.transform, "所持", 20, TextAlignmentOptions.Center, new Vector2(70f, 32f), new Vector2(545f, -72f));
        CreateText("ListCostHeader", listPanel.transform, "費用", 20, TextAlignmentOptions.Right, new Vector2(112f, 32f), new Vector2(660f, -72f));

        var detailPanel = CreatePanel("DetailPanel", root.transform, new Vector2(640f, 700f), new Vector2(-70f, -190f));
        AnchorTopRight(detailPanel.GetComponent<RectTransform>());
        var detailIcon = CreateImage("DetailIcon", detailPanel.transform, new Vector2(88f, 88f), new Vector2(40f, -42f));
        AnchorTopLeft(detailIcon.rectTransform);
        detailIcon.preserveAspect = true;
        var detailTitle = CreateText("DetailTitle", detailPanel.transform, "合成品", 32, TextAlignmentOptions.Left, new Vector2(420f, 46f), new Vector2(150f, -42f));
        AnchorTopLeft(detailTitle.rectTransform);
        var detailTag = CreateText("DetailTag", detailPanel.transform, "", 22, TextAlignmentOptions.Left, new Vector2(300f, 34f), new Vector2(150f, -90f));
        AnchorTopLeft(detailTag.rectTransform);
        var detailDescription = CreateText("DetailDescription", detailPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(560f, 110f), new Vector2(40f, -150f));
        AnchorTopLeft(detailDescription.rectTransform);

        var helpPanel = CreatePanel("HelpPanel", root.transform, new Vector2(-140f, 100f), new Vector2(0f, 120f));
        AnchorBottomStretch(helpPanel.GetComponent<RectTransform>(), 70f, 70f, 120f);
        var helpText = CreateText("HelpText", helpPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(1640f, 58f), new Vector2(30f, -22f));
        AnchorTopStretch(helpText.rectTransform, 30f, 30f, 22f);
        var synthesizeButton = CreateButton("SynthesizeButton", detailPanel.transform, "合成する", new Vector2(220f, 58f), new Vector2(380f, -610f));

        var backButton = CreateButton("BackButton", root.transform, "戻る", new Vector2(140f, 52f), new Vector2(-220f, -60f));
        AnchorTopRight(backButton.GetComponent<RectTransform>());
        var switchButton = backButton.gameObject.AddComponent<UIScreenSwitchButton>();
        var switchSerialized = new SerializedObject(switchButton);
        switchSerialized.FindProperty("hideTargetName").stringValue = "SynthesisScreen";
        switchSerialized.FindProperty("showTargetName").stringValue = "HomeScreen";
        switchSerialized.ApplyModifiedPropertiesWithoutUndo();

        var controller = root.GetComponent<SynthesisScreenPreviewController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("detailTitleText").objectReferenceValue = detailTitle;
        serialized.FindProperty("detailTagText").objectReferenceValue = detailTag;
        serialized.FindProperty("detailIconImage").objectReferenceValue = detailIcon;
        serialized.FindProperty("detailDescriptionText").objectReferenceValue = detailDescription;
        serialized.FindProperty("materialPanelView").objectReferenceValue = null;
        serialized.FindProperty("resultScreenView").objectReferenceValue = null;
        serialized.FindProperty("resultScreenPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SynthesisResultScreenView>(ResultScreenPrefabPath);
        serialized.FindProperty("moneyText").objectReferenceValue = moneyText;
        serialized.FindProperty("synthesisLevelText").objectReferenceValue = synthesisLevelText;
        serialized.FindProperty("synthesizeButton").objectReferenceValue = synthesizeButton;
        serialized.FindProperty("actionButtonLabel").objectReferenceValue = synthesizeButton.transform.Find("Label").GetComponent<TMP_Text>();
        serialized.FindProperty("recipeRowPrefab").objectReferenceValue = rowPrefab;
        serialized.FindProperty("recipeScrollRect").objectReferenceValue = scrollRect;
        serialized.FindProperty("recipeRowViewport").objectReferenceValue = viewport.GetComponent<RectTransform>();
        serialized.FindProperty("recipeRowContent").objectReferenceValue = content.GetComponent<RectTransform>();
        serialized.FindProperty("allCategoryButton").objectReferenceValue = allButton;
        serialized.FindProperty("consumableCategoryButton").objectReferenceValue = consumableButton;
        serialized.FindProperty("weaponCategoryButton").objectReferenceValue = weaponButton;
        serialized.FindProperty("armorCategoryButton").objectReferenceValue = armorButton;
        serialized.FindProperty("accessoryCategoryButton").objectReferenceValue = accessoryButton;
        serialized.FindProperty("recipeDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SynthesisRecipeDatabase>(RecipeDatabasePath);
        serialized.FindProperty("itemDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        serialized.FindProperty("equipmentDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(EquipmentDatabasePath);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void ConfigureHomeSynthesisButton()
    {
        var root = PrefabUtility.LoadPrefabContents(HomeScreenPath);
        try
        {
            var synthesisItem = root.GetComponentsInChildren<HomeMenuItemView>(true)
                .FirstOrDefault(item => item.Description.Contains("素材を組み合わせて"));
            if (synthesisItem == null)
            {
                Debug.LogWarning("Synthesis home menu item was not found.");
                return;
            }

            var switchButton = synthesisItem.GetComponent<UIScreenSwitchButton>()
                ?? synthesisItem.gameObject.AddComponent<UIScreenSwitchButton>();
            var serialized = new SerializedObject(switchButton);
            serialized.FindProperty("hideTargetName").stringValue = "HomeScreen";
            serialized.FindProperty("showTargetName").stringValue = "SynthesisScreen";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HomeScreenPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var panel = CreateUIObject(name, parent, size, position);
        var image = panel.AddComponent<Image>();
        image.sprite = LoadWindowSprite();
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var imageObject = CreateUIObject(name, parent, size, position);
        var image = imageObject.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 size, Vector2 position)
    {
        var textObject = CreateUIObject(name, parent, size, position);
        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = TextColor;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 size, Vector2 position)
    {
        var buttonObject = CreateUIObject(name, parent, size, position);
        var image = buttonObject.AddComponent<Image>();
        image.sprite = LoadWindowSprite();
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var hover = buttonObject.AddComponent<WindowHoverSpriteView>();
        var hoverSerialized = new SerializedObject(hover);
        hoverSerialized.FindProperty("windowImage").objectReferenceValue = image;
        hoverSerialized.FindProperty("normalWindowSprite").objectReferenceValue = LoadWindowSprite();
        hoverSerialized.FindProperty("highlightedWindowSprite").objectReferenceValue = LoadWindowHoverSprite();
        hoverSerialized.ApplyModifiedPropertiesWithoutUndo();
        var label = CreateText("Label", buttonObject.transform, text, 22, TextAlignmentOptions.Center, size, Vector2.zero);
        Stretch(label.rectTransform);
        label.color = AccentColor;
        return button;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size)
    {
        return CreateUIObject(name, parent, size, Vector2.zero);
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        var rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        AnchorTopLeft(rect);
        return gameObject;
    }

    private static void AnchorTopLeft(RectTransform rect)
    {
        Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    }

    private static void AnchorTopRight(RectTransform rect)
    {
        Anchor(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
    }

    private static void AnchorTopStretch(RectTransform rect, float left, float right, float top)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
        rect.sizeDelta = new Vector2(-(left + right), rect.sizeDelta.y);
    }

    private static void AnchorBottomStretch(RectTransform rect, float left, float right, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, bottom);
        rect.sizeDelta = new Vector2(-(left + right), rect.sizeDelta.y);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets/UI", "Synthesis");
        CreateFolder(SynthesisRoot, "Prefabs");
        CreateFolder(SynthesisRoot, "Backgrounds");
    }

    private static void CreateFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static Sprite LoadWindowSprite()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(WindowSpritePath);
    }

    private static Sprite LoadWindowHoverSprite()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(WindowHoverSpritePath);
    }
}
