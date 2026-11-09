using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEditor.Rendering.MaterialUpgrader;

public class GamePanel : BasePanel
{
    public static GamePanel instance;

    [SerializeField] playerUI PlayerUI;
    //[SerializeField] SaveIcon saveIcon;
    //[SerializeField] AreaTitle areaTitle;
    [SerializeField] Image blackMask;

    private float alpha;

    Vector3 playerUITempScale;
    //[HideInInspector] public DiologuePanel diologuePanel;
    //PlayerInputs playerInputs;

    //[HideInInspector] public dialogueText conversationData;
    public bool isEntered = false;
    [HideInInspector] public bool isPaused;
    private bool canPause;
    private void OnEnable()
    {
        EventManager.instance.AddVoidAction("UIShow", name, UIShow);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveVoidAction("canPause", name);
        EventManager.instance.RemoveVoidAction("UIShow", name);
        EventManager.instance.RemoveVoidAction("StartControl", name);
    }
    private void UIShow()
    {
        PlayerUI.Show();
    }

    internal void SaveIconShow()
    {
        throw new NotImplementedException();
    }
}
