using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SynthesisConsumableResultViewData
{
    public Sprite Icon { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string TagText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class SynthesisEquipmentResultViewData
{
    public Sprite Icon { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string TagText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EquipmentDetailData DefaultDetail { get; set; }
    public EquipmentDetailData ResultDetail { get; set; }
}

public sealed class SynthesisResultScreenView : MonoBehaviour
{
    [SerializeField] private GameObject consumableResultPanel;
    [SerializeField] private TMP_Text consumableTitleText;
    [SerializeField] private TMP_Text consumableTagText;
    [SerializeField] private Image consumableIconImage;
    [SerializeField] private TMP_Text consumableDescriptionText;
    [SerializeField] private Button consumableCloseButton;

    [SerializeField] private GameObject equipmentResultPanel;
    [SerializeField] private TMP_Text equipmentTitleText;
    [SerializeField] private TMP_Text equipmentTagText;
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private TMP_Text equipmentDescriptionText;
    [SerializeField] private EquipmentDetailPanelView defaultEquipmentDetailPanelView;
    [SerializeField] private EquipmentDetailPanelView resultEquipmentDetailPanelView;
    [SerializeField] private Button equipmentCloseButton;

    private bool showRequested;

    private void Awake()
    {
        if (consumableCloseButton != null)
        {
            consumableCloseButton.onClick.AddListener(Hide);
        }

        if (equipmentCloseButton != null)
        {
            equipmentCloseButton.onClick.AddListener(Hide);
        }

        if (!showRequested)
        {
            Hide();
        }
    }

    private void OnDestroy()
    {
        if (consumableCloseButton != null)
        {
            consumableCloseButton.onClick.RemoveListener(Hide);
        }

        if (equipmentCloseButton != null)
        {
            equipmentCloseButton.onClick.RemoveListener(Hide);
        }
    }

    public void ShowConsumable(SynthesisConsumableResultViewData data)
    {
        showRequested = true;
        gameObject.SetActive(true);
        SetPanelActive(consumableResultPanel, true);
        SetPanelActive(equipmentResultPanel, false);

        if (consumableTitleText != null)
        {
            consumableTitleText.text = data?.DisplayName ?? string.Empty;
        }

        if (consumableTagText != null)
        {
            consumableTagText.text = data?.TagText ?? string.Empty;
        }

        SetIcon(consumableIconImage, data?.Icon);

        if (consumableDescriptionText != null)
        {
            consumableDescriptionText.text = data?.Description ?? string.Empty;
        }
    }

    public void ShowEquipment(SynthesisEquipmentResultViewData data)
    {
        showRequested = true;
        gameObject.SetActive(true);
        SetPanelActive(consumableResultPanel, false);
        SetPanelActive(equipmentResultPanel, true);

        if (equipmentTitleText != null)
        {
            equipmentTitleText.text = data?.DisplayName ?? string.Empty;
        }

        if (equipmentTagText != null)
        {
            equipmentTagText.text = data?.TagText ?? string.Empty;
        }

        SetIcon(equipmentIconImage, data?.Icon);

        if (equipmentDescriptionText != null)
        {
            equipmentDescriptionText.text = data?.Description ?? string.Empty;
        }

        if (data?.DefaultDetail != null)
        {
            defaultEquipmentDetailPanelView?.Show(data.DefaultDetail);
        }
        else
        {
            defaultEquipmentDetailPanelView?.Hide();
        }

        if (data?.ResultDetail != null)
        {
            resultEquipmentDetailPanelView?.Show(data.ResultDetail);
        }
        else
        {
            resultEquipmentDetailPanelView?.Hide();
        }
    }

    public void Hide()
    {
        showRequested = false;
        SetPanelActive(consumableResultPanel, false);
        SetPanelActive(equipmentResultPanel, false);
        gameObject.SetActive(false);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void SetIcon(Image image, Sprite icon)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = icon;
        image.enabled = icon != null;
    }
}
