using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HomeScreenPrefabBuilder
{
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string BackgroundPath = "Assets/UI/Home/Backgrounds/HomeCastleBackground.png";
    private const string FontPath = "Assets/Fonts/NotoSansJP/NotoSansCJKjp-Regular SDF.asset";
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

    [MenuItem("Tools/RPG/Build Home Screen UI")]
    public static void Build()
    {
        EnsureFolders();
        ConfigureTextureImporters();

        var windowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowSpritePath);
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (windowSprite == null || backgroundSprite == null || font == null)
        {
            Debug.LogError("Home screen build failed. Missing window sprite, background sprite, or TMP font.");
            return;
        }

        var menuItemPrefab = CreateMenuItemPrefab(windowSprite, font, LoadSprite(Icon31Path, "icon-3_1_6"));
        var textPanelPrefab = CreateTextPanelPrefab(windowSprite, font);
        var homeScreenPrefab = CreateHomeScreenPrefab(windowSprite, backgroundSprite, font, menuItemPrefab, textPanelPrefab);

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

    private static GameObject CreateMenuItemPrefab(Sprite windowSprite, TMP_FontAsset font, Sprite defaultIcon)
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

        var label = CreateText("Label", root.transform, font, "クエスト", 30, TextAlignmentOptions.MidlineLeft, TextColor);
        Anchor(label, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(280, 0), new Vector2(320, 52));

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, MenuItemPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static GameObject CreateTextPanelPrefab(Sprite windowSprite, TMP_FontAsset font)
    {
        var root = CreateRectObject("HomeTextPanel");
        SetSize(root, 1800, 160);
        var image = root.gameObject.AddComponent<Image>();
        image.sprite = windowSprite;
        image.type = Image.Type.Sliced;
        image.color = WindowSpriteColor;

        var text = CreateText("Description", root.transform, font,
            "仲間から依頼を受注したり、完了したクエストを報告します。\nストーリーの進行や報酬の獲得ができます。",
            30, TextAlignmentOptions.MidlineLeft, TextColor);
        Stretch(text, new Vector2(55, 26), new Vector2(-55, -26));

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, TextPanelPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static GameObject CreateHomeScreenPrefab(
        Sprite windowSprite,
        Sprite backgroundSprite,
        TMP_FontAsset font,
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

        var day = CreateText("DayText", root.transform, font, "1 日目", 48, TextAlignmentOptions.MidlineLeft, TextColor);
        Anchor(day, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -72), new Vector2(300, 70));

        var dayLine = CreateImage("DayDivider", root.transform, null, Image.Type.Simple, new Color(0.45f, 0.36f, 0.25f, 0.75f));
        Anchor(dayLine, new Vector2(0, 1), new Vector2(0, 1), new Vector2(ScreenMargin + 150, -122), new Vector2(300, 2));

        var settings = CreateImage("SettingsButton", root.transform, windowSprite, Image.Type.Sliced, WindowSpriteColor);
        Anchor(settings, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(ScreenMargin + 88), -64), new Vector2(176, 68));
        settings.gameObject.AddComponent<Button>();
        var settingsIcon = CreateImage("Icon", settings.transform, LoadSprite(Icon12Path, "icon-1_2_138"), Image.Type.Simple, Color.white);
        settingsIcon.preserveAspect = true;
        settingsIcon.raycastTarget = false;
        Anchor(settingsIcon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(34, 34));

        var settingsText = CreateText("Label", settings.transform, font, "設定", 30, TextAlignmentOptions.MidlineLeft, TextColor);
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

        for (var i = 0; i < items.Length; i++)
        {
            var item = (GameObject)PrefabUtility.InstantiatePrefab(menuItemPrefab, menuRoot.transform);
            item.name = "MenuItem_" + items[i].Label;
            Anchor(item.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(260, -34 - i * 76), new Vector2(520, 68));
            item.GetComponent<Image>().color = WindowSpriteColor;
            item.transform.Find("Icon").GetComponent<Image>().sprite = LoadSprite(items[i].IconPath, items[i].IconName);
            item.transform.Find("Label").GetComponent<TMP_Text>().text = items[i].Label;
        }

        var textPanel = (GameObject)PrefabUtility.InstantiatePrefab(textPanelPrefab, root.transform);
        textPanel.name = "QuestDescriptionPanel";
        StretchToBottom(textPanel.GetComponent<RectTransform>(), ScreenMargin, ScreenMargin, 50, 160);

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
            if (existing.name == "HomeScreen" || existing.name == "EventSystem")
            {
                destroyTargets.Add(existing);
            }
        }

        foreach (var target in destroyTargets)
        {
            Object.DestroyImmediate(target);
        }

        PrefabUtility.InstantiatePrefab(homeScreenPrefab, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var rect = CreateRectObject(name);
        rect.transform.SetParent(parent, false);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
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
