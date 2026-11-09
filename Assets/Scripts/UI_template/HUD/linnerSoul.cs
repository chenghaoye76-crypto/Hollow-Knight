using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class linnerSoul : MonoBehaviour
{
    public int PosValue;
    public int CurrentValue;

    private bool isTranslation;
    internal bool IsVisuallyFull => !isTranslation && CurrentValue == 4;
    private bool samplingRefill;
    private readonly Dictionary<string, AnimationClip> refillClips = new Dictionary<string, AnimationClip>();
    private static readonly string[] IdleStates = { "empty", "oneQuarter", "half", "ThreeQuarter", "full" };
    private static readonly string[] DrainStates = { "downToEmpty", "downToOneQuarter", "downToHalf", "downToThreeQuarter" };

    Animator animator;
    private Image orbImage;
    private RectTransform fullTransitionRect;
    private Vector3 restingScale;
    private Vector2 restingPivot;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        orbImage = GetComponent<Image>();
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            refillClips[clip.name] = clip;

        PosValue = 0;
        CurrentValue = 0;
        isTranslation = false;
    }

    public void SetValue(int value)
    {
        if (samplingRefill)
        {
            if (value == PosValue)
                return;

            ResumeAnimator();
        }

        PosValue = value;

        if (!isTranslation)
        {
            UpdateAnim();
        }
    }

    internal void SetRefillAmount(int amount, int quarters)
    {
        int lowerLevel = Mathf.Min(amount / 8, 3);
        int lowerAmount = lowerLevel * 8;
        int upperAmount = lowerLevel == 3 ? 33 : lowerAmount + 8;
        bool atBoundary = amount == lowerAmount || amount == 33;
        string clipName = atBoundary ? IdleStates[quarters] : DrainStates[lowerLevel];

        if (!refillClips.TryGetValue(clipName, out AnimationClip clip))
        {
            SetValue(quarters);
            return;
        }

        samplingRefill = true;
        animator.enabled = false;
        PosValue = CurrentValue = quarters;
        isTranslation = false;

        float progress = atBoundary ? 0f : (upperAmount - amount) / (float)(upperAmount - lowerAmount);
        clip.SampleAnimation(gameObject, clip.length * progress);
    }

    private void ResumeAnimator()
    {
        samplingRefill = false;
        animator.enabled = true;
        animator.ResetTrigger("add");
        animator.ResetTrigger("decrease");
        animator.Play("Base Layer." + IdleStates[CurrentValue], 0, 0f);
    }

    public void UpdateAnim()
    {
        if (CurrentValue < PosValue)
            Add();
        else if (CurrentValue > PosValue)
            Decrease();
    }

    public void TranslationOver()
    {
        isTranslation = false;
        UpdateAnim();
    }

    public void Add()
    {
        isTranslation = true;
        CurrentValue++;
        animator.SetTrigger("add");
    }

    public void Decrease()
    {
        isTranslation = true;
        CurrentValue--;
        animator.SetTrigger("decrease");
    }

    public void Appear()
    {
        if (samplingRefill)
            ResumeAnimator();

        animator.SetTrigger("appear");
    }

    public void Bigger()
    {
        if (fullTransitionRect != null)
            return;

        fullTransitionRect = (RectTransform)transform;
        restingScale = fullTransitionRect.localScale;
        restingPivot = fullTransitionRect.pivot;

        // Both sets have a 37 px orb, but the full transition has a taller canvas.
        const float scale = 113f / 55f;
        fullTransitionRect.localScale = Vector3.Scale(restingScale, new Vector3(scale, scale, 1f));
        fullTransitionRect.pivot = restingPivot + new Vector2(0f, 9f / 113f);
    }

    private void LateUpdate()
    {
        Sprite sprite = orbImage.overrideSprite;
        if (sprite != null && sprite.rect.size == new Vector2(69f, 113f))
            Bigger();
        else
            Littler();
    }

    public void Littler()
    {
        if (fullTransitionRect == null)
            return;

        fullTransitionRect.localScale = restingScale;
        fullTransitionRect.pivot = restingPivot;
        fullTransitionRect = null;
    }

    private void OnDisable()
    {
        Littler();
    }
}
