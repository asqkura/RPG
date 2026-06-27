using System.Collections.Generic;
using System.IO;
using RPG.MasterData;
using UnityEditor;
using UnityEngine;

public static class TestSynthesisDataBuilder
{
    private const string RootFolder = "Assets/MasterData";
    private const string TestFolder = RootFolder + "/Test";
    private const string RecipeFolder = TestFolder + "/Recipes";
    private const string DatabaseFolder = TestFolder + "/Databases";
    private const string RecipeDatabasePath = DatabaseFolder + "/TestRecipeDatabase.asset";

    [MenuItem("Tools/RPG/Build Test Synthesis Data")]
    public static void Build()
    {
        EnsureFolders();
        DeleteGeneratedAssets();
        AssetDatabase.Refresh();

        var recipes = new[]
        {
            CreateRecipe(
                "recipe_potion",
                "ポーション",
                "薬草から基本の回復薬を作成します。",
                RecipeDataType.Consumable,
                1,
                RecipeResultDataType.Item,
                "item_potion",
                20,
                0,
                new IngredientSpec("mat_herb", 2)),
            CreateRecipe(
                "recipe_mana_drop",
                "マナの雫",
                "魔石の欠片からSP回復薬を作成します。",
                RecipeDataType.Consumable,
                1,
                RecipeResultDataType.Item,
                "item_mana_drop",
                40,
                1,
                new IngredientSpec("mat_magic_shard", 1),
                new IngredientSpec("mat_herb", 1)),
            CreateRecipe(
                "recipe_iron_sword",
                "鉄の剣",
                "鉄鉱石と木材で標準的な剣を作成します。",
                RecipeDataType.Weapon,
                1,
                RecipeResultDataType.Equipment,
                "eq_iron_sword",
                120,
                100,
                new IngredientSpec("mat_iron_ore", 3),
                new IngredientSpec("mat_sturdy_wood", 1)),
            CreateRecipe(
                "recipe_hunter_bow",
                "狩人の弓",
                "丈夫な木材と獣の皮で弓を作成します。",
                RecipeDataType.Weapon,
                1,
                RecipeResultDataType.Equipment,
                "eq_hunter_bow",
                110,
                101,
                new IngredientSpec("mat_sturdy_wood", 3),
                new IngredientSpec("mat_beast_hide", 1)),
            CreateRecipe(
                "recipe_leather_armor",
                "革の鎧",
                "獣の皮を加工して軽い防具を作成します。",
                RecipeDataType.Armor,
                1,
                RecipeResultDataType.Equipment,
                "eq_leather_armor",
                100,
                200,
                new IngredientSpec("mat_beast_hide", 3),
                new IngredientSpec("mat_iron_ore", 1)),
            CreateRecipe(
                "recipe_guard_ring",
                "守りの指輪",
                "鉄鉱石と魔石の欠片で防御用の指輪を作成します。",
                RecipeDataType.Accessory,
                2,
                RecipeResultDataType.Equipment,
                "eq_guard_ring",
                180,
                300,
                new IngredientSpec("mat_iron_ore", 2),
                new IngredientSpec("mat_magic_shard", 2)),
            CreateRecipe(
                "recipe_swift_boots",
                "疾風の靴",
                "上質な革と魔石で素早さを補う靴を作成します。",
                RecipeDataType.Accessory,
                2,
                RecipeResultDataType.Equipment,
                "eq_swift_boots",
                240,
                301,
                new IngredientSpec("mat_fine_leather", 2),
                new IngredientSpec("mat_magic_stone", 1)),
        };

        CreateOrUpdateDatabase(recipes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Test synthesis master data was generated.");
    }

    private static RecipeData CreateRecipe(
        string id,
        string displayName,
        string description,
        RecipeDataType recipeType,
        int requiredSynthesisLevel,
        RecipeResultDataType resultType,
        string resultItemId,
        int cost,
        int sortOrder,
        params IngredientSpec[] ingredients)
    {
        var recipe = LoadOrCreate<RecipeData>(RecipeFolder + "/" + id + ".asset");
        var serialized = new SerializedObject(recipe);
        SetMasterFields(serialized, id, displayName, description);
        serialized.FindProperty("recipeType").enumValueIndex = (int)recipeType;
        serialized.FindProperty("requiredSynthesisLevel").intValue = requiredSynthesisLevel;
        serialized.FindProperty("cost").intValue = cost;
        serialized.FindProperty("resultType").enumValueIndex = (int)resultType;
        serialized.FindProperty("resultItemId").stringValue = resultItemId;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        SetIngredients(serialized.FindProperty("ingredients"), ingredients);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void SetIngredients(SerializedProperty ingredientsProperty, IReadOnlyList<IngredientSpec> ingredients)
    {
        ingredientsProperty.arraySize = ingredients.Count;

        for (var i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredientsProperty.GetArrayElementAtIndex(i);
            ingredient.FindPropertyRelative("itemId").stringValue = ingredients[i].ItemId;
            ingredient.FindPropertyRelative("count").intValue = ingredients[i].Count;
        }
    }

    private static void CreateOrUpdateDatabase(RecipeData[] recipes)
    {
        var database = LoadOrCreate<RecipeDatabase>(RecipeDatabasePath);
        var serialized = new SerializedObject(database);
        var entriesProperty = serialized.FindProperty("entries");
        entriesProperty.arraySize = recipes.Length;

        for (var i = 0; i < recipes.Length; i++)
        {
            entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            return existing;
        }

        DeleteInvalidAsset(path);
        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void DeleteInvalidAsset(string path)
    {
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void DeleteGeneratedAssets()
    {
        DeleteAssetFiles(RecipeFolder);
        if (File.Exists(RecipeDatabasePath))
        {
            AssetDatabase.DeleteAsset(RecipeDatabasePath);
        }
    }

    private static void DeleteAssetFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(folder, "*.asset", SearchOption.TopDirectoryOnly))
        {
            AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
        }
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "MasterData");
        CreateFolder(RootFolder, "Test");
        CreateFolder(TestFolder, "Recipes");
        CreateFolder(TestFolder, "Databases");
    }

    private static void CreateFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void SetMasterFields(SerializedObject serialized, string id, string displayName, string description)
    {
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
    }

    private readonly struct IngredientSpec
    {
        public IngredientSpec(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public int Count { get; }
    }
}
