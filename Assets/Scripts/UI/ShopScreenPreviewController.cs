using TMPro;
using UnityEngine;

public sealed class ShopScreenPreviewController : MonoBehaviour
{
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailBodyText;
    [SerializeField] private TMP_Text detailStockText;
    [SerializeField] private TMP_Text detailOwnedText;
    [SerializeField] private TMP_Text detailPriceText;
    [SerializeField] private TMP_Text helpText;
    [SerializeField] private ShopItemRowView[] itemRows = { };

    private ShopItemRowView currentRow;

    private void Awake()
    {
        foreach (var row in itemRows)
        {
            if (row != null)
            {
                row.Initialize(this);
            }
        }

        if (itemRows.Length > 0 && itemRows[0] != null)
        {
            Hover(itemRows[0]);
        }
        else
        {
            ClearDetail();
        }
    }

    public void Hover(ShopItemRowView row)
    {
        if (row == null || currentRow == row)
        {
            return;
        }

        ClearCurrentRow();
        currentRow = row;
        currentRow.SetHighlighted(true);

        if (detailTitleText != null)
        {
            detailTitleText.text = row.ItemName;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = row.DetailText;
        }

        if (detailStockText != null)
        {
            detailStockText.text = row.StockText;
        }

        if (detailOwnedText != null)
        {
            detailOwnedText.text = row.OwnedText;
        }

        if (detailPriceText != null)
        {
            detailPriceText.text = row.PriceText;
        }

        if (helpText != null)
        {
            helpText.text = row.HelpText;
        }
    }

    public void Clear(ShopItemRowView row)
    {
    }

    private void ClearCurrentRow()
    {
        if (currentRow != null)
        {
            currentRow.SetHighlighted(false);
            currentRow = null;
        }
    }

    private void ClearDetail()
    {
        if (detailTitleText != null)
        {
            detailTitleText.text = string.Empty;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = string.Empty;
        }

        if (detailStockText != null)
        {
            detailStockText.text = string.Empty;
        }

        if (detailOwnedText != null)
        {
            detailOwnedText.text = string.Empty;
        }

        if (detailPriceText != null)
        {
            detailPriceText.text = string.Empty;
        }

        if (helpText != null)
        {
            helpText.text = string.Empty;
        }
    }
}
