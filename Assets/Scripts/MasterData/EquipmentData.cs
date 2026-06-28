using System.Collections.Generic;
using RPG.SaveData;
using UnityEngine;
using UnityEngine.Serialization;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Equipment", fileName = "EquipmentData")]
    public sealed class EquipmentData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private EquipmentDataType equipmentType;
        [SerializeField] private WeaponDataType weaponType;
        [SerializeField] private BattleStats statModifiers = new();
        [FormerlySerializedAs("baseModifiers")]
        [SerializeField] private List<EquipmentBaseTraitData> baseTraits = new();
        [SerializeField] private List<string> baseSkillIds = new();
        [SerializeField] private List<string> randomSkillPool = new();
        [SerializeField] private List<EquipmentModifierType> allowedRandomModifierTypes = new();
        [SerializeField] private List<string> equippableBy = new();
        [Min(0)]
        [SerializeField] private int price;
        [SerializeField] private bool unsellable;
        [SerializeField] private int sortOrder;

        public string EquipmentId => Id;
        public Sprite IconSprite => iconSprite;
        public EquipmentDataType EquipmentType => equipmentType;
        public WeaponDataType WeaponType => weaponType;
        public BattleStats StatModifiers => statModifiers;
        public IReadOnlyList<EquipmentBaseTraitData> BaseTraits => baseTraits;
        public IReadOnlyList<string> BaseSkillIds => baseSkillIds;
        public IReadOnlyList<string> RandomSkillPool => randomSkillPool;
        public IReadOnlyList<EquipmentModifierType> AllowedRandomModifierTypes => allowedRandomModifierTypes;
        public IReadOnlyList<string> EquippableBy => equippableBy;
        public int Price => price;
        public bool Unsellable => unsellable;
        public int SortOrder => sortOrder;
    }
}
