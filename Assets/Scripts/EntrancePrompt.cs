using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
public sealed class EntrancePrompt : MonoBehaviour
{
    public Prompt prompt;
    private readonly HashSet<Collider2D> occupants = new();
    private void OnEnable() { occupants.Clear();if(prompt!=null)prompt.HideImmediate(); }
    private void Start() { Refresh(); }
    private static bool IsPlayer(Collider2D other) => other != null && other.enabled && other.gameObject.activeInHierarchy && other.GetComponentInParent<PlayerController>() != null;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!IsPlayer(other))return;
        occupants.Add(other);Refresh();
    }
    private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);
    private void OnTriggerExit2D(Collider2D other) { occupants.Remove(other);Refresh(); }
    private void LateUpdate()
    {
        // Disabled/destroyed colliders do not always produce an exit callback.
        if(occupants.RemoveWhere(c=>!IsPlayer(c))>0)Refresh();
    }
    private void Refresh() { if(prompt!=null && prompt.IsVisible!=(occupants.Count>0))prompt.SetVisible(occupants.Count>0); }
    private void OnDisable() { occupants.Clear();if(prompt!=null)prompt.HideImmediate(); }
}
