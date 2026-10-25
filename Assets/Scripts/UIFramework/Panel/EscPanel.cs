using System;
using UnityEngine.UI;

public sealed class EscPanel : BasePanel
{
    private readonly PanelManager manager;
    private readonly Action resume;
    private PausePanelView view;
    private Button focus;

    public EscPanel(PanelManager manager, Action resume) : base("Prefabs/UI/Panel/EscPanel")
    {
        this.manager = manager;
        this.resume = resume;
    }

    protected override void OnInitialize()
    {
        view = PanelObject.AddComponent<PausePanelView>();
        view.Initialize(resume);
        focus = view.Bind("Continue", resume);
        view.Bind("Options", () =>
        {
            focus = view.FindButton("Options");
            manager.Push(new OptionsPanel(manager));
        });
        view.Bind("QuitToMenu", () =>
        {
            focus = view.FindButton("QuitToMenu");
            manager.Push(new ConfirmPanel(manager));
        });
    }

    public override void OnEnter(Action completed = null) =>
        base.OnEnter(() => { view.Focus(focus); completed?.Invoke(); });

    public override void OnResume(Action completed = null) => OnEnter(completed);
}
