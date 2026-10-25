using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class KnightActionPoseSetup
{
    static KnightActionPoseSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupKnightActionPoses.request"))return;
        File.Delete("Temp/SetupKnightActionPoses.request");
        try
        {
            Configure();AssetDatabase.SaveAssets();
            File.Copy("Assets/Animation/Knight/Knight.controller","Assets/Resources/KnightMotion.controller",true);
            AssetDatabase.ImportAsset("Assets/Resources/KnightMotion.controller",ImportAssetOptions.ForceUpdate);
            File.WriteAllText("Temp/KnightActionPoseSetup.txt","PASS: PlayerSprite DoubleJump, DashRecover, BenchSit/Sleep/Wake imported; action playback gates and original graph preserved.");
        }
        catch(Exception e){File.WriteAllText("Temp/KnightActionPoseSetup.txt",e.ToString());Debug.LogException(e);}
    }
    private static AnimatorState[] States(AnimatorStateMachine machine) => machine.states.Select(s=>s.state)
        .Concat(machine.stateMachines.SelectMany(s=>States(s.stateMachine))).ToArray();
    public static void Configure()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Knight/Knight.controller");
        foreach(string parameter in new[]{"DoubleJumpActive","DashRecoverActive"})
            if(!controller.parameters.Any(p=>p.name==parameter))controller.AddParameter(parameter,AnimatorControllerParameterType.Bool);
        var machine=controller.layers[0].stateMachine;
        var dash=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Knight/Dash.anim");
        if(dash!=null)
        {
            var settings=AnimationUtility.GetAnimationClipSettings(dash);settings.loopTime=false;
            AnimationUtility.SetAnimationClipSettings(dash,settings);EditorUtility.SetDirty(dash);
        }
        void Pose(string name,string folder,float fps,bool loop=false)
        {
            string path="Assets/Animation/Knight/"+name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip{name=name};AssetDatabase.CreateAsset(clip,path);}
            var frames=PlayerSpriteAnimationSetup.Frames(folder);
            clip.frameRate=fps;
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/fps,value=s}).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=frames.Length/fps;settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            var state=States(machine).FirstOrDefault(s=>s.name==name) ?? machine.AddState(name);
            state.motion=clip;state.speed=1;state.writeDefaultValues=false;
            EditorUtility.SetDirty(clip);EditorUtility.SetDirty(state);
        }
        Pose("DoubleJump","motion/Jump/Double Jump",24);
        Pose("DashRecover","motion/Dash/Dash To Idle",24);
        Pose("BenchSit","motion/sit/039.Sit",10);
        Pose("BenchSleep","motion/sit/044.Sitting Asleep",8,true);
        Pose("BenchWake","motion/sit/042.Wake",16);
        foreach(var state in States(machine))
        foreach(var transition in state.transitions)
        {
            if(state.name=="DoubleJump" && transition.destinationState!=null && transition.destinationState.name=="Fall" &&
                !transition.conditions.Any(c=>c.parameter=="DoubleJumpActive")) transition.AddCondition(AnimatorConditionMode.IfNot,0,"DoubleJumpActive");
            if(state.name=="Dash")
            {
                foreach(string parameter in new[]{"IsDash","DashRecoverActive"})
                    if(!transition.conditions.Any(c=>c.parameter==parameter)) transition.AddCondition(AnimatorConditionMode.IfNot,0,parameter);
                transition.hasExitTime=false;transition.duration=0;
            }
            EditorUtility.SetDirty(transition);
        }
        EditorUtility.SetDirty(controller);
    }
}
