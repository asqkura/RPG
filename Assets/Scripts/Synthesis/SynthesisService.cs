using System;
using System.Collections.Generic;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Shop;
using UnityEngine;

namespace RPG.Synthesis
{
    public enum SynthesisFailureReason
    {
        None,
        InvalidRequest,
        RecipeNotFound,
        ResultNotFound,
        RecipeLocked,
        NotEnoughMaterial,
        NotEnoughMoney,
        InventoryFull
    }

    public readonly struct SynthesisQuote
    {
        public SynthesisQuote(
            bool canSynthesize,
            SynthesisFailureReason failureReason,
            RecipeData recipe,
            string resultId,
            RecipeResultDataType resultType,
            int cost,
            bool hasResultRarity = false,
            EquipmentRarity resultRarity = default,
            IReadOnlyList<EquipmentModifierSaveData> resultModifiers = null,
            string resultRandomSkillId = "")
        {
            CanSynthesize = canSynthesize;
            FailureReason = failureReason;
            Recipe = recipe;
            ResultId = resultId ?? string.Empty;
            ResultType = resultType;
            Cost = Math.Max(0, cost);
            HasResultRarity = hasResultRarity;
            ResultRarity = resultRarity;
            ResultModifiers = resultModifiers ?? Array.Empty<EquipmentModifierSaveData>();
            ResultRandomSkillId = resultRandomSkillId ?? string.Empty;
        }

        public bool CanSynthesize { get; }
        public SynthesisFailureReason FailureReason { get; }
        public RecipeData Recipe { get; }
        public string ResultId { get; }
        public RecipeResultDataType ResultType { get; }
        public int Cost { get; }
        public bool HasResultRarity { get; }
        public EquipmentRarity ResultRarity { get; }
        public IReadOnlyList<EquipmentModifierSaveData> ResultModifiers { get; }
        public string ResultRandomSkillId { get; }
    }

    public sealed class SynthesisService
    {
        private static readonly EquipmentModifierType[] RandomModifierTypes =
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

        private readonly RecipeDatabase recipeDatabase;
        private readonly ItemDatabase itemDatabase;
        private readonly EquipmentDatabase equipmentDatabase;
        private readonly Func<string> createEquipmentInstanceId;

        public SynthesisService(
            RecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            Func<string> createEquipmentInstanceId = null)
        {
            this.recipeDatabase = recipeDatabase;
            this.itemDatabase = itemDatabase;
            this.equipmentDatabase = equipmentDatabase;
            this.createEquipmentInstanceId = createEquipmentInstanceId ?? CreateDefaultEquipmentInstanceId;
        }

        public bool IsRecipeVisible(RunSaveData saveData, RecipeData recipe)
        {
            return saveData != null
                && recipe != null
                && recipe.RequiredSynthesisLevel <= saveData.SynthesisLevel;
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

            return GetQuote(saveData, recipe);
        }

        public SynthesisQuote GetQuote(RunSaveData saveData, RecipeData recipe)
        {
            if (saveData == null || recipe == null)
            {
                return Failure(SynthesisFailureReason.InvalidRequest);
            }

            if (!IsRecipeVisible(saveData, recipe))
            {
                return Failure(SynthesisFailureReason.RecipeLocked, recipe);
            }

            if (!ResultExists(recipe))
            {
                return Failure(SynthesisFailureReason.ResultNotFound, recipe);
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient == null || saveData.GetMaterialCount(ingredient.ItemId) < ingredient.Count)
                {
                    return Failure(SynthesisFailureReason.NotEnoughMaterial, recipe);
                }
            }

            if (saveData.Money < recipe.Cost)
            {
                return Failure(SynthesisFailureReason.NotEnoughMoney, recipe);
            }

            if (WouldExceedConsumableLimit(saveData, recipe))
            {
                return Failure(SynthesisFailureReason.InventoryFull, recipe);
            }

            return Success(recipe);
        }

        public SynthesisQuote TrySynthesize(RunSaveData saveData, string recipeId)
        {
            var quote = GetQuote(saveData, recipeId);
            if (!quote.CanSynthesize)
            {
                return quote;
            }

            foreach (var ingredient in quote.Recipe.Ingredients)
            {
                if (!saveData.TryConsumeMaterial(ingredient.ItemId, ingredient.Count))
                {
                    return Failure(SynthesisFailureReason.NotEnoughMaterial, quote.Recipe);
                }
            }

            if (!saveData.TrySpendMoney(quote.Cost))
            {
                return Failure(SynthesisFailureReason.NotEnoughMoney, quote.Recipe);
            }

            var resultEquipment = AddResult(saveData, quote.Recipe);
            return quote.Recipe.ResultType == RecipeResultDataType.Equipment
                ? Success(
                    quote.Recipe,
                    true,
                    resultEquipment.Rarity,
                    resultEquipment.RandomModifiers,
                    resultEquipment.RandomSkillId)
                : Success(quote.Recipe);
        }

        private bool ResultExists(RecipeData recipe)
        {
            if (recipe.ResultType == RecipeResultDataType.Item)
            {
                return itemDatabase != null
                    && itemDatabase.TryGetById(recipe.ResultItemId, out var item)
                    && item != null;
            }

            return equipmentDatabase != null
                && equipmentDatabase.TryGetById(recipe.ResultItemId, out var equipment)
                && equipment != null;
        }

        private bool WouldExceedConsumableLimit(RunSaveData saveData, RecipeData recipe)
        {
            if (recipe.ResultType != RecipeResultDataType.Item
                || itemDatabase == null
                || !itemDatabase.TryGetById(recipe.ResultItemId, out var item)
                || item == null
                || item.ItemType != ItemDataType.Consumable)
            {
                return false;
            }

            var ownedConsumableCount = 0;
            foreach (var stack in saveData.ConsumableItems)
            {
                ownedConsumableCount += stack.Count;
            }

            return ownedConsumableCount + 1 > ShopPurchaseService.MaxConsumableCount;
        }

        private OwnedEquipmentSaveData AddResult(RunSaveData saveData, RecipeData recipe)
        {
            if (recipe.ResultType == RecipeResultDataType.Item)
            {
                var item = itemDatabase.GetById(recipe.ResultItemId);
                if (item.ItemType == ItemDataType.Consumable)
                {
                    saveData.AddConsumable(item.ItemId, 1);
                    return null;
                }

                saveData.AddMaterial(item.ItemId, 1);
                return null;
            }

            var equipment = equipmentDatabase.GetById(recipe.ResultItemId);
            var rarity = RollRarity(saveData.SynthesisLevel);
            var ownedEquipment = new OwnedEquipmentSaveData(
                createEquipmentInstanceId(),
                recipe.ResultItemId,
                rarity);
            AddRandomResults(ownedEquipment, equipment);
            saveData.AddOwnedEquipment(ownedEquipment);
            return ownedEquipment;
        }

        private static void AddRandomResults(
            OwnedEquipmentSaveData ownedEquipment,
            EquipmentData equipment)
        {
            if (ownedEquipment == null || equipment == null)
            {
                return;
            }

            var modifierCount = RollRandomModifierCount(ownedEquipment.Rarity);
            var candidates = equipment.AllowedRandomModifierTypes.Count > 0
                ? equipment.AllowedRandomModifierTypes
                : RandomModifierTypes;

            for (var i = 0; i < modifierCount && candidates.Count > 0; i++)
            {
                var modifierType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                ownedEquipment.AddRandomModifier(new EquipmentModifierSaveData(
                    modifierType,
                    GetDefaultModifierTargetId(modifierType),
                    RollModifierAmount(modifierType)));
            }

            if (equipment.RandomSkillPool.Count > 0 && RollRandomSkill(ownedEquipment.Rarity))
            {
                ownedEquipment.RandomSkillId = equipment.RandomSkillPool[UnityEngine.Random.Range(0, equipment.RandomSkillPool.Count)];
            }
        }

        private static int RollRandomModifierCount(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => UnityEngine.Random.Range(0, 2),
                EquipmentRarity.Rare => 1,
                EquipmentRarity.Epic => UnityEngine.Random.Range(1, 3),
                EquipmentRarity.Legendary => UnityEngine.Random.Range(2, 4),
                _ => 0
            };
        }

        private static bool RollRandomSkill(EquipmentRarity rarity)
        {
            var chance = rarity switch
            {
                EquipmentRarity.Rare => 30,
                EquipmentRarity.Epic => 70,
                EquipmentRarity.Legendary => 100,
                _ => 0
            };
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        private static string GetDefaultModifierTargetId(EquipmentModifierType modifierType)
        {
            return modifierType switch
            {
                EquipmentModifierType.AttributeResistance => "all",
                EquipmentModifierType.StatusResistance => "all",
                EquipmentModifierType.DebuffResistance => "all",
                _ => string.Empty
            };
        }

        private static int RollModifierAmount(EquipmentModifierType modifierType)
        {
            return modifierType switch
            {
                EquipmentModifierType.Hp => UnityEngine.Random.Range(10, 26),
                EquipmentModifierType.AttributeResistance => 10,
                EquipmentModifierType.StatusResistance => 10,
                EquipmentModifierType.DebuffResistance => 10,
                _ => UnityEngine.Random.Range(3, 9)
            };
        }

        private static EquipmentRarity RollRarity(int synthesisLevel)
        {
            var roll = UnityEngine.Random.Range(0, 100);
            return Mathf.Clamp(synthesisLevel, RunSaveData.InitialSynthesisLevel, RunSaveData.MaxSynthesisLevel) switch
            {
                1 => RollByTable(roll, 75, 98, 100),
                2 => RollByTable(roll, 65, 95, 100),
                3 => RollByTable(roll, 55, 90, 99),
                4 => RollByTable(roll, 45, 85, 98),
                _ => RollByTable(roll, 35, 80, 97)
            };
        }

        private static EquipmentRarity RollByTable(int roll, int commonLimit, int rareLimit, int epicLimit)
        {
            if (roll < commonLimit)
            {
                return EquipmentRarity.Common;
            }

            if (roll < rareLimit)
            {
                return EquipmentRarity.Rare;
            }

            return roll < epicLimit ? EquipmentRarity.Epic : EquipmentRarity.Legendary;
        }

        private static SynthesisQuote Success(
            RecipeData recipe,
            bool hasResultRarity = false,
            EquipmentRarity resultRarity = default,
            IReadOnlyList<EquipmentModifierSaveData> resultModifiers = null,
            string resultRandomSkillId = "")
        {
            return new SynthesisQuote(
                true,
                SynthesisFailureReason.None,
                recipe,
                recipe.ResultItemId,
                recipe.ResultType,
                recipe.Cost,
                hasResultRarity,
                resultRarity,
                resultModifiers,
                resultRandomSkillId);
        }

        private static SynthesisQuote Failure(SynthesisFailureReason reason, RecipeData recipe = null)
        {
            return new SynthesisQuote(
                false,
                reason,
                recipe,
                recipe != null ? recipe.ResultItemId : string.Empty,
                recipe != null ? recipe.ResultType : default,
                recipe != null ? recipe.Cost : 0);
        }

        private static string CreateDefaultEquipmentInstanceId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
