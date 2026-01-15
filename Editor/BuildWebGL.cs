#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

/// <summary>
/// WebGL Build Script - Builds NAVΛ Dashboard for browser deployment
/// </summary>
public class BuildWebGL : EditorWindow
{
    private static string buildPath = Path.Combine(Application.dataPath, "..", "build", "webgl");
    [MenuItem("NAVA-AI Dashboard/Build/WebGL Build")]
    static void BuildWebGLMenu()
    {
        BuildWebGL window = GetWindow<BuildWebGL>("WebGL Build");
        window.Show();
    }

    [MenuItem("NAVA-AI Dashboard/Build/Quick WebGL Build")]
    static void QuickBuildWebGL()
    {
        string buildPath = Path.Combine(Application.dataPath, "..", "build", "webgl");
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildWebGL] Starting WebGL build to: {buildPath}");
        
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildWebGL] Build succeeded: {summary.totalSize} bytes");
            EditorUtility.RevealInFinder(buildPath);
            
            EditorUtility.DisplayDialog("Build Successful", 
                $"WebGL build completed!\n\nOutput: {buildPath}\n\nTo run:\n1. cd {buildPath}\n2. python3 -m http.server 8000\n3. Open http://localhost:8000", 
                "OK");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("[BuildWebGL] Build failed");
            EditorUtility.DisplayDialog("Build Failed", 
                "WebGL build failed. Check Console for errors.", 
                "OK");
        }
    }

    static string[] GetEnabledScenes()
    {
        System.Collections.Generic.List<string> scenes = new System.Collections.Generic.List<string>();
        
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        
        if (scenes.Count == 0)
        {
            // Default to SampleScene if no scenes configured
            scenes.Add("Assets/Scenes/SampleScene.unity");
        }
        
        return scenes.ToArray();
    }

    void OnGUI()
    {
        GUILayout.Label("WebGL Build Settings", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Quick Build", GUILayout.Height(30)))
        {
            QuickBuildWebGL();
        }

        GUILayout.Space(10);
        GUILayout.Label("Build will output to: build/webgl/", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        if (GUILayout.Button("Open Build Folder"))
        {
            string buildPath = Path.Combine(Application.dataPath, "..", "build", "webgl");
            if (Directory.Exists(buildPath))
            {
                EditorUtility.RevealInFinder(buildPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Build Folder Not Found", 
                    "Build folder doesn't exist yet. Run a build first.", 
                    "OK");
            }
        }
    }
}
#endif
