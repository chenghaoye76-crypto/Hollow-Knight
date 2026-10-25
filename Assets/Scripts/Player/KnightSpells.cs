using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class KnightSpells : MonoBehaviour
{
    public enum Spell { ShadeSoul, DescendingDark, AbyssShriek }
    private enum State { None, Focus, Soul, Shriek, DiveCharge, DiveFall, DiveRecover }
    [Header("法术（默认全部获得）")]
    [Min(1)] public int soulCost = 33;
    [Min(.1f)] public float focusDuration = .891f;
    [Min(1)] public float diveSpeed = 38f;
    private State state;
    private PlayerController player;
    private PlayerHealth health;
    private PlayerSoul soul;
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private KnightSpellAssets assets;
    private AudioSource sound, focusSound;
    private InputAction focus, quick, aim;
    private KnightSpellVisual focusVisual, diveVisual;
    private float timer, heldTime, gravity, invulnerableUntil;
    private int paidFocus, pulses, castDirection;
    private bool pendingFocus, emitted, paused;
    private readonly HashSet<ICombatTarget> diveHits = new();
    private readonly List<(Collider2D own, Collider2D enemy)> ignoredPairs = new();
    public bool IsBusy => state != State.None;
    public bool IsFocusing => state == State.Focus;
    public bool IsInvulnerable => state == State.DiveFall || state == State.DiveRecover || Time.time < invulnerableUntil;

    private void Awake()
    {
        player = GetComponent<PlayerController>(); health = GetComponent<PlayerHealth>();
        soul = GetComponent<PlayerSoul>(); body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<BoxCollider2D>();
        gravity = body.gravityScale;
        assets = Resources.Load<KnightSpellAssets>("KnightSpells");
        focus = new InputAction("Focus / Cast", InputActionType.Button);
        quick = new InputAction("Quick Cast", InputActionType.Button);
        aim = new InputAction("Spell aim", InputActionType.Value);
        sound = Audio("Knight spells", .8f); focusSound = Audio("Knight focus", .65f);
        if (assets != null) { focusSound.clip = assets.focusSound; focusSound.loop = true; }
    }
    private AudioSource Audio(string label, float volume)
    {
        var child = new GameObject(label); child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = volume; source.spatialBlend = 0;
        child.AddComponent<OptionsAudioChannel>();
        return source;
    }
    private void OnEnable()
    {
        Rebind(); GameOptions.Changed += Rebind;
        if (health != null) health.Changed += HealthChanged;
    }
    private void Rebind()
    {
        if (focus == null) return;
        foreach (var action in new[] {focus, quick, aim})
        {
            action.Disable();
            while (action.bindings.Count > 0) action.ChangeBinding(0).Erase();
        }
        focus.AddBinding("<Keyboard>/"+GameOptions.Key(7)); focus.AddBinding("<Gamepad>/buttonEast");
        quick.AddBinding("<Keyboard>/"+GameOptions.Key(11)); quick.AddBinding("<Gamepad>/rightTrigger");
        aim.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/"+GameOptions.Key(2)).With("Negative", "<Keyboard>/"+GameOptions.Key(3));
        aim.AddBinding("<Gamepad>/leftStick/y"); aim.AddBinding("<Gamepad>/dpad/y");
        foreach (var action in new[] {focus, quick, aim}) action.Enable();
        pendingFocus = false;
    }
    private Spell AimedSpell() => aim.ReadValue<float>() > .4f ? Spell.AbyssShriek : aim.ReadValue<float>() < -.4f ? Spell.DescendingDark : Spell.ShadeSoul;
    public void ReadInput()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0) return;
        if (quick.WasPressedThisFrame()) { pendingFocus = false; TryCast(AimedSpell()); }
        if (focus.WasPressedThisFrame() && !IsBusy)
        {
            heldTime = 0; pendingFocus = true;
            if (Mathf.Abs(aim.ReadValue<float>()) > .4f) { pendingFocus = false; TryCast(AimedSpell()); }
        }
        if (pendingFocus && focus.IsPressed())
        {
            heldTime += Time.deltaTime;
            if (heldTime >= .25f) { pendingFocus = false; BeginFocus(); }
        }
        if (focus.WasReleasedThisFrame())
        {
            if (IsFocusing) Cancel();
            else if (pendingFocus) TryCast(AimedSpell());
            pendingFocus = false;
        }
    }
    public bool BeginFocus()
    {
        if (!CanStart() || !player.IsGrounded || health == null || health.CurrentHealth >= health.MaxHealth || soul.Current < soulCost) return false;
        Begin(State.Focus, "SpellFocus"); paidFocus = 0;
        focusVisual = KnightSpellVisual.Create(assets, "Focus soul aura", assets.focus, transform.position+Vector3.up*.25f,
            new Vector2(2.4f, 2.7f), 0, follow: transform, loop: true);
        if (focusSound.clip != null) focusSound.Play();
        return true;
    }
    private bool CanStart() => !IsBusy && assets != null && soul != null && player != null && player.CanCastSpell && health != null && health.CurrentHealth > 0;
    public bool TryCast(Spell spell)
    {
        if (!CanStart() || !soul.TrySpend(soulCost)) return false;
        castDirection = player.FacingDirection;
        pendingFocus = false;
        switch (spell)
        {
            case Spell.ShadeSoul: Begin(State.Soul, "SpellCast"); Play(assets.castSound); break;
            case Spell.AbyssShriek: Begin(State.Shriek, "SpellShriek"); Play(assets.shriekSound); break;
            default:
                bool lift = player.IsGrounded;
                Begin(State.DiveCharge, "SpellDiveCharge");
                body.linearVelocity = lift ? Vector2.up*6 : Vector2.zero;
                Play(assets.diveSound); break;
        }
        return true;
    }
    private void Begin(State next, string animation)
    {
        player.PrepareSpell();
        state = next; timer = 0; pulses = 0; emitted = false;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = next == State.Focus ? gravity : 0;
        player.PlaySpellPose(animation);
    }
    private void Play(AudioClip clip) { if (clip != null) sound.PlayOneShot(clip); }

    // Called by the movement controller after its ground probe; returns true while movement is locked.
    public bool TickPhysics(float dt)
    {
        if (!IsBusy) return false;
        if (!player.enabled || player.IsHurt || health.CurrentHealth <= 0) { Cancel(); return false; }
        timer += dt;
        switch (state)
        {
            case State.Focus:
                if (!focus.IsPressed() || !player.IsGrounded) { Cancel(); break; }
                body.linearVelocity = Vector2.zero;
                int due = Mathf.Min(soulCost, Mathf.FloorToInt(timer/focusDuration*soulCost));
                if (!soul.TrySpend(due-paidFocus)) { Cancel(); break; }
                paidFocus = due;
                if (timer >= focusDuration)
                {
                    health.Heal(1); Play(assets.healSound);
                    KnightSpellVisual.Create(assets, "Focus heal burst", assets.focusBurst, transform.position, new Vector2(3,3), .35f, grow: true);
                    if (health.CurrentHealth >= health.MaxHealth || soul.Current < soulCost) Cancel();
                    else { timer = 0; paidFocus = 0; }
                }
                break;
            case State.Soul:
                body.linearVelocity = Vector2.zero;
                if (!emitted && timer >= .08f)
                {
                    emitted = true;
                    KnightSoulProjectile.Launch(assets, gameObject.scene, transform.position+Vector3.right*castDirection*.8f, castDirection);
                }
                if (timer >= .32f) Cancel();
                break;
            case State.Shriek:
                body.linearVelocity = Vector2.zero;
                while (pulses < 4 && timer >= .12f+pulses*.12f)
                {
                    pulses++;
                    KnightSpellDamage.Area(gameObject.scene, (Vector2)transform.position+Vector2.up*2.1f, new Vector2(4.6f, 4.2f), 20);
                    ShriekVisuals(pulses);
                }
                if (timer >= .72f) Cancel();
                break;
            case State.DiveCharge:
                body.linearVelocityX = 0;
                if (timer >= .18f)
                {
                    state = State.DiveFall; timer = 0; diveHits.Clear(); IgnoreEnemies();
                    body.linearVelocity = Vector2.down*diveSpeed;
                    player.PlaySpellPose("SpellDive");
                    diveVisual = KnightSpellVisual.Create(assets, "Descending shadow trail", assets.tendrils, transform.position+Vector3.up*.7f,
                        new Vector2(2, 3.8f), 0, true, transform, true);
                }
                break;
            case State.DiveFall:
                body.linearVelocity = Vector2.down*diveSpeed;
                KnightSpellDamage.Area(gameObject.scene, bodyCollider.bounds.center, new Vector2(bodyCollider.bounds.size.x+1, bodyCollider.bounds.size.y+.8f), 15, diveHits);
                if ((player.IsGrounded && timer > .02f) || HasBreakableBelow()) LandDive();
                else if (timer > 2.5f) Cancel();
                break;
            case State.DiveRecover:
                body.linearVelocity = Vector2.zero;
                if (!emitted && timer >= .12f)
                {
                    emitted = true;
                    KnightSpellDamage.Area(gameObject.scene, (Vector2)transform.position+Vector2.down*.4f, new Vector2(6.5f,3), 15, breakStructures: true);
                    KnightSpellVisual.Create(assets, "Descending Dark aftershock", assets.impact, transform.position+Vector3.down*.6f,
                        new Vector2(7,2.3f), .35f, true, grow: true);
                }
                if (timer >= .35f) Cancel();
                break;
        }
        return IsBusy;
    }
    private bool HasBreakableBelow()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        var hits = new List<Collider2D>();
        gameObject.scene.GetPhysicsScene2D().OverlapBox((Vector2)bodyCollider.bounds.center+Vector2.down*(bodyCollider.bounds.extents.y+.25f),
            new Vector2(bodyCollider.bounds.size.x, .6f), 0, filter, hits);
        foreach (var hit in hits) if (hit.GetComponentInParent<SpellBreakable>() != null) return true;
        return false;
    }
    private void LandDive()
    {
        state = State.DiveRecover; timer = 0; emitted = false; invulnerableUntil = Time.time+.4f;
        body.linearVelocity = Vector2.zero;
        if (diveVisual != null) Destroy(diveVisual.gameObject);
        KnightSpellDamage.Area(gameObject.scene, (Vector2)transform.position+Vector2.down*.4f, new Vector2(6.5f,3), 30, breakStructures: true);
        KnightSpellVisual.Create(assets, "Descending Dark impact", assets.impact, transform.position+Vector3.down*.6f, new Vector2(7,2), .4f, true, grow: true);
        KnightSpellVisual.Create(assets, "Descending Dark ring", assets.rings, transform.position, new Vector2(5,3), .4f, grow: true);
        player.PlaySpellPose("SpellDiveLand"); Play(assets.impactSound); OptionsRuntime.Shake();
    }
    private void ShriekVisuals(int pulse)
    {
        if (pulse != 1) return;
        KnightSpellVisual.Create(assets, "Abyss Shriek souls", assets.shriek, transform.position+Vector3.up*2.1f,
            new Vector2(4.6f,4.2f), .6f, follow: transform);
        KnightSpellVisual.Create(assets, "Abyss Shriek base", assets.shriekBase, transform.position+Vector3.up*.2f,
            new Vector2(4.6f,1.2f), .6f, follow: transform);
    }
    private void IgnoreEnemies()
    {
        foreach (var enemy in FindObjectsByType<TiktikHealth>())
        {
            if (enemy.gameObject.scene != gameObject.scene) continue;
            foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
            {
                if (Physics2D.GetIgnoreCollision(bodyCollider, collider)) continue;
                Physics2D.IgnoreCollision(bodyCollider, collider, true); ignoredPairs.Add((bodyCollider, collider));
            }
        }
    }
    public void Cancel()
    {
        state = State.None; pendingFocus = false;
        if (body != null) body.gravityScale = gravity;
        if (focusSound != null) focusSound.Stop();
        if (focusVisual != null) Destroy(focusVisual.gameObject);
        if (diveVisual != null) Destroy(diveVisual.gameObject);
        foreach (var pair in ignoredPairs)
            if (pair.own != null && pair.enemy != null) Physics2D.IgnoreCollision(pair.own, pair.enemy, false);
        ignoredPairs.Clear();
    }
    private void HealthChanged(int before, int after) { if (after < before) Cancel(); }
    private void Update()
    {
        bool pauseNow = Time.timeScale <= 0;
        if (pauseNow == paused) return;
        paused = pauseNow;
        if (paused) { sound.Pause(); focusSound.Pause(); }
        else { sound.UnPause(); focusSound.UnPause(); }
    }
    private void OnDisable()
    {
        Cancel(); GameOptions.Changed -= Rebind;
        if (health != null) health.Changed -= HealthChanged;
        focus?.Disable(); quick?.Disable(); aim?.Disable();
    }
    private void OnDestroy() { focus?.Dispose(); quick?.Dispose(); aim?.Dispose(); }
}
