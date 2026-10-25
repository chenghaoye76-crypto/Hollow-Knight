using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// All positions are measured on a 1920 x 1080 canvas to match the supplied references.
public sealed class OptionsMenuView : MonoBehaviour
{
    private sealed class Row
    {
        public Button Button;
        public CanvasGroup Group;
        public TMP_Text Label, Value, Help;
        public string Caption, Description;
        public Func<string> Read;
        public Action<int> Change;
        public Slider Slider;
        public Func<float> ReadSlider;
    }
    private Action close;
    private CanvasGroup root, titleGroup, fleurGroup, backGroup, resetGroup;
    private RectTransform presentation, detail;
    private Transform menu;
    private TMP_Text title, message;
    private TMP_FontAsset menuFont, bodyFont, chineseFont;
    private Image fleur;
    private Sprite[] frames;
    private Sprite keyBorder;
    private GameObject rowTemplate;
    private Button back, reset;
    private string category, lastCategory;
    private bool switching, deviceChanged, initialized, navigationBeforeCapture;
    private int listening = -1;
    private float listenStart;
    private Gamepad rumblePad;
    private readonly List<Row> rows = new();
    private readonly List<Resolution> resolutions = new();
    private readonly Dictionary<Image, Material> backdropMaterials = new();
    private Material backdropBlur;
    private static readonly string[] PadButtons = { "buttonSouth", "buttonEast", "buttonWest", "buttonNorth", "leftShoulder", "rightShoulder", "leftTrigger", "rightTrigger" };

    public void Initialize(Action onClose)
    {
        close = onClose;
        root = GetComponent<CanvasGroup>();
        title = transform.Find("Options").GetComponent<TMP_Text>();
        menuFont = title.font;
        var style = Resources.Load<OptionsVisualAssets>("OptionsVisualAssets");
        bodyFont = style != null && style.bodyFont != null ? style.bodyFont : menuFont;
        chineseFont = style != null ? style.chineseFont : menuFont;
        keyBorder = Resources.Load<Sprite>("Sprites/Backend/button_skin_0004_square_key");
        presentation = Rect("Presentation", transform, Vector2.zero, new Vector2(1920f, 1080f));
        title.transform.SetParent(presentation, false);
        SetRect(title.rectTransform, new Vector2(0f, 380f), new Vector2(1200f, 85f));
        title.fontSize = 56f;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        titleGroup = Group(title.transform);
        fleur = transform.Find("Fleur").GetComponent<Image>();
        fleur.transform.SetParent(presentation, false);
        SetRect(fleur.rectTransform, new Vector2(0f, 310f), new Vector2(740f, 68f));
        fleur.preserveAspect = true;
        fleur.raycastTarget = false;
        fleurGroup = Group(fleur.transform);
        var animator = fleur.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        frames = new Sprite[9];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Resources.Load<Sprite>($"Sprites/Inventory & UI/Fleurs/Warning_Fleur{i:0000}");
        var veil = Rect("Background Veil", transform, Vector2.zero, Vector2.zero);
        veil.anchorMin = Vector2.zero;
        veil.anchorMax = Vector2.one;
        veil.offsetMin = veil.offsetMax = Vector2.zero;
        veil.SetAsFirstSibling();
        var shade = veil.gameObject.AddComponent<Image>();
        bool hasPauseBackdrop = transform.parent != null && transform.parent.Find("Pause Backdrop") != null;
        shade.color = new Color(0f, 0f, 0f, hasPauseBackdrop ? 0f : 0.5f);
        shade.raycastTarget = true;
        menu = transform.Find("Button");
        menu.SetParent(presentation, false);
        SetRect((RectTransform)menu, Vector2.zero, new Vector2(700f, 600f));
        var layout = menu.GetComponent<VerticalLayoutGroup>();
        if (layout != null) layout.enabled = false;
        rowTemplate = menu.Find("Audio").gameObject;
        back = transform.Find("Back").GetComponent<Button>();
        back.transform.SetParent(presentation, false);
        StyleCentered(back, "Back", -400f, 180f);
        backGroup = Group(back.transform);
        back.onClick.AddListener(() => MenuClickEffect.Ensure(back).PlayThen(Back));
        reset = Instantiate(rowTemplate, presentation, false).GetComponent<Button>();
        reset.name = "Reset Defaults";
        StyleCentered(reset, "Reset Defaults", -300f, 500f);
        reset.onClick.RemoveAllListeners();
        reset.onClick.AddListener(() => MenuClickEffect.Ensure(reset).PlayThen(ResetCurrent));
        resetGroup = Group(reset.transform);
        detail = Rect("Settings", presentation, Vector2.zero, new Vector2(1920f, 1080f));
        message = Text("Status", presentation, new Vector2(0f, -255f), new Vector2(1300f, 50f), 24f, TextAlignmentOptions.Center, true);
        initialized = true;
        InputSystem.onDeviceChange += OnDeviceChange;
        Resize();
        BuildMenu();
    }

    private static CanvasGroup Group(Transform target)
    {
        var group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, position, size);
        return rect;
    }
    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
    private TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, bool body = false)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font(body);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
    private TMP_FontAsset Font(bool body = false) => GameOptions.Chinese && chineseFont != null ? chineseFont : body ? bodyFont : menuFont;
    private static string T(string text) => OptionsText.Get(text);
    private static string OnOff(bool on) => T(on ? "On" : "Off");
    private static int Wrap(int value, int count) => (value % count + count) % count;

    private void StyleCentered(Button button, string caption, float y, float width)
    {
        SetRect((RectTransform)button.transform, new Vector2(0f, y), new Vector2(width, 64f));
        var label = button.GetComponent<TMP_Text>();
        if (label == null) label = button.GetComponentInChildren<TMP_Text>();
        label.text = T(caption);
        label.font = Font();
        label.fontSize = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = true;
        button.transition = Selectable.Transition.None;
        PositionPointers(button.transform, -width * 0.5f - 25f, width * 0.5f + 25f);
        InstallFeedback(button, null);
    }
    private static void PositionPointers(Transform parent, float left, float right)
    {
        foreach (var cursor in parent.GetComponentsInChildren<Cursor>(true))
        {
            var rect = (RectTransform)cursor.transform;
            rect.anchoredPosition = new Vector2(cursor.name == "CursorL" ? left : right, 0f);
        }
    }
    private static void InstallFeedback(Button button, Action<bool> highlighted)
    {
        var hover = button.GetComponent<ButtonHover>();
        if (hover == null) hover = button.gameObject.AddComponent<ButtonHover>();
        hover.SetCursors(button.GetComponentsInChildren<Cursor>(true));
        var feedback = button.GetComponent<OptionsRowFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<OptionsRowFeedback>();
        MenuClickEffect.Ensure(button);
        foreach (var pointer in button.GetComponentsInChildren<Cursor>(true))
            Group(pointer.transform).alpha = 1f;
        feedback.Highlight = active =>
        {
            highlighted?.Invoke(active);
        };
        feedback.Highlight(hover.IsHighlighted);
    }
    private void ClearRows()
    {
        foreach (Transform child in detail)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        rows.Clear();
        message.text = string.Empty;
    }
    private void Header(string caption, float y = 380f)
    {
        title.text = T(caption);
        title.font = Font();
        title.rectTransform.anchoredPosition = new Vector2(0f, y);
        fleur.rectTransform.anchoredPosition = new Vector2(0f, y - 65f);
    }
    private void BuildMenu()
    {
        category = null;
        ClearRows();
        Header("Options");
        detail.gameObject.SetActive(false);
        menu.gameObject.SetActive(true);
        reset.gameObject.SetActive(false);
        StyleCentered(back, "Back", -400f, 180f);
        int i = 0;
        foreach (Transform child in menu)
        {
            var button = child.GetComponent<Button>();
            if (button == null) continue;
            string name = child.name;
            StyleCentered(button, name, 210f - i++ * 88f, name == "Controller" ? 340f : 270f);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => MenuClickEffect.Ensure(button).PlayThen(() => Navigate(name)));
            rows.Add(new Row { Button = button, Group = Group(child), Caption = name });
        }
        ConfigureNavigation();
    }
    private void BuildCategory(string name)
    {
        category = name;
        ClearRows();
        menu.gameObject.SetActive(false);
        detail.gameObject.SetActive(true);
        reset.gameObject.SetActive(true);
        StyleCentered(reset, "Reset Defaults", name == "Keyboard" ? -315f : name == "Video" ? -330f : -300f, 500f);
        StyleCentered(back, "Back", name == "Controller" ? -445f : -400f, 180f);
        Header(name == "Game" ? "Game Options" : name == "ControllerAdvanced" || name == "VideoAdvanced" ? "Advanced Settings" : name == "ScreenScale" ? "Screen Scale" : name,
            name == "Video" || name.StartsWith("Controller") ? 405f : name == "Keyboard" ? 395f : 380f);
        switch (name)
        {
            case "Audio":
                AudioRow("Master Volume", 218f, () => GameOptions.Volume, GameOptions.SetVolume);
                AudioRow("Sound Volume", 134f, () => GameOptions.SoundVolume, v => GameOptions.SetFloat("SoundVolume", v));
                AudioRow("Music Volume", 50f, () => GameOptions.MusicVolume, v => GameOptions.SetFloat("MusicVolume", v));
                break;
            case "Game":
                Setting("Language", 238f, () => GameOptions.Chinese ? "简体中文" : "English", d => { Toggle("Chinese", GameOptions.Chinese); RefreshLanguage(); });
                Setting("Camera Shake", 164f, () => OnOff(GameOptions.CameraShake), d => Toggle("CameraShake", GameOptions.CameraShake));
                Setting("HUD Appearance", 90f, () => T(GameOptions.SmallHUD ? "Small" : "Large"), d => Toggle("SmallHUD", GameOptions.SmallHUD));
                Setting("Show Achievements", 16f, () => OnOff(GameOptions.ShowAchievements), d => Toggle("ShowAchievements", GameOptions.ShowAchievements), "Show in-game messages when earning an achievement.");
                Setting("Backer Credits", -58f, () => OnOff(GameOptions.BackerCredits), d => Toggle("BackerCredits", GameOptions.BackerCredits));
                break;
            case "Video":
                CollectResolutions();
                Setting("Resolution", 262f, ResolutionLabel, CycleResolution, null, 700f);
                Setting("Full Screen", 188f, () => OnOff(Fullscreen), d => GameOptions.SetDisplay(DisplayWidth, DisplayHeight, !Fullscreen), null, 700f);
                Setting("V-Sync", 114f, () => OnOff(GameOptions.VSync), d => Toggle("VSync", GameOptions.VSync), null, 700f);
                Setting("Frame Rate Cap", 40f, () => GameOptions.FrameRate < 0 ? T("Unlimited") : GameOptions.FrameRate.ToString(), CycleFrameRate, "Limit maximum frame rate for a consistent experience.", 700f);
                CenterAction("Screen Scale", -34f, () => Navigate("ScreenScale"));
                CenterAction("Brightness", -108f, () => Navigate("Brightness"));
                CenterAction("Advanced Settings", -182f, () => Navigate("VideoAdvanced"));
                break;
            case "Keyboard": BuildKeyboard(); break;
            case "Controller":
                reset.gameObject.SetActive(false);
                if (Gamepad.current == null)
                {
                    StaticText("No Active Controller", 190f, 37f);
                    StaticText("Press a button on the controller or connect a new controller to activate", 92f, 30f, true);
                }
                else { StaticText(Gamepad.current.displayName, 220f, 36f); PadRows(120f); }
                CenterAction("Advanced Settings", -360f, () => Navigate("ControllerAdvanced"));
                break;
            case "ControllerAdvanced":
                PadRows(210f);
                CenterAction("Vibration Test", -80f, () => { if (rumblePad == null) StartCoroutine(TestRumble()); });
                break;
            case "VideoAdvanced":
                Setting("Quality", 235f, () => QualitySettings.names[QualitySettings.GetQualityLevel()], d => GameOptions.Set("Quality", Wrap(QualitySettings.GetQualityLevel() + d, QualitySettings.names.Length)));
                Setting("Anti-Aliasing", 150f, () => QualitySettings.antiAliasing == 0 ? T("Off") : QualitySettings.antiAliasing + "x", CycleAA);
                Setting("Run in Background", 65f, () => OnOff(GameOptions.Background), d => Toggle("Background", GameOptions.Background));
                break;
            case "ScreenScale": AudioRow("Screen Scale", 100f, () => GameOptions.Float("ScreenScale", 1f), v => GameOptions.SetFloat("ScreenScale", Mathf.Clamp(v, 0.5f, 1f)), 5f); break;
            case "Brightness": AudioRow("Brightness", 100f, () => GameOptions.Float("Brightness", 0.5f), v => GameOptions.SetFloat("Brightness", v)); break;
        }
        ConfigureNavigation();
        RefreshLabels();
    }
    private Row NewRow(string caption, Vector2 position, float width = 900f)
    {
        var instance = Instantiate(rowTemplate, detail, false);
        instance.name = caption;
        instance.SetActive(true);
        SetRect((RectTransform)instance.transform, position, new Vector2(width, 64f));
        instance.GetComponent<TMP_Text>().text = string.Empty;
        var button = instance.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.transition = Selectable.Transition.None;
        PositionPointers(instance.transform, -width * 0.5f - 30f, width * 0.5f + 30f);
        var row = new Row { Button = button, Group = Group(instance.transform), Caption = caption };
        rows.Add(row);
        InstallFeedback(button, highlighted => { if (row.Help != null) row.Help.gameObject.SetActive(highlighted); });
        return row;
    }
    private Row Setting(string caption, float y, Func<string> value, Action<int> change, string help = null, float width = 900f)
    {
        var row = NewRow(caption, new Vector2(0f, y), width);
        row.Label = Text("Label", row.Button.transform, new Vector2(-width * 0.5f + 310f, 0f), new Vector2(570f, 60f), 36f, TextAlignmentOptions.Left);
        row.Value = Text("Value", row.Button.transform, new Vector2(width * 0.5f - 190f, 0f), new Vector2(330f, 60f), 32f, TextAlignmentOptions.Right);
        row.Read = value; row.Change = change; row.Description = help;
        if (!string.IsNullOrEmpty(help))
        {
            row.Help = Text("Description", row.Button.transform, new Vector2(0f, -34f), new Vector2(1100f, 40f), 25f, TextAlignmentOptions.Center, true);
            row.Help.text = T(help);
            row.Help.gameObject.SetActive(false);
        }
        row.Button.onClick.AddListener(() => MenuClickEffect.Ensure(row.Button).PlayThen(() => Change(row, 1)));
        return row;
    }
    private void CenterAction(string caption, float y, Action action)
    {
        var row = NewRow(caption, new Vector2(0f, y), caption == "Advanced Settings" ? 500f : 360f);
        row.Label = Text("Label", row.Button.transform, Vector2.zero, new Vector2(650f, 64f), 36f, TextAlignmentOptions.Center);
        row.Change = d => action();
        row.Button.onClick.AddListener(() => MenuClickEffect.Ensure(row.Button).PlayThen(() => Change(row, 1)));
    }
    private void StaticText(string caption, float y, float fontSize, bool body = false)
    {
        var text = Text(caption, detail, new Vector2(0f, y), new Vector2(1600f, 65f), fontSize, TextAlignmentOptions.Center, body);
        text.text = T(caption);
        rows.Add(new Row { Group = Group(text.transform), Caption = caption, Label = text });
    }
    private void AudioRow(string caption, float y, Func<float> read, Action<float> write, float min = 0f)
    {
        var row = Setting(caption, y, () => Mathf.RoundToInt(read() * 10f).ToString(), d => write(Mathf.Clamp(read() + d * 0.1f, min * 0.1f, 1f)));
        row.Value.font = Font(true);
        var sliderRect = Rect("Slider", row.Button.transform, new Vector2(210f, 0f), new Vector2(230f, 64f));
        sliderRect.gameObject.AddComponent<Image>().color = Color.clear;
        var slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = min; slider.maxValue = 10f; slider.wholeNumbers = true;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        var track = Rect("Track", sliderRect, Vector2.zero, new Vector2(210f, 3f));
        var background = track.gameObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.4f); background.raycastTarget = false;
        var fill = Rect("Fill", track, Vector2.zero, new Vector2(210f, 3f));
        fill.gameObject.AddComponent<Image>().raycastTarget = false;
        slider.fillRect = fill;
        fill.sizeDelta = new Vector2(0f, 3f);
        var area = Rect("Handle Area", sliderRect, Vector2.zero, new Vector2(210f, 64f));
        var handle = Rect("Handle", area, new Vector2(0f, 22f), new Vector2(20f, 34f));
        var image = handle.gameObject.AddComponent<OptionsSliderHandle>();
        slider.handleRect = handle; slider.targetGraphic = image;
        slider.SetValueWithoutNotify(read() * 10f);
        slider.onValueChanged.AddListener(value => { if (root.interactable && !switching) { write(value * 0.1f); RefreshLabels(); } });
        row.Slider = slider; row.ReadSlider = read;
    }
    private void BuildKeyboard()
    {
        int[] left = { 2, 3, 4, 6, 5, 7 }, right = { 0, 1, 8, 9, 10, 11 };
        for (int i = 0; i < 6; i++)
        {
            KeyRow(left[i], new Vector2(-330f, 220f - i * 70f));
            KeyRow(right[i], new Vector2(292f, 220f - i * 70f));
        }
        KeyRow(12, new Vector2(-20f, -200f));
    }
    private void KeyRow(int index, Vector2 position)
    {
        var row = NewRow(GameOptions.Actions[index], position, 500f);
        row.Label = Text("Label", row.Button.transform, new Vector2(-15f, 0f), new Vector2(390f, 60f), 32f, TextAlignmentOptions.Left);
        float width = index == 5 || index == 7 || index == 8 ? 96f : 56f;
        var frame = Rect("Keycap", row.Button.transform, new Vector2(218f - (width - 56f) * 0.5f, 0f), new Vector2(width, 56f));
        var image = frame.gameObject.AddComponent<Image>();
        image.sprite = keyBorder; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2.5f; image.raycastTarget = false;
        row.Value = Text("Key", frame, Vector2.zero, new Vector2(width - 8f, 50f), width > 56f ? 25f : 32f, TextAlignmentOptions.Center, true);
        row.Read = () => listening == index ? "..." : KeyLabel(GameOptions.Key(index));
        row.Change = d => BeginListen(index);
        row.Button.onClick.AddListener(() => MenuClickEffect.Ensure(row.Button).PlayThen(() => Change(row, 1)));
        PositionPointers(row.Button.transform, -260f, 290f);
    }
    private static string KeyLabel(string key)
    {
        if (key == "leftShift") return "L Shift";
        if (key == "rightShift") return "R Shift";
        if (key == "space") return "Space";
        if (key == "tab") return "Tab";
        return InputControlPath.ToHumanReadableString("<Keyboard>/" + key, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
    private void PadRows(float start)
    {
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Setting(GameOptions.Actions[i + 4], start - i * 82f, () => InputControlPath.ToHumanReadableString("<Gamepad>/" + GameOptions.Pad(index), InputControlPath.HumanReadableStringOptions.OmitDevice),
                d => GameOptions.SetPad(index, PadButtons[Wrap(Array.IndexOf(PadButtons, GameOptions.Pad(index)) + d, PadButtons.Length)]));
        }
    }
    private void ConfigureNavigation()
    {
        var active = rows.FindAll(r => r.Button != null && r.Button.interactable).ConvertAll(r => r.Button);
        if (reset.gameObject.activeSelf) active.Add(reset);
        active.Add(back);
        for (int i = 0; i < active.Count; i++)
            active[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = active[Wrap(i - 1, active.Count)], selectOnDown = active[Wrap(i + 1, active.Count)] };
        if (category == "Keyboard")
            for (int i = 0; i < 12; i++)
            {
                var nav = active[i].navigation;
                nav.selectOnLeft = nav.selectOnRight = active[i % 2 == 0 ? i + 1 : i - 1];
                nav.selectOnUp = i < 2 ? back : active[i - 2];
                nav.selectOnDown = i >= 10 ? active[12] : active[i + 2];
                active[i].navigation = nav;
            }
    }
    private void Change(Row row, int direction)
    {
        if (switching || !root.interactable || listening >= 0) return;
        row.Change?.Invoke(direction);
        RefreshLabels();
    }
    private void RefreshLabels()
    {
        foreach (var row in rows)
        {
            if (row.Label != null) row.Label.text = T(row.Caption);
            if (row.Value != null && row.Read != null) row.Value.text = row.Read();
            if (row.Slider != null) row.Slider.SetValueWithoutNotify(row.ReadSlider() * 10f);
        }
    }
    private void RefreshLanguage()
    {
        title.font = Font(); title.text = T(category == "Game" ? "Game Options" : category ?? "Options");
        foreach (var row in rows)
        {
            if (row.Label != null) row.Label.font = Font();
            if (row.Value != null) row.Value.font = Font(true);
            if (row.Help != null) { row.Help.font = Font(true); row.Help.text = T(row.Description); }
        }
        StyleCentered(reset, "Reset Defaults", reset.GetComponent<RectTransform>().anchoredPosition.y, 500f);
        StyleCentered(back, "Back", back.GetComponent<RectTransform>().anchoredPosition.y, 180f);
        RefreshLabels();
    }
    private static void Toggle(string key, bool value) => GameOptions.Set(key, value ? 0 : 1);
    private bool Fullscreen => GameOptions.GetBool("Fullscreen", true);
    private int DisplayWidth => PlayerPrefs.GetInt("Options.Width", Screen.currentResolution.width);
    private int DisplayHeight => PlayerPrefs.GetInt("Options.Height", Screen.currentResolution.height);
    private string ResolutionLabel() => $"{DisplayWidth} x {DisplayHeight} @ {Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)}Hz";
    private void CollectResolutions()
    {
        resolutions.Clear();
        foreach (var r in Screen.resolutions)
            if (r.width >= 800 && r.height >= 600 && !resolutions.Exists(v => v.width == r.width && v.height == r.height)) resolutions.Add(r);
        if (resolutions.Count == 0) resolutions.Add(Screen.currentResolution);
    }
    private void CycleResolution(int direction)
    {
        int index = resolutions.FindIndex(r => r.width == DisplayWidth && r.height == DisplayHeight);
        var resolution = resolutions[Wrap(index + direction, resolutions.Count)];
        GameOptions.SetDisplay(resolution.width, resolution.height, Fullscreen);
    }
    private void CycleFrameRate(int direction)
    {
        var limits = new List<int> { 30, 60, 90, 120, 144, 165, 180, 220, 240, 360 };
        int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        if (refresh > 0 && !limits.Contains(refresh)) { limits.Add(refresh); limits.Sort(); }
        limits.Add(-1);
        GameOptions.Set("FrameRate", limits[Wrap(limits.IndexOf(GameOptions.FrameRate) + direction, limits.Count)]);
    }
    private void CycleAA(int direction)
    {
        int[] levels = { 0, 2, 4, 8 };
        GameOptions.Set("AntiAliasing", levels[Wrap(Array.IndexOf(levels, QualitySettings.antiAliasing) + direction, levels.Length)]);
    }
    private void ResetCurrent()
    {
        if (switching || !root.interactable || listening >= 0) return;
        if (category == "ScreenScale") GameOptions.SetFloat("ScreenScale", 1f);
        else if (category == "Brightness") GameOptions.SetFloat("Brightness", 0.5f);
        else GameOptions.ResetCategory(category == "VideoAdvanced" ? "Video" : category == "ControllerAdvanced" ? "Controller" : category);
        RefreshLanguage();
    }
    private void Navigate(string name)
    {
        if (switching || !root.interactable) return;
        if (category == null && name != null) lastCategory = name;
        StartCoroutine(SwitchPage(name));
    }
    private IEnumerator SwitchPage(string name)
    {
        switching = true; root.interactable = false;
        yield return Animate(false);
        if (name == null) BuildMenu(); else BuildCategory(name);
        yield return Animate(true);
        root.interactable = true; switching = false;
        FocusFirst();
    }
    public void FocusFirst()
    {
        var row = rows.Find(r => r.Button != null && r.Button.interactable && (category != null || r.Caption == lastCategory));
        if (row == null) row = rows.Find(r => r.Button != null && r.Button.interactable);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (row != null) row.Button.Select(); else back.Select();
    }
    private void Back()
    {
        if (switching || !root.interactable) return;
        if (listening >= 0) { CancelListen(string.Empty); return; }
        if (category == "VideoAdvanced" || category == "ScreenScale" || category == "Brightness") Navigate("Video");
        else if (category == "ControllerAdvanced") Navigate("Controller");
        else if (category != null) Navigate(null); else close();
    }
    private void BeginListen(int index)
    {
        listening = index; listenStart = Time.unscaledTime;
        message.text = T("Press a key. Esc to cancel.");
        if (EventSystem.current != null)
        {
            navigationBeforeCapture = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
        }
    }
    private void CancelListen(string text)
    {
        if (listening >= 0 && EventSystem.current != null) EventSystem.current.sendNavigationEvents = navigationBeforeCapture;
        listening = -1;
        if (message != null) message.text = T(text);
        RefreshLabels();
    }
    private void Update()
    {
        if (!initialized || switching || !root.interactable) return;
        if (deviceChanged)
        {
            deviceChanged = false;
            if (category == "Controller") { Navigate("Controller"); return; }
        }
        var keyboard = Keyboard.current; var pad = Gamepad.current;
        if (listening >= 0)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { CancelListen(string.Empty); return; }
            if (Time.unscaledTime - listenStart > 10f) { CancelListen(string.Empty); return; }
            if (keyboard == null || Time.unscaledTime <= listenStart) return;
            foreach (var key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame || key == keyboard.escapeKey) continue;
                if (!GameOptions.SetKey(listening, key.name)) { message.text = T("That key is already assigned."); return; }
                CancelListen(string.Empty); return;
            }
            return;
        }
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame) { Back(); return; }
        RefreshLabels();
        if (category == null || category == "Keyboard" || EventSystem.current == null) return;
        int direction = keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame || pad != null && pad.dpad.left.wasPressedThisFrame ? -1 :
            keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame || pad != null && pad.dpad.right.wasPressedThisFrame ? 1 : 0;
        if (direction == 0) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        var selectedRow = rows.Find(r => r.Button != null && r.Button.gameObject == selected);
        if (selectedRow != null) Change(selectedRow, direction);
    }
    private void OnDeviceChange(InputDevice device, InputDeviceChange change) { if (device is Gamepad) deviceChanged = true; }
    public void PrepareExit()
    {
        CancelListen(string.Empty);
        foreach (var hover in GetComponentsInChildren<ButtonHover>()) hover.ResetHover();
        foreach (var row in rows) if (row.Help != null) row.Help.gameObject.SetActive(false);
    }
    public IEnumerator Animate(bool entering)
    {
        PrepareExit();
        float duration = 0.32f + rows.Count * 0.04f, elapsed = 0f;
        while (elapsed < duration)
        {
            Sample(elapsed, entering);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        Sample(duration, entering);
    }
    private void Sample(float elapsed, bool entering)
    {
        titleGroup.alpha = Visibility(elapsed, 0f, 0.2f, entering);
        float progress = Mathf.Clamp01(elapsed / 0.3f);
        if (!entering) progress = 1f - progress;
        fleurGroup.alpha = progress > 0f ? 1f : 0f;
        var sprite = frames[Mathf.Min((int)(progress * frames.Length), frames.Length - 1)];
        if (sprite != null) fleur.sprite = sprite;
        for (int i = 0; i < rows.Count; i++) rows[i].Group.alpha = Visibility(elapsed, 0.08f + (entering ? i : rows.Count - 1 - i) * 0.04f, 0.22f, entering);
        float footer = Visibility(elapsed, entering ? 0.12f + rows.Count * 0.04f : 0f, 0.2f, entering);
        backGroup.alpha = resetGroup.alpha = footer; message.alpha = footer;
    }
    private static float Visibility(float elapsed, float delay, float duration, bool entering)
    {
        float value = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - delay) / duration));
        return entering ? value : 1f - value;
    }
    private IEnumerator TestRumble()
    {
        rumblePad = Gamepad.current;
        if (rumblePad == null) yield break;
        rumblePad.SetMotorSpeeds(0.35f, 0.35f);
        yield return new WaitForSecondsRealtime(0.2f);
        StopRumble();
    }
    private void StopRumble()
    {
        if (rumblePad != null && rumblePad.added) rumblePad.SetMotorSpeeds(0f, 0f);
        rumblePad = null;
    }
    private void Resize()
    {
        if (presentation == null) return;
        var size = ((RectTransform)transform).rect.size;
        presentation.localScale = Vector3.one * Mathf.Min(size.x / 1920f, size.y / 1080f);
    }
    private void SetBackdrop(bool blur)
    {
        if (blur)
        {
            var shader = Resources.Load<Shader>("OptionsBackdropBlur");
            if (shader == null) return;
            if (backdropBlur == null) backdropBlur = new Material(shader);
            foreach (var image in FindObjectsByType<Image>())
            {
                if (!image.name.StartsWith("VoidHeartBG") || image.sprite == null || backdropMaterials.ContainsKey(image)) continue;
                backdropMaterials.Add(image, image.material);
                image.material = backdropBlur;
            }
        }
        else
        {
            foreach (var pair in backdropMaterials) if (pair.Key != null) pair.Key.material = pair.Value;
            backdropMaterials.Clear();
        }
    }
    private void OnRectTransformDimensionsChange() { if (initialized) Resize(); }
    private void OnEnable() { if (initialized) SetBackdrop(true); }
    private void OnDisable() { if (initialized) CancelListen(string.Empty); StopRumble(); SetBackdrop(false); }
    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        SetBackdrop(false);
        if (backdropBlur != null) Destroy(backdropBlur);
    }
}
