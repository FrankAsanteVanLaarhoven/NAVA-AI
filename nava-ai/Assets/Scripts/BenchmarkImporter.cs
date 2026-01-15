using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Benchmark Importer - Loads standardized research environments.
/// Supports Franka Kitchen, Trossen Office, and other research benchmarks.
/// </summary>
public class BenchmarkImporter : MonoBehaviour
{
    [Header("Research Environments")]
    [Tooltip("Environment name (e.g., 'Franka Kitchen', 'Trossen Office')")]
    public string environmentName = "Franka Kitchen";

    [Tooltip("Asset bundle path (Unity Package)")]
    public string assetBundlePath = "Assets/Environments/Franka_kitchen.unitypackage";

    [Tooltip("Fallback scene path")]
    public string scenePath = "Assets/Scenes/Franka_kitchen.unity";

    [Header("Status")]
    [Tooltip("Current environment status")]
    public Text statusText;

    private bool isLoaded = false;

    void Start()
    {
        if (statusText != null)
        {
            statusText.text = "READY TO LOAD";
        }
    }

    /// <summary>
    /// Load research environment
    /// </summary>
    public void LoadEnvironment()
    {
        if (string.IsNullOrEmpty(environmentName))
        {
            Debug.LogError("[Benchmark] Environment name not specified");
            return;
        }

        Debug.Log($"[Benchmark] Loading environment: {environmentName}");

        // Method 1: Try Unity Package (Asset Bundle)
        if (!string.IsNullOrEmpty(assetBundlePath) && File.Exists(assetBundlePath))
        {
            LoadFromPackage(assetBundlePath);
            return;
        }

        // Method 2: Try Scene file
        if (!string.IsNullOrEmpty(scenePath) && File.Exists(scenePath))
        {
            LoadFromScene(scenePath);
            return;
        }

        // Method 3: Try by name
        LoadFromName(environmentName);
    }

    /// <summary>
    /// Load environment from Unity Package
    /// </summary>
    void LoadFromPackage(string packagePath)
    {
#if UNITY_EDITOR
        Debug.Log($"[Benchmark] Loading from Unity Package: {packagePath}");
        
        try
        {
            AssetDatabase.ImportPackage(packagePath, false);
            Debug.Log($"[Benchmark] Environment '{environmentName}' loaded from Unity Package");
            isLoaded = true;
            
            if (statusText != null)
            {
                statusText.text = $"LOADED: {environmentName} (Package)";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Benchmark] Failed to import package: {e.Message}");
            if (statusText != null)
            {
                statusText.text = $"ERROR: {e.Message}";
            }
        }
#else
        Debug.LogWarning("[Benchmark] Package import only available in Editor");
        LoadFromName(environmentName);
#endif
    }

    /// <summary>
    /// Load environment from Scene file
    /// </summary>
    void LoadFromScene(string scenePath)
    {
        Debug.Log($"[Benchmark] Loading from Scene: {scenePath}");

        try
        {
            // Extract scene name from path
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            
            // Load scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            
            Debug.Log($"[Benchmark] Environment '{environmentName}' loaded from Scene");
            isLoaded = true;
            
            if (statusText != null)
            {
                statusText.text = $"LOADED: {environmentName} (Scene)";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Benchmark] Failed to load scene: {e.Message}");
            if (statusText != null)
            {
                statusText.text = $"ERROR: {e.Message}";
            }
        }
    }

    /// <summary>
    /// Load environment by name (fallback)
    /// </summary>
    void LoadFromName(string name)
    {
        Debug.Log($"[Benchmark] Attempting to load environment by name: {name}");

        // Try common research environment names
        string[] commonScenes = {
            "Franka_kitchen",
            "Trossen_office",
            "ALOHA_workspace",
            "Panda_manipulation",
            "Research_benchmark"
        };

        foreach (string sceneName in commonScenes)
        {
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                Debug.Log($"[Benchmark] Loaded scene: {sceneName}");
                isLoaded = true;
                
                if (statusText != null)
                {
                    statusText.text = $"LOADED: {sceneName}";
                }
                return;
            }
            catch
            {
                // Try next scene
                continue;
            }
        }

        Debug.LogError($"[Benchmark] Environment '{name}' not found. Available scenes may need to be imported.");
        if (statusText != null)
        {
            statusText.text = $"NOT FOUND: {name}";
        }
    }

    /// <summary>
    /// Check if environment is loaded
    /// </summary>
    public bool IsLoaded()
    {
        return isLoaded;
    }

    /// <summary>
    /// Get available benchmark environments
    /// </summary>
    public string[] GetAvailableEnvironments()
    {
        // In production, scan Assets/Environments directory
        string envPath = Path.Combine(Application.dataPath, "Environments");
        
        if (Directory.Exists(envPath))
        {
            return Directory.GetFiles(envPath, "*.unity")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();
        }

        return new string[] { "Franka Kitchen", "Trossen Office" };
    }
}
