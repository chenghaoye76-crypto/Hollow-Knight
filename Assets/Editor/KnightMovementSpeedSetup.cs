using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightMovementSpeedSetup
{
    static KnightMovementSpeedSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/SetupKnightMovementSpeed.request")) return;
        File.Delete("Temp/SetupKnightMovementSpeed.request");
        try { Apply(); File.WriteAllText("Temp/KnightMovementSpeedSetup.txt","PASS: normal ground/air speed 8.3, solemn ground/air speed 6; room settings installed; original animation graph extended with WalkOnly conditions."); }
        catch(Exception e) {File.WriteAllText("Temp/KnightMovementSpeedSetup.txt",e.ToString()); Debug.LogException(e);}
    }
    private static AnimatorState[] States(AnimatorStateMachine machine) => machine.states.Select(s=>s.state)
        .Concat(machine.stateMachines.SelectMany(s=>States(s.stateMachine))).ToArray();
    [MenuItem("Tools/Hollow Knight/Set Up Knight Movement Speeds")]
    public static void Apply()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Knight/Knight.controller");
        if(!controller.parameters.Any(p=>p.name=="WalkOnly"))controller.AddParameter("WalkOnly",AnimatorControllerParameterType.Bool);
        var states=States(controller.layers[0].stateMachine);
        var walk=states.First(s=>s.name=="Walk");var run=states.First(s=>s.name=="Run");
        foreach(var state in states)
        foreach(var transition in state.transitions.Where(t=>t.destinationState==run).ToArray())
        {
            if(!transition.conditions.Any(c=>c.parameter=="WalkOnly"))transition.AddCondition(AnimatorConditionMode.IfNot,0,"WalkOnly");
            if(state==walk)continue;
            if(state.transitions.Any(t=>t.destinationState==walk && t.conditions.Any(c=>c.parameter=="WalkOnly")))continue;
            var walking=state.AddTransition(walk);
            walking.hasExitTime=transition.hasExitTime;walking.exitTime=transition.exitTime;
            walking.hasFixedDuration=transition.hasFixedDuration;walking.duration=transition.duration;
            walking.offset=transition.offset;walking.canTransitionToSelf=false;
            foreach(var condition in transition.conditions.Where(c=>c.parameter!="WalkOnly"))walking.AddCondition(condition.mode,condition.threshold,condition.parameter);
            walking.AddCondition(AnimatorConditionMode.If,0,"WalkOnly");
        }
        if(!run.transitions.Any(t=>t.destinationState==walk && t.conditions.Any(c=>c.parameter=="WalkOnly")))
        {
            var transition=run.AddTransition(walk);transition.hasExitTime=false;transition.duration=0;
            transition.AddCondition(AnimatorConditionMode.If,0,"WalkOnly");
            transition.AddCondition(AnimatorConditionMode.If,0,"IsOnGround");
            transition.AddCondition(AnimatorConditionMode.NotEqual,0,"MoveChange");
        }
        KnightRunCycleSetup.Configure();
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        File.Copy("Assets/Animation/Knight/Knight.controller","Assets/Resources/KnightMotion.controller",true);
        AssetDatabase.ImportAsset("Assets/Resources/KnightMotion.controller",ImportAssetOptions.ForceUpdate);
        foreach(string path in new[]{"Assets/Scenes/Dirtmouth.unity","Assets/Scenes/Game.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var players=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerController>(true));
                foreach(var player in players)
                {
                    var data=new SerializedObject(player);data.FindProperty("moveSpeed").floatValue=8.3f;data.FindProperty("walkSpeed").floatValue=6f;data.ApplyModifiedProperties();
                }
                if(!scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<KnightMovementArea>(true)).Any())
                {
                    var settings=new GameObject("Knight Movement Area");SceneManager.MoveGameObjectToScene(settings,scene);settings.AddComponent<KnightMovementArea>();
                }
                var solemnBounds=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider2D>(true))
                    .FirstOrDefault(c=>string.Equals(c.name,"dirtmouthColl",StringComparison.OrdinalIgnoreCase));
                if(solemnBounds!=null)
                {
                    var settings=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<KnightMovementArea>(true)).First();
                    settings.walkOnly=true;settings.areaBounds=solemnBounds;EditorUtility.SetDirty(settings);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
