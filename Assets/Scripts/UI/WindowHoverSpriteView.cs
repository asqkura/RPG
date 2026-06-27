using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class WindowHoverSpriteView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image windowImage;
    [SerializeField] private Sprite normalWindowSprite;
    [SerializeField] private Sprite highlightedWindowSprite;

    private bool selected;

    private void Awake()
    {
        SetHighlighted(selected);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        SetHighlighted(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(selected);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(selected);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (windowImage == null)
        {
            return;
        }

        windowImage.sprite = highlighted && highlightedWindowSprite != null
            ? highlightedWindowSprite
            : normalWindowSprite;
    }
}
