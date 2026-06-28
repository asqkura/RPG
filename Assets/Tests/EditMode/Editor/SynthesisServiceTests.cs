using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Synthesis;
using UnityEditor;
using UnityEngine;

public sealed class SynthesisServiceTests
{
    [Test]
    public void TrySynthesizeConsumableConsumesCostsAndAddsResult()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 3);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_potion");

        Assert.IsTrue(result.CanSynthesize);
        Assert.AreEqual(1, context.SaveData.GetMaterialCount("mat_herb"));
        Assert.AreEqual(70, context.SaveData.Money);
        Assert.AreEqual(1, context.SaveData.GetConsumableCount("item_potion"));
    }

    [Test]
    public void TrySynthesizeEquipmentAddsOwnedEquipment()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(300);
        context.SaveData.AddMaterial("mat_herb", 2);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_sword");

        Assert.IsTrue(result.CanSynthesize);
        Assert.AreEqual(1, context.SaveData.OwnedEquipments.Count);
        Assert.AreEqual("eq_iron_sword", context.SaveData.OwnedEquipments[0].EquipmentId);
        Assert.IsNotNull(result.CreatedEquipment);
        Assert.AreSame(result.CreatedEquipment, context.SaveData.OwnedEquipments[0]);
    }

    [Test]
    public void TrySynthesizeEquipmentAddsRolledRarityModifiersAndSkill()
    {
        var context = CreateContext(new FixedRandom(99, 3, 0, 5, 0, 6, 0, 7, 0, 1));
        context.SaveData.SetSynthesisLevel(5);
        context.SaveData.AddMoney(300);
        context.SaveData.AddMaterial("mat_herb", 2);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_sword");

        Assert.IsTrue(result.CanSynthesize);
        Assert.IsNotNull(result.CreatedEquipment);
        Assert.AreEqual(EquipmentRarity.Legendary, result.CreatedEquipment.Rarity);
        Assert.AreEqual(3, result.CreatedEquipment.RandomModifiers.Count);
        Assert.AreEqual(EquipmentModifierType.Attack, result.CreatedEquipment.RandomModifiers[0].ModifierType);
        Assert.AreEqual(5, result.CreatedEquipment.RandomModifiers[0].Amount);
        Assert.AreEqual(EquipmentModifierType.Attack, result.CreatedEquipment.RandomModifiers[1].ModifierType);
        Assert.AreEqual(6, result.CreatedEquipment.RandomModifiers[1].Amount);
        Assert.AreEqual(EquipmentModifierType.Attack, result.CreatedEquipment.RandomModifiers[2].ModifierType);
        Assert.AreEqual(7, result.CreatedEquipment.RandomModifiers[2].Amount);
        Assert.AreEqual("skill_b", result.CreatedEquipment.RandomSkillId);
    }

    [Test]
    public void GetQuoteReturnsMaterialShortage()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 1);

        var quote = context.Service.GetQuote(context.SaveData, "syn_potion");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.NotEnoughMaterials, quote.FailureReason);
        Assert.AreEqual(1, quote.MaterialShortages.Count);
        Assert.AreEqual("mat_herb", quote.MaterialShortages[0].ItemId);
        Assert.AreEqual(2, quote.MaterialShortages[0].RequiredCount);
        Assert.AreEqual(1, quote.MaterialShortages[0].OwnedCount);
    }

    [Test]
    public void GetQuoteReturnsNotEnoughMoney()
    {
        var context = CreateContext();
        context.SaveData.AddMaterial("mat_herb", 2);

        var quote = context.Service.GetQuote(context.SaveData, "syn_potion");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.NotEnoughMoney, quote.FailureReason);
    }

    [Test]
    public void GetQuoteReturnsSynthesisLevelTooLow()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(500);
        context.SaveData.AddMaterial("mat_herb", 2);

        var quote = context.Service.GetQuote(context.SaveData, "syn_late");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.SynthesisLevelTooLow, quote.FailureReason);
    }

    [Test]
    public void GetQuoteAllowsRecipeWhenSynthesisLevelIsHighEnough()
    {
        var context = CreateContext();
        context.SaveData.SetSynthesisLevel(2);
        context.SaveData.AddMoney(500);
        context.SaveData.AddMaterial("mat_herb", 2);

        var quote = context.Service.GetQuote(context.SaveData, "syn_late");

        Assert.IsTrue(quote.CanSynthesize);
    }

    [Test]
    public void GetQuoteReturnsConsumableLimitReached()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 2);
        context.SaveData.AddConsumable("item_potion", 20);

        var quote = context.Service.GetQuote(context.SaveData, "syn_potion");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.ConsumableLimitReached, quote.FailureReason);
    }

    [Test]
    public void TrySynthesizeConsumableLimitReachedDoesNotMutateSaveData()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 2);
        context.SaveData.AddConsumable("item_potion", 20);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_potion");

        Assert.IsFalse(result.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.ConsumableLimitReached, result.FailureReason);
        Assert.AreEqual(100, context.SaveData.Money);
        Assert.AreEqual(2, context.SaveData.GetMaterialCount("mat_herb"));
        Assert.AreEqual(20, context.SaveData.GetConsumableCount("item_potion"));
    }

    [Test]
    public void GetQuoteReturnsRecipeNotFound()
    {
        var context = CreateContext();

        var quote = context.Service.GetQuote(context.SaveData, "missing");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.RecipeNotFound, quote.FailureReason);
    }

    [Test]
    public void FailedSynthesisDoesNotMutateSaveData()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 1);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_potion");

        Assert.IsFalse(result.CanSynthesize);
        Assert.AreEqual(100, context.SaveData.Money);
        Assert.AreEqual(1, context.SaveData.GetMaterialCount("mat_herb"));
        Assert.AreEqual(0, context.SaveData.GetConsumableCount("item_potion"));
        Assert.AreEqual(0, context.SaveData.OwnedEquipments.Count);
    }

    [Test]
    public void TrySynthesizeAggregatesDuplicateMaterialCosts()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 3);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_duplicate_material");

        Assert.IsTrue(result.CanSynthesize);
        Assert.AreEqual(0, context.SaveData.GetMaterialCount("mat_herb"));
        Assert.AreEqual(1, context.SaveData.GetConsumableCount("item_potion"));
    }

    [Test]
    public void FailedSynthesisWithDuplicateMaterialCostsDoesNotMutateSaveData()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(100);
        context.SaveData.AddMaterial("mat_herb", 2);

        var result = context.Service.TrySynthesize(context.SaveData, "syn_duplicate_material");

        Assert.IsFalse(result.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.NotEnoughMaterials, result.FailureReason);
        Assert.AreEqual(100, context.SaveData.Money);
        Assert.AreEqual(2, context.SaveData.GetMaterialCount("mat_herb"));
        Assert.AreEqual(0, context.SaveData.GetConsumableCount("item_potion"));
    }

    private static TestContext CreateContext(System.Random random = null)
    {
        var itemDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
        var equipmentDatabase = ScriptableObject.CreateInstance<EquipmentDatabase>();
        var recipeDatabase = ScriptableObject.CreateInstance<SynthesisRecipeDatabase>();

        var herb = CreateItem("mat_herb", "薬草", ItemDataType.Material);
        var potion = CreateItem("item_potion", "ポーション", ItemDataType.Consumable);
        var sword = CreateEquipment(
            "eq_iron_sword",
            "鉄の剣",
            EquipmentDataType.Weapon,
            new[] { EquipmentModifierType.Attack },
            "skill_a",
            "skill_b");

        SetDatabaseEntries(itemDatabase, herb, potion);
        SetDatabaseEntries(equipmentDatabase, sword);
        SetDatabaseEntries(
            recipeDatabase,
            CreateRecipe("syn_potion", SynthesisProductDataType.Consumable, potion, null, 1, 30, herb, 2),
            CreateRecipe("syn_sword", SynthesisProductDataType.Equipment, null, sword, 1, 120, herb, 2),
            CreateRecipe("syn_late", SynthesisProductDataType.Consumable, potion, null, 2, 30, herb, 2),
            CreateRecipe("syn_duplicate_material", SynthesisProductDataType.Consumable, potion, null, 1, 30, (herb, 1), (herb, 2)));

        return new TestContext(
            RunSaveData.CreateNew(),
            new SynthesisService(recipeDatabase, itemDatabase, equipmentDatabase, random));
    }

    private static ItemData CreateItem(string id, string displayName, ItemDataType itemType)
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        var serialized = new SerializedObject(item);
        SetMasterFields(serialized, id, displayName, string.Empty);
        serialized.FindProperty("itemType").enumValueIndex = (int)itemType;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    private static EquipmentData CreateEquipment(
        string id,
        string displayName,
        EquipmentDataType equipmentType,
        EquipmentModifierType[] allowedRandomModifierTypes = null,
        params string[] randomSkillPool)
    {
        var equipment = ScriptableObject.CreateInstance<EquipmentData>();
        var serialized = new SerializedObject(equipment);
        SetMasterFields(serialized, id, displayName, string.Empty);
        serialized.FindProperty("equipmentType").enumValueIndex = (int)equipmentType;

        if (allowedRandomModifierTypes != null)
        {
            var modifierTypes = serialized.FindProperty("allowedRandomModifierTypes");
            modifierTypes.arraySize = allowedRandomModifierTypes.Length;
            for (var i = 0; i < allowedRandomModifierTypes.Length; i++)
            {
                modifierTypes.GetArrayElementAtIndex(i).enumValueIndex = (int)allowedRandomModifierTypes[i];
            }
        }

        if (randomSkillPool != null)
        {
            var skillPool = serialized.FindProperty("randomSkillPool");
            skillPool.arraySize = randomSkillPool.Length;
            for (var i = 0; i < randomSkillPool.Length; i++)
            {
                skillPool.GetArrayElementAtIndex(i).stringValue = randomSkillPool[i];
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return equipment;
    }

    private static SynthesisRecipeData CreateRecipe(
        string id,
        SynthesisProductDataType productType,
        ItemData productItem,
        EquipmentData productEquipment,
        int requiredSynthesisLevel,
        int moneyCost,
        ItemData material,
        int materialCount)
    {
        return CreateRecipe(
            id,
            productType,
            productItem,
            productEquipment,
            requiredSynthesisLevel,
            moneyCost,
            new[] { (material, materialCount) });
    }

    private static SynthesisRecipeData CreateRecipe(
        string id,
        SynthesisProductDataType productType,
        ItemData productItem,
        EquipmentData productEquipment,
        int requiredSynthesisLevel,
        int moneyCost,
        params (ItemData Material, int Count)[] materialCosts)
    {
        var recipe = ScriptableObject.CreateInstance<SynthesisRecipeData>();
        var serialized = new SerializedObject(recipe);
        SetMasterFields(serialized, id, id, string.Empty);
        serialized.FindProperty("productType").enumValueIndex = (int)productType;
        serialized.FindProperty("productItem").objectReferenceValue = productItem;
        serialized.FindProperty("productEquipment").objectReferenceValue = productEquipment;
        serialized.FindProperty("requiredSynthesisLevel").intValue = requiredSynthesisLevel;
        serialized.FindProperty("moneyCost").intValue = moneyCost;
        var costs = serialized.FindProperty("materialCosts");
        costs.arraySize = materialCosts.Length;
        for (var i = 0; i < materialCosts.Length; i++)
        {
            costs.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = materialCosts[i].Material;
            costs.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = materialCosts[i].Count;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return recipe;
    }

    private static void SetMasterFields(SerializedObject serialized, string id, string displayName, string description)
    {
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
    }

    private static void SetDatabaseEntries<TData>(MasterDatabase<TData> database, params TData[] entries)
        where TData : MasterDataAsset
    {
        var serialized = new SerializedObject(database);
        var entriesProperty = serialized.FindProperty("entries");
        entriesProperty.arraySize = entries.Length;
        for (var i = 0; i < entries.Length; i++)
        {
            entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct TestContext
    {
        public TestContext(RunSaveData saveData, SynthesisService service)
        {
            SaveData = saveData;
            Service = service;
        }

        public RunSaveData SaveData { get; }
        public SynthesisService Service { get; }
    }

    private sealed class FixedRandom : System.Random
    {
        private readonly Queue<int> values;

        public FixedRandom(params int[] values)
        {
            this.values = new Queue<int>(values);
        }

        public override int Next(int maxValue)
        {
            return NextValue(0, maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            return NextValue(minValue, maxValue);
        }

        private int NextValue(int minValue, int maxValue)
        {
            Assert.IsTrue(values.Count > 0, "FixedRandom value queue was exhausted.");
            var value = values.Dequeue();
            Assert.GreaterOrEqual(value, minValue);
            Assert.Less(value, maxValue);
            return value;
        }
    }
}
