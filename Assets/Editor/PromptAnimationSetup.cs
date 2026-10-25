using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PromptAnimationSetup
{
    private const string Folder = "Assets/Animation/Prompt/";
    static PromptAnimationSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupPromptAnimation.request")) return;
        File.Delete("Temp/SetupPromptAnimation.request");
        try { Setup(); File.WriteAllText("Temp/PromptAnimationSetup.txt", "PASS: Up / Down / Idle sprite sequences, text fades, controller and prefab configured."); }
        catch (Exception e) { File.WriteAllText("Temp/PromptAnimationSetup.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Hollow Knight/Set Up Prompt Animations")]
    public static void Setup()
    {
        var up = BuildClip("Up", "015.Up", 24f, false);
        var down = BuildClip("Down", "016.Down", 24f, false);
        var idle = BuildClip("Idle", "017.Idle", 10f, true);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "Prompt.controller");
        if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
        var layer = controller.layers[0];
        layer.defaultWeight = 1f;
        var machine = layer.stateMachine;
        foreach (var child in machine.states) machine.RemoveState(child.state);
        var upState = machine.AddState("Up", new Vector3(250, 40)); upState.motion = up;
        var idleState = machine.AddState("Idle", new Vector3(500, 40)); idleState.motion = idle;
        var downState = machine.AddState("Down", new Vector3(500, 180)); downState.motion = down;
        machine.defaultState = upState;
        var transition = upState.AddTransition(idleState);
        transition.hasExitTime = true; transition.exitTime = 1f; transition.duration = 0f;
        controller.layers = new[] { layer };
        EditorUtility.SetDirty(controller);
        const string prefabPath = "Assets/Resources/Prefabs/UI/Prompt.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator = prefab.GetComponent<Animator>();
            if (animator == null) animator = prefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var canvas in prefab.GetComponentsInChildren<Canvas>(true))
            {
                var group = canvas.GetComponent<CanvasGroup>();
                if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip BuildClip(string name, string spritesFolder, float fps, bool loop)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + name + ".anim");
        var paths = Directory.GetFiles("Assets/Resources/Sprites/Inventory & UI/prompt/" + spritesFolder, "*.png").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new Exception("No prompt frames: " + spritesFolder);
        var frames = paths.Select((p, i) => new ObjectReferenceKeyframe { time = i / fps, value = AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/')) }).ToList();
        if (frames.Any(f => f.value == null)) throw new Exception("Prompt frames must be imported as sprites.");
        // Hold the last frame for one full frame before exiting/looping.
        frames.Add(new ObjectReferenceKeyframe { time = paths.Length / fps, value = frames[frames.Count - 1].value });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), frames.ToArray());
        float duration = paths.Length / fps;
        var alpha = name == "Up" ? AnimationCurve.EaseInOut(0, 0, duration * .75f, 1)
            : name == "Down" ? AnimationCurve.EaseInOut(0, 1, duration, 0)
            : AnimationCurve.Constant(0, duration, 1);
        clip.SetCurve("BenchCanvas", typeof(CanvasGroup), "m_Alpha", alpha);
        clip.frameRate = fps;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; settings.startTime = 0; settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }
}
