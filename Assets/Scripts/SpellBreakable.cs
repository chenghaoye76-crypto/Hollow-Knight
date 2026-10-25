using UnityEngine;
using UnityEngine.Events;

// Opt in only genuinely fragile structures; ordinary floors and walls remain solid.
[DisallowMultipleComponent]
public sealed class SpellBreakable : MonoBehaviour
{
    [Min(1)] public int health = 15;
    public UnityEvent broken;
    private bool isBroken;
    public void HitByDescendingDark(int damage)
    {
        if (isBroken || damage <= 0) return;
        health -= damage;
        if (health > 0) return;
        isBroken = true;
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        broken?.Invoke();
        Destroy(gameObject);
    }
}
