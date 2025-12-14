using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// This script forces the editor to always load Scene 0 (Main Menu) when you hit Play.
[InitializeOnLoad]
public class BootToMenu
{
    static BootToMenu()
    {
        EditorApplication.playModeStateChanged += LoadMainMenu;
    }

    private static void LoadMainMenu(PlayModeStateChange state)
    {
        // Only run this when we are about to hit play
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save the current scene so you don't lose work!
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        // Set the "Start Scene" to the first scene in your Build Settings
        if (!EditorApplication.isPlaying && EditorBuildSettings.scenes.Length > 0)
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
            EditorSceneManager.playModeStartScene = scene;
        }
    }
}