using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [Serializable]
    public sealed class SynthesisMaterialCostData
    {
        [SerializeField] private ItemData item;
        [Min(1)]
        [SerializeField] private int count = 1;

        public ItemData Item => item;
        public string ItemId => item != null ? item.ItemId : string.Empty;
        public int Count => count;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Synthesis Recipe", fileName = "SynthesisRecipeData")]
    public sealed class SynthesisRecipeData : MasterDataAsset
    {
        [Range(1, 4)]
        [SerializeField] private int availablePhase = 1;
        [SerializeField] private SynthesisProductDataType productType;
        [SerializeField] private ItemData productItem;
        [SerializeField] private EquipmentData productEquipment;
        [Min(1)]
        [SerializeField] private int resultCount = 1;
        [Min(0)]
        [SerializeField] private int moneyCost;
        [SerializeField] private List<SynthesisMaterialCostData> materialCosts = new();
        [SerializeField] private int sortOrder;

        public string RecipeId => Id;
        public int AvailablePhase => availablePhase;
        public SynthesisProductDataType ProductType => productType;
        public ItemData ProductItem => productItem;
        public EquipmentData ProductEquipment => productEquipment;
        public string ProductId => productType == SynthesisProductDataType.Equipment
            ? (productEquipment != null ? productEquipment.EquipmentId : string.Empty)
            : (productItem != null ? productItem.ItemId : string.Empty);
        public int ResultCount => productType == SynthesisProductDataType.Equipment ? 1 : resultCount;
        public int MoneyCost => moneyCost;
        public IReadOnlyList<SynthesisMaterialCostData> MaterialCosts => materialCosts;
        public int SortOrder => sortOrder;
    }
}
