using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightSpellValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static KnightSpellValidation() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || !File.Exists("Temp/ValidateKnightSpells.request")) return;
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool("KnightSpellsValidationEnteredPlay", true);
                EditorApplication.EnterPlaymode();
            }
            return;
        }
        File.Delete("Temp/ValidateKnightSpells.request"); Validate();
        if (SessionState.GetBool("KnightSpellsValidationEnteredPlay", false))
        {
            SessionState.SetBool("KnightSpellsValidationEnteredPlay", false);
            EditorApplication.delayCall += EditorApplication.ExitPlaymode;
        }
    }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    [MenuItem("Tools/Hollow Knight/Validate Knight Spells")]
    public static void Validate()
    {
        var report = new List<string>();
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.CreateScene("KnightSpellValidation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        var physics = scene.GetPhysicsScene2D();
        Keyboard keyboard = null;
        try
        {
            SceneManager.SetActiveScene(scene);
            keyboard = InputSystem.AddDevice<Keyboard>();
            void FocusKey(bool pressed)
            {
                var control = (UnityEngine.InputSystem.Controls.KeyControl)keyboard[GameOptions.Key(7)];
                InputSystem.QueueStateEvent(keyboard, pressed ? new KeyboardState(control.keyCode) : new KeyboardState()); InputSystem.Update();
            }
            var floor = new GameObject("Test floor") { layer = LayerMask.NameToLayer("Terrain") };
            floor.transform.position = new Vector3(0, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(40,1);
            var go = new GameObject("Spell test knight", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator));
            go.transform.position = new Vector3(0,.81f);
            var body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 4.84f; body.constraints = RigidbodyConstraints2D.FreezeRotation;
            go.GetComponent<BoxCollider2D>().size = new Vector2(.8f,1.6f);
            var health = go.AddComponent<PlayerHealth>();
            var player = go.AddComponent<PlayerController>();
            var soul = go.GetComponent<PlayerSoul>(); var spells = go.GetComponent<KnightSpells>();
            var assets = Resources.Load<KnightSpellAssets>("KnightSpells");
            go.GetComponent<SpriteRenderer>().sharedMaterial = assets.material;
            go.GetComponent<SpriteRenderer>().sortingLayerName = "Front";
            go.GetComponent<SpriteRenderer>().sortingOrder = 100;
            go.GetComponent<Animator>().Rebind(); go.GetComponent<Animator>().Update(0);
            void Reset()
            {
                spells.Cancel(); Set(spells,"invulnerableUntil",0f);
                foreach (var visual in UnityEngine.Object.FindObjectsByType<KnightSpellVisual>(FindObjectsSortMode.None))
                    if (visual.gameObject.scene == scene) UnityEngine.Object.DestroyImmediate(visual.gameObject);
                Set(health,"currentHealth",health.MaxHealth);
                foreach (var field in new[]{"hurtRemaining","attackAnimationRemaining","dashRemaining","jumpBuffer","holdRemaining","recoilRemaining","wallLockRemaining"}) Set(player,field,0f);
                Set(player,"facingRight",true); Set(player,"grounded",true);
                soul.Set(99); body.position = new Vector2(0,.81f); body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
            }
            TiktikHealth Enemy(string name, Vector2 position)
            {
                var target = new GameObject(name, typeof(Rigidbody2D), typeof(Animator));
                target.transform.position = position; target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                target.AddComponent<BoxCollider2D>().size = new Vector2(.7f,.7f);
                target.AddComponent<EnemyController>().enabled = false;
                var life = target.AddComponent<TiktikHealth>(); Set(life,"currentHealth",300);
                var extra = new GameObject("Second enemy collider"); extra.transform.SetParent(target.transform,false); extra.AddComponent<CircleCollider2D>().radius=.3f;
                return life;
            }
            Reset();
            var enemyA = Enemy("Enemy A", new Vector2(3,.81f));
            var enemyB = Enemy("Enemy B", new Vector2(6,.81f));
            Physics2D.SyncTransforms();
            soul.Set(20); Require(!spells.TryCast(KnightSpells.Spell.ShadeSoul) && soul.Current == 20, "Insufficient soul should reject a spell without consumption.");
            soul.Add(500); Require(soul.Current == 99,"Soul must clamp to capacity.");
            soul.Set(0); player.DealNormalAttackDamage(enemyA.GetComponent<EnemyController>());
            Require(soul.Current==11,"Nail hit should collect 11 soul.");
            int afterNail = enemyA.CurrentHealth;
            soul.Set(99); Require(spells.TryCast(KnightSpells.Spell.ShadeSoul),"Shade Soul could not start.");
            Require(soul.Current==66 && !spells.TryCast(KnightSpells.Spell.AbyssShriek),"Casting should spend once and lock out concurrent spells.");
            spells.TickPhysics(.1f);
            var projectile = UnityEngine.Object.FindAnyObjectByType<KnightSoulProjectile>();
            Require(projectile != null,"Shade Soul projectile missing.");
            var wall = new GameObject("Projectile penetrates wall") { layer=LayerMask.NameToLayer("Terrain") };
            wall.transform.position = new Vector3(4,.8f); wall.AddComponent<BoxCollider2D>().size = new Vector2(.5f,3);
            Physics2D.SyncTransforms();
            for (int i=0;i<16;i++) Call(projectile,"FixedUpdate");
            Require(enemyA.CurrentHealth==afterNail-30 && enemyB.CurrentHealth==270,"Projectile must penetrate walls/enemies, with one hit per multi-collider enemy.");
            Require(soul.Current==66,"Spells must not generate soul.");
            report.Add("PASS: soul capacity, insufficient-soul rejection, nail soul gain, cast lock, projectile piercing and collider deduplication.");
            UnityEngine.Object.DestroyImmediate(projectile.gameObject); UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(enemyA.gameObject); UnityEngine.Object.DestroyImmediate(enemyB.gameObject);

            Reset(); FocusKey(true); Set(health,"currentHealth",health.MaxHealth-2);
            Require(spells.BeginFocus(),"Focus could not begin on ground.");
            Capture(scene, go, "Focus");
            for(int i=0;i<45;i++) spells.TickPhysics(.02f);
            Require(health.CurrentHealth==health.MaxHealth-1 && soul.Current==66,"Focus should heal one mask for 33 soul.");
            spells.TickPhysics(.2f); int partial = soul.Current;
            FocusKey(false); spells.ReadInput();
            Require(!spells.IsBusy && soul.Current==partial && health.CurrentHealth==health.MaxHealth-1,"Releasing focus should cancel without healing/refunding spent soul.");
            FocusKey(true); Require(spells.BeginFocus(),"Focus restart failed.");
            spells.TickPhysics(.25f); health.TakeDamage(1);
            Require(!spells.IsBusy,"Damage must interrupt focus.");
            FocusKey(false);
            report.Add("PASS: focus heals, updates health events, drains soul, and cancels on release/damage without refunds.");

            Reset(); var above = Enemy("Above shriek target",new Vector2(0,3));
            var below = Enemy("Below shriek target",new Vector2(0,-3));
            Physics2D.SyncTransforms(); Require(spells.TryCast(KnightSpells.Spell.AbyssShriek),"Shriek failed to start.");
            for(int i=0;i<4;i++) spells.TickPhysics(.12f);
            Capture(scene,go,"AbyssShriek");
            Require(above.CurrentHealth==220 && below.CurrentHealth==300 && soul.Current==66,"Shriek should hit above four times for 20 damage each.");
            spells.TickPhysics(.3f); Require(!spells.IsBusy && Mathf.Abs(body.gravityScale-4.84f)<.01f,"Shriek must restore movement/gravity.");
            UnityEngine.Object.DestroyImmediate(above.gameObject); UnityEngine.Object.DestroyImmediate(below.gameObject);
            report.Add("PASS: Abyss Shriek four pulses, upper damage area, one soul charge, and gravity restoration.");

            Reset(); var side = Enemy("Dive impact target",new Vector2(2.3f,.8f));
            var fragile = new GameObject("Fragile structure"); fragile.transform.position=new Vector3(2,.2f);
            var fragileCollider = fragile.AddComponent<BoxCollider2D>(); fragileCollider.isTrigger=true;
            fragile.AddComponent<SpellBreakable>();
            Physics2D.SyncTransforms(); Require(spells.TryCast(KnightSpells.Spell.DescendingDark),"Dive failed to start.");
            for(int i=0;i<60 && spells.IsBusy;i++) { Call(player,"FixedUpdate"); physics.Simulate(.02f); Physics2D.SyncTransforms(); }
            Capture(scene,go,"DescendingDark");
            Require(!spells.IsBusy && soul.Current==66,"Dive should land, recover and charge once.");
            Require(side.CurrentHealth==255,"Dive landing waves should cause 30 + 15 damage.");
            Require(!fragileCollider.enabled,"Dive should break opted-in fragile structures.");
            Require(floor.GetComponent<Collider2D>().enabled,"Ordinary floor must remain intact.");
            int hp = health.CurrentHealth; health.TakeDamage(1); Require(health.CurrentHealth==hp,"Dive should grant short landing invulnerability.");
            Require(Mathf.Abs(body.gravityScale-4.84f)<.01f,"Dive must restore gravity.");
            UnityEngine.Object.DestroyImmediate(side.gameObject);
            report.Add("PASS: grounded dive launches, falls and lands; two damage waves, temporary invulnerability, fragile-structure break and ordinary-ground preservation.");

            Reset();
            void Keys(params string[] names)
            {
                var keys = Array.ConvertAll(names, name => ((UnityEngine.InputSystem.Controls.KeyControl)keyboard[name]).keyCode);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); Call(player,"Update");
            }
            string StateName() => typeof(KnightSpells).GetField("state",Private).GetValue(spells).ToString();
            Keys(GameOptions.Key(2),GameOptions.Key(11));
            Require(StateName()=="Shriek" && soul.Current==66,"Up + rebound Quick Cast must select Shriek.");
            Keys(); Reset(); Keys(GameOptions.Key(3),GameOptions.Key(11));
            Require(StateName()=="DiveCharge","Down + rebound Quick Cast must select Descending Dark.");
            Keys(); Reset(); Keys(GameOptions.Key(7));
            Require(!spells.IsBusy,"Neutral Focus press should wait to distinguish tap from hold.");
            Keys(); Require(StateName()=="Soul","Neutral Focus tap must cast Shade Soul on release.");
            Reset(); Set(health,"currentHealth",health.MaxHealth-1);
            Keys(GameOptions.Key(7)); Set(spells,"heldTime",.26f); InputSystem.Update(); Call(player,"Update");
            Require(spells.IsFocusing,"Holding configured Focus key must start healing.");
            Keys(); Require(!spells.IsBusy,"Releasing a Focus hold must cancel instead of firing a spell.");
            Reset(); float oldTimeScale=Time.timeScale;
            try { Time.timeScale=0; Keys(GameOptions.Key(11)); Require(!spells.IsBusy && soul.Current==99,"Paused input must not spend soul."); }
            finally { Time.timeScale=oldTimeScale; }
            Keys();
            report.Add("PASS: real configured up/down/quick-cast inputs, tap/hold distinction, release cancellation and paused input.");

            Reset(); Require(spells.TryCast(KnightSpells.Spell.ShadeSoul),"Final cast failed."); spells.TickPhysics(.1f);
            Capture(scene,go,"ShadeSoul");
            player.enabled=false;
            Require(!spells.IsBusy && Mathf.Abs(body.gravityScale-4.84f)<.01f,"Disabling player must release spell state.");
            report.Add("PASS: disabled-player cleanup; all four spell previews rendered.");
        }
        catch(Exception e) { report.Add("FAIL: "+e); Debug.LogException(e); }
        finally
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            SceneManager.SetActiveScene(previous);
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
            Directory.CreateDirectory("DirtmouthPreviews/Spells");
            File.WriteAllLines("Temp/KnightSpellValidation.txt",report);
            File.WriteAllLines("DirtmouthPreviews/Spells/validation.txt",report);
        }
    }
    private static void Capture(Scene scene, GameObject knight, string name)
    {
        void Layer(Transform root) { root.gameObject.layer=31;foreach(Transform child in root)Layer(child); }
        Layer(knight.transform);
        foreach(var visual in UnityEngine.Object.FindObjectsByType<KnightSpellVisual>(FindObjectsSortMode.None))
            if(visual.gameObject.scene==scene)Layer(visual.transform);
        var source=Camera.main;
        if(source==null)throw new Exception("Main camera missing for spell preview.");
        var cameraObject=UnityEngine.Object.Instantiate(source.gameObject);
        cameraObject.tag="Untagged";
        foreach(var script in cameraObject.GetComponents<MonoBehaviour>())script.enabled=false;
        var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
        camera.transform.position=new Vector3(knight.transform.position.x+1.5f,2.3f,-10);
        camera.orthographic=true;camera.orthographicSize=4.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.07f);
        var target=RenderTexture.GetTemporary(960,540,24);var active=RenderTexture.active;
        var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try
        {
            knight.GetComponent<Animator>().Update(.08f);
            var actorRenderer = knight.GetComponent<SpriteRenderer>();
            File.AppendAllText("Temp/KnightSpellCapture.txt", name+" sprite="+(actorRenderer.sprite != null ? actorRenderer.sprite.name : "NULL")+" bounds="+actorRenderer.bounds+" color="+actorRenderer.color+" enabled="+actorRenderer.enabled+" controller="+knight.GetComponent<Animator>().runtimeAnimatorController+"\n");
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            Directory.CreateDirectory("DirtmouthPreviews/Spells");File.WriteAllBytes("DirtmouthPreviews/Spells/"+name+".png",image.EncodeToPNG());
        }
        finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cameraObject);}
    }
}
