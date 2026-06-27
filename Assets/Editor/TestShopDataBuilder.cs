using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPG.MasterData;
using UnityEditor;
using UnityEngine;

public static class TestShopDataBuilder
{
    private const string RootFolder = "Assets/MasterData";
    private const string TestFolder = RootFolder + "/Test";
    private const string ItemFolder = TestFolder + "/Items";
    private const string EquipmentFolder = TestFolder + "/Equipment";
    private const string ShopFolder = TestFolder + "/Shop";
    private const string DatabaseFolder = TestFolder + "/Databases";

    private const string ItemDatabasePath = DatabaseFolder + "/TestItemDatabase.asset";
    private const string EquipmentDatabasePath = DatabaseFolder + "/TestEquipmentDatabase.asset";
    private const string ShopItemDatabasePath = DatabaseFolder + "/TestShopItemDatabase.asset";

    private const string Icon11Path = "Assets/UI/Icons/icon-1_1.png";
    private const string Icon12Path = "Assets/UI/Icons/icon-1_2.png";
    private const string Icon21Path = "Assets/UI/Icons/icon-2_1.png";
    private const string Icon31Path = "Assets/UI/Icons/icon-3_1.png";

    [MenuItem("Tools/RPG/Build Test Shop Data")]
    public static void Build()
    {
        EnsureFolders();
        DeleteGeneratedAssets();
        AssetDatabase.Refresh();

        var consumables = CreateConsumables();
        var materials = CreateMaterials();
        var equipments = CreateEquipments();

        CreateOrUpdateDatabase(ItemDatabasePath, consumables.Concat(materials).ToArray());
        CreateOrUpdateDatabase(EquipmentDatabasePath, equipments.ToArray());
        CreateOrUpdateDatabase(ShopItemDatabasePath, CreateShopItems(consumables, materials, equipments).ToArray());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Test shop master data was generated.");
    }

    private static List<ItemData> CreateConsumables()
    {
        var specs = new[]
        {
            new ItemSpec("item_potion", "ポーション", "HPを小回復する基本の薬。", "HP回復", 80, "icon-1_2_0", Icon12Path, true, true, ItemEffectDataType.RecoverHp, 50),
            new ItemSpec("item_high_potion", "ハイポーション", "HPを中回復する上等な薬。", "HP回復", 180, "icon-1_2_1", Icon12Path, true, true, ItemEffectDataType.RecoverHp, 120),
            new ItemSpec("item_mana_drop", "マナの雫", "SPを小回復する澄んだ雫。", "SP回復", 120, "icon-2_1_0", Icon21Path, true, true, ItemEffectDataType.RecoverSp, 30),
            new ItemSpec("item_mana_vial", "マナ小瓶", "SPを中回復する魔力の小瓶。", "SP回復", 260, "icon-2_1_1", Icon21Path, true, true, ItemEffectDataType.RecoverSp, 70),
            new ItemSpec("item_antidote", "毒消し", "毒を解除する苦い薬。", "毒回復", 60, "icon-1_1_140", Icon11Path, true, false, ItemEffectDataType.CurePoison, 1),
            new ItemSpec("item_clear_herb", "清めの薬草", "状態異常を解除する薬草。", "状態異常回復", 150, "icon-3_1_80", Icon31Path, true, false, ItemEffectDataType.CureStatus, 1),
            new ItemSpec("item_smoke_ball", "煙玉", "戦闘から逃げやすくする道具。", "逃走補助", 100, "icon-3_1_81", Icon31Path, true, false, ItemEffectDataType.Escape, 1),
            new ItemSpec("item_guard_salve", "守りの軟膏", "傷口を保護する応急薬。", "HP回復", 140, "icon-1_2_2", Icon12Path, true, true, ItemEffectDataType.RecoverHp, 80),
            new ItemSpec("item_spirit_tea", "精霊茶", "少量のSPを整える香草茶。", "SP回復", 90, "icon-2_1_2", Icon21Path, true, true, ItemEffectDataType.RecoverSp, 20),
            new ItemSpec("item_bitter_tonic", "苦い強壮薬", "HPを少し回復する安価な薬。", "HP回復", 45, "icon-1_2_3", Icon12Path, true, true, ItemEffectDataType.RecoverHp, 30),
        };

        return specs.Select((spec, index) => CreateOrUpdateItem(spec, ItemDataType.Consumable, 0, index)).ToList();
    }

    private static List<ItemData> CreateMaterials()
    {
        var specs = new[]
        {
            new MaterialSpec("mat_iron_ore", "鉄鉱石", "武器や防具の合成に使う基本鉱石。", "鉱石系", 1, 60, "icon-1_1_140", Icon11Path),
            new MaterialSpec("mat_steel_ore", "鋼鉱石", "強化装備に使う硬い鉱石。", "鉱石系", 2, 140, "icon-1_1_141", Icon11Path),
            new MaterialSpec("mat_sturdy_wood", "丈夫な木材", "道具や杖に使う扱いやすい木材。", "木材系", 1, 40, "icon-3_1_80", Icon31Path),
            new MaterialSpec("mat_hard_wood", "硬い木材", "弓や杖の芯材に使う木材。", "木材系", 2, 110, "icon-3_1_81", Icon31Path),
            new MaterialSpec("mat_beast_hide", "獣の皮", "軽装備に使うなめし前の皮。", "皮/布系", 1, 50, "icon-3_1_94", Icon31Path),
            new MaterialSpec("mat_fine_leather", "上質な革", "防具やアクセサリに使う革。", "皮/布系", 2, 130, "icon-3_1_95", Icon31Path),
            new MaterialSpec("mat_magic_shard", "魔石の欠片", "微かな魔力を帯びた欠片。", "魔石系", 1, 90, "icon-2_1_0", Icon21Path),
            new MaterialSpec("mat_magic_stone", "魔石", "合成に使う安定した魔石。", "魔石系", 2, 220, "icon-2_1_1", Icon21Path),
            new MaterialSpec("mat_herb", "薬草", "回復アイテムの基本素材。", "薬草系", 1, 30, "icon-3_1_80", Icon31Path),
            new MaterialSpec("mat_healing_grass", "癒し草", "効能の高い回復素材。", "薬草系", 2, 95, "icon-3_1_82", Icon31Path),
        };

        return specs.Select((spec, index) => CreateOrUpdateMaterial(spec, index + 100)).ToList();
    }

    private static List<EquipmentData> CreateEquipments()
    {
        var specs = new[]
        {
            new EquipmentSpec("eq_apprentice_dagger", "見習いの短剣", "扱いやすい短剣。素早い仲間向け。", EquipmentDataType.Weapon, WeaponDataType.Dagger, 620, "icon-3_1_6", Icon31Path),
            new EquipmentSpec("eq_iron_sword", "鉄の剣", "標準的な鉄製の剣。", EquipmentDataType.Weapon, WeaponDataType.Sword, 760, "icon-3_1_7", Icon31Path),
            new EquipmentSpec("eq_hunter_bow", "狩人の弓", "後衛から安定して攻撃できる弓。", EquipmentDataType.Weapon, WeaponDataType.Bow, 700, "icon-3_1_8", Icon31Path),
            new EquipmentSpec("eq_oak_staff", "樫の杖", "魔力を通しやすい木杖。", EquipmentDataType.Weapon, WeaponDataType.Staff, 680, "icon-3_1_9", Icon31Path),
            new EquipmentSpec("eq_traveler_cloak", "旅人の外套", "旅の汚れに強い軽い外套。", EquipmentDataType.Armor, WeaponDataType.None, 480, "icon-3_1_94", Icon31Path),
            new EquipmentSpec("eq_leather_armor", "革の鎧", "動きやすさを重視した防具。", EquipmentDataType.Armor, WeaponDataType.None, 650, "icon-3_1_95", Icon31Path),
            new EquipmentSpec("eq_iron_mail", "鉄の胸当て", "防御を固める金属防具。", EquipmentDataType.Armor, WeaponDataType.None, 920, "icon-3_1_96", Icon31Path),
            new EquipmentSpec("eq_lucky_charm", "幸運のお守り", "小さな幸運を呼ぶアクセサリ。", EquipmentDataType.Accessory, WeaponDataType.None, 420, "icon-1_2_99", Icon12Path),
            new EquipmentSpec("eq_guard_ring", "守りの指輪", "防御を少し高める指輪。", EquipmentDataType.Accessory, WeaponDataType.None, 560, "icon-1_2_100", Icon12Path),
            new EquipmentSpec("eq_swift_boots", "疾風の靴", "素早さを補う軽い靴。", EquipmentDataType.Accessory, WeaponDataType.None, 740, "icon-1_1_140", Icon11Path),
        };

        return specs.Select((spec, index) => CreateOrUpdateEquipment(spec, index)).ToList();
    }

    private static IEnumerable<ShopItemData> CreateShopItems(
        IReadOnlyList<ItemData> consumables,
        IReadOnlyList<ItemData> materials,
        IReadOnlyList<EquipmentData> equipments)
    {
        var result = new List<ShopItemData>();
        var sortOrder = 0;

        foreach (var item in consumables)
        {
            result.Add(CreateOrUpdateShopItem("shop_" + item.ItemId, item.ItemId, ShopProductDataType.Item, 1, ShopStockDataType.Unlimited, 0, sortOrder++));
        }

        foreach (var item in materials)
        {
            var phase = item.Rank <= 1 ? 1 : 2;
            result.Add(CreateOrUpdateShopItem("shop_" + item.ItemId, item.ItemId, ShopProductDataType.Item, phase, ShopStockDataType.Limited, item.Rank <= 1 ? 8 : 5, sortOrder++));
        }

        foreach (var equipment in equipments)
        {
            result.Add(CreateOrUpdateShopItem("shop_" + equipment.EquipmentId, equipment.EquipmentId, ShopProductDataType.Equipment, 1, ShopStockDataType.Limited, equipment.EquipmentType == EquipmentDataType.Accessory ? 2 : 1, sortOrder++));
        }

        return result;
    }

    private static ItemData CreateOrUpdateItem(ItemSpec spec, ItemDataType itemType, int rank, int sortOrder)
    {
        var item = LoadOrCreate<ItemData>(ItemFolder + "/" + spec.Id + ".asset");
        var serialized = new SerializedObject(item);
        SetMasterFields(serialized, spec.Id, spec.Name, spec.Description);
        serialized.FindProperty("iconSprite").objectReferenceValue = LoadSprite(spec.IconPath, spec.IconName);
        serialized.FindProperty("itemType").enumValueIndex = (int)itemType;
        serialized.FindProperty("category").stringValue = spec.Category;
        serialized.FindProperty("rank").intValue = rank;
        serialized.FindProperty("price").intValue = spec.Price;
        serialized.FindProperty("unsellable").boolValue = false;
        serialized.FindProperty("usableInBattle").boolValue = spec.UsableInBattle;
        serialized.FindProperty("usableInField").boolValue = spec.UsableInField;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        SetItemEffect(serialized.FindProperty("effects"), spec.EffectType, spec.EffectAmount);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static ItemData CreateOrUpdateMaterial(MaterialSpec spec, int sortOrder)
    {
        var item = LoadOrCreate<ItemData>(ItemFolder + "/" + spec.Id + ".asset");
        var serialized = new SerializedObject(item);
        SetMasterFields(serialized, spec.Id, spec.Name, spec.Description);
        serialized.FindProperty("iconSprite").objectReferenceValue = LoadSprite(spec.IconPath, spec.IconName);
        serialized.FindProperty("itemType").enumValueIndex = (int)ItemDataType.Material;
        serialized.FindProperty("category").stringValue = spec.Category;
        serialized.FindProperty("rank").intValue = spec.Rank;
        serialized.FindProperty("price").intValue = spec.Price;
        serialized.FindProperty("unsellable").boolValue = false;
        serialized.FindProperty("usableInBattle").boolValue = false;
        serialized.FindProperty("usableInField").boolValue = false;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        serialized.FindProperty("effects").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static EquipmentData CreateOrUpdateEquipment(EquipmentSpec spec, int sortOrder)
    {
        var equipment = LoadOrCreate<EquipmentData>(EquipmentFolder + "/" + spec.Id + ".asset");
        var serialized = new SerializedObject(equipment);
        SetMasterFields(serialized, spec.Id, spec.Name, spec.Description);
        serialized.FindProperty("iconSprite").objectReferenceValue = LoadSprite(spec.IconPath, spec.IconName);
        serialized.FindProperty("equipmentType").enumValueIndex = (int)spec.EquipmentType;
        serialized.FindProperty("weaponType").enumValueIndex = (int)spec.WeaponType;
        serialized.FindProperty("attackAttribute").stringValue = spec.WeaponType == WeaponDataType.None ? string.Empty : "物理";
        serialized.FindProperty("price").intValue = spec.Price;
        serialized.FindProperty("unsellable").boolValue = false;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(equipment);
        return equipment;
    }

    private static ShopItemData CreateOrUpdateShopItem(
        string id,
        string productId,
        ShopProductDataType productType,
        int availablePhase,
        ShopStockDataType stockType,
        int stockCount,
        int sortOrder)
    {
        var shopItem = LoadOrCreate<ShopItemData>(ShopFolder + "/" + id + ".asset");
        var serialized = new SerializedObject(shopItem);
        SetMasterFields(serialized, id, id, string.Empty);
        serialized.FindProperty("availablePhase").intValue = availablePhase;
        serialized.FindProperty("productType").enumValueIndex = (int)productType;
        serialized.FindProperty("productId").stringValue = productId;
        serialized.FindProperty("stockType").enumValueIndex = (int)stockType;
        serialized.FindProperty("stockCount").intValue = stockCount;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shopItem);
        return shopItem;
    }

    private static void CreateOrUpdateDatabase<TData>(string path, TData[] entries) where TData : MasterDataAsset
    {
        var database = LoadOrCreateDatabase<TData>(path);
        var serialized = new SerializedObject(database);
        var entriesProperty = serialized.FindProperty("entries");
        entriesProperty.arraySize = entries.Length;

        for (var i = 0; i < entries.Length; i++)
        {
            entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static MasterDatabase<TData> LoadOrCreateDatabase<TData>(string path) where TData : MasterDataAsset
    {
        var existing = AssetDatabase.LoadAssetAtPath<MasterDatabase<TData>>(path);
        if (existing != null)
        {
            return existing;
        }

        DeleteInvalidAsset(path);

        MasterDatabase<TData> database = typeof(TData) == typeof(ItemData)
            ? ScriptableObject.CreateInstance<ItemDatabase>() as MasterDatabase<TData>
            : typeof(TData) == typeof(EquipmentData)
                ? ScriptableObject.CreateInstance<EquipmentDatabase>() as MasterDatabase<TData>
                : ScriptableObject.CreateInstance<ShopItemDatabase>() as MasterDatabase<TData>;

        AssetDatabase.CreateAsset(database, path);
        return database;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            return existing;
        }

        DeleteInvalidAsset(path);

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void DeleteInvalidAsset(string path)
    {
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void DeleteGeneratedAssets()
    {
        DeleteAssetFiles(ItemFolder);
        DeleteAssetFiles(EquipmentFolder);
        DeleteAssetFiles(ShopFolder);
        DeleteAssetFiles(DatabaseFolder);
    }

    private static void DeleteAssetFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(folder, "*.asset", SearchOption.TopDirectoryOnly))
        {
            AssetDatabase.DeleteAsset(ToAssetPath(path));
        }
    }

    private static string ToAssetPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void SetMasterFields(SerializedObject serialized, string id, string displayName, string description)
    {
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
    }

    private static void SetItemEffect(SerializedProperty effectsProperty, ItemEffectDataType effectType, int amount)
    {
        effectsProperty.arraySize = 1;
        var effect = effectsProperty.GetArrayElementAtIndex(0);
        effect.FindPropertyRelative("effectType").enumValueIndex = (int)effectType;
        effect.FindPropertyRelative("amount").intValue = amount;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName)
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "MasterData");
        CreateFolder(RootFolder, "Test");
        CreateFolder(TestFolder, "Items");
        CreateFolder(TestFolder, "Equipment");
        CreateFolder(TestFolder, "Shop");
        CreateFolder(TestFolder, "Databases");
    }

    private static void CreateFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private readonly struct ItemSpec
    {
        public ItemSpec(string id, string name, string description, string category, int price, string iconName, string iconPath, bool usableInBattle, bool usableInField, ItemEffectDataType effectType, int effectAmount)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Price = price;
            IconName = iconName;
            IconPath = iconPath;
            UsableInBattle = usableInBattle;
            UsableInField = usableInField;
            EffectType = effectType;
            EffectAmount = effectAmount;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public int Price { get; }
        public string IconName { get; }
        public string IconPath { get; }
        public bool UsableInBattle { get; }
        public bool UsableInField { get; }
        public ItemEffectDataType EffectType { get; }
        public int EffectAmount { get; }
    }

    private readonly struct MaterialSpec
    {
        public MaterialSpec(string id, string name, string description, string category, int rank, int price, string iconName, string iconPath)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Rank = rank;
            Price = price;
            IconName = iconName;
            IconPath = iconPath;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public int Rank { get; }
        public int Price { get; }
        public string IconName { get; }
        public string IconPath { get; }
    }

    private readonly struct EquipmentSpec
    {
        public EquipmentSpec(string id, string name, string description, EquipmentDataType equipmentType, WeaponDataType weaponType, int price, string iconName, string iconPath)
        {
            Id = id;
            Name = name;
            Description = description;
            EquipmentType = equipmentType;
            WeaponType = weaponType;
            Price = price;
            IconName = iconName;
            IconPath = iconPath;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public EquipmentDataType EquipmentType { get; }
        public WeaponDataType WeaponType { get; }
        public int Price { get; }
        public string IconName { get; }
        public string IconPath { get; }
    }
}
