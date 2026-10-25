using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelFade : MonoBehaviour
{
    private CanvasGroup group;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    public void Show(float duration, Action completed = null)
    {
        gameObject.SetActive(true);
        StopFade();

        group.alpha = 0f;
        group.interactable = false;

        // 淡入时拦住点击，避免点到下层页面。
        group.blocksRaycasts = true;

        fadeCoroutine = StartCoroutine(FadeTo(1f, duration, completed));
    }

    public void Hide()
    {
        StopFade();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public void PlaySequence(IEnumerator sequence, bool entering, Action completed)
    {
        gameObject.SetActive(true);
        StopFade();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = true;
        fadeCoroutine = StartCoroutine(CompleteSequence(sequence, entering, completed));
    }

    public void FadeOut(float duration, Action completed = null)
    {
        StopFade();
        group.interactable = false;
        group.blocksRaycasts = true;

        fadeCoroutine = StartCoroutine(FadeTo(0f, duration, completed));
    }

    private IEnumerator FadeTo(float target, float duration, Action completed)
    {
        float initialAlpha = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            group.alpha = Mathf.Lerp(initialAlpha, target, eased);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        group.alpha = target;
        group.interactable = target > 0f;
        fadeCoroutine = null;
        completed?.Invoke();
    }

    private IEnumerator CompleteSequence(IEnumerator sequence, bool entering, Action completed)
    {
        while (sequence.MoveNext())
            yield return sequence.Current;

        group.interactable = entering;
        fadeCoroutine = null;
        completed?.Invoke();
    }

    private void StopFade()
    {
        if (fadeCoroutine == null)
            return;

        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }
}
