using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class HomeMenuInteractionInstaller
{
    private const string HomeScreenPrefabPath = "Assets/UI/Home/Prefabs/HomeScreen.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string WindowSpritePath = "Assets/UI/Windows/Sprites/Window.png";
    private const string WindowHoverSpritePath = "Assets/UI/Windows/Sprites/Window_Hover.png";

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

    [MenuItem("Tools/RPG/Install Home Menu Interactions")]
    public static void Install()
    {
        ConfigureWindowSprite(WindowHoverSpritePath);
        var normalWindowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowSpritePath);
        var hoverWindowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WindowHoverSpritePath);
        var root = PrefabUtility.LoadPrefabContents(HomeScreenPrefabPath);

        try
        {
            var descriptionText = FindDeep(root.transform, "Description")?.GetComponent<TMP_Text>();
            var controller = root.GetComponent<HomeMenuController>() ?? root.AddComponent<HomeMenuController>();
            var views = new List<HomeMenuItemView>();

            InstallSettingsHover(root.transform, normalWindowSprite, hoverWindowSprite);

            foreach (var pair in Descriptions)
            {
                var itemTransform = FindDeep(root.transform, "MenuItem_" + pair.Key);
                if (itemTransform == null)
                {
                    Debug.LogWarning($"Menu item was not found: {pair.Key}");
                    continue;
                }

                var itemImage = itemTransform.GetComponent<Image>();
                RemoveLegacyHighlightObjects(itemTransform);
                var icon = itemTransform.Find("Icon")?.GetComponent<Image>();
                var label = itemTransform.Find("Label")?.GetComponent<TMP_Text>();
                var view = itemTransform.GetComponent<HomeMenuItemView>() ?? itemTransform.gameObject.AddComponent<HomeMenuItemView>();

                var viewObject = new SerializedObject(view);
                viewObject.FindProperty("description").stringValue = pair.Value;
                viewObject.FindProperty("windowImage").objectReferenceValue = itemImage;
                viewObject.FindProperty("normalWindowSprite").objectReferenceValue = normalWindowSprite;
                viewObject.FindProperty("highlightedWindowSprite").objectReferenceValue = hoverWindowSprite;
                viewObject.FindProperty("iconImage").objectReferenceValue = icon;
                viewObject.FindProperty("labelText").objectReferenceValue = label;
                viewObject.ApplyModifiedPropertiesWithoutUndo();

                views.Add(view);
            }

            var controllerObject = new SerializedObject(controller);
            controllerObject.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            var itemsProperty = controllerObject.FindProperty("menuItems");
            itemsProperty.arraySize = views.Count;
            for (var i = 0; i < views.Count; i++)
            {
                itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HomeScreenPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        InstallEventSystemInScene();
        AssetDatabase.Refresh();
        Debug.Log("Home menu hover and description interactions were installed.");
    }

    private static void ConfigureWindowSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = new Vector4(13, 13, 13, 13);
        importer.SaveAndReimport();
    }

    private static void InstallEventSystemInScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            var eventSystemObject = eventSystem.gameObject;
            var standaloneInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                Object.DestroyImmediate(standaloneInputModule);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        if (scene.isDirty)
        {
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void InstallSettingsHover(Transform root, Sprite normalWindowSprite, Sprite hoverWindowSprite)
    {
        var settingsTransform = FindDeep(root, "SettingsButton");
        if (settingsTransform == null)
        {
            Debug.LogWarning("SettingsButton was not found.");
            return;
        }

        var settingsImage = settingsTransform.GetComponent<Image>();
        var hoverView = settingsTransform.GetComponent<WindowHoverSpriteView>() ?? settingsTransform.gameObject.AddComponent<WindowHoverSpriteView>();
        var hoverObject = new SerializedObject(hoverView);
        hoverObject.FindProperty("windowImage").objectReferenceValue = settingsImage;
        hoverObject.FindProperty("normalWindowSprite").objectReferenceValue = normalWindowSprite;
        hoverObject.FindProperty("highlightedWindowSprite").objectReferenceValue = hoverWindowSprite;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveLegacyHighlightObjects(Transform itemTransform)
    {
        foreach (var childName in new[] { "HoverGlow", "HoverFill", "HoverFrame" })
        {
            var child = itemTransform.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            var result = FindDeep(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
