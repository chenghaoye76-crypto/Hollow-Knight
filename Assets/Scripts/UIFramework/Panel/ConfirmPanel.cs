using System;
using TMPro;
using UnityEngine.UI;

public sealed class ConfirmPanel : BasePanel
{
    private readonly PanelManager manager;
    private PausePanelView view;
    private Button no;
    private TMP_Text status;

    public ConfirmPanel(PanelManager manager) : base("Prefabs/UI/Panel/ConfirmPanel") => this.manager = manager;

    protected override void OnInitialize()
    {
        view = PanelObject.AddComponent<PausePanelView>();
        view.Initialize(manager.Pop);
        no = view.Bind("No", manager.Pop);
        view.Bind("Yes", Confirm);
        // 预制体标题带有 Button 组件，只作标题显示。
        view.FindButton("QuitToMenu").interactable = false;
        status = PanelObject.transform.Find("ProgressWillBeSaved").GetComponent<TMP_Text>();
    }

    public override void OnEnter(Action completed = null) =>
        base.OnEnter(() => { view.Focus(no); completed?.Invoke(); });

    private void Confirm()
    {
        Group.interactable = false;
        if (SaveGameManager.Instance.TryReturnToMenu()) return;
        status.text = "Unable to save or return to menu. Please try again.";
        Group.interactable = true;
        view.Focus(no);
    }
}
