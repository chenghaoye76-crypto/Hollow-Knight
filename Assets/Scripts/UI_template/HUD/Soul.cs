using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Soul : MonoBehaviour
{
    [Header("position")]
    public Vector3 FullPos;
    public Vector3 EmptyPos;
    public Vector3 emptyPos;
    public Vector3 fullPos;

    [Header("sprite")]
    public Sprite full;
    public Sprite normal;
    public Image childImage;

    [Header("spped")]
    public float Speed;
    public float DecreasedSpeed;

    private bool FirstUpdate;
    private frame Frame;
    internal const int RefillPointsPerStep = 2;
    private const float RefillStepDelayScale = 0.5f;
    private int CurrentValue;
    internal int DisplayedSoul => CurrentValue;
    private Coroutine coroutine;
    [SerializeField] private Animator drainWaveAnimator;
    [SerializeField] private Animator soulFullAnimator;

    private static readonly int DrainWaveState = Animator.StringToHash("Base Layer.DrainWave");
    private static readonly int SoulFullState = Animator.StringToHash("Base Layer.full");
    private static readonly int FillProperty = Shader.PropertyToID("_Fill");
    private Material liquidMaterial;
    private float liquidFill;
    private RectTransform fillRect;
    private Vector2 fullAnchorPosition;

    private void Awake()
    {
        fillRect = drainWaveAnimator != null
            ? (RectTransform)drainWaveAnimator.transform
            : childImage.rectTransform;
        fullAnchorPosition = fillRect.anchoredPosition;

        Material sourceMaterial = childImage.material;
        if (sourceMaterial.HasProperty(FillProperty))
        {
            liquidMaterial = new Material(sourceMaterial);
            childImage.material = liquidMaterial;
        }
    }

    private void OnDestroy()
    {
        if (liquidMaterial != null)
            Destroy(liquidMaterial);
    }

    internal void Init(frame frame)
    {
        if (liquidMaterial != null)
            SetLiquidFill(0f);
        else
            fillRect.anchoredPosition = ToAnchorPosition(EmptyPos);
        Frame = frame;
        StartCoroutine(showImage());
    }
    IEnumerator showImage()
    {
        yield return new WaitForFixedUpdate();
        FirstUpdate = true;
        childImage.GetComponent<CanvasGroup>().alpha = 1;
    }

    internal void PlayDrainWave()
    {
        if (drainWaveAnimator != null)
            drainWaveAnimator.Play(DrainWaveState, 0, 0f);
    }

    internal void UpdateSoul(int value, bool isRefill = false)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        coroutine = StartCoroutine(_update(value, isRefill));
    }
    IEnumerator _update(int value, bool isRefill)
    {
        if (liquidMaterial != null && value / 99f < liquidFill)
        {
            yield return AnimateLiquidDrain(value);
            FirstUpdate = false;
            yield break;
        }

        int num = value > CurrentValue ? 1 : -1;

        if (num > 0 && !FirstUpdate)
        {
            while (CurrentValue != value)
            {
                yield return new WaitForSeconds(isRefill ? Speed * RefillStepDelayScale : Speed);
                int previousValue = CurrentValue;
                CurrentValue += isRefill ? Mathf.Min(RefillPointsPerStep, value - CurrentValue) : num;
                UpdateLiquidLevel();

                if (CurrentValue / 33 > previousValue / 33)
                    Frame.SoulBurst();
                UpdateLegacySprite();
            }
        }
        else
        {
            while (CurrentValue != value)
            {
                yield return new WaitForSeconds(isRefill && num > 0 ? Speed * RefillStepDelayScale : DecreasedSpeed);
                CurrentValue += isRefill && num > 0
                    ? Mathf.Min(RefillPointsPerStep, value - CurrentValue)
                    : num;
                UpdateLiquidLevel();
                UpdateLegacySprite();
            }
        }

        FirstUpdate = false;

        yield return new WaitForFixedUpdate();


        if (liquidMaterial != null)
            yield break;

        if (value == 99)
            fillRect.anchoredPosition = fullAnchorPosition;
        else if (value == 0)
            fillRect.anchoredPosition = ToAnchorPosition(EmptyPos);
    }

    private IEnumerator AnimateLiquidDrain(int value)
    {
        float startFill = liquidFill;
        float targetFill = value / 99f;
        float drainedFill = startFill - targetFill;
        float duration = Mathf.Clamp(drainedFill * 99f * DecreasedSpeed, 0.18f, 0.42f);
        float rebound = Mathf.Min(0.022f, drainedFill * 0.12f);

        for (float elapsed = 0f; elapsed < duration;)
        {
            yield return null;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fall = Mathf.SmoothStep(startFill, targetFill, progress);
            float sway = Mathf.Sin(progress * 2f * Mathf.PI) * Mathf.Sin(progress * Mathf.PI);
            SetLiquidFill(Mathf.Min(startFill, fall + rebound * sway));
            CurrentValue = Mathf.RoundToInt(liquidFill * 99f);
        }

        CurrentValue = value;
        SetLiquidFill(targetFill);
    }

    private void SetLiquidFill(float fill)
    {
        liquidFill = Mathf.Clamp01(fill);
        liquidMaterial.SetFloat(FillProperty, liquidFill);
    }

    private void UpdateLiquidLevel()
    {
        float ratio = CurrentValue / 99f;
        if (liquidMaterial != null)
            SetLiquidFill(ratio);
        else
            fillRect.anchoredPosition = CurrentValue == 99
                ? fullAnchorPosition
                : ToAnchorPosition(Vector3.Lerp(emptyPos, fullPos, ratio));

        if (CurrentValue == 99 && soulFullAnimator != null)
            soulFullAnimator.Play(SoulFullState, 0, 0f);
    }

    private void UpdateLegacySprite()
    {
        if (liquidMaterial == null)
            childImage.sprite = CurrentValue == 99 ? full : normal;
    }

    private Vector2 ToAnchorPosition(Vector3 position)
    {
        return fullAnchorPosition + (Vector2)(position - FullPos);
    }
}
