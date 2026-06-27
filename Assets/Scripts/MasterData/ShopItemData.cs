using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Shop Item", fileName = "ShopItemData")]
    public sealed class ShopItemData : MasterDataAsset
    {
        [Range(1, 4)]
        [SerializeField] private int availablePhase = 1;
        [SerializeField] private ShopProductDataType productType;
        [SerializeField] private string productId = string.Empty;
        [SerializeField] private ShopStockDataType stockType;
        [Min(0)]
        [SerializeField] private int stockCount;
        [SerializeField] private int sortOrder;

        public string ShopItemId => Id;
        public int AvailablePhase => availablePhase;
        public ShopProductDataType ProductType => productType;
        public string ProductId => productId;
        public ShopStockDataType StockType => stockType;
        public int StockCount => stockCount;
        public int SortOrder => sortOrder;
    }
}
