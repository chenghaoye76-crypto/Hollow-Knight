using System;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel
{
    private readonly PanelManager manager;

    public MainMenuPanel(PanelManager manager) : base("Prefabs/UI/Panel/MainMenuPanel")
    {
        this.manager = manager;
    }

    protected override void OnInitialize()
    {
        var start = PanelObject.transform.Find("StartGame").GetComponent<Button>();

        var options = PanelObject.transform.Find("Options").GetComponent<Button>();

        // 初始化时就接管闪光，避免未点击的按钮继续触发旧 Animator。
        MenuClickEffect.Ensure(start);
        MenuClickEffect.Ensure(options);

        start.onClick.AddListener(() => PlayClickThen(start, StartGame));
        options.onClick.AddListener(() => PlayClickThen(options, OpenOptions));
    }

    private void StartGame()
    {
        manager.Push(new RofilePanel(manager));
    }

    private void OpenOptions()
    {
        manager.Push(new OptionsPanel(manager));
    }

    private void PlayClickThen(Button button, Action next)
    {
        if (Group.interactable) MenuClickEffect.Ensure(button).PlayThen(next);
    }
}
