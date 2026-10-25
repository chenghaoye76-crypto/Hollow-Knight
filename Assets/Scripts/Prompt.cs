using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class Prompt : MonoBehaviour
{
    [SerializeField, Tooltip("提示中显示的 TextMeshPro 文字，留空时自动查找子物体。")]
    private TMP_Text textComponent;
    private Animator animator;

    [SerializeField, TextArea, Tooltip("提示文字，可在编辑器中直接修改。")]
    private string text = "text";
    private bool wantsVisible;
    public bool IsVisible => wantsVisible;

    // The entrance detector is a separate object and remains active while its prompt is hidden.
    public void SetVisible(bool visible)
    {
        if (wantsVisible == visible) return;
        wantsVisible = visible;
        if (animator == null) animator = GetComponent<Animator>();
        if (visible) gameObject.SetActive(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            gameObject.SetActive(visible);
            return;
        }
        // Restart the requested state even when the previous animation was interrupted.
        animator.Play(visible ? "Base Layer.Up" : "Base Layer.Down", 0, 0f);
        animator.Update(0f);
    }

    public void HideImmediate()
    {
        wantsVisible = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (wantsVisible || animator == null) return;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Base Layer.Down") && state.normalizedTime >= 1f)
            HideImmediate();
    }

    public string Text
    {
        get => text;
        set => SetText(value);
    }

    /// <summary>更新提示内容，也可绑定到 UnityEvent 的字符串参数。</summary>
    public void SetText(string value)
    {
        text = value ?? string.Empty;
        RefreshText();
    }

    private void Awake() => RefreshText();
    private void OnEnable() => RefreshText();
    private void OnValidate() => RefreshText();

    private void Reset()
    {
        textComponent = GetComponentInChildren<TMP_Text>(true);
        if (textComponent != null) text = textComponent.text;
    }

    private void RefreshText()
    {
        if (textComponent == null)
            textComponent = GetComponentInChildren<TMP_Text>(true);

        if (textComponent != null && textComponent.text != text)
            textComponent.text = text ?? string.Empty;
    }
}
