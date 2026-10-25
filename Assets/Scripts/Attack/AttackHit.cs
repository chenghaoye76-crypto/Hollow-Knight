using UnityEngine;

public class AttackHit : MonoBehaviour
{
    private PlayerController player;
    private bool hasHit = false;
    private bool terrainSoundPlayed;

    public void SetPlayer(PlayerController playerController)
    {
        player = playerController;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (player == null || hasHit) return;
        if (KnightCombat.NailHit(player, collision, 0))
        {
            hasHit = true;
        }
        else if (!terrainSoundPlayed && collision.gameObject.layer == LayerMask.NameToLayer("Terrain"))
        { terrainSoundPlayed=true;player.PlayTerrainHit(); }
    }
}
