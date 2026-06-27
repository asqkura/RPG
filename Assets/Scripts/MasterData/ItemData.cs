using System.Collections.Generic;
using UnityEngine;

namespace RPG.MasterData
{
    [CreateAssetMenu(menuName = "RPG/Master Data/Item", fileName = "ItemData")]
    public sealed class ItemData : MasterDataAsset
    {
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private ItemDataType itemType;
        [Min(0)]
        [SerializeField] private int rank;
        [Min(0)]
        [SerializeField] private int price;
        [SerializeField] private bool unsellable;
        [SerializeField] private bool usableInBattle;
        [SerializeField] private bool usableInField;
        [SerializeField] private List<ItemEffectData> effects = new();
        [SerializeField] private int sortOrder;

        public string ItemId => Id;
        public Sprite IconSprite => iconSprite;
        public ItemDataType ItemType => itemType;
        public int Rank => rank;
        public int Price => price;
        public bool Unsellable => unsellable;
        public bool UsableInBattle => usableInBattle;
        public bool UsableInField => usableInField;
        public IReadOnlyList<ItemEffectData> Effects => effects;
        public int SortOrder => sortOrder;
    }
}
