using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Observes the real scene and normal Start coroutine; never creates a player/bench fixture.</summary>
[InitializeOnLoad]
public static class KnightBenchSceneValidation
{
    private const string Request = "Temp/ValidateKnightBenchScene.request";
    private const string Report = "Temp/KnightBenchSceneValidation.txt";
    private const string SessionPrefix = "KnightBenchSceneValidation.";
    private const string PreviewDirectory = "DirtmouthPreviews/BenchScene";
    private enum Phase { WaitingForSleep, Sleeping, Waking, Released }
    private static readonly List<string> report = new();
    private static bool running, capturedWake, capturedLanding, originalRunInBackground, restoredPauseRequired;
    private static double started, phaseStarted;
    private static float sleepTime;
    private static Phase phase;
    private static PlayerController player;
    private static Rigidbody2D body;
    private static Animator animator;
    private static SpriteRenderer knight;
    private static SpriteRenderer bench;
    private static Camera camera;
    private static DirtmouthTown town;

    static KnightBenchSceneValidation()
    {
        EditorApplication.update += Check;
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    private static void PlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && File.Exists(Request)) Begin();
        else if (change == PlayModeStateChange.ExitingPlayMode && running)
            Finish(false, "Play Mode ended before the real entry sequence completed.");
    }

    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Read-only fingerprints survive the domain reload on entering Play Mode.
                SessionState.SetString(SessionPrefix + "Saves", SaveFingerprint());
                SessionState.SetBool(SessionPrefix + "EnteredPlay", true);
                EditorApplication.EnterPlaymode();
            }
            return;
        }
        if (!running && File.Exists(Request)) Begin();
        if (!running) return;
        try
        {
            KeepPlayerLoopRunning();
            Require(EditorApplication.timeSinceStartup - started < 20,
                "Timed out observing the real Sleep -> Wake -> Idle sequence. Last phase: " + phase + StateDescription());
            Observe();
        }
        catch (Exception error)
        {
            Finish(false, error.ToString());
        }
    }

    private static void Begin()
    {
        if (running) return;
        File.Delete(Request);
        if (!SessionState.GetBool(SessionPrefix + "EnteredPlay", false))
            SessionState.SetString(SessionPrefix + "Saves", SaveFingerprint());
        report.Clear();
        report.Add("Real Dirtmouth scene entry: " + DateTime.Now.ToString("O"));
        report.Add("Observes the normal PlayerController.Start coroutine without invoking it, changing time, injecting input, reloading a scene or creating fixtures.");
        originalRunInBackground = Application.runInBackground;
        restoredPauseRequired = false;
        phase = Phase.WaitingForSleep;
        running = true;
        capturedWake = false;
        capturedLanding = false;
        player = null; body = null; animator = null; knight = null; bench = null; camera = null; town = null;
        started = phaseStarted = EditorApplication.timeSinceStartup;
        KeepPlayerLoopRunning();
    }

    private static void KeepPlayerLoopRunning()
    {
        // The editor may be behind another window. This is scoped to the observer and
        // deliberately does not change GameOptions or the persisted background setting.
        // Reapply because the normal options Start can run after Begin and reset it.
        Application.runInBackground = true;
        if (SessionState.GetBool(SessionPrefix + "EnteredPlay", false) && EditorApplication.isPaused)
        {
            restoredPauseRequired = true;
            EditorApplication.isPaused = false;
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private static void Observe()
    {
        if (phase == Phase.WaitingForSleep)
        {
            if (player == null)
            {
                var scene = SceneManager.GetActiveScene();
                Require(scene.name.Equals("Dirtmouth", StringComparison.OrdinalIgnoreCase),
                    "Open the real Dirtmouth scene before requesting this validation; active scene is " + scene.path);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var candidate in root.GetComponentsInChildren<PlayerController>())
                        if (candidate.isActiveAndEnabled)
                        {
                            Require(player == null, "Multiple active knights in the real Dirtmouth scene.");
                            player = candidate;
                        }
                    foreach (var candidate in root.GetComponentsInChildren<DirtmouthTown>())
                        if (candidate.isActiveAndEnabled) town = candidate;
                }
                if (player == null) return;
                body = player.GetComponent<Rigidbody2D>();
                animator = player.GetComponent<Animator>();
                knight = player.GetComponent<SpriteRenderer>();
                foreach (var root in scene.GetRootGameObjects())
                foreach (var follow in root.GetComponentsInChildren<DirtmouthCamera>())
                    if (follow.target == player.transform) camera = follow.GetComponent<Camera>();
                Require(body != null && animator != null && knight != null && camera != null && town != null,
                    "The real scene needs its active knight, Town and following camera.");
                // Direct scene Play Mode uses no save slot. A test must not open or alter a user slot.
                var slot = typeof(SaveGameManager).GetField("activeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(slot != null && (int)slot.GetValue(SaveGameManager.Instance) < 0,
                    "Run this validation by entering Play Mode directly from Dirtmouth, without an active user save slot.");
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            Require(!state.IsName("BenchWake"), "The observer missed BenchSleep; a later wake alone cannot pass this test.");
            if (!state.IsName("BenchSleep")) return;
            Require(town.bench != null, "The town did not resolve its real foreground bench.");
            bench = town.bench.GetComponent<SpriteRenderer>();
            Require(bench != null && bench.enabled && bench.gameObject.activeInHierarchy && bench.sprite != null,
                "The resolved bench is not an active sprite in the real scene.");
            Require(bench.sortingLayerName == "Middle", "Entry selected the obscured generated bench instead of the Middle-layer foreground bench.");
            Require(Mathf.Abs(bench.transform.position.x - -18.51f) < .15f && Mathf.Abs(bench.transform.position.y - .91f) < .15f,
                "Entry did not select Dirtmouth's visible authored bench at (-18.51, 0.91). Actual: " + bench.transform.position);
            ValidateLocked();
            Require(Mathf.Abs(player.transform.position.x - bench.bounds.center.x) < .02f &&
                Mathf.Abs(player.transform.position.y - bench.bounds.center.y - .5f) < .08f,
                "The real knight is asleep away from the foreground bench seat: " + player.transform.position);
            ValidateVisible();
            Require(knight.sprite != null && AssetDatabase.GetAssetPath(knight.sprite).Contains("PlayerSprite/motion/sit/044.Sitting Asleep"),
                "BenchSleep is not displaying the PlayerSprite sleeping art.");
            report.Add($"PASS: observed actual BenchSleep at frame {Time.frameCount}, time {Time.time:F3}; knight={player.transform.position}, bench={bench.transform.position}, layer={bench.sortingLayerName}/{bench.sortingOrder}, sprite={bench.sprite.name}.");
            Capture("01-Sleep");
            sleepTime = Time.time;
            phase = Phase.Sleeping;
            phaseStarted = EditorApplication.timeSinceStartup;
            return;
        }
        Require(player != null && player.isActiveAndEnabled && animator != null && body != null,
            "The real knight disappeared or was disabled during scene entry.");
        var current = animator.GetCurrentAnimatorStateInfo(0);
        if (phase == Phase.Sleeping)
        {
            ValidateLocked();
            if (current.IsName("BenchSleep")) return;
            Require(current.IsName("BenchWake"), "Sleep ended without the wake animation." + StateDescription());
            ValidateVisible();
            report.Add($"PASS: normal coroutine entered BenchWake after {Time.time - sleepTime:F3}s of observed sleep; frame {Time.frameCount}.");
            phase = Phase.Waking;
            phaseStarted = EditorApplication.timeSinceStartup;
            return;
        }
        if (phase == Phase.Waking)
        {
            if (player.IsResting)
            {
                Require(current.IsName("BenchWake"), "The locked knight left the bench exit sequence unexpectedly." + StateDescription());
                ValidateLocked();
                ValidateVisible();
                if (!capturedLanding && current.IsName("BenchWake") && current.normalizedTime >= 11f/14f)
                {
                    Capture("04-Landing");capturedLanding=true;
                    var exit = (Vector3)typeof(PlayerController).GetField("benchExitPosition",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
                    Require(Vector3.Distance(player.transform.position,exit)<.025f,"The landing frames must already be planted on the floor.");
                    report.Add("PASS: the last three BenchWake frames land on the floor without replaying SoftLand; controls and physics remain locked.");
                }
                if (!capturedWake && current.IsName("BenchWake") && current.normalizedTime >= .3f)
                {
                    Capture("02-Wake");
                    capturedWake = true;
                }
                return;
            }
            Require(!player.IsResting && body.simulated, "Wake left the knight locked or physics disabled." + StateDescription());
            Require(capturedWake, "The observer missed the real wake animation; no wake capture was obtained.");
            Require(capturedLanding, "Bench exit skipped the landing animation.");
            phase = Phase.Released;
            phaseStarted = EditorApplication.timeSinceStartup;
            return;
        }
        Require(!player.IsResting && body.simulated, "The knight was locked again after waking.");
        if (EditorApplication.timeSinceStartup - phaseStarted < .15) return;
        if (!player.IsGrounded || !current.IsName("Idle"))
        {
            Require(EditorApplication.timeSinceStartup - phaseStarted < 2,
                "The knight did not settle into grounded Idle after waking." + StateDescription());
            return;
        }
        Require(Mathf.Abs(player.transform.position.x - bench.bounds.center.x) < .08f,
            "The real knight was moved away from the bench after waking: " + player.transform.position);
        ValidateVisible();
        Capture("03-Awake");
        report.Add($"PASS: real Sleep -> Wake -> grounded Idle completed at frame {Time.frameCount}, position={player.transform.position}; normal physics and control restored.");
        Finish(true, null);
    }

    private static void ValidateLocked()
    {
        Require(player.IsResting && !body.simulated && !player.CanCastSpell,
            "Bench animation must hold the real knight and disable physics/casting." + StateDescription());
    }

    private static void ValidateVisible()
    {
        Require(camera.isActiveAndEnabled && knight.enabled && knight.sprite != null,
            "The real knight or scene camera is not rendering.");
        Require((camera.cullingMask & (1 << knight.gameObject.layer)) != 0 &&
            (camera.cullingMask & (1 << bench.gameObject.layer)) != 0,
            "The actual camera excludes the knight or foreground bench.");
        var actorView = camera.WorldToViewportPoint(knight.bounds.center);
        var benchView = camera.WorldToViewportPoint(bench.bounds.center);
        Require(Inside(actorView) && Inside(benchView),
            "The actual scene camera does not frame the knight and bench: knight=" + actorView + ", bench=" + benchView);
    }

    private static bool Inside(Vector3 point) => point.z > 0 && point.x > 0 && point.x < 1 && point.y > 0 && point.y < 1;

    private static void Capture(string name)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            Directory.CreateDirectory(PreviewDirectory);
            File.WriteAllBytes(PreviewDirectory + "/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static string StateDescription() =>
        $" (frame={Time.frameCount}, time={Time.time:F3}, timeScale={Time.timeScale:F3}, paused={EditorApplication.isPaused}, background={Application.runInBackground}" +
        (player == null ? ", no active player observed)" :
        $", position={player.transform.position}, resting={player.IsResting}, simulated={(body != null && body.simulated)}, state={(animator != null ? animator.GetCurrentAnimatorStateInfo(0).shortNameHash : 0)})");

    private static void Finish(bool success, string error)
    {
        running = false;
        try
        {
            report.Add("Final runtime state:" + StateDescription());
            bool savesUnchanged = SessionState.GetString(SessionPrefix + "Saves", "") == SaveFingerprint();
            report.Add((savesUnchanged ? "PASS" : "FAIL") + ": user save files remained byte-for-byte unchanged.");
            if (!savesUnchanged) { success = false; error = "User save files changed during the observation."; }
            report.Add(success ? "PASS: real Dirtmouth scene bench-entry validation completed." : "FAIL: " + error);
            Directory.CreateDirectory("Temp");
            File.WriteAllLines(Report, report);
            if (success) Debug.Log(string.Join("\n", report));
            else Debug.LogError(string.Join("\n", report));
        }
        finally
        {
            Application.runInBackground = originalRunInBackground;
            if (restoredPauseRequired) EditorApplication.isPaused = true;
            bool exit = SessionState.GetBool(SessionPrefix + "EnteredPlay", false);
            SessionState.SetBool(SessionPrefix + "EnteredPlay", false);
            if (exit && EditorApplication.isPlaying)
                EditorApplication.delayCall += () => { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); };
        }
    }

    private static string SaveFingerprint()
    {
        var directory = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(directory)) return "no-save-directory";
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        var result = new StringBuilder();
        using (var sha = SHA256.Create())
        foreach (var file in files)
            result.Append(file.Substring(directory.Length)).Append(':').Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)))).Append('\n');
        return result.ToString();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
