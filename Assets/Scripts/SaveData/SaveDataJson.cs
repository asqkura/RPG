using UnityEngine;

namespace RPG.SaveData
{
    public static class SaveDataJson
    {
        public static string ToJson(SaveSlotData saveSlotData, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(saveSlotData, prettyPrint);
        }

        public static string ToJson(PersistentSaveData persistentSaveData, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(persistentSaveData, prettyPrint);
        }

        public static SaveSlotData FromSaveSlotJson(string json)
        {
            var saveSlotData = JsonUtility.FromJson<SaveSlotData>(json);
            saveSlotData?.RunSaveData?.NormalizeAfterLoad();
            return saveSlotData;
        }

        public static PersistentSaveData FromPersistentJson(string json)
        {
            return JsonUtility.FromJson<PersistentSaveData>(json);
        }
    }
}
