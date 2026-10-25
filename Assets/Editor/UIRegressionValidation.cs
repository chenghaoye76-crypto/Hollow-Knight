using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 使用独立 Canvas 验证真实预制体和按钮，不读写存档、不切换游戏场景。
[InitializeOnLoad]
public static class UIRegressionValidation
{
    private const string Request = "Temp/ValidateUIRegression.request";
    private static IEnumerator run;
    private static GameObject fixture, selection;
    private static float oldScale;
    private static bool oldPaused, oldBackground;
    private static readonly List<string> report = new();

    static UIRegressionValidation() => EditorApplication.update += Tick;

    [MenuItem("Tools/Hollow Knight/Validate UI Regression", true)]
    private static bool Available() => EditorApplication.isPlaying && run == null;

    [MenuItem("Tools/Hollow Knight/Validate UI Regression")]
    public static void Begin()
    {
        if (!Available()) return;
        report.Clear();
        oldScale = Time.timeScale;
        oldPaused = EditorApplication.isPaused;
        oldBackground = Application.runInBackground;
        selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        EditorApplication.isPaused = false;
        Application.runInBackground = true;
        Time.timeScale = 0;
        run = Validate();
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Request)) { File.Delete(Request); Begin(); }
        if (run == null) return;
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play Mode ended during validation.");
            if (!run.MoveNext()) Finish();
        }
        catch (Exception error) { report.Add("FAIL: " + error); Finish(); Debug.LogException(error); }
    }

    private static void Finish()
    {
        run = null;
        if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selection);
        Time.timeScale = oldScale;
        Application.runInBackground = oldBackground;
        EditorApplication.isPaused = oldPaused;
        File.WriteAllLines("Temp/UIRegressionValidation.txt", report);
    }

    private static IEnumerator Validate()
    {
        fixture = new GameObject("UI Regression Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        fixture.hideFlags = HideFlags.DontSave;
        fixture.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        fixture.GetComponent<Canvas>().sortingOrder = 1100;
        if (EventSystem.current == null)
        {
            var events = new GameObject("UI Regression EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(fixture.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var scaler = fixture.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var backdrop = new GameObject("Pause Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(fixture.transform, false);
        var rect = (RectTransform)backdrop.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
        var manager = new PanelManager(new UIManager(fixture.transform));
        int resumed = 0;
        manager.Push(new EscPanel(manager, () => manager.CloseRoot(() => resumed++)));
        var wait = Until(() => !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        var pause = manager.Current.PanelObject.GetComponent<PausePanelView>();
        var options = pause.FindButton("Options");
        var cursor = options.GetComponentInChildren<Cursor>(true);
        var progress = typeof(Cursor).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
        cursor.SetBtnState(false);
        wait = Delay(0.07); while (wait.MoveNext()) yield return null;
        float before = (float)progress.GetValue(cursor);
        Require(before > 0 && before < 1, "Cursor expands progressively at timeScale zero.");
        cursor.SetBtnState(true);
        Require(Mathf.Approximately(before, (float)progress.GetValue(cursor)), "Cursor reversal preserves its current frame.");
        wait = Delay(0.03); while (wait.MoveNext()) yield return null;
        Require((float)progress.GetValue(cursor) < before, "Cursor retracts from the current progress.");
        cursor.SetBtnState(false);
        wait = Delay(0.25); while (wait.MoveNext()) yield return null;
        Require(cursor.GetComponent<Image>().color.a > 0.99f, "Cursor remains visible after expansion.");
        report.Add("PASS: cursor expands/retracts continuously while paused.");

        options.onClick.Invoke(); options.onClick.Invoke();
        Require(!manager.Current.PanelObject.GetComponent<CanvasGroup>().interactable, "Click locks the page.");
        Require(options.transform.Find("Click Flash").GetComponent<Image>().enabled, "Pause menu click flash is visible.");
        wait = Until(() => manager.Current is OptionsPanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        Require(backdrop.activeInHierarchy, "Backdrop survives panel transitions.");
        var view = manager.Current.PanelObject.GetComponent<OptionsMenuView>();
        var audio = view.transform.Find("Presentation/Button/Audio").GetComponent<Button>();
        audio.onClick.Invoke(); audio.onClick.Invoke();
        Require(audio.transform.Find("Click Flash").GetComponent<Image>().enabled, "TMP-based Options rows have visible click flashes.");
        wait = Until(() => view.transform.Find("Presentation/Settings").gameObject.activeSelf && view.GetComponent<CanvasGroup>().interactable); while (wait.MoveNext()) yield return null;
        Require(view.transform.Find("Presentation/Settings").GetComponentsInChildren<Slider>().Length == 3, "Double-click opens Audio only once.");
        Require(view.GetComponentsInChildren<Cursor>().All(c => c.GetComponent<CanvasGroup>().alpha == 1), "Cursor retract animations are not cut off by CanvasGroups.");
        var back = view.transform.Find("Presentation/Back").GetComponent<Button>();
        back.onClick.Invoke();
        wait = Until(() => view.transform.Find("Presentation/Button").gameObject.activeSelf && view.GetComponent<CanvasGroup>().interactable); while (wait.MoveNext()) yield return null;
        back.onClick.Invoke();
        wait = Until(() => manager.Current is EscPanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        Require(EventSystem.current.currentSelectedGameObject == options.gameObject, "Returning restores Options focus.");
        Require(!options.transform.Find("Click Flash").GetComponent<Image>().enabled, "No click flash remains after returning.");
        report.Add("PASS: Options click flash, double-click guard, return focus, and persistent backdrop.");

        for (int i = 0; i < 2; i++)
        {
            pause.FindButton("QuitToMenu").onClick.Invoke();
            wait = Until(() => manager.Current is ConfirmPanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
            var no = manager.Current.PanelObject.GetComponent<PausePanelView>().FindButton("No");
            Require(EventSystem.current.currentSelectedGameObject == no.gameObject, "Confirmation defaults to No.");
            no.onClick.Invoke(); no.onClick.Invoke();
            Require(no.transform.Find("Click Flash").GetComponent<Image>().enabled, "Confirmation click flash is visible.");
            wait = Until(() => manager.Current is EscPanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        }
        pause.FindButton("Continue").onClick.Invoke();
        wait = Until(() => manager.Current == null); while (wait.MoveNext()) yield return null;
        Require(resumed == 1, "Continue closes the root exactly once.");
        Require(Time.timeScale == 0, "UI tests must leave gameplay paused.");
        report.Add("PASS: repeated confirmation/cancel, click feedback, and root close without duplicate callbacks.");

        manager.Push(new MainMenuPanel(manager));
        wait = Until(() => !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        var start = manager.Current.PanelObject.transform.Find("StartGame").GetComponent<Button>();
        start.onClick.Invoke(); start.onClick.Invoke();
        Require(start.transform.Find("Click Flash").GetComponent<Image>().enabled, "Main menu uses the same click effect.");
        wait = Until(() => manager.Current is RofilePanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        var profileBack = manager.Current.PanelObject.transform.Find("Back").GetComponent<Button>();
        profileBack.onClick.Invoke();
        Require(profileBack.transform.Find("Click Flash").GetComponent<Image>().enabled, "Profile Back uses the same click effect.");
        wait = Until(() => manager.Current is MainMenuPanel && !manager.IsTransitioning); while (wait.MoveNext()) yield return null;
        report.Add("PASS: main menu/profile click effect and round trip; no slots opened, changed, or deleted.");
    }

    private static IEnumerator Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 8;
        while (!condition())
        {
            Require(EditorApplication.timeSinceStartup < deadline, "UI transition timed out.");
            yield return null;
        }
    }
    private static IEnumerator Delay(double seconds)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < deadline) yield return null;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
