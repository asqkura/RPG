using System;
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
            int cost)
        {
            CanSynthesize = canSynthesize;
            FailureReason = failureReason;
            Recipe = recipe;
            ResultId = resultId ?? string.Empty;
            ResultType = resultType;
            Cost = Math.Max(0, cost);
        }

        public bool CanSynthesize { get; }
        public SynthesisFailureReason FailureReason { get; }
        public RecipeData Recipe { get; }
        public string ResultId { get; }
        public RecipeResultDataType ResultType { get; }
        public int Cost { get; }
    }

    public sealed class SynthesisService
    {
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

            AddResult(saveData, quote.Recipe);
            return Success(quote.Recipe);
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

        private void AddResult(RunSaveData saveData, RecipeData recipe)
        {
            if (recipe.ResultType == RecipeResultDataType.Item)
            {
                var item = itemDatabase.GetById(recipe.ResultItemId);
                if (item.ItemType == ItemDataType.Consumable)
                {
                    saveData.AddConsumable(item.ItemId, 1);
                    return;
                }

                saveData.AddMaterial(item.ItemId, 1);
                return;
            }

            saveData.AddOwnedEquipment(new OwnedEquipmentSaveData(
                createEquipmentInstanceId(),
                recipe.ResultItemId,
                RollRarity(saveData.SynthesisLevel)));
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

        private static SynthesisQuote Success(RecipeData recipe)
        {
            return new SynthesisQuote(
                true,
                SynthesisFailureReason.None,
                recipe,
                recipe.ResultItemId,
                recipe.ResultType,
                recipe.Cost);
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
