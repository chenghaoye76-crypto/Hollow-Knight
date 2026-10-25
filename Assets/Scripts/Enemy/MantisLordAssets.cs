using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hollow Knight/Mantis Lord Assets")]
public sealed class MantisLordAssets : ScriptableObject
{
    [Serializable] public struct Sequence { public string name; public Sprite[] frames; }
    public Sequence[] sequences;
    public Material material;
    public AudioClip music, victory, dash, drop, impact, vanish, land, wall, death, stand, gate, spikes;
    public Sprite[] Frames(string name)
    {
        if (sequences != null)
            foreach (var sequence in sequences) if (sequence.name == name) return sequence.frames;
        return Array.Empty<Sprite>();
    }
}
