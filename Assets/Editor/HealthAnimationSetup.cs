using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class HealthAnimationSetup
{
    private const string Folder = "Assets/Animation/Health/";
    static HealthAnimationSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupHealthAnimation.request")) return;
        File.Delete("Temp/SetupHealthAnimation.request");
        try { Setup(); File.WriteAllText("Temp/HealthAnimationSetup.txt", "PASS: Health clips, triggers and transitions rebuilt; Game and Dirtmouth HUD animation checks passed."); }
        catch (Exception error) { File.WriteAllText("Temp/HealthAnimationSetup.txt", error.ToString()); Debug.LogException(error); }
    }

    [MenuItem("Tools/Hollow Knight/Repair Health UI Animations")]
    public static void Setup()
    {
        var full = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Inventory & UI/select_game_HUD_0001_health.png");
        var empty = Frames("001.Health Empty")[0];
        var enter = Build("Enter", Frames("005.Health Appear").Concat(new[] { full }).ToArray(), 12f, 1, 1);
        var hurt = Build("Hurt", Frames("003.Health Break").Concat(new[] { empty }).ToArray(), 12f, 1, 1);
        var idle = Build("Idle", new[] { full }, 12f, 1, 1);
        var depleted = Build("Empty", new[] { empty }, 12f, 1, 1);
        var hide = Build("Hide", null, 5f, 1, 0);
        var hidden = Build("Hidden", null, 12f, 0, 0);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "Health.controller");
        if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
        var layer = controller.layers[0];
        layer.defaultWeight = 1;
        var machine = layer.stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        foreach (var name in new[] { "Enter", "Hurt", "Hide" }) controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        var hiddenState = State(machine, "Hidden", hidden, 250, 200);
        var enterState = State(machine, "Enter", enter, 250, 0);
        var idleState = State(machine, "Idle", idle, 500, 0);
        var hurtState = State(machine, "Hurt", hurt, 500, 100);
        var emptyState = State(machine, "Empty", depleted, 750, 100);
        var hideState = State(machine, "Hide", hide, 500, 200);
        machine.defaultState = hiddenState;
        Trigger(machine, enterState, "Enter");
        Trigger(machine, hurtState, "Hurt");
        Trigger(machine, hideState, "Hide");
        Finish(enterState, idleState); Finish(hurtState, emptyState); Finish(hideState, hiddenState);
        controller.layers = new[] { layer };
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Validate(controller);
    }

    private static Sprite[] Frames(string folder) => Directory.GetFiles("Assets/Sprite/UI/HUD/" + folder, "*.png")
        .OrderBy(p => p, StringComparer.Ordinal).SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p.Replace('\\', '/')).OfType<Sprite>()).ToArray();

    private static AnimationClip Build(string name, Sprite[] frames, float fps, float alphaStart, float alphaEnd)
    {
        if (frames != null && (frames.Length == 0 || frames.Any(s => s == null))) throw new Exception("Missing health sprites: " + name);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + name + ".anim");
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, Folder + name + ".anim"); }
        clip.ClearCurves(); clip.frameRate = fps;
        float duration = (frames == null ? 1 : frames.Length) / fps;
        // Only the primary Image draws the mask. The five old prototype overlays stay off.
        for (int i = 0; i < 6; i++)
        {
            string path = i == 0 ? "Image" : "Image (" + i + ")";
            clip.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, duration, i == 0 ? 1 : 0));
        }
        clip.SetCurve("Image", typeof(Image), "m_Color.a", AnimationCurve.Linear(0, alphaStart, duration, alphaEnd));
        if (frames != null)
        {
            var keys = frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = i / fps, value = sprite }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = duration, value = frames.Last() });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("Image", typeof(Image), "m_Sprite"), keys.ToArray());
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false; settings.startTime = 0; settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip, float x, float y)
    {
        var state = machine.AddState(name, new Vector3(x, y)); state.motion = clip; state.writeDefaultValues = false; return state;
    }
    private static void Trigger(AnimatorStateMachine machine, AnimatorState target, string parameter)
    {
        var transition = machine.AddAnyStateTransition(target);
        transition.hasExitTime = false; transition.duration = 0; transition.canTransitionToSelf = true;
        transition.AddCondition(AnimatorConditionMode.If, 0, parameter);
    }
    private static void Finish(AnimatorState from, AnimatorState to)
    {
        var transition = from.AddTransition(to); transition.hasExitTime = true; transition.exitTime = 1; transition.duration = 0;
    }

    private static void Validate(AnimatorController controller)
    {
        foreach (string path in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/Dirtmouth.unity" })
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerUI>(true)).Single();
                foreach (var animator in ui.healthItem)
                {
                    if (animator == null || animator.runtimeAnimatorController != controller) throw new Exception(path + ": missing health animator/controller");
                    if (animator.transform.Find("Image")?.GetComponent<Image>() == null) throw new Exception(path + ": missing primary health image");
                    animator.Rebind(); animator.Update(0);
                    Assert(animator, "Hidden", 0);
                    animator.SetTrigger("Enter"); animator.Update(.01f); animator.Update(.7f); animator.Update(.01f);
                    Assert(animator, "Idle", 1);
                    animator.SetTrigger("Hurt"); animator.Update(.01f); animator.Update(.7f); animator.Update(.01f);
                    Assert(animator, "Empty", 1);
                    animator.SetTrigger("Enter"); animator.Update(.01f);
                    animator.SetTrigger("Hide"); animator.Update(.01f); animator.Update(.3f); animator.Update(.01f);
                    Assert(animator, "Hidden", 0);
                    animator.SetTrigger("Enter"); animator.Update(.01f); animator.Update(.7f); animator.Update(.01f);
                    Assert(animator, "Idle", 1);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
    private static void Assert(Animator animator, string state, float alpha)
    {
        var image = animator.transform.Find("Image").GetComponent<Image>();
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(state) || Mathf.Abs(image.color.a - alpha) > .01f)
            throw new Exception(animator.name + ": expected " + state + " alpha " + alpha);
        if ((state == "Idle" || state == "Empty") && image.sprite == null) throw new Exception("Missing sprite in " + state);
    }
}
