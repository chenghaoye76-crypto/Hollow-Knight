using UnityEngine;
/// <summary>
/// 开始主面板
/// </summary>
public class StartPanel : BasePanel
{
    static readonly string path = "Prefabs/UI/Panel/MainMenuPanel";
    public StartPanel() : base(new UIType(path)) { }

}   
