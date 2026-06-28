using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SynthesisMaterialPanelSlotView : MonoBehaviour
{
    private static readonly Color NormalTextColor = new(0.86f, 0.82f, 0.75f, 1f);
    private static readonly Color ShortageTextColor = new(0.95f, 0.42f, 0.38f, 1f);

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    public void Show(SynthesisMaterialPanelEntry entry)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = entry.Icon;
            iconImage.enabled = entry.Icon != null;
        }

        if (nameText != null)
        {
            nameText.text = entry.DisplayName;
            nameText.color = entry.IsShortage ? ShortageTextColor : NormalTextColor;
        }

        if (countText != null)
        {
            countText.text = $"{entry.OwnedCount}/{entry.RequiredCount}";
            countText.color = entry.IsShortage ? ShortageTextColor : NormalTextColor;
        }

    }

    public void Clear()
    {
        gameObject.SetActive(false);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameText != null)
        {
            nameText.text = string.Empty;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }
    }
}
