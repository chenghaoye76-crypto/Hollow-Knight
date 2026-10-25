using UnityEngine;

public class UpAttackHit : MonoBehaviour
{
    private PlayerController player;
    private bool hasHit = false;

    public void SetPlayer(PlayerController playerController)
    {
        player = playerController;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (player == null || hasHit) return;
        if (KnightCombat.NailHit(player, collision, 1))
        {
            hasHit = true;
        }
        else if(collision.gameObject.layer == LayerMask.NameToLayer("Terrain"))
        {
            hasHit = true;
            player.PlayTerrainHit();
            player.UpHitRepel();
        }
    }
}
