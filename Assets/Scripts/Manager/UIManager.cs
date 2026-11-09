using System.Collections.Generic;
//using Unity.VisualScripting;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.AddressableAssets;
using System;
using UnityEditor;
using UnityEngine.Events;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

public class UIManager
{
    private static UIManager _instant;
    public static UIManager Instant
    {
        get
        {
            if (_instant == null)
                _instant = new UIManager();
            return _instant;
        }
    }

    private Transform canvasTransform;

    private Transform CanvasTransform
    {
        get
        {
            if (canvasTransform == null)
                canvasTransform = GameObject.Find("Canvas").transform;
            return canvasTransform;
        }
    }

    private Transform bgCanvasTransform;

    private Transform BGCanvasTransform
    {
        get
        {
            if (bgCanvasTransform == null)
                bgCanvasTransform = GameObject.Find("BGCanvas").transform;
            return bgCanvasTransform;
        }
    }

    private readonly string panelPath = "UIPanel";
    //字典保存场景内的面板
    private Dictionary<UIPanelType, BasePanel> panelDict;
    //面板栈
    private Stack<BasePanel> panelStack;

    private const string BGPath = "MenuBG";

    //存放当前激活的背景
    //private MenuBackGround currentBG;
    private MenuBGType previousBGType;
    private MenuBGType currentBGType;
    private readonly List<MenuBGType> BGArray = Enum.GetValues(typeof(MenuBGType)).Cast<MenuBGType>().ToList();

    //获取面板
    // ReSharper disable Unity.PerformanceAnalysis
    public BasePanel GetPanel(UIPanelType type)
    {
        panelDict ??= new Dictionary<UIPanelType, BasePanel>();

        //获取枚举类型为type的BasePanel
        var Panel1 = panelDict.TryGetValue(type);

        if (Panel1 != null)
            return Panel1;


        //如果没有找到，则创建一个
        var path = panelPath + "/" + type.ToString();

        var prefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
        if (prefab == null)
            throw new Exception("找不到该UIPanelType的Prefab");

        var instPanel = GameObject.Instantiate(prefab, CanvasTransform, false);

        //创建为Canvas的子物体
        var panel = instPanel.GetComponent<BasePanel>();
        panelDict.Add(type, panel);

        return panel;
    }

    //面板栈的操作
    // ReSharper disable Unity.PerformanceAnalysis
    public void PushPanel(UIPanelType type)
    {
        panelStack ??= new Stack<BasePanel>();

        BasePanel topPanel = null;
        BasePanel currPanel = null;

        if (panelStack.Count > 0)
            topPanel = panelStack.Peek();
        currPanel = GetPanel(type);
        panelStack.Push(currPanel);

        UnityAction finishEvent = currPanel.OnEnter;

        if (topPanel)
            topPanel.OnPause(finishEvent);
        else
            currPanel.OnEnter();
    }

    // 弹出顶部面板
    // ReSharper disable Unity.PerformanceAnalysis
    public void PopPanel()
    {
        panelStack ??= new Stack<BasePanel>();

        BasePanel topPanel = null;

        if (panelStack.Count == 0) return;
        topPanel = panelStack.Pop();

        UnityAction finishEvent = null;
        if (panelStack.Count != 0)
        {
            var currPanel = panelStack.Peek();
            finishEvent = currPanel.OnResume;
        }
        topPanel.OnExit(finishEvent);
    }

    // 清空面板
    public void ClearPanel()
    {
        panelStack?.Clear();
        panelDict?.Clear();
    }

    ////获取背景
    //public MenuBackGround GetBG(MenuBGType type)
    //{
    //    string path = BGPath + "/" + type.ToString();
    //    var prefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
    //    if (!prefab)
    //        throw new Exception("找不到该MenuBGType的Prefab");

    //    GameObject instBG = GameObject.Instantiate(prefab, BGCanvasTransform, false);
    //    //创建为Canvas的子物体
    //    return instBG.GetComponent<MenuBackGround>();
    //}

    //public void ExchangeBG()
    //{
    //    int index = DataManager.Instant.GetOptionData("Menu Style");
    //    var type = BGArray[index];

    //    if (currentBGType == type && currentBG) return;

    //    MenuBackGround newBG = GetBG(type);
    //    if (currentBG)
    //        currentBG.UnShow(newBG.Show);
    //    else
    //        newBG.Show();

    //    currentBG = newBG;
    //    previousBGType = currentBGType;
    //    currentBGType = type;
    //}

    //public void ExchangeBGByType(MenuBGType type)
    //{
    //    var newBG = GetBG(type);
    //    previousBGType = currentBGType;

    //    if (currentBG != null)
    //        currentBG.UnShow(newBG.Show);
    //    else
    //        newBG.Show();

    //    currentBG = newBG;
    //    currentBGType = type;
    //}

    //public MenuBGType GetPreviousBGType()
    //{
    //    return previousBGType;
    //}

    //public void Enter()
    //{
    //    ExchangeBG();
    //}
}
