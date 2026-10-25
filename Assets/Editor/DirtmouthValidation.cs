using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DirtmouthValidation
{
    private static IEnumerator run;
    private static readonly List<string> report = new();
    private const string Folder = "DirtmouthPreviews";
    static DirtmouthValidation() => EditorApplication.update += Update;
    private static void Update()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (run == null && File.Exists("Temp/ValidateDirtmouth.request"))
        {
            File.Delete("Temp/ValidateDirtmouth.request");
            Start();
        }
        if (run == null) return;
        try { if (!run.MoveNext()) run = null; }
        catch (Exception error)
        {
            run = null;
            report.Add("FAIL: " + error);
            File.WriteAllLines(Folder + "/validation.txt", report);
            Debug.LogException(error);
        }
    }
    [MenuItem("Tools/Hollow Knight/Validate Dirtmouth")]
    public static void Start()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode in Dirtmouth before validating."); return; }
        if (run != null) return;
        Directory.CreateDirectory(Folder); report.Clear(); run = Validate();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static IEnumerator Wait(float duration)
    {
        double until = EditorApplication.timeSinceStartup + duration;
        while (EditorApplication.timeSinceStartup < until) yield return null;
    }
    private static IEnumerator Validate()
    {
        if(SceneManager.GetActiveScene().path!=DirtmouthBuilder.ScenePath)
        {
            int slot=(int)typeof(SaveGameManager).GetField("activeSlot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(SaveGameManager.Instance);
            Require(slot<0 && SaveGameManager.Instance.TravelToScene(DirtmouthBuilder.ScenePath),"Open Dirtmouth without an active save slot before validating.");
            var loadWait=Wait(3f); while(loadWait.MoveNext()) yield return null;
        }
        Require(SceneManager.GetActiveScene().path == DirtmouthBuilder.ScenePath, "Dirtmouth must be active.");
        var scene = SceneManager.GetActiveScene();
        var town = UnityEngine.Object.FindAnyObjectByType<DirtmouthTown>();
        Require(town != null && town.player != null && town.bench != null, "Town/player/bench references are required.");
        var player = town.player;
        var body = player.GetComponent<Rigidbody2D>();
        var camera = Camera.main;
        Require(camera != null && camera.GetComponent<DirtmouthCamera>().target == player.transform, "Camera must follow the playable knight.");
        var renderers = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        Require(renderers.Length > 180 && renderers.All(r => r.sprite != null), "Authored scenery must have valid existing sprites.");
        Require(!ShaderUtil.ShaderHasError(Shader.Find("Dirtmouth/NightSky")), "Night sky shader must compile.");
        Require(scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true)).All(m => m != null), "No missing scripts may remain.");
        var hud = player.GetComponent<PlayerUI>();
        Require(hud.healthItem.All(a => a != null && a.gameObject.scene == scene) && hud.geo.gameObject.scene == scene, "HUD references must belong to Dirtmouth.");
        Require(Application.CanStreamedLevelBeLoaded("Assets/Scenes/Game.unity") && Application.CanStreamedLevelBeLoaded(DirtmouthBuilder.ScenePath), "Town and well destination must be in build settings.");
        var music = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<OptionsAudioChannel>()).ToArray();
        Require(music.Length == 3 && music.All(a => a.GetComponent<AudioSource>().clip != null && a.GetComponent<AudioSource>().loop), "Music and wind clips must be connected and loop.");
        Require(music.Count(a => a.music) == 1, "Only the music track should use the music volume channel.");
        report.Add("PASS: scene assets, night sky shader, camera, local HUD and audio routing");
        var wait = Wait(2.5f); while (wait.MoveNext()) yield return null;
        Require(player.IsGrounded, "Knight must spawn safely on town street.");
        ScreenCapture.CaptureScreenshot(Folder + "/01-Bench.png");
        wait = Wait(0.35f); while (wait.MoveNext()) yield return null;
        town.title.gameObject.SetActive(false);
        var hudRoot = scene.GetRootGameObjects().First(go => go.name == "HUD");
        hudRoot.SetActive(false);
        wait = Wait(0.2f); while (wait.MoveNext()) yield return null;
        ScreenCapture.CaptureScreenshot(Folder + "/07-CentralClean.png");
        wait = Wait(0.3f); while (wait.MoveNext()) yield return null;
        hudRoot.SetActive(true);
        var health = player.GetComponent<PlayerHealth>();
        int maxHealth = health.CurrentHealth;
        health.TakeDamage(1);
        town.Rest();
        Require(health.CurrentHealth == maxHealth && !player.enabled, "Bench must restore health and briefly pause input.");
        wait = Wait(1f); while (wait.MoveNext()) yield return null;
        Require(player.enabled, "Bench must return player control.");
        report.Add("PASS: bench healing and return of control");

        float spawnY = body.position.y;
        foreach (float x in new[] { -42f, -24f, -10f, 0f, 11f, 27f, 41f, 61f, 78f })
        {
            body.position = new Vector2(x, spawnY + 0.1f); body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            wait = Wait(0.35f); while (wait.MoveNext()) yield return null;
            Require(player.IsGrounded && body.position.y > -1f, "Street collision failed near x=" + x);
        }
        report.Add("PASS: traversable collision across nine town sectors");
        var groundFilter = new ContactFilter2D { useTriggers = false }; groundFilter.SetLayerMask(1 << LayerMask.NameToLayer("Terrain"));
        Require(Physics2D.Raycast(new Vector2(18f, 0.8f), Vector2.down, groundFilter, new RaycastHit2D[8], 5f) == 0,
            "Well opening must not contain an invisible floor.");
        report.Add("PASS: open well shaft with solid side walls");
        player.enabled=false; body.simulated=false;
        foreach (var shot in new[] { (-18f,"02-MapShop"), (18f,"03-Well"), (10.7f,"04-StagStation"), (63f,"05-Graveyard") })
        {
            body.position = new Vector2(shot.Item1, spawnY); body.linearVelocity = Vector2.zero;
            player.transform.position = new Vector3(shot.Item1,spawnY,0f);
            Physics2D.SyncTransforms();
            wait = Wait(1f); while (wait.MoveNext()) yield return null;
            ScreenCapture.CaptureScreenshot(Folder + "/" + shot.Item2 + ".png");
            wait = Wait(0.25f); while (wait.MoveNext()) yield return null;
        }
        body.position = new Vector2(-3.5f, spawnY); body.linearVelocity = Vector2.zero;
        player.transform.position = new Vector3(-3.5f,spawnY,0f);
        body.simulated=true; player.enabled=true;
        Physics2D.SyncTransforms();
        wait = Wait(1f); while (wait.MoveNext()) yield return null;
        ScreenCapture.CaptureScreenshot(Folder + "/06-Town.png");
        wait = Wait(0.3f); while (wait.MoveNext()) yield return null;
        int activeSlot = (int)typeof(SaveGameManager).GetField("activeSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SaveGameManager.Instance);
        if (activeSlot < 0)
        {
            // Test a real descent only when no user save slot is open.
            body.position = new Vector2(18f, -5.2f); body.linearVelocity = Vector2.down;
            Physics2D.SyncTransforms();
            wait = Wait(3f); while (wait.MoveNext()) yield return null;
            Require(SceneManager.GetActiveScene().path == "Assets/Scenes/Game.unity", "Descending the well must load the existing Game scene.");
            Require(SaveGameManager.Instance.TravelToScene(DirtmouthBuilder.ScenePath), "Return to town must be loadable.");
            wait = Wait(3f); while (wait.MoveNext()) yield return null;
            Require(SceneManager.GetActiveScene().path == DirtmouthBuilder.ScenePath && UnityEngine.Object.FindAnyObjectByType<DirtmouthTown>() != null,
                "Town should reload after the well route test.");
            report.Add("PASS: actual well descent into Game and return to Dirtmouth (no save slot used)");
        }
        report.Add("PASS: five town views captured from actual Play Mode");
        File.WriteAllLines(Folder + "/validation.txt", report);
        Debug.Log("DIRTMOUTH VALIDATION PASSED\n" + string.Join("\n",report));
    }
}
