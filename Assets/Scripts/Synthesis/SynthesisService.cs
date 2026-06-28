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
        NotAvailableInCurrentPhase,
        ProductNotFound,
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
            IReadOnlyList<SynthesisMaterialShortage> materialShortages)
        {
            CanSynthesize = canSynthesize;
            FailureReason = failureReason;
            Recipe = recipe;
            ProductId = productId ?? string.Empty;
            ProductType = productType;
            ResultCount = Math.Max(0, resultCount);
            MoneyCost = Math.Max(0, moneyCost);
            MaterialShortages = materialShortages ?? Array.Empty<SynthesisMaterialShortage>();
        }

        public bool CanSynthesize { get; }
        public SynthesisFailureReason FailureReason { get; }
        public SynthesisRecipeData Recipe { get; }
        public string ProductId { get; }
        public SynthesisProductDataType ProductType { get; }
        public int ResultCount { get; }
        public int MoneyCost { get; }
        public IReadOnlyList<SynthesisMaterialShortage> MaterialShortages { get; }
    }

    public sealed class SynthesisService
    {
        private readonly SynthesisRecipeDatabase recipeDatabase;
        private readonly ItemDatabase itemDatabase;
        private readonly EquipmentDatabase equipmentDatabase;

        public SynthesisService(
            SynthesisRecipeDatabase recipeDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase)
        {
            this.recipeDatabase = recipeDatabase;
            this.itemDatabase = itemDatabase;
            this.equipmentDatabase = equipmentDatabase;
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

            if (recipe.AvailablePhase > (int)saveData.CurrentPhase)
            {
                return Failure(SynthesisFailureReason.NotAvailableInCurrentPhase, recipe);
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

            foreach (var cost in quote.Recipe.MaterialCosts)
            {
                if (cost == null)
                {
                    continue;
                }

                if (!saveData.TryConsumeMaterial(cost.ItemId, cost.Count))
                {
                    saveData.AddMoney(quote.MoneyCost);
                    return WithFailure(quote, SynthesisFailureReason.NotEnoughMaterials);
                }
            }

            AddProduct(saveData, quote);
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
                return itemDatabase != null
                    && itemDatabase.TryGetById(recipe.ProductId, out var item)
                    && item != null
                    && item.ItemType == ItemDataType.Consumable;
            }

            return equipmentDatabase != null
                && equipmentDatabase.TryGetById(recipe.ProductId, out var equipment)
                && equipment != null;
        }

        private static List<SynthesisMaterialShortage> GetMaterialShortages(RunSaveData saveData, SynthesisRecipeData recipe)
        {
            var shortages = new List<SynthesisMaterialShortage>();
            foreach (var cost in recipe.MaterialCosts)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                var ownedCount = saveData.GetMaterialCount(cost.ItemId);
                if (ownedCount < cost.Count)
                {
                    shortages.Add(new SynthesisMaterialShortage(cost.ItemId, cost.Count, ownedCount));
                }
            }

            return shortages;
        }

        private static void AddProduct(RunSaveData saveData, SynthesisQuote quote)
        {
            if (quote.ProductType == SynthesisProductDataType.Consumable)
            {
                saveData.AddConsumable(quote.ProductId, quote.ResultCount);
                return;
            }

            saveData.AddOwnedEquipment(new OwnedEquipmentSaveData(
                CreateOwnedEquipmentInstanceId(quote.ProductId),
                quote.ProductId,
                EquipmentRarity.Common));
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
                quote.MaterialShortages);
        }
    }
}
