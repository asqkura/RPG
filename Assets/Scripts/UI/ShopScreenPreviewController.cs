using System.Collections.Generic;
using System.Linq;
using RPG.MasterData;
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
    [SerializeField] private ShopItemRowView itemRowPrefab;
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private RectTransform itemRowViewport;
    [SerializeField] private RectTransform itemRowContent;
    [SerializeField] private Button consumableCategoryButton;
    [SerializeField] private Button materialCategoryButton;
    [SerializeField] private Button equipmentCategoryButton;
    [SerializeField] private ShopItemDatabase shopItemDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;

    private ShopCategory currentCategory = ShopCategory.Consumable;
    private ShopItemRowView currentRow;
    private readonly List<ShopDisplayEntry> displayEntries = new();
    private readonly List<ShopItemRowView> itemRows = new();

    private void Awake()
    {
        RegisterCategoryButtons();
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
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
    }

    public void Hover(ShopItemRowView row)
    {
        if (row == null || currentRow == row)
        {
            return;
        }

        ClearCurrentRow();
        currentRow = row;
        currentRow.SetHighlighted(true);

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
    }

    public void Refresh()
    {
        ClearCurrentRow();
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
        ClearCurrentRow();
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
    }

    private void PopulateRows()
    {
        displayEntries.Clear();

        if (shopItemDatabase == null || itemDatabase == null || currentCategory == ShopCategory.Equipment && equipmentDatabase == null)
        {
            ClearRows();
            return;
        }

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
            item.IconSprite,
            item.DisplayName,
            BuildItemDetail(item),
            $"{item.DisplayName}を購入します。",
            FormatStock(shopItem),
            "0",
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
            equipment.IconSprite,
            equipment.DisplayName,
            BuildEquipmentDetail(equipment),
            $"{equipment.DisplayName}を購入します。",
            FormatStock(shopItem),
            "0",
            $"{equipment.Price} G");
        return true;
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

    private static string FormatStock(ShopItemData shopItem)
    {
        return shopItem.StockType == ShopStockDataType.Unlimited
            ? UnlimitedStockText
            : shopItem.StockCount.ToString();
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
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(consumableCategoryButton, currentCategory == ShopCategory.Consumable);
        SetCategoryButtonState(materialCategoryButton, currentCategory == ShopCategory.Material);
        SetCategoryButtonState(equipmentCategoryButton, currentCategory == ShopCategory.Equipment);
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
            Hover(firstActiveRow);
        }
        else
        {
            ClearDetail();
        }
    }

    private void ClearCurrentRow()
    {
        if (currentRow != null)
        {
            currentRow.SetHighlighted(false);
            currentRow = null;
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

    private readonly struct ShopDisplayEntry
    {
        public ShopDisplayEntry(
            Sprite icon,
            string name,
            string detail,
            string help,
            string stock,
            string owned,
            string price)
        {
            Icon = icon;
            Name = name;
            Detail = detail;
            Help = help;
            Stock = stock;
            Owned = owned;
            Price = price;
        }

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
}
