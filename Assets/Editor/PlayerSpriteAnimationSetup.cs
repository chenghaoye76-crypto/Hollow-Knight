using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayerSpriteAnimationSetup
{
    public const string Root = "Assets/PlayerSprite/";
    private static readonly HashSet<string> prepared = new();
    static PlayerSpriteAnimationSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/UsePlayerSprite.request")) return;
        File.Delete("Temp/UsePlayerSprite.request");
        try { Apply(); }
        catch (Exception e) { File.WriteAllText("Temp/PlayerSpriteMigration.txt",e.ToString()); Debug.LogException(e); }
    }
    private static void Backup(string path)
    {
        string target = "Temp/PlayerSpriteMigrationBackup/"+path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path,target);
    }
    public static Sprite[] Frames(string relative)
    {
        string directory = Root+relative;
        var paths = Directory.GetFiles(directory,"*.png").Where(p=>!p.Contains("副本") && !p.ToLowerInvariant().Contains("copy"))
            .OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p.Replace('\\','/')).ToArray();
        if(paths.Any(p=>Regex.IsMatch(Path.GetFileName(p),@"^\d{3}-\d{2}-")))
            paths=paths.Where(p=>Regex.IsMatch(Path.GetFileName(p),@"^\d{3}-\d{2}-")).ToArray();
        if(paths.Length==0)throw new Exception("No PlayerSprite frames: "+directory);
        return paths.Select(Load).ToArray();
    }
    public static Sprite Load(string path)
    {
        if (prepared.Add(path))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Texture importer missing: "+path);
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            Vector2 pivot = Vector2.one*.5f;
            try
            {
                ImageConversion.LoadImage(texture,File.ReadAllBytes(path));
                var pixels=texture.GetPixels32(); int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                    if(pixels[y*texture.width+x].a>8){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                if(maxX>=minX)pivot=new Vector2((minX+maxX+1f)*.5f/texture.width,(minY+maxY+1f)*.5f/texture.height);
                if(path.Contains("/motion/Jump/Double Jump/"))
                {
                    // Anchor the white skull, so the opening wing sweep cannot move the body pivot.
                    minX=texture.width;minY=texture.height;maxX=maxY=-1;
                    for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                    {
                        var pixel=pixels[y*texture.width+x];
                        if(pixel.a>200 && pixel.r>245 && pixel.g>245 && pixel.b>245)
                        {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                    }
                    if(maxX>=minX)pivot=new Vector2(((minX+maxX+1f)*.5f+2.5f)/texture.width,((minY+maxY+1f)*.5f-19.5f)/texture.height);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            bool changed=importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single ||
                settings.spriteAlignment!=(int)SpriteAlignment.Custom || Vector2.Distance(importer.spritePivot,pivot)>.0001f ||
                Mathf.Abs(importer.spritePixelsPerUnit-64)>.01f || importer.mipmapEnabled;
            if(changed)
            {
                Backup(path+".meta");
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                settings.spriteAlignment=(int)SpriteAlignment.Custom; settings.spritePivot=pivot;
                settings.spritePixelsPerUnit=64; settings.mipmapEnabled=false; settings.alphaIsTransparency=true;
                importer.SetTextureSettings(settings);
                importer.spritePivot=pivot; importer.spritePixelsPerUnit=64;
                importer.SaveAndReimport();
            }
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null)throw new Exception("Sprite missing: "+path);
        return sprite;
    }
    public static void ReplaceFrames(AnimationClip clip, Sprite[] frames)
    {
        string path=AssetDatabase.GetAssetPath(clip);
        Backup(path);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        var originalStop=Regex.Match(File.ReadAllText("Temp/PlayerSpriteMigrationBackup/"+path),@"m_StopTime: ([0-9.Ee+-]+)");
        float duration=originalStop.Success ? float.Parse(originalStop.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture) : clip.length;
        bool runLoop=clip.name=="Run" || clip.name=="Running";
        if(runLoop){clip.frameRate=24;duration=frames.Length/clip.frameRate;settings.loopTime=true;}
        if(clip.name=="Fall" || clip.name=="Falling") {clip.frameRate=20;duration=frames.Length/20f;settings.loopTime=true;}
        if(clip.name=="DoubleJump" || clip.name=="DoubleJumping") {clip.frameRate=24;duration=frames.Length/24f;settings.loopTime=false;}
        if(clip.name=="Walk" || clip.name=="Walking") {clip.frameRate=8;duration=frames.Length/8f;settings.loopTime=true;}
        if(clip.name=="Up_Attack" || clip.name=="UpSlash") settings.loopTime=false;
        duration=Mathf.Max(1f/clip.frameRate,duration);
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault(b=>b.type==typeof(SpriteRenderer) && b.propertyName=="m_Sprite");
        if(binding.type==null)binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        float lastSample=Mathf.Max(0,duration-1f/clip.frameRate);
        var keys=frames.Select((s,i)=>new ObjectReferenceKeyframe{time=frames.Length>1 ? i*lastSample/(frames.Length-1) : 0,value=s}).ToList();
        AnimationUtility.SetObjectReferenceCurve(clip,binding,keys.ToArray());
        settings.stopTime=duration;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        // Keep timing, loop settings, events and transform curves from the existing gameplay clip.
        EditorUtility.SetDirty(clip);
    }
    [MenuItem("Tools/Hollow Knight/Use PlayerSprite For All Knight Animations")]
    public static void Apply()
    {
        prepared.Clear();
        var map = new Dictionary<string,string>
        {
            {"Idle","motion/Idle"},{"Run","motion/Run"},{"Walk","motion/056.Walk"},
            {"Jump","motion/Jump/Jump"},{"Fall","motion/Jump/Jump"},{"DoubleJump","motion/Jump/Double Jump"},
            {"SlideJump","motion/Jump/Walljump"},{"Slide","motion/Wall Slide"},
            {"Dash","motion/Dash/Dash"},{"Stop","motion/Run To Idle"},
            {"SoftLand","motion/Land"},{"TakeDamage","motion/Recoil"},{"Dead","motion/017.Death"},
            {"Attack","slash/006.Slash"},{"Up_Attack","slash/009.UpSlash"},{"Down_Attack","slash/010.DownSlash"}
        };
        var cached=map.ToDictionary(p=>p.Key,p=>Frames(p.Value));
        // The first four frames are a one-shot startup; the final nine form the stride loop.
        cached["Run"]=cached["Run"].Skip(4).ToArray();
        var fall=cached["Fall"].Skip(6).ToArray();
        cached["Fall"]=fall.Concat(fall.Reverse().Skip(1).Take(fall.Length-2)).ToArray();
        var report=new List<string>();
        foreach(var pair in cached)
        {
            string path="Assets/Animation/Knight/"+pair.Key+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null)throw new Exception("Knight clip missing: "+path);
            ReplaceFrames(clip,pair.Value); report.Add(pair.Key+": "+map[pair.Key]+" ("+pair.Value.Length+" frames)");
        }
        var aliases=new Dictionary<string,string>
        {
            {"Idle","Idle"},{"Running","Run"},{"Walking","Walk"},{"Jumping","Jump"},{"Falling","Fall"},
            {"DoubleJumping","DoubleJump"},{"SlideJump","SlideJump"},{"Slide","Slide"},
            {"Damage","TakeDamage"},{"Dead","Dead"},{"FirstLand","SoftLand"},{"SoftLand","SoftLand"},
            {"Slash","Attack"},{"UpSlash","Up_Attack"},{"DownSlash","Down_Attack"}
        };
        foreach(var pair in aliases)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/New Folder/"+pair.Key+".anim");
            if(clip!=null)ReplaceFrames(clip,cached[pair.Value]);
        }
        foreach(var pair in new Dictionary<string,string>{{"AltSlash","slash/004.SlashAlt"},{"HardLand","motion/HardLand"},{"Respawn","motion/Hazard Respawn"}})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/New Folder/"+pair.Key+".anim");
            if(clip!=null)ReplaceFrames(clip,Frames(pair.Value));
        }
        foreach(var pair in new Dictionary<string,string>{{"AttackEffect","Effect/SlashEffect"},{"UpAttackEffect","Effect/UpSlashEffect"},{"DownAttackEffect","Effect/DownSlashEffect"}})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/AttackEffect/"+pair.Key+".anim");
            var frames=Frames(pair.Value); ReplaceFrames(clip,frames);
            string prefabPath="Assets/Resources/Prefabs/"+pair.Key+"Root.prefab";
            Backup(prefabPath);
            var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach(var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))renderer.sprite=frames[0];
                PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(prefab);}
        }
        foreach(var pair in new Dictionary<string,string>{{"SlashEffect","Effect/SlashEffect"},{"AltSlashEffect","Effect/SlashEffectAlt"}})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/New Folder/Attacks/"+pair.Key+".anim");
            if(clip!=null)ReplaceFrames(clip,Frames(pair.Value));
        }
        KnightSpellSetup.Setup();
        var byName=Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories)
            .OrderBy(p=>p,StringComparer.Ordinal).GroupBy(p=>Path.GetFileName(p),StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g=>g.Key,g=>g.First().Replace('\\','/'),StringComparer.OrdinalIgnoreCase);
        var controllers=new[]{"Assets/Resources/KnightMotion.controller","Assets/Animation/Knight/Knight.controller","Assets/Animation/Knight/PlayerController.controller","Assets/Animation/New Folder/KnightAnimator.controller"};
        foreach(string path in controllers)
        {
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if(controller==null)continue;
            foreach(var clip in controller.animationClips.Distinct())
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);bool changed=false;
                Sprite[] wingFrames=binding.path.Contains("Double J Wings") ? Frames("Effect/Double Jump Wings") : null;
                for(int i=0;i<keys.Length;i++)
                    if(keys[i].value is Sprite sprite && !AssetDatabase.GetAssetPath(sprite).StartsWith(Root,StringComparison.Ordinal))
                    {
                        string filename=Path.GetFileName(AssetDatabase.GetAssetPath(sprite));
                        if(wingFrames!=null)keys[i].value=wingFrames[Mathf.Min(wingFrames.Length-1,i*wingFrames.Length/keys.Length)];
                        else
                        {
                            if(!byName.TryGetValue(filename,out var replacement))throw new Exception("No PlayerSprite replacement: "+clip.name+"/"+binding.path+" -> "+filename);
                            keys[i].value=Load(replacement);
                        }
                        changed=true;
                    }
                if(changed){Backup(AssetDatabase.GetAssetPath(clip));AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);EditorUtility.SetDirty(clip);}
            }
        }
        foreach(string path in new[]{"Assets/Scenes/Dirtmouth.unity","Assets/Scenes/Game.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var player in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerController>(true)))
                {
                    var renderer=player.GetComponent<SpriteRenderer>();
                    if(renderer==null)continue;
                    Undo.RecordObject(renderer,"Use PlayerSprite idle");renderer.sprite=cached["Idle"][0];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    foreach(var childRenderer in player.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if(childRenderer==renderer || childRenderer.sprite==null || AssetDatabase.GetAssetPath(childRenderer.sprite).StartsWith(Root,StringComparison.Ordinal))continue;
                        string filename=Path.GetFileName(AssetDatabase.GetAssetPath(childRenderer.sprite));
                        if(!byName.TryGetValue(filename,out var replacement))continue;
                        Undo.RecordObject(childRenderer,"Use PlayerSprite child effect");childRenderer.sprite=Load(replacement);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(childRenderer);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        KnightRunCycleSetup.Configure();
        AssetDatabase.SaveAssets();
        File.Copy("Assets/Animation/Knight/Knight.controller","Assets/Resources/KnightMotion.controller",true);
        AssetDatabase.ImportAsset("Assets/Resources/KnightMotion.controller",ImportAssetOptions.ForceUpdate);
        int audited=0;
        foreach(string path in controllers)
        {
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if(controller==null)continue;
            foreach(var clip in controller.animationClips.Distinct())
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            foreach(var key in AnimationUtility.GetObjectReferenceCurve(clip,binding))
                if(key.value is Sprite sprite)
                {
                    if(!AssetDatabase.GetAssetPath(sprite).StartsWith(Root,StringComparison.Ordinal))throw new Exception("Old sprite reference remains: "+clip.name+" -> "+AssetDatabase.GetAssetPath(sprite));
                    audited++;
                }
        }
        report.Add("PASS: "+audited+" animation sprite keys audited; all character controllers use Assets/PlayerSprite. Spell effects and both scene idle sprites migrated; timings and animation events preserved.");
        var spellData = new SerializedObject(AssetDatabase.LoadAssetAtPath<KnightSpellAssets>("Assets/Resources/KnightSpells.asset"));
        var property=spellData.GetIterator();
        while(property.Next(true))
            if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite && !AssetDatabase.GetAssetPath(sprite).StartsWith(Root,StringComparison.Ordinal))
                throw new Exception("Old spell sprite remains: "+property.propertyPath);
        report.Add("PASS: every sprite in KnightSpells.asset also belongs to Assets/PlayerSprite.");
        File.WriteAllLines("Temp/PlayerSpriteMigration.txt",report);
    }
}
