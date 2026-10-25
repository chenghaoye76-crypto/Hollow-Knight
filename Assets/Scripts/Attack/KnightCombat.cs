using UnityEngine;

public static class KnightCombat
{
    public static bool NailHit(PlayerController player, Collider2D collider, int direction)
    {
        var blade = collider.GetComponentInParent<MantisBlade>();
        if (blade != null && blade.IsActive)
        {
            if (direction < 0) player.DownHitRepel();
            else if (direction > 0) player.UpHitRepel();
            else player.HitRepel();
            player.PlayTerrainHit();
            return true;
        }
        var target = collider.GetComponentInParent<ICombatTarget>();
        if (target == null || !target.CanBeHit || target.CurrentHealth <= 0) return false;
        if (direction < 0) player.DownHitRepel();
        else if (direction > 0) player.UpHitRepel();
        else
        {
            player.HitRepel();
            var enemy = collider.GetComponentInParent<EnemyController>();
            if (enemy != null) enemy.StartCoroutine(enemy.Hurt());
        }
        player.DealNailDamage(target);
        return true;
    }
}
