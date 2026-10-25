using UnityEngine;

// Spell art comes from PlayerSprite's dedicated effect sequences.
public sealed class KnightSpellVisual : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer rim, core;
    private float age, duration;
    private Vector2 size;
    private Color tint;
    private Transform follow;
    private Vector3 followOffset;
    private bool loop, grow;
    public static KnightSpellVisual Create(KnightSpellAssets assets, string label, Sprite[] frames,
        Vector3 position, Vector2 size, float duration, bool dark = false, Transform follow = null,
        bool loop = false, bool grow = false, float angle = 0)
    {
        if (assets == null || frames == null || frames.Length == 0) return null;
        var go = new GameObject(label);
        var visual = go.AddComponent<KnightSpellVisual>();
        go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, 0, angle);
        visual.frames = frames; visual.duration = duration; visual.size = size;
        visual.loop = loop; visual.grow = grow; visual.follow = follow;
        visual.followOffset = follow != null ? position-follow.position : Vector3.zero;
        visual.tint = dark ? new Color(.72f, .79f, 1f, .85f) : new Color(.85f, .92f, 1f);
        visual.rim = go.AddComponent<SpriteRenderer>();
        visual.rim.sharedMaterial = assets.material;
        visual.rim.sortingLayerName = "Front"; visual.rim.sortingOrder = 65;
        if (dark)
        {
            var child = new GameObject("Shadow core"); child.transform.SetParent(go.transform, false);
            child.transform.localScale = Vector3.one * .87f;
            visual.core = child.AddComponent<SpriteRenderer>(); visual.core.sharedMaterial = assets.material;
            visual.core.sortingLayerName = "Front"; visual.core.sortingOrder = 66;
        }
        visual.Draw();
        return visual;
    }
    private void Update()
    {
        age += Time.deltaTime;
        if (duration > 0 && age >= duration) { Destroy(gameObject); return; }
        Draw();
    }
    private void LateUpdate()
    {
        if (follow != null) transform.position = follow.position+followOffset;
    }
    private void Draw()
    {
        if (rim == null || frames == null) return;
        float phase = duration > 0 ? Mathf.Clamp01(age/duration) : 0;
        int index = loop || duration <= 0 ? Mathf.FloorToInt(age*18)%frames.Length : Mathf.Min(frames.Length-1, Mathf.FloorToInt(phase*frames.Length));
        var sprite = frames[index];
        rim.sprite = sprite;
        if (sprite == null) return;
        float fade = duration > 0 ? Mathf.Min(1, (1-phase)*5) : 1;
        float expansion = grow ? Mathf.Lerp(.35f, 1.3f, phase) : 1;
        transform.localScale = new Vector3(size.x/Mathf.Max(.01f,sprite.bounds.size.x), size.y/Mathf.Max(.01f,sprite.bounds.size.y), 1)*expansion;
        var color = tint; color.a *= fade; rim.color = color;
        if (core != null) { core.sprite = sprite; core.color = new Color(.025f, .018f, .055f, fade); }
    }
}
