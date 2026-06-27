using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.SaveData
{
    [Serializable]
    public sealed class RunSaveData
    {
        public const int FirstDay = 1;
        public const int LastActionDay = 40;
        public const int FinalEventDay = 41;
        public const int MaxMainProgress = 4;
        public const int InitialSynthesisLevel = 1;
        public const int MaxSynthesisLevel = 5;

        [SerializeField] private int currentDay = FirstDay;
        [SerializeField] private GamePhase currentPhase = GamePhase.Phase1;
        [SerializeField] private int mainProgress;
        [SerializeField] private bool actionCompletedToday;
        [Min(0)]
        [SerializeField] private int money;
        [SerializeField] private List<ItemStackSaveData> consumableItems = new();
        [SerializeField] private List<ItemStackSaveData> materials = new();
        [SerializeField] private List<OwnedEquipmentSaveData> ownedEquipments = new();
        [SerializeField] private List<CharacterSaveData> characters = new();
        [SerializeField] private List<string> partyCharacterIds = new();
        [SerializeField] private List<string> occurredEventIds = new();
        [SerializeField] private List<BoolStateEntry> flags = new();
        [SerializeField] private List<IntStateEntry> variables = new();
        [SerializeField] private List<QuestStateSaveData> questStates = new();
        [SerializeField] private List<QuestNodeStateSaveData> questNodeStates = new();
        [SerializeField] private List<string> unlockedWanderLocationIds = new();
        [SerializeField] private List<ShopStockSaveData> shopStocks = new();
        [SerializeField] private List<int> restockedPhaseNumbers = new();
        [SerializeField] private int synthesisLevel = InitialSynthesisLevel;
        [SerializeField] private List<int> completedSynthesisLevelUps = new();

        public int CurrentDay => currentDay;
        public GamePhase CurrentPhase => currentPhase;
        public int MainProgress => mainProgress;
        public bool ActionCompletedToday => actionCompletedToday;
        public int Money => money;
        public IReadOnlyList<ItemStackSaveData> ConsumableItems => consumableItems;
        public IReadOnlyList<ItemStackSaveData> Materials => materials;
        public IReadOnlyList<OwnedEquipmentSaveData> OwnedEquipments => ownedEquipments;
        public IReadOnlyList<CharacterSaveData> Characters => characters;
        public IReadOnlyList<string> PartyCharacterIds => partyCharacterIds;
        public IReadOnlyList<string> OccurredEventIds => occurredEventIds;
        public IReadOnlyList<BoolStateEntry> Flags => flags;
        public IReadOnlyList<IntStateEntry> Variables => variables;
        public IReadOnlyList<QuestStateSaveData> QuestStates => questStates;
        public IReadOnlyList<QuestNodeStateSaveData> QuestNodeStates => questNodeStates;
        public IReadOnlyList<string> UnlockedWanderLocationIds => unlockedWanderLocationIds;
        public IReadOnlyList<ShopStockSaveData> ShopStocks => shopStocks;
        public IReadOnlyList<int> RestockedPhaseNumbers => restockedPhaseNumbers;
        public int SynthesisLevel => synthesisLevel;
        public IReadOnlyList<int> CompletedSynthesisLevelUps => completedSynthesisLevelUps;
        public bool CanTakeNormalAction => currentDay <= LastActionDay && !actionCompletedToday;
        public bool IsFinalEventDay => currentDay >= FinalEventDay;

        public static RunSaveData CreateNew()
        {
            return new RunSaveData();
        }

        public static GamePhase GetPhaseForDay(int day)
        {
            if (day >= FinalEventDay)
            {
                return GamePhase.Final;
            }

            if (day >= 31)
            {
                return GamePhase.Phase4;
            }

            if (day >= 21)
            {
                return GamePhase.Phase3;
            }

            if (day >= 11)
            {
                return GamePhase.Phase2;
            }

            return GamePhase.Phase1;
        }

        public void MarkActionCompleted()
        {
            actionCompletedToday = true;
        }

        public void AdvanceToNextDay()
        {
            currentDay = Mathf.Min(currentDay + 1, FinalEventDay);
            currentPhase = GetPhaseForDay(currentDay);
            actionCompletedToday = false;
        }

        public void SetDay(int day)
        {
            currentDay = Mathf.Clamp(day, FirstDay, FinalEventDay);
            currentPhase = GetPhaseForDay(currentDay);
            actionCompletedToday = false;
        }

        public void SetMainProgress(int progress)
        {
            mainProgress = Mathf.Clamp(progress, 0, MaxMainProgress);
        }

        public void NormalizeAfterLoad()
        {
            currentDay = Mathf.Clamp(currentDay, FirstDay, FinalEventDay);
            currentPhase = GetPhaseForDay(currentDay);
            mainProgress = Mathf.Clamp(mainProgress, 0, MaxMainProgress);
            money = Mathf.Max(0, money);
            synthesisLevel = Mathf.Clamp(synthesisLevel, InitialSynthesisLevel, MaxSynthesisLevel);
        }

        public void AddMoney(int amount)
        {
            if (amount > 0)
            {
                money += amount;
            }
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount < 0 || money < amount)
            {
                return false;
            }

            money -= amount;
            return true;
        }

        public int GetConsumableCount(string itemId)
        {
            return FindItemStack(consumableItems, itemId)?.Count ?? 0;
        }

        public int GetMaterialCount(string itemId)
        {
            return FindItemStack(materials, itemId)?.Count ?? 0;
        }

        public void AddConsumable(string itemId, int count)
        {
            AddItemStack(consumableItems, itemId, count);
        }

        public bool TryConsumeConsumable(string itemId, int count)
        {
            return TryRemoveItemStack(consumableItems, itemId, count);
        }

        public void AddMaterial(string itemId, int count)
        {
            AddItemStack(materials, itemId, count);
        }

        public bool TryConsumeMaterial(string itemId, int count)
        {
            return TryRemoveItemStack(materials, itemId, count);
        }

        public void AddOwnedEquipment(OwnedEquipmentSaveData equipment)
        {
            if (equipment != null && !string.IsNullOrWhiteSpace(equipment.OwnedEquipmentInstanceId))
            {
                ownedEquipments.Add(equipment);
            }
        }

        public bool HasOwnedEquipment(string ownedEquipmentInstanceId)
        {
            return ownedEquipments.Exists(equipment => equipment.OwnedEquipmentInstanceId == ownedEquipmentInstanceId);
        }

        public bool TryRemoveOwnedEquipment(string ownedEquipmentInstanceId)
        {
            if (string.IsNullOrWhiteSpace(ownedEquipmentInstanceId))
            {
                return false;
            }

            var equipment = ownedEquipments.Find(entry => entry.OwnedEquipmentInstanceId == ownedEquipmentInstanceId);
            if (equipment == null)
            {
                return false;
            }

            ownedEquipments.Remove(equipment);
            return true;
        }

        public CharacterSaveData GetOrCreateCharacter(string characterId)
        {
            var character = characters.Find(entry => entry.CharacterId == characterId);
            if (character != null)
            {
                return character;
            }

            character = new CharacterSaveData(characterId);
            characters.Add(character);
            return character;
        }

        public void SetParty(IEnumerable<string> characterIds)
        {
            partyCharacterIds.Clear();

            if (characterIds == null)
            {
                return;
            }

            foreach (var characterId in characterIds)
            {
                if (!string.IsNullOrWhiteSpace(characterId) && !partyCharacterIds.Contains(characterId))
                {
                    partyCharacterIds.Add(characterId);
                }
            }
        }

        public bool HasEventOccurred(string eventId)
        {
            return occurredEventIds.Contains(eventId);
        }

        public void MarkEventOccurred(string eventId)
        {
            AddUniqueId(occurredEventIds, eventId);
        }

        public bool GetFlag(string flagId)
        {
            return flags.Find(entry => entry.Id == flagId)?.Value ?? false;
        }

        public void SetFlag(string flagId, bool value)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            var entry = flags.Find(flag => flag.Id == flagId);
            if (entry == null)
            {
                flags.Add(new BoolStateEntry(flagId, value));
                return;
            }

            entry.Value = value;
        }

        public int GetVariable(string variableId)
        {
            return variables.Find(entry => entry.Id == variableId)?.Value ?? 0;
        }

        public void SetVariable(string variableId, int value)
        {
            if (string.IsNullOrWhiteSpace(variableId))
            {
                return;
            }

            var entry = variables.Find(variable => variable.Id == variableId);
            if (entry == null)
            {
                variables.Add(new IntStateEntry(variableId, value));
                return;
            }

            entry.Value = value;
        }

        public void AddVariable(string variableId, int amount)
        {
            SetVariable(variableId, GetVariable(variableId) + amount);
        }

        public QuestProgressState GetQuestState(string questId)
        {
            return questStates.Find(entry => entry.QuestId == questId)?.State ?? QuestProgressState.Locked;
        }

        public void SetQuestState(string questId, QuestProgressState state)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return;
            }

            var entry = questStates.Find(questState => questState.QuestId == questId);
            if (entry == null)
            {
                questStates.Add(new QuestStateSaveData(questId, state));
                return;
            }

            entry.State = state;
        }

        public QuestNodeStateSaveData GetOrCreateQuestNodeState(string questId, string nodeId)
        {
            var nodeState = questNodeStates.Find(entry => entry.QuestId == questId && entry.NodeId == nodeId);
            if (nodeState != null)
            {
                return nodeState;
            }

            nodeState = new QuestNodeStateSaveData(questId, nodeId);
            questNodeStates.Add(nodeState);
            return nodeState;
        }

        public bool IsWanderLocationUnlocked(string locationId)
        {
            return unlockedWanderLocationIds.Contains(locationId);
        }

        public void UnlockWanderLocation(string locationId)
        {
            AddUniqueId(unlockedWanderLocationIds, locationId);
        }

        public ShopStockSaveData GetOrCreateShopStock(string shopItemId, int defaultRemainingCount)
        {
            var stock = shopStocks.Find(entry => entry.ShopItemId == shopItemId);
            if (stock != null)
            {
                return stock;
            }

            stock = new ShopStockSaveData(shopItemId, defaultRemainingCount);
            shopStocks.Add(stock);
            return stock;
        }

        public bool WasPhaseRestocked(GamePhase phase)
        {
            return restockedPhaseNumbers.Contains((int)phase);
        }

        public void MarkPhaseRestocked(GamePhase phase)
        {
            if (phase != GamePhase.Final)
            {
                AddUniqueInt(restockedPhaseNumbers, (int)phase);
            }
        }

        public void SetSynthesisLevel(int level)
        {
            synthesisLevel = Mathf.Clamp(level, InitialSynthesisLevel, MaxSynthesisLevel);
        }

        public void MarkSynthesisLevelUpCompleted(int reachedLevel)
        {
            if (reachedLevel >= InitialSynthesisLevel && reachedLevel <= MaxSynthesisLevel)
            {
                AddUniqueInt(completedSynthesisLevelUps, reachedLevel);
            }
        }

        private static ItemStackSaveData FindItemStack(List<ItemStackSaveData> stacks, string itemId)
        {
            return stacks.Find(stack => stack.ItemId == itemId);
        }

        private static void AddItemStack(List<ItemStackSaveData> stacks, string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return;
            }

            var stack = FindItemStack(stacks, itemId);
            if (stack == null)
            {
                stacks.Add(new ItemStackSaveData(itemId, count));
                return;
            }

            stack.Count += count;
        }

        private static bool TryRemoveItemStack(List<ItemStackSaveData> stacks, string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return false;
            }

            var stack = FindItemStack(stacks, itemId);
            if (stack == null || stack.Count < count)
            {
                return false;
            }

            stack.Count -= count;

            if (stack.Count == 0)
            {
                stacks.Remove(stack);
            }

            return true;
        }

        private static void AddUniqueId(List<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        private static void AddUniqueInt(List<int> values, int value)
        {
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }
    }
}
