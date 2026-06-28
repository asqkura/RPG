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
    private const string HomeScreenPath = "Assets/UI/Home/Prefabs/HomeScreen.prefab";
    private const string ItemDatabasePath = "Assets/MasterData/Test/Databases/TestItemDatabase.asset";
    private const string EquipmentDatabasePath = "Assets/MasterData/Test/Databases/TestEquipmentDatabase.asset";
    private const string RecipeDatabasePath = "Assets/MasterData/Test/Databases/TestSynthesisRecipeDatabase.asset";

    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color AccentColor = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color PanelColor = new(0.08f, 0.07f, 0.06f, 0.92f);
    private static readonly Color ButtonColor = new(0.18f, 0.15f, 0.12f, 1f);

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
        var root = CreateUIObject("SynthesisRecipeRow", null, new Vector2(760f, 50f));
        var image = root.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        var view = root.AddComponent<SynthesisRecipeRowView>();

        var icon = CreateImage("Icon", root.transform, new Vector2(42f, 42f), new Vector2(32f, 0f));
        Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        icon.preserveAspect = true;

        var nameLabel = CreateText("Name", root.transform, "レシピ名", 24, TextAlignmentOptions.Left, new Vector2(360f, 40f), new Vector2(270f, 0f));
        Anchor(nameLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
        var ownedLabel = CreateText("Owned", root.transform, "0", 22, TextAlignmentOptions.Center, new Vector2(100f, 40f), new Vector2(-150f, 0f));
        Anchor(ownedLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
        var costLabel = CreateText("Cost", root.transform, "-", 22, TextAlignmentOptions.Center, new Vector2(120f, 40f), new Vector2(-48f, 0f));
        Anchor(costLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));

        var serialized = new SerializedObject(view);
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
        rootRect.localScale = Vector3.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var background = CreateImage("Background", root.transform, new Vector2(1920f, 1080f), Vector2.zero);
        Stretch(background.rectTransform);
        background.color = new Color(0.04f, 0.035f, 0.03f, 1f);

        var title = CreateText("Title", root.transform, "合成", 46, TextAlignmentOptions.Left, new Vector2(320f, 70f), new Vector2(90f, -60f));
        AnchorTopLeft(title.rectTransform);
        var moneyCaption = CreateText("MoneyCaption", root.transform, "所持金", 24, TextAlignmentOptions.Right, new Vector2(120f, 38f), new Vector2(-270f, -62f));
        AnchorTopRight(moneyCaption.rectTransform);
        var moneyText = CreateText("MoneyText", root.transform, "0", 28, TextAlignmentOptions.Right, new Vector2(180f, 42f), new Vector2(-80f, -60f));
        AnchorTopRight(moneyText.rectTransform);

        var allButton = CreateButton("AllButton", root.transform, "すべて", new Vector2(132f, 46f), new Vector2(90f, -140f));
        var consumableButton = CreateButton("ConsumableButton", root.transform, "消耗品", new Vector2(132f, 46f), new Vector2(236f, -140f));
        var weaponButton = CreateButton("WeaponButton", root.transform, "武器", new Vector2(132f, 46f), new Vector2(382f, -140f));
        var armorButton = CreateButton("ArmorButton", root.transform, "防具", new Vector2(132f, 46f), new Vector2(528f, -140f));
        var accessoryButton = CreateButton("AccessoryButton", root.transform, "装飾品", new Vector2(132f, 46f), new Vector2(674f, -140f));

        var listPanel = CreatePanel("RecipeListPanel", root.transform, new Vector2(850f, 760f), new Vector2(90f, -205f));
        var viewport = CreatePanel("Viewport", listPanel.transform, new Vector2(800f, 650f), new Vector2(25f, -70f));
        viewport.AddComponent<RectMask2D>();
        var content = CreateUIObject("Content", viewport.transform, new Vector2(800f, 650f));
        AnchorTopStretch(content.GetComponent<RectTransform>(), 0f, 0f, 0f);
        var scrollRect = listPanel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        CreateText("ListNameHeader", listPanel.transform, "合成品", 20, TextAlignmentOptions.Left, new Vector2(180f, 32f), new Vector2(30f, -28f));
        CreateText("ListOwnedHeader", listPanel.transform, "所持", 20, TextAlignmentOptions.Center, new Vector2(80f, 32f), new Vector2(610f, -28f));
        CreateText("ListCostHeader", listPanel.transform, "費用", 20, TextAlignmentOptions.Center, new Vector2(80f, 32f), new Vector2(720f, -28f));

        var detailPanel = CreatePanel("DetailPanel", root.transform, new Vector2(840f, 760f), new Vector2(990f, -205f));
        var detailIcon = CreateImage("DetailIcon", detailPanel.transform, new Vector2(88f, 88f), new Vector2(40f, -42f));
        AnchorTopLeft(detailIcon.rectTransform);
        detailIcon.preserveAspect = true;
        var detailTitle = CreateText("DetailTitle", detailPanel.transform, "合成品", 32, TextAlignmentOptions.Left, new Vector2(500f, 46f), new Vector2(150f, -42f));
        AnchorTopLeft(detailTitle.rectTransform);
        var detailTag = CreateText("DetailTag", detailPanel.transform, "", 22, TextAlignmentOptions.Left, new Vector2(300f, 34f), new Vector2(150f, -90f));
        AnchorTopLeft(detailTag.rectTransform);
        var detailDescription = CreateText("DetailDescription", detailPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(760f, 110f), new Vector2(40f, -150f));
        AnchorTopLeft(detailDescription.rectTransform);

        CreateText("OwnedCaption", detailPanel.transform, "所持数", 22, TextAlignmentOptions.Left, new Vector2(120f, 32f), new Vector2(40f, -285f));
        var ownedText = CreateText("OwnedText", detailPanel.transform, "0", 22, TextAlignmentOptions.Left, new Vector2(200f, 32f), new Vector2(170f, -285f));
        CreateText("MoneyCostCaption", detailPanel.transform, "必要金額", 22, TextAlignmentOptions.Left, new Vector2(120f, 32f), new Vector2(40f, -330f));
        var moneyCostText = CreateText("MoneyCostText", detailPanel.transform, "0", 22, TextAlignmentOptions.Left, new Vector2(200f, 32f), new Vector2(170f, -330f));
        CreateText("MaterialCostCaption", detailPanel.transform, "必要素材", 22, TextAlignmentOptions.Left, new Vector2(160f, 32f), new Vector2(40f, -385f));
        var materialCostText = CreateText("MaterialCostText", detailPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(760f, 180f), new Vector2(40f, -430f));
        var helpText = CreateText("HelpText", detailPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(760f, 70f), new Vector2(40f, -640f));
        var synthesizeButton = CreateButton("SynthesizeButton", detailPanel.transform, "合成する", new Vector2(220f, 58f), new Vector2(580f, -665f));

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
        serialized.FindProperty("materialCostText").objectReferenceValue = materialCostText;
        serialized.FindProperty("moneyCostText").objectReferenceValue = moneyCostText;
        serialized.FindProperty("ownedText").objectReferenceValue = ownedText;
        serialized.FindProperty("helpText").objectReferenceValue = helpText;
        serialized.FindProperty("moneyText").objectReferenceValue = moneyText;
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
        image.color = PanelColor;
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
        image.color = ButtonColor;
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
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
