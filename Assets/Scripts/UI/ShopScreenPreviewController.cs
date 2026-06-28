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
    [SerializeField] private TMP_Text detailTagText;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private EquipmentDetailPanelView equipmentDetailPanelView;
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
    [SerializeField] private Button weaponCategoryButton;
    [SerializeField] private Button armorCategoryButton;
    [SerializeField] private Button accessoryCategoryButton;
    [SerializeField] private ShopItemDatabase shopItemDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private SkillDatabase skillDatabase;

    private ShopCategory currentCategory = ShopCategory.All;
    private ShopMode currentMode = ShopMode.Buy;
    private ShopItemRowView selectedRow;
    private ShopItemRowView previewRow;
    private RunSaveData runSaveData;
    private ShopPurchaseService purchaseService;
    private ShopSellService sellService;
    private readonly List<ShopDisplayEntry> displayEntries = new();
    private readonly List<ShopItemRowView> itemRows = new();
    private readonly Dictionary<string, EquipmentDetailData> equipmentDetailsByEntryId = new();
    private bool initialized;

    private void Awake()
    {
        InitializeGameState();
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

        if (detailTagText != null)
        {
            detailTagText.text = row.TagText;
        }

        if (detailIconImage != null)
        {
            detailIconImage.sprite = row.IconSprite;
            detailIconImage.enabled = row.IconSprite != null;
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = row.DescriptionText;
        }

        if (equipmentDetailsByEntryId.TryGetValue(row.ShopItemId, out var equipmentDetail))
        {
            equipmentDetailPanelView?.Show(equipmentDetail);
        }
        else
        {
            equipmentDetailPanelView?.Hide();
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
        equipmentDetailsByEntryId.Clear();

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
                    entry.Tag,
                    entry.Description,
                    entry.EquipmentDetail,
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

        if (IsEquipmentCategory(currentCategory))
        {
            return false;
        }

        entry = new ShopDisplayEntry(
            shopItem.ShopItemId,
            item.IconSprite,
            item.DisplayName,
            BuildItemTag(item),
            item.Description,
            string.Empty,
            $"{item.DisplayName}を購入します。",
            FormatStock(shopItem),
            FormatOwnedCount(GetOwnedItemCount(item)),
            item.Price.ToString());
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
            BuildEquipmentTag(equipment, EquipmentRarity.Common),
            equipment.Description,
            string.Empty,
            $"{equipment.DisplayName}を購入します。",
            FormatStock(shopItem),
            FormatOwnedCount(GetOwnedEquipmentCount(equipment.EquipmentId)),
            equipment.Price.ToString());
        equipmentDetailsByEntryId[shopItem.ShopItemId] = BuildEquipmentDetailData(equipment, null);
        return true;
    }

    private static ShopDisplayEntry CreateSellItemEntry(ItemData item, int ownedCount)
    {
        var sellPrice = item.Unsellable ? "不可" : ShopSellService.CalculateSellPrice(item.Price).ToString();
        return new ShopDisplayEntry(
            item.ItemId,
            item.IconSprite,
            item.DisplayName,
            BuildItemTag(item),
            item.Description,
            string.Empty,
            $"{item.DisplayName}を売却します。",
            string.Empty,
            FormatOwnedCount(ownedCount),
            sellPrice);
    }

    private ShopDisplayEntry CreateSellEquipmentEntry(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment)
    {
        var sellPrice = equipment.Unsellable ? "不可" : ShopSellService.CalculateSellPrice(equipment.Price).ToString();
        var entry = new ShopDisplayEntry(
            ownedEquipment.OwnedEquipmentInstanceId,
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentTag(equipment, ownedEquipment.Rarity),
            equipment.Description,
            string.Empty,
            $"{equipment.DisplayName}を売却します。",
            string.Empty,
            FormatOwnedCount(1),
            sellPrice);
        equipmentDetailsByEntryId[ownedEquipment.OwnedEquipmentInstanceId] = BuildEquipmentDetailData(equipment, ownedEquipment);
        return entry;
    }

    private static string BuildItemTag(ItemData item)
    {
        return MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatItemType(item.ItemType));
    }

    private static string BuildEquipmentTag(EquipmentData equipment, EquipmentRarity rarity)
    {
        return MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatEquipmentType(equipment))
            + MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatRarity(rarity));
    }

    private EquipmentDetailData BuildEquipmentDetailData(EquipmentData equipment, OwnedEquipmentSaveData ownedEquipment)
    {
        var detail = new EquipmentDetailData
        {
            Description = equipment.Description
        };

        AddStatData(detail.Stats, "HP", (equipment.StatModifiers?.Hp ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Hp));
        AddStatData(detail.Stats, "SP", equipment.StatModifiers?.Sp ?? 0);
        AddStatData(detail.Stats, "攻撃", (equipment.StatModifiers?.Attack ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Attack));
        AddStatData(detail.Stats, "魔力", (equipment.StatModifiers?.Magic ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Magic));
        AddStatData(detail.Stats, "防御", (equipment.StatModifiers?.Defense ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Defense));
        AddStatData(detail.Stats, "素早さ", (equipment.StatModifiers?.Speed ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Speed));
        var criticalRate = Mathf.RoundToInt((equipment.StatModifiers?.CriticalRate ?? 0f) * 100f) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.CriticalRate);
        AddStatData(detail.Stats, "会心率", criticalRate, "%");

        foreach (var skillId in equipment.BaseSkillIds)
        {
            var skillName = FormatSkillName(skillId, string.Empty);
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(skillName));
            }
        }

        var randomSkillName = FormatSkillName(ownedEquipment?.RandomSkillId, string.Empty);
        if (!string.IsNullOrWhiteSpace(randomSkillName))
        {
            detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(randomSkillName));
        }

        foreach (var trait in equipment.BaseTraits)
        {
            if (trait != null)
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(FormatBaseTrait(trait)));
            }
        }

        if (ownedEquipment != null)
        {
            foreach (var modifier in ownedEquipment.RandomModifiers)
            {
                if (modifier != null && !IsStatModifier(modifier.ModifierType))
                {
                    detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(FormatModifier(modifier)));
                }
            }
        }

        return detail;
    }

    private static void AddStatData(List<EquipmentDetailStat> stats, string label, int value, string suffix = "")
    {
        var text = value != 0 ? $"{FormatSigned(value)}{suffix}" : "-";
        stats.Add(new EquipmentDetailStat(label, text, value.CompareTo(0)));
    }

    private static int GetRandomModifierAmount(OwnedEquipmentSaveData ownedEquipment, EquipmentModifierType modifierType)
    {
        if (ownedEquipment == null)
        {
            return 0;
        }

        var amount = 0;
        foreach (var modifier in ownedEquipment.RandomModifiers)
        {
            if (modifier != null && modifier.ModifierType == modifierType)
            {
                amount += modifier.Amount;
            }
        }

        return amount;
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

    private static bool IsStatModifier(EquipmentModifierType modifierType)
    {
        return modifierType == EquipmentModifierType.Hp
            || modifierType == EquipmentModifierType.Attack
            || modifierType == EquipmentModifierType.Magic
            || modifierType == EquipmentModifierType.Defense
            || modifierType == EquipmentModifierType.Speed
            || modifierType == EquipmentModifierType.CriticalRate;
    }

    private static string FormatModifier(EquipmentModifierSaveData modifier)
    {
        var target = FormatModifierTarget(modifier.TargetId);
        var sign = modifier.Amount >= 0 ? "+" : string.Empty;
        return string.IsNullOrWhiteSpace(target)
            ? $"{FormatModifierType(modifier.ModifierType)} {sign}{modifier.Amount}{FormatModifierUnit(modifier.ModifierType)}"
            : $"{target}{FormatModifierType(modifier.ModifierType)} {sign}{modifier.Amount}{FormatModifierUnit(modifier.ModifierType)}";
    }

    private static string FormatBaseTrait(EquipmentBaseTraitData trait)
    {
        return FormatBaseTraitType(trait.TraitType);
    }

    private static string FormatModifierType(EquipmentModifierType modifierType)
    {
        return modifierType switch
        {
            EquipmentModifierType.AttributeResistance => "属性耐性",
            EquipmentModifierType.StatusResistance => "状態異常耐性",
            EquipmentModifierType.DebuffResistance => "弱体耐性",
            _ => modifierType.ToString()
        };
    }

    private static string FormatBaseTraitType(EquipmentBaseTraitType traitType)
    {
        return traitType switch
        {
            EquipmentBaseTraitType.AttributeResistance => "属性耐性",
            EquipmentBaseTraitType.StatusResistance => "状態異常耐性",
            EquipmentBaseTraitType.DebuffResistance => "弱体耐性",
            _ => traitType.ToString()
        };
    }

    private static string FormatModifierUnit(EquipmentModifierType modifierType)
    {
        return modifierType == EquipmentModifierType.AttributeResistance
            || modifierType == EquipmentModifierType.StatusResistance
            || modifierType == EquipmentModifierType.DebuffResistance
            ? "%"
            : string.Empty;
    }

    private static string FormatModifierTarget(string targetId)
    {
        return string.IsNullOrWhiteSpace(targetId) || targetId == "all"
            ? string.Empty
            : $"{targetId} ";
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private string FormatStock(ShopItemData shopItem)
    {
        return shopItem.StockType == ShopStockDataType.Unlimited
            ? UnlimitedStockText
            : purchaseService.GetRemainingStock(runSaveData, shopItem).ToString();
    }

    private static string FormatOwnedCount(int count)
    {
        return count <= 0 ? "-" : count.ToString();
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
        SetCategoryButtonState(weaponCategoryButton, currentCategory == ShopCategory.Weapon);
        SetCategoryButtonState(armorCategoryButton, currentCategory == ShopCategory.Armor);
        SetCategoryButtonState(accessoryCategoryButton, currentCategory == ShopCategory.Accessory);
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

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -index * RowStride);
        rowRect.sizeDelta = new Vector2(0f, 50f);
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

        if (detailTagText != null)
        {
            detailTagText.text = string.Empty;
        }

        if (detailIconImage != null)
        {
            detailIconImage.sprite = null;
            detailIconImage.enabled = false;
        }

        equipmentDetailPanelView?.Hide();

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = string.Empty;
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
            moneyText.text = $"{runSaveData.Money:N0}";
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
        return category == ShopCategory.Weapon
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

    private readonly struct ShopDisplayEntry
    {
        public ShopDisplayEntry(
            string shopItemId,
            Sprite icon,
            string name,
            string tag,
            string description,
            string equipmentDetail,
            string help,
            string stock,
            string owned,
            string price)
        {
            ShopItemId = shopItemId ?? string.Empty;
            Icon = icon;
            Name = name;
            Tag = tag ?? string.Empty;
            Description = description ?? string.Empty;
            EquipmentDetail = equipmentDetail;
            Help = help;
            Stock = stock;
            Owned = owned;
            Price = price;
        }

        public string ShopItemId { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Tag { get; }
        public string Description { get; }
        public string EquipmentDetail { get; }
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
        Weapon,
        Armor,
        Accessory
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
