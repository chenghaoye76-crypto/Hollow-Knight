using System.Collections.Generic;
using UnityEngine;
public class UIManager
{
    private readonly Transform panelRoot;
    private readonly Dictionary<string, BasePanel> panels = new();
    public UIManager(Transform panelRoot)
    {
        this.panelRoot = panelRoot;
    }
    public BasePanel GetOrCreate(BasePanel requestedPanel)
    {
        string path = requestedPanel.Path;
        if (panels.TryGetValue(path, out BasePanel cached))
        {
            return cached;
        }
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"找不到 UI 预制体：Resources/{path}");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, panelRoot, false);
        requestedPanel.Initialize(instance);
        // 等上一页退出后再显示，避免实例化的新页面提前露出来。
        instance.SetActive(false);
        panels.Add(path, requestedPanel);

        return requestedPanel;
    }

    public void DestroyPanel(BasePanel panel)
    {
        panels.Remove(panel.Path);

        if (panel.PanelObject != null)
        {
            Object.Destroy(panel.PanelObject);
        }
    }

}
