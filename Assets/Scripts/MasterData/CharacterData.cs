using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Character", fileName = "CharacterData")]
    public sealed class CharacterData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private BattleStats baseStats = new();

        public string CharacterId => Id;
        public Sprite IconSprite => iconSprite;
        public BattleStats BaseStats => baseStats;
    }
}
