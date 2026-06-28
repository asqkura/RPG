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
        ProductNotFound,
        NotEnoughMaterials,
        NotEnoughMoney,
        ConsumableLimitReached
    }

    public enum SynthesisLevelUpFailureReason
    {
        None,
        InvalidRequest,
        MaxLevelReached,
        NotEnoughMaterials,
        NotEnoughMoney
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

    public readonly struct SynthesisLevelUpQuote
    {
        public SynthesisLevelUpQuote(
            bool canLevelUp,
            SynthesisLevelUpFailureReason failureReason,
            int currentLevel,
            int targetLevel,
            int moneyCost,
            IReadOnlyList<SynthesisMaterialShortage> materialShortages,
            SynthesisLevelUpRequirementData requirement = null)
        {
            CanLevelUp = canLevelUp;
            FailureReason = failureReason;
            CurrentLevel = Math.Max(0, currentLevel);
            TargetLevel = Math.Max(0, targetLevel);
            MoneyCost = Math.Max(0, moneyCost);
            MaterialShortages = materialShortages ?? Array.Empty<SynthesisMaterialShortage>();
            Requirement = requirement;
        }

        public bool CanLevelUp { get; }
        public SynthesisLevelUpFailureReason FailureReason { get; }
        public int CurrentLevel { get; }
        public int TargetLevel { get; }
        public int MoneyCost { get; }
        public IReadOnlyList<SynthesisMaterialShortage> MaterialShortages { get; }
        public SynthesisLevelUpRequirementData Requirement { get; }
    }

    public sealed class SynthesisService
    {
        private readonly SynthesisRecipeDatabase recipeDatabase;
        private readonly SynthesisLevelUpRequirementDatabase levelUpRequirementDatabase;
        private readonly Random random;

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase)
            : this(recipeDatabase, itemDatabase, equipmentDatabase, null, new Random())
        {
        }

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            Random random)
            : this(recipeDatabase, itemDatabase, equipmentDatabase, null, random)
        {
        }

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            SynthesisLevelUpRequirementDatabase levelUpRequirementDatabase)
            : this(recipeDatabase, itemDatabase, equipmentDatabase, levelUpRequirementDatabase, new Random())
        {
        }

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            SynthesisLevelUpRequirementDatabase levelUpRequirementDatabase,
            Random random)
        {
            this.recipeDatabase = recipeDatabase;
            this.levelUpRequirementDatabase = levelUpRequirementDatabase;
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

        public SynthesisLevelUpQuote GetLevelUpQuote(RunSaveData saveData)
        {
            if (saveData == null)
            {
                return LevelUpFailure(SynthesisLevelUpFailureReason.InvalidRequest);
            }

            if (saveData.SynthesisLevel >= RunSaveData.MaxSynthesisLevel)
            {
                return LevelUpFailure(
                    SynthesisLevelUpFailureReason.MaxLevelReached,
                    saveData.SynthesisLevel,
                    saveData.SynthesisLevel);
            }

            var requirement = GetLevelUpRequirement(saveData.SynthesisLevel);
            if (requirement.TargetLevel != saveData.SynthesisLevel + 1)
            {
                return LevelUpFailure(
                    SynthesisLevelUpFailureReason.InvalidRequest,
                    saveData.SynthesisLevel,
                    saveData.SynthesisLevel + 1,
                    requirement.MoneyCost,
                    null,
                    requirement.Data);
            }

            var shortages = GetMaterialShortages(saveData, requirement.MaterialCosts);
            if (shortages.Count > 0)
            {
                return LevelUpFailure(
                    SynthesisLevelUpFailureReason.NotEnoughMaterials,
                    saveData.SynthesisLevel,
                    requirement.TargetLevel,
                    requirement.MoneyCost,
                    shortages,
                    requirement.Data);
            }

            if (saveData.Money < requirement.MoneyCost)
            {
                return LevelUpFailure(
                    SynthesisLevelUpFailureReason.NotEnoughMoney,
                    saveData.SynthesisLevel,
                    requirement.TargetLevel,
                    requirement.MoneyCost,
                    null,
                    requirement.Data);
            }

            return new SynthesisLevelUpQuote(
                true,
                SynthesisLevelUpFailureReason.None,
                saveData.SynthesisLevel,
                requirement.TargetLevel,
                requirement.MoneyCost,
                Array.Empty<SynthesisMaterialShortage>(),
                requirement.Data);
        }

        public SynthesisLevelUpQuote TryRaiseSynthesisLevel(RunSaveData saveData)
        {
            var quote = GetLevelUpQuote(saveData);
            if (!quote.CanLevelUp)
            {
                return quote;
            }

            if (!saveData.TrySpendMoney(quote.MoneyCost))
            {
                return WithLevelUpFailure(quote, SynthesisLevelUpFailureReason.NotEnoughMoney);
            }

            var requirement = GetLevelUpRequirement(quote.CurrentLevel);
            var consumedMaterials = new List<KeyValuePair<string, int>>();
            foreach (var cost in requirement.MaterialCosts)
            {
                if (!saveData.TryConsumeMaterial(cost.Key, cost.Value))
                {
                    saveData.AddMoney(quote.MoneyCost);
                    foreach (var consumedMaterial in consumedMaterials)
                    {
                        saveData.AddMaterial(consumedMaterial.Key, consumedMaterial.Value);
                    }

                    return WithLevelUpFailure(quote, SynthesisLevelUpFailureReason.NotEnoughMaterials);
                }

                consumedMaterials.Add(cost);
            }

            if (!saveData.TryRaiseSynthesisLevel())
            {
                saveData.AddMoney(quote.MoneyCost);
                foreach (var consumedMaterial in consumedMaterials)
                {
                    saveData.AddMaterial(consumedMaterial.Key, consumedMaterial.Value);
                }

                return WithLevelUpFailure(quote, SynthesisLevelUpFailureReason.MaxLevelReached);
            }

            return quote;
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
            return GetMaterialShortages(saveData, GetRequiredMaterialCounts(recipe));
        }

        private static List<SynthesisMaterialShortage> GetMaterialShortages(
            RunSaveData saveData,
            IReadOnlyDictionary<string, int> requiredMaterialCounts)
        {
            var shortages = new List<SynthesisMaterialShortage>();
            foreach (var cost in requiredMaterialCounts)
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
            return GetRequiredMaterialCounts(recipe?.MaterialCosts);
        }

        private static Dictionary<string, int> GetRequiredMaterialCounts(IReadOnlyList<SynthesisMaterialCostData> materialCosts)
        {
            var requiredCountsByItemId = new Dictionary<string, int>();
            if (materialCosts == null)
            {
                return requiredCountsByItemId;
            }

            foreach (var cost in materialCosts)
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
            var ownedEquipment = new OwnedEquipmentSaveData(
                CreateOwnedEquipmentInstanceId(equipmentId),
                equipmentId);

            AddRandomStatModifiers(ownedEquipment, equipment, synthesisLevel);
            RollRandomPassive(ownedEquipment, equipment, synthesisLevel);
            return ownedEquipment;
        }

        private void AddRandomStatModifiers(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment, int synthesisLevel)
        {
            var candidates = GetRandomStatCandidates(equipment);
            foreach (var modifierType in candidates)
            {
                var amount = RollRandomStatModifierAmount(modifierType, synthesisLevel);
                if (amount == 0)
                {
                    continue;
                }

                ownedEquipment.AddRandomStatModifier(new EquipmentModifierSaveData(
                    modifierType,
                    string.Empty,
                    amount));
            }
        }

        private List<EquipmentModifierType> GetRandomStatCandidates(EquipmentData equipment)
        {
            var candidates = new List<EquipmentModifierType>();
            if (equipment == null)
            {
                candidates.Add(EquipmentModifierType.Speed);
                return candidates;
            }

            if (equipment.AllowedRandomStatTypes.Count > 0)
            {
                foreach (var candidate in equipment.AllowedRandomStatTypes)
                {
                    AddUniqueStatCandidate(candidates, candidate);
                }
            }
            else
            {
                var stats = equipment.StatModifiers;
                if (stats != null)
                {
                    if (stats.Attack != 0)
                    {
                        AddUniqueStatCandidate(candidates, EquipmentModifierType.Attack);
                    }

                    if (stats.Magic != 0)
                    {
                        AddUniqueStatCandidate(candidates, EquipmentModifierType.Magic);
                    }

                    if (stats.Defense != 0)
                    {
                        AddUniqueStatCandidate(candidates, EquipmentModifierType.Defense);
                    }

                    if (stats.Speed != 0)
                    {
                        AddUniqueStatCandidate(candidates, EquipmentModifierType.Speed);
                    }

                    if (stats.CriticalRate != 0f)
                    {
                        AddUniqueStatCandidate(candidates, EquipmentModifierType.CriticalRate);
                    }
                }
            }

            AddUniqueStatCandidate(candidates, EquipmentModifierType.Speed);
            return candidates;
        }

        private int RollRandomStatModifierAmount(EquipmentModifierType modifierType, int synthesisLevel)
        {
            var qualityBonus = RollQualityBonus(synthesisLevel);
            return modifierType switch
            {
                EquipmentModifierType.Attack => random.Next(0, 4) + qualityBonus,
                EquipmentModifierType.Magic => random.Next(0, 4) + qualityBonus,
                EquipmentModifierType.Defense => random.Next(0, 4) + qualityBonus,
                EquipmentModifierType.Speed => RollSpeedIndividuality(synthesisLevel),
                EquipmentModifierType.CriticalRate => random.Next(0, 3) + qualityBonus,
                _ => 0
            };
        }

        private int RollQualityBonus(int synthesisLevel)
        {
            var chance = synthesisLevel switch
            {
                1 => 20,
                2 => 35,
                3 => 50,
                4 => 65,
                _ => 80
            };

            return random.Next(100) < chance ? 1 : 0;
        }

        private int RollSpeedIndividuality(int synthesisLevel)
        {
            var roll = random.Next(100);
            var positiveThreshold = synthesisLevel switch
            {
                1 => 45,
                2 => 55,
                3 => 65,
                4 => 75,
                _ => 85
            };
            if (roll < 15)
            {
                return -1;
            }

            return roll < positiveThreshold ? 1 : 0;
        }

        private void RollRandomPassive(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment, int synthesisLevel)
        {
            if (ownedEquipment == null || equipment == null || equipment.RandomPassivePool.Count == 0)
            {
                return;
            }

            var passive = equipment.RandomPassivePool[random.Next(equipment.RandomPassivePool.Count)];
            if (passive == null || string.IsNullOrWhiteSpace(passive.PassiveId))
            {
                return;
            }

            ownedEquipment.RandomPassiveId = passive.PassiveId;
            ownedEquipment.RandomPassiveLevel = RollPassiveLevel(passive, synthesisLevel);
        }

        private int RollPassiveLevel(EquipmentRandomPassiveData passive, int synthesisLevel)
        {
            var maxLevel = Math.Max(passive.MinLevel, passive.MaxLevel);
            if (maxLevel <= passive.MinLevel)
            {
                return passive.MinLevel;
            }

            var level3Chance = synthesisLevel switch
            {
                1 => 0,
                2 => 5,
                3 => 15,
                4 => 30,
                _ => 45
            };
            var level2Chance = synthesisLevel switch
            {
                1 => 20,
                2 => 35,
                3 => 50,
                4 => 60,
                _ => 70
            };

            var roll = random.Next(100);
            if (maxLevel >= 3 && roll < level3Chance)
            {
                return Math.Max(passive.MinLevel, 3);
            }

            if (maxLevel >= 2 && roll < level2Chance)
            {
                return Math.Max(passive.MinLevel, 2);
            }

            return passive.MinLevel;
        }

        private static void AddUniqueStatCandidate(List<EquipmentModifierType> candidates, EquipmentModifierType modifierType)
        {
            if (!IsRandomStatModifier(modifierType) || candidates.Contains(modifierType))
            {
                return;
            }

            candidates.Add(modifierType);
        }

        private static bool IsRandomStatModifier(EquipmentModifierType modifierType)
        {
            return modifierType == EquipmentModifierType.Attack
                || modifierType == EquipmentModifierType.Magic
                || modifierType == EquipmentModifierType.Defense
                || modifierType == EquipmentModifierType.Speed
                || modifierType == EquipmentModifierType.CriticalRate;
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

        private static SynthesisLevelUpQuote LevelUpFailure(
            SynthesisLevelUpFailureReason reason,
            int currentLevel = 0,
            int targetLevel = 0,
            int moneyCost = 0,
            IReadOnlyList<SynthesisMaterialShortage> materialShortages = null,
            SynthesisLevelUpRequirementData requirement = null)
        {
            return new SynthesisLevelUpQuote(
                false,
                reason,
                currentLevel,
                targetLevel,
                moneyCost,
                materialShortages,
                requirement);
        }

        private static SynthesisLevelUpQuote WithLevelUpFailure(
            SynthesisLevelUpQuote quote,
            SynthesisLevelUpFailureReason reason)
        {
            return new SynthesisLevelUpQuote(
                false,
                reason,
                quote.CurrentLevel,
                quote.TargetLevel,
                quote.MoneyCost,
                quote.MaterialShortages,
                quote.Requirement);
        }

        private SynthesisLevelUpRequirement GetLevelUpRequirement(int currentLevel)
        {
            if (levelUpRequirementDatabase != null
                && levelUpRequirementDatabase.TryGetByCurrentLevel(currentLevel, out var requirementData)
                && requirementData != null)
            {
                return new SynthesisLevelUpRequirement(
                    requirementData.CurrentLevel,
                    requirementData.TargetLevel,
                    requirementData.MoneyCost,
                    GetRequiredMaterialCounts(requirementData.MaterialCosts),
                    requirementData);
            }

            return currentLevel switch
            {
                1 => new SynthesisLevelUpRequirement(
                    1,
                    2,
                    100,
                    new Dictionary<string, int>
                    {
                        { "mat_iron_ore", 3 },
                        { "mat_sturdy_wood", 2 },
                        { "mat_beast_hide", 2 }
                    }),
                2 => new SynthesisLevelUpRequirement(
                    2,
                    3,
                    300,
                    new Dictionary<string, int>
                    {
                        { "mat_steel_ore", 3 },
                        { "mat_hard_wood", 2 },
                        { "mat_magic_shard", 3 },
                        { "mat_forest_core", 1 }
                    }),
                3 => new SynthesisLevelUpRequirement(
                    3,
                    4,
                    700,
                    new Dictionary<string, int>
                    {
                        { "mat_mithril_ore", 3 },
                        { "mat_demon_hide", 2 },
                        { "mat_magic_stone", 3 },
                        { "mat_ruin_gear", 1 }
                    }),
                4 => new SynthesisLevelUpRequirement(
                    4,
                    5,
                    1500,
                    new Dictionary<string, int>
                    {
                        { "mat_star_silver_ore", 3 },
                        { "mat_ancient_wood", 2 },
                        { "mat_great_magic_stone", 3 },
                        { "mat_ancient_dragon_crystal", 1 }
                    }),
                _ => new SynthesisLevelUpRequirement(currentLevel, currentLevel, 0, new Dictionary<string, int>())
            };
        }

        private readonly struct SynthesisLevelUpRequirement
        {
            public SynthesisLevelUpRequirement(
                int currentLevel,
                int targetLevel,
                int moneyCost,
                IReadOnlyDictionary<string, int> materialCosts,
                SynthesisLevelUpRequirementData data = null)
            {
                CurrentLevel = Math.Max(0, currentLevel);
                TargetLevel = Math.Max(0, targetLevel);
                MoneyCost = Math.Max(0, moneyCost);
                MaterialCosts = materialCosts ?? new Dictionary<string, int>();
                Data = data;
            }

            public int CurrentLevel { get; }
            public int TargetLevel { get; }
            public int MoneyCost { get; }
            public IReadOnlyDictionary<string, int> MaterialCosts { get; }
            public SynthesisLevelUpRequirementData Data { get; }
        }

    }
}
