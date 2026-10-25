using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 面板管理器，用栈来存储UI
/// </summary>
public class PanelManager
{
    public PanelManager()
    {
        stackPanel = new Stack<BasePanel>();
        uiManager = new UIManager();
    }
    /// <summary>
    /// 存储UI面板的栈
    /// </summary>
    private Stack<BasePanel> stackPanel;
    /// <summary>
    /// UI管理器
    /// </summary>
    private UIManager uiManager;
    private BasePanel basePanel;
    /// <summary>
    /// UI的入栈操作，此操作会显示一个面板
    /// </summary>
    /// <param name="nextPanel">要显示的面板</param>
    public void Push(BasePanel nextPanel)
    {
        if (stackPanel.Count > 0)
        {
            basePanel = stackPanel.Peek();
            basePanel.OnPause();
        }
        stackPanel.Push(nextPanel);
        GameObject panelGO = uiManager.GetSingleUI(nextPanel.UIType);
    }
    /// <summary>
    /// 执行面板的出栈操作，此操作会执行面板的OnExit方法
    /// </summary>
    public void Pop()
    {
        if (stackPanel.Count > 0)
        {
            stackPanel.Peek().OnExit();
            stackPanel.Pop();
        }
        if (stackPanel.Count > 0)
        {
            stackPanel.Peek().OnResume();
        }
    }
}
