using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Cursor[] targetCursors;
    private Image hoverBackground;
    private static ButtonHover activeButton;
    public event System.Action<bool> HighlightChanged;
    public bool IsHighlighted => activeButton == this;
    private static readonly Color BackgroundNormal = new Color(0.88f, 0.88f, 0.88f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => activeButton = null;

    public void SetCursors(Cursor[] cursors) => targetCursors = cursors;
    public void OnSelect(BaseEventData eventData) => Activate();
    public void OnDeselect(BaseEventData eventData) { }

    public void SetBackground(Image background)
    {
        hoverBackground = background;
        SetBackgroundState(true, 0f);
    }

    private void OnDisable()
    {
        // 页面隐藏时清理保留的光标，避免返回时沿用旧状态。
        ResetHover();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.IsInteractable()) return;
        Activate();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void ResetHover()
    {
        if (activeButton == this) activeButton = null;
        SetState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 移出命中区域后保留动画，直到另一个按钮获得悬停或导航焦点。
    }

    private void Activate()
    {
        if (activeButton == this) { SetState(false); return; }
        if (activeButton != null) activeButton.SetState(true);
        activeButton = this;
        SetState(false);
    }

    private void SetState(bool isNormal)
    {
        SetBackgroundState(isNormal, 0.08f);
        HighlightChanged?.Invoke(!isNormal);

        if (targetCursors == null)
            return;

        foreach (Cursor cursor in targetCursors)
        {
            if (cursor != null)
                cursor.SetBtnState(isNormal);
        }
    }

    private void SetBackgroundState(bool isNormal, float duration)
    {
        if (hoverBackground != null)
        {
            // 只改变 RGB 亮度，保持 Alpha 为 1，不产生透明度过渡。
            hoverBackground.CrossFadeColor(isNormal ? BackgroundNormal : Color.white,
                duration, true, false, true);
        }
    }
}
