using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface ISynthesisRecipeRowViewController
{
    void Hover(SynthesisRecipeRowView row);
    void Select(SynthesisRecipeRowView row);
    void Clear(SynthesisRecipeRowView row);
}

public sealed class SynthesisRecipeRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string recipeId;
    [SerializeField] private string itemName;
    [SerializeField] private string tagText;
    [SerializeField] private string descriptionText;
    [SerializeField] private string ownedText;
    [SerializeField] private string costText;
    [SerializeField] private Image windowImage;
    [SerializeField] private Graphic selectionMarker;
    [SerializeField] private Sprite normalWindowSprite;
    [SerializeField] private Sprite highlightedWindowSprite;
    [SerializeField] private TMP_Text[] labelTexts = { };
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text ownedLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Color normalTextColor = new(0.86f, 0.82f, 0.75f, 1f);
    [SerializeField] private Color highlightedTextColor = new(1f, 0.96f, 0.84f, 1f);
    [SerializeField] private Color normalBackgroundColor = Color.clear;
    [SerializeField] private Color highlightedBackgroundColor = new(0.42f, 0.34f, 0.23f, 0.45f);

    private ISynthesisRecipeRowViewController controller;
    private Image hitAreaImage;

    public string RecipeId => recipeId;
    public string ItemName => itemName;
    public string TagText => tagText;
    public string DescriptionText => descriptionText;
    public string OwnedText => ownedText;
    public string CostText => costText;
    public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;

    public void Configure(
        string recipeId,
        Sprite icon,
        string displayName,
        string tag,
        string description,
        string owned,
        string cost)
    {
        this.recipeId = recipeId ?? string.Empty;
        itemName = displayName ?? string.Empty;
        tagText = tag ?? string.Empty;
        descriptionText = description ?? string.Empty;
        ownedText = owned ?? string.Empty;
        costText = cost ?? string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        SetLabelText(nameLabel, 0, itemName);
        SetLabelText(ownedLabel, 1, ownedText);
        SetLabelText(costLabel, 2, costText);

        gameObject.SetActive(true);
        SetHighlighted(false);
    }

    public void ClearRow()
    {
        recipeId = string.Empty;
        itemName = string.Empty;
        tagText = string.Empty;
        descriptionText = string.Empty;
        ownedText = string.Empty;
        costText = string.Empty;
        gameObject.SetActive(false);
    }

    public void Initialize(ISynthesisRecipeRowViewController owner)
    {
        controller = owner;
        EnsureHitArea();
        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller?.Hover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller?.Clear(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller?.Select(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        controller?.Select(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
    }

    public void SetHighlighted(bool highlighted)
    {
        EnsureHitArea();

        if (windowImage != null)
        {
            if (highlighted && highlightedWindowSprite != null)
            {
                windowImage.sprite = highlightedWindowSprite;
                windowImage.type = Image.Type.Sliced;
                windowImage.color = Color.white;
            }
            else
            {
                windowImage.sprite = normalWindowSprite;
                windowImage.type = normalWindowSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                windowImage.color = normalWindowSprite != null
                    ? Color.white
                    : highlighted ? highlightedBackgroundColor : normalBackgroundColor;
            }
        }

        foreach (var text in labelTexts)
        {
            if (text != null)
            {
                text.color = highlighted ? highlightedTextColor : normalTextColor;
            }
        }

        if (selectionMarker != null)
        {
            selectionMarker.gameObject.SetActive(false);
        }
    }

    private void SetLabelText(TMP_Text label, int fallbackIndex, string value)
    {
        if (label != null)
        {
            label.text = value;
            return;
        }

        if (fallbackIndex >= 0 && fallbackIndex < labelTexts.Length && labelTexts[fallbackIndex] != null)
        {
            labelTexts[fallbackIndex].text = value;
        }
    }

    private void EnsureHitArea()
    {
        if (hitAreaImage != null)
        {
            return;
        }

        hitAreaImage = GetComponent<Image>();
        if (hitAreaImage == null)
        {
            Debug.LogError($"{nameof(SynthesisRecipeRowView)} requires an Image component for pointer input.", this);
            return;
        }

        hitAreaImage.sprite = null;
        hitAreaImage.type = Image.Type.Simple;
        hitAreaImage.color = new Color(1f, 1f, 1f, 0.001f);
        hitAreaImage.raycastTarget = true;
    }
}
