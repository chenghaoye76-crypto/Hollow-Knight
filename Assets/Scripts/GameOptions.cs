using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Settings are independent of save slots and are applied even when starting in Game.
public static class GameOptions
{
    private const string Prefix = "Options.";
    public static event Action Changed;
    public static readonly string[] Actions = { "Left", "Right", "Up", "Down", "Jump", "Dash", "Attack", "Focus / Cast", "Quick Map", "Super Dash", "Dream Nail", "Quick Cast", "Inventory" };
    private static readonly string[] DefaultKeys = { "a", "d", "w", "s", "k", "leftShift", "j", "space", "tab", "o", "l", "f", "i" };
    private static readonly string[] DefaultButtons = { "buttonSouth", "rightShoulder", "buttonWest" };

    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", 1f));
    public static float SoundVolume => Float("SoundVolume", 1f);
    public static float MusicVolume => Float("MusicVolume", 1f);
    public static bool Chinese => GetBool("Chinese", false);
    public static bool CameraShake => GetBool("CameraShake", true);
    public static bool SmallHUD => GetBool("SmallHUD", true);
    public static bool ShowAchievements => GetBool("ShowAchievements", true);
    public static bool BackerCredits => GetBool("BackerCredits", false);
    public static float Float(string name, float fallback) => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + name, fallback));
    public static bool Muted => GetBool("Muted", false);
    public static bool AutoSave => GetBool("AutoSave", true);
    public static bool Background => GetBool("Background", false);
    public static bool VSync => GetBool("VSync", true);
    public static int FrameRate => PlayerPrefs.GetInt(Prefix + "FrameRate", 60);
    public static bool GetBool(string name, bool fallback) => PlayerPrefs.GetInt(Prefix + name, fallback ? 1 : 0) != 0;
    public static string Key(int index) => PlayerPrefs.GetString(Prefix + "Key." + Actions[index], DefaultKeys[index]);
    public static string Pad(int index) => PlayerPrefs.GetString(Prefix + "Pad." + index, DefaultButtons[index]);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
        displayApplied = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => Apply();

    public static void Set(string name, int value)
    {
        PlayerPrefs.SetInt(Prefix + name, value);
        Commit();
    }

    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(Prefix + "Volume", Mathf.Clamp01(value));
        Commit();
    }

    public static void SetFloat(string name, float value)
    {
        PlayerPrefs.SetFloat(Prefix + name, Mathf.Clamp01(value));
        Commit();
    }

    public static bool SetKey(int index, string key)
    {
        for (int i = 0; i < Actions.Length; i++)
            if (i != index && string.Equals(Key(i), key, StringComparison.OrdinalIgnoreCase))
                return false;
        PlayerPrefs.SetString(Prefix + "Key." + Actions[index], key);
        Commit();
        return true;
    }

    public static void SetPad(int index, string button)
    {
        // Swap conflicts so all three actions remain reachable.
        string previous = Pad(index);
        for (int i = 0; i < 3; i++)
            if (i != index && Pad(i) == button)
                PlayerPrefs.SetString(Prefix + "Pad." + i, previous);
        PlayerPrefs.SetString(Prefix + "Pad." + index, button);
        Commit();
    }

    public static void ResetCategory(string category)
    {
        string[] names = category == "Audio" ? new[] { "Volume", "Muted", "SoundVolume", "MusicVolume" } :
            category == "Video" ? new[] { "VSync", "FrameRate", "Fullscreen", "Width", "Height", "Quality", "ScreenScale", "Brightness", "AntiAliasing" } :
            category == "Game" ? new[] { "Chinese", "CameraShake", "SmallHUD", "ShowAchievements", "BackerCredits" } :
            category == "Controller" ? new[] { "Pad.0", "Pad.1", "Pad.2" } : Array.Empty<string>();
        foreach (string name in names) PlayerPrefs.DeleteKey(Prefix + name);
        if (category == "Keyboard")
            foreach (string action in Actions) PlayerPrefs.DeleteKey(Prefix + "Key." + action);
        if (category == "Video") QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1);
        Commit();
        if (category == "Video") SetDisplay(Screen.currentResolution.width, Screen.currentResolution.height, true);
    }

    public static void SetDisplay(int width, int height, bool fullscreen)
    {
        displayApplied = true;
        PlayerPrefs.SetInt(Prefix + "Width", width);
        PlayerPrefs.SetInt(Prefix + "Height", height);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", fullscreen ? 1 : 0);
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        PlayerPrefs.Save();
    }

    private static void Commit()
    {
        PlayerPrefs.Save();
        Apply();
        Changed?.Invoke();
    }

    private static void Apply()
    {
        AudioListener.volume = Muted ? 0f : Volume;
        Application.runInBackground = Background;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = FrameRate;
        if (PlayerPrefs.HasKey(Prefix + "AntiAliasing"))
            QualitySettings.antiAliasing = PlayerPrefs.GetInt(Prefix + "AntiAliasing");
        if (PlayerPrefs.HasKey(Prefix + "Quality") && PlayerPrefs.GetInt(Prefix + "Quality") != QualitySettings.GetQualityLevel())
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality"), 0, QualitySettings.names.Length - 1));
        if (PlayerPrefs.HasKey(Prefix + "Width") && !displayApplied)
        {
            displayApplied = true;
            Screen.SetResolution(PlayerPrefs.GetInt(Prefix + "Width"), PlayerPrefs.GetInt(Prefix + "Height"),
                GetBool("Fullscreen", true) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }

    private static bool displayApplied;

    public static void ConfigureInput(InputAction movement, InputAction jump, InputAction dash,
        InputAction attack, InputAction up, InputAction down)
    {
        // Rebuild only these private gameplay actions; UI input keeps its own bindings.
        foreach (var action in new[] { movement, jump, dash, attack, up, down })
        {
            action.Disable();
            while (action.bindings.Count > 0) action.ChangeBinding(0).Erase();
        }
        movement.AddCompositeBinding("2DVector").With("Left", "<Keyboard>/" + Key(0))
            .With("Right", "<Keyboard>/" + Key(1));
        movement.AddBinding("<Gamepad>/leftStick");
        movement.AddBinding("<Gamepad>/dpad");
        InputAction[] buttons = { jump, dash, attack };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].AddBinding("<Keyboard>/" + Key(i + 4));
            buttons[i].AddBinding("<Gamepad>/" + Pad(i));
        }
        up.AddCompositeBinding("OneModifier").With("Modifier", "<Keyboard>/" + Key(2))
            .With("Binding", "<Keyboard>/" + Key(6));
        down.AddCompositeBinding("OneModifier").With("Modifier", "<Keyboard>/" + Key(3))
            .With("Binding", "<Keyboard>/" + Key(6));
        foreach (string direction in new[] { "leftStick", "dpad" })
        {
            up.AddCompositeBinding("OneModifier").With("Modifier", "<Gamepad>/" + direction + "/up")
                .With("Binding", "<Gamepad>/" + Pad(2));
            down.AddCompositeBinding("OneModifier").With("Modifier", "<Gamepad>/" + direction + "/down")
                .With("Binding", "<Gamepad>/" + Pad(2));
        }
        foreach (var action in new[] { movement, jump, dash, attack, up, down }) action.Enable();
    }
}
