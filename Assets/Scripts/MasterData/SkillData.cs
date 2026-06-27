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
}
