using System.Collections.Generic;
using System.Linq;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopScreenPreviewController : MonoBehaviour
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
    [SerializeField] private ShopItemRowView itemRowPrefab;
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private RectTransform itemRowViewport;
    [SerializeField] private RectTransform itemRowContent;
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Button consumableCategoryButton;
    [SerializeField] private Button materialCategoryButton;
    [SerializeField] private Button equipmentCategoryButton;
    [SerializeField] private ShopItemDatabase shopItemDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [Min(0)]
    [SerializeField] private int previewMoney = 12480;

    private ShopCategory currentCategory = ShopCategory.Consumable;
    private ShopMode currentMode = ShopMode.Buy;
    private ShopItemRowView selectedRow;
    private ShopItemRowView previewRow;
    private RunSaveData previewSaveData;
    private ShopPurchaseService purchaseService;
    private ShopSellService sellService;
    private readonly List<ShopDisplayEntry> displayEntries = new();
    private readonly List<ShopItemRowView> itemRows = new();

    private void Awake()
    {
        InitializePreviewState();
        ResolveOptionalReferences();
        RegisterCategoryButtons();
        RegisterModeButtons();
        RefreshModeButtons();
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
        RefreshMoneyText();
    }

    private void OnDestroy()
    {
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
            helpText.text = row.HelpText;
        }
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
        if (selectedRow == null || purchaseService == null || previewSaveData == null)
        {
            SetHelpText("購入する商品を選んでください。");
            return;
        }

        var purchasedShopItemId = selectedRow.ShopItemId;
        var purchasedItemName = selectedRow.ItemName;
        var result = purchaseService.TryPurchase(previewSaveData, purchasedShopItemId);
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
        SetHelpText($"{purchasedItemName}を購入しました。");
    }

    public void SellCurrentItem()
    {
        if (selectedRow == null || sellService == null || previewSaveData == null)
        {
            SetHelpText("売却する所持品を選んでください。");
            return;
        }

        var soldEntryId = selectedRow.ShopItemId;
        var soldItemName = selectedRow.ItemName;
        var result = currentCategory == ShopCategory.Equipment
            ? sellService.TrySellEquipment(previewSaveData, soldEntryId)
            : sellService.TrySellItem(previewSaveData, soldEntryId);
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
    }

    private void PopulateRows()
    {
        displayEntries.Clear();

        if (shopItemDatabase == null
            || itemDatabase == null
            || purchaseService == null
            || previewSaveData == null
            || currentCategory == ShopCategory.Equipment && equipmentDatabase == null)
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
        if (currentCategory == ShopCategory.Consumable)
        {
            foreach (var stack in previewSaveData.ConsumableItems.Where(stack => stack.Count > 0))
            {
                if (itemDatabase.TryGetById(stack.ItemId, out var item) && item.ItemType == ItemDataType.Consumable)
                {
                    displayEntries.Add(CreateSellItemEntry(item, stack.Count));
                }
            }

            displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return;
        }

        if (currentCategory == ShopCategory.Material)
        {
            foreach (var stack in previewSaveData.Materials.Where(stack => stack.Count > 0))
            {
                if (itemDatabase.TryGetById(stack.ItemId, out var item) && item.ItemType == ItemDataType.Material)
                {
                    displayEntries.Add(CreateSellItemEntry(item, stack.Count));
                }
            }

            displayEntries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return;
        }

        foreach (var ownedEquipment in previewSaveData.OwnedEquipments)
        {
            if (!equipmentDatabase.TryGetById(ownedEquipment.EquipmentId, out var equipment))
            {
                continue;
            }

            displayEntries.Add(CreateSellEquipmentEntry(ownedEquipment, equipment));
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

        if (currentCategory == ShopCategory.Equipment)
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

        if (currentCategory != ShopCategory.Equipment
            || shopItem.ProductType != ShopProductDataType.Equipment
            || equipmentDatabase == null
            || !equipmentDatabase.TryGetById(shopItem.ProductId, out var equipment))
        {
            return false;
        }

        entry = new ShopDisplayEntry(
            shopItem.ShopItemId,
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentDetail(equipment),
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

    private static ShopDisplayEntry CreateSellEquipmentEntry(OwnedEquipmentSaveData ownedEquipment, EquipmentData equipment)
    {
        var sellPrice = equipment.Unsellable ? "不可" : $"{ShopSellService.CalculateSellPrice(equipment.Price)} G";
        return new ShopDisplayEntry(
            ownedEquipment.OwnedEquipmentInstanceId,
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentDetail(equipment),
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

    private static string BuildEquipmentDetail(EquipmentData equipment)
    {
        var detail = equipment.Description;
        var typeText = equipment.EquipmentType switch
        {
            EquipmentDataType.Weapon => $"武器 / {FormatWeaponType(equipment.WeaponType)}",
            EquipmentDataType.Armor => "防具",
            EquipmentDataType.Accessory => "アクセサリ",
            _ => equipment.EquipmentType.ToString()
        };

        return $"{detail}\n\n種別: {typeText}";
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
            : purchaseService.GetRemainingStock(previewSaveData, shopItem).ToString();
    }

    private int GetOwnedItemCount(ItemData item)
    {
        if (previewSaveData == null || item == null)
        {
            return 0;
        }

        return item.ItemType == ItemDataType.Consumable
            ? previewSaveData.GetConsumableCount(item.ItemId)
            : previewSaveData.GetMaterialCount(item.ItemId);
    }

    private int GetOwnedEquipmentCount(string equipmentId)
    {
        if (previewSaveData == null || string.IsNullOrWhiteSpace(equipmentId))
        {
            return 0;
        }

        return previewSaveData.OwnedEquipments.Count(equipment => equipment.EquipmentId == equipmentId);
    }

    private void InitializePreviewState()
    {
        previewSaveData = RunSaveData.CreateNew();
        previewSaveData.AddMoney(previewMoney);
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

        if (actionButtonLabel == null)
        {
            actionButtonLabel = FindDeep(transform, "BuyButton")?.Find("Label")?.GetComponent<TMP_Text>();
        }
    }

    private void RegisterCategoryButtons()
    {
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
        SetCategoryButtonState(consumableCategoryButton, currentCategory == ShopCategory.Consumable);
        SetCategoryButtonState(materialCategoryButton, currentCategory == ShopCategory.Material);
        SetCategoryButtonState(equipmentCategoryButton, currentCategory == ShopCategory.Equipment);
    }

    private void RefreshModeButtons()
    {
        SetCategoryButtonState(buyTabButton, currentMode == ShopMode.Buy);
        SetCategoryButtonState(sellTabButton, currentMode == ShopMode.Sell);

        if (actionButtonLabel != null)
        {
            actionButtonLabel.text = currentMode == ShopMode.Buy ? "購入" : "売却";
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
        rowRect.sizeDelta = new Vector2(710f, 50f);
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
        if (moneyText != null && previewSaveData != null)
        {
            moneyText.text = $"{previewSaveData.Money:N0} G";
        }
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
        Consumable,
        Material,
        Equipment
    }

    private enum ShopMode
    {
        Buy,
        Sell
    }
}
