using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIScreenSwitchButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private GameObject hideTarget;
    [SerializeField] private GameObject showTarget;

    public void OnPointerClick(PointerEventData eventData)
    {
        Switch();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Switch();
    }

    private void Switch()
    {
        if (hideTarget != null)
        {
            hideTarget.SetActive(false);
        }

        if (showTarget != null)
        {
            showTarget.SetActive(true);
        }
    }
}
