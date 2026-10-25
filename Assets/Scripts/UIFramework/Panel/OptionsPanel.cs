using System;

public class OptionsPanel : BasePanel
{
    private readonly PanelManager manager;
    private OptionsMenuView view;


    public OptionsPanel(PanelManager manager) : base("Prefabs/UI/Panel/OptionsPanel")
    {
        this.manager = manager;
    }

    protected override void OnInitialize()
    {
        view = PanelObject.AddComponent<OptionsMenuView>();
        view.Initialize(manager.Pop);
    }

    public override void OnEnter(Action completed = null) =>
        Fade.PlaySequence(view.Animate(true), true, () => { view.FocusFirst(); completed?.Invoke(); });

    public override void OnResume(Action completed = null) => OnEnter(completed);

    public override void PlayExit(Action completed)
    {
        view.PrepareExit();
        Fade.PlaySequence(view.Animate(false), false, completed);
    }
}
