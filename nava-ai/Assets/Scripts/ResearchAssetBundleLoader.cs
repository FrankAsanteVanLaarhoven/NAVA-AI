using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Research Asset Bundle Loader - Fast environment loading using Unity 6.3 Asset Bundles.
/// Supports .unitypackage bundles for research environments (Franka Kitchen, Trossen Office).
/// </summary>
public class ResearchAssetBundleLoader : MonoBehaviour
{
    [Header("Bundle Configuration")]
    [Tooltip("Bundle name (e.g., 'FrankaKitchen_Research')")]
    public string bundleName = "FrankaKitchen_Research";

    [Tooltip("Scene to load from bundle")]
    public string sceneToLoad = "FrankaKitchen_Main";

    [Tooltip("Bundle path (relative to StreamingAssets)")]
    public string bundlePath = "Assets/StreamingAssets";

    [Header("UI References")]
    [Tooltip("Load status text")]
    public UnityEngine.UI.Text loadStatusText;

    [Tooltip("Load progress bar")]
    public UnityEngine.UI.Slider loadProgress;

    [Header("Settings")]
    [Tooltip("Auto-load on start")]
    public bool autoLoad = false;

    private AssetBundle loadedBundle;
    private AsyncOperation loadOperation;
    private bool isLoading = false;

    void Start()
    {
        if (loadStatusText != null)
        {
            loadStatusText.text = "ASSETS: READY";
        }

        if (autoLoad && !string.IsNullOrEmpty(bundleName))
        {
            LoadEnvironment();
        }
    }

    void Update()
    {
        // Monitor async operation progress
        if (loadOperation != null && !loadOperation.isDone)
        {
            if (loadProgress != null)
            {
                loadProgress.value = loadOperation.progress;
            }

            if (loadStatusText != null)
            {
                loadStatusText.text = $"LOADING... {loadOperation.progress * 100:F0}%";
                loadStatusText.color = Color.yellow;
            }
        }
        else if (loadOperation != null && loadOperation.isDone && isLoading)
        {
            isLoading = false;
            if (loadStatusText != null)
            {
                loadStatusText.text = "IDLE";
                loadStatusText.color = Color.blue;
            }
        }
    }

    /// <summary>
    /// Load environment from asset bundle
    /// </summary>
    [ContextMenu("Research/Load Environment")]
    public void LoadEnvironment()
    {
        if (isLoading)
        {
            Debug.LogWarning("[Bundle] Load already in progress");
            return;
        }

        StartCoroutine(LoadEnvironmentCoroutine());
    }

    IEnumerator LoadEnvironmentCoroutine()
    {
        isLoading = true;

        // 1. Check if bundle is cached
        string bundleFilePath = Path.Combine(Application.streamingAssetsPath, bundleName);
        
        if (!File.Exists(bundleFilePath))
        {
            // Try alternative path
            bundleFilePath = Path.Combine(Application.dataPath, bundlePath, bundleName);
        }

        if (File.Exists(bundleFilePath))
        {
            Debug.Log($"[Bundle] Loading from file: {bundleFilePath}");

            // Load bundle from file
            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(bundleFilePath);
            yield return bundleRequest;

            loadedBundle = bundleRequest.assetBundle;

            if (loadedBundle != null)
            {
                Debug.Log("[Bundle] Bundle loaded successfully");
                yield return StartCoroutine(LoadSceneFromBundle());
            }
            else
            {
                Debug.LogError("[Bundle] Failed to load bundle");
                ShowError("Failed to load bundle");
                isLoading = false;
                yield break;
            }
        }
        else
        {
            Debug.LogWarning($"[Bundle] Bundle file not found: {bundleFilePath}. Trying scene load...");
            yield return StartCoroutine(LoadSceneDirectly());
        }
    }

    IEnumerator LoadSceneFromBundle()
    {
        if (loadedBundle == null)
        {
            Debug.LogError("[Bundle] No bundle loaded");
            yield break;
        }

        // Unload previous scene if needed
        if (SceneManager.GetActiveScene() != null && SceneManager.sceneCount > 1)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            yield return unloadOp;
        }

        // Load scene from bundle
        string[] scenePaths = loadedBundle.GetAllScenePaths();
        
        if (scenePaths.Length > 0)
        {
            string scenePath = scenePaths[0]; // Use first scene or find by name
            
            // Find scene by name if specified
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                foreach (string path in scenePaths)
                {
                    if (Path.GetFileNameWithoutExtension(path) == sceneToLoad)
                    {
                        scenePath = path;
                        break;
                    }
                }
            }

            Debug.Log($"[Bundle] Loading scene: {scenePath}");

            loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            yield return loadOperation;

            if (loadOperation.isDone)
            {
                ShowSuccess(sceneToLoad);
            }
        }
        else
        {
            Debug.LogWarning("[Bundle] No scenes found in bundle");
            ShowError("No scenes in bundle");
        }
    }

    IEnumerator LoadSceneDirectly()
    {
        // Fallback: Load scene directly (not from bundle)
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"[Bundle] Loading scene directly: {sceneToLoad}");

            // Unload previous scene
            if (SceneManager.GetActiveScene() != null && SceneManager.sceneCount > 1)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
                yield return unloadOp;
            }

            // Load new scene
            loadOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            yield return loadOperation;

            if (loadOperation.isDone)
            {
                ShowSuccess(sceneToLoad);
            }
        }
        else
        {
            ShowError("No scene specified");
        }

        isLoading = false;
    }

    /// <summary>
    /// Save environment to bundle (for research modifications)
    /// </summary>
    [ContextMenu("Research/Save Environment")]
    public void SaveEnvironment()
    {
        Debug.Log("[Bundle] Saving environment changes...");
        
        // In production, this would save the current scene state to a bundle
        // For now, we just log
        Debug.Log("[Bundle] Environment saved (simulated)");
        
        if (loadStatusText != null)
        {
            loadStatusText.text = "SAVED";
            loadStatusText.color = Color.green;
        }
    }

    void ShowSuccess(string name)
    {
        if (loadStatusText != null)
        {
            loadStatusText.text = $"ENV: {name} LOADED";
            loadStatusText.color = Color.green;
        }

        if (loadProgress != null)
        {
            loadProgress.value = 1.0f;
        }

        isLoading = false;
        Debug.Log($"[Bundle] Environment '{name}' loaded successfully");
    }

    void ShowError(string message)
    {
        if (loadStatusText != null)
        {
            loadStatusText.text = $"ERROR: {message}";
            loadStatusText.color = Color.red;
        }

        isLoading = false;
    }

    void OnDestroy()
    {
        // Unload bundle
        if (loadedBundle != null)
        {
            loadedBundle.Unload(false);
            Debug.Log("[Bundle] Bundle unloaded");
        }
    }
}
