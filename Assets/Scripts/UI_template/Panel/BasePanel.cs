using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//面板基类：状态机
public class BasePanel : MonoBehaviour
{
    public GameObject firstSelectedOgj = null;
    protected CanvasGroup canvasGroup;
    protected Button CloseButton = null;
    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        CloseButton = FindCloseButton("CloseButton");
        if (canvasGroup == null)
            canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    public virtual void OnEnter()//开始面板
    {
        SetFirstObj();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
    }
    public virtual void OnResume()//继续（当上层面板关闭时）
    {
        SetFirstObj();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
    }
    public virtual void OnPause(UnityAction finishEvent = null)//暂停（当有上层面板覆盖时）
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        if (finishEvent != null)
        {
            finishEvent();
        }
    }
    public virtual void OnExit(UnityAction finishEvent = null)//关闭面板
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        if (finishEvent != null)
        {
            finishEvent();
        }
    }

    private void SetFirstObj()
    {
        var eventSystemManager = EventSystemManager.instance;
        if (eventSystemManager != null)
            eventSystemManager.lastGameObject = firstSelectedOgj;

        var eventSystem = EventSystem.current;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(firstSelectedOgj);
    }

    private Button FindCloseButton(string childName)
    {
        Button closeButton = null;
        foreach (var button in GetComponentsInChildren<Button>())
        {
            if (button.name == childName)
                closeButton = button;
        }
        return closeButton;
    }
}
