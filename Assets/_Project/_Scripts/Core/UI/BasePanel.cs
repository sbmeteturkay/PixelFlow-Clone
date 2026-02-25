using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public abstract class BasePanel : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    [SerializeField] Button closeButton;

    protected virtual void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        Subscribe();
        if (closeButton)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDestroy() => Unsubscribe();

    protected abstract void Subscribe();
    protected abstract void Unsubscribe();

    protected void Show()
    {
        Tween.Alpha(_canvasGroup, 1f, 0.3f);
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    protected void Hide()
    {
        Tween.Alpha(_canvasGroup, 0f, 0.3f);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    } 
}