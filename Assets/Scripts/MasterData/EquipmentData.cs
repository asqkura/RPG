using System;
using System.Collections.Generic;
using RPG.SaveData;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Equipment", fileName = "EquipmentData")]
    public sealed class EquipmentData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private EquipmentDataType equipmentType;
        [SerializeField] private WeaponDataType weaponType;
        [SerializeField] private BattleStats statModifiers = new();
        [SerializeField] private List<string> activeSkillIds = new();
        [SerializeField] private List<string> fixedPassiveIds = new();
        [SerializeField] private List<EquipmentRandomPassiveData> randomPassivePool = new();
        [SerializeField] private List<EquipmentModifierType> allowedRandomStatTypes = new();
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
        public IReadOnlyList<string> ActiveSkillIds => equipmentType == EquipmentDataType.Weapon
            ? activeSkillIds
            : Array.Empty<string>();
        public IReadOnlyList<string> FixedPassiveIds => fixedPassiveIds;
        public IReadOnlyList<EquipmentRandomPassiveData> RandomPassivePool => randomPassivePool;
        public IReadOnlyList<EquipmentModifierType> AllowedRandomStatTypes => allowedRandomStatTypes;
        public IReadOnlyList<string> EquippableBy => equippableBy;
        public int Price => price;
        public bool Unsellable => unsellable;
        public int SortOrder => sortOrder;
    }
}
