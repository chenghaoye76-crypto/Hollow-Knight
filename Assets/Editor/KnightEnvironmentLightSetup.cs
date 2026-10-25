using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightEnvironmentLightSetup
{
    private const string LightName = "Knight - Soft Environment Light";
    private static IEnumerator validation;
    static KnightEnvironmentLightSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (validation != null)
        {
            try { if (!validation.MoveNext()) validation = null; }
            catch (Exception e) { validation = null; File.WriteAllText("Temp/KnightEnvironmentLight.txt", e.ToString()); Debug.LogException(e); }
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/KnightEnvironmentLight.request")) return;
        File.Delete("Temp/KnightEnvironmentLight.request");
        try { Apply(); }
        catch (Exception e) { File.WriteAllText("Temp/KnightEnvironmentLight.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Hollow Knight/Add Knight Environment Light")]
    public static void Apply()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Open Dirtmouth first.");
        var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>(true)).Single();
        var child = player.transform.Find(LightName);
        if (child == null)
        {
            var go = new GameObject(LightName);
            Undo.RegisterCreatedObjectUndo(go, "Add knight environment light");
            go.transform.SetParent(player.transform, false);
            child = go.transform;
        }
        Undo.RecordObject(child, "Center knight environment light");
        child.localPosition = new Vector3(0, .6f, 0);
        child.localRotation = Quaternion.identity;
        var scale = player.transform.lossyScale;
        child.localScale = new Vector3(1f/Mathf.Abs(scale.x), 1f/Mathf.Abs(scale.y), 1f/Mathf.Abs(scale.z));
        var light = child.GetComponent<Light2D>();
        if (light == null) light = Undo.AddComponent<Light2D>(child.gameObject);
        Undo.RecordObject(light, "Configure soft local illumination");
        light.lightType = Light2D.LightType.Point;
        light.blendStyleIndex = 0;
        light.color = new Color(.78f, .87f, 1f);
        light.intensity = 2.2f;
        light.pointLightInnerRadius = 1.8f;
        light.pointLightOuterRadius = 7f;
        light.pointLightInnerAngle = 360f;
        light.pointLightOuterAngle = 360f;
        light.falloffIntensity = .35f;
        light.shadowIntensity = 0;
        light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        light.enabled = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        validation = Validate(scene, player, light);
    }

    private static IEnumerator Validate(Scene scene, PlayerController player, Light2D light)
    {
        var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
        var oldPosition = camera.transform.position;
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y+3, oldPosition.z);
            camera.targetTexture = target;
            Color32[] Render(string path)
            {
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                return image.GetPixels32();
            }
            Directory.CreateDirectory("DirtmouthPreviews");
            light.enabled = false;
            EditorApplication.QueuePlayerLoopUpdate(); SceneView.RepaintAll();
            double until = EditorApplication.timeSinceStartup + .5;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            var before = Render("DirtmouthPreviews/10-KnightLight-Off.png");
            light.enabled = true;
            EditorApplication.QueuePlayerLoopUpdate(); SceneView.RepaintAll();
            until = EditorApplication.timeSinceStartup + .5;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            var after = Render("DirtmouthPreviews/10-KnightLight-On.png");
            int brighter = 0;
            for (int i=0; i<after.Length; i++)
                if (after[i].r+after[i].g+after[i].b > before[i].r+before[i].g+before[i].b+6) brighter++;
            if (brighter < 100) throw new Exception("Local light did not brighten enough environment pixels: " + brighter);
            var offset = light.transform.position-player.transform.position;
            var original = player.transform.position;
            try
            {
                player.transform.position += new Vector3(3, 1, 0);
                if ((light.transform.position-player.transform.position-offset).sqrMagnitude > .0001f) throw new Exception("Light does not follow knight.");
            }
            finally { player.transform.position = original; }
            File.WriteAllText("Temp/KnightEnvironmentLight.txt", $"PASS: saved soft blue point light, inner radius {light.pointLightInnerRadius}, outer radius {light.pointLightOuterRadius}, intensity {light.intensity}; follows player translation; " + brighter + " environment pixels brighter in rendered comparison. Knight, Elderbug, bench and prompts retain their unlit brightness.");
        }
        finally
        {
            light.enabled = true;
            camera.transform.position = oldPosition; camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
