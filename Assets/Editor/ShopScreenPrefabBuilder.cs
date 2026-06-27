using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ShopScreenPrefabBuilder
{
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";
    private const string BackgroundPath = "Assets/UI/Shop/Backgrounds/ShopInteriorBackground.png";
    private const string FontPath = "Assets/Fonts/NotoSansJP/NotoSansCJKjp-Regular SDF.asset";
    private const string Icon11Path = "Assets/UI/Icons/icon-1_1.png";
    private const string Icon12Path = "Assets/UI/Icons/icon-1_2.png";
    private const string Icon21Path = "Assets/UI/Icons/icon-2_1.png";
    private const string Icon31Path = "Assets/UI/Icons/icon-3_1.png";
    private const string PrefabFolder = "Assets/UI/Shop/Prefabs";
    private const string RowPrefabPath = PrefabFolder + "/ShopItemRow.prefab";
    private const string ShopScreenPrefabPath = PrefabFolder + "/ShopScreen.prefab";
    private const float ScreenMargin = 72f;

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
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (windowSprite == null || hoverSprite == null || backgroundSprite == null || font == null)
        {
            Debug.LogError("Shop screen build failed. Missing window sprite, hover sprite, background sprite, or TMP font.");
            return;
        }

        var rowPrefab = CreateRowPrefab(windowSprite, font, LoadSprite(Icon12Path, "icon-1_2_0"));
        CreateShopScreenPrefab(windowSprite, hoverSprite, backgroundSprite, font, rowPrefab);

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

    private static GameObject CreateRowPrefab(Sprite windowSprite, TMP_FontAsset font, Sprite defaultIcon)
    {
        var root = CreateRectObject("ShopItemRow");
        SetSize(root, 710, 56);
        var image = root.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.clear;

        var icon = CreateImage("Icon", root.transform, defaultIcon, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(26, 26));

        var marker = CreateImage("SelectionMarker", root.transform, null, Image.Type.Simple, AccentTextColor);
        marker.raycastTarget = false;
        Anchor(marker, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(4, 30));
        marker.gameObject.SetActive(false);

        CreateText("Name", root.transform, font, "ポーション", 23, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(210, 0), new Vector2(320, 36));
        CreateText("Stock", root.transform, font, "-", 21, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-262, 0), new Vector2(80, 32));
        CreateText("Owned", root.transform, font, "3", 21, TextAlignmentOptions.Center, MutedTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(80, 32));
        CreateText("Price", root.transform, font, "80 G", 23, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-70, 0), new Vector2(100, 34));

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static void CreateShopScreenPrefab(
        Sprite windowSprite,
        Sprite hoverSprite,
        Sprite backgroundSprite,
        TMP_FontAsset font,
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

        CreateText("Title", root.transform, font, "ショップ", 46, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 128, -76), new Vector2(260, 66));
        var titleLine = CreateImage("TitleDivider", root.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(titleLine, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -126), new Vector2(300, 2));

        var moneyPanel = CreateImage("MoneyPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(moneyPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-382, -72), new Vector2(300, 62));
        CreateText("Label", moneyPanel.transform, font, "所持金", 20, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(76, 10), new Vector2(120, 28));
        CreateText("Value", moneyPanel.transform, font, "12,480 G", 28, TextAlignmentOptions.MidlineRight, AccentTextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-105, -6), new Vector2(180, 38));

        var backButton = CreateImage("BackButton", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(backButton, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(ScreenMargin + 80), -72), new Vector2(160, 62));
        backButton.gameObject.AddComponent<Button>();
        AddWindowHover(backButton.gameObject, backButton, windowSprite, hoverSprite);
        CreateText("Label", backButton.transform, font, "戻る", 28, TextAlignmentOptions.Center, TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 44));

        var listPanel = CreateImage("ItemListPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(listPanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 396, -536), new Vector2(820, 608));
        CreateTab(root.transform, font, windowSprite, hoverSprite, "BuyTab", "購入", new Vector2(ScreenMargin + 210, -262), true);
        CreateTab(root.transform, font, windowSprite, hoverSprite, "SellTab", "売却", new Vector2(ScreenMargin + 350, -262), false);
        CreateText("PanelTitle", listPanel.transform, font, "商品一覧", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(102, -40), new Vector2(150, 34));
        CreateText("CategoryLabel", listPanel.transform, font, "分類", 17, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(472, -42), new Vector2(70, 26));
        CreateCategoryButton(listPanel.transform, font, windowSprite, hoverSprite, "ConsumableCategory", "消耗品", new Vector2(562, -42), true);
        CreateCategoryButton(listPanel.transform, font, windowSprite, hoverSprite, "MaterialCategory", "素材", new Vector2(666, -42), false);
        CreateCategoryButton(listPanel.transform, font, windowSprite, hoverSprite, "EquipmentCategory", "装備", new Vector2(770, -42), false);

        var headerLine = CreateImage("HeaderLine", listPanel.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(headerLine, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), Vector2.zero);
        var headerLineRect = headerLine.GetComponent<RectTransform>();
        headerLineRect.offsetMin = new Vector2(52, -92);
        headerLineRect.offsetMax = new Vector2(-52, -91);
        headerLine.raycastTarget = false;

        CreateColumnHeader(listPanel.transform, font, "商品名", 110, -116, 140);
        CreateColumnHeader(listPanel.transform, font, "在庫", 504, -116, 70);
        CreateColumnHeader(listPanel.transform, font, "所持", 596, -116, 70);
        CreateColumnHeader(listPanel.transform, font, "価格", 686, -116, 80);

        var rows = CreateRows(listPanel.transform, rowPrefab, windowSprite, hoverSprite);

        var detailPanel = CreateImage("DetailPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(detailPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-432, -536), new Vector2(660, 608));

        CreateText("PanelTitle", detailPanel.transform, font, "詳細", 22, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -36), new Vector2(100, 32));
        var detailTitle = CreateText("DetailTitle", detailPanel.transform, font, "", 32, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(230, -82), new Vector2(420, 48));
        var detailBody = CreateText("DetailBody", detailPanel.transform, font, "", 21, TextAlignmentOptions.TopLeft, TextColor,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
        Stretch(detailBody, new Vector2(48, -332), new Vector2(-48, -172));

        var detailStock = CreateDetailStat(detailPanel.transform, font, "在庫", "-", new Vector2(92, -394));
        var detailOwned = CreateDetailStat(detailPanel.transform, font, "所持", "3", new Vector2(244, -394));
        var detailPrice = CreateDetailStat(detailPanel.transform, font, "価格", "80 G", new Vector2(416, -394));

        var quantityPanel = CreateImage("QuantityPanel", detailPanel.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(quantityPanel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(136, 58), new Vector2(190, 58));
        CreateText("Label", quantityPanel.transform, font, "数量", 20, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(56, 0), new Vector2(70, 30));
        CreateText("Value", quantityPanel.transform, font, "1", 28, TextAlignmentOptions.MidlineRight, TextColor,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(70, 36));

        var buyButton = CreateImage("BuyButton", detailPanel.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(buyButton, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-146, 58), new Vector2(210, 58));
        buyButton.gameObject.AddComponent<Button>();
        AddWindowHover(buyButton.gameObject, buyButton, windowSprite, hoverSprite);
        CreateText("Label", buyButton.transform, font, "購入", 30, TextAlignmentOptions.Center, AccentTextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 44));

        var helpPanel = CreateImage("HelpPanel", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        StretchToBottom(helpPanel.GetComponent<RectTransform>(), ScreenMargin, ScreenMargin, 44, 96);
        var helpText = CreateText("HelpText", helpPanel.transform, font, "", 24, TextAlignmentOptions.MidlineLeft, TextColor,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Stretch(helpText, new Vector2(52, 14), new Vector2(-52, -14));

        var controller = root.AddComponent<ShopScreenPreviewController>();
        var controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("detailTitleText").objectReferenceValue = detailTitle;
        controllerObject.FindProperty("detailBodyText").objectReferenceValue = detailBody;
        controllerObject.FindProperty("detailStockText").objectReferenceValue = detailStock;
        controllerObject.FindProperty("detailOwnedText").objectReferenceValue = detailOwned;
        controllerObject.FindProperty("detailPriceText").objectReferenceValue = detailPrice;
        controllerObject.FindProperty("helpText").objectReferenceValue = helpText;
        var rowsProperty = controllerObject.FindProperty("itemRows");
        rowsProperty.arraySize = rows.Count;
        for (var i = 0; i < rows.Count; i++)
        {
            rowsProperty.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        }
        controllerObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ShopScreenPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static List<ShopItemRowView> CreateRows(Transform parent, GameObject rowPrefab, Sprite windowSprite, Sprite hoverSprite)
    {
        var items = new[]
        {
            new ShopPreviewItem("ポーション", "-", "3", "80 G", Icon12Path, "icon-1_2_0",
                "HPを小回復する基本の薬。\n\n種別: 消耗品\n効果: HP +50\n購入制限: 消耗品の合計所持数 20 個まで",
                "ポーションを購入します。消耗品の所持上限に注意してください。"),
            new ShopPreviewItem("マナの雫", "-", "1", "120 G", Icon21Path, "icon-2_1_0",
                "SPを小回復する澄んだ雫。\n\n種別: 消耗品\n効果: SP +30\n購入制限: 消耗品の合計所持数 20 個まで",
                "マナの雫を購入します。探索や長期戦の保険になります。"),
            new ShopPreviewItem("鉄鉱石", "8", "12", "60 G", Icon11Path, "icon-1_1_140",
                "武器や防具の合成に使う扱いやすい鉱石。\n\n種別: 素材\n用途: 装備合成\n在庫: フェーズ開始時に補充",
                "合成素材を購入します。素材と装備は店舗在庫がなくなると購入できません。"),
            new ShopPreviewItem("薬草束", "5", "4", "45 G", Icon31Path, "icon-3_1_80",
                "調合や簡易手当に使える薬草の束。\n\n種別: 素材\n用途: 消耗品作成、合成\n在庫: フェーズ開始時に補充",
                "薬草束を購入します。回復系の準備に使います。"),
            new ShopPreviewItem("見習いの短剣", "1", "0", "620 G", Icon31Path, "icon-3_1_6",
                "扱いやすい短剣。素早い仲間向け。\n\n種別: 武器 / 短剣\n基本性能: 攻撃 +8 / 速度 +2\n固定スキル: クイックスタブ",
                "装備品を購入します。装備可能者や固定スキルを確認してください。"),
            new ShopPreviewItem("旅人の外套", "2", "1", "480 G", Icon31Path, "icon-3_1_94",
                "旅の汚れに強い軽い外套。\n\n種別: 防具\n基本性能: 防御 +5 / 速度 +1\n固定スキル: なし",
                "装備品を購入します。売却不可の装備は売却側で選択できません。"),
        };

        var rows = new List<ShopItemRowView>();
        for (var i = 0; i < items.Length; i++)
        {
            var row = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, parent);
            row.name = "ItemRow_" + (i + 1).ToString("00");
            Anchor(row.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(404, -158 - i * 62), new Vector2(710, 56));

            row.transform.Find("Icon").GetComponent<Image>().sprite = LoadSprite(items[i].IconPath, items[i].IconName);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = items[i].Name;
            row.transform.Find("Stock").GetComponent<TMP_Text>().text = items[i].Stock;
            row.transform.Find("Owned").GetComponent<TMP_Text>().text = items[i].Owned;
            row.transform.Find("Price").GetComponent<TMP_Text>().text = items[i].Price;
            var view = row.AddComponent<ShopItemRowView>();
            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("itemName").stringValue = items[i].Name;
            viewObject.FindProperty("detailText").stringValue = items[i].Detail;
            viewObject.FindProperty("helpText").stringValue = items[i].Help;
            viewObject.FindProperty("stockText").stringValue = items[i].Stock;
            viewObject.FindProperty("ownedText").stringValue = items[i].Owned;
            viewObject.FindProperty("priceText").stringValue = items[i].Price;
            viewObject.FindProperty("windowImage").objectReferenceValue = row.GetComponent<Image>();
            viewObject.FindProperty("selectionMarker").objectReferenceValue = row.transform.Find("SelectionMarker").GetComponent<Graphic>();
            viewObject.FindProperty("normalWindowSprite").objectReferenceValue = null;
            viewObject.FindProperty("highlightedWindowSprite").objectReferenceValue = null;

            var labels = new[]
            {
                row.transform.Find("Name").GetComponent<TMP_Text>(),
                row.transform.Find("Stock").GetComponent<TMP_Text>(),
                row.transform.Find("Owned").GetComponent<TMP_Text>(),
                row.transform.Find("Price").GetComponent<TMP_Text>(),
            };
            var labelsProperty = viewObject.FindProperty("labelTexts");
            labelsProperty.arraySize = labels.Length;
            for (var labelIndex = 0; labelIndex < labels.Length; labelIndex++)
            {
                labelsProperty.GetArrayElementAtIndex(labelIndex).objectReferenceValue = labels[labelIndex];
            }
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            rows.Add(view);
        }

        return rows;
    }

    private static void CreateTab(
        Transform parent,
        TMP_FontAsset font,
        Sprite windowSprite,
        Sprite hoverSprite,
        string name,
        string label,
        Vector2 anchoredPosition,
        bool selected)
    {
        var tab = CreateImage(name, parent, selected ? hoverSprite : windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(tab, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(112, 44));
        tab.gameObject.AddComponent<Button>();
        AddWindowHover(tab.gameObject, tab, windowSprite, hoverSprite);
        CreateText("Label", tab.transform, font, label, 21, TextAlignmentOptions.Center, selected ? AccentTextColor : TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 32));
    }

    private static void CreateCategoryButton(
        Transform parent,
        TMP_FontAsset font,
        Sprite windowSprite,
        Sprite hoverSprite,
        string name,
        string label,
        Vector2 anchoredPosition,
        bool selected)
    {
        var button = CreateImage(name, parent, selected ? hoverSprite : windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(button, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(92, 44));
        button.gameObject.AddComponent<Button>();
        AddWindowHover(button.gameObject, button, windowSprite, hoverSprite);
        CreateText("Label", button.transform, font, label, 19, TextAlignmentOptions.Center, selected ? AccentTextColor : TextColor,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 30));
    }

    private static void CreateColumnHeader(Transform parent, TMP_FontAsset font, string label, float x, float y, float width)
    {
        CreateText(label + "Header", parent, font, label, 18, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(width, 28));
    }

    private static TMP_Text CreateDetailStat(Transform parent, TMP_FontAsset font, string label, string value, Vector2 anchoredPosition)
    {
        var root = CreateRectObject(label + "Stat");
        root.transform.SetParent(parent, false);
        Anchor(root, new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(130, 56));

        CreateText("Label", root, font, label, 18, TextAlignmentOptions.MidlineLeft, MutedTextColor,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -14), new Vector2(80, 24));
        return CreateText("Value", root, font, value, 24, TextAlignmentOptions.MidlineLeft, TextColor,
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
        TMP_FontAsset font,
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
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
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

    private readonly struct ShopPreviewItem
    {
        public ShopPreviewItem(
            string name,
            string stock,
            string owned,
            string price,
            string iconPath,
            string iconName,
            string detail,
            string help)
        {
            Name = name;
            Stock = stock;
            Owned = owned;
            Price = price;
            IconPath = iconPath;
            IconName = iconName;
            Detail = detail;
            Help = help;
        }

        public string Name { get; }
        public string Stock { get; }
        public string Owned { get; }
        public string Price { get; }
        public string IconPath { get; }
        public string IconName { get; }
        public string Detail { get; }
        public string Help { get; }
    }
}
