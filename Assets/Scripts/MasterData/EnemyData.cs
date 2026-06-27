using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
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
}
