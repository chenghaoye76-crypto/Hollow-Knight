using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MantisLord : MonoBehaviour, ICombatTarget
{
    public MantisLordAssets assets;
    public SpriteRenderer visual;
    public int startingHealth = 210;
    public int CurrentHealth { get; private set; }
    public bool CanBeHit => fighting && !defeated && visual != null && visual.enabled;
    public bool IsBusy { get; private set; }
    public bool IsDefeated => defeated;
    public string CurrentMove { get; private set; } = "idle";
    public MantisLordsEncounter Encounter { get; private set; }
    private bool fighting, defeated;
    private Vector3 seat, seatScale;
    private BoxCollider2D hurtbox;
    private Sprite[] frames;
    private float animationTime, flashUntil;
    private bool loop;
    private readonly List<Collider2D> hits = new();

    public void Initialize(MantisLordsEncounter encounter, int health)
    {
        Encounter = encounter;
        startingHealth = health; CurrentHealth = health;
        seat = transform.position; seatScale = transform.localScale;
        visual ??= GetComponentInChildren<SpriteRenderer>();
        foreach (var collider in GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        var body = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        hurtbox = gameObject.AddComponent<BoxCollider2D>();
        hurtbox.isTrigger = true; hurtbox.offset = new Vector2(0, 1.8f); hurtbox.size = new Vector2(1.25f, 3.6f);
        hurtbox.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        if (visual != null)
        {
            if (assets.material != null) visual.sharedMaterial = assets.material;
            visual.sortingLayerName = "Front"; visual.sortingOrder = 12;
        }
        foreach (var particles in GetComponentsInChildren<ParticleSystem>(true)) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Play("idle");
    }
    private void Update()
    {
        if (frames != null && frames.Length > 0 && visual != null)
        {
            animationTime += Time.deltaTime;
            int index = Mathf.FloorToInt(animationTime * 16f);
            visual.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
            visual.color = Time.time < flashUntil ? new Color(1.7f, 1.7f, 1.7f) : Color.white;
        }
    }
    public float Play(string name, bool repeat = false)
    {
        CurrentMove = name; frames = assets.Frames(name); animationTime = 0; loop = repeat;
        if (visual != null && frames.Length > 0) visual.sprite = frames[0];
        return Mathf.Max(.08f, frames.Length / 16f);
    }
    public void Watch(bool watching)
    {
        if (fighting || defeated || IsBusy || CurrentMove == (watching ? "look" : "idle")) return;
        Play(watching ? "look" : "idle");
    }
    public IEnumerator EnterBattle()
    {
        IsBusy = true;
        Encounter.Sound(assets.stand);
        yield return new WaitForSeconds(Play("stand"));
        yield return new WaitForSeconds(Play("leave"));
        fighting = true; Hide(); IsBusy = false;
    }
    private void Show(Vector2 point, int direction)
    {
        transform.position = new Vector3(point.x, point.y, seat.z);
        // All original Mantis Lord combat sheets face left.
        transform.localScale = new Vector3(Mathf.Abs(seatScale.x) * (direction > 0 ? -1 : 1), seatScale.y, seatScale.z);
        visual.enabled = true; hurtbox.enabled = true;
    }
    private void Hide() { visual.enabled = false; hurtbox.enabled = false; }
    private float Tempo => Mathf.Lerp(.78f, 1f, CurrentHealth / (float)startingHealth);
    public IEnumerator Dash(bool fromLeft)
    {
        if (defeated || !fighting) yield break;
        IsBusy = true;
        int direction = fromLeft ? 1 : -1;
        float start = fromLeft ? Encounter.Left + .9f : Encounter.Right - .9f;
        float finish = fromLeft ? Encounter.Right - .9f : Encounter.Left + .9f;
        Show(new Vector2(start, Encounter.Floor), direction);
        Encounter.Sound(assets.land);
        yield return new WaitForSeconds(Play("dash arrive") * Tempo);
        yield return new WaitForSeconds(Play("dash start") * Tempo);
        Play("dash", true); Encounter.Sound(assets.dash);
        while (Mathf.Abs(transform.position.x - finish) > .02f && !defeated)
        {
            Vector2 before = transform.position;
            float next = Mathf.MoveTowards(before.x, finish, 35f / Tempo * Time.deltaTime);
            transform.position = new Vector3(next, before.y, seat.z);
            // Only the lance is dangerous; the body does not cause contact damage.
            DamageBox(new Vector2((before.x + next) * .5f + direction * 1.6f, Encounter.Floor + .85f),
                new Vector2(3.1f + Mathf.Abs(next - before.x), .65f));
            yield return null;
        }
        yield return new WaitForSeconds(Play("dash end") * Tempo + .15f);
        yield return Leave("dash leave");
    }
    public IEnumerator Drop()
    {
        if (defeated || !fighting) yield break;
        IsBusy = true;
        float x = Mathf.Clamp(Encounter.Player.transform.position.x, Encounter.Left + 1.4f, Encounter.Right - 1.4f);
        int direction = Random.value > .5f ? 1 : -1;
        Show(new Vector2(x, Encounter.Floor + 8f), direction);
        Encounter.Sound(assets.vanish);
        Play("dstab arrive");
        // Position locks after this readable anticipation; no homing during the stab.
        yield return new WaitForSeconds(.55f * Tempo);
        Play("dstab", true); Encounter.Sound(assets.drop);
        while (transform.position.y > Encounter.Floor && !defeated)
        {
            Vector2 before = transform.position;
            float next = Mathf.MoveTowards(before.y, Encounter.Floor, 40f / Tempo * Time.deltaTime);
            transform.position = new Vector3(before.x, next, seat.z);
            DamageBox(new Vector2(before.x, (before.y + next) * .5f + .65f), new Vector2(1.1f, 1.6f + Mathf.Abs(next - before.y)));
            yield return null;
        }
        Encounter.Sound(assets.impact); OptionsRuntime.Shake();
        Play("dstab land");
        yield return new WaitForSeconds(.5f * Tempo);
        yield return Leave("dstab leave");
    }
    public IEnumerator Throw(bool onLeft, bool high, MantisBlade.Pattern pattern, bool punishWall = false)
    {
        if (defeated || !fighting) yield break;
        IsBusy = true;
        int direction = onLeft ? 1 : -1;
        float x = onLeft ? Encounter.Left + .25f : Encounter.Right - .25f;
        float height = punishWall ? Mathf.Clamp(Encounter.Player.transform.position.y - Encounter.Floor, 2f, 9f) : high ? 5.3f : 2.3f;
        Show(new Vector2(x, Encounter.Floor + height), direction);
        // Wall artwork puts the pivot on the cage, unlike the standing hurtbox.
        hurtbox.offset = new Vector2(-direction * .65f, 0);
        Encounter.Sound(assets.wall);
        yield return new WaitForSeconds(Play("wall arrive") * Tempo);
        Play("wall ready", true);
        yield return new WaitForSeconds(.3f * Tempo);
        Play("throw");
        yield return new WaitForSeconds(.12f);
        MantisBlade.Launch(this, new Vector2(x + direction * .75f, Encounter.Floor + height), direction, high, pattern);
        Play("wall ready", true);
        yield return new WaitForSeconds(pattern == MantisBlade.Pattern.ShortPair ? 2.9f : 2.4f);
        yield return Leave("wall leave1");
        hurtbox.offset = new Vector2(0, 1.8f);
    }
    private IEnumerator Leave(string pose)
    {
        Encounter.Sound(assets.vanish);
        yield return new WaitForSeconds(Play(pose));
        Hide(); hurtbox.offset = new Vector2(0, 1.8f); IsBusy = false;
    }
    private void DamageBox(Vector2 center, Vector2 size)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        hits.Clear(); gameObject.scene.GetPhysicsScene2D().OverlapBox(center, size, 0, filter, hits);
        foreach (var hit in hits)
            if (hit.GetComponentInParent<PlayerController>() is PlayerController player && player == Encounter.Player)
            { player.ReceiveEnemyHit(center); return; }
    }
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !CanBeHit) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage); flashUntil = Time.time + .09f;
        if (CurrentHealth > 0) return;
        defeated = true; fighting = false; hurtbox.enabled = false;
        StopAllCoroutines(); IsBusy = true;
        StartCoroutine(ReturnToSeat());
    }
    private IEnumerator ReturnToSeat()
    {
        Encounter.Sound(assets.death);
        yield return new WaitForSeconds(Play("wounded"));
        yield return new WaitForSeconds(Play("death leave"));
        transform.position = seat; transform.localScale = seatScale;
        visual.enabled = true; Play("idle"); IsBusy = false;
        Encounter.LordDefeated(this);
    }
    public void ResetToSeat()
    {
        StopAllCoroutines(); fighting = defeated = IsBusy = false; CurrentHealth = startingHealth;
        transform.position = seat; transform.localScale = seatScale;
        if (hurtbox != null) { hurtbox.enabled = false; hurtbox.offset = new Vector2(0, 1.8f); }
        if (visual != null) { visual.enabled = true; visual.color = Color.white; }
        Play("idle");
    }
    public void Bow() { ResetToSeat(); defeated = true; Play("bow"); }
}
