using System.Collections.Generic;
using System.IO;
using RPG.MasterData;
using UnityEditor;
using UnityEngine;

public static class TestSynthesisDataBuilder
{
    private const string RootFolder = "Assets/MasterData";
    private const string TestFolder = RootFolder + "/Test";
    private const string SynthesisFolder = TestFolder + "/Synthesis";
    private const string SynthesisLevelUpFolder = TestFolder + "/SynthesisLevelUp";
    private const string DatabaseFolder = TestFolder + "/Databases";

    private const string RecipeDatabasePath = DatabaseFolder + "/TestSynthesisRecipeDatabase.asset";
    private const string LevelUpRequirementDatabasePath = DatabaseFolder + "/TestSynthesisLevelUpRequirementDatabase.asset";

    [MenuItem("Tools/RPG/Build Test Synthesis Data")]
    public static void Build()
    {
        EnsureFolders();
        DeleteGeneratedAssets();
        AssetDatabase.Refresh();

        var recipes = new[]
        {
            CreateOrUpdateRecipe(
                "syn_potion",
                SynthesisProductDataType.Consumable,
                "item_potion",
                1,
                30,
                new[] { new MaterialCostSpec("mat_herb", 2) },
                0),
            CreateOrUpdateRecipe(
                "syn_high_potion",
                SynthesisProductDataType.Consumable,
                "item_high_potion",
                2,
                90,
                new[] { new MaterialCostSpec("mat_healing_grass", 1), new MaterialCostSpec("mat_magic_shard", 1) },
                1),
            CreateOrUpdateRecipe(
                "syn_antidote",
                SynthesisProductDataType.Consumable,
                "item_antidote",
                1,
                40,
                new[] { new MaterialCostSpec("mat_herb", 1), new MaterialCostSpec("mat_magic_shard", 1) },
                2),
            CreateOrUpdateRecipe(
                "syn_mana_drop",
                SynthesisProductDataType.Consumable,
                "item_mana_drop",
                1,
                60,
                new[] { new MaterialCostSpec("mat_magic_shard", 2) },
                3),
            CreateOrUpdateRecipe(
                "syn_guard_salve",
                SynthesisProductDataType.Consumable,
                "item_guard_salve",
                2,
                80,
                new[] { new MaterialCostSpec("mat_healing_grass", 1), new MaterialCostSpec("mat_beast_hide", 1) },
                4),
            CreateOrUpdateRecipe(
                "syn_iron_sword",
                SynthesisProductDataType.Equipment,
                "eq_iron_sword",
                1,
                180,
                new[] { new MaterialCostSpec("mat_iron_ore", 3), new MaterialCostSpec("mat_sturdy_wood", 1) },
                10),
            CreateOrUpdateRecipe(
                "syn_leather_armor",
                SynthesisProductDataType.Equipment,
                "eq_leather_armor",
                1,
                160,
                new[] { new MaterialCostSpec("mat_beast_hide", 3), new MaterialCostSpec("mat_iron_ore", 1) },
                11),
            CreateOrUpdateRecipe(
                "syn_guard_ring",
                SynthesisProductDataType.Equipment,
                "eq_guard_ring",
                2,
                220,
                new[] { new MaterialCostSpec("mat_magic_stone", 1), new MaterialCostSpec("mat_fine_leather", 1) },
                12),
            CreateOrUpdateRecipe(
                "syn_apprentice_dagger",
                SynthesisProductDataType.Equipment,
                "eq_apprentice_dagger",
                1,
                120,
                new[] { new MaterialCostSpec("mat_iron_ore", 1), new MaterialCostSpec("mat_sturdy_wood", 2), new MaterialCostSpec("mat_magic_shard", 1) },
                13),
            CreateOrUpdateRecipe(
                "syn_hunter_bow",
                SynthesisProductDataType.Equipment,
                "eq_hunter_bow",
                2,
                180,
                new[] { new MaterialCostSpec("mat_hard_wood", 2), new MaterialCostSpec("mat_sturdy_wood", 2), new MaterialCostSpec("mat_beast_hide", 1) },
                14),
            CreateOrUpdateRecipe(
                "syn_oak_staff",
                SynthesisProductDataType.Equipment,
                "eq_oak_staff",
                2,
                200,
                new[] { new MaterialCostSpec("mat_hard_wood", 2), new MaterialCostSpec("mat_magic_shard", 2), new MaterialCostSpec("mat_magic_stone", 1) },
                15),
            CreateOrUpdateRecipe(
                "syn_iron_mail",
                SynthesisProductDataType.Equipment,
                "eq_iron_mail",
                3,
                320,
                new[] { new MaterialCostSpec("mat_steel_ore", 2), new MaterialCostSpec("mat_iron_ore", 4), new MaterialCostSpec("mat_fine_leather", 1) },
                16),
            CreateOrUpdateRecipe(
                "syn_lucky_charm",
                SynthesisProductDataType.Equipment,
                "eq_lucky_charm",
                1,
                140,
                new[] { new MaterialCostSpec("mat_magic_shard", 2), new MaterialCostSpec("mat_fine_leather", 1) },
                17),
            CreateOrUpdateRecipe(
                "syn_swift_boots",
                SynthesisProductDataType.Equipment,
                "eq_swift_boots",
                3,
                260,
                new[] { new MaterialCostSpec("mat_fine_leather", 2), new MaterialCostSpec("mat_hard_wood", 1), new MaterialCostSpec("mat_magic_stone", 1) },
                18),
        };

        CreateOrUpdateDatabase(RecipeDatabasePath, recipes);

        var levelUpRequirements = new[]
        {
            CreateOrUpdateLevelUpRequirement(
                "syn_level_1_to_2",
                "合成Lv2へ強化",
                "序盤の強化装備と基本アクセサリのレシピが増えます。",
                1,
                2,
                100,
                new[]
                {
                    new MaterialCostSpec("mat_iron_ore", 3),
                    new MaterialCostSpec("mat_sturdy_wood", 2),
                    new MaterialCostSpec("mat_beast_hide", 2)
                },
                0)
        };

        CreateOrUpdateLevelUpRequirementDatabase(LevelUpRequirementDatabasePath, levelUpRequirements);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Test synthesis master data was generated.");
    }

    private static SynthesisRecipeData CreateOrUpdateRecipe(
        string id,
        SynthesisProductDataType productType,
        string productId,
        int ignoredFormerSynthesisLevel,
        int moneyCost,
        IReadOnlyList<MaterialCostSpec> materialCosts,
        int sortOrder)
    {
        var recipe = LoadOrCreate<SynthesisRecipeData>(SynthesisFolder + "/" + id + ".asset");
        var serialized = new SerializedObject(recipe);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("productType").enumValueIndex = (int)productType;
        serialized.FindProperty("productItem").objectReferenceValue = productType == SynthesisProductDataType.Consumable
            ? LoadMasterAsset<ItemData>("Items", productId)
            : null;
        serialized.FindProperty("productEquipment").objectReferenceValue = productType == SynthesisProductDataType.Equipment
            ? LoadMasterAsset<EquipmentData>("Equipment", productId)
            : null;
        serialized.FindProperty("moneyCost").intValue = moneyCost;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        SetMaterialCosts(serialized.FindProperty("materialCosts"), materialCosts);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static SynthesisLevelUpRequirementData CreateOrUpdateLevelUpRequirement(
        string id,
        string displayName,
        string description,
        int currentLevel,
        int targetLevel,
        int moneyCost,
        IReadOnlyList<MaterialCostSpec> materialCosts,
        int sortOrder)
    {
        var requirement = LoadOrCreate<SynthesisLevelUpRequirementData>(SynthesisLevelUpFolder + "/" + id + ".asset");
        var serialized = new SerializedObject(requirement);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
        serialized.FindProperty("currentLevel").intValue = currentLevel;
        serialized.FindProperty("targetLevel").intValue = targetLevel;
        serialized.FindProperty("moneyCost").intValue = moneyCost;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        SetMaterialCosts(serialized.FindProperty("materialCosts"), materialCosts);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(requirement);
        return requirement;
    }

    private static void SetMaterialCosts(SerializedProperty property, IReadOnlyList<MaterialCostSpec> materialCosts)
    {
        property.arraySize = materialCosts.Count;
        for (var i = 0; i < materialCosts.Count; i++)
        {
            var cost = property.GetArrayElementAtIndex(i);
            cost.FindPropertyRelative("item").objectReferenceValue = LoadMasterAsset<ItemData>("Items", materialCosts[i].ItemId);
            cost.FindPropertyRelative("count").intValue = materialCosts[i].Count;
        }
    }

    private static T LoadMasterAsset<T>(string folderName, string id) where T : MasterDataAsset
    {
        return AssetDatabase.LoadAssetAtPath<T>($"{TestFolder}/{folderName}/{id}.asset");
    }

    private static void CreateOrUpdateDatabase(string path, SynthesisRecipeData[] entries)
    {
        var database = LoadOrCreate<SynthesisRecipeDatabase>(path);
        var serialized = new SerializedObject(database);
        var entriesProperty = serialized.FindProperty("entries");
        entriesProperty.arraySize = entries.Length;

        for (var i = 0; i < entries.Length; i++)
        {
            entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void CreateOrUpdateLevelUpRequirementDatabase(string path, SynthesisLevelUpRequirementData[] entries)
    {
        var database = LoadOrCreate<SynthesisLevelUpRequirementDatabase>(path);
        var serialized = new SerializedObject(database);
        var entriesProperty = serialized.FindProperty("entries");
        entriesProperty.arraySize = entries.Length;

        for (var i = 0; i < entries.Length; i++)
        {
            entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
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

    private static void DeleteGeneratedAssets()
    {
        if (!Directory.Exists(SynthesisFolder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(SynthesisFolder, "*.asset", SearchOption.TopDirectoryOnly))
        {
            AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
        }

        if (!Directory.Exists(SynthesisLevelUpFolder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(SynthesisLevelUpFolder, "*.asset", SearchOption.TopDirectoryOnly))
        {
            AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
        }
    }

    private static void DeleteInvalidAsset(string path)
    {
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "MasterData");
        CreateFolder(RootFolder, "Test");
        CreateFolder(TestFolder, "Synthesis");
        CreateFolder(TestFolder, "SynthesisLevelUp");
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

    private readonly struct MaterialCostSpec
    {
        public MaterialCostSpec(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public int Count { get; }
    }
}
