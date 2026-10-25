using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class MantisLordsEncounter : MonoBehaviour
{
    public enum BattleState { Waiting, Preparing, FirstPhase, Intermission, SecondPhase, Victory }
    public MantisLordAssets assets;
    public MantisLord[] lords;
    public Transform room;
    public Prompt challengePrompt;
    public TMP_Text title;
    [Header("Arena coordinates relative to room")]
    public float left = -12.2f, right = 12.2f, floor = -5.6f;
    public float challengeHalfWidth = 3.2f;
    public UnityEvent onVictory = new();
    public BattleState State { get; private set; }
    public PlayerController Player { get; private set; }
    public bool IsFighting => State == BattleState.FirstPhase || State == BattleState.SecondPhase;
    public float Left => room.position.x + left;
    public float Right => room.position.x + right;
    public float Floor => room.position.y + floor;
    private Transform[] gates;
    private Vector3[] gateOpen;
    private Transform[] spikeFloors;
    private Vector3[] spikesHidden;
    private Collider2D[] spikes;
    private ParticleSystem smoke;
    private AudioSource sound, music;
    private InputAction challenge;
    private float safeX, gateWarningUntil;
    private bool initialized;

    private void Start()
    {
        room ??= transform.parent != null ? transform.parent : transform;
        Player = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(p => p.gameObject.scene == gameObject.scene);
        if (assets == null || lords == null || lords.Length != 3 || lords.Any(l => l == null))
        { Debug.LogError("Mantis Lords requires its assets and three sisters.", this); enabled = false; return; }
        for (int i = 0; i < lords.Length; i++) lords[i].Initialize(this, i == 0 ? 210 : 160);
        var transforms = room.GetComponentsInChildren<Transform>(true);
        gates = transforms.Where(t => t.name == "Door_Left" || t.name == "Door_Right").ToArray();
        gateOpen = gates.Select(t => t.localPosition).ToArray();
        spikeFloors = transforms.Where(t => t.name == "spikeFloor_Left" || t.name == "spikeFloor_Right").ToArray();
        spikesHidden = spikeFloors.Select(t => t.localPosition).ToArray();
        spikes = spikeFloors.SelectMany(t => t.GetComponentsInChildren<Collider2D>(true)).Where(c => c.name == "Spike").ToArray();
        foreach (var c in spikes) { c.enabled = false; c.isTrigger = true; }
        smoke = transforms.FirstOrDefault(t => t.name == "GroundStartSmoke" && !t.IsChildOf(transform))?.GetComponent<ParticleSystem>();
        if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        // This old unconfigured entrance collider must not block the challenge zone.
        foreach (var c in GetComponentsInChildren<Collider2D>()) if (c.name == "EnterColl") c.enabled = false;
        sound = Source("Boss effects", false, .85f); music = Source("Boss music", true, .65f);
        challenge = new InputAction("Challenge Mantis Lords", InputActionType.Button);
        Rebind(); GameOptions.Changed += Rebind;
        if (challengePrompt != null) challengePrompt.HideImmediate();
        if (title != null) title.text = "";
        initialized = true;
        if (SaveGameManager.Instance.MantisLordsDefeated)
        {
            State = BattleState.Victory;
            foreach (var lord in lords) lord.Bow();
            OpenPassage();
        }
    }
    private AudioSource Source(string label, bool isMusic, float volume)
    {
        var go = new GameObject(label); go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = volume;
        go.AddComponent<OptionsAudioChannel>().music = isMusic;
        go.GetComponent<OptionsAudioChannel>().Apply();
        return source;
    }
    private void Rebind()
    {
        challenge.Disable();
        while (challenge.bindings.Count > 0) challenge.ChangeBinding(0).Erase();
        challenge.AddBinding("<Keyboard>/" + GameOptions.Key(2));
        challenge.AddBinding("<Gamepad>/dpad/up"); challenge.AddBinding("<Gamepad>/leftStick/up");
        challenge.Enable();
    }
    public void Sound(AudioClip clip) { if (sound != null && clip != null) sound.PlayOneShot(clip); }
    private void Update()
    {
        if (!initialized || Player == null || Time.timeScale <= 0) return;
        var health = Player.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHealth <= 0 && State != BattleState.Waiting && State != BattleState.Victory)
        { Abort(); return; }
        if (State == BattleState.Waiting)
        {
            bool near = Player.enabled && !Player.IsResting && Player.IsGrounded &&
                Mathf.Abs(Player.transform.position.x - room.position.x) < challengeHalfWidth;
            foreach (var lord in lords) lord.Watch(near);
            if (challengePrompt != null)
            {
                challengePrompt.SetVisible(near);
            }
            if (near && challenge.WasPressedThisFrame()) BeginChallenge();
            // Approaching Deepnest before victory prompts the left sister's refusal.
            if (Player.transform.position.x < Left - 5 && Time.time > gateWarningUntil)
            { gateWarningUntil = Time.time + 4; lords[1].Play("gesture"); Sound(assets.gate); }
        }
        if (IsFighting)
        {
            float x = Player.transform.position.x;
            if (Mathf.Abs(x - room.position.x) < 7.7f && Player.IsGrounded) safeX = x;
            bool pit = Mathf.Abs(x - room.position.x) > 8.5f && Mathf.Abs(x - room.position.x) < 12.1f && Player.transform.position.y < Floor + .45f;
            if (pit || Player.transform.position.y < Floor - 5)
            {
                if (Player.ReceiveEnemyHit(new Vector2(x, Floor - 1)))
                {
                    var body = Player.GetComponent<Rigidbody2D>();
                    body.position = new Vector2(Mathf.Clamp(safeX, room.position.x - 7, room.position.x + 7), Floor + 1.2f);
                    body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                }
            }
        }
    }
    public bool BeginChallenge()
    {
        if (!initialized || State != BattleState.Waiting || Player == null || !Player.enabled || Player.IsResting || !Player.IsGrounded) return false;
        State = BattleState.Preparing;
        SaveGameManager.Instance.SaveCurrentGame();
        Player.SuppressPositionSaving = true; safeX = Player.transform.position.x;
        if (challengePrompt != null) challengePrompt.HideImmediate();
        StartCoroutine(Prepare()); return true;
    }
    private IEnumerator Prepare()
    {
        if (title != null) title.text = "螳螂领主";
        Sound(assets.gate);
        foreach (var gate in gates)
        foreach (var c in gate.GetComponentsInChildren<Collider2D>()) { c.enabled = true; c.gameObject.layer = LayerMask.NameToLayer("Terrain"); }
        float elapsed = 0;
        while (elapsed < 1.2f)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < gates.Length; i++) gates[i].localPosition = Vector3.Lerp(gateOpen[i], gateOpen[i] - Vector3.up * 20, Mathf.SmoothStep(0, 1, elapsed / 1.2f));
            yield return null;
        }
        Sound(assets.spikes);
        for (int i = 0; i < spikeFloors.Length; i++) spikeFloors[i].localPosition = spikesHidden[i] + Vector3.up * .55f;
        smoke?.Play();
        music.clip = assets.music; music.loop = true; music.Play();
        yield return lords[0].StartCoroutine(lords[0].EnterBattle());
        State = BattleState.FirstPhase;
        StartCoroutine(Solo(lords[0]));
        yield return new WaitForSeconds(2f);
        if (title != null) title.text = "";
    }
    private bool AtEdge => Player.IsWallSliding || Player.transform.position.x < Left + 1.4f ||
        Player.transform.position.x > Right - 1.4f || Player.transform.position.y > Floor + 9;
    private IEnumerator Solo(MantisLord lord)
    {
        int previous = -1;
        while (IsFighting && !lord.IsDefeated)
        {
            int choice = Random.Range(0, 3);
            if (choice == previous) choice = (choice + 1) % 3;
            previous = choice;
            IEnumerator attack;
            if (AtEdge) attack = lord.Throw(Player.transform.position.x > room.position.x, false, MantisBlade.Pattern.WallPunish, true);
            else if (choice == 0) attack = lord.Dash(Player.transform.position.x > room.position.x);
            else if (choice == 1) attack = lord.Drop();
            else attack = lord.Throw(Random.value > .5f, Random.value > .5f, MantisBlade.Pattern.Single);
            lord.StartCoroutine(attack);
            yield return new WaitUntil(() => !lord.IsBusy || lord.IsDefeated);
            yield return new WaitForSeconds(Mathf.Lerp(.16f, .45f, lord.CurrentHealth / (float)lord.startingHealth));
        }
    }
    private IEnumerator Pair()
    {
        int round = 0;
        while (State == BattleState.SecondPhase)
        {
            var alive = lords.Skip(1).Where(l => !l.IsDefeated).ToArray();
            if (alive.Length == 0) yield break;
            if (alive.Length == 1) { yield return Solo(alive[0]); yield break; }
            if (AtEdge)
            {
                alive[0].StartCoroutine(alive[0].Throw(Player.transform.position.x > room.position.x, false, MantisBlade.Pattern.WallPunish, true));
                yield return new WaitUntil(() => !alive[0].IsBusy || alive[0].IsDefeated);
            }
            else if (round % 3 == 2)
            {
                var pattern = Random.value > .5f ? MantisBlade.Pattern.WidePair : MantisBlade.Pattern.ShortPair;
                alive[0].StartCoroutine(alive[0].Throw(true, true, pattern));
                alive[1].StartCoroutine(alive[1].Throw(false, true, pattern));
                yield return new WaitUntil(() => alive.All(l => !l.IsBusy || l.IsDefeated));
            }
            else if (round % 3 == 0)
            {
                alive[0].StartCoroutine(alive[0].Dash(true));
                alive[1].StartCoroutine(alive[1].Dash(false));
                yield return new WaitUntil(() => alive.All(l => !l.IsBusy || l.IsDefeated));
            }
            else
            {
                alive[0].StartCoroutine(alive[0].Dash(Player.transform.position.x > room.position.x));
                yield return new WaitForSeconds(.42f);
                if (!alive[1].IsDefeated) alive[1].StartCoroutine(alive[1].Drop());
                yield return new WaitUntil(() => alive.All(l => !l.IsBusy || l.IsDefeated));
            }
            round++; yield return new WaitForSeconds(.2f);
        }
    }
    public void LordDefeated(MantisLord lord)
    {
        if (lord == lords[0] && State == BattleState.FirstPhase)
        {
            State = BattleState.Intermission;
            ClearBlades(); StartCoroutine(SecondPhase());
        }
        else if (State == BattleState.SecondPhase && lords.Skip(1).All(l => l.IsDefeated)) StartCoroutine(Win());
    }
    private IEnumerator SecondPhase()
    {
        yield return new WaitForSeconds(.7f);
        lords[1].StartCoroutine(lords[1].EnterBattle());
        lords[2].StartCoroutine(lords[2].EnterBattle());
        yield return new WaitUntil(() => !lords[1].IsBusy && !lords[2].IsBusy);
        State = BattleState.SecondPhase; StartCoroutine(Pair());
    }
    private IEnumerator Win()
    {
        State = BattleState.Victory; ClearBlades(); music.Stop(); Sound(assets.victory);
        foreach (var lord in lords) lord.Bow();
        yield return new WaitForSeconds(1f);
        RestoreArena(); OpenPassage();
        Player.SuppressPositionSaving = false;
        SaveGameManager.Instance.CompleteMantisLords();
        if (title != null) title.text = "尊重";
        onVictory.Invoke();
        yield return new WaitForSeconds(3);
        if (title != null) title.text = "";
    }
    private void OpenPassage()
    {
        foreach (var t in room.GetComponentsInChildren<Transform>(true))
            if (t.name == "MantisDoor" || t.name == "DoorColl")
            {
                foreach (var c in t.GetComponentsInChildren<Collider2D>()) c.enabled = false;
                var animator = t.GetComponent<Animator>();
                if (animator != null) animator.SetTrigger("hide");
            }
    }
    private void ClearBlades() { foreach (var blade in GetComponentsInChildren<MantisBlade>()) Destroy(blade.gameObject); }
    private void RestoreArena()
    {
        for (int i = 0; i < gates.Length; i++) gates[i].localPosition = gateOpen[i];
        for (int i = 0; i < spikeFloors.Length; i++) spikeFloors[i].localPosition = spikesHidden[i];
        foreach (var c in spikes) c.enabled = false;
        if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    public void Abort()
    {
        StopAllCoroutines(); ClearBlades(); music?.Stop(); RestoreArena();
        foreach (var lord in lords) lord.ResetToSeat();
        State = BattleState.Waiting;
        if (Player != null) Player.SuppressPositionSaving = false;
        if (title != null) title.text = "";
        if (challengePrompt != null) challengePrompt.HideImmediate();
    }
    private void OnDisable()
    {
        GameOptions.Changed -= Rebind; challenge?.Disable();
        if (initialized && State != BattleState.Victory) Abort();
    }
    private void OnDestroy() { challenge?.Dispose(); if (Player != null) Player.SuppressPositionSaving = false; }
}
