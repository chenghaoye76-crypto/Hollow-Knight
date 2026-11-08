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
    private int CurrentValue;
    private Coroutine coroutine;
    [SerializeField] private Animator drainWaveAnimator;

    private static readonly int DrainWaveState = Animator.StringToHash("Base Layer.DrainWave");
    private static readonly int FillProperty = Shader.PropertyToID("_Fill");
    private Material liquidMaterial;
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
            liquidMaterial.SetFloat(FillProperty, 0f);
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

    internal void UpdateSoul(int value)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        coroutine = StartCoroutine(_update(value));
    }
    IEnumerator _update(int value)
    {
        int num = value > CurrentValue ? 1 : -1;

        if (num > 0 && !FirstUpdate)
        {
            while (CurrentValue != value)
            {
                yield return new WaitForSeconds(Speed);
                CurrentValue += num;
                UpdateLiquidLevel();

                if (CurrentValue % 33 == 0 && num > 0)
                    Frame.SoulBurst();
                UpdateLegacySprite();
            }
        }
        else
        {
            while (CurrentValue != value)
            {
                yield return new WaitForSeconds(DecreasedSpeed);
                CurrentValue += num;
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

    private void UpdateLiquidLevel()
    {
        float ratio = CurrentValue / 99f;
        if (liquidMaterial != null)
            liquidMaterial.SetFloat(FillProperty, Mathf.Clamp01(ratio));
        else
            fillRect.anchoredPosition = ToAnchorPosition(Vector3.Lerp(emptyPos, fullPos, ratio));
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
