using System;
using System.Collections.Generic;
using RPG.MasterData;
using RPG.SaveData;

namespace RPG.Synthesis
{
    public enum SynthesisFailureReason
    {
        None,
        InvalidRequest,
        RecipeNotFound,
        SynthesisLevelTooLow,
        ProductNotFound,
        NotEnoughMaterials,
        NotEnoughMoney,
        ConsumableLimitReached
    }

    public readonly struct SynthesisMaterialShortage
    {
        public SynthesisMaterialShortage(string itemId, int requiredCount, int ownedCount)
        {
            ItemId = itemId ?? string.Empty;
            RequiredCount = Math.Max(0, requiredCount);
            OwnedCount = Math.Max(0, ownedCount);
        }

        public string ItemId { get; }
        public int RequiredCount { get; }
        public int OwnedCount { get; }
        public int MissingCount => Math.Max(0, RequiredCount - OwnedCount);
    }

    public readonly struct SynthesisQuote
    {
        public SynthesisQuote(
            bool canSynthesize,
            SynthesisFailureReason failureReason,
            SynthesisRecipeData recipe,
            string productId,
            SynthesisProductDataType productType,
            int resultCount,
            int moneyCost,
            IReadOnlyList<SynthesisMaterialShortage> materialShortages,
            OwnedEquipmentSaveData createdEquipment = null)
        {
            CanSynthesize = canSynthesize;
            FailureReason = failureReason;
            Recipe = recipe;
            ProductId = productId ?? string.Empty;
            ProductType = productType;
            ResultCount = Math.Max(0, resultCount);
            MoneyCost = Math.Max(0, moneyCost);
            MaterialShortages = materialShortages ?? Array.Empty<SynthesisMaterialShortage>();
            CreatedEquipment = createdEquipment;
        }

        public bool CanSynthesize { get; }
        public SynthesisFailureReason FailureReason { get; }
        public SynthesisRecipeData Recipe { get; }
        public string ProductId { get; }
        public SynthesisProductDataType ProductType { get; }
        public int ResultCount { get; }
        public int MoneyCost { get; }
        public IReadOnlyList<SynthesisMaterialShortage> MaterialShortages { get; }
        public OwnedEquipmentSaveData CreatedEquipment { get; }
    }

    public sealed class SynthesisService
    {
        private readonly SynthesisRecipeDatabase recipeDatabase;
        private readonly Random random;

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase)
            : this(recipeDatabase, itemDatabase, equipmentDatabase, new Random())
        {
        }

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            Random random)
        {
            this.recipeDatabase = recipeDatabase;
            this.random = random ?? new Random();
        }

        public SynthesisQuote GetQuote(RunSaveData saveData, string recipeId)
        {
            if (saveData == null || recipeDatabase == null || string.IsNullOrWhiteSpace(recipeId))
            {
                return Failure(SynthesisFailureReason.InvalidRequest);
            }

            if (!recipeDatabase.TryGetById(recipeId, out var recipe) || recipe == null)
            {
                return Failure(SynthesisFailureReason.RecipeNotFound);
            }

            if (recipe.RequiredSynthesisLevel > saveData.SynthesisLevel)
            {
                return Failure(SynthesisFailureReason.SynthesisLevelTooLow, recipe);
            }

            if (!ProductExists(recipe))
            {
                return Failure(SynthesisFailureReason.ProductNotFound, recipe);
            }

            var shortages = GetMaterialShortages(saveData, recipe);
            if (shortages.Count > 0)
            {
                return Failure(SynthesisFailureReason.NotEnoughMaterials, recipe, shortages);
            }

            if (saveData.Money < recipe.MoneyCost)
            {
                return Failure(SynthesisFailureReason.NotEnoughMoney, recipe);
            }

            if (WouldExceedConsumableLimit(saveData, recipe))
            {
                return Failure(SynthesisFailureReason.ConsumableLimitReached, recipe);
            }

            return new SynthesisQuote(
                true,
                SynthesisFailureReason.None,
                recipe,
                recipe.ProductId,
                recipe.ProductType,
                recipe.ResultCount,
                recipe.MoneyCost,
                Array.Empty<SynthesisMaterialShortage>());
        }

        public SynthesisQuote TrySynthesize(RunSaveData saveData, string recipeId)
        {
            var quote = GetQuote(saveData, recipeId);
            if (!quote.CanSynthesize)
            {
                return quote;
            }

            if (!saveData.TrySpendMoney(quote.MoneyCost))
            {
                return WithFailure(quote, SynthesisFailureReason.NotEnoughMoney);
            }

            var consumedMaterials = new List<KeyValuePair<string, int>>();
            foreach (var cost in GetRequiredMaterialCounts(quote.Recipe))
            {
                if (!saveData.TryConsumeMaterial(cost.Key, cost.Value))
                {
                    saveData.AddMoney(quote.MoneyCost);
                    foreach (var consumedMaterial in consumedMaterials)
                    {
                        saveData.AddMaterial(consumedMaterial.Key, consumedMaterial.Value);
                    }

                    return WithFailure(quote, SynthesisFailureReason.NotEnoughMaterials);
                }

                consumedMaterials.Add(cost);
            }

            return AddProduct(saveData, quote);
        }

        private bool ProductExists(SynthesisRecipeData recipe)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.ProductId))
            {
                return false;
            }

            if (recipe.ProductType == SynthesisProductDataType.Consumable)
            {
                return recipe.ProductItem != null
                    && recipe.ProductItem.ItemType == ItemDataType.Consumable;
            }

            return recipe.ProductEquipment != null;
        }

        private static List<SynthesisMaterialShortage> GetMaterialShortages(RunSaveData saveData, SynthesisRecipeData recipe)
        {
            var shortages = new List<SynthesisMaterialShortage>();
            foreach (var cost in GetRequiredMaterialCounts(recipe))
            {
                var ownedCount = saveData.GetMaterialCount(cost.Key);
                if (ownedCount < cost.Value)
                {
                    shortages.Add(new SynthesisMaterialShortage(cost.Key, cost.Value, ownedCount));
                }
            }

            return shortages;
        }

        private static Dictionary<string, int> GetRequiredMaterialCounts(SynthesisRecipeData recipe)
        {
            var requiredCountsByItemId = new Dictionary<string, int>();
            if (recipe == null)
            {
                return requiredCountsByItemId;
            }

            foreach (var cost in recipe.MaterialCosts)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                requiredCountsByItemId.TryGetValue(cost.ItemId, out var currentCount);
                requiredCountsByItemId[cost.ItemId] = currentCount + cost.Count;
            }

            return requiredCountsByItemId;
        }

        private static bool WouldExceedConsumableLimit(RunSaveData saveData, SynthesisRecipeData recipe)
        {
            return recipe.ProductType == SynthesisProductDataType.Consumable
                && saveData.GetTotalConsumableCount() + recipe.ResultCount > RunSaveData.MaxConsumableCount;
        }

        private SynthesisQuote AddProduct(RunSaveData saveData, SynthesisQuote quote)
        {
            if (quote.ProductType == SynthesisProductDataType.Consumable)
            {
                saveData.AddConsumable(quote.ProductId, quote.ResultCount);
                return quote;
            }

            var equipment = CreateOwnedEquipment(quote.ProductId, saveData.SynthesisLevel, quote.Recipe.ProductEquipment);
            saveData.AddOwnedEquipment(equipment);
            return new SynthesisQuote(
                quote.CanSynthesize,
                quote.FailureReason,
                quote.Recipe,
                quote.ProductId,
                quote.ProductType,
                quote.ResultCount,
                quote.MoneyCost,
                quote.MaterialShortages,
                equipment);
        }

        private OwnedEquipmentSaveData CreateOwnedEquipment(string equipmentId, int synthesisLevel, EquipmentData equipment)
        {
            var rarity = RollSynthesisRarity(synthesisLevel);
            var ownedEquipment = new OwnedEquipmentSaveData(
                CreateOwnedEquipmentInstanceId(equipmentId),
                equipmentId,
                rarity);

            AddRandomModifiers(ownedEquipment, equipment, rarity);
            ownedEquipment.RandomSkillId = RollRandomSkill(equipment, rarity);
            return ownedEquipment;
        }

        private EquipmentRarity RollSynthesisRarity(int synthesisLevel)
        {
            var roll = random.Next(100);
            var commonThreshold = synthesisLevel switch
            {
                1 => 75,
                2 => 65,
                3 => 55,
                4 => 45,
                _ => 35
            };
            var rareThreshold = synthesisLevel switch
            {
                1 => 98,
                2 => 95,
                3 => 90,
                4 => 85,
                _ => 80
            };
            var epicThreshold = synthesisLevel switch
            {
                1 => 100,
                2 => 100,
                3 => 99,
                4 => 98,
                _ => 97
            };

            if (roll < commonThreshold)
            {
                return EquipmentRarity.Common;
            }

            if (roll < rareThreshold)
            {
                return EquipmentRarity.Rare;
            }

            return roll < epicThreshold ? EquipmentRarity.Epic : EquipmentRarity.Legendary;
        }

        private void AddRandomModifiers(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment, EquipmentRarity rarity)
        {
            var count = RollRandomModifierCount(rarity);
            for (var i = 0; i < count; i++)
            {
                var modifierType = i == 0
                    ? RollRandomStatModifierType(equipment)
                    : RollRandomModifierType(equipment);
                ownedEquipment.AddRandomModifier(new EquipmentModifierSaveData(
                    modifierType,
                    string.Empty,
                    RollRandomModifierAmount(modifierType)));
            }
        }

        private int RollRandomModifierCount(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 1,
                EquipmentRarity.Rare => 1,
                EquipmentRarity.Epic => random.Next(1, 3),
                EquipmentRarity.Legendary => random.Next(2, 4),
                _ => 0
            };
        }

        private EquipmentModifierType RollRandomModifierType(EquipmentData equipment)
        {
            var candidates = equipment != null && equipment.AllowedRandomModifierTypes.Count > 0
                ? equipment.AllowedRandomModifierTypes
                : DefaultRandomModifierTypes;
            return candidates[random.Next(candidates.Count)];
        }

        private EquipmentModifierType RollRandomStatModifierType(EquipmentData equipment)
        {
            var source = equipment != null && equipment.AllowedRandomModifierTypes.Count > 0
                ? equipment.AllowedRandomModifierTypes
                : DefaultRandomModifierTypes;
            var candidates = new List<EquipmentModifierType>();
            foreach (var modifierType in source)
            {
                if (IsStatModifier(modifierType))
                {
                    candidates.Add(modifierType);
                }
            }

            return candidates.Count > 0
                ? candidates[random.Next(candidates.Count)]
                : RollRandomModifierType(equipment);
        }

        private int RollRandomModifierAmount(EquipmentModifierType modifierType)
        {
            return modifierType switch
            {
                EquipmentModifierType.Hp => random.Next(10, 26),
                EquipmentModifierType.Attack => random.Next(3, 9),
                EquipmentModifierType.Magic => random.Next(3, 9),
                EquipmentModifierType.Defense => random.Next(3, 9),
                EquipmentModifierType.Speed => random.Next(3, 9),
                EquipmentModifierType.CriticalRate => random.Next(3, 9),
                EquipmentModifierType.AttributeResistance => 10,
                EquipmentModifierType.StatusResistance => 10,
                EquipmentModifierType.DebuffResistance => 10,
                _ => 0
            };
        }

        private static bool IsStatModifier(EquipmentModifierType modifierType)
        {
            return modifierType == EquipmentModifierType.Hp
                || modifierType == EquipmentModifierType.Attack
                || modifierType == EquipmentModifierType.Magic
                || modifierType == EquipmentModifierType.Defense
                || modifierType == EquipmentModifierType.Speed
                || modifierType == EquipmentModifierType.CriticalRate;
        }

        private string RollRandomSkill(EquipmentData equipment, EquipmentRarity rarity)
        {
            if (equipment == null || equipment.RandomSkillPool.Count == 0 || !ShouldAddRandomSkill(rarity))
            {
                return string.Empty;
            }

            return equipment.RandomSkillPool[random.Next(equipment.RandomSkillPool.Count)];
        }

        private bool ShouldAddRandomSkill(EquipmentRarity rarity)
        {
            var chance = rarity switch
            {
                EquipmentRarity.Rare => 30,
                EquipmentRarity.Epic => 70,
                EquipmentRarity.Legendary => 100,
                _ => 0
            };
            return random.Next(100) < chance;
        }

        private static string CreateOwnedEquipmentInstanceId(string equipmentId)
        {
            return $"owned_{equipmentId}_{Guid.NewGuid():N}";
        }

        private static SynthesisQuote Failure(
            SynthesisFailureReason reason,
            SynthesisRecipeData recipe = null,
            IReadOnlyList<SynthesisMaterialShortage> materialShortages = null)
        {
            return new SynthesisQuote(
                false,
                reason,
                recipe,
                recipe != null ? recipe.ProductId : string.Empty,
                recipe != null ? recipe.ProductType : default,
                recipe != null ? recipe.ResultCount : 0,
                recipe != null ? recipe.MoneyCost : 0,
                materialShortages);
        }

        private static SynthesisQuote WithFailure(SynthesisQuote quote, SynthesisFailureReason reason)
        {
            return new SynthesisQuote(
                false,
                reason,
                quote.Recipe,
                quote.ProductId,
                quote.ProductType,
                quote.ResultCount,
                quote.MoneyCost,
                quote.MaterialShortages,
                quote.CreatedEquipment);
        }

        private static readonly IReadOnlyList<EquipmentModifierType> DefaultRandomModifierTypes = new[]
        {
            EquipmentModifierType.Hp,
            EquipmentModifierType.Attack,
            EquipmentModifierType.Magic,
            EquipmentModifierType.Defense,
            EquipmentModifierType.Speed,
            EquipmentModifierType.CriticalRate,
            EquipmentModifierType.AttributeResistance,
            EquipmentModifierType.StatusResistance,
            EquipmentModifierType.DebuffResistance
        };
    }
}
