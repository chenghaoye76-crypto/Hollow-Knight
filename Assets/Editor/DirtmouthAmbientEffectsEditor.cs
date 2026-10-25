using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DirtmouthAmbientEffectsEditor
{
    private const string Request = "Temp/ApplyDirtmouthAtmosphere.request";
    static DirtmouthAmbientEffectsEditor()
    {
        EditorApplication.update += Check;
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.ExitingEditMode || state==PlayModeStateChange.ExitingPlayMode)
                foreach(var fx in UnityEngine.Object.FindObjectsByType<DirtmouthAmbientEffects>(FindObjectsSortMode.None)) fx.SuspendRendering();
            if(state==PlayModeStateChange.EnteredEditMode || state==PlayModeStateChange.EnteredPlayMode)
                EditorApplication.delayCall += () =>
                {
                    foreach(var fx in UnityEngine.Object.FindObjectsByType<DirtmouthAmbientEffects>(FindObjectsSortMode.None)) fx.Rebuild();
                };
        };
        EditorSceneManager.sceneSaving += (scene, path) =>
        {
            foreach(var fx in Effects(scene)) fx.SuspendRendering();
        };
        EditorSceneManager.sceneSaved += scene => EditorApplication.delayCall += () =>
        {
            if(scene.IsValid() && scene.isLoaded) foreach(var fx in Effects(scene)) fx.Rebuild();
        };
    }
    private static DirtmouthAmbientEffects[] Effects(Scene scene) => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DirtmouthAmbientEffects>(true)).ToArray();
    private static void Check()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Apply(); }
        catch(Exception e) { File.WriteAllText("Temp/DirtmouthAtmosphere-result.txt",e.ToString());Debug.LogException(e); }
    }
    [MenuItem("Tools/Hollow Knight/Apply Dirtmouth Atmosphere")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Apply atmosphere outside Play Mode.");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if(File.Exists("Temp/ReloadDirtmouthAtmosphere.request") && scene.IsValid() && scene.isLoaded && !scene.isDirty)
        {
            File.Delete("Temp/ReloadDirtmouthAtmosphere.request");
            scene=EditorSceneManager.OpenScene("Assets/Scenes/Dirtmouth.unity",OpenSceneMode.Single);
        }
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene("Assets/Scenes/Dirtmouth.unity",OpenSceneMode.Additive);
        foreach(string path in new[]{"Assets/Resources/DirtmouthMist.shader","Assets/Resources/DirtmouthDistanceBlur.shader"})
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
            if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Shader failed: "+path);
        }
        var root=scene.GetRootGameObjects().FirstOrDefault(r=>r.GetComponent<DirtmouthAmbientEffects>()!=null);
        if(root==null) {root=new GameObject("Dirtmouth atmosphere - mist, bench motes and white moths");SceneManager.MoveGameObjectToScene(root,scene);}
        var fx=root.GetComponent<DirtmouthAmbientEffects>() ?? root.AddComponent<DirtmouthAmbientEffects>();
        fx.SuspendRendering();
        fx.sceneryMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
        foreach(var sr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)))
            if(sr.sharedMaterial==null) sr.sharedMaterial=fx.sceneryMaterial;
        fx.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        fx.Rebuild();fx.Tick(9f);
        var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).First(c=>c.CompareTag("MainCamera"));
        var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        Vector3 cameraPosition=camera.transform.position;
        var visibleBench=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>()).FirstOrDefault(sr=>sr.sprite!=null && sr.sprite.name=="town_bench");
        if(visibleBench!=null) camera.transform.position=new Vector3(visibleBench.bounds.center.x-0.5f,visibleBench.bounds.min.y+5.7f,cameraPosition.z);
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        float fogDifference=0;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            Directory.CreateDirectory("DirtmouthPreviews");File.WriteAllBytes("DirtmouthPreviews/08-Atmosphere.png",image.EncodeToPNG());
            // Isolate fog motion: all the other animated effect batches are hidden in both frames.
            var otherBatches=fx.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name!="Drifting blue ground mist").ToArray();
            foreach(var r in otherBatches)r.enabled=false;
            try
            {
                Color32[] first=null;
                Directory.CreateDirectory("DirtmouthPreviews/FogFlow");
                for(int frame=0;frame<16;frame++)
                {
                    fx.Tick(9+frame*0.25f);camera.Render();RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                    File.WriteAllBytes($"DirtmouthPreviews/FogFlow/{frame:00}.png",image.EncodeToPNG());
                    if(frame==0)first=image.GetPixels32();
                    if(frame==12)
                    {
                        var last=image.GetPixels32();long delta=0;
                        for(int y=130;y<460;y++)for(int x=100;x<1500;x++)
                        {int k=y*1600+x;delta+=Math.Abs(first[k].r-last[k].r)+Math.Abs(first[k].g-last[k].g)+Math.Abs(first[k].b-last[k].b);}
                        fogDifference=(float)delta/(330*1400*3*255);
                    }
                }
            }
            finally {foreach(var r in otherBatches)r.enabled=true;fx.Tick(9f);}
            if(fx.fogFlowSpeed>0 && fogDifference<0.0007f)throw new Exception("Fog motion not visible in rendered frames: "+fogDifference);
        }
        finally {camera.targetTexture=previous;camera.transform.position=cameraPosition;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
        string report="PASS: both atmosphere shaders compiled; scene saved with effect component; preview rendered.\n";
        report+="Scene: "+scene.path+"\nBlur: "+fx.distantBlur+" texels; animated ground mist; 24 rising bench motes; four white moths per lamp bulb.\n";
        report+=$"PASS: fog-only rendered pixel difference at +3 seconds = {fogDifference:P2}; motes and moths excluded. 16 flow preview frames exported.\n";
        foreach(var sr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)))
            if(sr.sprite != null && (sr.sprite.name.ToLowerInvariant().Contains("bench") || sr.sprite.name.ToLowerInvariant().Contains("lamp") || sr.name.Contains("hazy")))
                report+=$"{sr.name} | {sr.sprite.name} | active={sr.gameObject.activeInHierarchy} | order={sr.sortingOrder} layer={sr.sortingLayerName} | bounds={sr.bounds}\n";
        report+="Camera: "+camera.transform.position+" size="+camera.orthographicSize+"\n";
        File.WriteAllLines("Temp/Dirtmouth-sprites.txt",scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>()).Where(sr=>sr.sprite!=null).Select(sr=>$"{sr.transform.parent?.name}/{sr.name} | {sr.sprite.name} | layer={sr.sortingLayerName} order={sr.sortingOrder} | bounds={sr.bounds}"));
        File.WriteAllText("Temp/DirtmouthAtmosphere-result.txt",report);
        File.WriteAllText("DirtmouthPreviews/atmosphere-validation.txt",report);
        Debug.Log(report);
    }
}
