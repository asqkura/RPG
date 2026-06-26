using TMPro;
using UnityEngine;

public sealed class HomeMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private HomeMenuItemView[] menuItems = { };

    private HomeMenuItemView currentItem;

    private void Awake()
    {
        foreach (var item in menuItems)
        {
            if (item != null)
            {
                item.Initialize(this);
            }
        }

        ClearDescription();
    }

    public void Hover(HomeMenuItemView item)
    {
        if (item == null || currentItem == item)
        {
            return;
        }

        ClearCurrentItem();

        currentItem = item;
        currentItem.SetHighlighted(true);

        if (descriptionText != null)
        {
            descriptionText.text = item.Description;
        }
    }

    public void Clear(HomeMenuItemView item)
    {
        if (item == null || currentItem != item)
        {
            return;
        }

        ClearCurrentItem();
        ClearDescription();
    }

    private void ClearCurrentItem()
    {
        if (currentItem != null)
        {
            currentItem.SetHighlighted(false);
            currentItem = null;
        }
    }

    private void ClearDescription()
    {
        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }
    }
}
