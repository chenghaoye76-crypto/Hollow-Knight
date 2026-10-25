using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightVisualValidation
{
    private static IEnumerator run;
    private static readonly List<string> report=new();
    private static readonly List<GameObject> temporary=new();
    static KnightVisualValidation()=>EditorApplication.update+=Update;
    private static void Update()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlaying)
        {
            if(File.Exists("Temp/ValidateKnightVisuals.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool("KnightVisualValidationEnteredPlay",true);
                EditorApplication.EnterPlaymode();
            }
            return;
        }
        if(run==null && File.Exists("Temp/ValidateKnightVisuals.request"))
        {File.Delete("Temp/ValidateKnightVisuals.request");report.Clear();run=Validate();}
        if(run==null)return;
        try {if(!run.MoveNext()){run=null;Save();}}
        catch(Exception e){run=null;report.Add("FAIL: "+e);foreach(var go in temporary)if(go!=null)UnityEngine.Object.DestroyImmediate(go);temporary.Clear();Save();Debug.LogException(e);}
    }
    private static void Save()
    {
        Directory.CreateDirectory("DirtmouthPreviews");File.WriteAllLines("DirtmouthPreviews/knight-visual-validation.txt",report);File.WriteAllLines("Temp/KnightVisualValidation.txt",report);
        if(SessionState.GetBool("KnightVisualValidationEnteredPlay",false))
        {
            SessionState.SetBool("KnightVisualValidationEnteredPlay",false);
            EditorApplication.delayCall+=EditorApplication.ExitPlaymode;
        }
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static IEnumerator Wait(float duration){double until=EditorApplication.timeSinceStartup+duration;while(EditorApplication.timeSinceStartup<until)yield return null;}
    private static IEnumerator Validate()
    {
        ValidateTriggers();
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var camera=Camera.main;
        Require(player!=null && camera!=null,"Need the live Knight and camera.");
        foreach(string name in new[]{"AttackEffectRoot","UpAttackEffectRoot","DownAttackEffectRoot"})
        {
            var prefab=Resources.Load<GameObject>("Prefabs/"+name);
            Require(prefab!=null,"Missing attack prefab: "+name);
            var renderer=prefab.GetComponentInChildren<SpriteRenderer>();
            Require(renderer.sprite!=null && renderer.sortingLayerName=="Front" && renderer.sortingOrder>=50,"Attack sorting not repaired.");
            Require(renderer.sharedMaterial!=null && !ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),"Attack material failed.");
            Require((camera.cullingMask&(1<<renderer.gameObject.layer))!=0,"Camera excludes the attack layer.");
            ValidateFollowing(prefab);
            Vector3 offset=name.StartsWith("Down")?new Vector3(0,-1.23f):name.StartsWith("Up")?new Vector3(0.2f,0):new Vector3(0.5f,0);
            Capture(camera,player,prefab,offset,name);
        }
        report.Add("PASS: normal/up/down slash prefabs render visible pixels over the actual Dirtmouth scene; screenshots saved.");
        report.Add("PASS: normal/up/down slash roots and hitboxes follow owner translation while preserving the original swing direction and scale.");
        var effects=new List<GameObject>();
        foreach(string name in new[]{"AttackEffectRoot","UpAttackEffectRoot","DownAttackEffectRoot"})
        {var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/"+name),player.transform.position,Quaternion.identity);effects.Add(go);temporary.Add(go);}
        var wait=Wait(0.065f);while(wait.MoveNext())yield return null;
        Require(effects.All(go=>go!=null),"Slash disappeared before it could be seen.");
        wait=Wait(0.22f);while(wait.MoveNext())yield return null;
        Require(effects.All(go=>go==null),"Slash animation did not clean itself up.");temporary.Clear();
        report.Add("PASS: all three slashes survive the first 65ms and destroy themselves after playback.");
        Debug.Log(string.Join("\n",report));
    }
    private static void ValidateFollowing(GameObject prefab)
    {
        var owner=new GameObject("Slash follow test owner");GameObject effect=null;
        try
        {
            owner.transform.position=new Vector3(1000,1000,0);
            Vector3 offset=new Vector3(0.5f,-0.2f,0);
            effect=UnityEngine.Object.Instantiate(prefab,owner.transform.position+offset,Quaternion.Euler(0,180,0));
            var visual=effect.GetComponentInChildren<AttackEffect>();visual.Follow(owner.transform);
            var rotation=effect.transform.rotation;var scale=effect.transform.lossyScale;
            owner.transform.position+=new Vector3(3,2,0);owner.transform.localScale=new Vector3(-1,1,1);
            visual.SendMessage("LateUpdate",SendMessageOptions.RequireReceiver);
            Require(Vector3.Distance(effect.transform.position,owner.transform.position+offset)<0.001f,"Slash failed to follow a moving owner.");
            Require(Quaternion.Angle(effect.transform.rotation,rotation)<0.001f && Vector3.Distance(effect.transform.lossyScale,scale)<0.001f,"Turning owner flipped an in-progress slash.");
            owner.transform.position+=Vector3.left;
            visual.SendMessage("FixedUpdate",SendMessageOptions.RequireReceiver);
            Require(Vector3.Distance(effect.transform.position,owner.transform.position+offset)<0.001f && effect.GetComponent<Collider2D>()!=null,"Hitbox failed to follow in the physics step.");
        }
        finally {if(effect!=null)UnityEngine.Object.DestroyImmediate(effect);UnityEngine.Object.DestroyImmediate(owner);}
    }
    private static void Capture(Camera camera,PlayerController player,GameObject prefab,Vector3 offset,string name)
    {
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var target=RenderTexture.GetTemporary(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        GameObject effect=null;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var before=image.GetPixels32();
            effect=UnityEngine.Object.Instantiate(prefab,player.transform.position+offset,Quaternion.identity);
            effect.GetComponentInChildren<Animator>().Update(0.02f);
            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var after=image.GetPixels32();
            int visible=0;for(int i=0;i<before.Length;i++)if(Math.Abs(after[i].r-before[i].r)+Math.Abs(after[i].g-before[i].g)+Math.Abs(after[i].b-before[i].b)>100)visible++;
            Require(visible>30,"Slash did not produce visible pixels: "+name+" / "+visible);
            Directory.CreateDirectory("DirtmouthPreviews");File.WriteAllBytes("DirtmouthPreviews/"+name+"-visible.png",image.EncodeToPNG());
            report.Add(name+": "+visible+" visible changed pixels.");
        }
        finally{if(effect!=null)UnityEngine.Object.DestroyImmediate(effect);camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
    }
    private static void ValidateTriggers()
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("EntrancePromptValidation",new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        var objects=new List<GameObject>();
        try
        {
            GameObject Create(string label){var go=new GameObject(label);SceneManager.MoveGameObjectToScene(go,scene);objects.Add(go);return go;}
            var visual=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Prompt"));SceneManager.MoveGameObjectToScene(visual,scene);objects.Add(visual);var prompt=visual.GetComponent<Prompt>();
            var zone=Create("entrance");zone.SetActive(false);var box=zone.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(4,4);
            var trigger=zone.AddComponent<EntrancePrompt>();trigger.prompt=prompt;zone.SetActive(true);
            Require(!prompt.IsVisible,"Entrance prompt must start hidden.");
            var other=Create("Non player");var otherBody=other.AddComponent<Rigidbody2D>();otherBody.gravityScale=0;other.AddComponent<BoxCollider2D>();
            var physics=scene.GetPhysicsScene2D();physics.Simulate(0.02f);Require(!prompt.IsVisible,"Non-player showed a prompt.");other.transform.position=new Vector3(20,0);
            var actor=Create("Test Knight");actor.SetActive(false);actor.transform.position=new Vector3(20,0);var body=actor.AddComponent<Rigidbody2D>();actor.AddComponent<BoxCollider2D>();actor.AddComponent<Animator>();var pc=actor.AddComponent<PlayerController>();actor.SetActive(true);pc.enabled=false;body.gravityScale=0;
            body.position=Vector2.zero;Physics2D.SyncTransforms();physics.Simulate(0.02f);Require(prompt.IsVisible,"Player enter did not show prompt.");
            var extra=actor.AddComponent<CircleCollider2D>();physics.Simulate(0.02f);extra.offset=new Vector2(20,0);Physics2D.SyncTransforms();physics.Simulate(0.02f);Require(prompt.IsVisible,"One collider exiting hid a still-occupied prompt.");
            body.position=new Vector2(20,0);Physics2D.SyncTransforms();physics.Simulate(0.02f);Require(!prompt.IsVisible,"Player exit did not hide prompt.");
            body.position=Vector2.zero;Physics2D.SyncTransforms();physics.Simulate(0.02f);Require(prompt.IsVisible,"Re-enter did not show prompt.");
            zone.SetActive(false);Require(!prompt.IsVisible,"Disabled entrance left prompt visible.");
            report.Add("PASS: actual 2D trigger simulation: initially hidden, non-player ignored, enter/re-enter show, multi-collider exit handled, exit/disable hide.");
        }
        finally{foreach(var go in objects)if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);SceneManager.UnloadSceneAsync(scene);}
    }
}
