using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Invincibility : MonoBehaviour
{
    public SpriteRenderer render;
    public Color normalColor = Color.white;
    public Color flashColor = new Color(1f, 1f, 1f, 0.5f);
    public float defaultDuration = 1.3f;
    public bool isInvincible;
    private Coroutine invCoroutine;
    private readonly List<(Collider2D player, Collider2D enemy)> ignoredPairs = new();

    private void Awake() { if (render == null) render = GetComponent<SpriteRenderer>(); }
    private void Reset() => render = GetComponent<SpriteRenderer>();
    public void Activate(float seconds)
    {
        if (!isActiveAndEnabled) return;
        if (invCoroutine != null) StopCoroutine(invCoroutine);
        Restore();
        invCoroutine = StartCoroutine(DoInvincibility(seconds > 0f ? seconds : defaultDuration));
    }
    private IEnumerator DoInvincibility(float seconds)
    {
        isInvincible = true;
        // 仅忽略当前角色与敌人的碰撞，避免全局层碰撞开关影响其他物体。
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        var ownColliders = GetComponentsInChildren<Collider2D>();
        foreach (var enemy in FindObjectsByType<Collider2D>())
        {
            if (enemy.gameObject.layer != enemyLayer || enemy.gameObject.scene != gameObject.scene) continue;
            foreach (var own in ownColliders)
            {
                if (own == enemy || Physics2D.GetIgnoreCollision(own, enemy)) continue;
                Physics2D.IgnoreCollision(own, enemy, true);
                ignoredPairs.Add((own, enemy));
            }
        }
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (render != null) render.color = ((int)(elapsed / 0.1f) & 1) == 0 ? flashColor : normalColor;
            yield return null;
            elapsed += Time.deltaTime;
        }
        invCoroutine = null;
        Restore();
    }
    private void Restore()
    {
        foreach (var pair in ignoredPairs)
            if (pair.player != null && pair.enemy != null) Physics2D.IgnoreCollision(pair.player, pair.enemy, false);
        ignoredPairs.Clear();
        if (render != null) render.color = normalColor;
        isInvincible = false;
    }
    private void OnDisable()
    {
        if (invCoroutine != null) StopCoroutine(invCoroutine);
        invCoroutine = null;
        Restore();
    }
}
