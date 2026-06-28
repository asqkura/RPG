using System;
using RPG.MasterData;
using RPG.SaveData;

namespace RPG.Shop
{
    public enum ShopPurchaseFailureReason
    {
        None,
        InvalidRequest,
        ShopItemNotFound,
        ProductNotFound,
        NotAvailableInCurrentPhase,
        SoldOut,
        NotEnoughMoney,
        InventoryFull
    }

    public readonly struct ShopPurchaseQuote
    {
        public ShopPurchaseQuote(
            bool canPurchase,
            ShopPurchaseFailureReason failureReason,
            ShopItemData shopItem,
            string productId,
            ShopProductDataType productType,
            int unitPrice,
            int quantity,
            int totalPrice,
            int remainingStock)
        {
            CanPurchase = canPurchase;
            FailureReason = failureReason;
            ShopItem = shopItem;
            ProductId = productId ?? string.Empty;
            ProductType = productType;
            UnitPrice = unitPrice;
            Quantity = quantity;
            TotalPrice = totalPrice;
            RemainingStock = remainingStock;
        }

        public bool CanPurchase { get; }
        public ShopPurchaseFailureReason FailureReason { get; }
        public ShopItemData ShopItem { get; }
        public string ProductId { get; }
        public ShopProductDataType ProductType { get; }
        public int UnitPrice { get; }
        public int Quantity { get; }
        public int TotalPrice { get; }
        public int RemainingStock { get; }
    }

    public sealed class ShopPurchaseService
    {
        private readonly ShopItemDatabase shopItemDatabase;
        private readonly ItemDatabase itemDatabase;
        private readonly EquipmentDatabase equipmentDatabase;
        private readonly Func<string> createEquipmentInstanceId;

        public ShopPurchaseService(
            ShopItemDatabase shopItemDatabase,
            ItemDatabase itemDatabase,
            EquipmentDatabase equipmentDatabase,
            Func<string> createEquipmentInstanceId = null)
        {
            this.shopItemDatabase = shopItemDatabase;
            this.itemDatabase = itemDatabase;
            this.equipmentDatabase = equipmentDatabase;
            this.createEquipmentInstanceId = createEquipmentInstanceId ?? CreateDefaultEquipmentInstanceId;
        }

        public ShopPurchaseQuote GetQuote(RunSaveData saveData, string shopItemId, int quantity = 1)
        {
            if (saveData == null
                || shopItemDatabase == null
                || string.IsNullOrWhiteSpace(shopItemId)
                || quantity <= 0)
            {
                return Failure(ShopPurchaseFailureReason.InvalidRequest);
            }

            if (!shopItemDatabase.TryGetById(shopItemId, out var shopItem) || shopItem == null)
            {
                return Failure(ShopPurchaseFailureReason.ShopItemNotFound, quantity);
            }

            if (shopItem.AvailablePhase > (int)saveData.CurrentPhase)
            {
                return Failure(ShopPurchaseFailureReason.NotAvailableInCurrentPhase, shopItem, quantity);
            }

            if (!TryGetProductPrice(shopItem, out var price))
            {
                return Failure(ShopPurchaseFailureReason.ProductNotFound, shopItem, quantity);
            }

            if (WouldExceedConsumableLimit(saveData, shopItem, quantity))
            {
                return Failure(ShopPurchaseFailureReason.InventoryFull, shopItem, quantity, price, GetRemainingStock(saveData, shopItem));
            }

            var remainingStock = GetRemainingStock(saveData, shopItem);
            if (shopItem.StockType == ShopStockDataType.Limited && remainingStock < quantity)
            {
                return Failure(ShopPurchaseFailureReason.SoldOut, shopItem, quantity, price, remainingStock);
            }

            if (!TryCalculateTotalPrice(price, quantity, out var totalPrice))
            {
                return Failure(ShopPurchaseFailureReason.InvalidRequest, shopItem, quantity, price, remainingStock);
            }

            if (saveData.Money < totalPrice)
            {
                return Failure(ShopPurchaseFailureReason.NotEnoughMoney, shopItem, quantity, price, remainingStock);
            }

            return new ShopPurchaseQuote(
                true,
                ShopPurchaseFailureReason.None,
                shopItem,
                shopItem.ProductId,
                shopItem.ProductType,
                price,
                quantity,
                totalPrice,
                remainingStock);
        }

        public ShopPurchaseQuote TryPurchase(RunSaveData saveData, string shopItemId, int quantity = 1)
        {
            var quote = GetQuote(saveData, shopItemId, quantity);
            if (!quote.CanPurchase)
            {
                return quote;
            }

            if (!saveData.TrySpendMoney(quote.TotalPrice))
            {
                return WithFailure(quote, ShopPurchaseFailureReason.NotEnoughMoney);
            }

            AddProductToInventory(saveData, quote.ShopItem, quote.Quantity);

            if (quote.ShopItem.StockType == ShopStockDataType.Limited)
            {
                var stock = saveData.GetOrCreateShopStock(quote.ShopItem.ShopItemId, quote.ShopItem.StockCount);
                stock.RemainingCount -= quote.Quantity;
            }

            return new ShopPurchaseQuote(
                true,
                ShopPurchaseFailureReason.None,
                quote.ShopItem,
                quote.ProductId,
                quote.ProductType,
                quote.UnitPrice,
                quote.Quantity,
                quote.TotalPrice,
                quote.ShopItem.StockType == ShopStockDataType.Unlimited
                    ? int.MaxValue
                    : Math.Max(0, quote.RemainingStock - quote.Quantity));
        }

        public int GetRemainingStock(RunSaveData saveData, ShopItemData shopItem)
        {
            if (saveData == null || shopItem == null)
            {
                return 0;
            }

            if (shopItem.StockType == ShopStockDataType.Unlimited)
            {
                return int.MaxValue;
            }

            return saveData.TryGetShopStock(shopItem.ShopItemId, out var stock)
                ? stock.RemainingCount
                : shopItem.StockCount;
        }

        private bool TryGetProductPrice(ShopItemData shopItem, out int price)
        {
            price = 0;

            if (shopItem == null)
            {
                return false;
            }

            if (shopItem.ProductType == ShopProductDataType.Item)
            {
                if (itemDatabase == null || !itemDatabase.TryGetById(shopItem.ProductId, out var item) || item == null)
                {
                    return false;
                }

                price = item.Price;
                return true;
            }

            if (equipmentDatabase == null
                || !equipmentDatabase.TryGetById(shopItem.ProductId, out var equipment)
                || equipment == null)
            {
                return false;
            }

            price = equipment.Price;
            return true;
        }

        private bool WouldExceedConsumableLimit(RunSaveData saveData, ShopItemData shopItem, int quantity)
        {
            if (shopItem == null
                || shopItem.ProductType != ShopProductDataType.Item
                || itemDatabase == null
                || !itemDatabase.TryGetById(shopItem.ProductId, out var item)
                || item == null
                || item.ItemType != ItemDataType.Consumable)
            {
                return false;
            }

            return saveData.GetTotalConsumableCount() > RunSaveData.MaxConsumableCount - quantity;
        }

        private void AddProductToInventory(RunSaveData saveData, ShopItemData shopItem, int quantity)
        {
            if (shopItem.ProductType == ShopProductDataType.Item)
            {
                var item = itemDatabase.GetById(shopItem.ProductId);
                if (item.ItemType == ItemDataType.Consumable)
                {
                    saveData.AddConsumable(item.ItemId, quantity);
                    return;
                }

                saveData.AddMaterial(item.ItemId, quantity);
                return;
            }

            for (var i = 0; i < quantity; i++)
            {
                saveData.AddOwnedEquipment(new OwnedEquipmentSaveData(
                    createEquipmentInstanceId(),
                    shopItem.ProductId,
                    EquipmentRarity.Common));
            }
        }

        private static ShopPurchaseQuote Failure(ShopPurchaseFailureReason reason, int quantity = 0)
        {
            return new ShopPurchaseQuote(false, reason, null, string.Empty, default, 0, quantity, 0, 0);
        }

        private static ShopPurchaseQuote Failure(
            ShopPurchaseFailureReason reason,
            ShopItemData shopItem,
            int quantity,
            int unitPrice = 0,
            int remainingStock = 0)
        {
            var totalPrice = TryCalculateTotalPrice(unitPrice, quantity, out var calculatedTotalPrice)
                ? calculatedTotalPrice
                : 0;

            return new ShopPurchaseQuote(
                false,
                reason,
                shopItem,
                shopItem != null ? shopItem.ProductId : string.Empty,
                shopItem != null ? shopItem.ProductType : default,
                unitPrice,
                quantity,
                totalPrice,
                remainingStock);
        }

        private static ShopPurchaseQuote WithFailure(ShopPurchaseQuote quote, ShopPurchaseFailureReason reason)
        {
            return new ShopPurchaseQuote(
                false,
                reason,
                quote.ShopItem,
                quote.ProductId,
                quote.ProductType,
                quote.UnitPrice,
                quote.Quantity,
                quote.TotalPrice,
                quote.RemainingStock);
        }

        private static string CreateDefaultEquipmentInstanceId()
        {
            return Guid.NewGuid().ToString("N");
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
