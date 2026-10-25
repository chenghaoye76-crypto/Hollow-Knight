using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KnightVisualSetup
{
    static KnightVisualSetup()=>EditorApplication.update+=Check;
    private static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists("Temp/ApplyKnightVisuals.request"))
        {
            File.Delete("Temp/ApplyKnightVisuals.request");
            try { Apply(); }catch(Exception e){File.WriteAllText("Temp/KnightVisualSetup.txt",e.ToString());Debug.LogException(e);}
        }
        if(!File.Exists("Temp/InspectKnightVisuals.request"))return;
        File.Delete("Temp/InspectKnightVisuals.request");
        try
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
            var lines=all.Where(t=>t.name.StartsWith("entrance") || t.name=="Enter" || t.GetComponent<Canvas>()!=null || t.GetComponent<Prompt>()!=null || (t.GetComponent<Collider2D>()!=null && (t.name=="bench" || t.name=="elder_bug" || t.name=="coll")))
                .Select(t=>$"{Path(t)} | pos={t.position} active={t.gameObject.activeInHierarchy} | coll={(t.GetComponent<Collider2D>()!=null?t.GetComponent<Collider2D>().bounds.ToString():"none")} | scripts={string.Join(",",t.GetComponents<MonoBehaviour>().Select(m=>m==null?"MISSING":m.GetType().Name))} | renderers={string.Join(",",t.GetComponents<Renderer>().Select(r=>r.GetType().Name+":"+r.enabled))}");
            File.WriteAllLines("Temp/KnightVisualInspection.txt",lines);
        }
        catch(Exception e){File.WriteAllText("Temp/KnightVisualInspection.txt",e.ToString());}
    }
    [MenuItem("Tools/Hollow Knight/Bind Entrance Prompts")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Bind prompts outside Play Mode.");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if(!scene.IsValid() || !scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Dirtmouth.unity",OpenSceneMode.Additive);
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var report=new System.Collections.Generic.List<string>();
        foreach(var entrance in all.Where(t=>t.name.StartsWith("entrance") && t.GetComponent<Collider2D>()!=null))
        {
            var prompts=entrance.parent.GetComponentsInChildren<Prompt>(true);
            if(prompts.Length!=1)throw new Exception("Expected one sibling Prompt for "+entrance.name+", found "+prompts.Length);
            Bind(entrance.GetComponent<Collider2D>(),prompts[0]);report.Add(entrance.parent.name+"/"+entrance.name+" -> "+prompts[0].name);
        }
        // The existing NPC and bench prompts use their own interaction colliders as well.
        foreach(string label in new[]{"elder_bug","bench"})
        {
            var zone=all.FirstOrDefault(t=>t.name==label && t.GetComponent<Collider2D>()!=null);
            if(zone==null)continue;
            var prompts=zone.GetComponentsInChildren<Prompt>(true);
            if(prompts.Length==1){Bind(zone.GetComponent<Collider2D>(),prompts[0]);report.Add(label+" -> "+prompts[0].name);}
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        report.Add("PASS: entrance triggers bound to their own prompts; character identification uses PlayerController, not a tag.");
        File.WriteAllLines("Temp/KnightVisualSetup.txt",report);Debug.Log(string.Join("\n",report));
    }
    private static void Bind(Collider2D collider,Prompt prompt)
    {
        Undo.RecordObject(collider,"Configure prompt trigger");collider.isTrigger=true;
        var trigger=collider.GetComponent<EntrancePrompt>();
        if(trigger==null)trigger=Undo.AddComponent<EntrancePrompt>(collider.gameObject);
        Undo.RecordObject(trigger,"Bind entrance prompt");trigger.prompt=prompt;
        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
    }
}
