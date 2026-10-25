using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class LightingRepair
{
    static LightingRepair() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/LightingRepair.request")) return;
        string mode = File.ReadAllText("Temp/LightingRepair.request").Trim();
        File.Delete("Temp/LightingRepair.request");
        try { Run(mode); }
        catch (Exception error) { File.WriteAllText("Temp/LightingRepair.txt", error.ToString()); Debug.LogException(error); }
    }

    private static void Run(string mode)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var sprites = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include)
            .Where(s => s.gameObject.scene == scene).ToArray();
        var lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
            .Where(l => l.gameObject.scene == scene).ToArray();
        var report = new StringBuilder();
        if (mode == "fix")
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before repairing scene lighting.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            if (material == null) throw new InvalidOperationException("URP sprite lit material is missing.");
            int changed = 0;
            Undo.SetCurrentGroupName("Enable 2D scene lighting");
            foreach (var sprite in sprites)
            {
                // UI ornaments stay unlit so interaction prompts remain legible.
                if (sprite.GetComponentInParent<Canvas>() != null || sprite.GetComponentInParent<Prompt>() != null
                    || sprite.sharedMaterial == null || sprite.sharedMaterial.shader.name != "Sprites/Default") continue;
                Undo.RecordObject(sprite, "Enable sprite lighting");
                sprite.sharedMaterial = material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);
                changed++;
            }
            if (!lights.Any(l => l.isActiveAndEnabled && l.lightType == Light2D.LightType.Global && l.blendStyleIndex == 0))
            {
                var ambientObject = new GameObject("Global Light 2D - Ambient");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ambientObject, scene);
                Undo.RegisterCreatedObjectUndo(ambientObject, "Preserve scene ambient brightness");
                var ambient = Undo.AddComponent<Light2D>(ambientObject);
                ambient.lightType = Light2D.LightType.Global;
                ambient.intensity = 1f;
                ambient.color = Color.white;
                ambient.blendStyleIndex = 0;
                ambient.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            report.AppendLine($"PASS: converted {changed} sprite materials; ambient light available; existing scene edits preserved (scene not saved).");
        }
        report.AppendLine($"Scene: {scene.path}; pipeline={GraphicsSettings.currentRenderPipeline}");
        foreach (var group in sprites.GroupBy(s => s.sharedMaterial ? s.sharedMaterial.shader.name : "null"))
            report.AppendLine($"Shader: {group.Key}; sprites={group.Count()}");
        foreach (var light in lights)
        {
            var serialized = new SerializedObject(light);
            var layers = serialized.FindProperty("m_ApplyToSortingLayers");
            report.AppendLine($"Light: {light.name}; active={light.isActiveAndEnabled}; type={light.lightType}; position={light.transform.position}; intensity={light.intensity}; blend={light.blendStyleIndex}; radius={light.pointLightOuterRadius}; angle={light.pointLightOuterAngle}; layers={string.Join(",", Enumerable.Range(0,layers.arraySize).Select(i => SortingLayer.IDToName(layers.GetArrayElementAtIndex(i).intValue)))}");
            foreach (var sprite in sprites.OrderBy(s => Vector3.Distance(s.bounds.ClosestPoint(light.transform.position), light.transform.position)).Take(8))
                report.AppendLine($"  Nearby: {sprite.name}; layer={sprite.sortingLayerName}; bounds={sprite.bounds}; shader={sprite.sharedMaterial?.shader.name}");
        }
        File.WriteAllText("Temp/LightingRepair.txt", report.ToString());
    }

    [MenuItem("Tools/Hollow Knight/Enable Scene 2D Lighting")]
    public static void FixLighting() => Run("fix");
}
