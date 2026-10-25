using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DirtmouthCameraSetup
{
    static DirtmouthCameraSetup() => EditorApplication.update += Check;
    private static void Check()
    {
        const string request="Temp/ApplyDirtmouthCamera.request";
        if(!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(request);
        try { Apply(); }
        catch(Exception e) { File.WriteAllText("Temp/DirtmouthCamera-result.txt",e.ToString());Debug.LogException(e); }
    }
    [MenuItem("Tools/Hollow Knight/Apply And Validate Dirtmouth Camera")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Apply camera setup outside Play Mode.");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Dirtmouth.unity");
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene("Assets/Scenes/Dirtmouth.unity",OpenSceneMode.Additive);
        var roots=scene.GetRootGameObjects();
        var follow=roots.SelectMany(r=>r.GetComponentsInChildren<DirtmouthCamera>()).Single();
        follow.cameraColl=roots.SelectMany(r=>r.GetComponentsInChildren<Collider2D>()).Single(c=>c.name=="cameraColl");
        if(follow.target==null) throw new Exception("Camera target must reference the Knight.");
        follow.smoothTime=0.10f;follow.frameCentralSquare=false;
        Undo.RecordObject(follow.transform,"Centre Dirtmouth camera");
        Physics2D.SyncTransforms();follow.SnapToTarget();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Validate();
        string report="PASS: camera follows the Knight in X/Y, with a 0.10s smoothing time and no central dead zone.\n"+
            "Boundary: "+follow.cameraColl.name+" "+follow.cameraColl.bounds+"\n"+
            "PASS: viewport containment at 4:3, 16:9, 21:9; target centred away from edges; moved/scaled boundary; small-boundary fit.\n";
        File.WriteAllText("Temp/DirtmouthCamera-result.txt",report);
        Directory.CreateDirectory("DirtmouthPreviews");File.WriteAllText("DirtmouthPreviews/camera-validation.txt",report);Debug.Log(report);
    }
    private static void Require(bool ok,string text) { if(!ok)throw new Exception(text); }
    private static void Validate()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var boundaryObject=new GameObject("cameraColl");SceneManager.MoveGameObjectToScene(boundaryObject,scene);
            var boundary=boundaryObject.AddComponent<PolygonCollider2D>();boundary.isTrigger=true;
            boundary.points=new[]{new Vector2(-40,-20),new Vector2(40,-20),new Vector2(40,20),new Vector2(-40,20)};
            var actor=new GameObject("Camera test target");SceneManager.MoveGameObjectToScene(actor,scene);
            var cameraObject=new GameObject("Camera test");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var view=cameraObject.AddComponent<Camera>();view.enabled=false;view.orthographic=true;view.orthographicSize=6;
            cameraObject.transform.position=new Vector3(0,0,-20);
            var follow=cameraObject.AddComponent<DirtmouthCamera>();follow.target=actor.transform;follow.cameraColl=boundary;
            foreach(float aspect in new[]{4f/3,16f/9,21f/9})
            {
                view.aspect=aspect;
                foreach(var p in new[]{Vector2.zero,new Vector2(9,4),new Vector2(-100,-100),new Vector2(100,100),new Vector2(100,-100),new Vector2(-100,100)})
                {
                    actor.transform.position=p;Physics2D.SyncTransforms();follow.SnapToTarget();
                    foreach(var corner in new[]{new Vector3(0,0,20),new Vector3(1,0,20),new Vector3(1,1,20),new Vector3(0,1,20)})
                    {
                        Vector2 point=view.ViewportToWorldPoint(corner);
                        Require(Vector2.Distance(boundary.ClosestPoint(point),point)<0.002f,"Viewport escaped cameraColl at "+aspect+" / "+p);
                    }
                    if(p.sqrMagnitude<150) Require(Vector2.Distance((Vector2)view.WorldToViewportPoint(p),Vector2.one*0.5f)<0.001f,"Target is not centred.");
                }
            }
            boundaryObject.transform.position=new Vector3(12,8);boundaryObject.transform.localScale=Vector3.one*0.1f;
            Physics2D.SyncTransforms();actor.transform.position=new Vector3(12,8);follow.SnapToTarget();
            Require(view.orthographicSize<=boundary.bounds.extents.y+0.001f,"Small boundary height fit failed.");
            Require(view.orthographicSize*view.aspect<=boundary.bounds.extents.x+0.001f,"Small boundary width fit failed.");
            Require(Vector2.Distance(cameraObject.transform.position,actor.transform.position)<0.001f,"Moved boundary did not update.");
        }
        finally {EditorSceneManager.CloseScene(scene,true);if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
}
