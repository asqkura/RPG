using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentDetailPanelView : MonoBehaviour
{
    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color MutedTextColor = new(0.62f, 0.58f, 0.52f, 1f);
    private static readonly Color AccentTextColor = new(1f, 0.9f, 0.62f, 1f);
    private static readonly Color DividerColor = new(0.45f, 0.36f, 0.25f, 0.55f);
    private static readonly Color RowColor = new(0.1f, 0.09f, 0.075f, 0.35f);

    private RectTransform contentRoot;

    public void Show(EquipmentDetailData data)
    {
        EnsureContentRoot();
        ClearChildren(transform);
        gameObject.SetActive(true);

        if (!string.IsNullOrWhiteSpace(data.Description))
        {
            var description = GetTextItem("Description", 20f, TextAlignmentOptions.TopLeft, TextColor);
            description.text = data.Description;
        }

        AddSection("ステータス");
        if (data.Stats.Count > 0)
        {
            foreach (var stat in data.Stats)
            {
                AddRow(stat.Label, stat.Value, true);
            }
        }
        else
        {
            AddRow("なし", "-");
        }

        AddSection("スキル");
        AddListRows(data.FixedSkills);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void EnsureContentRoot()
    {
        if (contentRoot != null)
        {
            return;
        }

        contentRoot = gameObject.GetComponent<RectTransform>();
        var layout = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void AddSection(string title)
    {
        var header = GetTextItem("Section", 20f, TextAlignmentOptions.MidlineLeft, AccentTextColor);
        header.text = title;
        header.fontStyle = FontStyles.Bold;

        var divider = GetRectItem("Divider");
        var image = divider.GetComponent<Image>() ?? divider.gameObject.AddComponent<Image>();
        image.color = DividerColor;
        var layout = divider.GetComponent<LayoutElement>() ?? divider.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 1f;
        layout.preferredHeight = 1f;
    }

    private void AddListRows(IReadOnlyList<string> values)
    {
        if (values == null || values.Count == 0)
        {
            AddRow("なし", "-");
            return;
        }

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                AddRow(value, string.Empty);
            }
        }
    }

    private void AddRow(string label, string value, bool accentValue = false)
    {
        var row = GetRectItem("Row");
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(12, 12, 4, 4);
        rowLayout.spacing = 12f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var image = row.GetComponent<Image>() ?? row.gameObject.AddComponent<Image>();
        image.color = RowColor;

        ClearChildren(row);

        var labelText = CreateText("Label", row, 19f, TextAlignmentOptions.MidlineLeft, string.IsNullOrWhiteSpace(value) ? TextColor : MutedTextColor);
        labelText.text = label;
        var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = string.IsNullOrWhiteSpace(value) ? 1f : 0f;
        labelLayout.preferredWidth = string.IsNullOrWhiteSpace(value) ? 420f : 170f;

        if (!string.IsNullOrWhiteSpace(value))
        {
            var valueText = CreateText("Value", row, 19f, TextAlignmentOptions.MidlineRight, accentValue ? AccentTextColor : TextColor);
            valueText.text = value;
            var valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
        }
    }

    private TMP_Text GetTextItem(string name, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var rect = GetRectItem(name);
        ClearChildren(rect);
        var text = rect.GetComponent<TMP_Text>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.fontStyle = FontStyles.Normal;
        return text;
    }

    private RectTransform GetRectItem(string name)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(contentRoot, false);
        item.name = name;
        return item.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var text = rect.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}

public sealed class EquipmentDetailData
{
    public string Description { get; set; } = string.Empty;
    public List<EquipmentDetailStat> Stats { get; } = new();
    public List<string> FixedSkills { get; } = new();
}

public readonly struct EquipmentDetailStat
{
    public EquipmentDetailStat(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}
