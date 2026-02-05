using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-setup script that runs when Unity loads the project
/// This automatically sets up the ROS2 dashboard scene
/// </summary>
[InitializeOnLoad]
public class AutoSceneSetup
{
    private static bool hasSetupRun = false;
    
    static AutoSceneSetup()
    {
        // Delay execution to ensure Unity is fully loaded
        EditorApplication.delayCall += RunAutoSetup;
        
        // Also listen for scene changes
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    
    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        // Reset flag when scene changes
        hasSetupRun = false;
        EditorApplication.delayCall += RunAutoSetup;
    }
    
    static void RunAutoSetup()
    {
        // Only run once per scene load
        if (hasSetupRun)
            return;
            
        // Check if we're in the correct scene
        Scene currentScene = SceneManager.GetActiveScene();
        if (!currentScene.IsValid() || string.IsNullOrEmpty(currentScene.name))
            return;
            
        if (currentScene.name == "SampleScene" || currentScene.path.Contains("SampleScene"))
        {
            // Check if scene is already set up
            GameObject rosManager = GameObject.Find("ROS_Manager");
            GameObject realRobot = GameObject.Find("RealRobot");
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            
            // If scene is not set up, run auto-setup
            if (rosManager == null || realRobot == null || canvas == null)
            {
                Debug.Log("[AutoSceneSetup] Scene not set up. Running automatic setup...");
                
                try
                {
                    // Call the SceneSetupHelper's setup method
                    SceneSetupHelper.SetupCompleteScene();
                    
                    // Mark scene as dirty so it saves
                    EditorSceneManager.MarkSceneDirty(currentScene);
                    
                    Debug.Log("[AutoSceneSetup] Scene setup complete! Press Play to see the dashboard.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoSceneSetup] Error during setup: {e.Message}");
                    Debug.LogError($"[AutoSceneSetup] Stack trace: {e.StackTrace}");
                }
            }
            else
            {
                Debug.Log("[AutoSceneSetup] Scene already set up. Ready to play!");
            }
            
            hasSetupRun = true;
        }
    }
    
    /// <summary>
    /// Menu item to manually trigger setup
    /// </summary>
    [MenuItem("NAVA-AI Dashboard/Auto-Setup Scene Now")]
    public static void ManualSetup()
    {
        hasSetupRun = false;
        RunAutoSetup();
    }
}
