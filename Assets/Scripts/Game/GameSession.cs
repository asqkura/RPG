using RPG.SaveData;
using UnityEngine;

namespace RPG.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession current;

        [Min(0)]
        [SerializeField] private int initialMoney = 12480;

        private RunSaveData runSaveData;

        public static GameSession Current => current;

        public RunSaveData RunSaveData
        {
            get
            {
                EnsureRunSaveData();
                return runSaveData;
            }
        }

        public static GameSession GetOrCreate()
        {
            if (current != null)
            {
                return current;
            }

            var sessionObject = new GameObject("GameSession");
            return sessionObject.AddComponent<GameSession>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnGameStart()
        {
            GetOrCreate();
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            DontDestroyOnLoad(gameObject);
            EnsureRunSaveData();
        }

        public void StartNewRun()
        {
            runSaveData = CreateInitialRunSaveData();
        }

        private void EnsureRunSaveData()
        {
            if (runSaveData == null)
            {
                runSaveData = CreateInitialRunSaveData();
            }
        }

        private RunSaveData CreateInitialRunSaveData()
        {
            var saveData = RunSaveData.CreateNew();
            saveData.AddMoney(initialMoney);
            saveData.SetSynthesisLevel(RunSaveData.MaxSynthesisLevel);
            AddInitialSynthesisMaterials(saveData);
            return saveData;
        }

        private static void AddInitialSynthesisMaterials(RunSaveData saveData)
        {
            saveData.AddMaterial("mat_iron_ore", 50);
            saveData.AddMaterial("mat_steel_ore", 50);
            saveData.AddMaterial("mat_sturdy_wood", 50);
            saveData.AddMaterial("mat_hard_wood", 50);
            saveData.AddMaterial("mat_beast_hide", 50);
            saveData.AddMaterial("mat_fine_leather", 50);
            saveData.AddMaterial("mat_magic_shard", 50);
            saveData.AddMaterial("mat_magic_stone", 50);
            saveData.AddMaterial("mat_herb", 50);
            saveData.AddMaterial("mat_healing_grass", 50);
        }
    }
}
