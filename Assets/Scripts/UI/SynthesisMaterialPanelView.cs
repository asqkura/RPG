using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct SynthesisMaterialPanelEntry
{
    public SynthesisMaterialPanelEntry(Sprite icon, string displayName, int ownedCount, int requiredCount, bool isShortage)
    {
        Icon = icon;
        DisplayName = displayName ?? string.Empty;
        OwnedCount = Mathf.Max(0, ownedCount);
        RequiredCount = Mathf.Max(0, requiredCount);
        IsShortage = isShortage;
    }

    public Sprite Icon { get; }
    public string DisplayName { get; }
    public int OwnedCount { get; }
    public int RequiredCount { get; }
    public bool IsShortage { get; }
}

public sealed class SynthesisMaterialPanelView : MonoBehaviour
{
    [SerializeField] private SynthesisMaterialPanelSlotView[] slots = { };
    [SerializeField] private TMP_Text overflowText;
    [SerializeField] private TMP_Text emptyText;

    public void Show(IReadOnlyList<SynthesisMaterialPanelEntry> entries, int overflowCount)
    {
        gameObject.SetActive(true);

        var count = entries?.Count ?? 0;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < count)
            {
                slot.Show(entries[i]);
            }
            else
            {
                slot.Clear();
            }
        }

        if (overflowText != null)
        {
            overflowText.gameObject.SetActive(overflowCount > 0);
            overflowText.text = overflowCount > 0 ? $"+{overflowCount}" : string.Empty;
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(count == 0 && overflowCount <= 0);
            emptyText.text = "必要素材なし";
        }
    }

    public void Clear()
    {
        foreach (var slot in slots)
        {
            slot?.Clear();
        }

        if (overflowText != null)
        {
            overflowText.gameObject.SetActive(false);
            overflowText.text = string.Empty;
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
            emptyText.text = string.Empty;
        }
    }
}
