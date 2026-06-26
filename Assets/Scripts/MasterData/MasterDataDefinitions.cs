using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Skill", fileName = "SkillData")]
    public sealed class SkillData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;

        public string SkillId => Id;
        public Sprite IconSprite => iconSprite;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Item", fileName = "ItemData")]
    public sealed class ItemData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;

        public string ItemId => Id;
        public Sprite IconSprite => iconSprite;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Equipment", fileName = "EquipmentData")]
    public sealed class EquipmentData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;

        public string EquipmentId => Id;
        public Sprite IconSprite => iconSprite;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Enemy", fileName = "EnemyData")]
    public sealed class EnemyData : MasterDataAsset
    {
        [SerializeField] private int level = 1;
        [SerializeField] private BattleStats stats = new();
        [SerializeField] private List<string> skillIds = new();
        [SerializeField] private List<EnemyAction> actions = new();
        [Min(0)]
        [SerializeField] private int exp;
        [Min(0)]
        [SerializeField] private int money;
        [SerializeField] private List<EnemyDrop> drops = new();
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Vector2 displayScale = Vector2.one;
        [SerializeField] private Vector2 displayOffset;
        [SerializeField] private string defaultAttackSkillId = string.Empty;

        public string EnemyId => Id;
        public int Level => level;
        public BattleStats Stats => stats;
        public IReadOnlyList<string> SkillIds => skillIds;
        public IReadOnlyList<EnemyAction> Actions => actions;
        public int Exp => exp;
        public int Money => money;
        public IReadOnlyList<EnemyDrop> Drops => drops;
        public Sprite EnemySprite => enemySprite;
        public Vector2 DisplayScale => displayScale;
        public Vector2 DisplayOffset => displayOffset;
        public string DefaultAttackSkillId => defaultAttackSkillId;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Battle", fileName = "BattleData")]
    public sealed class BattleData : MasterDataAsset
    {
        [SerializeField] private List<BattleEnemyEntry> enemyEntries = new();
        [SerializeField] private Sprite battleBackgroundSprite;
        [SerializeField] private string battleBgmId = string.Empty;
        [SerializeField] private bool canEscape = true;
        [SerializeField] private bool escapeCountsAsClear = true;
        [SerializeField] private bool escapeSuccessItemAllowed = true;
        [SerializeField] private bool isBoss;
        [SerializeField] private bool gameOverOnDefeat;
        [SerializeField] private DefeatResultType defeatResultType;
        [SerializeField] private string victoryEventId = string.Empty;
        [SerializeField] private string defeatEventId = string.Empty;

        public string BattleId => Id;
        public IReadOnlyList<BattleEnemyEntry> EnemyEntries => enemyEntries;
        public Sprite BattleBackgroundSprite => battleBackgroundSprite;
        public string BattleBgmId => battleBgmId;
        public bool CanEscape => canEscape;
        public bool EscapeCountsAsClear => escapeCountsAsClear;
        public bool EscapeSuccessItemAllowed => escapeSuccessItemAllowed;
        public bool IsBoss => isBoss;
        public bool GameOverOnDefeat => gameOverOnDefeat;
        public DefeatResultType DefeatResultType => defeatResultType;
        public string VictoryEventId => victoryEventId;
        public string DefeatEventId => defeatEventId;
    }

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

    [CreateAssetMenu(menuName = "RPG/Master Data/Event", fileName = "EventData")]
    public sealed class EventData : MasterDataAsset
    {
        [SerializeField] private EventType eventType;
        [SerializeField] private ConditionGroup conditions = new();
        [SerializeField] private EventPriorityCategory priorityCategory = EventPriorityCategory.Normal;
        [Min(0)]
        [SerializeField] private int weight = 1;
        [SerializeField] private List<EventStep> steps = new();
        [SerializeField] private List<EventEffect> effects = new();
        [SerializeField] private string nextEventId = string.Empty;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private string bgmId = string.Empty;

        public string EventId => Id;
        public EventType EventType => eventType;
        public ConditionGroup Conditions => conditions;
        public EventPriorityCategory PriorityCategory => priorityCategory;
        public int Weight => weight;
        public IReadOnlyList<EventStep> Steps => steps;
        public IReadOnlyList<EventEffect> Effects => effects;
        public string NextEventId => nextEventId;
        public Sprite BackgroundSprite => backgroundSprite;
        public string BgmId => bgmId;
    }

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

    [CreateAssetMenu(menuName = "RPG/Master Data/Character", fileName = "CharacterData")]
    public sealed class CharacterData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private BattleStats baseStats = new();

        public string CharacterId => Id;
        public Sprite IconSprite => iconSprite;
        public BattleStats BaseStats => baseStats;
    }

    [CreateAssetMenu(menuName = "RPG/Master Data/Recipe", fileName = "RecipeData")]
    public sealed class RecipeData : MasterDataAsset
    {
        [SerializeField] private string resultEquipmentId = string.Empty;
        [SerializeField] private List<RewardEntry> costs = new();

        public string RecipeId => Id;
        public string ResultEquipmentId => resultEquipmentId;
        public IReadOnlyList<RewardEntry> Costs => costs;
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
        SetSynthesisLevel,
        UnlockRecipe,
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
