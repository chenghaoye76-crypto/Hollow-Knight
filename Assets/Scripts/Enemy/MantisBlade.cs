using System.Collections.Generic;
using UnityEngine;

public sealed class MantisBlade : MonoBehaviour
{
    public enum Pattern { Single, WidePair, ShortPair, WallPunish }
    public bool IsActive => encounter != null && encounter.IsFighting && age < duration;
    private MantisLordsEncounter encounter;
    private SpriteRenderer visual;
    private Sprite[] frames;
    private Vector2 origin;
    private int direction;
    private bool high;
    private Pattern pattern;
    private float age, duration, distance;
    private readonly List<Collider2D> hits = new();
    public static MantisBlade Launch(MantisLord owner, Vector2 origin, int direction, bool high, Pattern pattern)
    {
        var go = new GameObject("Mantis spinning blade", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
        go.transform.SetParent(owner.Encounter.transform, false); go.transform.position = origin;
        var blade = go.AddComponent<MantisBlade>(); blade.encounter = owner.Encounter;
        blade.origin = origin; blade.direction = direction; blade.high = high; blade.pattern = pattern;
        blade.duration = pattern == Pattern.ShortPair ? 2.8f : 2.3f;
        blade.distance = owner.Encounter.Right - owner.Encounter.Left - 1.4f;
        blade.frames = owner.assets.Frames("air projectile"); blade.visual = go.GetComponent<SpriteRenderer>();
        blade.visual.sharedMaterial = owner.assets.material; blade.visual.sortingLayerName = "Front"; blade.visual.sortingOrder = 15;
        var body = go.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        go.GetComponent<CircleCollider2D>().isTrigger = true; go.GetComponent<CircleCollider2D>().radius = .65f;
        return blade;
    }
    // Pure trajectory function shared by play mode and validation.
    public static Vector2 Position(Pattern pattern, Vector2 start, int direction, bool high, float distance, float floor, float t)
    {
        t = Mathf.Clamp01(t);
        float x, y;
        if (pattern == Pattern.ShortPair)
        {
            float half = distance * .5f;
            if (t < .3f) { x = Mathf.Lerp(0, half, t / .3f); y = Mathf.Lerp(start.y, floor + 4.3f, t / .3f); }
            else if (t < .67f) { x = half; y = Mathf.Lerp(floor + 4.3f, floor + 2.5f, (t - .3f) / .37f); }
            else { x = Mathf.Lerp(half, -1.5f, (t - .67f) / .33f); y = floor + 2.5f; }
            return new Vector2(start.x + direction * x, y);
        }
        float travel = Mathf.Sin(t * Mathf.PI);
        x = start.x + direction * distance * travel;
        y = pattern == Pattern.WidePair ? Mathf.Lerp(start.y, floor + .9f, t) :
            pattern == Pattern.WallPunish ? start.y + t * 1.2f :
            Mathf.Lerp(start.y, start.y + (high ? -3.8f : 3.8f), t);
        return new Vector2(x, y);
    }
    private void FixedUpdate()
    {
        if (!IsActive) { Destroy(gameObject); return; }
        Vector2 previous = transform.position;
        age += Time.fixedDeltaTime;
        Vector2 next = Position(pattern, origin, direction, high, distance, encounter.Floor, age / duration);
        transform.position = next;
        if (frames.Length > 0)
        {
            visual.sprite = frames[Mathf.FloorToInt(age * 20) % frames.Length];
            // Projectile sprites use a corner pivot; center them on the hitbox.
            Vector3 center = visual.sprite.bounds.center;
            // SpriteRenderer offset is handled by a child so the physics center stays fixed.
            if (visual.transform == transform)
            {
                var child = new GameObject("Blade artwork"); child.transform.SetParent(transform, false);
                var renderer = child.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = visual.sharedMaterial;
                renderer.sortingLayerID = visual.sortingLayerID; renderer.sortingOrder = visual.sortingOrder;
                renderer.sprite = visual.sprite; visual.enabled = false; visual = renderer;
            }
            visual.transform.localPosition = -center;
        }
        var filter = new ContactFilter2D { useTriggers = false };
        hits.Clear(); gameObject.scene.GetPhysicsScene2D().OverlapBox((previous + next) * .5f,
            new Vector2(1.1f + Mathf.Abs(next.x - previous.x), 1.1f + Mathf.Abs(next.y - previous.y)), 0, filter, hits);
        foreach (var hit in hits)
            if (hit.GetComponentInParent<PlayerController>() is PlayerController player && player == encounter.Player)
            { player.ReceiveEnemyHit(next); break; }
    }
}
