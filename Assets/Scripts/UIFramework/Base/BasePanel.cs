using System;
using UnityEngine;
public class BasePanel
{

    public string Path { get; }
    public GameObject PanelObject { get; private set; }
    protected CanvasGroup Group { get; private set; }
    protected PanelFade Fade { get; private set; }
    public BasePanel(string path)
    {
        Path = path;
    }


    public void Initialize(GameObject panelObject)
    {
        PanelObject = panelObject;

        Group = panelObject.GetComponent<CanvasGroup>();
        if (Group == null)
        {
            Group = panelObject.AddComponent<CanvasGroup>();
        }

        Fade = panelObject.GetComponent<PanelFade>();
        if (Fade == null)
            Fade = panelObject.AddComponent<PanelFade>();
        OnInitialize();
    }

    // 每个面板实例只初始化一次，用于绑定按钮事件。
    protected virtual void OnInitialize() { }

    public virtual void OnEnter(Action completed = null) => Fade.Show(0.2f, completed);
    public virtual void OnPause() => Fade.Hide();
    public virtual void OnResume(Action completed = null) => Fade.Show(0.2f, completed);
    public virtual void OnExit() => Fade.Hide();

    public virtual void PlayExit(Action completed) => Fade.FadeOut(0.2f, completed);
}
