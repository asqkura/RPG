using System;
using RPG.MasterData;
using RPG.SaveData;

namespace RPG.Shop
{
    public enum ShopSellFailureReason
    {
        None,
        InvalidRequest,
        ProductNotFound,
        NotOwned,
        Unsellable
    }

    public readonly struct ShopSellQuote
    {
        public ShopSellQuote(
            bool canSell,
            ShopSellFailureReason failureReason,
            string productId,
            string ownedEquipmentInstanceId,
            ShopProductDataType productType,
            int unitPrice,
            int quantity,
            int totalPrice)
        {
            CanSell = canSell;
            FailureReason = failureReason;
            ProductId = productId ?? string.Empty;
            OwnedEquipmentInstanceId = ownedEquipmentInstanceId ?? string.Empty;
            ProductType = productType;
            UnitPrice = unitPrice;
            Quantity = quantity;
            TotalPrice = totalPrice;
        }

        public bool CanSell { get; }
        public ShopSellFailureReason FailureReason { get; }
        public string ProductId { get; }
        public string OwnedEquipmentInstanceId { get; }
        public ShopProductDataType ProductType { get; }
        public int UnitPrice { get; }
        public int Quantity { get; }
        public int TotalPrice { get; }
    }

    public sealed class ShopSellService
    {
        private readonly ItemDatabase itemDatabase;
        private readonly EquipmentDatabase equipmentDatabase;

        public ShopSellService(ItemDatabase itemDatabase, EquipmentDatabase equipmentDatabase)
        {
            this.itemDatabase = itemDatabase;
            this.equipmentDatabase = equipmentDatabase;
        }

        public ShopSellQuote GetItemQuote(RunSaveData saveData, string itemId, int quantity = 1)
        {
            if (saveData == null || itemDatabase == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return Failure(ShopSellFailureReason.InvalidRequest, itemId, string.Empty, ShopProductDataType.Item, quantity);
            }

            if (!itemDatabase.TryGetById(itemId, out var item) || item == null)
            {
                return Failure(ShopSellFailureReason.ProductNotFound, itemId, string.Empty, ShopProductDataType.Item, quantity);
            }

            if (item.Unsellable)
            {
                return Failure(ShopSellFailureReason.Unsellable, itemId, string.Empty, ShopProductDataType.Item, quantity);
            }

            var ownedCount = item.ItemType == ItemDataType.Consumable
                ? saveData.GetConsumableCount(item.ItemId)
                : saveData.GetMaterialCount(item.ItemId);
            if (ownedCount < quantity)
            {
                return Failure(ShopSellFailureReason.NotOwned, itemId, string.Empty, ShopProductDataType.Item, quantity);
            }

            var unitPrice = CalculateSellPrice(item.Price);
            if (!TryCalculateTotalPrice(unitPrice, quantity, out _))
            {
                return Failure(ShopSellFailureReason.InvalidRequest, itemId, string.Empty, ShopProductDataType.Item, quantity);
            }

            return Success(item.ItemId, string.Empty, ShopProductDataType.Item, unitPrice, quantity);
        }

        public ShopSellQuote GetEquipmentQuote(RunSaveData saveData, string ownedEquipmentInstanceId)
        {
            if (saveData == null
                || equipmentDatabase == null
                || string.IsNullOrWhiteSpace(ownedEquipmentInstanceId))
            {
                return Failure(ShopSellFailureReason.InvalidRequest, string.Empty, ownedEquipmentInstanceId, ShopProductDataType.Equipment, 1);
            }

            var ownedEquipment = FindOwnedEquipment(saveData, ownedEquipmentInstanceId);
            if (ownedEquipment == null)
            {
                return Failure(ShopSellFailureReason.NotOwned, string.Empty, ownedEquipmentInstanceId, ShopProductDataType.Equipment, 1);
            }

            if (!equipmentDatabase.TryGetById(ownedEquipment.EquipmentId, out var equipment) || equipment == null)
            {
                return Failure(ShopSellFailureReason.ProductNotFound, ownedEquipment.EquipmentId, ownedEquipmentInstanceId, ShopProductDataType.Equipment, 1);
            }

            if (equipment.Unsellable)
            {
                return Failure(ShopSellFailureReason.Unsellable, equipment.EquipmentId, ownedEquipmentInstanceId, ShopProductDataType.Equipment, 1);
            }

            return Success(equipment.EquipmentId, ownedEquipmentInstanceId, ShopProductDataType.Equipment, CalculateSellPrice(equipment.Price), 1);
        }

        public ShopSellQuote TrySellItem(RunSaveData saveData, string itemId, int quantity = 1)
        {
            var quote = GetItemQuote(saveData, itemId, quantity);
            if (!quote.CanSell)
            {
                return quote;
            }

            var item = itemDatabase.GetById(itemId);
            var removed = item.ItemType == ItemDataType.Consumable
                ? saveData.TryConsumeConsumable(item.ItemId, quantity)
                : saveData.TryConsumeMaterial(item.ItemId, quantity);
            if (!removed)
            {
                return WithFailure(quote, ShopSellFailureReason.NotOwned);
            }

            saveData.AddMoney(quote.TotalPrice);
            return quote;
        }

        public ShopSellQuote TrySellEquipment(RunSaveData saveData, string ownedEquipmentInstanceId)
        {
            var quote = GetEquipmentQuote(saveData, ownedEquipmentInstanceId);
            if (!quote.CanSell)
            {
                return quote;
            }

            if (!saveData.TryRemoveOwnedEquipment(ownedEquipmentInstanceId))
            {
                return WithFailure(quote, ShopSellFailureReason.NotOwned);
            }

            saveData.AddMoney(quote.TotalPrice);
            return quote;
        }

        public static int CalculateSellPrice(int basePrice)
        {
            if (basePrice <= 0)
            {
                return 0;
            }

            return Math.Max(1, basePrice / 2);
        }

        private static OwnedEquipmentSaveData FindOwnedEquipment(RunSaveData saveData, string ownedEquipmentInstanceId)
        {
            foreach (var equipment in saveData.OwnedEquipments)
            {
                if (equipment.OwnedEquipmentInstanceId == ownedEquipmentInstanceId)
                {
                    return equipment;
                }
            }

            return null;
        }

        private static ShopSellQuote Success(
            string productId,
            string ownedEquipmentInstanceId,
            ShopProductDataType productType,
            int unitPrice,
            int quantity)
        {
            var totalPrice = TryCalculateTotalPrice(unitPrice, quantity, out var calculatedTotalPrice)
                ? calculatedTotalPrice
                : 0;

            return new ShopSellQuote(
                true,
                ShopSellFailureReason.None,
                productId,
                ownedEquipmentInstanceId,
                productType,
                unitPrice,
                quantity,
                totalPrice);
        }

        private static ShopSellQuote Failure(
            ShopSellFailureReason reason,
            string productId,
            string ownedEquipmentInstanceId,
            ShopProductDataType productType,
            int quantity)
        {
            return new ShopSellQuote(false, reason, productId, ownedEquipmentInstanceId, productType, 0, quantity, 0);
        }

        private static ShopSellQuote WithFailure(ShopSellQuote quote, ShopSellFailureReason reason)
        {
            return new ShopSellQuote(
                false,
                reason,
                quote.ProductId,
                quote.OwnedEquipmentInstanceId,
                quote.ProductType,
                quote.UnitPrice,
                quote.Quantity,
                quote.TotalPrice);
        }

        private static bool TryCalculateTotalPrice(int unitPrice, int quantity, out int totalPrice)
        {
            totalPrice = 0;
            if (unitPrice < 0 || quantity <= 0)
            {
                return false;
            }

            var total = (long)unitPrice * quantity;
            if (total > int.MaxValue)
            {
                return false;
            }

            totalPrice = (int)total;
            return true;
        }
    }
}
