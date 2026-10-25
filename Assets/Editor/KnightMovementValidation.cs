using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Tests run in a disposable local physics scene, without loading or saving the user's scene.
[InitializeOnLoad]
public static class KnightMovementValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Request = "Temp/ValidateKnightMovement.request";
    private const string Report = "Temp/KnightMovementValidation.txt";
    static KnightMovementValidation() => EditorApplication.update += CheckRequest;
    private static void CheckRequest()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        File.Delete(Request);
        Validate();
    }
    private static void Set(PlayerController player, string field, object value) => typeof(PlayerController).GetField(field, Private).SetValue(player, value);
    private static T Get<T>(PlayerController player, string field) => (T)typeof(PlayerController).GetField(field, Private).GetValue(player);
    private static void Call(PlayerController player, string method) => typeof(PlayerController).GetMethod(method, Private).Invoke(player, null);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [MenuItem("Tools/Hollow Knight/Validate Knight Movement")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode before validating knight movement."); return; }
        var results = new List<string>();
        float originalTimeScale = Time.timeScale;
        var originalActiveScene = SceneManager.GetActiveScene();
        var scene = SceneManager.CreateScene("KnightMovementValidation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        var physics = scene.GetPhysicsScene2D();
        Keyboard keyboard = null;
        PlayerController player = null;
        try
        {
            GameObject Surface(string name, Vector2 position, Vector2 size)
            {
                var go = new GameObject(name) { layer = LayerMask.NameToLayer("Terrain"), hideFlags = HideFlags.DontSave };
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = position;
                go.AddComponent<BoxCollider2D>().size = size;
                return go;
            }
            Surface("FloorLeft", new Vector2(-2.5f, -0.5f), new Vector2(5f, 1f));
            Surface("FloorRight", new Vector2(2.5f, -0.5f), new Vector2(5f, 1f));
            Surface("WallLeft", new Vector2(-3f, 6f), new Vector2(1f, 12f));
            Surface("WallRight", new Vector2(3f, 6f), new Vector2(1f, 12f));
            var go = new GameObject("TestKnight", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator)) { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(go, scene);
            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 4.84f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            go.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            player = go.AddComponent<PlayerController>();
            if (Get<Rigidbody2D>(player, "body") == null) { Call(player, "Awake"); Call(player, "OnEnable"); }
            go.GetComponent<Animator>().Rebind();
            go.GetComponent<Animator>().Update(0f);
            void Reset(Vector2 position, Vector2 velocity)
            {
                Call(player,"CancelDashPose");
                foreach (string field in new[] { "moveInput", "jumpBuffer", "groundGrace", "wallGrace", "holdRemaining", "dashRemaining", "dashCooldownRemaining", "wallLockRemaining", "hurtRemaining", "recoilRemaining", "attackAnimationRemaining", "specialJumpRemaining", "landRemaining" }) Set(player, field, 0f);
                foreach (string field in new[] { "grounded", "wallSliding", "jumpHeld", "jumpReleased", "dashQueued" }) Set(player, field, false);
                Set(player, "remainingJumps", 1); Set(player, "remainingDashes", 1);
                player.HasMantisClaw = true;
                body.gravityScale = 4.84f;
                body.position = position;
                body.linearVelocity = velocity;
                Physics2D.SyncTransforms();
            }
            void Tick(bool simulate = false)
            {
                Call(player, "FixedUpdate");
                if (simulate) physics.Simulate(Time.fixedDeltaTime);
                go.GetComponent<Animator>().Update(Time.fixedDeltaTime);
            }
            Reset(new Vector2(-0.2f, 0.81f), Vector2.zero);
            Tick(); Require(player.IsGrounded && player.CanSave, "Standing on floor should be grounded and safe to save.");
            body.position = new Vector2(0.2f, 0.81f); Physics2D.SyncTransforms(); Tick();
            Require(player.IsGrounded, "Crossing a collider seam must not lose grounding.");
            results.Add("PASS: floor detection and adjacent collider seam");

            foreach (int side in new[] { -1, 1 })
            {
                Reset(new Vector2(2.09f * side, 5f), new Vector2(0f, -8f));
                Set(player, "moveInput", (float)side); Tick();
                Require(player.IsWallSliding && body.linearVelocity.y >= -2.501f, "Wall slide failed on side " + side);
                Require(!player.IsGrounded, "Wall must not count as floor.");
                Require(go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Slide"), "Slide animation missing.");
                Set(player, "jumpBuffer", 0.12f); Set(player, "jumpHeld", true); Tick(true);
                Require(body.linearVelocity.x * side < -8f && body.linearVelocity.y > 10f, "Wall jump must launch away and upward.");
                Require(!player.IsWallSliding, "Wall jump must release wall.");
                Require(go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("SlideJump"), "Wall jump animation missing.");
                Require(go.GetComponent<SpriteRenderer>().flipX, "Wall jump artwork must be mirrored relative to normal artwork.");
                Tick(true); Require(body.linearVelocity.x * side < -8f, "Holding toward wall must not cancel wall jump launch.");
                for (int i = 0; i < 7; i++) Tick(true);
                Require(go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("SlideJump") &&
                    Mathf.Sign(go.transform.localScale.x) == side, "Wall jump clip must retain outward facing after movement lock ends.");
                for (int i = 0; i < 5; i++) Tick(true);
                Require(!go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("SlideJump") && !go.GetComponent<SpriteRenderer>().flipX,
                    "Completed wall jump must return to normal airborne animation and artwork orientation.");
                Require(body.linearVelocity.x * side > 6f, "Horizontal control must resume after wall jump lock.");
                results.Add("PASS: " + (side < 0 ? "left" : "right") + " wall slide, wall jump, facing, animation and control recovery");

                foreach (bool alreadySliding in new[] { false, true })
                {
                    Reset(new Vector2(2.09f * side, 5f), new Vector2(0f, -8f));
                    Set(player, "moveInput", (float)side);
                    if (alreadySliding) Tick();
                    Set(player, "dashQueued", true); Tick(true);
                    Require(player.IsDashing && body.linearVelocity.x * side < -19f && Mathf.Abs(body.linearVelocity.y) < 0.01f,
                        "Wall dash must launch away from wall on both first contact and sustained slide.");
                    Require(Mathf.Sign(go.transform.localScale.x) == side && !go.GetComponent<SpriteRenderer>().flipX &&
                        go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dash"), "Wall dash artwork must face away from wall.");
                }
                results.Add("PASS: " + (side < 0 ? "left" : "right") + " outward wall dash on first contact and sustained slide");
            }
            Reset(new Vector2(2.09f, 5f), new Vector2(0f, -8f));
            player.HasMantisClaw = false; Set(player, "remainingJumps", 0); Set(player, "moveInput", 1f); Set(player, "jumpBuffer", 0.12f); Tick();
            Require(!player.IsWallSliding && body.linearVelocity.y < 0f, "Locked claw must not slide or wall jump.");
            results.Add("PASS: Mantis Claw ability toggle");

            Reset(new Vector2(2.09f, 8f), new Vector2(0f, -8f));
            Set(player, "moveInput", 1f);
            for (int i = 0; i < 30; i++) Tick(true);
            Require(player.IsWallSliding && body.position.y < 7f && body.linearVelocity.y < -1f && body.linearVelocity.y > -3.6f,
                "Sustained wall contact must descend slowly instead of sticking or free falling.");
            Set(player, "moveInput", -1f); Tick(true);
            Require(!player.IsWallSliding && body.linearVelocity.x < 0f, "Moving away must immediately release wall.");
            results.Add("PASS: sustained slide without wall friction and immediate release");

            var ceiling = Surface("Ceiling", new Vector2(0f, 7.31f), new Vector2(2f, 1f));
            Reset(new Vector2(0f, 6f), new Vector2(0f, 10f)); Set(player, "jumpHeld", true); Set(player, "holdRemaining", 0.2f); Tick();
            Require(Get<float>(player, "holdRemaining") <= 0f && body.linearVelocity.y <= 0f, "Ceiling hit must cancel jump hold.");
            UnityEngine.Object.DestroyImmediate(ceiling);
            results.Add("PASS: ceiling cancels jump hold");

            Reset(new Vector2(0f, 5f), Vector2.zero); Set(player, "groundGrace", 0.08f); Set(player, "jumpBuffer", 0.12f); Tick();
            Require(body.linearVelocity.y > 15f && Get<int>(player, "remainingJumps") == 1, "Coyote jump must preserve air jump.");
            Set(player, "jumpReleased", true); Tick(); Require(body.linearVelocity.y < 8f, "Release should shorten jump.");
            Set(player, "jumpBuffer", 0.12f); Tick(); Require(body.linearVelocity.y > 15f && Get<int>(player, "remainingJumps") == 0, "Air jump count incorrect.");
            Set(player, "jumpBuffer", 0.12f); body.linearVelocityY = -2f; Tick(); Require(body.linearVelocity.y < 0f, "No extra air jumps should be allowed.");
            results.Add("PASS: coyote jump, variable height, double jump limit");

            Reset(new Vector2(0f, 0.81f), new Vector2(0f, -2f)); Set(player, "remainingJumps", 0); Set(player, "jumpBuffer", 0.08f); Tick();
            Require(body.linearVelocity.y > 15f && !player.IsGrounded, "Buffered landing jump must launch.");
            results.Add("PASS: buffered jump on landing");

            Reset(new Vector2(0f, 5f), Vector2.zero); Set(player, "dashQueued", true); Tick();
            Require(player.IsDashing && body.gravityScale == 0f, "Dash must disable gravity.");
            for (int i = 0; i < 40; i++) Tick();
            Require(!player.IsDashing && body.gravityScale == 4.84f, "Dash must restore gravity.");
            Set(player, "dashQueued", true); Tick(); Require(!player.IsDashing, "Cooldown alone must not refill an air dash.");
            player.DownHitRepel(); Set(player, "dashQueued", true); Tick(); Require(player.IsDashing, "Successful pogo must refill air dash.");
            Call(player, "TakeDamage"); Require(!player.IsDashing && body.gravityScale == 4.84f, "Damage must cancel dash and restore gravity.");
            results.Add("PASS: dash lifetime, gravity restore, air charge, pogo refresh and damage interrupt");

            Reset(new Vector2(0f, 5f), Vector2.zero); player.HitRepel();
            for (int i = 0; i < 15; i++) Tick();
            Require(Get<float>(player, "recoilRemaining") <= 0f, "Hit recoil must expire rather than restarting every tick.");
            results.Add("PASS: recoil duration expires");

            Reset(new Vector2(0f, 0.81f), Vector2.zero);
            Time.timeScale = 1f; // The earlier damage test can invoke the live TimeManager's hit-stop.
            keyboard = InputSystem.AddDevice<Keyboard>();
            var key = (Key)Enum.Parse(typeof(Key), GameOptions.Key(4), true);
            Get<InputAction>(player, "jumpAction").Reset(); // A physical jump key may already be held while this test starts.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            Call(player, "Update"); Tick();
            Require(body.linearVelocity.y > 15f, "Configured keyboard jump must drive the actual controller.");
            results.Add("PASS: actual rebound keyboard input");

            var enemy = Surface("InvincibilityTarget", new Vector2(10f, 5f), Vector2.one);
            enemy.hideFlags = HideFlags.None; // FindObjectsByType excludes DontSave objects.
            enemy.layer = LayerMask.NameToLayer("Enemy");
            var inv = go.AddComponent<Invincibility>();
            inv.render = null; // 无渲染器也必须能正确清理无敌状态。
            bool layerWasIgnored = Physics2D.GetIgnoreLayerCollision(go.layer, enemy.layer);
            inv.Activate(0.2f);
            Require(inv.isInvincible && Physics2D.GetIgnoreCollision(go.GetComponent<BoxCollider2D>(), enemy.GetComponent<BoxCollider2D>()), "Invulnerability must ignore only this player's collision pair.");
            Require(Physics2D.GetIgnoreLayerCollision(go.layer, enemy.layer) == layerWasIgnored, "Invulnerability must not change global layer collisions.");
            inv.enabled = false;
            Require(!inv.isInvincible && !Physics2D.GetIgnoreCollision(go.GetComponent<BoxCollider2D>(), enemy.GetComponent<BoxCollider2D>()), "Disabling invulnerability must restore collision pairs even without renderer.");
            results.Add("PASS: invulnerability cleanup and isolated collision pairs");

            int deathCount = 0;
            player.HandleDeath(() => deathCount++);
            player.OnDeathAnimationEnd(); player.OnDeathAnimationEnd();
            Require(deathCount == 1 && !player.enabled && body.bodyType == RigidbodyType2D.Static && !go.GetComponent<BoxCollider2D>().enabled,
                "Death must stop motion and invoke its callback only once.");
            results.Add("PASS: death stops input/physics and callback runs once");
            File.WriteAllLines(Report, results);
            Debug.Log("KNIGHT MOVEMENT VALIDATION PASSED\n" + string.Join("\n", results));
        }
        catch (Exception error)
        {
            File.WriteAllText(Report, string.Join("\n", results) + "\nFAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (player != null) UnityEngine.Object.DestroyImmediate(player.gameObject);
            if (originalActiveScene.IsValid()) SceneManager.SetActiveScene(originalActiveScene);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
