using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SynthesisScreenSceneInstaller
{
    private const string HomeScreenPrefabPath = "Assets/UI/Home/Prefabs/HomeScreen.prefab";
    private const string SynthesisScreenPrefabPath = "Assets/UI/Synthesis/Prefabs/SynthesisScreen.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/RPG/Install Synthesis Screen In Scene")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var homeScreen = FindRoot("HomeScreen");
        if (homeScreen == null)
        {
            var homePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeScreenPrefabPath);
            if (homePrefab != null)
            {
                homeScreen = (GameObject)PrefabUtility.InstantiatePrefab(homePrefab, scene);
                homeScreen.name = "HomeScreen";
            }
        }

        var synthesisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SynthesisScreenPrefabPath);
        if (homeScreen == null || synthesisPrefab == null)
        {
            Debug.LogError("Synthesis screen install failed. Missing HomeScreen or SynthesisScreen prefab.");
            return;
        }

        var synthesisScreen = FindRoot("SynthesisScreen");
        if (synthesisScreen != null)
        {
            Object.DestroyImmediate(synthesisScreen);
        }

        synthesisScreen = (GameObject)PrefabUtility.InstantiatePrefab(synthesisPrefab, scene);
        synthesisScreen.name = "SynthesisScreen";

        homeScreen.SetActive(true);
        synthesisScreen.SetActive(false);

        InstallSwitch(FindDeep(homeScreen.transform, "MenuItem_合成"), homeScreen, synthesisScreen);
        InstallSwitch(FindDeep(synthesisScreen.transform, "BackButton"), synthesisScreen, homeScreen);
        InstallEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Synthesis screen was installed in SampleScene.");
    }

    private static void InstallSwitch(Transform buttonTransform, GameObject hideTarget, GameObject showTarget)
    {
        if (buttonTransform == null)
        {
            Debug.LogWarning("Screen switch button target was not found.");
            return;
        }

        if (buttonTransform.GetComponent<Button>() == null)
        {
            buttonTransform.gameObject.AddComponent<Button>();
        }

        var switchButton = buttonTransform.GetComponent<UIScreenSwitchButton>() ?? buttonTransform.gameObject.AddComponent<UIScreenSwitchButton>();
        var switchObject = new SerializedObject(switchButton);
        switchObject.FindProperty("hideTargetName").stringValue = hideTarget != null ? hideTarget.name : string.Empty;
        switchObject.FindProperty("showTargetName").stringValue = showTarget != null ? showTarget.name : string.Empty;
        switchObject.ApplyModifiedPropertiesWithoutUndo();
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

    private static GameObject FindRoot(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

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
