using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    public enum ItemDataType
    {
        Consumable,
        Material
    }

    public enum ItemEffectDataType
    {
        RecoverHp,
        RecoverSp,
        CurePoison,
        CureStatus,
        Escape
    }

    public enum EquipmentDataType
    {
        Weapon,
        Armor,
        Accessory
    }

    public enum WeaponDataType
    {
        None,
        Sword,
        Dagger,
        Axe,
        Spear,
        Bow,
        Staff
    }

    public enum ShopProductDataType
    {
        Item,
        Equipment
    }

    public enum ShopStockDataType
    {
        Unlimited,
        Limited
    }

    [Serializable]
    public sealed class ItemEffectData
    {
        [SerializeField] private ItemEffectDataType effectType;
        [Min(0)]
        [SerializeField] private int amount;

        public ItemEffectDataType EffectType => effectType;
        public int Amount => amount;
    }

    [Serializable]
    public sealed class BattleStats
    {
        [Min(0)] [SerializeField] private int hp;
        [Min(0)] [SerializeField] private int sp;
        [SerializeField] private int attack;
        [SerializeField] private int magic;
        [SerializeField] private int defense;
        [SerializeField] private int speed;
        [Range(0f, 1f)] [SerializeField] private float criticalRate;

        public int Hp => hp;
        public int Sp => sp;
        public int Attack => attack;
        public int Magic => magic;
        public int Defense => defense;
        public int Speed => speed;
        public float CriticalRate => criticalRate;
    }

    [Serializable]
    public sealed class EquipmentBaseTraitData
    {
        [UnityEngine.Serialization.FormerlySerializedAs("modifierType")]
        [SerializeField] private EquipmentBaseTraitType traitType;

        public EquipmentBaseTraitType TraitType => traitType;
    }

    public enum EquipmentBaseTraitType
    {
        AttributeResistance,
        StatusResistance,
        DebuffResistance
    }

    public enum EnemyActionSelectionType
    {
        Priority,
        Weighted
    }

    [Serializable]
    public sealed class EnemyAction
    {
        [SerializeField] private string skillId = string.Empty;
        [SerializeField] private EnemyActionSelectionType selectionType = EnemyActionSelectionType.Weighted;
        [SerializeField] private int priority;
        [Min(0)]
        [SerializeField] private int weight = 1;
        [SerializeField] private ConditionGroup conditions = new();

        public string SkillId => skillId;
        public EnemyActionSelectionType SelectionType => selectionType;
        public int Priority => priority;
        public int Weight => weight;
        public ConditionGroup Conditions => conditions;
    }

    [Serializable]
    public sealed class EnemyDrop
    {
        [SerializeField] private string itemId = string.Empty;
        [Range(0f, 1f)]
        [SerializeField] private float dropRate = 1f;
        [Min(0)]
        [SerializeField] private int minCount = 1;
        [Min(0)]
        [SerializeField] private int maxCount = 1;

        public string ItemId => itemId;
        public float DropRate => dropRate;
        public int MinCount => minCount;
        public int MaxCount => maxCount;
    }

    public enum DefeatResultType
    {
        None,
        QuestFailure,
        EventFailure,
        Ending
    }

    [Serializable]
    public sealed class BattleEnemyEntry
    {
        [SerializeField] private string enemyId = string.Empty;
        [Min(1)]
        [SerializeField] private int count = 1;
        [SerializeField] private Vector2 position;

        public string EnemyId => enemyId;
        public int Count => count;
        public Vector2 Position => position;
    }

    public enum QuestType
    {
        Main,
        Sub
    }

    public enum QuestRetryPolicy
    {
        Repeatable,
        UntilClear,
        Once
    }

    public enum QuestNodeType
    {
        Start,
        Event,
        Battle,
        Treasure,
        End
    }

    public enum LockedDisplayType
    {
        Hidden,
        Disabled
    }

    [Serializable]
    public sealed class QuestNodeData
    {
        [SerializeField] private string nodeId = string.Empty;
        [SerializeField] private QuestNodeType nodeType;
        [SerializeField] private string title = string.Empty;
        [TextArea]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private Vector2 position;
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private string battleId = string.Empty;
        [SerializeField] private List<RewardEntry> treasureRewards = new();
        [SerializeField] private List<QuestNodeConnection> connections = new();

        public string NodeId => nodeId;
        public QuestNodeType NodeType => nodeType;
        public string Title => title;
        public string Description => description;
        public Vector2 Position => position;
        public string EventId => eventId;
        public string BattleId => battleId;
        public IReadOnlyList<RewardEntry> TreasureRewards => treasureRewards;
        public IReadOnlyList<QuestNodeConnection> Connections => connections;
    }

    [Serializable]
    public sealed class QuestNodeConnection
    {
        [SerializeField] private string toNodeId = string.Empty;
        [SerializeField] private string label = string.Empty;
        [SerializeField] private ConditionGroup conditions = new();
        [SerializeField] private LockedDisplayType lockedDisplay;
        [SerializeField] private string lockedReason = string.Empty;
        [SerializeField] private int sortOrder;

        public string ToNodeId => toNodeId;
        public string Label => label;
        public ConditionGroup Conditions => conditions;
        public LockedDisplayType LockedDisplay => lockedDisplay;
        public string LockedReason => lockedReason;
        public int SortOrder => sortOrder;
    }

    public enum EventType
    {
        Main,
        Wander,
        Character,
        Quest,
        Battle,
        System
    }

    public enum EventPriorityCategory
    {
        Forced,
        Important,
        Normal,
        Fallback
    }

    public enum EventStepType
    {
        Message,
        Narration,
        Choice,
        Effect,
        ChangeBackground,
        ChangeBgm,
        SoundEffect
    }

    [Serializable]
    public sealed class EventStep
    {
        [SerializeField] private EventStepType stepType;
        [SerializeField] private string speakerName = string.Empty;
        [TextArea]
        [SerializeField] private string text = string.Empty;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private string bgmId = string.Empty;
        [SerializeField] private string soundEffectId = string.Empty;
        [SerializeField] private List<EventChoice> choices = new();
        [SerializeField] private List<EventEffect> effects = new();

        public EventStepType StepType => stepType;
        public string SpeakerName => speakerName;
        public string Text => text;
        public Sprite CharacterSprite => characterSprite;
        public Sprite BackgroundSprite => backgroundSprite;
        public string BgmId => bgmId;
        public string SoundEffectId => soundEffectId;
        public IReadOnlyList<EventChoice> Choices => choices;
        public IReadOnlyList<EventEffect> Effects => effects;
    }

    [Serializable]
    public sealed class EventChoice
    {
        [SerializeField] private string choiceId = string.Empty;
        [SerializeField] private string label = string.Empty;
        [SerializeField] private ConditionGroup conditions = new();
        [SerializeField] private List<EventEffect> effects = new();
        [SerializeField] private int nextStepIndex = -1;

        public string ChoiceId => choiceId;
        public string Label => label;
        public ConditionGroup Conditions => conditions;
        public IReadOnlyList<EventEffect> Effects => effects;
        public int NextStepIndex => nextStepIndex;
    }

    public enum EventEffectType
    {
        SetFlag,
        AddVariable,
        SetVariable,
        JoinCharacter,
        LeaveCharacter,
        UnlockQuest,
        UnlockWanderLocation,
        AddItem,
        RemoveItem,
        AddMoney,
        RemoveMoney,
        AddEquipment,
        StartBattle,
        UnlockEpilogue
    }

    [Serializable]
    public sealed class EventEffect
    {
        [SerializeField] private EventEffectType effectType;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string value = string.Empty;
        [SerializeField] private int amount;
        [SerializeField] private ConditionGroup conditions = new();

        public EventEffectType EffectType => effectType;
        public string TargetId => targetId;
        public string Value => value;
        public int Amount => amount;
        public ConditionGroup Conditions => conditions;
    }
}
