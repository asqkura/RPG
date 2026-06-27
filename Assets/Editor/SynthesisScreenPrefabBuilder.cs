using RPG.MasterData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SynthesisScreenPrefabBuilder
{
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";
    private const string ScrollBarSpritePath = "Assets/UI/Windows/Sprites/ScrollBar.png";
    private const string ScrollHandleSpritePath = "Assets/UI/Windows/Sprites/ScrollHandle.png";
    private const string BackgroundPath = "Assets/UI/Synthesis/Backgrounds/SynthesisWorkshopBackground.png";
    private const string TestItemDatabasePath = "Assets/MasterData/Test/Databases/TestItemDatabase.asset";
    private const string TestEquipmentDatabasePath = "Assets/MasterData/Test/Databases/TestEquipmentDatabase.asset";
    private const string TestRecipeDatabasePath = "Assets/MasterData/Test/Databases/TestRecipeDatabase.asset";
    private const string PrefabFolder = "Assets/UI/Synthesis/Prefabs";
    private const string RowPrefabPath = PrefabFolder + "/SynthesisRecipeRow.prefab";
    private const string ResultPopupPrefabPath = PrefabFolder + "/SynthesisResultPopup.prefab";
    private const string SynthesisScreenPrefabPath = PrefabFolder + "/SynthesisScreen.prefab";

    private const float ScreenMargin = 70f;
    private const float PanelTop = -230f;
    private const float MainPanelHeight = 610f;
    private const float ListPanelWidth = 820f;
    private const float DetailPanelWidth = 660f;

    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color MutedTextColor = new(0.62f, 0.58f, 0.52f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color PopupPanelColor = new(0.08f, 0.075f, 0.065f, 0.96f);
    private static readonly Color PopupBackdropColor = new(0f, 0f, 0f, 0.58f);

    [MenuItem("Tools/RPG/Build Synthesis Screen UI")]
    public static void Build()
    {
        EnsureFolders();
        ConfigureTextureImporters();

        var windowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowSpritePath);
        var hoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowHoverSpritePath);
        var scrollBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ScrollBarSpritePath);
        var scrollHandleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ScrollHandleSpritePath);
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

        if (windowSprite == null || hoverSprite == null || scrollBarSprite == null || scrollHandleSprite == null || backgroundSprite == null)
        {
            Debug.LogError("Synthesis screen build failed. Missing UI sprite or background sprite.");
            return;
        }

        var rowPrefab = CreateRowPrefab();
        var resultPopupPrefab = CreateResultPopupPrefab(windowSprite);
        CreateSynthesisScreenPrefab(windowSprite, hoverSprite, scrollBarSprite, scrollHandleSprite, backgroundSprite, rowPrefab, resultPopupPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Synthesis screen UI prefab was generated.");
    }

    private static GameObject CreateRowPrefab()
    {
        var root = CreateRectObject("SynthesisRecipeRow");
        Anchor(root, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 50));

        var image = root.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        var selectionBackground = CreateImage("SelectionBackground", root, null, Image.Type.Simple, Color.clear);
        selectionBackground.raycastTarget = false;
        Stretch(selectionBackground, new Vector2(12, 2), new Vector2(-12, -2));

        var icon = CreateImage("Icon", root, null, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(28, 28));

        var name = CreateText("Name", root, "ポーション", 23, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(202, 0), new Vector2(330, 36));
        var level = CreateText("RequiredLevel", root, "Lv1", 22, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-246, 0), new Vector2(76, 36));
        var owned = CreateText("Owned", root, "0", 22, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-154, 0), new Vector2(70, 36));
        var cost = CreateText("Cost", root, "20 G", 22, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-58, 0), new Vector2(102, 36));

        var view = root.gameObject.AddComponent<ShopItemRowView>();
        var viewObject = new SerializedObject(view);
        viewObject.FindProperty("windowImage").objectReferenceValue = selectionBackground;
        viewObject.FindProperty("selectionMarker").objectReferenceValue = null;
        viewObject.FindProperty("normalWindowSprite").objectReferenceValue = null;
        viewObject.FindProperty("highlightedWindowSprite").objectReferenceValue = null;
        viewObject.FindProperty("iconImage").objectReferenceValue = icon;
        viewObject.FindProperty("nameLabel").objectReferenceValue = name;
        viewObject.FindProperty("stockLabel").objectReferenceValue = level;
        viewObject.FindProperty("ownedLabel").objectReferenceValue = owned;
        viewObject.FindProperty("priceLabel").objectReferenceValue = cost;
        var labelsProperty = viewObject.FindProperty("labelTexts");
        labelsProperty.arraySize = 4;
        labelsProperty.GetArrayElementAtIndex(0).objectReferenceValue = name;
        labelsProperty.GetArrayElementAtIndex(1).objectReferenceValue = level;
        labelsProperty.GetArrayElementAtIndex(2).objectReferenceValue = owned;
        labelsProperty.GetArrayElementAtIndex(3).objectReferenceValue = cost;
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static GameObject CreateResultPopupPrefab(Sprite windowSprite)
    {
        var root = CreateRectObject("SynthesisResultPopup");
        Stretch(root, Vector2.zero, Vector2.zero);

        var backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = PopupBackdropColor;

        var backdropButton = root.gameObject.AddComponent<Button>();
        backdropButton.targetGraphic = backdrop;

        var panel = CreateImage("Panel", root, windowSprite, Image.Type.Sliced, PopupPanelColor);
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 320));

        CreateText("Title", panel.transform, "合成完了", 28, TextAlignmentOptions.Center, AccentTextColor,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(440, 42));

        var icon = CreateImage("ResultIcon", panel.transform, null, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Anchor(icon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(82, -118), new Vector2(80, 80));

        CreateText("ResultName", panel.transform, "作成物名", 24, TextAlignmentOptions.MidlineLeft, AccentTextColor,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(142, -96), new Vector2(-178, 36));
        CreateText("ResultRarity", panel.transform, "入手数: 1", 20, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(142, -134), new Vector2(-178, 30));
        CreateText("ResultDetail", panel.transform, "合成結果の説明が入ります。", 19, TextAlignmentOptions.TopLeft, TextColor,
            Vector2.zero, Vector2.one, new Vector2(0, -42), new Vector2(-80, -180));

        var closeImage = CreateImage("CloseButton", panel.transform, windowSprite, Image.Type.Sliced, new Color(0.32f, 0.25f, 0.14f, 1f));
        Anchor(closeImage, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(160, 44));
        var closeButton = closeImage.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        CreateText("Label", closeImage.transform, "閉じる", 22, TextAlignmentOptions.Center, AccentTextColor,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ResultPopupPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static void CreateSynthesisScreenPrefab(
        Sprite windowSprite,
        Sprite hoverSprite,
        Sprite scrollBarSprite,
        Sprite scrollHandleSprite,
        Sprite backgroundSprite,
        GameObject rowPrefab,
        GameObject resultPopupPrefab)
    {
        var root = new GameObject("SynthesisScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreateImage("Background", root.transform, backgroundSprite, Image.Type.Simple, Color.white);
        Stretch(background, Vector2.zero, Vector2.zero);

        CreateText("Title", root.transform, "合成", 46, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 130, -73), new Vector2(260, 46));

        var titleLine = CreateImage("TitleDivider", root.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(titleLine, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -130), new Vector2(300, 2));

        var moneyPanel = CreateImage("MoneyPanel", root.transform, windowSprite, Image.Type.Sliced, Color.white);
        Anchor(moneyPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-390, -70), new Vector2(300, 60));
        CreateText("Label", moneyPanel.transform, "所持金", 20, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(76, 10), new Vector2(120, 28));
        var moneyText = CreateText("Value", moneyPanel.transform, "12,480 G", 28, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-105, -6), new Vector2(180, 38));

        var backButton = CreateImage("BackButton", root.transform, windowSprite, Image.Type.Sliced, Color.white);
        Anchor(backButton, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(ScreenMargin + 80), -70), new Vector2(160, 60));
        backButton.gameObject.AddComponent<Button>();
        AddWindowHover(backButton.gameObject, backButton, windowSprite, hoverSprite);
        ConfigureScreenSwitch(backButton.gameObject, "SynthesisScreen", "HomeScreen");
        CreateText("Label", backButton.transform, "戻る", 28, TextAlignmentOptions.Center, TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 44));

        var listPanel = CreateImage("RecipeListPanel", root.transform, windowSprite, Image.Type.Sliced, Color.white);
        AnchorFromLeftTop(listPanel, ScreenMargin, PanelTop, ListPanelWidth, MainPanelHeight);
        CreateText("PanelTitle", listPanel.transform, "レシピ一覧", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -40), new Vector2(150, 34));
        CreateText("CategoryLabel", listPanel.transform, "分類", 24, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(398, -42), new Vector2(70, 34));
        var consumableCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "ConsumableCategory", "消耗品", new Vector2(488, -42), true);
        var weaponCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "WeaponCategory", "武器", new Vector2(592, -42), false);
        var armorCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "ArmorCategory", "防具", new Vector2(696, -42), false);
        var accessoryCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "AccessoryCategory", "装飾", new Vector2(800, -42), false);

        var headerLine = CreateImage("HeaderLine", listPanel.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(headerLine, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -91.5f), new Vector2(-104, 1));
        headerLine.raycastTarget = false;

        CreateColumnHeader(listPanel.transform, "レシピ名", 108, -116, 140);
        var levelHeader = CreateColumnHeader(listPanel.transform, "必要Lv", 500, -116, 80);
        var ownedHeader = CreateColumnHeader(listPanel.transform, "所持", 598, -116, 70);
        var costHeader = CreateColumnHeader(listPanel.transform, "費用", 688, -116, 80);

        var rowViewport = CreateRectObject("ItemRowViewport");
        rowViewport.transform.SetParent(listPanel.transform, false);
        Stretch(rowViewport, new Vector2(52, 36), new Vector2(-52, -148));
        rowViewport.gameObject.AddComponent<Image>().color = Color.clear;
        rowViewport.gameObject.AddComponent<RectMask2D>();

        var rowContent = CreateRectObject("ItemRowContent");
        rowContent.transform.SetParent(rowViewport, false);
        rowContent.anchorMin = new Vector2(0, 1);
        rowContent.anchorMax = new Vector2(1, 1);
        rowContent.pivot = new Vector2(0.5f, 1f);
        rowContent.anchoredPosition = Vector2.zero;
        rowContent.sizeDelta = Vector2.zero;

        var scrollRect = rowViewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = rowViewport;
        scrollRect.content = rowContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        var itemScrollbar = CreateVerticalScrollbar(listPanel.transform, scrollBarSprite, scrollHandleSprite);
        scrollRect.verticalScrollbar = itemScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        var detailPanel = CreateImage("DetailPanel", root.transform, windowSprite, Image.Type.Sliced, Color.white);
        AnchorFromRightTop(detailPanel, ScreenMargin, PanelTop, DetailPanelWidth, MainPanelHeight);
        CreateText("PanelTitle", detailPanel.transform, "詳細", 22, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -36), new Vector2(100, 32));

        var detailIcon = CreateImage("DetailIcon", detailPanel.transform, null, Image.Type.Simple, Color.white);
        detailIcon.preserveAspect = true;
        detailIcon.enabled = false;
        detailIcon.raycastTarget = false;
        Anchor(detailIcon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(78, -106), new Vector2(34, 34));
        var detailTitle = CreateText("DetailTitle", detailPanel.transform, "", 32, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(310, -82), new Vector2(420, 48));
        var detailBody = CreateText("DetailBody", detailPanel.transform, "", 21, TextAlignmentOptions.TopLeft, TextColor,
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Stretch(detailBody, new Vector2(48, -252), new Vector2(-48, -172));

        CreateText("IngredientTitle", detailPanel.transform, "必要素材", 18, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -278), new Vector2(120, 26));
        var ingredientIconImages = new Image[3];
        var ingredientNameTexts = new TMP_Text[3];
        var ingredientCountTexts = new TMP_Text[3];
        for (var i = 0; i < 3; i++)
        {
            CreateIngredientRow(detailPanel.transform, i, new Vector2(92, -316 - i * 42), out ingredientIconImages[i], out ingredientNameTexts[i], out ingredientCountTexts[i]);
        }

        var detailLevel = CreateDetailStat(detailPanel.transform, "必要Lv", "Lv1", new Vector2(92, -462));
        var detailOwned = CreateDetailStat(detailPanel.transform, "所持", "0", new Vector2(244, -462));
        var detailCost = CreateDetailStat(detailPanel.transform, "費用", "120 G", new Vector2(416, -462));

        var synthesizeButton = CreateImage("SynthesizeButton", detailPanel.transform, windowSprite, Image.Type.Sliced, Color.white);
        Anchor(synthesizeButton, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(250, 58));
        synthesizeButton.gameObject.AddComponent<Button>();
        AddWindowHover(synthesizeButton.gameObject, synthesizeButton, windowSprite, hoverSprite);
        var actionButtonLabel = CreateText("Label", synthesizeButton.transform, "作成", 30, TextAlignmentOptions.Center, AccentTextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 44));

        var helpPanel = CreateImage("HelpPanel", root.transform, windowSprite, Image.Type.Sliced, Color.white);
        StretchToBottom(helpPanel.GetComponent<RectTransform>(), ScreenMargin, ScreenMargin, 40, 100);
        var helpText = CreateText("HelpText", helpPanel.transform, "", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Stretch(helpText, new Vector2(52, 14), new Vector2(-52, -14));

        var controller = root.AddComponent<SynthesisScreenPreviewController>();
        var controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("detailTitleText").objectReferenceValue = detailTitle;
        controllerObject.FindProperty("detailIconImage").objectReferenceValue = detailIcon;
        controllerObject.FindProperty("detailBodyText").objectReferenceValue = detailBody;
        controllerObject.FindProperty("detailStockText").objectReferenceValue = detailLevel;
        controllerObject.FindProperty("detailOwnedText").objectReferenceValue = detailOwned;
        controllerObject.FindProperty("detailPriceText").objectReferenceValue = detailCost;
        SetObjectReferenceArray(controllerObject.FindProperty("ingredientIconImages"), ingredientIconImages);
        SetObjectReferenceArray(controllerObject.FindProperty("ingredientNameTexts"), ingredientNameTexts);
        SetObjectReferenceArray(controllerObject.FindProperty("ingredientCountTexts"), ingredientCountTexts);
        controllerObject.FindProperty("helpText").objectReferenceValue = helpText;
        controllerObject.FindProperty("moneyText").objectReferenceValue = moneyText;
        controllerObject.FindProperty("synthesizeButton").objectReferenceValue = synthesizeButton.GetComponent<Button>();
        controllerObject.FindProperty("actionButtonLabel").objectReferenceValue = actionButtonLabel;
        controllerObject.FindProperty("stockHeaderText").objectReferenceValue = levelHeader;
        controllerObject.FindProperty("ownedHeaderText").objectReferenceValue = ownedHeader;
        controllerObject.FindProperty("priceHeaderText").objectReferenceValue = costHeader;
        controllerObject.FindProperty("itemRowPrefab").objectReferenceValue = rowPrefab.GetComponent<ShopItemRowView>();
        controllerObject.FindProperty("itemScrollRect").objectReferenceValue = scrollRect;
        controllerObject.FindProperty("itemRowViewport").objectReferenceValue = rowViewport;
        controllerObject.FindProperty("itemRowContent").objectReferenceValue = rowContent;
        controllerObject.FindProperty("consumableCategoryButton").objectReferenceValue = consumableCategoryButton;
        controllerObject.FindProperty("weaponCategoryButton").objectReferenceValue = weaponCategoryButton;
        controllerObject.FindProperty("armorCategoryButton").objectReferenceValue = armorCategoryButton;
        controllerObject.FindProperty("accessoryCategoryButton").objectReferenceValue = accessoryCategoryButton;
        controllerObject.FindProperty("resultPopupPrefab").objectReferenceValue = resultPopupPrefab;
        controllerObject.FindProperty("recipeDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(TestRecipeDatabasePath);
        controllerObject.FindProperty("itemDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDatabase>(TestItemDatabasePath);
        controllerObject.FindProperty("equipmentDatabase").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(TestEquipmentDatabasePath);
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SynthesisScreenPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Button CreateCategoryButton(Transform parent, Sprite windowSprite, Sprite hoverSprite, string name, string label, Vector2 anchoredPosition, bool selected)
    {
        var image = CreateImage(name, parent, selected ? hoverSprite : windowSprite, Image.Type.Sliced, Color.white);
        Anchor(image, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(92, 44));
        var button = image.gameObject.AddComponent<Button>();
        AddWindowHover(image.gameObject, image, windowSprite, hoverSprite);
        CreateText("Label", image.transform, label, 24, TextAlignmentOptions.Center, selected ? AccentTextColor : TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 36));
        return button;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform parent, Sprite barSprite, Sprite handleSprite)
    {
        var track = CreateImage("ItemScrollbar", parent, barSprite, Image.Type.Sliced, Color.white);
        Anchor(track, new Vector2(0, 1), new Vector2(0, 1), new Vector2(790, -360), new Vector2(27, 424));

        var slidingArea = CreateRectObject("SlidingArea");
        slidingArea.transform.SetParent(track.transform, false);
        slidingArea.anchorMin = new Vector2(0.5f, 0f);
        slidingArea.anchorMax = new Vector2(0.5f, 1f);
        slidingArea.sizeDelta = new Vector2(20f, 0f);

        var handle = CreateImage("Handle", slidingArea, handleSprite, Image.Type.Sliced, Color.white);
        Stretch(handle, Vector2.zero, Vector2.zero);

        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handle;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.value = 1f;
        scrollbar.size = 1f;
        track.gameObject.SetActive(false);
        return scrollbar;
    }

    private static TMP_Text CreateColumnHeader(Transform parent, string label, float x, float y, float width)
    {
        return CreateText(label + "Header", parent, label, 18, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(width, 28));
    }

    private static TMP_Text CreateDetailStat(Transform parent, string label, string value, Vector2 anchoredPosition)
    {
        var root = CreateRectObject(label + "Stat");
        root.transform.SetParent(parent, false);
        Anchor(root, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(130, 56));
        CreateText("Label", root, label, 18, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -14), new Vector2(80, 24));
        return CreateText("Value", root, value, 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -40), new Vector2(90, 30));
    }

    private static void CreateIngredientRow(
        Transform parent,
        int index,
        Vector2 anchoredPosition,
        out Image icon,
        out TMP_Text nameText,
        out TMP_Text countText)
    {
        var root = CreateRectObject("IngredientRow" + (index + 1));
        root.transform.SetParent(parent, false);
        root.pivot = new Vector2(0f, 0.5f);
        Anchor(root, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(480, 34));

        icon = CreateImage("Icon", root, null, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(26, 26));

        nameText = CreateText("Name", root, "", 20, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(166, 0), new Vector2(250, 30));
        countText = CreateText("Count", root, "", 20, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-42, 0), new Vector2(96, 30));
        root.gameObject.SetActive(false);
    }

    private static void SetObjectReferenceArray(SerializedProperty property, Object[] references)
    {
        property.arraySize = references.Length;

        for (var i = 0; i < references.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
        }
    }

    private static void AddWindowHover(GameObject target, Image image, Sprite windowSprite, Sprite hoverSprite)
    {
        var hover = target.AddComponent<WindowHoverSpriteView>();
        var hoverObject = new SerializedObject(hover);
        hoverObject.FindProperty("windowImage").objectReferenceValue = image;
        hoverObject.FindProperty("normalWindowSprite").objectReferenceValue = windowSprite;
        hoverObject.FindProperty("highlightedWindowSprite").objectReferenceValue = hoverSprite;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureScreenSwitch(GameObject target, string hideTargetName, string showTargetName)
    {
        var switchButton = target.GetComponent<UIScreenSwitchButton>() ?? target.AddComponent<UIScreenSwitchButton>();
        var switchObject = new SerializedObject(switchButton);
        switchObject.FindProperty("hideTargetName").stringValue = hideTargetName;
        switchObject.FindProperty("showTargetName").stringValue = showTargetName;
        switchObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Image.Type type, Color color)
    {
        var rect = CreateRectObject(name);
        rect.transform.SetParent(parent, false);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var rect = CreateRectObject(name);
        rect.transform.SetParent(parent, false);
        Anchor(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);

        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRectObject(string name)
    {
        return new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
    }

    private static void Stretch(Component component, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = component.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void AnchorFromLeftTop(Component component, float left, float top, float width, float height)
    {
        Anchor(component, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left + width * 0.5f, top - height * 0.5f), new Vector2(width, height));
    }

    private static void AnchorFromRightTop(Component component, float right, float top, float width, float height)
    {
        Anchor(component, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(right + width * 0.5f), top - height * 0.5f), new Vector2(width, height));
    }

    private static void Anchor(Component component, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Anchor(component.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPosition, sizeDelta);
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void StretchToBottom(RectTransform rect, float left, float right, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void ConfigureTextureImporters()
    {
        ConfigureSprite(WindowSpritePath, new Vector4(13, 13, 13, 13), FilterMode.Point);
        ConfigureSprite(WindowHoverSpritePath, new Vector4(13, 13, 13, 13), FilterMode.Point);
        ConfigureSprite(ScrollBarSpritePath, new Vector4(10, 30, 10, 30), FilterMode.Point);
        ConfigureSprite(ScrollHandleSpritePath, new Vector4(10, 30, 10, 30), FilterMode.Point);
        ConfigureSprite(BackgroundPath, Vector4.zero, FilterMode.Bilinear);
    }

    private static void ConfigureSprite(string path, Vector4 border, FilterMode filterMode)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = filterMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "UI");
        CreateFolder("Assets/UI", "Synthesis");
        CreateFolder("Assets/UI/Synthesis", "Backgrounds");
        CreateFolder("Assets/UI/Synthesis", "Prefabs");
    }

    private static void CreateFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
