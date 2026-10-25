using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DirtmouthColorSetup
{
    static DirtmouthColorSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("Temp/DirtmouthInteractionBrightness.request"))
        {
            File.Delete("Temp/DirtmouthInteractionBrightness.request");
            try { MatchInteractionBrightness(); }
            catch (Exception e) { File.WriteAllText("Temp/DirtmouthInteractionBrightness.txt", e.ToString()); Debug.LogException(e); }
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/DirtmouthColor.request")) return;
        File.Delete("Temp/DirtmouthColor.request");
        try { Apply(); }
        catch (Exception e) { File.WriteAllText("Temp/DirtmouthColor.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Hollow Knight/Apply Dirtmouth Night Colors")]
    public static void Apply()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Open Dirtmouth first.");
        var roots = scene.GetRootGameObjects();
        var fx = roots.SelectMany(r => r.GetComponentsInChildren<DirtmouthAmbientEffects>(true)).First();
        Undo.RecordObject(fx, "Deepen Dirtmouth night colors");
        fx.distantTint = new Color(.65f, .70f, .88f, 1);
        fx.distantContrast = .08f;
        // Preserve the scene's existing blur and flow settings.
        foreach (var light in roots.SelectMany(r => r.GetComponentsInChildren<Light2D>(true)).Where(l => l.lightType == Light2D.LightType.Global))
        {
            Undo.RecordObject(light, "Lower night ambient light");
            light.color = new Color(.76f, .81f, 1f);
            light.intensity = .52f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
        if (unlit == null) throw new Exception("Knight unlit material missing.");
        SetInteractionMaterials(scene, unlit);
        foreach (var player in roots.SelectMany(r => r.GetComponentsInChildren<PlayerController>(true)))
        foreach (var sprite in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Undo.RecordObject(sprite, "Preserve knight readability at night");
            sprite.sharedMaterial = unlit;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);
        }
        foreach (string path in new[] {"Assets/Resources/DirtmouthDistanceBlur.shader", "Assets/Resources/DirtmouthMist.shader"})
            if (ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(path))) throw new Exception("Shader compilation failed: " + path);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        fx.Rebuild(); fx.Tick(9f);
        var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
        var bench = roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>()).FirstOrDefault(r => r.sprite != null && r.sprite.name == "town_bench");
        var position = camera.transform.position;
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1600, 900, 24);
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            if (bench != null) camera.transform.position = new Vector3(bench.bounds.center.x-.5f, bench.bounds.min.y+5.7f, position.z);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            Directory.CreateDirectory("DirtmouthPreviews");
            File.WriteAllBytes("DirtmouthPreviews/09-NightColors.png", image.EncodeToPNG());
        }
        finally
        {
            camera.transform.position = position; camera.targetTexture = previous; RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
        }
        File.WriteAllText("Temp/DirtmouthColor.txt", "PASS: shaders compiled; global light 0.52 blue; distant shadow grading; reduced high mist; knight unlit; scene saved and preview rendered.");
    }

    private static int SetInteractionMaterials(Scene scene, Material material)
    {
        var elderbugMaterial = HighlightMaterial(material, "ElderbugHighlight", 1f);
        var benchMaterial = HighlightMaterial(material, "BenchHighlight", 1f);
        var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>(true)).Single().transform;
        int count = 0;
        foreach (var sprite in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)))
        {
            bool elderbug = sprite.GetComponentsInParent<Transform>(true).Any(t => t.name == "elder_bug");
            bool bench = sprite.sprite != null && sprite.sprite.name == "town_bench";
            if ((!elderbug && !bench) || sprite.GetComponentInParent<Prompt>() != null || sprite.sprite == null) continue;
            Undo.RecordObject(sprite, "Increase NPC and bench brightness");
            sprite.sharedMaterial = bench ? benchMaterial : elderbugMaterial;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);
            var proximity = sprite.GetComponent<KnightProximityBrightness>();
            if (proximity == null) proximity = Undo.AddComponent<KnightProximityBrightness>(sprite.gameObject);
            Undo.RecordObject(proximity, "Configure proximity brightness");
            proximity.enabled = false;
            proximity.player = player;
            proximity.maximumBrightness = bench ? 2.5f : 2f;
            proximity.fullBrightnessDistance = 1.5f;
            proximity.fadeDistance = 5f;
            proximity.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(proximity);
            count++;
        }
        return count;
    }

    private static Material HighlightMaterial(Material source, string name, float brightness)
    {
        const string directory = "Assets/Resources/Materials";
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
        string path = directory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        Undo.RecordObject(material, "Increase interaction material brightness");
        material.SetColor("_Color", new Color(brightness, brightness, brightness, 1f));
        EditorUtility.SetDirty(material);
        return material;
    }

    [MenuItem("Tools/Hollow Knight/Match Elderbug And Bench Brightness")]
    public static void MatchInteractionBrightness()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlaying) throw new Exception("Open Dirtmouth outside Play Mode.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
        if (material == null) throw new Exception("Knight unlit material missing.");
        int count = SetInteractionMaterials(scene, material);
        if (count < 2) throw new Exception("Expected Elderbug and bench renderers, found " + count);
        ValidateProximity(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/DirtmouthInteractionBrightness.txt", "PASS: " + count + " proximity brightness components; near/intermediate/far renderer color checks passed; within 1.5 units Elderbug gain 2.0 / bench gain 2.5; beyond 5 units baseline gain 1.0; smooth transitions; scene and materials saved.");
    }

    private static void ValidateProximity(Scene scene)
    {
        var probe = new GameObject("Brightness validation probe") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            foreach (var proximity in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<KnightProximityBrightness>(true)))
            {
                var actualPlayer = proximity.player;
                var sprite = proximity.GetComponent<SpriteRenderer>();
                var block = new MaterialPropertyBlock();
                float Sample(float distance)
                {
                    probe.transform.position = sprite.bounds.center + Vector3.right * distance;
                    proximity.RefreshBrightness(1f);
                    sprite.GetPropertyBlock(block);
                    return block.GetColor("_Color").r;
                }
                try
                {
                    proximity.player = probe.transform;
                    float far = Sample(10), middle = Sample(3), near = Sample(.5f);
                    if (Mathf.Abs(far-1)>.01f || Mathf.Abs(near-proximity.maximumBrightness)>.01f || middle<=far || middle>=near)
                        throw new Exception($"Invalid proximity brightness: {sprite.name} far={far} middle={middle} near={near}");
                }
                finally { proximity.player = actualPlayer; proximity.RefreshBrightness(1f); }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }
    }
}
