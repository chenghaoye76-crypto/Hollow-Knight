using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightActionPoseValidation
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static KnightActionPoseValidation()=>EditorApplication.update+=Check;
    private static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/ValidateKnightActionPoses.request"))return;
        if(!EditorApplication.isPlaying)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool("ActionPoseTestEnteredPlay",true);EditorApplication.EnterPlaymode();}
            return;
        }
        File.Delete("Temp/ValidateKnightActionPoses.request");
        var report=new List<string>();
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.CreateScene("Knight action validation",new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        PlayerController player=null;
        try
        {
            SceneManager.SetActiveScene(scene);
            var floor=new GameObject("Test floor",typeof(BoxCollider2D)){layer=LayerMask.NameToLayer("Terrain")};
            floor.transform.position=Vector3.down*.5f;floor.GetComponent<BoxCollider2D>().size=new Vector2(40,1);
            var actor=new GameObject("Action test knight"){layer=31};actor.SetActive(false);actor.transform.position=new Vector3(0,.81f);
            var renderer=actor.AddComponent<SpriteRenderer>();renderer.sharedMaterial=Resources.Load<KnightSpellAssets>("KnightSpells").material;renderer.sortingLayerName="Front";renderer.sortingOrder=100;
            var body=actor.AddComponent<Rigidbody2D>();body.gravityScale=4.84f;body.constraints=RigidbodyConstraints2D.FreezeRotation;
            actor.AddComponent<BoxCollider2D>().size=new Vector2(.8f,1.6f);
            var animator=actor.AddComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Knight/Knight.controller");
            actor.AddComponent<PlayerHealth>();player=actor.AddComponent<PlayerController>();player.enabled=false;actor.SetActive(true);
            Call(player,"OnEnable");animator.Rebind();animator.Update(0);
            void Tick(){Call(player,"FixedUpdate");animator.Update(.02f);}
            bool Is(string name)=>animator.GetCurrentAnimatorStateInfo(0).IsName(name);
            body.position=new Vector2(0,5);Physics2D.SyncTransforms();
            typeof(PlayerController).GetMethod("BeginJump",Private).Invoke(player,new object[]{0f,"DoubleJump"});animator.Update(0);
            var seen=new HashSet<Sprite>{renderer.sprite};
            for(int i=0;i<16;i++)
            {
                body.linearVelocity=new Vector2(0,-2);Tick();
                Require(Is("DoubleJump"),"Early descent cut off the double-jump wingbeat.");seen.Add(renderer.sprite);
            }
            Require(seen.Count==8,"Double jump must display all eight PlayerSprite frames.");
            Tick();Tick();Require(Is("Fall"),"Completed double jump must return to Fall.");
            report.Add("PASS: immediate double jump displays all eight wingbeat frames even during early descent, then returns to Fall.");
            ValidateDashSequence(player,animator,renderer,body,false,false,1);
            ValidateDashSequence(player,animator,renderer,body,true,false,1);
            ValidateDashSequence(player,animator,renderer,body,true,true,1);
            ValidateDashSequence(player,animator,renderer,body,false,false,.5f);
            report.Add("PASS: running Dash -> all four RunStart frames -> Run; solemn ground Dash -> all four DashRecover frames -> Walk; air Dash -> Fall. Full main/ground-transition clips and unchanged physical duration verified, including half-speed playback.");
            ValidateAirDashLanding(player,animator,body,scene.GetPhysicsScene2D(),1f,false);
            ValidateAirDashLanding(player,animator,body,scene.GetPhysicsScene2D(),3.5f,true);
            ValidateAirDashLanding(player,animator,body,scene.GetPhysicsScene2D(),12f,false);
            ValidateDashOffLedge(player,animator,body,scene.GetPhysicsScene2D());
            report.Add("PASS: simulated low air dash queues contact until the main clip ends; middle/high air dashes show Fall before terrain contact. Every contact plays full SoftLand; ground dash off a ledge also falls and lands.");
            body.position=new Vector2(0,5);Physics2D.SyncTransforms();
            typeof(PlayerController).GetMethod("BeginDash",Private).Invoke(player,null);
            for(int i=0;i<16;i++)Tick();
            Require(!player.IsDashing && Is("Dash"),"Main animation must continue after physical dash ends.");
            typeof(PlayerController).GetMethod("BeginJump",Private).Invoke(player,new object[]{0f,"Jump"});
            for(int i=0;i<40;i++){Tick();Require(!Is("DashRecover"),"A cancelled dash must not replay recovery after jumping.");}
            typeof(PlayerController).GetMethod("BeginDash",Private).Invoke(player,null);Tick();
            Call(player,"OnDisable");
            Require(typeof(PlayerController).GetField("dashPose",Private).GetValue(player).ToString()=="None" && body.gravityScale==4.84f,"Disabling the knight must clear dash presentation and restore gravity.");
            report.Add("PASS: jump interruption and disable clear the pending sequence; no delayed recovery or extended dash force.");
            body.position=new Vector2(8,.81f);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Tick();
            var wake=(IEnumerator)typeof(PlayerController).GetMethod("EnterFromBench",Private).Invoke(player,null);
            Require(wake.MoveNext(),"Entry must begin on the bench.");animator.Update(0);
            Require(Is("BenchSleep") && player.IsResting && !body.simulated && !player.CanCastSpell,"Entry sleep must lock control, casting and physics.");
            var seat=Array.Find(SceneManager.GetActiveScene().GetRootGameObjects(),g=>g.name=="Scene entry bench");
            Require(seat!=null && seat.GetComponent<SpriteRenderer>().sprite!=null,"Scenes without an authored bench need a visible entry bench.");
            Require(Mathf.Abs(seat.transform.position.x)<.01f,"Fallback bench must use scene entry position, not an arbitrary restored save position.");
            seat.layer=31;Capture("BenchSleep",actor.transform.position);
            Require(wake.MoveNext(),"Sleep must lead to waking.");animator.Update(0);
            Require(Is("BenchWake"),"Entry must play dedicated BenchWake animation.");
            var seated=actor.transform.position;
            float previousY=seated.y;
            for(int frame=0;frame<17;frame++)
            {
                animator.Update(player.PoseDuration("BenchWake")*.05f);Call(player,"LateUpdate");
                Require(actor.transform.position.y<=previousY+.001f,"Bench exit must descend without an artificial upward bounce.");
                previousY=actor.transform.position.y;
            }
            var exit=(Vector3)typeof(PlayerController).GetField("benchExitPosition",Private).GetValue(player);
            Require(Vector3.Distance(actor.transform.position,exit)<.001f,"BenchWake's authored landing frames must already be on the floor.");
            Require(Vector3.Distance(actor.transform.position,exit)<=Vector3.Distance(seated,exit),"Wake must ease from the seat toward the standing position.");
            Capture("BenchWake",actor.transform.position);
            animator.Update(player.PoseDuration("BenchWake")*.16f);Call(player,"LateUpdate");
            Require(!wake.MoveNext(),"Wake routine must complete.");Tick();
            Require(!player.IsResting && body.simulated && Mathf.Abs(body.position.x-seat.transform.position.x)<.01f,"Wake must restore physics and finish next to the bench.");
            report.Add("PASS: scene entry creates/reuses a bench at the authored spawn, plays Sleep -> Wake, blocks controls and restores physics on the floor.");
            Transform authoredBench=null;DirtmouthCamera authoredCamera=null;
            foreach(var root in previous.GetRootGameObjects())
            {
                foreach(var town in root.GetComponentsInChildren<DirtmouthTown>())if(town.bench!=null && town.bench.gameObject.activeInHierarchy)authoredBench=town.bench;
                foreach(var follow in root.GetComponentsInChildren<DirtmouthCamera>())authoredCamera=follow;
            }
            Require(authoredBench!=null,"Need the visible authored Dirtmouth bench for scene-entry integration validation.");
            var actualSeat=new GameObject("Authored Dirtmouth bench fixture",typeof(SpriteRenderer)){layer=31};
            actualSeat.transform.position=authoredBench.position;actualSeat.transform.localScale=authoredBench.lossyScale;
            var benchRenderer=actualSeat.GetComponent<SpriteRenderer>();benchRenderer.sprite=authoredBench.GetComponent<SpriteRenderer>().sprite;
            benchRenderer.sharedMaterial=renderer.sharedMaterial;benchRenderer.sortingLayerName="Front";benchRenderer.sortingOrder=99;
            var townObject=new GameObject("Authored town fixture");var townFixture=townObject.AddComponent<DirtmouthTown>();townFixture.enabled=false;townFixture.bench=actualSeat.transform;
            var cameraObject=new GameObject("Scene entry follow fixture");cameraObject.SetActive(false);
            var entryCamera=cameraObject.AddComponent<Camera>();entryCamera.enabled=false;entryCamera.orthographic=true;
            entryCamera.orthographicSize=authoredCamera!=null?authoredCamera.GetComponent<Camera>().orthographicSize:5;
            var entryFollow=cameraObject.AddComponent<DirtmouthCamera>();entryFollow.target=actor.transform;
            if(authoredCamera!=null && authoredCamera.cameraColl!=null)
            {
                var bounds=authoredCamera.cameraColl.bounds;
                var boundary=new GameObject("Authored camera boundary fixture",typeof(BoxCollider2D));boundary.transform.position=bounds.center;
                var box=boundary.GetComponent<BoxCollider2D>();box.size=bounds.size;box.isTrigger=true;entryFollow.cameraColl=box;
            }
            cameraObject.transform.position=new Vector3(-26.45f,1.28f,-10);cameraObject.SetActive(true);
            body.position=new Vector2(-26.45f,1.28f);Physics2D.SyncTransforms();
            var authoredWake=(IEnumerator)typeof(PlayerController).GetMethod("EnterFromBench",Private).Invoke(player,null);
            Require(authoredWake.MoveNext(),"Authored entry must begin immediately.");
            Require(Is("BenchSleep") && Mathf.Abs(actor.transform.position.x-benchRenderer.bounds.center.x)<.001f,"First entry yield must already show the knight asleep on the actual authored bench.");
            var viewport=entryCamera.WorldToViewportPoint(actor.transform.position);
            Require(viewport.x>=0 && viewport.x<=1 && viewport.y>=0 && viewport.y<=1,"Scene camera must frame the bench from the first entry yield.");
            Capture("AuthoredBenchSleep",actor.transform.position);
            Require(authoredWake.MoveNext(),"Authored bench must lead to Wake.");animator.Update(player.PoseDuration("BenchWake"));Call(player,"LateUpdate");
            Require(!authoredWake.MoveNext() && !player.IsResting && body.simulated,"Authored bench wake must finish and release controls.");Tick();
            report.Add("PASS: fixture using Dirtmouth bench art/position and camera bounds starts in BenchSleep, frames the knight immediately and restores control after Wake. Real Start lifecycle is covered separately by KnightBenchSceneValidation.");
            player.RestOnBench(seat.transform);animator.Update(0);
            Require(Is("BenchSit") && player.IsResting && !body.simulated,"Rest interaction must start BenchSit.");
            Capture("BenchSit",actor.transform.position);
            Call(player,"OnDisable");Require(!player.IsResting && body.simulated,"Disabling a resting knight must restore physics and clear bench state.");
            report.Add("PASS: resting starts BenchSit, and interruption/disable restores player physics without leaving a frozen knight.");
        }
        catch(Exception e){report.Add("FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(player!=null)Call(player,"OnDisable");
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            SceneManager.SetActiveScene(previous);SceneManager.UnloadSceneAsync(scene);
            File.WriteAllLines("Temp/KnightActionPoseValidation.txt",report);
            if(SessionState.GetBool("ActionPoseTestEnteredPlay",false)){SessionState.SetBool("ActionPoseTestEnteredPlay",false);EditorApplication.delayCall+=EditorApplication.ExitPlaymode;}
        }
    }
    private static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
    private static void ResetDashTest(PlayerController player,Animator animator,Rigidbody2D body,Vector2 position,float input)
    {
        Call(player,"CancelDashPose");
        foreach(string field in new[]{"dashRemaining","hurtRemaining","attackAnimationRemaining","specialJumpRemaining","recoilRemaining","wallLockRemaining","jumpBuffer","holdRemaining","landRemaining"})Set(player,field,0f);
        Set(player,"grounded",false);Set(player,"jumpHeld",false);Set(player,"jumpReleased",false);Set(player,"dashQueued",false);
        Set(player,"remainingDashes",1);Set(player,"moveInput",input);
        player.transform.position=position;body.position=position;body.linearVelocity=Vector2.zero;body.gravityScale=4.84f;Physics2D.SyncTransforms();
        typeof(PlayerController).GetMethod("PlayAnimation",Private).Invoke(player,new object[]{"Fall",true});
        Call(player,"FixedUpdate");animator.Update(.02f);
    }
    private static void ValidateAirDashLanding(PlayerController player,Animator animator,Rigidbody2D body,PhysicsScene2D physics,float height,bool moving)
    {
        ResetDashTest(player,animator,body,new Vector2(0,height),moving?1f:0f);
        Require(!player.IsGrounded && Mathf.Abs(body.position.y-height)<.01f,"Air dash fixture must start at its requested airborne height.");
        typeof(PlayerController).GetMethod("BeginDash",Private).Invoke(player,null);animator.Update(0);
        bool seenFall=false,seenLand=false,landedDuringDash=false,finished=false;
        float mainProgress=0,landProgress=0;
        var trace=new List<string>();int previousState=0;
        for(int i=0;i<160;i++)
        {
            var before=animator.GetCurrentAnimatorStateInfo(0);
            bool wasGrounded=player.IsGrounded;
            Call(player,"FixedUpdate");physics.Simulate(.02f);animator.Update(.02f);
            var info=animator.GetCurrentAnimatorStateInfo(0);
            if(info.shortNameHash!=previousState)
            {
                trace.Add($"t={(i+1)*.02f:F2}, y={body.position.y:F2}, state={info.shortNameHash}, ground={player.IsGrounded}");previousState=info.shortNameHash;
            }
            if(!wasGrounded && player.IsGrounded)
            {
                landedDuringDash=before.IsName("Dash");
            }
            if(info.IsName("Dash"))mainProgress=info.normalizedTime;
            Require(!info.IsName("DashRecover"),"The standing recovery pose must not cover an air dash's Fall animation.");
            if(info.IsName("Fall"))seenFall=true;
            if(info.IsName("SoftLand"))
            {
                Require(mainProgress>=1f,"Contact truncated the main Dash animation.");
                Require(player.IsGrounded,"SoftLand must only play on a real terrain contact.");
                seenLand=true;landProgress=info.normalizedTime;
            }
            else if(seenLand)
            {
                // Animator transitions at exitTime=1 within its current sample.
                Require(landProgress>=.9f,"Dash landing animation was cut short.");
                Require(info.IsName(moving?"Run":"Idle"),"Landing must return to the authored ground state.");finished=true;break;
            }
        }
        Require(seenLand && finished,"Air dash must reach SoftLand and release to locomotion, height="+height);
        if(height<2f)Require(landedDuringDash,"Low-dash test must contact terrain during the full Dash clip.");
        else Require(seenFall,"An air dash must return to Fall before landing: "+string.Join("; ",trace));
    }
    private static void ValidateDashOffLedge(PlayerController player,Animator animator,Rigidbody2D body,PhysicsScene2D physics)
    {
        var lower=new GameObject("Lower landing floor",typeof(BoxCollider2D)){layer=LayerMask.NameToLayer("Terrain")};
        lower.transform.position=new Vector3(25,-2.5f);lower.GetComponent<BoxCollider2D>().size=new Vector2(15,1);
        try
        {
            ResetDashTest(player,animator,body,new Vector2(19,.81f),0);
            typeof(PlayerController).GetMethod("SetFacing",Private).Invoke(player,new object[]{true});
            Require(player.IsGrounded,"Ledge test must start on the upper floor.");
            typeof(PlayerController).GetMethod("BeginDash",Private).Invoke(player,null);animator.Update(0);
            bool airborne=false,landed=false;
            for(int i=0;i<160;i++)
            {
                Call(player,"FixedUpdate");physics.Simulate(.02f);animator.Update(.02f);
                airborne|=!player.IsGrounded;
                if(animator.GetCurrentAnimatorStateInfo(0).IsName("SoftLand")){landed=true;break;}
            }
            Require(airborne && landed,"A dash that leaves a ledge must retain its eventual landing animation.");
        }
        finally{UnityEngine.Object.DestroyImmediate(lower);}
    }
    private static void ValidateDashSequence(PlayerController player,Animator animator,SpriteRenderer renderer,Rigidbody2D body,bool ground,bool solemn,float speed)
    {
        GameObject area=null;
        try
        {
            if(solemn){area=new GameObject("Dash validation solemn area");area.AddComponent<KnightMovementArea>().walkOnly=true;}
            animator.speed=speed;
            Call(player,"CancelDashPose");
            foreach(string field in new[]{"dashRemaining","hurtRemaining","attackAnimationRemaining","specialJumpRemaining","recoilRemaining","wallLockRemaining","jumpBuffer","holdRemaining"})Set(player,field,0f);
            Set(player,"jumpHeld",false);Set(player,"jumpReleased",false);Set(player,"dashQueued",false);Set(player,"remainingDashes",1);
            Set(player,"moveInput",1f);player.transform.position=new Vector2(0,ground ? .81f : 5f);body.position=player.transform.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
            Call(player,"FixedUpdate");animator.Update(.02f);
            var expected=new List<Sprite>();
            foreach(string folder in new[]{"Dash","Dash To Idle"})
            {
                if(!ground && folder=="Dash To Idle")continue;
                bool startup=ground && !solemn && folder=="Dash To Idle";
                var paths=Directory.GetFiles(startup?"Assets/PlayerSprite/motion/Run":"Assets/PlayerSprite/motion/Dash/"+folder,"*.png");Array.Sort(paths,StringComparer.Ordinal);
                for(int i=0;i<(startup?4:paths.Length);i++)expected.Add(AssetDatabase.LoadAssetAtPath<Sprite>(paths[i].Replace('\\','/')));
            }
            typeof(PlayerController).GetMethod("BeginDash",Private).Invoke(player,null);animator.Update(0);
            var seen=new List<Sprite>{renderer.sprite};bool recovering=false,finished=false,physicsReleased=false;
            float lastMain=0,lastRecovery=0,physicalEnd=-1;
            string transition=ground && !solemn?"RunStart":"DashRecover";
            for(int i=0;i<200;i++)
            {
                Call(player,"FixedUpdate");animator.Update(.02f);var info=animator.GetCurrentAnimatorStateInfo(0);
                if(!player.IsDashing && physicalEnd<0)physicalEnd=(i+1)*.02f;
                if(info.IsName("Dash"))
                {
                    Require(!recovering,"Dash startup must not replay after recovery begins.");lastMain=info.normalizedTime;
                    if(!player.IsDashing && body.gravityScale==4.84f)physicsReleased=true;
                }
                else if(info.IsName(transition))
                {
                    if(!recovering)Require(lastMain>=1,"Recovery started before Dash completed.");
                    recovering=true;lastRecovery=info.normalizedTime;
                }
                else
                {
                    Require(ground ? recovering && lastRecovery>=1 : lastMain>=1,"Locomotion interrupted the main/ground recovery clip.");
                    Require(info.IsName(ground?(solemn?"Walk":"Run"):"Fall"),"Wrong locomotion after complete recovery.");finished=true;break;
                }
                if(renderer.sprite!=seen[seen.Count-1])seen.Add(renderer.sprite);
            }
            Require(finished && seen.Count==expected.Count,"Dash sequence did not display all required main/ground recovery frames.");
            for(int i=0;i<seen.Count;i++)Require(seen[i]==expected[i],"Wrong dash sequence frame at "+i);
            float duration=(float)typeof(PlayerController).GetField("dashDuration",Private).GetValue(player);
            Require(physicsReleased && physicalEnd>=duration-.001f && physicalEnd<=duration+.021f,"Animation playback must not extend physical dash time or suppress gravity.");
        }
        finally{animator.speed=1;if(area!=null)UnityEngine.Object.DestroyImmediate(area);}
    }
    private static void Call(object target,string method)=>target.GetType().GetMethod(method,Private).Invoke(target,null);
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Capture(string name,Vector3 centre)
    {
        var go=new GameObject("Action preview camera",typeof(Camera));var camera=go.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=2.4f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.055f,.09f);
        go.transform.position=new Vector3(centre.x,Mathf.Max(1.1f,centre.y-.1f),-10);var target=new RenderTexture(960,540,24);var previous=RenderTexture.active;
        var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            Directory.CreateDirectory("DirtmouthPreviews/Actions");File.WriteAllBytes("DirtmouthPreviews/Actions/"+name+".png",image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
    }
}
