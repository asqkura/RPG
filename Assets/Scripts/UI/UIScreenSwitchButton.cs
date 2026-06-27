using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIScreenSwitchButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private string hideTargetName;
    [SerializeField] private string showTargetName;

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
        var resolvedHideTarget = ResolveTarget(hideTargetName);
        var resolvedShowTarget = ResolveTarget(showTargetName);

        if (resolvedHideTarget != null)
        {
            resolvedHideTarget.SetActive(false);
        }

        if (resolvedShowTarget != null)
        {
            resolvedShowTarget.SetActive(true);
        }
    }

    private static GameObject ResolveTarget(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == targetName)
            {
                return root;
            }
        }

        return null;
    }
}
