using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PromptAnimationValidation
{
    private static IEnumerator routine;
    private static GameObject instance;
    private static readonly List<string> report = new();
    static PromptAnimationValidation() => EditorApplication.update += Tick;
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (routine == null && File.Exists("Temp/ValidatePromptAnimation.request"))
        {
            File.Delete("Temp/ValidatePromptAnimation.request"); report.Clear(); routine = Run();
        }
        if (routine == null) return;
        try { if (routine.MoveNext()) return; }
        catch (Exception e) { report.Add("FAIL: " + e); }
        routine = null;
        if (instance != null) UnityEngine.Object.Destroy(instance);
        File.WriteAllLines("Temp/PromptAnimationValidation.txt", report);
    }
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        report.Add("PASS: " + label);
    }
    private static IEnumerator Run()
    {
        instance = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Prompt"));
        instance.transform.position = new Vector3(1000, 1000, 0);
        var prompt = instance.GetComponent<Prompt>();
        var animator = instance.GetComponent<Animator>();
        var sprite = instance.GetComponent<SpriteRenderer>();
        var group = instance.GetComponentInChildren<CanvasGroup>(true);
        prompt.HideImmediate();
        Check(!instance.activeSelf && !prompt.IsVisible, "initial prompt hidden");
        prompt.SetVisible(true);
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Up"), "entry plays Up");
        var firstSprite = sprite.sprite;
        float until = Time.realtimeSinceStartup + .3f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Check(sprite.sprite != firstSprite && group.alpha > 0f, "sprite frames advance and text fades in");
        until = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && group.alpha > .99f, "Up completes into looping Idle with visible text");
        prompt.SetVisible(false);
        Check(instance.activeSelf && !prompt.IsVisible && animator.GetCurrentAnimatorStateInfo(0).IsName("Down"), "exit keeps prompt active while Down plays");
        prompt.SetVisible(false);
        until = Time.realtimeSinceStartup + .15f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > .05f, "repeated exit does not restart Down");
        prompt.SetVisible(true);
        Check(prompt.IsVisible && animator.GetCurrentAnimatorStateInfo(0).IsName("Up"), "quick reentry interrupts Down with Up");
        until = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Check(instance.activeSelf && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "reentry remains visible");
        prompt.SetVisible(false);
        until = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Check(!instance.activeSelf, "Down finishes before deactivation");
    }
}
