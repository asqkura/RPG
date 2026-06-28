using System.Collections.Generic;
using System.Linq;
using RPG.Game;
using RPG.MasterData;
using RPG.SaveData;
using RPG.Synthesis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SynthesisScreenPreviewController : MonoBehaviour, ISynthesisRecipeRowViewController
{
    private const float RowStride = 54f;
    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);

    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailTagText;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private EquipmentDetailPanelView equipmentDetailPanelView;
    [SerializeField] private SynthesisMaterialPanelView materialPanelView;
    [SerializeField] private SynthesisResultScreenView resultScreenView;
    [SerializeField] private SynthesisResultScreenView resultScreenPrefab;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text synthesisLevelText;
    [SerializeField] private Button synthesizeButton;
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private SynthesisRecipeRowView recipeRowPrefab;
    [SerializeField] private ScrollRect recipeScrollRect;
    [SerializeField] private RectTransform recipeRowViewport;
    [SerializeField] private RectTransform recipeRowContent;
    [SerializeField] private Button allCategoryButton;
    [SerializeField] private Button consumableCategoryButton;
    [SerializeField] private Button weaponCategoryButton;
    [SerializeField] private Button armorCategoryButton;
    [SerializeField] private Button accessoryCategoryButton;
    [SerializeField] private Button otherCategoryButton;
    [SerializeField] private SynthesisRecipeDatabase recipeDatabase;
    [SerializeField] private SynthesisLevelUpRequirementDatabase levelUpRequirementDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private SkillDatabase skillDatabase;

    private SynthesisCategory currentCategory = SynthesisCategory.All;
    private SynthesisRecipeRowView selectedRow;
    private SynthesisRecipeRowView previewRow;
    private RunSaveData runSaveData;
    private SynthesisService synthesisService;
    private readonly List<SynthesisDisplayEntry> displayEntries = new();
    private readonly List<SynthesisRecipeRowView> recipeRows = new();
    private readonly Dictionary<string, EquipmentDetailData> equipmentDetailsByRecipeId = new();
    private bool initialized;

    private void Awake()
    {
        InitializeGameState();
        EnsureResultScreenView();
        RegisterCategoryButtons();
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.AddListener(SubmitCurrentRecipe);
        }

        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
        RefreshMoneyText();
        RefreshSynthesisLevelText();
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
        UnregisterCategoryButtons();
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.RemoveListener(SubmitCurrentRecipe);
        }
    }

    public void Hover(SynthesisRecipeRowView row)
    {
        if (row == null || previewRow == row)
        {
            return;
        }

        previewRow = row;
        ShowDetail(row);
    }

    public void Select(SynthesisRecipeRowView row)
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

    public void Clear(SynthesisRecipeRowView row)
    {
        if (row == null || previewRow != row)
        {
            return;
        }

        previewRow = selectedRow;
        if (selectedRow != null)
        {
            ShowDetail(selectedRow);
        }
        else
        {
            ClearDetail();
        }
    }

    public void Refresh()
    {
        PopulateRows();
        if (selectedRow != null)
        {
            SelectRowByRecipeId(selectedRow.RecipeId);
        }

        if (selectedRow == null)
        {
            SelectFirstRow();
        }

        RefreshMoneyText();
        RefreshSynthesisLevelText();
    }

    private void SubmitCurrentRecipe()
    {
        if (selectedRow == null || synthesisService == null || runSaveData == null)
        {
            return;
        }

        var recipeId = selectedRow.RecipeId;
        if (TryGetDisplayEntry(recipeId, out var selectedEntry)
            && selectedEntry.EntryType == SynthesisDisplayEntryType.LevelUpRequirement)
        {
            SubmitLevelUpRequirement(recipeId);
            return;
        }

        var result = synthesisService.TrySynthesize(runSaveData, recipeId);
        if (!result.CanSynthesize)
        {
            RefreshActionButtonState();
            return;
        }

        PopulateRows();
        SelectRowByRecipeId(recipeId);
        RefreshMoneyText();
        ShowSynthesisResult(result);
    }

    private void ShowAll()
    {
        ShowCategory(SynthesisCategory.All);
    }

    private void ShowConsumables()
    {
        ShowCategory(SynthesisCategory.Consumable);
    }

    private void ShowWeapons()
    {
        ShowCategory(SynthesisCategory.Weapon);
    }

    private void ShowArmor()
    {
        ShowCategory(SynthesisCategory.Armor);
    }

    private void ShowAccessories()
    {
        ShowCategory(SynthesisCategory.Accessory);
    }

    private void ShowOther()
    {
        ShowCategory(SynthesisCategory.Other);
    }

    private void ShowCategory(SynthesisCategory category)
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

    private void ShowDetail(SynthesisRecipeRowView row)
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

        if (equipmentDetailsByRecipeId.TryGetValue(row.RecipeId, out var equipmentDetail))
        {
            equipmentDetailPanelView?.Show(equipmentDetail);
        }
        else
        {
            equipmentDetailPanelView?.Hide();
        }

        RefreshMaterialPanel(row.RecipeId);
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

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = string.Empty;
        }

        equipmentDetailPanelView?.Hide();
        materialPanelView?.Clear();
        RefreshActionButtonState();
    }

    private void PopulateRows()
    {
        displayEntries.Clear();
        equipmentDetailsByRecipeId.Clear();

        if (currentCategory == SynthesisCategory.Other)
        {
            if (TryCreateLevelUpEntry(out var levelUpEntry))
            {
                displayEntries.Add(levelUpEntry);
            }
        }
        else if (recipeDatabase != null)
        {
            foreach (var recipe in recipeDatabase.Entries
                .Where(recipe => recipe != null)
                .OrderBy(recipe => recipe.SortOrder)
                .ThenBy(recipe => recipe.RecipeId))
            {
                if (TryCreateEntry(recipe, out var entry))
                {
                    displayEntries.Add(entry);
                }
            }
        }
        else
        {
            ClearRows();
            return;
        }

        EnsureRowCount(displayEntries.Count);
        for (var i = 0; i < recipeRows.Count; i++)
        {
            if (i < displayEntries.Count)
            {
                var entry = displayEntries[i];
                recipeRows[i].Configure(
                    entry.RecipeId,
                    entry.Icon,
                    entry.Name,
                    entry.Tag,
                    entry.Description,
                    entry.Owned,
                    entry.Cost);
            }
            else
            {
                recipeRows[i].ClearRow();
            }
        }

        RefreshScrollArea();
    }

    private bool TryCreateEntry(SynthesisRecipeData recipe, out SynthesisDisplayEntry entry)
    {
        entry = default;

        if (recipe.ProductType == SynthesisProductDataType.Consumable)
        {
            if (currentCategory != SynthesisCategory.All && currentCategory != SynthesisCategory.Consumable)
            {
                return false;
            }

            var item = recipe.ProductItem;
            if (item == null
                || item.ItemType != ItemDataType.Consumable)
            {
                return false;
            }

            entry = new SynthesisDisplayEntry(
                SynthesisDisplayEntryType.Recipe,
                recipe.RecipeId,
                item.IconSprite,
                item.DisplayName,
                MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatItemType(item.ItemType)),
                item.Description,
                FormatOwnedCount(runSaveData?.GetConsumableCount(item.ItemId) ?? 0),
                FormatRecipeCost(recipe));
            return true;
        }

        var equipment = recipe.ProductEquipment;
        if (equipment == null
            || !MatchesEquipmentCategory(equipment, currentCategory))
        {
            return false;
        }

        entry = new SynthesisDisplayEntry(
            SynthesisDisplayEntryType.Recipe,
            recipe.RecipeId,
            equipment.IconSprite,
            equipment.DisplayName,
            MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatEquipmentType(equipment)),
            equipment.Description,
            FormatOwnedCount(GetOwnedEquipmentCount(equipment.EquipmentId)),
            FormatRecipeCost(recipe));
        equipmentDetailsByRecipeId[recipe.RecipeId] = BuildEquipmentDetailData(equipment);
        return true;
    }

    private bool TryCreateLevelUpEntry(out SynthesisDisplayEntry entry)
    {
        entry = default;
        if (synthesisService == null || runSaveData == null)
        {
            return false;
        }

        var quote = synthesisService.GetLevelUpQuote(runSaveData);
        if (quote.FailureReason == SynthesisLevelUpFailureReason.MaxLevelReached)
        {
            return false;
        }

        var requirement = quote.Requirement;
        var entryId = requirement != null
            ? requirement.RequirementId
            : $"synthesis_level_{quote.CurrentLevel}_to_{quote.TargetLevel}";
        var displayName = requirement != null && !string.IsNullOrWhiteSpace(requirement.DisplayName)
            ? requirement.DisplayName
            : $"合成Lv{quote.TargetLevel}へ強化";
        var description = requirement != null && !string.IsNullOrWhiteSpace(requirement.Description)
            ? requirement.Description
            : "合成レベルを上げ、より良い装備個体を作りやすくします。";

        entry = new SynthesisDisplayEntry(
            SynthesisDisplayEntryType.LevelUpRequirement,
            entryId,
            requirement != null ? requirement.IconSprite : null,
            displayName,
            MasterDataDisplayLabels.FormatTag($"Lv{quote.CurrentLevel} から Lv{quote.TargetLevel}"),
            description,
            $"Lv{runSaveData.SynthesisLevel}",
            quote.MoneyCost > 0 ? quote.MoneyCost.ToString() : "-",
            requirement);
        return true;
    }

    private void InitializeGameState()
    {
        runSaveData = GameSession.GetOrCreate().RunSaveData;
        synthesisService = new SynthesisService(recipeDatabase, itemDatabase, equipmentDatabase, levelUpRequirementDatabase);
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
    }

    private void UnregisterCategoryButtons()
    {
        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.RemoveListener(ShowAll);
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
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(allCategoryButton, currentCategory == SynthesisCategory.All);
        SetCategoryButtonState(consumableCategoryButton, currentCategory == SynthesisCategory.Consumable);
        SetCategoryButtonState(weaponCategoryButton, currentCategory == SynthesisCategory.Weapon);
        SetCategoryButtonState(armorCategoryButton, currentCategory == SynthesisCategory.Armor);
        SetCategoryButtonState(accessoryCategoryButton, currentCategory == SynthesisCategory.Accessory);
        SetCategoryButtonState(otherCategoryButton, currentCategory == SynthesisCategory.Other);
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
        foreach (var row in recipeRows)
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
        if (recipeRowPrefab == null || recipeRowContent == null)
        {
            return;
        }

        while (recipeRows.Count < count)
        {
            var row = Instantiate(recipeRowPrefab, recipeRowContent);
            row.name = "RecipeRow_" + (recipeRows.Count + 1).ToString("00");
            SetupRowTransform(row.GetComponent<RectTransform>(), recipeRows.Count);
            row.Initialize(this);
            row.ClearRow();
            recipeRows.Add(row);
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
        if (recipeScrollRect == null || recipeRowViewport == null || recipeRowContent == null)
        {
            return;
        }

        var contentHeight = displayEntries.Count * RowStride;
        var viewportHeight = recipeRowViewport.rect.height;
        var scrollable = contentHeight > viewportHeight + 0.5f;
        var contentAreaHeight = Mathf.Max(contentHeight, viewportHeight);

        recipeRowContent.sizeDelta = new Vector2(recipeRowContent.sizeDelta.x, contentAreaHeight);
        recipeRowContent.anchoredPosition = Vector2.zero;
        recipeScrollRect.verticalNormalizedPosition = 1f;
        recipeScrollRect.vertical = scrollable;
        recipeScrollRect.enabled = true;
    }

    private void SelectFirstRow()
    {
        var firstActiveRow = recipeRows.FirstOrDefault(row => row != null && row.gameObject.activeSelf);
        if (firstActiveRow != null)
        {
            Select(firstActiveRow);
        }
        else
        {
            previewRow = null;
            ClearSelectedRow();
            ClearDetail();
        }
    }

    private void SelectRowByRecipeId(string recipeId)
    {
        ClearSelectedRow();
        var row = recipeRows.FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeSelf && candidate.RecipeId == recipeId);
        if (row != null)
        {
            Select(row);
        }
    }

    private void ClearSelectedRow()
    {
        if (selectedRow != null)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == selectedRow.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            selectedRow.SetHighlighted(false);
            selectedRow = null;
        }
    }

    private static void SetEventSystemSelection(GameObject target)
    {
        if (EventSystem.current != null && target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    private void RefreshMaterialPanel(string recipeId)
    {
        if (materialPanelView == null)
        {
            return;
        }

        if (!TryGetDisplayEntry(recipeId, out var entry))
        {
            materialPanelView.Clear();
            return;
        }

        if (entry.EntryType == SynthesisDisplayEntryType.LevelUpRequirement)
        {
            var levelUpEntries = entry.LevelUpRequirement != null
                ? BuildMaterialPanelEntries(entry.LevelUpRequirement.MaterialCosts)
                : new List<SynthesisMaterialPanelEntry>();
            ShowMaterialPanelEntries(levelUpEntries);
            return;
        }

        if (recipeDatabase == null
            || string.IsNullOrWhiteSpace(recipeId)
            || !recipeDatabase.TryGetById(recipeId, out var recipe)
            || recipe == null)
        {
            materialPanelView.Clear();
            return;
        }

        ShowMaterialPanelEntries(BuildMaterialPanelEntries(recipe));
    }

    private void ShowMaterialPanelEntries(List<SynthesisMaterialPanelEntry> allEntries)
    {
        var overflowCount = 0;
        if (allEntries.Count > 3)
        {
            overflowCount = allEntries.Count - 2;
            allEntries.RemoveRange(2, allEntries.Count - 2);
        }

        materialPanelView.Show(allEntries, overflowCount);
    }

    private List<SynthesisMaterialPanelEntry> BuildMaterialPanelEntries(SynthesisRecipeData recipe)
    {
        if (recipe == null)
        {
            return new List<SynthesisMaterialPanelEntry>();
        }

        return BuildMaterialPanelEntries(recipe.MaterialCosts);
    }

    private List<SynthesisMaterialPanelEntry> BuildMaterialPanelEntries(IReadOnlyList<SynthesisMaterialCostData> costs)
    {
        var materialCosts = new List<SynthesisMaterialPanelCost>();
        if (costs == null)
        {
            return new List<SynthesisMaterialPanelEntry>();
        }

        foreach (var cost in costs)
        {
            if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId) || cost.Count <= 0)
            {
                continue;
            }

            var existingIndex = materialCosts.FindIndex(entry => entry.ItemId == cost.ItemId);
            if (existingIndex >= 0)
            {
                var existing = materialCosts[existingIndex];
                materialCosts[existingIndex] = existing.AddCount(cost.Count);
                continue;
            }

            materialCosts.Add(new SynthesisMaterialPanelCost(cost.ItemId, cost.Item, cost.Count));
        }

        var entries = new List<SynthesisMaterialPanelEntry>(materialCosts.Count);
        foreach (var cost in materialCosts)
        {
            var ownedCount = runSaveData?.GetMaterialCount(cost.ItemId) ?? 0;
            entries.Add(new SynthesisMaterialPanelEntry(
                cost.Item != null ? cost.Item.IconSprite : null,
                FormatMaterialName(cost.Item, cost.ItemId),
                ownedCount,
                cost.RequiredCount,
                ownedCount < cost.RequiredCount));
        }

        return entries;
    }

    private string FormatRecipeCost(SynthesisRecipeData recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        return recipe.MoneyCost > 0 ? recipe.MoneyCost.ToString() : "-";
    }

    private static string FormatOwnedCount(int count)
    {
        return count.ToString();
    }

    private int GetOwnedEquipmentCount(string equipmentId)
    {
        return runSaveData == null || string.IsNullOrWhiteSpace(equipmentId)
            ? 0
            : runSaveData.OwnedEquipments.Count(equipment => equipment != null && equipment.EquipmentId == equipmentId);
    }

    private static string FormatMaterialName(SynthesisMaterialCostData cost)
    {
        if (cost == null)
        {
            return string.Empty;
        }

        return FormatMaterialName(cost.Item, cost.ItemId);
    }

    private static string FormatMaterialName(ItemData item, string itemId)
    {
        return item != null
            && !string.IsNullOrWhiteSpace(item.DisplayName)
            ? item.DisplayName
            : itemId;
    }

    private void RefreshMoneyText()
    {
        if (moneyText != null)
        {
            if (runSaveData == null)
            {
                moneyText.text = "0";
                return;
            }

            moneyText.text = synthesisLevelText == null
                ? $"Lv{runSaveData.SynthesisLevel} / {runSaveData.Money:N0}"
                : $"{runSaveData.Money:N0}";
        }
    }

    private void RefreshSynthesisLevelText()
    {
        if (synthesisLevelText != null)
        {
            synthesisLevelText.text = runSaveData != null ? $"Lv{runSaveData.SynthesisLevel}" : "Lv1";
        }
    }

    private void ShowSynthesisResult(SynthesisQuote result)
    {
        EnsureResultScreenView();
        if (resultScreenView == null || result.Recipe == null)
        {
            return;
        }

        if (result.ProductType == SynthesisProductDataType.Consumable)
        {
            var item = result.Recipe.ProductItem;
            if (item == null)
            {
                return;
            }

            resultScreenView.ShowConsumable(new SynthesisConsumableResultViewData
            {
                Icon = item.IconSprite,
                DisplayName = item.DisplayName,
                TagText = MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatItemType(item.ItemType)),
                Description = item.Description
            });
            return;
        }

        var equipment = result.Recipe.ProductEquipment;
        if (equipment == null)
        {
            return;
        }

        resultScreenView.ShowEquipment(new SynthesisEquipmentResultViewData
        {
            Icon = equipment.IconSprite,
            DisplayName = equipment.DisplayName,
            TagText = MasterDataDisplayLabels.FormatTag(MasterDataDisplayLabels.FormatEquipmentType(equipment)),
            Description = equipment.Description,
            DefaultDetail = BuildEquipmentDetailData(equipment),
            ResultDetail = BuildEquipmentDetailData(equipment, result.CreatedEquipment)
        });
    }

    private void EnsureResultScreenView()
    {
        if (resultScreenView != null || resultScreenPrefab == null)
        {
            return;
        }

        var parent = transform.parent;
        resultScreenView = parent != null
            ? Instantiate(resultScreenPrefab, parent, false)
            : Instantiate(resultScreenPrefab);
        resultScreenView.name = resultScreenPrefab.name;
    }

    private void RefreshActionButtonState()
    {
        if (synthesizeButton == null)
        {
            return;
        }

        var canSubmit = false;
        var label = "合成する";
        if (selectedRow != null && synthesisService != null && runSaveData != null)
        {
            if (TryGetDisplayEntry(selectedRow.RecipeId, out var entry)
                && entry.EntryType == SynthesisDisplayEntryType.LevelUpRequirement)
            {
                var quote = synthesisService.GetLevelUpQuote(runSaveData);
                canSubmit = quote.CanLevelUp;
                label = FormatLevelUpActionButtonLabel(quote.FailureReason);
            }
            else
            {
                var quote = synthesisService.GetQuote(runSaveData, selectedRow.RecipeId);
                canSubmit = quote.CanSynthesize;
                label = FormatActionButtonLabel(quote.FailureReason);
            }
        }

        synthesizeButton.interactable = canSubmit;
        if (actionButtonLabel != null)
        {
            actionButtonLabel.text = label;
            actionButtonLabel.color = canSubmit ? AccentTextColor : TextColor;
        }
    }

    private static string FormatActionButtonLabel(SynthesisFailureReason failureReason)
    {
        return failureReason == SynthesisFailureReason.NotEnoughMaterials
            ? "素材不足"
            : "合成する";
    }

    private static string FormatLevelUpActionButtonLabel(SynthesisLevelUpFailureReason failureReason)
    {
        return failureReason == SynthesisLevelUpFailureReason.NotEnoughMaterials
            ? "素材不足"
            : failureReason == SynthesisLevelUpFailureReason.NotEnoughMoney
                ? "お金不足"
                : "強化する";
    }

    private EquipmentDetailData BuildEquipmentDetailData(EquipmentData equipment, OwnedEquipmentSaveData ownedEquipment = null)
    {
        var detail = new EquipmentDetailData
        {
            Description = equipment.Description
        };

        AddStatData(detail.Stats, "攻撃", (equipment.StatModifiers?.Attack ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Attack));
        AddStatData(detail.Stats, "魔力", (equipment.StatModifiers?.Magic ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Magic));
        AddStatData(detail.Stats, "防御", (equipment.StatModifiers?.Defense ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Defense));
        AddStatData(detail.Stats, "素早さ", (equipment.StatModifiers?.Speed ?? 0) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.Speed));
        var criticalRate = Mathf.RoundToInt((equipment.StatModifiers?.CriticalRate ?? 0f) * 100f) + GetRandomModifierAmount(ownedEquipment, EquipmentModifierType.CriticalRate);
        AddStatData(detail.Stats, "会心率", criticalRate, "%");

        foreach (var skillId in equipment.ActiveSkillIds)
        {
            var skillName = FormatSkillName(skillId, string.Empty);
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(skillName));
            }
        }

        foreach (var passiveId in equipment.FixedPassiveIds)
        {
            if (!string.IsNullOrWhiteSpace(passiveId))
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(passiveId));
            }
        }

        if (ownedEquipment != null)
        {
            if (!string.IsNullOrWhiteSpace(ownedEquipment.RandomPassiveId))
            {
                var levelText = ownedEquipment.RandomPassiveLevel > 0
                    ? $" Lv{ownedEquipment.RandomPassiveLevel}"
                    : string.Empty;
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(ownedEquipment.RandomPassiveId + levelText));
            }

        }

        return detail;
    }

    private static void AddStatData(List<EquipmentDetailStat> stats, string label, int value, string suffix = "")
    {
        var text = value != 0 ? $"{FormatSigned(value)}{suffix}" : "-";
        stats.Add(new EquipmentDetailStat(label, text, value.CompareTo(0)));
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

    private static int GetRandomModifierAmount(OwnedEquipmentSaveData ownedEquipment, EquipmentModifierType modifierType)
    {
        if (ownedEquipment == null)
        {
            return 0;
        }

        var amount = 0;
        foreach (var modifier in ownedEquipment.RandomStatModifiers)
        {
            if (modifier != null && modifier.ModifierType == modifierType)
            {
                amount += modifier.Amount;
            }
        }

        return amount;
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static bool MatchesEquipmentCategory(EquipmentData equipment, SynthesisCategory category)
    {
        if (equipment == null)
        {
            return false;
        }

        return category switch
        {
            SynthesisCategory.All => true,
            SynthesisCategory.Weapon => equipment.EquipmentType == EquipmentDataType.Weapon,
            SynthesisCategory.Armor => equipment.EquipmentType == EquipmentDataType.Armor,
            SynthesisCategory.Accessory => equipment.EquipmentType == EquipmentDataType.Accessory,
            _ => false
        };
    }

    private void SubmitLevelUpRequirement(string entryId)
    {
        var result = synthesisService.TryRaiseSynthesisLevel(runSaveData);
        if (!result.CanLevelUp)
        {
            RefreshActionButtonState();
            return;
        }

        PopulateRows();
        SelectRowByRecipeId(entryId);
        RefreshMoneyText();
        RefreshSynthesisLevelText();
    }

    private bool TryGetDisplayEntry(string entryId, out SynthesisDisplayEntry entry)
    {
        entry = displayEntries.FirstOrDefault(candidate => candidate.EntryId == entryId);
        return !string.IsNullOrWhiteSpace(entry.EntryId);
    }

    private readonly struct SynthesisDisplayEntry
    {
        public SynthesisDisplayEntry(
            SynthesisDisplayEntryType entryType,
            string entryId,
            Sprite icon,
            string name,
            string tag,
            string description,
            string owned,
            string cost,
            SynthesisLevelUpRequirementData levelUpRequirement = null)
        {
            EntryType = entryType;
            EntryId = entryId ?? string.Empty;
            Icon = icon;
            Name = name ?? string.Empty;
            Tag = tag ?? string.Empty;
            Description = description ?? string.Empty;
            Owned = owned ?? string.Empty;
            Cost = cost ?? string.Empty;
            LevelUpRequirement = levelUpRequirement;
        }

        public SynthesisDisplayEntryType EntryType { get; }
        public string EntryId { get; }
        public string RecipeId => EntryId;
        public Sprite Icon { get; }
        public string Name { get; }
        public string Tag { get; }
        public string Description { get; }
        public string Owned { get; }
        public string Cost { get; }
        public SynthesisLevelUpRequirementData LevelUpRequirement { get; }
    }

    private readonly struct SynthesisMaterialPanelCost
    {
        public SynthesisMaterialPanelCost(string itemId, ItemData item, int requiredCount)
        {
            ItemId = itemId ?? string.Empty;
            Item = item;
            RequiredCount = Mathf.Max(0, requiredCount);
        }

        public string ItemId { get; }
        public ItemData Item { get; }
        public int RequiredCount { get; }

        public SynthesisMaterialPanelCost AddCount(int count)
        {
            return new SynthesisMaterialPanelCost(ItemId, Item, RequiredCount + Mathf.Max(0, count));
        }
    }

    private enum SynthesisCategory
    {
        All,
        Consumable,
        Weapon,
        Armor,
        Accessory,
        Other
    }

    private enum SynthesisDisplayEntryType
    {
        Recipe,
        LevelUpRequirement
    }
}
