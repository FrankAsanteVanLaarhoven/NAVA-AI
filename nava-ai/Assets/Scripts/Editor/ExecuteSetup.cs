using UnityEngine;
using UnityEditor;

/// <summary>
/// Executable setup script that can be called from command line
/// </summary>
public class ExecuteSetup
{
    /// <summary>
    /// Execute scene setup - can be called from Unity command line
    /// </summary>
    public static void Execute()
    {
        Debug.Log("[ExecuteSetup] Starting automatic scene setup...");
        
        // Call the setup method from SceneSetupHelper
        SceneSetupHelper.SetupCompleteScene();
        
        Debug.Log("[ExecuteSetup] Scene setup complete!");
        Debug.Log("[ExecuteSetup] You can now press Play to see the dashboard.");
        
        // Mark scene as dirty
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }
    }
}
