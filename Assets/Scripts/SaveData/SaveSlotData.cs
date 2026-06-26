using System;
using UnityEngine;

namespace RPG.SaveData
{
    [Serializable]
    public sealed class SaveSlotData
    {
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private SaveSlotKind slotKind;
        [SerializeField] private int manualSlotIndex;
        [SerializeField] private string savedAtUtc = string.Empty;
        [SerializeField] private RunSaveData runSaveData = RunSaveData.CreateNew();

        public string SlotId => slotId;
        public SaveSlotKind SlotKind => slotKind;
        public int ManualSlotIndex => manualSlotIndex;
        public string SavedAtUtc => savedAtUtc;
        public RunSaveData RunSaveData => runSaveData;

        public static SaveSlotData CreateManual(int manualSlotIndex, RunSaveData runSaveData)
        {
            return new SaveSlotData
            {
                slotId = SaveSlotIds.GetManualSlotId(manualSlotIndex),
                slotKind = SaveSlotKind.Manual,
                manualSlotIndex = Mathf.Clamp(manualSlotIndex, 1, SaveSlotIds.ManualSlotCount),
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                runSaveData = runSaveData ?? RunSaveData.CreateNew()
            };
        }

        public static SaveSlotData CreateAuto(RunSaveData runSaveData)
        {
            return new SaveSlotData
            {
                slotId = SaveSlotIds.AutoSlotId,
                slotKind = SaveSlotKind.Auto,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                runSaveData = runSaveData ?? RunSaveData.CreateNew()
            };
        }

        public void RefreshSavedAt()
        {
            savedAtUtc = DateTime.UtcNow.ToString("O");
        }
    }

    public static class SaveSlotIds
    {
        public const int ManualSlotCount = 3;
        public const string AutoSlotId = "auto";

        public static string GetManualSlotId(int manualSlotIndex)
        {
            return $"manual_{Mathf.Clamp(manualSlotIndex, 1, ManualSlotCount)}";
        }
    }
}
