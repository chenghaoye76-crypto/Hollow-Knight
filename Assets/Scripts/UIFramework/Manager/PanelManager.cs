using System.Collections.Generic;
public class PanelManager
{
    private readonly Stack<BasePanel> stack = new();
    private readonly UIManager uiManager;
    private bool isTransitioning;
    public BasePanel Current => stack.Count > 0 ? stack.Peek() : null;
    public bool IsTransitioning => isTransitioning;

    public PanelManager(UIManager uIManager)
    {
        this.uiManager = uIManager;
    }
    public void Push(BasePanel requestedPanel)
    {
        if (isTransitioning)
            return;

        //防止同一个页面连续入栈
        if (stack.Count > 0 && stack.Peek().Path == requestedPanel.Path)
        {
            return;
        }

        BasePanel panel = uiManager.GetOrCreate(requestedPanel);
        if (panel == null)
            return;

        //当前版本不允许同一个页面在栈里出现多次
        if (stack.Contains(panel))
        {
            return;
        }

        isTransitioning = true;
        if (stack.Count == 0)
            EnterPanel(panel);
        else
        {
            BasePanel previous = stack.Peek();
            previous.PlayExit(() =>
            {
                previous.OnPause();
                EnterPanel(panel);
            });
        }
    }

    public void Pop()
    {
        //保留根页面
        if (isTransitioning || stack.Count <= 1)
        {
            return;
        }

        isTransitioning = true;
        BasePanel current = stack.Peek();
        current.PlayExit(() =>
        {
            stack.Pop();
            current.OnExit();
            uiManager.DestroyPanel(current);
            stack.Peek().OnResume(() => isTransitioning = false);
        });
    }

    private void EnterPanel(BasePanel panel)
    {
        stack.Push(panel);
        panel.PanelObject.transform.SetAsLastSibling();
        panel.OnEnter(() => isTransitioning = false);
    }

    // 游戏暂停菜单允许关闭根页；主菜单仍使用保留根页的 Pop。
    public void CloseRoot(System.Action completed)
    {
        if (isTransitioning || stack.Count != 1) return;
        isTransitioning = true;
        BasePanel current = stack.Peek();
        current.PlayExit(() =>
        {
            stack.Pop();
            current.OnExit();
            uiManager.DestroyPanel(current);
            isTransitioning = false;
            completed?.Invoke();
        });
    }
}
