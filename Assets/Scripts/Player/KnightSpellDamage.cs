using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KnightSpellDamage
{
    // A separate set per pulse prevents multiple colliders from multiplying spell damage.
    public static int Area(Scene scene, Vector2 center, Vector2 size, int damage,
        HashSet<ICombatTarget> alreadyHit = null, bool breakStructures = false)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        var hits = new List<Collider2D>();
        scene.GetPhysicsScene2D().OverlapBox(center, size, 0, filter, hits);
        var enemies = alreadyHit ?? new HashSet<ICombatTarget>();
        var structures = new HashSet<SpellBreakable>();
        int count = 0;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<ICombatTarget>();
            if (enemy != null && enemy.CanBeHit && enemy.CurrentHealth > 0 && enemies.Add(enemy))
            { enemy.TakeDamage(damage); count++; }
            if (!breakStructures) continue;
            var structure = hit.GetComponentInParent<SpellBreakable>();
            if (structure != null && structures.Add(structure)) structure.HitByDescendingDark(damage);
        }
        return count;
    }
}
