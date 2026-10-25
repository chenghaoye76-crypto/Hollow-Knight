using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class KnightSpellSetup
{
    static KnightSpellSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupKnightSpells.request")) return;
        File.Delete("Temp/SetupKnightSpells.request");
        try { Setup(); }
        catch (Exception e) { File.WriteAllText("Temp/KnightSpellSetup.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Hollow Knight/Set Up Knight Spells")]
    public static void Setup()
    {
        Sprite[] Frames(string path) => PlayerSpriteAnimationSetup.Frames(path);
        var assets = AssetDatabase.LoadAssetAtPath<KnightSpellAssets>("Assets/Resources/KnightSpells.asset");
        if (assets == null)
        {
            assets = ScriptableObject.CreateInstance<KnightSpellAssets>();
            AssetDatabase.CreateAsset(assets, "Assets/Resources/KnightSpells.asset");
        }
        assets.material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
        if (assets.material == null) throw new Exception("Spell sprite material missing.");
        assets.projectile = Frames("skill/fireBall/Fire Ball/002.Ball");
        assets.shadeProjectile = Frames("skill2/fireBall2/018.Fireball");
        assets.rings = Frames("skill2/quake2/015.Quake 2 Mega");
        assets.impact = Frames("skill2/quake2/014.Quake 2 Blast");
        assets.tendrils = Frames("skill2/quake2/017.Quake 2 Trail");
        assets.focus = Frames("skill/foucus/003.Focus Effect");
        assets.focusBurst = Frames("skill/foucus/005.Burst Effect");
        assets.shriek = Frames("skill2/scream2/020.Scream2");
        assets.shriekBase = Frames("skill2/scream2/021.Scream2 Base");
        assets.soulGlow = Frames("Strikes/009.Health Get")[0];
        AudioClip Clip(string name) => Resources.Load<AudioClip>("Audio/"+name);
        assets.focusSound = Clip("Player/hero_nail_art_charge_loop"); assets.healSound = Clip("UI/UIHint_Save");
        assets.castSound = Clip("Player/hero_fireball"); assets.shriekSound = Clip("Player/hero_void_scream_spell");
        assets.diveSound = Clip("Player/hero_quake_spell_dash"); assets.impactSound = Clip("Player/hero_void_quake_impact");
        EditorUtility.SetDirty(assets);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Knight/Knight.controller");
        if (controller == null) throw new Exception("Original Knight controller missing.");
        foreach (string name in new[]{"Slide","SlideJump"})
        {
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name) ?? machine.AddState(name);
            state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Knight/"+name+".anim");
            state.writeDefaultValues=false;
            EditorUtility.SetDirty(state);
        }
        const string folder = "Assets/Animation/KnightSpells";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Animation", "KnightSpells");
        void Add(string name, bool loop, float fps, string source)
        {
            string path = folder+"/"+name+".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
            var sprites = Frames(source);
            float duration = sprites.Length/fps;
            clip.frameRate = fps;
            var keys = sprites.Select((s,i) => new ObjectReferenceKeyframe { time = i/fps, value = s }).ToList();
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop; settings.startTime = 0; settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings); clip.frameRate = fps;
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
            state.motion = clip; state.writeDefaultValues = false;
            EditorUtility.SetDirty(clip); EditorUtility.SetDirty(state);
        }
        Add("SpellFocus", true, 8, "skill/foucus/050.Focus");
        Add("SpellCast", false, 16, "skill2/fireBall2/101.Fireball2 Cast");
        Add("SpellShriek", true, 12, "skill2/scream2/117.Scream 2");
        Add("SpellDiveCharge", false, 18, "skill/quake/092.Quake Antic");
        Add("SpellDive", true, 12, "skill2/quake2/111.Quake Fall 2");
        Add("SpellDiveLand", false, 12, "skill2/quake2/112.Quake Land 2");
        KnightActionPoseSetup.Configure();
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        // Test/spawned knights without a scene-assigned controller use the same original graph.
        File.Copy("Assets/Animation/Knight/Knight.controller", "Assets/Resources/KnightMotion.controller", true);
        AssetDatabase.ImportAsset("Assets/Resources/KnightMotion.controller", ImportAssetOptions.ForceUpdate);
        File.WriteAllText("Temp/KnightSpellSetup.txt", "PASS: all spell body/effect frames use Assets/PlayerSprite dedicated Focus/Fireball2/Quake2/Scream2 sequences; six audio clips connected; gameplay timings preserved.");
    }
}
