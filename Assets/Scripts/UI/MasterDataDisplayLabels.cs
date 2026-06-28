using RPG.MasterData;
using RPG.SaveData;

public static class MasterDataDisplayLabels
{
    public const string Consumable = "消耗品";
    public const string Material = "素材";
    public const string Weapon = "武器";
    public const string Armor = "防具";
    public const string Accessory = "アクセサリ";

    public static string FormatItemType(ItemDataType itemType)
    {
        return itemType switch
        {
            ItemDataType.Consumable => Consumable,
            ItemDataType.Material => Material,
            _ => itemType.ToString()
        };
    }

    public static string FormatEquipmentType(EquipmentData equipment)
    {
        if (equipment == null)
        {
            return string.Empty;
        }

        return equipment.EquipmentType switch
        {
            EquipmentDataType.Weapon => $"{Weapon}/{FormatWeaponType(equipment.WeaponType)}",
            EquipmentDataType.Armor => Armor,
            EquipmentDataType.Accessory => Accessory,
            _ => equipment.EquipmentType.ToString()
        };
    }

    public static string FormatWeaponType(WeaponDataType weaponType)
    {
        return weaponType switch
        {
            WeaponDataType.Sword => "剣",
            WeaponDataType.Dagger => "短剣",
            WeaponDataType.Axe => "斧",
            WeaponDataType.Spear => "槍",
            WeaponDataType.Bow => "弓",
            WeaponDataType.Staff => "杖",
            _ => Weapon
        };
    }

    public static string FormatRarity(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => "コモン",
            EquipmentRarity.Rare => "レア",
            EquipmentRarity.Epic => "エピック",
            EquipmentRarity.Legendary => "レジェンダリー",
            _ => rarity.ToString()
        };
    }

    public static string FormatTag(string label)
    {
        return string.IsNullOrWhiteSpace(label) ? string.Empty : $"【{label}】";
    }
}
