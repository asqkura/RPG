using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopItemRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string itemName;
    [SerializeField] private string detailText;
    [SerializeField] private string helpText;
    [SerializeField] private string stockText;
    [SerializeField] private string ownedText;
    [SerializeField] private string priceText;
    [SerializeField] private Image windowImage;
    [SerializeField] private Graphic selectionMarker;
    [SerializeField] private Sprite normalWindowSprite;
    [SerializeField] private Sprite highlightedWindowSprite;
    [SerializeField] private TMP_Text[] labelTexts = { };
    [SerializeField] private Color normalTextColor = new(0.86f, 0.82f, 0.75f, 1f);
    [SerializeField] private Color highlightedTextColor = new(1f, 0.96f, 0.84f, 1f);

    private ShopScreenPreviewController controller;

    public string ItemName => itemName;
    public string DetailText => detailText;
    public string HelpText => helpText;
    public string StockText => stockText;
    public string OwnedText => ownedText;
    public string PriceText => priceText;

    public void Initialize(ShopScreenPreviewController owner)
    {
        controller = owner;
        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller?.Hover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    public void OnSelect(BaseEventData eventData)
    {
        controller?.Hover(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
    }

    public void SetHighlighted(bool highlighted)
    {
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
                windowImage.color = normalWindowSprite != null ? Color.white : Color.clear;
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
            selectionMarker.gameObject.SetActive(highlighted);
        }
    }
}
