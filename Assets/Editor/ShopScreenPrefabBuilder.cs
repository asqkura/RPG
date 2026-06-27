using RPG.MasterData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ShopScreenPrefabBuilder
{
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";
    private const string ScrollBarSpritePath = "Assets/UI/Windows/Sprites/ScrollBar.png";
    private const string ScrollHandleSpritePath = "Assets/UI/Windows/Sprites/ScrollHandle.png";
    private const string BackgroundPath = "Assets/UI/Shop/Backgrounds/ShopInteriorBackground.png";
    private const string TestItemDatabasePath = "Assets/MasterData/Test/Databases/TestItemDatabase.asset";
    private const string TestEquipmentDatabasePath = "Assets/MasterData/Test/Databases/TestEquipmentDatabase.asset";
    private const string TestShopItemDatabasePath = "Assets/MasterData/Test/Databases/TestShopItemDatabase.asset";
    private const string PrefabFolder = "Assets/UI/Shop/Prefabs";
    private const string RowPrefabPath = PrefabFolder + "/ShopItemRow.prefab";
    private const string ShopScreenPrefabPath = PrefabFolder + "/ShopScreen.prefab";
    private const float ScreenMargin = 70f;
    private const float PanelTop = -230f;
    private const float MainPanelHeight = 610f;
    private const float ItemListPanelLeft = ScreenMargin;
    private const float ItemListPanelWidth = 820f;
    private const float DetailPanelRight = ScreenMargin;
    private const float DetailPanelWidth = 660f;
    private const float ShopRowHeight = 50f;

    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color MutedTextColor = new(0.62f, 0.58f, 0.52f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color WindowSpriteColor = Color.white;

    [MenuItem("Tools/RPG/Build Shop Screen UI")]
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
            Debug.LogError("Shop screen build failed. Missing window sprite, hover sprite, scroll sprite, or background sprite.");
            return;
        }

        var rowPrefab = CreateRowPrefab(windowSprite);
        CreateShopScreenPrefab(windowSprite, hoverSprite, scrollBarSprite, scrollHandleSprite, backgroundSprite, rowPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Shop screen UI prefab was generated.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "UI");
        CreateFolder("Assets/UI", "Shop");
        CreateFolder("Assets/UI/Shop", "Backgrounds");
        CreateFolder("Assets/UI/Shop", "Prefabs");
    }

    private static void CreateFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
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

    private static GameObject CreateRowPrefab(Sprite windowSprite)
    {
        var root = CreateRectObject("ShopItemRow");
        SetSize(root, 710, ShopRowHeight);
        var image = root.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.clear;

        var selectionBackground = CreateImage("SelectionBackground", root.transform, null, Image.Type.Simple, Color.clear);
        selectionBackground.raycastTarget = false;
        Stretch(selectionBackground, new Vector2(20, 3), new Vector2(-20, -3));

        var icon = CreateImage("Icon", root.transform, null, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(33, 0), new Vector2(28, 28));

        CreateText("Name", root.transform, "ポーション", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(220, 0), new Vector2(320, 36));
        CreateText("Stock", root.transform, "-", 24, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-250, 0), new Vector2(70, 36));
        CreateText("Owned", root.transform, "3", 24, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-164, 0), new Vector2(70, 36));
        CreateText("Price", root.transform, "80 G", 24, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-64, 0), new Vector2(100, 36));

        var view = root.gameObject.AddComponent<ShopItemRowView>();
        var viewObject = new SerializedObject(view);
        viewObject.FindProperty("windowImage").objectReferenceValue = selectionBackground;
        viewObject.FindProperty("selectionMarker").objectReferenceValue = null;
        viewObject.FindProperty("normalWindowSprite").objectReferenceValue = null;
        viewObject.FindProperty("highlightedWindowSprite").objectReferenceValue = null;
        viewObject.FindProperty("iconImage").objectReferenceValue = icon;
        viewObject.FindProperty("nameLabel").objectReferenceValue = root.transform.Find("Name").GetComponent<TMP_Text>();
        viewObject.FindProperty("stockLabel").objectReferenceValue = root.transform.Find("Stock").GetComponent<TMP_Text>();
        viewObject.FindProperty("ownedLabel").objectReferenceValue = root.transform.Find("Owned").GetComponent<TMP_Text>();
        viewObject.FindProperty("priceLabel").objectReferenceValue = root.transform.Find("Price").GetComponent<TMP_Text>();
        var labelsProperty = viewObject.FindProperty("labelTexts");
        labelsProperty.arraySize = 4;
        labelsProperty.GetArrayElementAtIndex(0).objectReferenceValue = root.transform.Find("Name").GetComponent<TMP_Text>();
        labelsProperty.GetArrayElementAtIndex(1).objectReferenceValue = root.transform.Find("Stock").GetComponent<TMP_Text>();
        labelsProperty.GetArrayElementAtIndex(2).objectReferenceValue = root.transform.Find("Owned").GetComponent<TMP_Text>();
        labelsProperty.GetArrayElementAtIndex(3).objectReferenceValue = root.transform.Find("Price").GetComponent<TMP_Text>();
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static void CreateShopScreenPrefab(
        Sprite windowSprite,
        Sprite hoverSprite,
        Sprite scrollBarSprite,
        Sprite scrollHandleSprite,
        Sprite backgroundSprite,
        GameObject rowPrefab)
    {
        var root = new GameObject("ShopScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreateImage("Background", root.transform, backgroundSprite, Image.Type.Simple, Color.white);
        Stretch(background, Vector2.zero, Vector2.zero);
        background.preserveAspect = false;

        CreateText("Title", root.transform, "ショップ", 46, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 130, -73), new Vector2(260, 46));
        var titleLine = CreateImage("TitleDivider", root.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(titleLine, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -130), new Vector2(300, 2));

        var moneyPanel = CreateImage("MoneyPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(moneyPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-390, -70), new Vector2(300, 60));
        CreateText("Label", moneyPanel.transform, "所持金", 20, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(76, 10), new Vector2(120, 28));
        var moneyText = CreateText("Value", moneyPanel.transform, "12,480 G", 28, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-105, -6), new Vector2(180, 38));

        var backButton = CreateImage("BackButton", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(backButton, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(ScreenMargin + 80), -70), new Vector2(160, 60));
        backButton.gameObject.AddComponent<Button>();
        AddWindowHover(backButton.gameObject, backButton, windowSprite, hoverSprite);
        ConfigureScreenSwitch(backButton.gameObject, "ShopScreen", "HomeScreen");
        CreateText("Label", backButton.transform, "戻る", 28, TextAlignmentOptions.Center, TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 44));

        var listPanel = CreateImage("ItemListPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        AnchorFromLeftTop(listPanel, ItemListPanelLeft, PanelTop, ItemListPanelWidth, MainPanelHeight);
        var buyTabButton = CreateTab(root.transform, windowSprite, hoverSprite, "BuyTab", "購入", new Vector2(ItemListPanelLeft + 90, PanelTop + 20), true);
        var sellTabButton = CreateTab(root.transform, windowSprite, hoverSprite, "SellTab", "売却", new Vector2(ItemListPanelLeft + 230, PanelTop + 20), false);
        CreateText("PanelTitle", listPanel.transform, "商品一覧", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -40), new Vector2(150, 34));
        CreateText("CategoryLabel", listPanel.transform, "分類", 24, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(472, -42), new Vector2(70, 34));
        var consumableCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "ConsumableCategory", "消耗品", new Vector2(562, -42), true);
        var materialCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "MaterialCategory", "素材", new Vector2(666, -42), false);
        var equipmentCategoryButton = CreateCategoryButton(listPanel.transform, windowSprite, hoverSprite, "EquipmentCategory", "装備", new Vector2(770, -42), false);

        var headerLine = CreateImage("HeaderLine", listPanel.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(headerLine, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), Vector2.zero);
        var headerLineRect = headerLine.GetComponent<RectTransform>();
        headerLineRect.offsetMin = new Vector2(52, -92);
        headerLineRect.offsetMax = new Vector2(-52, -91);
        headerLine.raycastTarget = false;

        CreateColumnHeader(listPanel.transform, "商品名", 108, -116, 140);
        var stockHeader = CreateColumnHeader(listPanel.transform, "在庫", 512, -116, 70);
        var ownedHeader = CreateColumnHeader(listPanel.transform, "所持", 598, -116, 70);
        var priceHeader = CreateColumnHeader(listPanel.transform, "価格", 688, -116, 80);

        var rowViewport = CreateRectObject("ItemRowViewport");
        rowViewport.transform.SetParent(listPanel.transform, false);
        Stretch(rowViewport, new Vector2(52, 36), new Vector2(-52, -148));
        var viewportImage = rowViewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
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
        scrollRect.verticalScrollbarSpacing = 0f;

        var detailPanel = CreateImage("DetailPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        AnchorFromRightTop(detailPanel, DetailPanelRight, PanelTop, DetailPanelWidth, MainPanelHeight);

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
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
        Stretch(detailBody, new Vector2(48, -332), new Vector2(-48, -172));

        var detailStock = CreateDetailStat(detailPanel.transform, "在庫", "-", new Vector2(92, -394));
        var detailOwned = CreateDetailStat(detailPanel.transform, "所持", "3", new Vector2(244, -394));
        var detailPrice = CreateDetailStat(detailPanel.transform, "価格", "80 G", new Vector2(416, -394));

        var buyButton = CreateImage("BuyButton", detailPanel.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(buyButton, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(250, 58));
        buyButton.gameObject.AddComponent<Button>();
        AddWindowHover(buyButton.gameObject, buyButton, windowSprite, hoverSprite);
        var actionButtonLabel = CreateText("Label", buyButton.transform, "購入", 30, TextAlignmentOptions.Center, AccentTextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 44));

        var helpPanel = CreateImage("HelpPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        StretchToBottom(helpPanel.GetComponent<RectTransform>(), ScreenMargin, ScreenMargin, 40, 100);
        var helpText = CreateText("HelpText", helpPanel.transform, "", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Stretch(helpText, new Vector2(52, 14), new Vector2(-52, -14));

        var controller = root.AddComponent<ShopScreenPreviewController>();
        var controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("detailTitleText").objectReferenceValue = detailTitle;
        controllerObject.FindProperty("detailIconImage").objectReferenceValue = detailIcon;
        controllerObject.FindProperty("detailBodyText").objectReferenceValue = detailBody;
        controllerObject.FindProperty("detailStockText").objectReferenceValue = detailStock;
        controllerObject.FindProperty("detailOwnedText").objectReferenceValue = detailOwned;
        controllerObject.FindProperty("detailPriceText").objectReferenceValue = detailPrice;
        controllerObject.FindProperty("helpText").objectReferenceValue = helpText;
        controllerObject.FindProperty("moneyText").objectReferenceValue = moneyText;
        controllerObject.FindProperty("buyButton").objectReferenceValue = buyButton.GetComponent<Button>();
        controllerObject.FindProperty("actionButtonLabel").objectReferenceValue = actionButtonLabel;
        controllerObject.FindProperty("stockHeaderText").objectReferenceValue = stockHeader;
        controllerObject.FindProperty("ownedHeaderText").objectReferenceValue = ownedHeader;
        controllerObject.FindProperty("priceHeaderText").objectReferenceValue = priceHeader;
        controllerObject.FindProperty("itemRowPrefab").objectReferenceValue = rowPrefab.GetComponent<ShopItemRowView>();
        controllerObject.FindProperty("itemScrollRect").objectReferenceValue = scrollRect;
        controllerObject.FindProperty("itemRowViewport").objectReferenceValue = rowViewport;
        controllerObject.FindProperty("itemRowContent").objectReferenceValue = rowContent;
        controllerObject.FindProperty("buyTabButton").objectReferenceValue = buyTabButton;
        controllerObject.FindProperty("sellTabButton").objectReferenceValue = sellTabButton;
        controllerObject.FindProperty("consumableCategoryButton").objectReferenceValue = consumableCategoryButton;
        controllerObject.FindProperty("materialCategoryButton").objectReferenceValue = materialCategoryButton;
        controllerObject.FindProperty("equipmentCategoryButton").objectReferenceValue = equipmentCategoryButton;
        controllerObject.FindProperty("shopItemDatabase").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ShopItemDatabase>(TestShopItemDatabasePath);
        controllerObject.FindProperty("itemDatabase").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ItemDatabase>(TestItemDatabasePath);
        controllerObject.FindProperty("equipmentDatabase").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(TestEquipmentDatabasePath);
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ShopScreenPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Button CreateTab(
        Transform parent,
        Sprite windowSprite,
        Sprite hoverSprite,
        string name,
        string label,
        Vector2 anchoredPosition,
        bool selected)
    {
        var tab = CreateImage(name, parent, selected ? hoverSprite : windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(tab, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(130, 50));
        var button = tab.gameObject.AddComponent<Button>();
        AddWindowHover(tab.gameObject, tab, windowSprite, hoverSprite);
        CreateText("Label", tab.transform, label, 24, TextAlignmentOptions.Center, selected ? AccentTextColor : TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 38));
        return button;
    }

    private static Button CreateCategoryButton(
        Transform parent,
        Sprite windowSprite,
        Sprite hoverSprite,
        string name,
        string label,
        Vector2 anchoredPosition,
        bool selected)
    {
        var button = CreateImage(name, parent, selected ? hoverSprite : windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(button, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(92, 44));
        var buttonComponent = button.gameObject.AddComponent<Button>();
        AddWindowHover(button.gameObject, button, windowSprite, hoverSprite);
        CreateText("Label", button.transform, label, 24, TextAlignmentOptions.Center, selected ? AccentTextColor : TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 36));
        return buttonComponent;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform parent, Sprite barSprite, Sprite handleSprite)
    {
        var track = CreateImage("ItemScrollbar", parent, barSprite, Image.Type.Sliced, Color.white);
        Anchor(track, new Vector2(0, 1), new Vector2(0, 1), new Vector2(790, -360), new Vector2(27, 424));

        var slidingArea = CreateRectObject("SlidingArea");
        slidingArea.transform.SetParent(track.transform, false);
        slidingArea.anchorMin = new Vector2(0.5f, 0f);
        slidingArea.anchorMax = new Vector2(0.5f, 1f);
        slidingArea.pivot = new Vector2(0.5f, 0.5f);
        slidingArea.anchoredPosition = Vector2.zero;
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

    private static RectTransform CreateRectObject(string name)
    {
        return new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
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

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
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

    private static void Stretch(Component component, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = component.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void Anchor(Component component, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Anchor(component.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPosition, sizeDelta);
    }

    private static void AnchorFromLeftTop(Component component, float left, float top, float width, float height)
    {
        Anchor(component, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left + width * 0.5f, top - height * 0.5f), new Vector2(width, height));
    }

    private static void AnchorFromRightTop(Component component, float right, float top, float width, float height)
    {
        Anchor(component, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(right + width * 0.5f), top - height * 0.5f), new Vector2(width, height));
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void SetSize(Component component, float width, float height)
    {
        component.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

}
