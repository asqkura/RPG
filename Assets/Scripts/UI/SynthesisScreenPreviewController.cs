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
    [SerializeField] private TMP_Text materialCostText;
    [SerializeField] private TMP_Text moneyCostText;
    [SerializeField] private TMP_Text ownedText;
    [SerializeField] private EquipmentDetailPanelView equipmentDetailPanelView;
    [SerializeField] private TMP_Text helpText;
    [SerializeField] private TMP_Text moneyText;
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
    [SerializeField] private SynthesisRecipeDatabase recipeDatabase;
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
        RegisterCategoryButtons();
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.AddListener(SubmitCurrentRecipe);
        }

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
    }

    private void SubmitCurrentRecipe()
    {
        if (selectedRow == null || synthesisService == null || runSaveData == null)
        {
            return;
        }

        var recipeId = selectedRow.RecipeId;
        var result = synthesisService.TrySynthesize(runSaveData, recipeId);
        if (!result.CanSynthesize)
        {
            SetHelpText(FormatFailure(result.FailureReason));
            RefreshActionButtonState();
            return;
        }

        PopulateRows();
        SelectRowByRecipeId(recipeId);
        RefreshMoneyText();
        SetHelpText($"{selectedRow.ItemName}を合成しました。");
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

    private void ShowCategory(SynthesisCategory category)
    {
        if (currentCategory == category)
        {
            return;
        }

        currentCategory = category;
        RefreshCategoryButtons();
        PopulateRows();
        SelectFirstRow();
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

        if (recipeDatabase != null && recipeDatabase.TryGetById(row.RecipeId, out var recipe))
        {
            RefreshCostTexts(recipe);
        }

        if (equipmentDetailsByRecipeId.TryGetValue(row.RecipeId, out var equipmentDetail))
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

        if (materialCostText != null)
        {
            materialCostText.text = string.Empty;
        }

        if (moneyCostText != null)
        {
            moneyCostText.text = string.Empty;
        }

        if (ownedText != null)
        {
            ownedText.text = string.Empty;
        }

        equipmentDetailPanelView?.Hide();
        SetHelpText(string.Empty);
        RefreshActionButtonState();
    }

    private void PopulateRows()
    {
        displayEntries.Clear();
        equipmentDetailsByRecipeId.Clear();

        if (recipeDatabase == null)
        {
            ClearRows();
            return;
        }

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

    private void InitializeGameState()
    {
        runSaveData = GameSession.GetOrCreate().RunSaveData;
        synthesisService = new SynthesisService(recipeDatabase, itemDatabase, equipmentDatabase);
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
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(allCategoryButton, currentCategory == SynthesisCategory.All);
        SetCategoryButtonState(consumableCategoryButton, currentCategory == SynthesisCategory.Consumable);
        SetCategoryButtonState(weaponCategoryButton, currentCategory == SynthesisCategory.Weapon);
        SetCategoryButtonState(armorCategoryButton, currentCategory == SynthesisCategory.Armor);
        SetCategoryButtonState(accessoryCategoryButton, currentCategory == SynthesisCategory.Accessory);
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

    private void RefreshCostTexts(SynthesisRecipeData recipe)
    {
        if (materialCostText != null)
        {
            materialCostText.text = FormatMaterialCosts(recipe);
        }

        if (moneyCostText != null)
        {
            moneyCostText.text = recipe.MoneyCost > 0 ? recipe.MoneyCost.ToString() : "0";
        }

        if (ownedText != null)
        {
            ownedText.text = FormatOwnedForRecipe(recipe);
        }
    }

    private string FormatMaterialCosts(SynthesisRecipeData recipe)
    {
        if (recipe == null || recipe.MaterialCosts.Count == 0)
        {
            return "なし";
        }

        var lines = new List<string>();
        foreach (var cost in recipe.MaterialCosts)
        {
            if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId))
            {
                continue;
            }

            var ownedCount = runSaveData?.GetMaterialCount(cost.ItemId) ?? 0;
            var materialName = FormatMaterialName(cost);
            var shortage = ownedCount < cost.Count ? " 不足" : string.Empty;
            lines.Add($"{materialName} {ownedCount}/{cost.Count}{shortage}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "なし";
    }

    private string FormatRecipeCost(SynthesisRecipeData recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        return recipe.MoneyCost > 0 ? recipe.MoneyCost.ToString() : "-";
    }

    private string FormatOwnedForRecipe(SynthesisRecipeData recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        if (recipe.ProductType == SynthesisProductDataType.Consumable)
        {
            return FormatOwnedCount(runSaveData?.GetConsumableCount(recipe.ProductId) ?? 0);
        }

        return FormatOwnedCount(GetOwnedEquipmentCount(recipe.ProductId));
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

        return cost.Item != null
            && !string.IsNullOrWhiteSpace(cost.Item.DisplayName)
            ? cost.Item.DisplayName
            : cost.ItemId;
    }

    private void RefreshMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = runSaveData != null ? $"{runSaveData.Money:N0}" : "0";
        }
    }

    private void RefreshActionButtonState()
    {
        if (synthesizeButton == null)
        {
            return;
        }

        var canSubmit = selectedRow != null
            && synthesisService != null
            && runSaveData != null
            && synthesisService.GetQuote(runSaveData, selectedRow.RecipeId).CanSynthesize;

        synthesizeButton.interactable = canSubmit;
        if (actionButtonLabel != null)
        {
            actionButtonLabel.color = canSubmit ? AccentTextColor : TextColor;
        }
    }

    private string FormatDetailHelp(SynthesisRecipeRowView row)
    {
        if (row == null)
        {
            return string.Empty;
        }

        if (row != selectedRow)
        {
            return $"{row.ItemName}の詳細です。クリックで対象にします。";
        }

        var quote = synthesisService != null && runSaveData != null
            ? synthesisService.GetQuote(runSaveData, row.RecipeId)
            : default;
        return quote.CanSynthesize
            ? $"{row.ItemName}を合成対象にしています。"
            : FormatFailure(quote.FailureReason);
    }

    private void SetHelpText(string message)
    {
        if (helpText != null)
        {
            helpText.text = message;
        }
    }

    private static string FormatFailure(SynthesisFailureReason reason)
    {
        return reason switch
        {
            SynthesisFailureReason.RecipeNotFound => "レシピデータが見つかりません。",
            SynthesisFailureReason.NotAvailableInCurrentPhase => "このレシピはまだ合成できません。",
            SynthesisFailureReason.ProductNotFound => "合成結果のデータが見つかりません。",
            SynthesisFailureReason.NotEnoughMaterials => "素材が足りません。",
            SynthesisFailureReason.NotEnoughMoney => "所持金が足りません。",
            _ => "合成できません。"
        };
    }

    private EquipmentDetailData BuildEquipmentDetailData(EquipmentData equipment)
    {
        var detail = new EquipmentDetailData
        {
            Description = equipment.Description
        };

        AddStatData(detail.Stats, "HP", equipment.StatModifiers?.Hp ?? 0);
        AddStatData(detail.Stats, "SP", equipment.StatModifiers?.Sp ?? 0);
        AddStatData(detail.Stats, "攻撃", equipment.StatModifiers?.Attack ?? 0);
        AddStatData(detail.Stats, "魔力", equipment.StatModifiers?.Magic ?? 0);
        AddStatData(detail.Stats, "防御", equipment.StatModifiers?.Defense ?? 0);
        AddStatData(detail.Stats, "素早さ", equipment.StatModifiers?.Speed ?? 0);
        AddStatData(detail.Stats, "会心率", Mathf.RoundToInt((equipment.StatModifiers?.CriticalRate ?? 0f) * 100f), "%");

        foreach (var skillId in equipment.BaseSkillIds)
        {
            var skillName = FormatSkillName(skillId, string.Empty);
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(skillName));
            }
        }

        foreach (var trait in equipment.BaseTraits)
        {
            if (trait != null)
            {
                detail.FixedSkills.Add(MasterDataDisplayLabels.FormatTag(FormatBaseTraitType(trait.TraitType)));
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

    private readonly struct SynthesisDisplayEntry
    {
        public SynthesisDisplayEntry(
            string recipeId,
            Sprite icon,
            string name,
            string tag,
            string description,
            string owned,
            string cost)
        {
            RecipeId = recipeId ?? string.Empty;
            Icon = icon;
            Name = name ?? string.Empty;
            Tag = tag ?? string.Empty;
            Description = description ?? string.Empty;
            Owned = owned ?? string.Empty;
            Cost = cost ?? string.Empty;
        }

        public string RecipeId { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Tag { get; }
        public string Description { get; }
        public string Owned { get; }
        public string Cost { get; }
    }

    private enum SynthesisCategory
    {
        All,
        Consumable,
        Weapon,
        Armor,
        Accessory
    }
}
