using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class WindowHoverSpriteView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image windowImage;
    [SerializeField] private Sprite normalWindowSprite;
    [SerializeField] private Sprite highlightedWindowSprite;

    private void Awake()
    {
        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
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
