using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KnightStateMachineValidation
{
    static KnightStateMachineValidation() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/ValidateKnightStateMachine.request")) return;
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool("KnightStateMachineTestEnteredPlay", true);
                EditorApplication.EnterPlaymode();
            }
            return;
        }
        File.Delete("Temp/ValidateKnightStateMachine.request");
        GameObject go = null;
        try
        {
            var original = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Knight/Knight.controller");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Require(player != null && player.GetComponent<Animator>().runtimeAnimatorController == original, "Scene knight must retain its original controller at runtime.");
            go = new GameObject("Original Animator validation", typeof(SpriteRenderer), typeof(Animator));
            var animator = go.GetComponent<Animator>();
            animator.runtimeAnimatorController = original; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.SetBool("IsOnGround", true); animator.Update(0);
            void Step(float seconds) { for (int i=0;i<Mathf.CeilToInt(seconds/.01f);i++) animator.Update(.01f); }
            bool Is(string name) => animator.GetCurrentAnimatorStateInfo(0).IsName(name);
            Require(Is("Idle"), "Original default state must be Idle.");
            ValidateRunCycle(animator,go.GetComponent<SpriteRenderer>());
            animator.SetInteger("MoveChange", 1); Step(.02f);
            Require(Is("Walk"), "Movement must enter original Walk startup.");
            Step(2); Require(Is("Run"), "Walk must transition to Run without code playing Run.");
            animator.SetBool("WalkOnly",true); animator.SetFloat("WalkMode",1); Step(.02f); Require(Is("Walk"),"Entering a solemn area must switch Run to Walk.");
            ValidateSolemnWalk(animator,go.GetComponent<SpriteRenderer>());
            Step(2); Require(Is("Walk"),"Solemn movement must remain walking for multiple cycles.");
            animator.SetBool("WalkOnly",false); animator.SetFloat("WalkMode",0); Step(2); Require(Is("Run"),"Leaving a solemn area must restore Run.");
            animator.SetInteger("MoveChange", 0); Step(.02f);
            Require(Is("Stop"), "Release must enter original Stop.");
            Step(2); Require(Is("Idle"), "Stop must return to Idle.");
            animator.SetBool("IsOnGround",false); animator.SetBool("IsFall",false); animator.SetTrigger("Jump"); Step(.02f);
            Require(Is("Jump"), "Jump trigger must enter nested Jump.");
            animator.SetBool("DoubleJumpActive",true);animator.SetTrigger("DoubleJump"); Step(.02f);
            Require(Is("DoubleJump"), "Early double jump must interrupt Jump immediately, without waiting for exit time.");
            Step(.12f); Require(Is("DoubleJump"), "Second jump must play one wingbeat while ascending.");
            animator.SetBool("IsFall",true); Step(.1f); Require(Is("DoubleJump"), "Descending must not truncate the wingbeat.");
            Step(.2f);animator.SetBool("DoubleJumpActive",false);Step(.02f);Require(Is("Fall"),"Completed wingbeat must enter Fall.");
            animator.SetBool("IsFall",false); animator.SetBool("DoubleJumpActive",true);animator.SetTrigger("DoubleJump"); Step(.02f);
            Require(Is("DoubleJump"), "Double jump must also respond immediately from Fall.");
            animator.SetBool("IsFall",true); Step(.02f);
            Require(Is("DoubleJump"), "Early descent must preserve the full wingbeat.");
            Step(.34f);animator.SetBool("DoubleJumpActive",false);Step(.02f);Require(Is("Fall"),"Finished DoubleJump must release the pose gate.");
            animator.SetBool("IsOnGround",true); animator.SetBool("IsFall",false); Step(.02f);
            Require(Is("SoftLand"), "Landing must enter nested SoftLand.");
            Step(2); Require(Is("Idle"), "Landing must return to original Idle.");
            var upClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Knight/Up_Attack.anim");
            var spritesSeen=new HashSet<Sprite>();
            animator.SetBool("IsOnGround",false); animator.SetBool("IsFall",true);
            animator.Play("Base Layer.Attack State Machine.Up_Attack",0,0); animator.Update(0);
            for(float elapsed=0;elapsed<upClip.length-.01f;elapsed+=.005f)
            {
                animator.Update(.005f);
                Require(Is("Up_Attack"),"Descending must not interrupt upward slash before its final frame.");
                spritesSeen.Add(go.GetComponent<SpriteRenderer>().sprite);
            }
            Require(spritesSeen.Count==5,"Descending upward slash must display all five PlayerSprite frames.");
            Step(.06f); Require(Is("Fall"),"Completed airborne upward slash must return directly to Fall.");
            animator.Play("Base Layer.Attack State Machine.Up_Attack",0,0); Step(.05f);
            animator.SetBool("IsOnGround",true); animator.SetBool("IsFall",false); Step(.15f);
            Require(Is("Up_Attack"),"Landing midway through upward slash must preserve playback.");
            Step(upClip.length); Require(Is("Idle"),"Landed upward slash must return to Idle after completion.");
            animator.SetBool("IsOnGround",false);animator.SetBool("IsFall",true);animator.SetBool("IsDash",true);animator.SetInteger("MoveChange",1);
            animator.Play("Base Layer.Dash",0,0);Step(.35f);
            Require(Is("Dash"),"Airborne Dash must not be interrupted by IsFall or held movement input.");
            animator.SetBool("IsDash",false);Step(.02f);Require(Is("Fall"),"Finished airborne Dash must return to Fall.");
            foreach(bool solemn in new[]{false,true})
            {
                animator.SetBool("WalkOnly",solemn);animator.SetFloat("WalkMode",solemn?1:0);animator.SetBool("IsOnGround",true);animator.SetBool("IsFall",false);animator.SetBool("IsDash",true);
                animator.Play("Base Layer.Dash",0,0);Step(.35f);Require(Is("Dash"),"Ground Dash must remain active until the dash timer ends.");
                animator.SetBool("IsDash",false);Step(.02f);Require(Is(solemn?"Walk":"Run"),"Finished ground Dash must select the correct walking/running animation.");
            }
            animator.SetBool("WalkOnly",false);animator.SetFloat("WalkMode",0);animator.SetInteger("MoveChange",0);Step(2);
            foreach (string attack in new[]{"Attack","Up_Attack","Down_Attack"})
            {
                animator.Play("Base Layer.Attack State Machine."+attack,0,0); Step(.01f);
                Require(Is(attack),"Nested attack missing: "+attack); Step(2);
                Require(Is("Idle"),"Attack must exit using original transitions: "+attack);
            }
            ValidateSpeeds(original);
            File.WriteAllText("Temp/KnightStateMachineValidation.txt", "PASS: airborne/ground Dash remains active until IsDash clears, then selects Fall/Run/solemn Walk correctly. Solemn walk loops, Run 4+9 cycles, 8.3/6 ground/air speeds, DoubleJump/descent and complete upward attacks pass.");
        }
        catch(Exception e) { File.WriteAllText("Temp/KnightStateMachineValidation.txt", "FAIL: "+e); Debug.LogException(e); }
        finally
        {
            if(go!=null) UnityEngine.Object.DestroyImmediate(go);
            if(SessionState.GetBool("KnightStateMachineTestEnteredPlay",false))
            {
                SessionState.SetBool("KnightStateMachineTestEnteredPlay",false);
                EditorApplication.delayCall += EditorApplication.ExitPlaymode;
            }
        }
    }
    private static void ValidateSolemnWalk(Animator animator,SpriteRenderer renderer)
    {
        var paths=Directory.GetFiles("Assets/PlayerSprite/motion/056.Walk","*.png");Array.Sort(paths,StringComparer.Ordinal);
        var expected=Array.ConvertAll(paths,p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/')));
        animator.Play("Base Layer.Walk",0,0);animator.Update(0);
        var seen=new List<Sprite>{renderer.sprite};
        for(int i=0;i<310;i++)
        {
            animator.Update(.005f);
            Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Solemn walking must not enter Run.");
            if(renderer.sprite!=seen[seen.Count-1])seen.Add(renderer.sprite);
        }
        Require(seen.Count>=12,"Solemn walking must continue animating for three full cycles.");
        for(int i=0;i<seen.Count;i++)Require(seen[i]==expected[i%expected.Length],"Wrong solemn walk frame at "+i);
    }
    private static void ValidateRunCycle(Animator animator,SpriteRenderer renderer)
    {
        var paths=Directory.GetFiles("Assets/PlayerSprite/motion/Run","*.png");Array.Sort(paths,StringComparer.Ordinal);
        var expected=Array.ConvertAll(paths,p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/')));
        var seen=new List<Sprite>();
        animator.SetInteger("MoveChange",1);
        for(int i=0;i<290;i++)
        {
            animator.Update(.005f);var sprite=renderer.sprite;
            if(seen.Count==0 && sprite!=expected[0])continue;
            if(seen.Count==0 || sprite!=seen[seen.Count-1])seen.Add(sprite);
        }
        Require(seen.Count>=31,"Run must render the startup and three full nine-frame cycles.");
        for(int i=0;i<seen.Count;i++)Require(seen[i]==expected[i<4?i:4+(i-4)%9],"Wrong Run frame at sample "+i+": "+seen[i]);
        animator.SetInteger("MoveChange",0);for(int i=0;i<200;i++)animator.Update(.01f);
        animator.SetInteger("MoveChange",1);animator.Update(.01f);
        Require(renderer.sprite==expected[0],"Moving again must replay the four-frame startup.");
        animator.SetInteger("MoveChange",0);for(int i=0;i<200;i++)animator.Update(.01f);
    }
    private static void ValidateSpeeds(RuntimeAnimatorController original)
    {
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Knight Speed Validation",new UnityEngine.SceneManagement.CreateSceneParameters(UnityEngine.SceneManagement.LocalPhysicsMode.Physics2D));
        GameObject actor=null,floor=null,area=null;
        try
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            floor=new GameObject("Speed test floor",typeof(BoxCollider2D));floor.layer=LayerMask.NameToLayer("Terrain");floor.transform.position=new Vector3(0,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(20,1);
            actor=new GameObject("Speed test knight",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(Animator));
            var body=actor.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=4.84f;
            actor.GetComponent<BoxCollider2D>().size=new Vector2(.8f,1.6f);actor.GetComponent<Animator>().runtimeAnimatorController=original;
            var controller=actor.AddComponent<PlayerController>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            void CheckSpeed(float height,float input,float expected)
            {
                body.position=new Vector2(0,height);body.linearVelocity=Vector2.zero;
                typeof(PlayerController).GetField("moveInput",flags).SetValue(controller,input);Physics2D.SyncTransforms();
                typeof(PlayerController).GetMethod("FixedUpdate",flags).Invoke(controller,null);
                Require(Mathf.Abs(body.linearVelocity.x-input*expected)<.001f,"Wrong actual horizontal speed at height "+height+": "+body.linearVelocity.x);
                Require(controller.IsGrounded==(height<1),"Speed validation must cover actual ground/air detection.");
            }
            CheckSpeed(.81f,1,8.3f);CheckSpeed(5,-1,8.3f);
            area=new GameObject("Solemn test area");var settings=area.AddComponent<KnightMovementArea>();settings.walkOnly=true;
            CheckSpeed(.81f,-1,6);CheckSpeed(5,1,6);
            settings.enabled=false;CheckSpeed(5,-1,8.3f);CheckSpeed(.81f,1,8.3f);
        }
        finally
        {
            foreach(var item in new[]{actor,floor,area})if(item!=null)UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        }
    }
    private static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
}
