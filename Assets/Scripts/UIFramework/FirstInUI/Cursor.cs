using UnityEngine;
using UnityEngine.UI;

public class Cursor : MonoBehaviour
{
    private Animator animator;
    private AnimationClip reveal;
    private Image image;
    private float progress;
    private bool highlighted;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "cursorShow") { reveal = clip; break; }
            // 正放/倒放同一段序列，快速切换时沿当前进度反向，不从头跳帧。
            animator.enabled = false;
        }

        // 装饰只负责显示，鼠标命中区域由父按钮负责。
        image = GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }

    private void OnEnable()
    {
        // 返回页面时立即恢复普通状态，避免沿用隐藏前的动画画面。
        progress = 0;
        highlighted = false;
        Sample();
    }

    public void SetBtnState(bool isnormal)
    {
        highlighted = !isnormal;
    }

    private void Update()
    {
        float target = highlighted ? 1f : 0f;
        if (Mathf.Approximately(progress, target)) return;
        progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / (reveal != null ? Mathf.Max(reveal.length, 0.01f) : 0.18f));
        Sample();
    }

    private void Sample()
    {
        if (image == null) return;
        if (reveal != null) reveal.SampleAnimation(gameObject, progress * reveal.length);
        var color = image.color;
        color.a = progress <= 0 ? 0 : 1;
        image.color = color;
    }

}
