using System.Collections.Generic;
using System.Linq;
using RPG.Game;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Shop;
using RPG.Synthesis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SynthesisScreenPreviewController : MonoBehaviour, IItemRowViewController
{
    private const int MaxVisibleIngredientCount = 3;
    private const float RowStride = 54f;
    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color MissingTextColor = new(0.95f, 0.48f, 0.42f, 1f);
    private static readonly Color PopupPanelColor = new(0.08f, 0.075f, 0.065f, 0.96f);
    private static readonly Color PopupBackdropColor = new(0f, 0f, 0f, 0.58f);

    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailBodyText;
    [SerializeField] private TMP_Text detailStockText;
    [SerializeField] private TMP_Text detailOwnedText;
    [SerializeField] private TMP_Text detailPriceText;
    [SerializeField] private Image[] ingredientIconImages = { };
    [SerializeField] private TMP_Text[] ingredientNameTexts = { };
    [SerializeField] private TMP_Text[] ingredientCountTexts = { };
    [SerializeField] private TMP_Text helpText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button synthesizeButton;
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private TMP_Text stockHeaderText;
    [SerializeField] private TMP_Text ownedHeaderText;
    [SerializeField] private TMP_Text priceHeaderText;
    [SerializeField] private ShopItemRowView itemRowPrefab;
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private RectTransform itemRowViewport;
    [SerializeField] private RectTransform itemRowContent;
    [SerializeField] private Button consumableCategoryButton;
    [SerializeField] private Button weaponCategoryButton;
    [SerializeField] private Button armorCategoryButton;
    [SerializeField] private Button accessoryCategoryButton;
    [SerializeField] private GameObject resultPopupPrefab;
    [SerializeField] private GameObject resultPopupRoot;
    [SerializeField] private Image resultPopupIconImage;
    [SerializeField] private TMP_Text resultPopupTitleText;
    [SerializeField] private TMP_Text resultPopupNameText;
    [SerializeField] private TMP_Text resultPopupDetailText;
    [SerializeField] private TMP_Text resultPopupRarityText;
    [SerializeField] private Button resultPopupBackdropButton;
    [SerializeField] private Button resultPopupCloseButton;
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private SkillDatabase skillDatabase;

    private RecipeDataType currentCategory = RecipeDataType.Consumable;
    private ShopItemRowView selectedRow;
    private ShopItemRowView previewRow;
    private RunSaveData runSaveData;
    private SynthesisService synthesisService;
    private readonly List<SynthesisDisplayEntry> displayEntries = new();
    private readonly List<ShopItemRowView> itemRows = new();
    private bool initialized;

    private void Awake()
    {
        InitializePreviewState();
        ResolveOptionalReferences();
        RegisterButtons();
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
            RefreshCurrentView();
        }
    }

    private void OnDestroy()
    {
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.RemoveListener(SynthesizeCurrentRecipe);
        }

        if (consumableCategoryButton != null)
        {
            consumableCategoryButton.onClick.RemoveListener(ShowConsumables);
        }

        if (weaponCategoryButton != null)
        {
            weaponCategoryButton.onClick.RemoveListener(ShowWeapons);
        }

        if (armorCategoryButton != null)
        {
            armorCategoryButton.onClick.RemoveListener(ShowArmors);
        }

        if (accessoryCategoryButton != null)
        {
            accessoryCategoryButton.onClick.RemoveListener(ShowAccessories);
        }

        if (resultPopupCloseButton != null)
        {
            resultPopupCloseButton.onClick.RemoveListener(HideResultPopup);
        }

        if (resultPopupBackdropButton != null)
        {
            resultPopupBackdropButton.onClick.RemoveListener(HideResultPopup);
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

        ClearSelectedRow();
        selectedRow = row;
        selectedRow.SetHighlighted(true);
        SetEventSystemSelection(selectedRow.gameObject);
        previewRow = row;
        ShowDetail(row);
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

    public void SynthesizeCurrentRecipe()
    {
        if (selectedRow == null || synthesisService == null || runSaveData == null)
        {
            SetHelpText("作成するレシピを選んでください。");
            return;
        }

        var recipeId = selectedRow.ShopItemId;
        var recipeName = selectedRow.ItemName;
        var result = synthesisService.TrySynthesize(runSaveData, recipeId);
        if (!result.CanSynthesize)
        {
            SetHelpText(FormatFailure(result.FailureReason));
            return;
        }

        PopulateRows();
        ClearSelectedRow();
        previewRow = null;
        SelectRowByRecipeId(recipeId);
        RefreshMoneyText();
        RefreshActionButtonState();
        SetHelpText($"{recipeName}を作成しました。");
        ShowResultPopup(result);
    }

    private void RefreshCurrentView()
    {
        var selectedRecipeId = selectedRow != null ? selectedRow.ShopItemId : string.Empty;
        ClearSelectedRow();
        previewRow = null;
        PopulateRows();

        if (!string.IsNullOrWhiteSpace(selectedRecipeId))
        {
            SelectRowByRecipeId(selectedRecipeId);
        }

        if (selectedRow == null)
        {
            SelectFirstRow();
        }

        RefreshMoneyText();
        RefreshActionButtonState();
    }

    private void ShowConsumables() => ShowCategory(RecipeDataType.Consumable);
    private void ShowWeapons() => ShowCategory(RecipeDataType.Weapon);
    private void ShowArmors() => ShowCategory(RecipeDataType.Armor);
    private void ShowAccessories() => ShowCategory(RecipeDataType.Accessory);

    private void ShowCategory(RecipeDataType category)
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

        if (recipeDatabase == null || itemDatabase == null || equipmentDatabase == null || synthesisService == null || runSaveData == null)
        {
            ClearRows();
            return;
        }

        foreach (var recipe in recipeDatabase.Entries
            .Where(entry => entry != null
                && entry.RecipeType == currentCategory
                && synthesisService.IsRecipeVisible(runSaveData, entry))
            .OrderBy(entry => entry.SortOrder))
        {
            if (TryCreateEntry(recipe, out var entry))
            {
                displayEntries.Add(entry);
            }
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
                row.Configure(entry.RecipeId, entry.Icon, entry.Name, entry.Detail, entry.Help, entry.Level, entry.Owned, entry.Cost);
            }
            else
            {
                row.ClearRow();
            }
        }

        RefreshScrollArea();
        RefreshActionButtonState();
    }

    private bool TryCreateEntry(RecipeData recipe, out SynthesisDisplayEntry entry)
    {
        entry = default;

        var quote = synthesisService.GetQuote(runSaveData, recipe);
        var help = quote.CanSynthesize ? $"{recipe.DisplayName}を作成します。" : FormatFailure(quote.FailureReason);

        if (recipe.ResultType == RecipeResultDataType.Item)
        {
            if (!itemDatabase.TryGetById(recipe.ResultItemId, out var item) || item == null)
            {
                return false;
            }

            entry = new SynthesisDisplayEntry(
                recipe.RecipeId,
                item.IconSprite,
                recipe.DisplayName,
                BuildItemDetail(recipe, item),
                help,
                $"Lv{recipe.RequiredSynthesisLevel}",
                GetOwnedItemCount(item).ToString(),
                $"{recipe.Cost} G");
            return true;
        }

        if (!equipmentDatabase.TryGetById(recipe.ResultItemId, out var equipment) || equipment == null)
        {
            return false;
        }

        entry = new SynthesisDisplayEntry(
            recipe.RecipeId,
            equipment.IconSprite,
            recipe.DisplayName,
            BuildEquipmentDetail(recipe, equipment),
            help,
            $"Lv{recipe.RequiredSynthesisLevel}",
            GetOwnedEquipmentCount(equipment.EquipmentId).ToString(),
            $"{recipe.Cost} G");
        return true;
    }

    private string BuildItemDetail(RecipeData recipe, ItemData item)
    {
        return $"{item.Description}\n\n種別: {FormatRecipeType(recipe.RecipeType)}";
    }

    private string BuildEquipmentDetail(RecipeData recipe, EquipmentData equipment)
    {
        return $"{equipment.Description}\n\n種別: {FormatRecipeType(recipe.RecipeType)}";
    }

    private int GetOwnedItemCount(ItemData item)
    {
        if (item.ItemType == ItemDataType.Consumable)
        {
            return runSaveData.GetConsumableCount(item.ItemId);
        }

        return runSaveData.GetMaterialCount(item.ItemId);
    }

    private int GetOwnedEquipmentCount(string equipmentId)
    {
        return runSaveData.OwnedEquipments.Count(entry => entry.EquipmentId == equipmentId);
    }

    private void ShowDetail(ShopItemRowView row)
    {
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

        SetHelpText(row.HelpText);
        ShowIngredients(row.ShopItemId);
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
            detailIconImage.enabled = false;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = string.Empty;
        }

        SetHelpText("作成するレシピを選んでください。");
        ClearIngredients();
        RefreshActionButtonState();
    }

    private void ShowIngredients(string recipeId)
    {
        if (recipeDatabase == null
            || string.IsNullOrWhiteSpace(recipeId)
            || !recipeDatabase.TryGetById(recipeId, out var recipe)
            || recipe == null)
        {
            ClearIngredients();
            return;
        }

        for (var i = 0; i < MaxVisibleIngredientCount; i++)
        {
            var hasIngredient = i < recipe.Ingredients.Count && recipe.Ingredients[i] != null;
            SetIngredientRowActive(i, hasIngredient);

            if (!hasIngredient)
            {
                continue;
            }

            var ingredient = recipe.Ingredients[i];
            var owned = runSaveData != null ? runSaveData.GetMaterialCount(ingredient.ItemId) : 0;
            var hasEnough = owned >= ingredient.Count;
            var itemName = ingredient.ItemId;
            Sprite icon = null;

            if (itemDatabase != null && itemDatabase.TryGetById(ingredient.ItemId, out var item) && item != null)
            {
                itemName = item.DisplayName;
                icon = item.IconSprite;
            }

            if (i < ingredientIconImages.Length && ingredientIconImages[i] != null)
            {
                ingredientIconImages[i].sprite = icon;
                ingredientIconImages[i].enabled = icon != null;
            }

            if (i < ingredientNameTexts.Length && ingredientNameTexts[i] != null)
            {
                ingredientNameTexts[i].text = itemName;
                ingredientNameTexts[i].color = hasEnough ? TextColor : MissingTextColor;
            }

            if (i < ingredientCountTexts.Length && ingredientCountTexts[i] != null)
            {
                ingredientCountTexts[i].text = $"{owned} / {ingredient.Count}";
                ingredientCountTexts[i].color = hasEnough ? AccentTextColor : MissingTextColor;
            }
        }
    }

    private void ClearIngredients()
    {
        for (var i = 0; i < MaxVisibleIngredientCount; i++)
        {
            SetIngredientRowActive(i, false);
        }
    }

    private void SetIngredientRowActive(int index, bool active)
    {
        if (index < ingredientIconImages.Length && ingredientIconImages[index] != null)
        {
            ingredientIconImages[index].transform.parent.gameObject.SetActive(active);
        }
        else if (index < ingredientNameTexts.Length && ingredientNameTexts[index] != null)
        {
            ingredientNameTexts[index].transform.parent.gameObject.SetActive(active);
        }
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
            row.Initialize(this);
            itemRows.Add(row);
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
    }

    private void ClearSelectedRow()
    {
        if (selectedRow != null)
        {
            selectedRow.SetHighlighted(false);
            selectedRow = null;
        }
    }

    private void SelectFirstRow()
    {
        var firstRow = itemRows.FirstOrDefault(row => row != null && row.gameObject.activeSelf);
        if (firstRow != null)
        {
            Select(firstRow);
            return;
        }

        ClearDetail();
    }

    private void SelectRowByRecipeId(string recipeId)
    {
        var row = itemRows.FirstOrDefault(entry => entry != null && entry.gameObject.activeSelf && entry.ShopItemId == recipeId);
        if (row != null)
        {
            Select(row);
        }
    }

    private void RefreshScrollArea()
    {
        if (itemRowContent == null)
        {
            return;
        }

        var activeCount = itemRows.Count(row => row != null && row.gameObject.activeSelf);
        var contentHeight = activeCount * RowStride;
        var viewportHeight = itemRowViewport != null ? itemRowViewport.rect.height : 0f;
        var scrollable = viewportHeight > 0f && contentHeight > viewportHeight + 0.5f;
        var contentAreaHeight = Mathf.Max(contentHeight, viewportHeight);
        itemRowContent.sizeDelta = new Vector2(itemRowContent.sizeDelta.x, contentAreaHeight);
        itemRowContent.anchoredPosition = Vector2.zero;

        for (var i = 0; i < itemRows.Count; i++)
        {
            if (itemRows[i] == null)
            {
                continue;
            }

            var rect = itemRows[i].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0, RowStride);
            rect.anchoredPosition = new Vector2(0, -i * RowStride);
        }

        if (itemScrollRect != null)
        {
            itemScrollRect.verticalNormalizedPosition = 1f;
            itemScrollRect.vertical = scrollable;

            if (itemScrollRect.verticalScrollbar != null)
            {
                itemScrollRect.verticalScrollbar.gameObject.SetActive(scrollable);
                itemScrollRect.verticalScrollbar.value = 1f;
                itemScrollRect.verticalScrollbar.size = contentAreaHeight > 0f
                    ? Mathf.Clamp01(viewportHeight / contentAreaHeight)
                    : 1f;
            }
        }
    }

    private void RefreshMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{runSaveData?.Money ?? 0:N0} G";
        }
    }

    private void RefreshColumnHeaders()
    {
        if (stockHeaderText != null)
        {
            stockHeaderText.text = "必要Lv";
        }

        if (ownedHeaderText != null)
        {
            ownedHeaderText.text = "所持";
        }

        if (priceHeaderText != null)
        {
            priceHeaderText.text = "費用";
        }

        if (actionButtonLabel != null)
        {
            actionButtonLabel.text = "作成";
        }
    }

    private void RefreshCategoryButtons()
    {
        SetButtonLabelColor(consumableCategoryButton, currentCategory == RecipeDataType.Consumable);
        SetButtonLabelColor(weaponCategoryButton, currentCategory == RecipeDataType.Weapon);
        SetButtonLabelColor(armorCategoryButton, currentCategory == RecipeDataType.Armor);
        SetButtonLabelColor(accessoryCategoryButton, currentCategory == RecipeDataType.Accessory);
    }

    private void RefreshActionButtonState()
    {
        if (synthesizeButton == null)
        {
            return;
        }

        synthesizeButton.interactable = selectedRow != null
            && synthesisService != null
            && runSaveData != null
            && synthesisService.GetQuote(runSaveData, selectedRow.ShopItemId).CanSynthesize;
    }

    private void SetHelpText(string value)
    {
        if (helpText != null)
        {
            helpText.text = value;
        }
    }

    private void RegisterButtons()
    {
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.AddListener(SynthesizeCurrentRecipe);
        }

        if (consumableCategoryButton != null)
        {
            consumableCategoryButton.onClick.AddListener(ShowConsumables);
        }

        if (weaponCategoryButton != null)
        {
            weaponCategoryButton.onClick.AddListener(ShowWeapons);
        }

        if (armorCategoryButton != null)
        {
            armorCategoryButton.onClick.AddListener(ShowArmors);
        }

        if (accessoryCategoryButton != null)
        {
            accessoryCategoryButton.onClick.AddListener(ShowAccessories);
        }

        EnsureResultPopup();

        if (resultPopupCloseButton != null)
        {
            resultPopupCloseButton.onClick.AddListener(HideResultPopup);
        }

        if (resultPopupBackdropButton != null)
        {
            resultPopupBackdropButton.onClick.AddListener(HideResultPopup);
        }

        HideResultPopup();
    }

    private void InitializePreviewState()
    {
        runSaveData = GameSession.GetOrCreate().RunSaveData;
        if (runSaveData.SynthesisLevel < 2)
        {
            runSaveData.SetSynthesisLevel(2);
        }

        synthesisService = new SynthesisService(recipeDatabase, itemDatabase, equipmentDatabase);
    }

    private void ResolveOptionalReferences()
    {
        if (synthesizeButton == null)
        {
            synthesizeButton = FindDeep(transform, "BuyButton")?.GetComponent<Button>();
        }

        if (actionButtonLabel == null)
        {
            actionButtonLabel = FindDeep(transform, "BuyButton")?.Find("Label")?.GetComponent<TMP_Text>();
        }

        if (resultPopupRoot == null)
        {
            resultPopupRoot = FindDeep(transform, "SynthesisResultPopup")?.gameObject;
        }

        if (resultPopupRoot != null)
        {
            ResolveResultPopupReferences();
        }
    }

    private void ResolveResultPopupReferences()
    {
        if (resultPopupRoot == null)
        {
            return;
        }

        if (resultPopupIconImage == null)
        {
            resultPopupIconImage = FindDeep(resultPopupRoot.transform, "ResultIcon")?.GetComponent<Image>();
        }

        if (resultPopupTitleText == null)
        {
            resultPopupTitleText = FindDeep(resultPopupRoot.transform, "Title")?.GetComponent<TMP_Text>();
        }

        if (resultPopupNameText == null)
        {
            resultPopupNameText = FindDeep(resultPopupRoot.transform, "ResultName")?.GetComponent<TMP_Text>();
        }

        if (resultPopupDetailText == null)
        {
            resultPopupDetailText = FindDeep(resultPopupRoot.transform, "ResultDetail")?.GetComponent<TMP_Text>();
        }

        if (resultPopupRarityText == null)
        {
            resultPopupRarityText = FindDeep(resultPopupRoot.transform, "ResultRarity")?.GetComponent<TMP_Text>();
        }

        if (resultPopupBackdropButton == null)
        {
            resultPopupBackdropButton = resultPopupRoot.GetComponent<Button>();
        }

        if (resultPopupCloseButton == null)
        {
            resultPopupCloseButton = FindDeep(resultPopupRoot.transform, "CloseButton")?.GetComponent<Button>();
        }
    }

    private void ShowResultPopup(SynthesisQuote result)
    {
        EnsureResultPopup();

        if (resultPopupRoot == null || result.Recipe == null)
        {
            return;
        }

        var resultName = result.Recipe.DisplayName;
        var resultDetail = string.Empty;
        Sprite resultIcon = null;

        if (result.ResultType == RecipeResultDataType.Item)
        {
            if (itemDatabase != null && itemDatabase.TryGetById(result.ResultId, out var item) && item != null)
            {
                resultName = item.DisplayName;
                resultDetail = item.Description;
                resultIcon = item.IconSprite;
            }
        }
        else if (equipmentDatabase != null && equipmentDatabase.TryGetById(result.ResultId, out var equipment) && equipment != null)
        {
            resultName = equipment.DisplayName;
            resultDetail = equipment.Description;
            resultIcon = equipment.IconSprite;
        }

        if (resultPopupTitleText != null)
        {
            resultPopupTitleText.text = "合成完了";
        }

        if (resultPopupNameText != null)
        {
            resultPopupNameText.text = resultName;
        }

        if (resultPopupDetailText != null)
        {
            resultPopupDetailText.text = BuildResultPopupDetail(result, resultName, resultDetail);
        }

        if (resultPopupRarityText != null)
        {
            resultPopupRarityText.text = result.HasResultRarity
                ? $"レアリティ: {FormatRarity(result.ResultRarity)}"
                : "入手数: 1";
        }

        if (resultPopupIconImage != null)
        {
            resultPopupIconImage.sprite = resultIcon;
            resultPopupIconImage.enabled = resultIcon != null;
        }

        resultPopupRoot.SetActive(true);

        if (resultPopupCloseButton != null)
        {
            SetEventSystemSelection(resultPopupCloseButton.gameObject);
        }
    }

    private void HideResultPopup()
    {
        if (resultPopupRoot != null)
        {
            resultPopupRoot.SetActive(false);
        }
    }

    private void EnsureResultPopup()
    {
        if (resultPopupRoot != null)
        {
            ResolveResultPopupReferences();
            return;
        }

        if (resultPopupPrefab != null)
        {
            resultPopupRoot = Instantiate(resultPopupPrefab, transform);
            resultPopupRoot.name = resultPopupPrefab.name;
            resultPopupRoot.transform.SetAsLastSibling();

            if (resultPopupRoot.TryGetComponent<RectTransform>(out var prefabRect))
            {
                prefabRect.anchorMin = Vector2.zero;
                prefabRect.anchorMax = Vector2.one;
                prefabRect.offsetMin = Vector2.zero;
                prefabRect.offsetMax = Vector2.zero;
            }

            ResolveResultPopupReferences();
            return;
        }

        var root = new GameObject("SynthesisResultPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(transform, false);
        resultPopupRoot = root;

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsLastSibling();

        var backdrop = root.GetComponent<Image>();
        backdrop.color = PopupBackdropColor;

        var closeBackdrop = root.AddComponent<Button>();
        closeBackdrop.targetGraphic = backdrop;
        resultPopupBackdropButton = closeBackdrop;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 320f);
        panelRect.anchoredPosition = Vector2.zero;

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = PopupPanelColor;

        resultPopupTitleText = CreatePopupText(panel.transform, "Title", "合成完了", 28, TextAlignmentOptions.Center, AccentTextColor);
        SetRect(resultPopupTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(440f, 42f));

        var iconObject = new GameObject("ResultIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(panel.transform, false);
        resultPopupIconImage = iconObject.GetComponent<Image>();
        resultPopupIconImage.preserveAspect = true;
        SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -118f), new Vector2(80f, 80f));

        resultPopupNameText = CreatePopupText(panel.transform, "ResultName", string.Empty, 24, TextAlignmentOptions.Left, AccentTextColor);
        SetRect(resultPopupNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(142f, -96f), new Vector2(-178f, 36f));

        resultPopupRarityText = CreatePopupText(panel.transform, "ResultRarity", string.Empty, 20, TextAlignmentOptions.Left, TextColor);
        SetRect(resultPopupRarityText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(142f, -134f), new Vector2(-178f, 30f));

        resultPopupDetailText = CreatePopupText(panel.transform, "ResultDetail", string.Empty, 19, TextAlignmentOptions.TopLeft, TextColor);
        SetRect(resultPopupDetailText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(-80f, -180f));

        var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(panel.transform, false);
        resultPopupCloseButton = closeObject.GetComponent<Button>();
        var closeImage = closeObject.GetComponent<Image>();
        closeImage.color = new Color(0.32f, 0.25f, 0.14f, 1f);
        resultPopupCloseButton.targetGraphic = closeImage;
        SetRect(closeObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(160f, 44f));

        var closeLabel = CreatePopupText(closeObject.transform, "Label", "閉じる", 22, TextAlignmentOptions.Center, AccentTextColor);
        SetRect(closeLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static TMP_Text CreatePopupText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var label = textObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private string BuildResultPopupDetail(SynthesisQuote result, string resultName, string resultDetail)
    {
        var lines = new List<string>();

        if (string.IsNullOrWhiteSpace(resultDetail))
        {
            lines.Add($"{resultName}を作成しました。");
        }
        else
        {
            lines.Add(resultDetail);
        }

        if (!result.HasResultRarity)
        {
            AppendConsumedResources(lines, result);
            return string.Join("\n", lines);
        }

        var randomLines = new List<string>();
        foreach (var modifier in result.ResultModifiers)
        {
            if (modifier == null)
            {
                continue;
            }

            randomLines.Add($"・{FormatModifier(modifier)}");
        }

        if (!string.IsNullOrWhiteSpace(result.ResultRandomSkillId))
        {
            randomLines.Add($"・ランダムスキル: {FormatSkillName(result.ResultRandomSkillId)}");
        }

        if (randomLines.Count == 0)
        {
            randomLines.Add("・なし");
        }

        lines.Add(string.Empty);
        lines.Add("ランダム結果");
        lines.AddRange(randomLines);
        AppendConsumedResources(lines, result);
        return string.Join("\n", lines);
    }

    private void AppendConsumedResources(List<string> lines, SynthesisQuote result)
    {
        if (result.Recipe == null)
        {
            return;
        }

        lines.Add(string.Empty);
        lines.Add("消費");

        foreach (var ingredient in result.Recipe.Ingredients)
        {
            if (ingredient == null)
            {
                continue;
            }

            lines.Add($"・{FormatItemName(ingredient.ItemId)} x{ingredient.Count}");
        }

        lines.Add($"・{result.Cost} G");
    }

    private static void SetButtonLabelColor(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.color = selected ? AccentTextColor : TextColor;
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            var result = FindDeep(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static string FormatRecipeType(RecipeDataType recipeType)
    {
        return recipeType switch
        {
            RecipeDataType.Consumable => "消耗品",
            RecipeDataType.Weapon => "武器",
            RecipeDataType.Armor => "防具",
            RecipeDataType.Accessory => "アクセサリ",
            _ => recipeType.ToString()
        };
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

    private static string FormatModifier(EquipmentModifierSaveData modifier)
    {
        var sign = modifier.Amount >= 0 ? "+" : string.Empty;
        var target = FormatModifierTarget(modifier.TargetId);
        return string.IsNullOrWhiteSpace(target)
            ? $"{FormatModifierType(modifier.ModifierType)} {sign}{modifier.Amount}{FormatModifierUnit(modifier.ModifierType)}"
            : $"{target}{FormatModifierType(modifier.ModifierType)} {sign}{modifier.Amount}{FormatModifierUnit(modifier.ModifierType)}";
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

    private static string FormatModifierTarget(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId) || targetId == "all")
        {
            return string.Empty;
        }

        return $"{targetId} ";
    }

    private string FormatItemName(string itemId)
    {
        if (itemDatabase != null
            && itemDatabase.TryGetById(itemId, out var item)
            && item != null
            && !string.IsNullOrWhiteSpace(item.DisplayName))
        {
            return item.DisplayName;
        }

        return itemId;
    }

    private string FormatSkillName(string skillId)
    {
        if (skillDatabase != null
            && skillDatabase.TryGetById(skillId, out var skill)
            && skill != null
            && !string.IsNullOrWhiteSpace(skill.DisplayName))
        {
            return skill.DisplayName;
        }

        return skillId;
    }

    private static string FormatFailure(SynthesisFailureReason reason)
    {
        return reason switch
        {
            SynthesisFailureReason.InvalidRequest => "レシピを選んでください。",
            SynthesisFailureReason.RecipeNotFound => "レシピデータが見つかりません。",
            SynthesisFailureReason.ResultNotFound => "作成物のデータが見つかりません。",
            SynthesisFailureReason.RecipeLocked => "合成レベルが足りません。",
            SynthesisFailureReason.NotEnoughMaterial => "素材が足りません。",
            SynthesisFailureReason.NotEnoughMoney => "所持金が足りません。",
            SynthesisFailureReason.InventoryFull => $"消耗品は{ShopPurchaseService.MaxConsumableCount}個まで所持できます。",
            _ => string.Empty
        };
    }

    private readonly struct SynthesisDisplayEntry
    {
        public SynthesisDisplayEntry(string recipeId, Sprite icon, string name, string detail, string help, string level, string owned, string cost)
        {
            RecipeId = recipeId ?? string.Empty;
            Icon = icon;
            Name = name ?? string.Empty;
            Detail = detail ?? string.Empty;
            Help = help ?? string.Empty;
            Level = level ?? string.Empty;
            Owned = owned ?? string.Empty;
            Cost = cost ?? string.Empty;
        }

        public string RecipeId { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Detail { get; }
        public string Help { get; }
        public string Level { get; }
        public string Owned { get; }
        public string Cost { get; }
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
