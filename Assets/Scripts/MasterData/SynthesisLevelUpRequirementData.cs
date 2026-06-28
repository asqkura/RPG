using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Synthesis Level Up Requirement", fileName = "SynthesisLevelUpRequirementData")]
    public sealed class SynthesisLevelUpRequirementData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [Range(1, 4)]
        [SerializeField] private int currentLevel = 1;
        [Range(2, 5)]
        [SerializeField] private int targetLevel = 2;
        [Min(0)]
        [SerializeField] private int moneyCost;
        [SerializeField] private List<SynthesisMaterialCostData> materialCosts = new();
        [SerializeField] private int sortOrder;

        public string RequirementId => Id;
        public Sprite IconSprite => iconSprite;
        public int CurrentLevel => currentLevel;
        public int TargetLevel => targetLevel;
        public int MoneyCost => moneyCost;
        public IReadOnlyList<SynthesisMaterialCostData> MaterialCosts => materialCosts;
        public int SortOrder => sortOrder;
    }
}
