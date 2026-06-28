using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.SaveData
{
    public enum GamePhase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Phase4 = 4,
        Final = 5
    }

    public enum QuestProgressState
    {
        Locked,
        Unlocked,
        InProgress,
        Cleared,
        Failed
    }

    public enum FormationPosition
    {
        Front,
        Back
    }

    public enum EquipmentModifierType
    {
        Attack,
        Magic,
        Defense,
        Speed,
        CriticalRate
    }

    public enum SaveSlotKind
    {
        Manual,
        Auto
    }

    [Serializable]
    public sealed class BoolStateEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private bool value;

        public BoolStateEntry(string id, bool value)
        {
            this.id = id;
            this.value = value;
        }

        public string Id => id;
        public bool Value { get => value; set => this.value = value; }
    }

    [Serializable]
    public sealed class IntStateEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private int value;

        public IntStateEntry(string id, int value)
        {
            this.id = id;
            this.value = value;
        }

        public string Id => id;
        public int Value { get => value; set => this.value = value; }
    }

    [Serializable]
    public sealed class ItemStackSaveData
    {
        [SerializeField] private string itemId = string.Empty;
        [Min(0)]
        [SerializeField] private int count;

        public ItemStackSaveData(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = Mathf.Max(0, count);
        }

        public string ItemId => itemId;
        public int Count { get => count; set => count = Mathf.Max(0, value); }
    }

    [Serializable]
    public sealed class EquipmentModifierSaveData
    {
        [SerializeField] private EquipmentModifierType modifierType;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private int amount;

        public EquipmentModifierSaveData(EquipmentModifierType modifierType, string targetId, int amount)
        {
            this.modifierType = modifierType;
            this.targetId = targetId;
            this.amount = amount;
        }

        public EquipmentModifierType ModifierType => modifierType;
        public string TargetId => targetId;
        public int Amount => amount;
    }

    [Serializable]
    public sealed class OwnedEquipmentSaveData
    {
        [SerializeField] private string ownedEquipmentInstanceId = string.Empty;
        [SerializeField] private string equipmentId = string.Empty;
        [SerializeField] private List<EquipmentModifierSaveData> randomStatModifiers = new();
        [SerializeField] private string randomPassiveId = string.Empty;
        [SerializeField] private int randomPassiveLevel;

        public OwnedEquipmentSaveData(string ownedEquipmentInstanceId, string equipmentId)
        {
            this.ownedEquipmentInstanceId = ownedEquipmentInstanceId;
            this.equipmentId = equipmentId;
        }

        public string OwnedEquipmentInstanceId => ownedEquipmentInstanceId;
        public string EquipmentId => equipmentId;
        public IReadOnlyList<EquipmentModifierSaveData> RandomStatModifiers => randomStatModifiers;
        public string RandomPassiveId { get => randomPassiveId; set => randomPassiveId = value ?? string.Empty; }
        public int RandomPassiveLevel { get => randomPassiveLevel; set => randomPassiveLevel = Mathf.Max(0, value); }

        public void AddRandomStatModifier(EquipmentModifierSaveData modifier)
        {
            if (modifier != null)
            {
                randomStatModifiers.Add(modifier);
            }
        }
    }

    [Serializable]
    public sealed class CharacterSaveData
    {
        [SerializeField] private string characterId = string.Empty;
        [SerializeField] private bool isJoined;
        [Min(1)]
        [SerializeField] private int level = 1;
        [Min(0)]
        [SerializeField] private int exp;
        [Min(0)]
        [SerializeField] private int currentHp;
        [Min(0)]
        [SerializeField] private int currentSp;
        [SerializeField] private FormationPosition formation = FormationPosition.Front;
        [SerializeField] private string equippedWeaponInstanceId = string.Empty;
        [SerializeField] private string equippedArmorInstanceId = string.Empty;
        [SerializeField] private List<string> equippedAccessoryInstanceIds = new();

        public CharacterSaveData(string characterId)
        {
            this.characterId = characterId;
        }

        public string CharacterId => characterId;
        public bool IsJoined { get => isJoined; set => isJoined = value; }
        public int Level { get => level; set => level = Mathf.Max(1, value); }
        public int Exp { get => exp; set => exp = Mathf.Max(0, value); }
        public int CurrentHp { get => currentHp; set => currentHp = Mathf.Max(0, value); }
        public int CurrentSp { get => currentSp; set => currentSp = Mathf.Max(0, value); }
        public FormationPosition Formation { get => formation; set => formation = value; }
        public string EquippedWeaponInstanceId { get => equippedWeaponInstanceId; set => equippedWeaponInstanceId = value ?? string.Empty; }
        public string EquippedArmorInstanceId { get => equippedArmorInstanceId; set => equippedArmorInstanceId = value ?? string.Empty; }
        public IReadOnlyList<string> EquippedAccessoryInstanceIds => equippedAccessoryInstanceIds;

        public void SetEquippedAccessories(IEnumerable<string> instanceIds)
        {
            equippedAccessoryInstanceIds.Clear();

            if (instanceIds == null)
            {
                return;
            }

            foreach (var instanceId in instanceIds)
            {
                if (!string.IsNullOrWhiteSpace(instanceId) && equippedAccessoryInstanceIds.Count < 2)
                {
                    equippedAccessoryInstanceIds.Add(instanceId);
                }
            }
        }
    }

    [Serializable]
    public sealed class QuestStateSaveData
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private QuestProgressState state;

        public QuestStateSaveData(string questId, QuestProgressState state)
        {
            this.questId = questId;
            this.state = state;
        }

        public string QuestId => questId;
        public QuestProgressState State { get => state; set => state = value; }
    }

    [Serializable]
    public sealed class QuestNodeStateSaveData
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private string nodeId = string.Empty;
        [SerializeField] private bool treasureObtained;
        [SerializeField] private bool battleCleared;
        [SerializeField] private bool eventCompleted;

        public QuestNodeStateSaveData(string questId, string nodeId)
        {
            this.questId = questId;
            this.nodeId = nodeId;
        }

        public string QuestId => questId;
        public string NodeId => nodeId;
        public bool TreasureObtained { get => treasureObtained; set => treasureObtained = value; }
        public bool BattleCleared { get => battleCleared; set => battleCleared = value; }
        public bool EventCompleted { get => eventCompleted; set => eventCompleted = value; }
    }

    [Serializable]
    public sealed class ShopStockSaveData
    {
        [SerializeField] private string shopItemId = string.Empty;
        [Min(0)]
        [SerializeField] private int remainingCount;

        public ShopStockSaveData(string shopItemId, int remainingCount)
        {
            this.shopItemId = shopItemId;
            this.remainingCount = Mathf.Max(0, remainingCount);
        }

        public string ShopItemId => shopItemId;
        public int RemainingCount { get => remainingCount; set => remainingCount = Mathf.Max(0, value); }
    }
}
