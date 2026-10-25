using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Disposable actor: never registers a player, changes a save slot, or respawns the user's knight.
[InitializeOnLoad]
public static class KnightAudioValidation
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static KnightAudioValidation()=>EditorApplication.update+=Check;
    private static void Check()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/ValidateKnightAudio.request"))return;
        File.Delete("Temp/ValidateKnightAudio.request");Validate();
    }
    private static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Tools/Hollow Knight/Validate Knight Audio")]
    public static void Validate()
    {
        if(!EditorApplication.isPlaying) {Debug.Log("Enter Play Mode before validating Knight audio.");return;}
        var report=new List<string>();var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.CreateScene("KnightAudioValidation",new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        GameObject actor=null;float timeScale=Time.timeScale;
        try
        {
            Time.timeScale=1;
            foreach(string clip in new[]{"Player/PlayerJump","Player/hero_wings","Player/PlayerWallJump","Player/hero_dash","Player/PlayerSoftLand","Player/PlayerHardLand","Player/PlayerInjured","Player/PlayerDeath","Enemy/EnermyDamageAudio","object/sword_hit_reject","UI/UIHint_Save","Player/sword_2","Player/sword_3","Player/sword_4","Player/PlayerRun","Player/PlayerWallSlide","Player/PlayerFalling"})
                Require(Resources.Load<AudioClip>("Audio/"+clip)!=null,"Missing Knight audio clip: "+clip);
            report.Add("PASS: all 17 existing audio assets resolve.");
            actor=new GameObject("Audio test Knight");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,scene);
            var body=actor.AddComponent<Rigidbody2D>();actor.AddComponent<BoxCollider2D>();actor.AddComponent<Animator>();
            var health=actor.AddComponent<PlayerHealth>();var player=actor.AddComponent<PlayerController>();
            actor.SetActive(true);player.enabled=false;
            var audio=actor.GetComponent<KnightAudio>();Require(audio!=null,"PlayerController did not install KnightAudio.");
            AudioSource Source(string name)=>actor.GetComponentsInChildren<AudioSource>().Single(s=>s.name==name);
            var actions=Source("Knight actions");var impacts=Source("Knight impacts");var steps=Source("Knight footsteps");var slide=Source("Knight wall slide");
            foreach(string action in new[]{"Jump","DoubleJump","SlideJump"})
            {actions.Stop();Call(player,"BeginJump",0f,action);Require(actions.isPlaying,action+" callback did not play audio.");}
            actions.Stop();Call(player,"BeginDash");Require(actions.isPlaying,"Dash callback did not play audio.");
            report.Add("PASS: actual jump, double jump, wall jump and dash callbacks play action audio.");
            actions.Stop();audio.Slash();Require(actions.isPlaying,"Slash did not play.");
            impacts.Stop();audio.Hit();Require(impacts.isPlaying,"Enemy hit did not play.");
            impacts.Stop();Set(audio,"nextImpact",0f);player.PlayTerrainHit();Require(impacts.isPlaying,"Terrain hit did not play.");
            impacts.Stop();health.TakeDamage(1);Require(impacts.isPlaying,"Health damage callback did not play.");
            impacts.Stop();Set(audio,"airborne",true);Set(audio,"peakFallSpeed",18f);audio.Land();Require(impacts.isPlaying,"Hard landing did not play.");
            actions.Stop();audio.Rest();Require(actions.isPlaying,"Bench rest did not play.");
            report.Add("PASS: slash, enemy/terrain impacts, damage, hard landing and rest audio playback.");
            player.enabled=true;Set(player,"dashRemaining",0f);Set(player,"grounded",true);body.linearVelocity=new Vector2(7,0);Call(audio,"Update");
            Require(steps.isPlaying,"Running did not start footsteps.");
            body.linearVelocity=Vector2.zero;Call(audio,"Update");Require(!steps.isPlaying,"Footsteps continued at rest.");
            Set(player,"grounded",false);Set(player,"wallSliding",true);Call(audio,"Update");Require(slide.isPlaying,"Wall slide loop did not start.");
            Set(player,"wallSliding",false);Call(audio,"Update");Require(!slide.isPlaying,"Wall slide loop did not stop.");
            Set(player,"grounded",true);body.linearVelocity=new Vector2(7,0);Call(audio,"Update");
            Time.timeScale=0;Call(audio,"Update");Require(!steps.isPlaying,"Footsteps did not pause.");
            Time.timeScale=1;Call(audio,"Update");Require(steps.isPlaying,"Footsteps did not resume.");
            player.enabled=false;Call(audio,"Update");Require(!steps.isPlaying,"Disabled controller kept footsteps playing.");
            report.Add("PASS: running/idle, wall slide enter/exit, pause/resume and disabled-controller loop cleanup.");
            foreach(var source in actor.GetComponentsInChildren<AudioSource>())Require(source.GetComponent<OptionsAudioChannel>()!=null && !source.GetComponent<OptionsAudioChannel>().music,"Knight sound is not routed to Sound Volume.");
            Require(Mathf.Abs(actions.volume-audio.actionVolume*GameOptions.SoundVolume)<0.001f,"Sound volume is applied incorrectly.");
            audio.Hurt(true);Require(impacts.isPlaying,"Death feedback did not play.");audio.enabled=false;
            Require(actor.GetComponentsInChildren<AudioSource>().All(s=>!s.isPlaying),"Disabling audio left a source playing.");
            report.Add("PASS: Sound Volume routing, death feedback and audio teardown.");
        }
        catch(Exception e){report.Add("FAIL: "+e);Debug.LogException(e);}
        finally
        {
            Time.timeScale=timeScale;if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);SceneManager.UnloadSceneAsync(scene);
            Directory.CreateDirectory("DirtmouthPreviews");File.WriteAllLines("DirtmouthPreviews/knight-audio-validation.txt",report);
            File.WriteAllLines("Temp/KnightAudioValidation.txt",report);Debug.Log(string.Join("\n",report));
        }
    }
}
