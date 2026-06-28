using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("availablePhase")]
        [Range(1, 5)]
        [SerializeField] private int requiredSynthesisLevel = 1;
        [SerializeField] private SynthesisProductDataType productType;
        [SerializeField] private ItemData productItem;
        [SerializeField] private EquipmentData productEquipment;
        [Min(0)]
        [SerializeField] private int moneyCost;
        [SerializeField] private List<SynthesisMaterialCostData> materialCosts = new();
        [SerializeField] private int sortOrder;

        public string RecipeId => Id;
        public int RequiredSynthesisLevel => requiredSynthesisLevel;
        public SynthesisProductDataType ProductType => productType;
        public ItemData ProductItem => productItem;
        public EquipmentData ProductEquipment => productEquipment;
        public string ProductId => productType == SynthesisProductDataType.Equipment
            ? (productEquipment != null ? productEquipment.EquipmentId : string.Empty)
            : (productItem != null ? productItem.ItemId : string.Empty);
        public int ResultCount => 1;
        public int MoneyCost => moneyCost;
        public IReadOnlyList<SynthesisMaterialCostData> MaterialCosts => materialCosts;
        public int SortOrder => sortOrder;
    }
}
