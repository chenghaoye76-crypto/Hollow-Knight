using System.Collections.Generic;
using UnityEngine;

// No collider means the whole room uses this policy. Optional bounds can mark a smaller area.
public class KnightMovementArea : MonoBehaviour
{
    private static readonly HashSet<KnightMovementArea> active = new();
    [Tooltip("庄重区域：地面走动和空中水平移动均使用 6 的速度。")]
    public bool walkOnly;
    [Tooltip("留空作用于整个场景；指定 Collider2D 时只在范围内部生效。边界建议覆盖空中移动空间。")]
    public Collider2D areaBounds;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    public static bool RequiresWalking(PlayerController player)
    {
        foreach (var area in active)
            if (area != null && area.walkOnly && area.gameObject.scene == player.gameObject.scene &&
                (area.areaBounds == null || (area.areaBounds.enabled && area.areaBounds.gameObject.activeInHierarchy &&
                 area.areaBounds.OverlapPoint(player.transform.position)))) return true;
        return false;
    }
}
