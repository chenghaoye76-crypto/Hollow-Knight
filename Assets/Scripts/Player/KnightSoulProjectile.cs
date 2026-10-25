using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class KnightSoulProjectile : MonoBehaviour
{
    public float speed = 22.5f;
    public int damage = 30;
    private int direction;
    private float remaining = 1.5f;
    private readonly HashSet<ICombatTarget> hit = new();
    public static KnightSoulProjectile Launch(KnightSpellAssets assets, Scene scene, Vector3 position, int direction)
    {
        var visual = KnightSpellVisual.Create(assets, "Shade Soul", assets.shadeProjectile, position, new Vector2(3.4f, 1.5f), 1.5f, loop: true);
        if (visual == null) return null;
        SceneManager.MoveGameObjectToScene(visual.gameObject, scene);
        // PlayerSprite's Fireball2 sequence faces right.
        if (direction < 0) visual.transform.rotation = Quaternion.Euler(0, 180, 0);
        var projectile = visual.gameObject.AddComponent<KnightSoulProjectile>();
        projectile.direction = direction;
        return projectile;
    }
    private void FixedUpdate()
    {
        float travel = direction*speed*Time.fixedDeltaTime;
        var position = transform.position;
        KnightSpellDamage.Area(gameObject.scene, (Vector2)position+Vector2.right*travel*.5f,
            new Vector2(2.5f+Mathf.Abs(travel), 1.2f), damage, hit);
        transform.position = position+Vector3.right*travel;
        remaining -= Time.fixedDeltaTime;
        if (remaining <= 0) Destroy(gameObject);
    }
}
