using UnityEngine;

// Put this on a music source to explicitly route it to the Music Volume slider.
// Unmarked sources are routed to Sound Volume by OptionsRuntime.
[RequireComponent(typeof(AudioSource))]
public sealed class OptionsAudioChannel : MonoBehaviour
{
    public bool music;
    private AudioSource source;
    private float authoredVolume;
    private void Awake() { source = GetComponent<AudioSource>(); authoredVolume = source.volume; }
    private void OnEnable() { GameOptions.Changed += Apply; Apply(); }
    private void OnDisable() { GameOptions.Changed -= Apply; }
    public void Apply()
    {
        if (source != null) source.volume = authoredVolume * (music ? GameOptions.MusicVolume : GameOptions.SoundVolume);
    }
}
