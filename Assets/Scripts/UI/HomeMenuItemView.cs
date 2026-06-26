using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HomeMenuItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string description;
    [SerializeField] private Image windowImage;
    [SerializeField] private Sprite normalWindowSprite;
    [SerializeField] private Sprite highlightedWindowSprite;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Color normalTextColor = new(0.86f, 0.82f, 0.75f, 1f);
    [SerializeField] private Color highlightedTextColor = new(1f, 0.96f, 0.84f, 1f);

    private HomeMenuController controller;

    public string Description => description;

    public void Initialize(HomeMenuController owner)
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
        controller?.Clear(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        controller?.Hover(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        controller?.Clear(this);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (windowImage != null)
        {
            windowImage.sprite = highlighted && highlightedWindowSprite != null
                ? highlightedWindowSprite
                : normalWindowSprite;
        }

        if (labelText != null)
        {
            labelText.color = highlighted ? highlightedTextColor : normalTextColor;
        }

        if (iconImage != null)
        {
            iconImage.color = Color.white;
        }
    }
}
