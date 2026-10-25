using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightBenchSelectionValidation
{
    static KnightBenchSelectionValidation() => EditorApplication.update += Check;

    private static void Check()
    {
        const string request = "Temp/ValidateKnightBenchSelection.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        File.Delete(request);
        try { File.WriteAllText("Temp/KnightBenchSelectionValidation.txt", Run()); }
        catch (Exception error)
        {
            File.WriteAllText("Temp/KnightBenchSelectionValidation.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    public static string Run()
    {
        var previous = SceneManager.GetActiveScene();
        Scene scene = default, otherScene = default;
        int index = 0;
        Scene NewScene() => EditorApplication.isPlaying ? SceneManager.CreateScene("Bench selection validation " + ++index) :
            EditorSceneManager.NewPreviewScene();
        try
        {
            scene = NewScene();
            otherScene = NewScene();
            var sprite = Resources.Load<Sprite>("Sprites/Architecture & Environment/Area specfic architecture/Dirthmouth/town_bench");
            Require(sprite != null, "Need the actual town_bench sprite.");
            SpriteRenderer Bench(string name, Scene owner, string layer, int order)
            {
                var go = new GameObject(name);
                SceneManager.MoveGameObjectToScene(go, owner);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.sortingLayerName = layer; renderer.sortingOrder = order;
                return renderer;
            }
            var stale = Bench("Old generated Rest bench", scene, "Default", 100);
            var visible = Bench("Foreground authored bench", scene, "Middle", -10);
            Require(KnightBench.Find(scene, stale.transform) == visible.transform,
                "A stale town.bench reference must not beat a bench on a foreground layer.");
            var disabled = Bench("Disabled foreground bench", scene, "Front", 100); disabled.enabled = false;
            var inactive = Bench("Inactive foreground bench", scene, "Front", 101); inactive.gameObject.SetActive(false);
            var transparent = Bench("Invisible foreground bench", scene, "Front", 102); transparent.color = Color.clear;
            Bench("Other scene foreground bench", otherScene, "Front", 1000);
            Require(KnightBench.Find(scene, stale.transform) == visible.transform,
                "Hidden benches and benches from other scenes must be ignored.");
            var ordered = Bench("Highest sorting order bench", scene, "Middle", -9);
            Require(KnightBench.Find(scene, stale.transform) == ordered.transform,
                "Sorting order must break a sorting-layer tie.");
            ordered.enabled = false;
            var townObject = new GameObject("Town selection fixture");
            SceneManager.MoveGameObjectToScene(townObject, scene);
            var town = townObject.AddComponent<DirtmouthTown>();
            town.enabled = false;
            town.bench = stale.transform;
            typeof(DirtmouthTown).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(town, null);
            Require(town.bench == visible.transform,
                "Town rest interaction must resolve the same visible bench as scene entry.");
            return "PASS: foreground layer overrides stale town.bench; same-layer order, disabled/inactive/transparent benches and scene isolation verified.\n" +
                "This is a bench-selection fixture test; actual scene lifecycle and first-frame rendering are validated separately.";
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            foreach (var temporary in new[] { scene, otherScene })
            {
                if (!temporary.IsValid() || !temporary.isLoaded) continue;
                if (EditorApplication.isPlaying)
                {
                    foreach (var root in temporary.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
                    SceneManager.UnloadSceneAsync(temporary);
                }
                else EditorSceneManager.ClosePreviewScene(temporary);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
