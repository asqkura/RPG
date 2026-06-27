using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Recipe", fileName = "RecipeData")]
    public sealed class RecipeData : MasterDataAsset
    {
        [SerializeField] private string resultEquipmentId = string.Empty;
        [SerializeField] private List<RewardEntry> costs = new();

        public string RecipeId => Id;
        public string ResultEquipmentId => resultEquipmentId;
        public IReadOnlyList<RewardEntry> Costs => costs;
    }
}
