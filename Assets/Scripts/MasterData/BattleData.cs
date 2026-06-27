using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
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
}
