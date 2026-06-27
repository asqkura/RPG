using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Quest", fileName = "QuestData")]
    public sealed class QuestData : MasterDataAsset
    {
        [SerializeField] private QuestType questType;
        [SerializeField] private ConditionGroup unlockConditions = new();
        [SerializeField] private QuestRetryPolicy retryPolicy = QuestRetryPolicy.Repeatable;
        [Min(0)]
        [SerializeField] private int difficulty;
        [SerializeField] private int sortOrder;
        [SerializeField] private string startNodeId = string.Empty;
        [SerializeField] private string clearEventId = string.Empty;
        [SerializeField] private string failureEventId = string.Empty;
        [SerializeField] private List<QuestNodeData> nodes = new();

        public string QuestId => Id;
        public QuestType QuestType => questType;
        public ConditionGroup UnlockConditions => unlockConditions;
        public QuestRetryPolicy RetryPolicy => retryPolicy;
        public int Difficulty => difficulty;
        public int SortOrder => sortOrder;
        public string StartNodeId => startNodeId;
        public string ClearEventId => clearEventId;
        public string FailureEventId => failureEventId;
        public IReadOnlyList<QuestNodeData> Nodes => nodes;
    }
}
