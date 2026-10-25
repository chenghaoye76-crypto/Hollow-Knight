using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 保留已有预制体布局和光标，统一处理暂停期间的动画、导航与取消。
public sealed class PausePanelView : MonoBehaviour
{
    private CanvasGroup group;
    private Action cancel;
    private readonly List<Button> buttons = new();

    public void Initialize(Action onCancel)
    {
        group = GetComponent<CanvasGroup>();
        cancel = onCancel;
        foreach (var animator in GetComponentsInChildren<Animator>(true))
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    public Button FindButton(string name)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.name == name) return button;
        throw new InvalidOperationException($"{gameObject.name} 缺少按钮 {name}");
    }

    public Button Bind(string name, Action action)
    {
        var button = FindButton(name);
        var hover = button.GetComponent<ButtonHover>();
        if (hover == null) hover = button.gameObject.AddComponent<ButtonHover>();
        hover.SetCursors(button.GetComponentsInChildren<Cursor>(true));
        buttons.Add(button);
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + buttons.Count - 1) % buttons.Count],
                selectOnDown = buttons[(i + 1) % buttons.Count]
            };
        }
        var effect = MenuClickEffect.Ensure(button);
        button.onClick.AddListener(() => effect.PlayThen(action));
        return button;
    }

    public void Focus(Button button)
    {
        if (EventSystem.current == null || button == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void Update()
    {
        if (group == null || !group.interactable) return;
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)))
            cancel?.Invoke();
    }
}
