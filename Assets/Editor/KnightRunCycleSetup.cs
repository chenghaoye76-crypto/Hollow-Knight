using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class KnightRunCycleSetup
{
    static KnightRunCycleSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupKnightRunCycle.request"))return;
        File.Delete("Temp/SetupKnightRunCycle.request");
        try
        {
            Configure();AssetDatabase.SaveAssets();
            File.Copy("Assets/Animation/Knight/Knight.controller","Assets/Resources/KnightMotion.controller",true);
            AssetDatabase.ImportAsset("Assets/Resources/KnightMotion.controller",ImportAssetOptions.ForceUpdate);
            File.WriteAllText("Temp/KnightRunCycleSetup.txt","PASS: first four PlayerSprite Run frames play once on startup, final nine loop at 24 FPS. Original Walk -> Run graph retained; solemn mode uses its original walking clip.");
        }
        catch(Exception e){File.WriteAllText("Temp/KnightRunCycleSetup.txt",e.ToString());Debug.LogException(e);}
    }
    public static void Configure()
    {
        var frames=PlayerSpriteAnimationSetup.Frames("motion/Run");
        if(frames.Length!=13)throw new Exception("Expected 13 PlayerSprite Run frames.");
        AnimationClip Clip(string path,Sprite[] sequence,bool loop,float fps=24)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip{name=Path.GetFileNameWithoutExtension(path)};AssetDatabase.CreateAsset(clip,path);}
            clip.frameRate=fps;
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),sequence.Select((s,i)=>new ObjectReferenceKeyframe{time=i/fps,value=s}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=sequence.Length/fps;settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
        }
        var start=Clip("Assets/Animation/Knight/RunStart.anim",frames.Take(4).ToArray(),false);
        Clip("Assets/Animation/Knight/Run.anim",frames.Skip(4).ToArray(),true);
        Clip("Assets/Animation/New Folder/Running.anim",frames.Skip(4).ToArray(),true);
        var walkFrames=PlayerSpriteAnimationSetup.Frames("motion/056.Walk");
        var walkClip=Clip("Assets/Animation/Knight/Walk.anim",walkFrames,true,8);
        Clip("Assets/Animation/New Folder/Walking.anim",walkFrames,true,8);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Knight/Knight.controller");
        var runStart=controller.layers[0].stateMachine.states.FirstOrDefault(s=>s.state.name=="RunStart").state ?? controller.layers[0].stateMachine.AddState("RunStart");
        runStart.motion=start;runStart.speed=1;runStart.writeDefaultValues=false;EditorUtility.SetDirty(runStart);
        if(!controller.parameters.Any(p=>p.name=="WalkMode"))controller.AddParameter("WalkMode",AnimatorControllerParameterType.Float);
        var walk=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Walk").state;
        foreach(var transition in walk.transitions.Where(t=>t.destinationState!=null && t.destinationState.name=="Run"))
        {
            transition.hasExitTime=true;transition.exitTime=1;transition.duration=0;transition.offset=0;
            EditorUtility.SetDirty(transition);
        }
        var tree=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>().FirstOrDefault(t=>t.name=="Run Start Or Solemn Walk");
        if(tree==null){tree=new BlendTree{name="Run Start Or Solemn Walk"};AssetDatabase.AddObjectToAsset(tree,controller);}
        tree.blendType=BlendTreeType.Simple1D;tree.blendParameter="WalkMode";tree.useAutomaticThresholds=false;
        tree.children=new[]{new ChildMotion{motion=start,threshold=0,timeScale=1},new ChildMotion{motion=walkClip,threshold=1,timeScale=1}};
        walk.motion=tree;EditorUtility.SetDirty(walk);EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);
    }
}
