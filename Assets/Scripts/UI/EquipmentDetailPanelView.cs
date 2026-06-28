using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class EquipmentDetailPanelView : MonoBehaviour
{
    private static readonly Color TextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color PositiveTextColor = new(0.48f, 0.86f, 0.56f, 1f);
    private static readonly Color NegativeTextColor = new(0.95f, 0.42f, 0.38f, 1f);

    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text spValueText;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text magicValueText;
    [SerializeField] private TMP_Text defenseValueText;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text criticalRateValueText;
    [SerializeField] private TMP_Text fixedSkillsText;

    public void Show(EquipmentDetailData data)
    {
        gameObject.SetActive(true);
        if (data == null)
        {
            return;
        }

        SetFixedStat(hpValueText, data.FindStat("HP"));
        SetFixedStat(spValueText, data.FindStat("SP"));
        SetFixedStat(attackValueText, data.FindStat("攻撃"));
        SetFixedStat(magicValueText, data.FindStat("魔力"));
        SetFixedStat(defenseValueText, data.FindStat("防御"));
        SetFixedStat(speedValueText, data.FindStat("素早さ"));
        SetFixedStat(criticalRateValueText, data.FindStat("会心率"));
        fixedSkillsText.text = FormatFixedSkills(data.FixedSkills);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void SetFixedStat(TMP_Text text, EquipmentDetailStat stat)
    {
        if (text == null)
        {
            return;
        }

        text.text = string.IsNullOrWhiteSpace(stat.Value) ? "-" : stat.Value;
        text.color = GetValueColor(stat.Sign);
    }

    private static Color GetValueColor(int statSign)
    {
        if (statSign > 0)
        {
            return PositiveTextColor;
        }

        if (statSign < 0)
        {
            return NegativeTextColor;
        }

        return TextColor;
    }

    private static string FormatFixedSkills(IReadOnlyList<string> fixedSkills)
    {
        if (fixedSkills == null || fixedSkills.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var fixedSkill in fixedSkills)
        {
            if (!string.IsNullOrWhiteSpace(fixedSkill))
            {
                lines.Add(fixedSkill);
            }
        }

        return string.Join("\n", lines);
    }
}

public sealed class EquipmentDetailData
{
    public string Description { get; set; } = string.Empty;
    public List<EquipmentDetailStat> Stats { get; } = new();
    public List<string> FixedSkills { get; } = new();

    public EquipmentDetailStat FindStat(string label)
    {
        foreach (var stat in Stats)
        {
            if (stat.Label == label)
            {
                return stat;
            }
        }

        return new EquipmentDetailStat(label, "-", 0);
    }
}

public readonly struct EquipmentDetailStat
{
    public EquipmentDetailStat(string label, string value)
        : this(label, value, 0)
    {
    }

    public EquipmentDetailStat(string label, string value, int sign)
    {
        Label = label;
        Value = value;
        Sign = sign;
    }

    public string Label { get; }
    public string Value { get; }
    public int Sign { get; }
}
