using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 独立图层播放原始闪光帧，避免 Button 状态机覆盖图片或透明度。
[RequireComponent(typeof(Button))]
public sealed class MenuClickEffect : MonoBehaviour
{
    private static Sprite[] frames;
    private Image flash;
    private CanvasGroup panel;
    private bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => frames = null;

    public static MenuClickEffect Ensure(Button button)
    {
        var effect = button.GetComponent<MenuClickEffect>();
        return effect != null ? effect : button.gameObject.AddComponent<MenuClickEffect>();
    }

    private void Awake()
    {
        if (frames == null)
        {
            frames = new Sprite[6];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Resources.Load<Sprite>($"Sprites/Particles & Effects/Button_flash{i + 2:0000}");
        }
        GetComponent<Button>().transition = Selectable.Transition.None;
        var animator = GetComponent<Animator>();
        if (animator != null && animator.GetLayerIndex("press") >= 0)
        {
            animator.enabled = false;
            var image = GetComponent<Image>();
            if (image != null) { var color = image.color; color.a = 0; image.color = color; }
        }
        var fade = GetComponentInParent<PanelFade>();
        panel = fade != null ? fade.GetComponent<CanvasGroup>() : null;
        var child = transform.Find("Click Flash") as RectTransform;
        if (child == null)
        {
            child = new GameObject("Click Flash", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            child.SetParent(transform, false);
        }
        var background = transform.Find("BackGround");
        if (background != null) child.SetSiblingIndex(background.GetSiblingIndex() + 1);
        else child.SetAsFirstSibling();
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = child.offsetMax = Vector2.zero;
        flash = child.GetComponent<Image>();
        flash.raycastTarget = false;
        flash.color = Color.white;
        flash.enabled = false;
    }

    public void PlayThen(Action action)
    {
        if (playing || !isActiveAndEnabled || !GetComponent<Button>().IsInteractable() ||
            (panel != null && !panel.interactable)) return;
        playing = true;
        if (panel != null) panel.interactable = false;
        StartCoroutine(Play(action));
    }

    private IEnumerator Play(Action action)
    {
        float elapsed = 0;
        flash.enabled = true;
        while (elapsed < 0.2f)
        {
            flash.sprite = frames[Mathf.Min((int)(elapsed * 30f), frames.Length - 1)];
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        flash.enabled = false;
        playing = false;
        if (panel != null) panel.interactable = true;
        action?.Invoke();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (playing && panel != null) panel.interactable = true;
        playing = false;
        if (flash != null) flash.enabled = false;
    }
}
