using TMPro;
using UnityEngine;
using UIButton = UnityEngine.UI.Button;

// Bind scene actions on instances rather than storing scene references in prefabs.
public class MenuPanelView : MonoBehaviour
{
    private StartManager manager;
    private void Awake()
    {
        manager = FindFirstObjectByType<StartManager>();
        Bind("StartGame", () => manager.StartGame());
        Bind("Options", () => manager.OpenOptions());
        Bind("QuitGame", () => manager.QuitGame());
        Bind("Back", () => manager.Back());
        Bind("VolumeDown", () => SetVolume(AudioListener.volume - 0.1f));
        Bind("VolumeUp", () => SetVolume(AudioListener.volume + 0.1f));
        Bind("Fullscreen", () => {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshLabels();
        });
    }
    private void OnEnable() { RefreshLabels(); }
    private void Bind(string child, UnityEngine.Events.UnityAction action)
    {
        var target = transform.Find(child);
        if (target && target.TryGetComponent<UIButton>(out var button))
            button.onClick.AddListener(action);
    }
    private void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("UI.MasterVolume", AudioListener.volume);
        PlayerPrefs.Save();
        RefreshLabels();
    }
    private void RefreshLabels()
    {
        var volume = transform.Find("VolumeLabel");
        if (volume) volume.GetComponent<TMP_Text>().text = $"MASTER VOLUME  {Mathf.RoundToInt(AudioListener.volume * 100)}%";
        var fullscreen = transform.Find("Fullscreen");
        if (fullscreen) fullscreen.GetComponentInChildren<TMP_Text>(true).text =
            "FULLSCREEN: " + (Screen.fullScreen ? "ON" : "OFF");
    }
}