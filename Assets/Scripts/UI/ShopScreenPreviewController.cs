using System.Collections.Generic;
using System.Linq;
using RPG.Game;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopScreenPreviewController : MonoBehaviour, IItemRowViewController
{
    private const int PreviewPhase = 1;
    private const string UnlimitedStockText = "-";
    private const float RowStride = 54f;
    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);

    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailBodyText;
    [SerializeField] private TMP_Text detailStockText;
    [SerializeField] private TMP_Text detailOwnedText;
    [SerializeField] private TMP_Text detailPriceText;
    [SerializeField] private TMP_Text helpText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private TMP_Text stockHeaderText;
    [SerializeField] private TMP_Text ownedHeaderText;
    [SerializeField] private TMP_Text priceHeaderText;
    [SerializeField] private ShopItemRowView itemRowPrefab;
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private RectTransform itemRowViewport;
    [SerializeField] private RectTransform itemRowContent;
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Button allCategoryButton;
    [SerializeField] private Button consumableCategoryButton;
    [SerializeField] private Button materialCategoryButton;
    [SerializeField] private Button equipmentCategoryButton;
    [SerializeField] private Button weaponCategoryButton;
    [SerializeField] private Button armorCategoryButton;
    [SerializeField] private Button accessoryCategoryButton;
    [SerializeField] private Button otherCategoryButton;
    [SerializeField] private ShopItemDatabase shopItemDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private CharacterDatabase characterDatabase;

    private ShopCategory currentCategory = ShopCategory.All;
    private ShopMode currentMode = ShopMode.Buy;
    private ShopItemRowView selectedRow;
    private ShopItemRowView previewRow;
    private RunSaveData runSaveData;
    private ShopPurchaseService purchaseService;
    private ShopSellService sellService;
    private readonly List<ShopDisplayEntry> displayEntries = new();
    private readonly List<ShopItemRowView> itemRows = new();
    private bool initialized;

    private void Awake()
    {
        InitializeGameState();
        ApplyReferenceShopLayout();
        ResolveOptionalReferences();
        RegisterCategoryButtons();
        RegisterModeButtons();
        RefreshModeButtons();
        RefreshColumnHeaders();
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
        RefreshMoneyText();
        initialized = true;
    }

    private void OnEnable()
    {
        if (initialized)
        {
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.RemoveListener(ShowAll);
        }

        if (consumableCategoryButton != null)
        {
            consumableCategoryButton.onClick.RemoveListener(ShowConsumables);
        }

        if (materialCategoryButton != null)
        {
            materialCategoryButton.onClick.RemoveListener(ShowMaterials);
        }

        if (equipmentCategoryButton != null)
        {
            equipmentCategoryButton.onClick.RemoveListener(ShowEquipment);
        }

        if (weaponCategoryButton != null)
        {
            weaponCategoryButton.onClick.RemoveListener(ShowWeapons);
        }

        if (armorCategoryButton != null)
        {
            armorCategoryButton.onClick.RemoveListener(ShowArmor);
        }

        if (accessoryCategoryButton != null)
        {
            accessoryCategoryButton.onClick.RemoveListener(ShowAccessories);
        }

        if (otherCategoryButton != null)
        {
            otherCategoryButton.onClick.RemoveListener(ShowOther);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(SubmitCurrentItem);
        }

        if (buyTabButton != null)
        {
            buyTabButton.onClick.RemoveListener(ShowBuyMode);
        }

        if (sellTabButton != null)
        {
            sellTabButton.onClick.RemoveListener(ShowSellMode);
        }
    }

    public void Hover(ShopItemRowView row)
    {
        if (row == null || previewRow == row)
        {
            return;
        }

        previewRow = row;
        ShowDetail(row);
    }

    public void Select(ShopItemRowView row)
    {
        if (row == null)
        {
            return;
        }

        if (selectedRow == row)
        {
            previewRow = row;
            ShowDetail(row);
            return;
        }

        ClearSelectedRow();
        selectedRow = row;
        selectedRow.SetHighlighted(true);
        SetEventSystemSelection(selectedRow.gameObject);
        previewRow = row;
        ShowDetail(row);
    }

    private void ShowDetail(ShopItemRowView row)
    {
        if (row == null)
        {
            ClearDetail();
            return;
        }

        if (detailTitleText != null)
        {
            detailTitleText.text = row.ItemName;
        }

        if (detailIconImage != null)
        {
            detailIconImage.sprite = row.IconSprite;
            detailIconImage.enabled = row.IconSprite != null;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = row.DetailText;
        }

        if (detailStockText != null)
        {
            detailStockText.text = row.StockText;
        }

        if (detailOwnedText != null)
        {
            detailOwnedText.text = row.OwnedText;
        }

        if (detailPriceText != null)
        {
            detailPriceText.text = row.PriceText;
        }

        if (helpText != null)
        {
            helpText.text = FormatDetailHelp(row);
        }

        RefreshActionButtonState();
    }

    public void Clear(ShopItemRowView row)
    {
        if (row == null || previewRow != row)
        {
            return;
        }

        previewRow = selectedRow;

        if (selectedRow != null)
        {
            ShowDetail(selectedRow);
            return;
        }

        ClearDetail();
    }

    public void Refresh()
    {
        ClearSelectedRow();
        previewRow = null;
        PopulateRows();
        SelectFirstRow();
        RefreshMoneyText();
        RefreshActionButtonState();
    }

    public void SubmitCurrentItem()
    {
        if (currentMode == ShopMode.Sell)
        {
            SellCurrentItem();
            return;
        }

        PurchaseCurrentItem();
    }

    public void PurchaseCurrentItem()
    {
        if (selectedRow == null || purchaseService == null || runSaveData == null)
        {
            SetHelpText("購入する商品を選んでください。");
            return;
        }

        var purchasedShopItemId = selectedRow.ShopItemId;
        var purchasedItemName = selectedRow.ItemName;
        var result = purchaseService.TryPurchase(runSaveData, purchasedShopItemId);
        if (!result.CanPurchase)
        {
            SetHelpText(FormatPurchaseFailure(result.FailureReason));
            return;
        }

        PopulateRows();
        ClearSelectedRow();
        previewRow = null;
        SelectRowByShopItemId(purchasedShopItemId);
        RefreshMoneyText();
        RefreshActionButtonState();
        SetHelpText($"{purchasedItemName}を購入しました。");
    }

    public void SellCurrentItem()
    {
        if (selectedRow == null || sellService == null || runSaveData == null)
        {
            SetHelpText("売却する所持品を選んでください。");
            return;
        }

        var soldEntryId = selectedRow.ShopItemId;
        var soldItemName = selectedRow.ItemName;
        var result = ShouldSellSelectedAsEquipment(soldEntryId)
            ? sellService.TrySellEquipment(runSaveData, soldEntryId)
            : sellService.TrySellItem(runSaveData, soldEntryId);
        if (!result.CanSell)
        {
            SetHelpText(FormatSellFailure(result.FailureReason));
            return;
        }

        PopulateRows();
        ClearSelectedRow();
        previewRow = null;
        SelectRowByShopItemId(soldEntryId);
        RefreshMoneyText();
        RefreshActionButtonState();
        SetHelpText($"{soldItemName}を売却しました。");
    }

    private void ShowBuyMode()
    {
        ShowMode(ShopMode.Buy);
    }

    private void ShowSellMode()
    {
        ShowMode(ShopMode.Sell);
    }

    private void ShowMode(ShopMode mode)
    {
        if (currentMode == mode)
        {
            return;
        }

        currentMode = mode;
        ClearSelectedRow();
        previewRow = null;
        RefreshModeButtons();
        PopulateRows();
        SelectFirstRow();
        RefreshColumnHeaders();
        RefreshActionButtonState();
    }

    private void ShowAll()
    {
        ShowCategory(ShopCategory.All);
    }

    private void ShowConsumables()
    {
        ShowCategory(ShopCategory.Consumable);
    }

    private void ShowMaterials()
    {
        ShowCategory(ShopCategory.Material);
    }

    private void ShowEquipment()
    {
        ShowCategory(ShopCategory.Equipment);
    }

    private void ShowWeapons()
    {
        ShowCategory(ShopCategory.Weapon);
    }

    private void ShowArmor()
    {
        ShowCategory(ShopCategory.Armor);
    }

    private void ShowAccessories()
    {
        ShowCategory(ShopCategory.Accessory);
    }

    private void ShowOther()
    {
        ShowCategory(ShopCategory.Other);
    }

    private void ShowCategory(ShopCategory category)
    {
        if (currentCategory == category)
        {
            return;
        }

        currentCategory = category;
        ClearSelectedRow();
        previewRow = null;
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
        RefreshActionButtonState();
    }

    private void PopulateRows()
    {
        displayEntries.Clear();

        if (shopItemDatabase == null
            || itemDatabase == null
            || purchaseService == null
            || runSaveData == null
            || IsEquipmentCategory(currentCategory) && equipmentDatabase == null)
        {
            ClearRows();
            return;
        }

        if (currentMode == ShopMode.Sell)
        {
            PopulateSellEntries();
        }
        else
        {
            PopulateBuyEntries();
        }

        EnsureRowCount(displayEntries.Count);

        for (var i = 0; i < itemRows.Count; i++)
        {
            var row = itemRows[i];
            if (row == null)
            {
                continue;
            }

            if (i < displayEntries.Count)
            {
                var entry = displayEntries[i];
                row.Configure(
                    entry.ShopItemId,
                    entry.Icon,
                    entry.Name,
                    entry.Detail,
                    entry.Help,
                    entry.Stock,
                    entry.Owned,
                    entry.Price);
            }
            else
            {
                row.ClearRow();
            }
        }

        RefreshScrollArea();
        RefreshColumnHeaders();
        RefreshActionButtonState();
    }

    private void PopulateBuyEntries()
    {
        foreach (var shopItem in shopItemDatabase.Entries
            .Where(entry => entry != null && entry.AvailablePhase <= PreviewPhase)
            .OrderBy(entry => entry.SortOrder))
        {
            if (!TryCreateEntry(shopItem, out var entry))
            {
                continue;
            }

            displayEntries.Add(entry);
        }
    }

    private void PopulateSellEntries()
    {
        if (currentCategory == ShopCategory.All || currentCategory == ShopCategory.Consumable)
        {
            foreach (var stack in runSaveData.ConsumableItems.Where(stack => stack.Count > 0))
            {
                if (itemDatabase.TryGetById(stack.ItemId, out var item) && item.ItemType == ItemDataType.Consumable)
                {
                    for (var i = 0; i < stack.Count; i++)
                    {
                        displayEntries.Add(CreateSellItemEntry(item, stack.Count));
                    }
                }
            }

            if (currentCategory == ShopCategory.Consumable)
            {
                displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                return;
            }
        }

        if (currentCategory == ShopCategory.All || currentCategory == ShopCategory.Material)
        {
            foreach (var stack in runSaveData.Materials.Where(stack => stack.Count > 0))
            {
                if (itemDatabase.TryGetById(stack.ItemId, out var item) && item.ItemType == ItemDataType.Material)
                {
                    displayEntries.Add(CreateSellItemEntry(item, stack.Count));
                }
            }

            if (currentCategory == ShopCategory.Material)
            {
                displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                return;
            }
        }

        if (!ShowsEquipment(currentCategory))
        {
            displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return;
        }

        foreach (var ownedEquipment in runSaveData.OwnedEquipments)
        {
            if (!equipmentDatabase.TryGetById(ownedEquipment.EquipmentId, out var equipment))
            {
                continue;
            }

            if (MatchesEquipmentCategory(equipment, currentCategory))
            {
                displayEntries.Add(CreateSellEquipmentEntry(ownedEquipment, equipment));
            }
        }

        displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

    private bool TryCreateEntry(ShopItemData shopItem, out ShopDisplayEntry entry)
    {
        entry = default;

        if (shopItem.ProductType != ShopProductDataType.Item)
        {
            return TryCreateEquipmentEntry(shopItem, out entry);
        }

        if (!itemDatabase.TryGetById(shopItem.ProductId, out var item))
        {
            return false;
        }

        if (currentCategory == ShopCategory.Consumable && item.ItemType != ItemDataType.Consumable)
        {
            return false;
        }

        if (currentCategory == ShopCategory.Material && item.ItemType != ItemDataType.Material)
        {
            return false;
        }

        if (IsEquipmentCategory(currentCategory) || currentCategory == ShopCategory.Other)
        {
            return false;
        }

        entry = new ShopDisplayEntry(
            shopItem.ShopItemId,
            item.IconSprite,
            item.DisplayName,
            BuildItemDetail(item),
            $"{item.DisplayName}を購入します。",
            FormatStock(shopItem),
            GetOwnedItemCount(item).ToString(),
            $"{item.Price} G");
        return true;
    }

    private bool TryCreateEquipmentEntry(ShopItemData shopItem, out ShopDisplayEntry entry)
    {
        entry = default;

        if (!ShowsEquipment(currentCategory)
            || shopItem.ProductType != ShopProductDataType.Equipment
            || equipmentDatabase == null
            || !equipmentDatabase.TryGetById(shopItem.ProductId, out var equipment))
        {
            return false;
        }

        if (!MatchesEquipmentCategory(equipment, currentCategory))
        {
            return false;
        }

        entry = new ShopDisplayEntry(
            shopItem.ShopItemId,
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentDetail(equipment, null),
            $"{equipment.DisplayName}を購入します。",
            FormatStock(shopItem),
            GetOwnedEquipmentCount(equipment.EquipmentId).ToString(),
            $"{equipment.Price} G");
        return true;
    }

    private static ShopDisplayEntry CreateSellItemEntry(ItemData item, int ownedCount)
    {
        var sellPrice = item.Unsellable ? "不可" : $"{ShopSellService.CalculateSellPrice(item.Price)} G";
        return new ShopDisplayEntry(
            item.ItemId,
            item.IconSprite,
            item.DisplayName,
            BuildItemDetail(item),
            $"{item.DisplayName}を売却します。",
            UnlimitedStockText,
            ownedCount.ToString(),
            sellPrice);
    }

    private ShopDisplayEntry CreateSellEquipmentEntry(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment)
    {
        var sellPrice = equipment.Unsellable ? "不可" : $"{ShopSellService.CalculateSellPrice(equipment.Price)} G";
        return new ShopDisplayEntry(
            ownedEquipment.OwnedEquipmentInstanceId,
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentDetail(equipment, ownedEquipment),
            $"{equipment.DisplayName}を売却します。",
            UnlimitedStockText,
            "1",
            sellPrice);
    }

    private static string BuildItemDetail(ItemData item)
    {
        var detail = item.Description;
        var typeText = item.ItemType == ItemDataType.Consumable ? "消耗品" : "素材";

        if (!string.IsNullOrWhiteSpace(item.Category))
        {
            typeText += $" / {item.Category}";
        }

        return $"{detail}\n\n種別: {typeText}";
    }

    private string BuildEquipmentDetail(EquipmentData equipment, OwnedEquipmentSaveData ownedEquipment)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(equipment.Description))
        {
            lines.Add(equipment.Description);
            lines.Add(string.Empty);
        }

        lines.Add($"種別: {FormatEquipmentType(equipment)}");

        if (equipment.EquipmentType == EquipmentDataType.Weapon && !string.IsNullOrWhiteSpace(equipment.AttackAttribute))
        {
            lines.Add($"通常攻撃属性: {equipment.AttackAttribute}");
        }

        lines.Add($"上昇ステータス: {FormatStats(equipment.StatModifiers)}");
        lines.Add($"装備条件: {FormatEquippableBy(equipment)}");
        lines.Add($"固定スキル: {FormatSkillList(equipment.BaseSkillIds)}");

        if (ownedEquipment != null)
        {
            lines.Add(string.Empty);
            lines.Add($"レアリティ: {FormatRarity(ownedEquipment.Rarity)}");
            lines.Add($"ランダム補正: {FormatRandomModifiers(ownedEquipment.RandomModifiers)}");
            lines.Add($"ランダムスキル: {FormatSkillName(ownedEquipment.RandomSkillId, "なし")}");
        }
        else
        {
            lines.Add("ランダム性能: 入手時に抽選");
        }

        return string.Join("\n", lines);
    }

    private static string FormatEquipmentType(EquipmentData equipment)
    {
        return equipment.EquipmentType switch
        {
            EquipmentDataType.Weapon => $"武器 / {FormatWeaponType(equipment.WeaponType)}",
            EquipmentDataType.Armor => "防具",
            EquipmentDataType.Accessory => "アクセサリ",
            _ => equipment.EquipmentType.ToString()
        };
    }

    private static string FormatStats(BattleStats stats)
    {
        if (stats == null)
        {
            return "なし";
        }

        var parts = new List<string>();
        AddStat(parts, "HP", stats.Hp);
        AddStat(parts, "SP", stats.Sp);
        AddStat(parts, "攻撃", stats.Attack);
        AddStat(parts, "魔力", stats.Magic);
        AddStat(parts, "防御", stats.Defense);
        AddStat(parts, "素早さ", stats.Speed);

        if (!Mathf.Approximately(stats.CriticalRate, 0f))
        {
            parts.Add($"会心率 {FormatSignedPercent(stats.CriticalRate)}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "なし";
    }

    private static void AddStat(List<string> parts, string label, int value)
    {
        if (value != 0)
        {
            parts.Add($"{label} {FormatSigned(value)}");
        }
    }

    private string FormatEquippableBy(EquipmentData equipment)
    {
        if (equipment.EquipmentType == EquipmentDataType.Weapon)
        {
            var weaponType = FormatWeaponType(equipment.WeaponType);
            return equipment.EquippableBy.Count > 0
                ? $"{weaponType} / {FormatCharacterList(equipment.EquippableBy)}"
                : weaponType;
        }

        return equipment.EquippableBy.Count > 0 ? FormatCharacterList(equipment.EquippableBy) : "全員";
    }

    private string FormatCharacterList(IReadOnlyList<string> characterIds)
    {
        var names = new List<string>();

        foreach (var characterId in characterIds)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                continue;
            }

            if (characterDatabase != null
                && characterDatabase.TryGetById(characterId, out var character)
                && character != null
                && !string.IsNullOrWhiteSpace(character.DisplayName))
            {
                names.Add(character.DisplayName);
            }
            else
            {
                names.Add(characterId);
            }
        }

        return names.Count > 0 ? string.Join("、", names) : "全員";
    }

    private string FormatSkillList(IReadOnlyList<string> skillIds)
    {
        if (skillIds == null || skillIds.Count == 0)
        {
            return "なし";
        }

        var names = new List<string>();
        foreach (var skillId in skillIds)
        {
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                names.Add(FormatSkillName(skillId, skillId));
            }
        }

        return names.Count > 0 ? string.Join("、", names) : "なし";
    }

    private string FormatRandomModifiers(IReadOnlyList<EquipmentModifierSaveData> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return "なし";
        }

        var parts = new List<string>();
        foreach (var modifier in modifiers)
        {
            if (modifier != null)
            {
                parts.Add(FormatModifier(modifier));
            }
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "なし";
    }

    private string FormatSkillName(string skillId, string emptyText)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return emptyText;
        }

        if (skillDatabase != null
            && skillDatabase.TryGetById(skillId, out var skill)
            && skill != null
            && !string.IsNullOrWhiteSpace(skill.DisplayName))
        {
            return skill.DisplayName;
        }

        return skillId;
    }

    private static string FormatModifier(EquipmentModifierSaveData modifier)
    {
        var target = string.IsNullOrWhiteSpace(modifier.TargetId) || modifier.TargetId == "all"
            ? string.Empty
            : $"{modifier.TargetId} ";
        return $"{target}{FormatModifierType(modifier.ModifierType)} {FormatSigned(modifier.Amount)}{FormatModifierUnit(modifier.ModifierType)}";
    }

    private static string FormatModifierType(EquipmentModifierType modifierType)
    {
        return modifierType switch
        {
            EquipmentModifierType.Hp => "HP",
            EquipmentModifierType.Attack => "攻撃",
            EquipmentModifierType.Magic => "魔力",
            EquipmentModifierType.Defense => "防御",
            EquipmentModifierType.Speed => "素早さ",
            EquipmentModifierType.CriticalRate => "会心率",
            EquipmentModifierType.AttributeResistance => "属性耐性",
            EquipmentModifierType.StatusResistance => "状態異常耐性",
            EquipmentModifierType.DebuffResistance => "弱体耐性",
            _ => modifierType.ToString()
        };
    }

    private static string FormatModifierUnit(EquipmentModifierType modifierType)
    {
        return modifierType == EquipmentModifierType.CriticalRate
            || modifierType == EquipmentModifierType.AttributeResistance
            || modifierType == EquipmentModifierType.StatusResistance
            || modifierType == EquipmentModifierType.DebuffResistance
            ? "%"
            : string.Empty;
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string FormatSignedPercent(float value)
    {
        var percent = Mathf.RoundToInt(value * 100f);
        return $"{FormatSigned(percent)}%";
    }

    private static string FormatRarity(EquipmentRarity rarity)
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

    private static string FormatWeaponType(WeaponDataType weaponType)
    {
        return weaponType switch
        {
            WeaponDataType.Sword => "剣",
            WeaponDataType.Dagger => "短剣",
            WeaponDataType.Axe => "斧",
            WeaponDataType.Spear => "槍",
            WeaponDataType.Bow => "弓",
            WeaponDataType.Staff => "杖",
            _ => "なし"
        };
    }

    private string FormatStock(ShopItemData shopItem)
    {
        return shopItem.StockType == ShopStockDataType.Unlimited
            ? UnlimitedStockText
            : purchaseService.GetRemainingStock(runSaveData, shopItem).ToString();
    }

    private int GetOwnedItemCount(ItemData item)
    {
        if (runSaveData == null || item == null)
        {
            return 0;
        }

        return item.ItemType == ItemDataType.Consumable
            ? runSaveData.GetConsumableCount(item.ItemId)
            : runSaveData.GetMaterialCount(item.ItemId);
    }

    private int GetOwnedEquipmentCount(string equipmentId)
    {
        if (runSaveData == null || string.IsNullOrWhiteSpace(equipmentId))
        {
            return 0;
        }

        return runSaveData.OwnedEquipments.Count(equipment => equipment.EquipmentId == equipmentId);
    }

    private void InitializeGameState()
    {
        runSaveData = GameSession.GetOrCreate().RunSaveData;
        purchaseService = new ShopPurchaseService(shopItemDatabase, itemDatabase, equipmentDatabase);
        sellService = new ShopSellService(itemDatabase, equipmentDatabase);
    }

    private void ResolveOptionalReferences()
    {
        if (buyButton == null)
        {
            buyButton = FindDeep(transform, "BuyButton")?.GetComponent<Button>();
        }

        if (moneyText == null)
        {
            moneyText = FindDeep(transform, "MoneyPanel")?.Find("Value")?.GetComponent<TMP_Text>();
        }

        if (buyTabButton == null)
        {
            buyTabButton = FindDeep(transform, "BuyTab")?.GetComponent<Button>();
        }

        if (sellTabButton == null)
        {
            sellTabButton = FindDeep(transform, "SellTab")?.GetComponent<Button>();
        }

        if (allCategoryButton == null)
        {
            allCategoryButton = FindDeep(transform, "AllCategory")?.GetComponent<Button>();
        }

        if (weaponCategoryButton == null)
        {
            weaponCategoryButton = FindDeep(transform, "WeaponCategory")?.GetComponent<Button>();
        }

        if (armorCategoryButton == null)
        {
            armorCategoryButton = FindDeep(transform, "ArmorCategory")?.GetComponent<Button>();
        }

        if (accessoryCategoryButton == null)
        {
            accessoryCategoryButton = FindDeep(transform, "AccessoryCategory")?.GetComponent<Button>();
        }

        if (otherCategoryButton == null)
        {
            otherCategoryButton = FindDeep(transform, "OtherCategory")?.GetComponent<Button>();
        }

        if (actionButtonLabel == null)
        {
            actionButtonLabel = FindDeep(transform, "BuyButton")?.Find("Label")?.GetComponent<TMP_Text>();
        }

        if (stockHeaderText == null)
        {
            stockHeaderText = FindDeep(transform, "在庫Header")?.GetComponent<TMP_Text>();
        }

        if (ownedHeaderText == null)
        {
            ownedHeaderText = FindDeep(transform, "所持Header")?.GetComponent<TMP_Text>();
        }

        if (priceHeaderText == null)
        {
            priceHeaderText = FindDeep(transform, "価格Header")?.GetComponent<TMP_Text>();
        }

        var quantityPanel = FindDeep(transform, "QuantityPanel");
        if (quantityPanel != null)
        {
            quantityPanel.gameObject.SetActive(false);
        }
    }

    private void ApplyReferenceShopLayout()
    {
        var listPanel = FindDeep(transform, "ItemListPanel") as RectTransform;
        var detailPanel = FindDeep(transform, "DetailPanel") as RectTransform;
        if (listPanel == null || detailPanel == null)
        {
            return;
        }

        var windowSprite = listPanel.GetComponent<Image>()?.sprite;
        var hoverSprite = (FindDeep(transform, "BuyTab") as RectTransform)?.GetComponent<Image>()?.sprite ?? windowSprite;

        SetLeftTop(listPanel, 344f, -192f, 780f, 700f);
        SetRightTop(detailPanel, 34f, -192f, 640f, 700f);
        SetLeftTop(FindDeep(transform, "BuyTab") as RectTransform, 34f, -105f, 250f, 62f);
        SetLeftTop(FindDeep(transform, "SellTab") as RectTransform, 284f, -105f, 250f, 62f);
        SetRightTop(FindDeep(transform, "MoneyPanel") as RectTransform, 239f, -32f, 430f, 72f);
        SetRightTop(FindDeep(transform, "BackButton") as RectTransform, 34f, -32f, 160f, 72f);

        var categoryPanel = EnsureImagePanel("CategoryPanel", windowSprite);
        SetLeftTop(categoryPanel, 34f, -192f, 280f, 700f);
        EnsureCategoryTitle(categoryPanel);

        allCategoryButton ??= EnsureCategoryButton(categoryPanel, windowSprite, hoverSprite, "AllCategory", "すべて", -102f);
        consumableCategoryButton = MoveOrCreateCategoryButton(categoryPanel, windowSprite, hoverSprite, consumableCategoryButton, "ConsumableCategory", "消耗品", -162f);
        materialCategoryButton = MoveOrCreateCategoryButton(categoryPanel, windowSprite, hoverSprite, materialCategoryButton, "MaterialCategory", "素材", -222f);
        weaponCategoryButton ??= EnsureCategoryButton(categoryPanel, windowSprite, hoverSprite, "WeaponCategory", "武器", -282f);
        armorCategoryButton ??= EnsureCategoryButton(categoryPanel, windowSprite, hoverSprite, "ArmorCategory", "防具", -342f);
        accessoryCategoryButton ??= EnsureCategoryButton(categoryPanel, windowSprite, hoverSprite, "AccessoryCategory", "装飾品", -402f);
        otherCategoryButton ??= EnsureCategoryButton(categoryPanel, windowSprite, hoverSprite, "OtherCategory", "その他", -462f);

        if (equipmentCategoryButton != null)
        {
            equipmentCategoryButton.gameObject.SetActive(false);
        }

        var categoryLabel = FindDeep(listPanel, "CategoryLabel");
        if (categoryLabel != null)
        {
            categoryLabel.gameObject.SetActive(false);
        }

        SetStretch(FindDeep(transform, "ItemRowViewport") as RectTransform, new Vector2(38f, 36f), new Vector2(-48f, -148f));
        SetLeftTop(FindDeep(transform, "ItemScrollbar") as RectTransform, 732.5f, -139f, 27f, 510f);
        SetAnchor(FindDeep(transform, "DetailIcon") as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -148f), new Vector2(116f, 116f));
        SetTopStretch(FindDeep(transform, "DetailTitle") as RectTransform, 250f, 54f, 116f, 54f);
        SetTopStretch(FindDeep(transform, "DetailBody") as RectTransform, 68f, 64f, 190f, 264f);
        SetAnchor(FindDeep(transform, "BuyButton") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(360f, 72f));
    }

    private RectTransform EnsureImagePanel(string name, Sprite sprite)
    {
        var existing = FindDeep(transform, name) as RectTransform;
        if (existing != null)
        {
            return existing;
        }

        var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        var image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        return rect;
    }

    private void EnsureCategoryTitle(RectTransform categoryPanel)
    {
        if (FindDeep(categoryPanel, "PanelTitle") != null)
        {
            return;
        }

        var title = CreateRuntimeText("PanelTitle", categoryPanel, "カテゴリ", 22f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetAnchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -42f), new Vector2(150f, 34f));
    }

    private Button MoveOrCreateCategoryButton(RectTransform parent, Sprite windowSprite, Sprite hoverSprite, Button button, string name, string label, float y)
    {
        if (button == null)
        {
            return EnsureCategoryButton(parent, windowSprite, hoverSprite, name, label, y);
        }

        button.transform.SetParent(parent, false);
        button.name = name;
        SetAnchor(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, y), new Vector2(256f, 58f));
        SetCategoryLabel(button.transform, label);
        return button;
    }

    private Button EnsureCategoryButton(RectTransform parent, Sprite windowSprite, Sprite hoverSprite, string name, string label, float y)
    {
        var existing = FindDeep(parent, name)?.GetComponent<Button>();
        if (existing != null)
        {
            return MoveOrCreateCategoryButton(parent, windowSprite, hoverSprite, existing, name, label, y);
        }

        var rect = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetAnchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, y), new Vector2(256f, 58f));

        var image = rect.GetComponent<Image>();
        image.sprite = windowSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var labelText = CreateRuntimeText("Label", rect, label, 24f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetStretch(labelText.rectTransform, new Vector2(38f, 8f), new Vector2(-24f, -8f));
        return rect.GetComponent<Button>();
    }

    private void SetCategoryLabel(Transform buttonTransform, string label)
    {
        var labelTransform = buttonTransform.Find("Label") as RectTransform;
        var labelText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (labelText == null)
        {
            labelText = CreateRuntimeText("Label", buttonTransform, label, 24f, TextAlignmentOptions.MidlineLeft, TextColor);
            labelTransform = labelText.rectTransform;
        }

        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        SetStretch(labelTransform, new Vector2(38f, 8f), new Vector2(-24f, -8f));
    }

    private static TMP_Text CreateRuntimeText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var text = rect.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetLeftTop(RectTransform rect, float left, float top, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        SetAnchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left + width * 0.5f, top - height * 0.5f), new Vector2(width, height));
    }

    private static void SetRightTop(RectTransform rect, float right, float top, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        SetAnchor(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-(right + width * 0.5f), top - height * 0.5f), new Vector2(width, height));
    }

    private static void SetAnchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void RegisterCategoryButtons()
    {
        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.AddListener(ShowAll);
        }

        if (consumableCategoryButton != null)
        {
            consumableCategoryButton.onClick.AddListener(ShowConsumables);
        }

        if (materialCategoryButton != null)
        {
            materialCategoryButton.onClick.AddListener(ShowMaterials);
        }

        if (equipmentCategoryButton != null)
        {
            equipmentCategoryButton.onClick.AddListener(ShowEquipment);
        }

        if (weaponCategoryButton != null)
        {
            weaponCategoryButton.onClick.AddListener(ShowWeapons);
        }

        if (armorCategoryButton != null)
        {
            armorCategoryButton.onClick.AddListener(ShowArmor);
        }

        if (accessoryCategoryButton != null)
        {
            accessoryCategoryButton.onClick.AddListener(ShowAccessories);
        }

        if (otherCategoryButton != null)
        {
            otherCategoryButton.onClick.AddListener(ShowOther);
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(SubmitCurrentItem);
        }
    }

    private void RegisterModeButtons()
    {
        if (buyTabButton != null)
        {
            buyTabButton.onClick.AddListener(ShowBuyMode);
        }

        if (sellTabButton != null)
        {
            sellTabButton.onClick.AddListener(ShowSellMode);
        }
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(allCategoryButton, currentCategory == ShopCategory.All);
        SetCategoryButtonState(consumableCategoryButton, currentCategory == ShopCategory.Consumable);
        SetCategoryButtonState(materialCategoryButton, currentCategory == ShopCategory.Material);
        SetCategoryButtonState(equipmentCategoryButton, currentCategory == ShopCategory.Equipment);
        SetCategoryButtonState(weaponCategoryButton, currentCategory == ShopCategory.Weapon);
        SetCategoryButtonState(armorCategoryButton, currentCategory == ShopCategory.Armor);
        SetCategoryButtonState(accessoryCategoryButton, currentCategory == ShopCategory.Accessory);
        SetCategoryButtonState(otherCategoryButton, currentCategory == ShopCategory.Other);
    }

    private void RefreshModeButtons()
    {
        SetCategoryButtonState(buyTabButton, currentMode == ShopMode.Buy);
        SetCategoryButtonState(sellTabButton, currentMode == ShopMode.Sell);

        if (actionButtonLabel != null)
        {
            actionButtonLabel.text = currentMode == ShopMode.Buy ? "購入する" : "売却する";
        }

        RefreshActionButtonState();
    }

    private void RefreshColumnHeaders()
    {
        if (stockHeaderText != null)
        {
            stockHeaderText.text = currentMode == ShopMode.Buy ? "在庫" : string.Empty;
        }

        if (ownedHeaderText != null)
        {
            ownedHeaderText.text = "所持";
        }

        if (priceHeaderText != null)
        {
            priceHeaderText.text = currentMode == ShopMode.Buy ? "価格" : "売値";
        }
    }

    private static void SetCategoryButtonState(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        if (button.TryGetComponent<WindowHoverSpriteView>(out var hover))
        {
            hover.SetSelected(selected);
        }

        var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.color = selected ? AccentTextColor : TextColor;
        }
    }

    private void ClearRows()
    {
        foreach (var row in itemRows)
        {
            if (row != null)
            {
                row.ClearRow();
            }
        }

        RefreshScrollArea();
    }

    private void EnsureRowCount(int count)
    {
        if (itemRowPrefab == null || itemRowContent == null)
        {
            return;
        }

        while (itemRows.Count < count)
        {
            var row = Instantiate(itemRowPrefab, itemRowContent);
            row.name = "ItemRow_" + (itemRows.Count + 1).ToString("00");
            SetupRowTransform(row.GetComponent<RectTransform>(), itemRows.Count);
            row.Initialize(this);
            row.ClearRow();
            itemRows.Add(row);
        }
    }

    private static void SetupRowTransform(RectTransform rowRect, int index)
    {
        if (rowRect == null)
        {
            return;
        }

        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, -25f - index * RowStride);
        rowRect.sizeDelta = new Vector2(690f, 50f);
    }

    private void RefreshScrollArea()
    {
        if (itemScrollRect == null || itemRowViewport == null || itemRowContent == null)
        {
            return;
        }

        var contentHeight = displayEntries.Count * RowStride;
        var viewportHeight = itemRowViewport.rect.height;
        var scrollable = contentHeight > viewportHeight + 0.5f;
        var contentAreaHeight = Mathf.Max(contentHeight, viewportHeight);

        itemRowContent.sizeDelta = new Vector2(itemRowContent.sizeDelta.x, contentAreaHeight);
        itemRowContent.anchoredPosition = Vector2.zero;
        itemScrollRect.verticalNormalizedPosition = 1f;
        itemScrollRect.vertical = scrollable;
        itemScrollRect.enabled = true;

        if (itemScrollRect.verticalScrollbar != null)
        {
            itemScrollRect.verticalScrollbar.gameObject.SetActive(scrollable);
            itemScrollRect.verticalScrollbar.value = 1f;
            itemScrollRect.verticalScrollbar.size = contentAreaHeight > 0f
                ? Mathf.Clamp01(viewportHeight / contentAreaHeight)
                : 1f;
        }
    }

    private void SelectFirstRow()
    {
        var firstActiveRow = itemRows.FirstOrDefault(row => row != null && row.gameObject.activeSelf);
        if (firstActiveRow != null)
        {
            Select(firstActiveRow);
        }
        else
        {
            previewRow = null;
            ClearDetail();
        }
    }

    private void SelectRowByShopItemId(string shopItemId)
    {
        var row = itemRows.FirstOrDefault(entry => entry != null
            && entry.gameObject.activeSelf
            && entry.ShopItemId == shopItemId);

        if (row != null)
        {
            Select(row);
            return;
        }

        SelectFirstRow();
    }

    private void ClearSelectedRow()
    {
        if (selectedRow != null)
        {
            selectedRow.SetHighlighted(false);
            selectedRow = null;
        }

        RefreshActionButtonState();
    }

    private void ClearDetail()
    {
        if (detailTitleText != null)
        {
            detailTitleText.text = string.Empty;
        }

        if (detailIconImage != null)
        {
            detailIconImage.sprite = null;
            detailIconImage.enabled = false;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = string.Empty;
        }

        if (detailStockText != null)
        {
            detailStockText.text = string.Empty;
        }

        if (detailOwnedText != null)
        {
            detailOwnedText.text = string.Empty;
        }

        if (detailPriceText != null)
        {
            detailPriceText.text = string.Empty;
        }

        if (helpText != null)
        {
            helpText.text = string.Empty;
        }
    }

    private void RefreshMoneyText()
    {
        if (moneyText != null && runSaveData != null)
        {
            moneyText.text = $"{runSaveData.Money:N0} G";
        }
    }

    private void RefreshActionButtonState()
    {
        if (buyButton == null)
        {
            return;
        }

        var canSubmit = CanSubmitSelectedItem();
        buyButton.interactable = canSubmit;

        if (actionButtonLabel != null)
        {
            actionButtonLabel.color = canSubmit ? AccentTextColor : TextColor;
        }
    }

    private bool CanSubmitSelectedItem()
    {
        if (selectedRow == null || runSaveData == null)
        {
            return false;
        }

        if (currentMode == ShopMode.Buy)
        {
            return purchaseService != null
                && purchaseService.GetQuote(runSaveData, selectedRow.ShopItemId).CanPurchase;
        }

        if (sellService == null)
        {
            return false;
        }

        return ShouldSellSelectedAsEquipment(selectedRow.ShopItemId)
            ? sellService.GetEquipmentQuote(runSaveData, selectedRow.ShopItemId).CanSell
            : sellService.GetItemQuote(runSaveData, selectedRow.ShopItemId).CanSell;
    }

    private bool ShouldSellSelectedAsEquipment(string entryId)
    {
        if (IsEquipmentCategory(currentCategory))
        {
            return true;
        }

        if (currentCategory != ShopCategory.All || runSaveData == null)
        {
            return false;
        }

        return runSaveData.OwnedEquipments.Any(equipment => equipment.OwnedEquipmentInstanceId == entryId);
    }

    private static bool ShowsEquipment(ShopCategory category)
    {
        return category == ShopCategory.All || IsEquipmentCategory(category);
    }

    private static bool IsEquipmentCategory(ShopCategory category)
    {
        return category == ShopCategory.Equipment
            || category == ShopCategory.Weapon
            || category == ShopCategory.Armor
            || category == ShopCategory.Accessory;
    }

    private static bool MatchesEquipmentCategory(EquipmentData equipment, ShopCategory category)
    {
        if (equipment == null)
        {
            return false;
        }

        switch (category)
        {
            case ShopCategory.All:
            case ShopCategory.Equipment:
                return true;
            case ShopCategory.Weapon:
                return equipment.EquipmentType == EquipmentDataType.Weapon;
            case ShopCategory.Armor:
                return equipment.EquipmentType == EquipmentDataType.Armor;
            case ShopCategory.Accessory:
                return equipment.EquipmentType == EquipmentDataType.Accessory;
            default:
                return false;
        }
    }

    private string FormatDetailHelp(ShopItemRowView row)
    {
        if (row == null)
        {
            return string.Empty;
        }

        if (row != selectedRow)
        {
            return $"{row.ItemName}の詳細です。クリックで対象にします。";
        }

        if (currentMode == ShopMode.Buy)
        {
            var quote = purchaseService != null && runSaveData != null
                ? purchaseService.GetQuote(runSaveData, row.ShopItemId)
                : default;
            return quote.CanPurchase
                ? $"{row.ItemName}を購入対象にしています。"
                : FormatPurchaseFailure(quote.FailureReason);
        }

        var sellQuote = ShouldSellSelectedAsEquipment(row.ShopItemId)
            ? sellService?.GetEquipmentQuote(runSaveData, row.ShopItemId) ?? default
            : sellService?.GetItemQuote(runSaveData, row.ShopItemId) ?? default;
        return sellQuote.CanSell
            ? $"{row.ItemName}を売却対象にしています。"
            : FormatSellFailure(sellQuote.FailureReason);
    }

    private void SetHelpText(string message)
    {
        if (helpText != null)
        {
            helpText.text = message;
        }
    }

    private static string FormatPurchaseFailure(ShopPurchaseFailureReason reason)
    {
        return reason switch
        {
            ShopPurchaseFailureReason.ShopItemNotFound => "商品データが見つかりません。",
            ShopPurchaseFailureReason.ProductNotFound => "商品内容のデータが見つかりません。",
            ShopPurchaseFailureReason.NotAvailableInCurrentPhase => "この商品はまだ購入できません。",
            ShopPurchaseFailureReason.SoldOut => "在庫がありません。",
            ShopPurchaseFailureReason.NotEnoughMoney => "所持金が足りません。",
            ShopPurchaseFailureReason.InventoryFull => $"消耗品は{ShopPurchaseService.MaxConsumableCount}個まで所持できます。",
            _ => "購入できません。"
        };
    }

    private static string FormatSellFailure(ShopSellFailureReason reason)
    {
        return reason switch
        {
            ShopSellFailureReason.ProductNotFound => "所持品のデータが見つかりません。",
            ShopSellFailureReason.NotOwned => "所持していません。",
            ShopSellFailureReason.Unsellable => "この所持品は売却できません。",
            _ => "売却できません。"
        };
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            var result = FindDeep(child, targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private readonly struct ShopDisplayEntry
    {
        public ShopDisplayEntry(
            string shopItemId,
            Sprite icon,
            string name,
            string detail,
            string help,
            string stock,
            string owned,
            string price)
        {
            ShopItemId = shopItemId ?? string.Empty;
            Icon = icon;
            Name = name;
            Detail = detail;
            Help = help;
            Stock = stock;
            Owned = owned;
            Price = price;
        }

        public string ShopItemId { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Detail { get; }
        public string Help { get; }
        public string Stock { get; }
        public string Owned { get; }
        public string Price { get; }
    }

    private enum ShopCategory
    {
        All,
        Consumable,
        Material,
        Equipment,
        Weapon,
        Armor,
        Accessory,
        Other
    }

    private enum ShopMode
    {
        Buy,
        Sell
    }

    private static void SetEventSystemSelection(GameObject target)
    {
        if (EventSystem.current != null
            && target != null
            && EventSystem.current.currentSelectedGameObject != target
            && !EventSystem.current.alreadySelecting)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
