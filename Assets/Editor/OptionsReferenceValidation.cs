using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// Exercise the actual button listeners and animation coroutines in Play Mode.
public static class OptionsReferenceValidation
{
    private static IEnumerator running;
    private static Keyboard keyboard;
    private static float volume;
    private static bool hadVolume;
    private static string leftKey;
    private static bool hadLeftKey;

    [MenuItem("Tools/Hollow Knight/Validate Reference UI", true)]
    private static bool Available() => EditorApplication.isPlaying && running == null;

    [MenuItem("Tools/Hollow Knight/Validate Reference UI")]
    private static void Start()
    {
        volume = GameOptions.Volume;
        hadVolume = PlayerPrefs.HasKey("Options.Volume");
        leftKey = GameOptions.Key(0);
        hadLeftKey = PlayerPrefs.HasKey("Options.Key.Left");
        running = CheckPages();
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || !running.MoveNext()) Finish();
        }
        catch (Exception error)
        {
            Finish();
            Debug.LogException(error);
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        running = null;
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        GameOptions.SetVolume(volume);
        if (!hadVolume) PlayerPrefs.DeleteKey("Options.Volume");
        if (hadLeftKey) PlayerPrefs.SetString("Options.Key.Left", leftKey);
        else PlayerPrefs.DeleteKey("Options.Key.Left");
        PlayerPrefs.Save();
    }

    private static IEnumerator CheckPages()
    {
        string folder = Path.GetFullPath("Temp/OptionsReferencePreviews");
        Directory.CreateDirectory(folder);
        var view = UnityEngine.Object.FindAnyObjectByType<OptionsMenuView>();
        if (view == null)
        {
            var options = UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Options" && b.transform.parent.name.StartsWith("MainMenuPanel"));
            Require(options != null, "Start this validation from MainMenu.");
            options.onClick.Invoke();
            double ready = EditorApplication.timeSinceStartup + 1.8;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            view = UnityEngine.Object.FindAnyObjectByType<OptionsMenuView>();
        }
        Require(view != null, "Options opens from MainMenu.");
        Require(view.transform.Find("Presentation/Button").gameObject.activeInHierarchy, "Start validation on the Options category menu.");

        foreach (string page in new[] { "Audio", "Game", "Keyboard", "Controller", "Video" })
        {
            var button = view.transform.Find("Presentation/Button/" + page).GetComponent<Button>();
            button.onClick.Invoke();
            double ready = EditorApplication.timeSinceStartup + 1.8;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            Require(view.GetComponent<CanvasGroup>().interactable, page + " animation completes and unlocks input.");
            Transform settings = view.transform.Find("Presentation/Settings");
            var title = view.transform.Find("Presentation/Options").GetComponent<TMP_Text>();
            Require(title.text == OptionsText.Get(page == "Game" ? "Game Options" : page), page + " title.");
            if (page == "Audio")
            {
                var sliders = settings.GetComponentsInChildren<Slider>();
                Require(sliders.Length == 3, "Audio has three separate sliders.");
                Require(sliders.All(s => s.wholeNumbers && s.maxValue == 10f), "Audio range is 0 to 10.");
                Require(sliders.All(s => s.handleRect.GetComponent<CanvasRenderer>() != null), "Audio chevrons have a renderer.");
                sliders[0].value = 3f;
                Require(Mathf.Abs(GameOptions.Volume - 0.3f) < 0.001f, "Dragging the master slider applies volume.");
                Require(Mathf.Abs(sliders[0].fillRect.rect.width - 63f) < 1f, "Slider fill stays inside its 210-unit track.");
                GameOptions.SetVolume(volume);
            }
            if (page == "Keyboard")
            {
                Require(settings.GetComponentsInChildren<Image>().Count(i => i.name == "Keycap") == 13, "Keyboard has all thirteen keycaps.");
                var up = (RectTransform)settings.Find("Up");
                var left = (RectTransform)settings.Find("Left");
                Require(up.anchoredPosition.x < 0f && left.anchoredPosition.x > 0f, "Keyboard has two columns.");
                settings.Find("Left").GetComponent<Button>().onClick.Invoke();
                keyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F9));
                ready = EditorApplication.timeSinceStartup + 0.3;
                while (EditorApplication.timeSinceStartup < ready) yield return null;
                Require(GameOptions.Key(0) == "f9", "Clicking a keycap captures and saves the next key.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.RemoveDevice(keyboard); keyboard = null;
                Require(GameOptions.SetKey(0, leftKey), "Original key can be restored.");
            }
            if (page == "Controller" && Gamepad.current == null)
                Require(settings.GetComponentsInChildren<TMP_Text>().Any(t => t.text == OptionsText.Get("No Active Controller")), "Disconnected controller message.");
            if (page == "Video")
            {
                Require(settings.Find("Screen Scale") != null && settings.Find("Brightness") != null && settings.Find("Advanced Settings") != null, "Video links are present.");
            }
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, page + ".png"));
            ready = EditorApplication.timeSinceStartup + 0.3;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            view.transform.Find("Presentation/Back").GetComponent<Button>().onClick.Invoke();
            ready = EditorApplication.timeSinceStartup + 1.8;
            while (EditorApplication.timeSinceStartup < ready) yield return null;
            Require(view.transform.Find("Presentation/Button").gameObject.activeInHierarchy, page + " returns to the category menu.");
        }
        File.WriteAllText(Path.Combine(folder, "validation.txt"), "PASS: five pages, transitions and back navigation, three audio sliders and volume application, bounded slider fill, thirteen keyboard bindings, two-column layout, live key capture, controller disconnection, video links.");
        Debug.Log("OPTIONS REFERENCE UI VALIDATION PASSED. Previews: " + folder);
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Options reference UI validation failed: " + reason);
    }
}
