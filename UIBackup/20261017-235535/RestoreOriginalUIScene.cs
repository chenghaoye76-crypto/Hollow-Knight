using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class RestoreOriginalUIScene
{
    static RestoreOriginalUIScene() { EditorApplication.delayCall += Restore; }
    static void Restore()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Restore; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; EditorApplication.delayCall += Restore; return; }
        if (SceneManager.GetActiveScene().path == "Assets/Scenes/MainMenu.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        File.WriteAllText("Library/UIRollback-complete.txt", "Original scene reloaded.");
        string target = "UIBackup/20261017-235535/RestoreOriginalUIScene.cs";
        File.Move("Assets/Editor/RestoreOriginalUIScene.cs", target);
        if (File.Exists("Assets/Editor/RestoreOriginalUIScene.cs.meta"))
            File.Move("Assets/Editor/RestoreOriginalUIScene.cs.meta", target + ".meta");
        AssetDatabase.Refresh();
    }
}