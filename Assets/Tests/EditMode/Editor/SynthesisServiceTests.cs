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
        Assert.AreEqual(2, context.SaveData.GetConsumableCount("item_potion"));
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
        Assert.AreEqual(EquipmentRarity.Common, context.SaveData.OwnedEquipments[0].Rarity);
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
    public void GetQuoteReturnsNotAvailableInCurrentPhase()
    {
        var context = CreateContext();
        context.SaveData.AddMoney(500);
        context.SaveData.AddMaterial("mat_herb", 2);

        var quote = context.Service.GetQuote(context.SaveData, "syn_late");

        Assert.IsFalse(quote.CanSynthesize);
        Assert.AreEqual(SynthesisFailureReason.NotAvailableInCurrentPhase, quote.FailureReason);
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

    private static TestContext CreateContext()
    {
        var itemDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
        var equipmentDatabase = ScriptableObject.CreateInstance<EquipmentDatabase>();
        var recipeDatabase = ScriptableObject.CreateInstance<SynthesisRecipeDatabase>();

        var herb = CreateItem("mat_herb", "薬草", ItemDataType.Material);
        var potion = CreateItem("item_potion", "ポーション", ItemDataType.Consumable);
        var sword = CreateEquipment("eq_iron_sword", "鉄の剣", EquipmentDataType.Weapon);

        SetDatabaseEntries(itemDatabase, herb, potion);
        SetDatabaseEntries(equipmentDatabase, sword);
        SetDatabaseEntries(
            recipeDatabase,
            CreateRecipe("syn_potion", SynthesisProductDataType.Consumable, "item_potion", 2, 1, 30, "mat_herb", 2),
            CreateRecipe("syn_sword", SynthesisProductDataType.Equipment, "eq_iron_sword", 1, 1, 120, "mat_herb", 2),
            CreateRecipe("syn_late", SynthesisProductDataType.Consumable, "item_potion", 1, 2, 30, "mat_herb", 2));

        return new TestContext(
            RunSaveData.CreateNew(),
            new SynthesisService(recipeDatabase, itemDatabase, equipmentDatabase));
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

    private static EquipmentData CreateEquipment(string id, string displayName, EquipmentDataType equipmentType)
    {
        var equipment = ScriptableObject.CreateInstance<EquipmentData>();
        var serialized = new SerializedObject(equipment);
        SetMasterFields(serialized, id, displayName, string.Empty);
        serialized.FindProperty("equipmentType").enumValueIndex = (int)equipmentType;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return equipment;
    }

    private static SynthesisRecipeData CreateRecipe(
        string id,
        SynthesisProductDataType productType,
        string productId,
        int resultCount,
        int availablePhase,
        int moneyCost,
        string materialId,
        int materialCount)
    {
        var recipe = ScriptableObject.CreateInstance<SynthesisRecipeData>();
        var serialized = new SerializedObject(recipe);
        SetMasterFields(serialized, id, id, string.Empty);
        serialized.FindProperty("productType").enumValueIndex = (int)productType;
        serialized.FindProperty("productId").stringValue = productId;
        serialized.FindProperty("resultCount").intValue = resultCount;
        serialized.FindProperty("availablePhase").intValue = availablePhase;
        serialized.FindProperty("moneyCost").intValue = moneyCost;
        var costs = serialized.FindProperty("materialCosts");
        costs.arraySize = 1;
        costs.GetArrayElementAtIndex(0).FindPropertyRelative("itemId").stringValue = materialId;
        costs.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = materialCount;
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
}
