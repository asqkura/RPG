using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    public enum ConditionType
    {
        Flag,
        Day,
        Phase,
        MainProgress,
        QuestState,
        QuestCleared,
        QuestFailed,
        CharacterJoined,
        CharacterInParty,
        CharacterEventStage,
        ItemCount,
        EquipmentOwned,
        BattleResult,
        EventOccurred
    }

    public enum ConditionOperator
    {
        Equal,
        NotEqual,
        GreaterThanOrEqual,
        LessThanOrEqual,
        GreaterThan,
        LessThan
    }

    public enum ConditionMatchType
    {
        All,
        Any
    }

    public enum RewardType
    {
        Money,
        Item,
        Equipment,
        Exp
    }

    [Serializable]
    public sealed class Condition
    {
        [SerializeField] private ConditionType conditionType;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private ConditionOperator operatorType;
        [SerializeField] private string value = string.Empty;

        public ConditionType ConditionType => conditionType;
        public string TargetId => targetId;
        public ConditionOperator OperatorType => operatorType;
        public string Value => value;
    }

    [Serializable]
    public sealed class ConditionGroup
    {
        [SerializeField] private ConditionMatchType matchType = ConditionMatchType.All;
        [SerializeField] private List<Condition> conditions = new();

        public ConditionMatchType MatchType => matchType;
        public IReadOnlyList<Condition> Conditions => conditions;
    }

    [Serializable]
    public sealed class RewardEntry
    {
        [SerializeField] private RewardType rewardType;
        [SerializeField] private string targetId = string.Empty;
        [Min(0)]
        [SerializeField] private int amount = 1;
        [Range(0f, 1f)]
        [SerializeField] private float dropRate = 1f;

        public RewardType RewardType => rewardType;
        public string TargetId => targetId;
        public int Amount => amount;
        public float DropRate => dropRate;
    }
}
