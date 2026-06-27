using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Wander Location", fileName = "WanderLocationData")]
    public sealed class WanderLocationData : MasterDataAsset
    {
        [SerializeField] private ConditionGroup unlockConditions = new();
        [SerializeField] private List<string> eventCandidateIds = new();
        [SerializeField] private string fallbackEventId = string.Empty;
        [SerializeField] private int sortOrder;

        public string LocationId => Id;
        public ConditionGroup UnlockConditions => unlockConditions;
        public IReadOnlyList<string> EventCandidateIds => eventCandidateIds;
        public string FallbackEventId => fallbackEventId;
        public int SortOrder => sortOrder;
    }
}
