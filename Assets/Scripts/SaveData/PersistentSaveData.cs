using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.SaveData
{
    [Serializable]
    public sealed class PersistentSaveData
    {
        [SerializeField] private List<string> clearedEndingIds = new();
        [SerializeField] private List<string> viewedEpilogueIds = new();
        [SerializeField] private List<string> tutorialSeenFlags = new();
        [SerializeField] private GameSettingsSaveData settings = new();
        [SerializeField] private string lastLoadedSlotId = string.Empty;

        public IReadOnlyList<string> ClearedEndingIds => clearedEndingIds;
        public IReadOnlyList<string> ViewedEpilogueIds => viewedEpilogueIds;
        public IReadOnlyList<string> TutorialSeenFlags => tutorialSeenFlags;
        public GameSettingsSaveData Settings => settings;
        public string LastLoadedSlotId { get => lastLoadedSlotId; set => lastLoadedSlotId = value ?? string.Empty; }

        public void MarkEndingCleared(string endingId)
        {
            AddUniqueId(clearedEndingIds, endingId);
        }

        public void MarkEpilogueViewed(string epilogueId)
        {
            AddUniqueId(viewedEpilogueIds, epilogueId);
        }

        public bool HasSeenTutorial(string tutorialFlagId)
        {
            return tutorialSeenFlags.Contains(tutorialFlagId);
        }

        public void MarkTutorialSeen(string tutorialFlagId)
        {
            AddUniqueId(tutorialSeenFlags, tutorialFlagId);
        }

        private static void AddUniqueId(List<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }
    }

    [Serializable]
    public sealed class GameSettingsSaveData
    {
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float seVolume = 1f;
        [Min(0.1f)]
        [SerializeField] private float messageSpeed = 1f;
        [Min(0.1f)]
        [SerializeField] private float battleSpeed = 1f;
        [SerializeField] private bool shortenEffects;
        [SerializeField] private bool rememberBattleCommand = true;

        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }
        public float BgmVolume { get => bgmVolume; set => bgmVolume = Mathf.Clamp01(value); }
        public float SeVolume { get => seVolume; set => seVolume = Mathf.Clamp01(value); }
        public float MessageSpeed { get => messageSpeed; set => messageSpeed = Mathf.Max(0.1f, value); }
        public float BattleSpeed { get => battleSpeed; set => battleSpeed = Mathf.Max(0.1f, value); }
        public bool ShortenEffects { get => shortenEffects; set => shortenEffects = value; }
        public bool RememberBattleCommand { get => rememberBattleCommand; set => rememberBattleCommand = value; }
    }
}
