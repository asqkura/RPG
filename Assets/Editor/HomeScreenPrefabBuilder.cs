using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class HomeScreenPrefabBuilder
{
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";
    private const string BackgroundPath = "Assets/UI/Home/Backgrounds/HomeCastleBackground.png";
    private const string Icon11Path = "Assets/UI/Icons/icon-1_1.png";
    private const string Icon12Path = "Assets/UI/Icons/icon-1_2.png";
    private const string Icon21Path = "Assets/UI/Icons/icon-2_1.png";
    private const string Icon31Path = "Assets/UI/Icons/icon-3_1.png";
    private const string PrefabFolder = "Assets/UI/Home/Prefabs";
    private const string MenuItemPrefabPath = PrefabFolder + "/HomeMenuItem.prefab";
    private const string TextPanelPrefabPath = PrefabFolder + "/HomeTextPanel.prefab";
    private const string HomeScreenPrefabPath = PrefabFolder + "/HomeScreen.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const float ScreenMargin = 72f;

    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color MutedTextColor = new(0.55f, 0.52f, 0.47f, 1f);
    private static readonly Color WindowSpriteColor = Color.white;
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["クエスト"] = "仲間から依頼を受注したり、完了したクエストを報告します。\nストーリーの進行や報酬の獲得ができます。",
        ["うろつき"] = "街や周辺を探索して、仲間との会話やイベントを進めます。\n思わぬ発見や新しい出会いがあるかもしれません。",
        ["合成"] = "素材を組み合わせて、道具や装備品を作成します。\n冒険に役立つ品を準備できます。",
        ["ショップ"] = "所持金を使って道具や装備品を購入します。\n不要なアイテムの売却もここで行います。",
        ["編成"] = "冒険に参加する仲間や装備を整えます。\n目的に合わせて隊列や役割を調整できます。",
        ["キャンプ"] = "一息ついて仲間の状態を確認します。\n休息や会話で次の行動に備えます。",
        ["セーブ"] = "現在の進行状況を保存します。\n続きから再開できるように記録します。",
    };

    [MenuItem("Tools/RPG/Build Home Screen UI")]
    public static void Build()
    {
        EnsureFolders();
        ConfigureTextureImporters();

        var windowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowSpritePath);
        var hoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowHoverSpritePath);
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

        if (windowSprite == null || hoverSprite == null || backgroundSprite == null)
        {
            Debug.LogError("Home screen build failed. Missing window sprite, hover sprite, or background sprite.");
            return;
        }

        var menuItemPrefab = CreateMenuItemPrefab(windowSprite, LoadSprite(Icon31Path, "icon-3_1_6"));
        var textPanelPrefab = CreateTextPanelPrefab(windowSprite);
        var homeScreenPrefab = CreateHomeScreenPrefab(windowSprite, hoverSprite, backgroundSprite, menuItemPrefab, textPanelPrefab);

        PlaceHomeScreenInScene(homeScreenPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Home screen UI prefabs and SampleScene placement were generated.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "UI");
        CreateFolder("Assets/UI", "Home");
        CreateFolder("Assets/UI/Home", "Backgrounds");
        CreateFolder("Assets/UI/Home", "Prefabs");
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

    private static GameObject CreateMenuItemPrefab(Sprite windowSprite, Sprite defaultIcon)
    {
        var root = CreateRectObject("HomeMenuItem");
        SetSize(root, 520, 68);
        var image = root.gameObject.AddComponent<Image>();
        image.sprite = windowSprite;
        image.type = Image.Type.Sliced;
        image.color = WindowSpriteColor;

        var button = root.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        button.colors = colors;

        var icon = CreateImage("Icon", root.transform, defaultIcon, Image.Type.Simple, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(74, 0), new Vector2(42, 42));

        var label = CreateText("Label", root.transform, "クエスト", 30, TextAlignmentOptions.MidlineLeft, TextColor);
        Anchor(label, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(280, 0), new Vector2(320, 52));

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, MenuItemPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static GameObject CreateTextPanelPrefab(Sprite windowSprite)
    {
        var root = CreateRectObject("HomeTextPanel");
        SetSize(root, 1800, 160);
        var image = root.gameObject.AddComponent<Image>();
        image.sprite = windowSprite;
        image.type = Image.Type.Sliced;
        image.color = WindowSpriteColor;

        var text = CreateText("Description", root.transform,
            "仲間から依頼を受注したり、完了したクエストを報告します。\nストーリーの進行や報酬の獲得ができます。",
            30, TextAlignmentOptions.MidlineLeft, TextColor);
        Stretch(text, new Vector2(55, 26), new Vector2(-55, -26));

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, TextPanelPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static GameObject CreateHomeScreenPrefab(
        Sprite windowSprite,
        Sprite hoverSprite,
        Sprite backgroundSprite,
        GameObject menuItemPrefab,
        GameObject textPanelPrefab)
    {
        var root = new GameObject("HomeScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        var day = CreateText("DayText", root.transform, "1 日目", 48, TextAlignmentOptions.MidlineLeft, TextColor);
        Anchor(day, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -72), new Vector2(300, 70));

        var dayLine = CreateImage("DayDivider", root.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(dayLine, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -122), new Vector2(300, 2));

        var settings = CreateImage("SettingsButton", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(settings, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(ScreenMargin + 88), -64), new Vector2(176, 68));
        settings.gameObject.AddComponent<Button>();
        AddWindowHover(settings.gameObject, settings, windowSprite, hoverSprite);
        var settingsIcon = CreateImage("Icon", settings.transform, LoadSprite(Icon12Path, "icon-1_2_138"), Image.Type.Simple, Color.white);
        settingsIcon.preserveAspect = true;
        settingsIcon.raycastTarget = false;
        Anchor(settingsIcon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(34, 34));

        var settingsText = CreateText("Label", settings.transform, "設定", 30, TextAlignmentOptions.MidlineLeft, TextColor);
        Anchor(settingsText, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(118, 0), new Vector2(88, 44));

        var menuRoot = CreateRectObject("Menu");
        menuRoot.transform.SetParent(root.transform, false);
        var menuRect = menuRoot.GetComponent<RectTransform>();
        menuRect.pivot = new Vector2(0, 1);
        Anchor(menuRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin, -140), new Vector2(520, 532));

        var items = new[]
        {
            new MenuCommand("クエスト", Icon31Path, "icon-3_1_6"),
            new MenuCommand("うろつき", Icon11Path, "icon-1_1_140"),
            new MenuCommand("合成", Icon21Path, "icon-2_1_0"),
            new MenuCommand("ショップ", Icon12Path, "icon-1_2_0"),
            new MenuCommand("編成", Icon31Path, "icon-3_1_94"),
            new MenuCommand("キャンプ", Icon31Path, "icon-3_1_80"),
            new MenuCommand("セーブ", Icon12Path, "icon-1_2_99"),
        };

        var textPanel = (GameObject)PrefabUtility.InstantiatePrefab(textPanelPrefab, root.transform);
        textPanel.name = "QuestDescriptionPanel";
        StretchToBottom(textPanel.GetComponent<RectTransform>(), ScreenMargin, ScreenMargin, 50, 160);
        var descriptionText = textPanel.transform.Find("Description")?.GetComponent<TMP_Text>();
        var views = new List<HomeMenuItemView>();

        for (var i = 0; i < items.Length; i++)
        {
            var item = (GameObject)PrefabUtility.InstantiatePrefab(menuItemPrefab, menuRoot.transform);
            item.name = "MenuItem_" + items[i].Label;
            Anchor(item.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(260, -34 - i * 76), new Vector2(520, 68));
            item.GetComponent<Image>().color = WindowSpriteColor;
            item.transform.Find("Icon").GetComponent<Image>().sprite = LoadSprite(items[i].IconPath, items[i].IconName);
            item.transform.Find("Label").GetComponent<TMP_Text>().text = items[i].Label;
            views.Add(ConfigureMenuItemView(item, items[i].Label, windowSprite, hoverSprite));

            if (items[i].Label == "ショップ")
            {
                ConfigureScreenSwitch(item, "HomeScreen", "ShopScreen");
            }
        }

        ConfigureMenuController(root, descriptionText, views);

        var saved = PrefabUtility.SaveAsPrefabAsset(root, HomeScreenPrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static void PlaceHomeScreenInScene(GameObject homeScreenPrefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var destroyTargets = new List<GameObject>();
        foreach (var existing in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (existing.name == "HomeScreen")
            {
                destroyTargets.Add(existing);
            }
        }

        foreach (var target in destroyTargets)
        {
            Object.DestroyImmediate(target);
        }

        PrefabUtility.InstantiatePrefab(homeScreenPrefab, scene);
        InstallEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void InstallEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return;
        }

        var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            Object.DestroyImmediate(standaloneInputModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
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

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static void ConfigureScreenSwitch(GameObject target, string hideTargetName, string showTargetName)
    {
        var switchButton = target.GetComponent<UIScreenSwitchButton>() ?? target.AddComponent<UIScreenSwitchButton>();
        var switchObject = new SerializedObject(switchButton);
        switchObject.FindProperty("hideTargetName").stringValue = hideTargetName;
        switchObject.FindProperty("showTargetName").stringValue = showTargetName;
        switchObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HomeMenuItemView ConfigureMenuItemView(GameObject item, string label, Sprite normalWindowSprite, Sprite hoverWindowSprite)
    {
        var view = item.GetComponent<HomeMenuItemView>() ?? item.AddComponent<HomeMenuItemView>();
        var viewObject = new SerializedObject(view);
        viewObject.FindProperty("description").stringValue = Descriptions[label];
        viewObject.FindProperty("windowImage").objectReferenceValue = item.GetComponent<Image>();
        viewObject.FindProperty("normalWindowSprite").objectReferenceValue = normalWindowSprite;
        viewObject.FindProperty("highlightedWindowSprite").objectReferenceValue = hoverWindowSprite;
        viewObject.FindProperty("iconImage").objectReferenceValue = item.transform.Find("Icon")?.GetComponent<Image>();
        viewObject.FindProperty("labelText").objectReferenceValue = item.transform.Find("Label")?.GetComponent<TMP_Text>();
        viewObject.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static void ConfigureMenuController(GameObject root, TMP_Text descriptionText, IReadOnlyList<HomeMenuItemView> views)
    {
        var controller = root.GetComponent<HomeMenuController>() ?? root.AddComponent<HomeMenuController>();
        var controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        var itemsProperty = controllerObject.FindProperty("menuItems");
        itemsProperty.arraySize = views.Count;
        for (var i = 0; i < views.Count; i++)
        {
            itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
        }

        controllerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddWindowHover(GameObject target, Image image, Sprite windowSprite, Sprite hoverSprite)
    {
        var hover = target.GetComponent<WindowHoverSpriteView>() ?? target.AddComponent<WindowHoverSpriteView>();
        var hoverObject = new SerializedObject(hover);
        hoverObject.FindProperty("windowImage").objectReferenceValue = image;
        hoverObject.FindProperty("normalWindowSprite").objectReferenceValue = windowSprite;
        hoverObject.FindProperty("highlightedWindowSprite").objectReferenceValue = hoverSprite;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var rect = CreateRectObject(name);
        rect.transform.SetParent(parent, false);
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

    private static void SetHeight(Component component, float height)
    {
        component.GetComponent<RectTransform>().sizeDelta = new Vector2(component.GetComponent<RectTransform>().sizeDelta.x, height);
    }

    private readonly struct MenuCommand
    {
        public MenuCommand(string label, string iconPath, string iconName)
        {
            Label = label;
            IconPath = iconPath;
            IconName = iconName;
        }

        public string Label { get; }
        public string IconPath { get; }
        public string IconName { get; }
    }
}
