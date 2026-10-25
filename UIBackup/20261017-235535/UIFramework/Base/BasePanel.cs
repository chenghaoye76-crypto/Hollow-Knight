using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 所有UI面板的父类，包含UI的状态信息
/// </summary>
public class BasePanel
{
    /// <summary>
    /// UI信息
    /// </summary>
    public UIType UIType { get; private set; } 
    public GameObject firstSelectedOgj = null;
    protected CanvasGroup canvasGroup;
    protected Button CloseButton = null;
    //protected virtual void Awake()
    //{
    //    canvasGroup = GetComponent<CanvasGroup>();
    //    CloseButton = GetComponent<Button>();
    //    if (canvasGroup == null)
    //        canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
    //    canvasGroup.alpha = 0f;
    //    canvasGroup.blocksRaycasts = false;
    //}
    public BasePanel(UIType uiType)
    {
        UIType = uiType;
    }
    /// <summary>
    /// UI进入时执行的操作，只会执行一次
    /// </summary>
    public virtual void OnEnter()
    {
        SetFirstObj();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
    }
    /// <summary>
    /// UI暂停时的操作
    /// </summary>
    public virtual void OnPause(UnityAction finishEvent = null)
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        if (finishEvent != null)
        {
            finishEvent();
        }
    }
    /// <summary>
    /// UI执行时的操作
    /// </summary>
    public virtual void OnResume()
    {
        SetFirstObj();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
    }
    /// <summary>
    /// UI退出时执行的操作
    /// </summary>
    /// <param name="finishEvent"></param>
    public virtual void OnExit(UnityAction finishEvent = null)
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        if (finishEvent != null)
        {
            finishEvent();
        }
    }
    protected void SetFirstObj()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedOgj);
    }
}
