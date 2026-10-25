using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RofilePanel : BasePanel
{
    private readonly PanelManager manager;
    private CanvasGroup title;
    private CanvasGroup fleur;
    private CanvasGroup backGroup;
    private Image fleurImage;
    private Sprite[] fleurFrames;
    private Row[] rows;
    private bool isOpening;

    // 参考视频：标题/花纹先出现，每隔 0.2 秒展开下一行，Back 最后显示。
    private const float RowStart = 0.25f;
    private const float RowInterval = 0.2f;
    private const float RowRevealDuration = 0.5f;
    private const float BackDelay = 0.2f;

    private sealed class Row
    {
        public CanvasGroup[] Content;
        public CanvasGroup ClearSave;
        public Image Background;
        public Button SelectButton;
        public Button ClearButton;
        public TMP_Text Label;
        public bool HasSave;
    }

    public RofilePanel(PanelManager manager) : base("Prefabs/UI/Panel/RofilePanel")
    {
        this.manager = manager;
    }

    protected override void OnInitialize()
    {
        var back = PanelObject.transform.Find("Back").GetComponent<Button>();

        back.onClick.AddListener(() => MenuClickEffect.Ensure(back).PlayThen(manager.Pop));

        title = GetGroup(PanelObject.transform.Find("Text"));
        fleur = GetGroup(PanelObject.transform.Find("Fleur"));
        backGroup = GetGroup(back.transform);
        fleurImage = fleur.GetComponent<Image>();
        fleurImage.raycastTarget = false;

        // 手动采样花纹序列：进入正放、退出倒放，避免 Animator 自动重播。
        var fleurAnimator = fleur.GetComponent<Animator>();
        if (fleurAnimator != null)
            fleurAnimator.enabled = false;

        fleurFrames = new Sprite[9];
        for (int i = 0; i < fleurFrames.Length; i++)
            fleurFrames[i] = Resources.Load<Sprite>(
                $"Sprites/Inventory & UI/Fleurs/Warning_Fleur{i:0000}");

        Transform slots = PanelObject.transform.Find("Button1");
        Transform clearSaves = PanelObject.transform.Find("Button2");
        rows = new Row[slots.childCount];
        for (int i = 0; i < rows.Length; i++)
        {
            Transform slot = slots.GetChild(i);
            var background = slot.Find("BackGround").GetComponent<Image>();
            background.type = Image.Type.Filled;
            background.fillMethod = Image.FillMethod.Horizontal;
            background.fillOrigin = (int)Image.OriginHorizontal.Left;

            // 背景只做横向展开，文字与悬停装饰单独淡入淡出。
            GetGroup(slot).alpha = 1f;
            var content = new List<CanvasGroup>();
            foreach (Transform child in slot)
            {
                if (child != background.transform)
                    content.Add(GetGroup(child));
            }

            // 锁定页面输入时，Button 默认会把背景染成半透明。
            var backgroundButton = background.GetComponent<Button>();
            if (backgroundButton != null)
                backgroundButton.enabled = false;
            background.CrossFadeColor(Color.white, 0f, true, true);

            var hover = slot.GetComponent<ButtonHover>();
            if (hover != null)
                hover.SetBackground(background);

            // 把点击事件放在整行上，点击文字或背景都能选择存档。
            var select = slot.GetComponent<Button>();
            if (select == null)
                select = slot.gameObject.AddComponent<Button>();
            select.targetGraphic = background;
            select.transition = Selectable.Transition.None;
            int slotIndex = i;
            select.onClick.AddListener(() => MenuClickEffect.Ensure(select).PlayThen(() => OpenSlot(slotIndex)));

            Transform clear = clearSaves.GetChild(i);
            var clearButton = clear.GetComponent<Button>();
            if (clearButton == null)
                clearButton = clear.gameObject.AddComponent<Button>();
            clearButton.targetGraphic = clear.GetComponent<Image>();
            clearButton.transition = Selectable.Transition.None;
            clearButton.onClick.AddListener(() => MenuClickEffect.Ensure(clearButton).PlayThen(() => ClearSlot(slotIndex)));

            var label = slot.Find("New Game").GetComponent<TMP_Text>();
            // 为场景名和游玩时间留出空间，保持文字左边的位置。
            RectTransform labelRect = label.rectTransform;
            Vector2 labelPosition = labelRect.anchoredPosition;
            labelPosition.x -= labelRect.sizeDelta.x * labelRect.pivot.x;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(450f, 50f);
            labelRect.anchoredPosition = labelPosition;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 20f;

            rows[i] = new Row
            {
                Content = content.ToArray(),
                ClearSave = i < clearSaves.childCount ? GetGroup(clearSaves.GetChild(i)) : null,
                Background = background,
                SelectButton = select,
                ClearButton = clearButton,
                Label = label
            };
        }

        RefreshSlots();
        ApplySequence(0f, true);
    }

    public override void OnEnter(Action completed = null)
    {
        isOpening = false;
        RefreshSlots();
        Fade.PlaySequence(Animate(true), true, completed);
    }

    public override void OnResume(Action completed = null)
    {
        OnEnter(completed);
    }

    public override void PlayExit(Action completed)
    {
        foreach (var hover in PanelObject.GetComponentsInChildren<ButtonHover>())
            hover.ResetHover();
        Fade.PlaySequence(Animate(false), false, completed);
    }

    private IEnumerator Animate(bool entering)
    {
        float duration = GetBackStart(entering) + 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ApplySequence(elapsed, entering);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        ApplySequence(duration, entering);
    }

    private void ApplySequence(float elapsed, bool entering)
    {
        title.alpha = Visibility(elapsed, 0f, 0.2f, entering);
        float frameProgress = Progress(elapsed, entering ? 0.04f : 0f, 0.3f);
        if (!entering)
            frameProgress = 1f - frameProgress;
        fleur.alpha = frameProgress > 0f ? 1f : 0f;
        // 精灵序列已经带有展开节奏，按线性时间采样原素材。
        int frame = Mathf.Min((int)(frameProgress * fleurFrames.Length), fleurFrames.Length - 1);
        if (fleurFrames[frame] != null)
            fleurImage.sprite = fleurFrames[frame];

        for (int i = 0; i < rows.Length; i++)
        {
            float start = (entering ? RowStart : 0.2f) + i * RowInterval;
            float alpha = Visibility(elapsed, start + (entering ? 0.04f : 0f), 0.2f, entering);
            foreach (var content in rows[i].Content)
                content.alpha = alpha;
            rows[i].Background.fillAmount = Visibility(elapsed, start,
                entering ? RowRevealDuration : 0.25f, entering);
            if (rows[i].ClearSave != null)
                rows[i].ClearSave.alpha = rows[i].HasSave ? alpha : 0f;
        }

        backGroup.alpha = Visibility(elapsed, GetBackStart(entering), 0.2f, entering);
    }

    private float GetBackStart(bool entering)
    {
        return entering
            ? RowStart + (rows.Length - 1) * RowInterval + RowRevealDuration + BackDelay
            : 0.2f + rows.Length * RowInterval;
    }

    private static float Visibility(float elapsed, float delay, float duration, bool entering)
    {
        float value = Mathf.SmoothStep(0f, 1f, Progress(elapsed, delay, duration));
        return entering ? value : 1f - value;
    }

    private static float Progress(float elapsed, float delay, float duration)
    {
        return Mathf.Clamp01((elapsed - delay) / duration);
    }

    private static CanvasGroup GetGroup(Transform target)
    {
        var group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            SaveSlotStatus status = SaveGameManager.Instance.ReadSlot(i, out var data);
            rows[i].HasSave = status != SaveSlotStatus.Empty;
            rows[i].SelectButton.interactable = status != SaveSlotStatus.Corrupt;
            rows[i].ClearButton.interactable = rows[i].HasSave;
            rows[i].ClearSave.interactable = rows[i].HasSave;
            rows[i].ClearSave.blocksRaycasts = rows[i].HasSave;
            rows[i].ClearSave.alpha = rows[i].HasSave ? 1f : 0f;
            if (status == SaveSlotStatus.Empty)
                rows[i].Label.text = "New Game";
            else if (status == SaveSlotStatus.Corrupt)
                rows[i].Label.text = "Damaged Save — Clear Save to restart";
            else
            {
                var playTime = TimeSpan.FromSeconds(Math.Min(data.playSeconds, 3153600000d));
                rows[i].Label.text = $"Continue  ·  {System.IO.Path.GetFileNameWithoutExtension(data.scenePath)}  " +
                    $"{(int)playTime.TotalHours:00}:{playTime.Minutes:00}";
            }
        }
    }

    private void OpenSlot(int slot)
    {
        if (isOpening || !Group.interactable)
            return;
        isOpening = true;
        PlayExit(() =>
        {
            if (!SaveGameManager.Instance.OpenSlot(slot))
                OnEnter();
        });
    }

    private void ClearSlot(int slot)
    {
        if (isOpening || !Group.interactable)
            return;
        if (SaveGameManager.Instance.DeleteSlot(slot))
        {
            foreach (var hover in rows[slot].ClearSave.GetComponentsInChildren<ButtonHover>())
                hover.ResetHover();
            RefreshSlots();
        }
    }
}
