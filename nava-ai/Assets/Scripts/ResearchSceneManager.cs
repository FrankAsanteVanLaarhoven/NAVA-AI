using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Research Scene Manager - Non-blocking scene loading for large environments.
/// Unity 6.3 feature: Uses LoadSceneAsync with Additive mode for background loading.
/// </summary>
public class ResearchSceneManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Currently active scene name")]
    public string activeSceneName;

    [Tooltip("Status text display")]
    public UnityEngine.UI.Text statusText;

    [Tooltip("Progress bar for loading")]
    public UnityEngine.UI.Slider progressBar;

    [Header("Settings")]
    [Tooltip("Auto-unload previous scene")]
    public bool autoUnloadPrevious = true;

    [Tooltip("Keep objects list (don't unload these)")]
    public List<GameObject> keepLoaded = new List<GameObject>();

    private List<GameObject> activeObjects = new List<GameObject>();
    private AsyncOperation currentLoadOperation;
    private bool isLoading = false;

    void Start()
    {
        // Track currently loaded objects
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene != null)
        {
            activeSceneName = activeScene.name;
            activeScene.GetRootGameObjects(activeObjects);
        }

        if (statusText != null)
        {
            statusText.text = $"SCENE: {activeSceneName} ACTIVE";
            statusText.color = Color.green;
        }
    }

    void Update()
    {
        // Monitor loading progress
        if (currentLoadOperation != null && !currentLoadOperation.isDone)
        {
            if (progressBar != null)
            {
                progressBar.value = currentLoadOperation.progress;
            }

            if (statusText != null)
            {
                statusText.text = $"IMPORTING... {currentLoadOperation.progress * 100:F0}%";
                statusText.color = Color.yellow;
            }
        }
        else if (currentLoadOperation != null && currentLoadOperation.isDone && isLoading)
        {
            isLoading = false;
            if (statusText != null)
            {
                statusText.text = $"SCENE: {activeSceneName} ACTIVE";
                statusText.color = Color.green;
            }
        }
    }

    /// <summary>
    /// Load environment asynchronously (non-blocking)
    /// </summary>
    public void LoadEnvironmentAsync(string sceneName, List<GameObject> keepLoadedObjects = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneManager] Load already in progress");
            return;
        }

        if (keepLoadedObjects != null)
        {
            keepLoaded = keepLoadedObjects;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;

        if (statusText != null)
        {
            statusText.text = "IMPORTING...";
            statusText.color = Color.yellow;
        }

        // 1. Unload previous scene if needed
        if (autoUnloadPrevious)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            if (previousScene != null && previousScene.name != sceneName)
            {
                Debug.Log($"[SceneManager] Unloading previous scene: {previousScene.name}");

                // Get objects to keep
                List<GameObject> objectsToKeep = new List<GameObject>();
                if (keepLoaded != null)
                {
                    objectsToKeep.AddRange(keepLoaded);
                }

                // Move keep objects to DontDestroyOnLoad
                foreach (GameObject obj in objectsToKeep)
                {
                    if (obj != null)
                    {
                        SceneManager.MoveGameObjectToScene(obj, SceneManager.GetSceneByName("DontDestroyOnLoad"));
                    }
                }

                // Unload scene
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previousScene);
                yield return unloadOp;

                Debug.Log("[SceneManager] Previous scene unloaded");
            }
        }

        // 2. Load new scene (Additive mode for non-blocking)
        Debug.Log($"[SceneManager] Loading scene: {sceneName}");

        currentLoadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        currentLoadOperation.allowSceneActivation = true;

        // Wait for load to complete
        while (!currentLoadOperation.isDone)
        {
            yield return null;
        }

        // 3. Set as active scene
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            activeSceneName = sceneName;

            // Get root objects
            newScene.GetRootGameObjects(activeObjects);

            Debug.Log($"[SceneManager] Scene '{sceneName}' loaded and activated");
        }

        // 4. Move keep objects back
        if (keepLoaded != null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject obj in keepLoaded)
            {
                if (obj != null)
                {
                    SceneManager.MoveGameObjectToScene(obj, activeScene);
                }
            }
        }

        // 5. Update status
        if (statusText != null)
        {
            statusText.text = $"SCENE: {activeSceneName} ACTIVE";
            statusText.color = Color.green;
        }

        if (progressBar != null)
        {
            progressBar.value = 1.0f;
        }

        isLoading = false;
        currentLoadOperation = null;
    }

    /// <summary>
    /// Unload environment (free RAM)
    /// </summary>
    public void UnloadEnvironment(List<GameObject> objectsToDestroy = null)
    {
        if (objectsToDestroy == null)
        {
            objectsToDestroy = new List<GameObject>(activeObjects);
        }

        int destroyedCount = 0;
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null && activeObjects.Contains(obj))
            {
                Destroy(obj);
                activeObjects.Remove(obj);
                destroyedCount++;
            }
        }

        Debug.Log($"[SceneManager] Unloaded {destroyedCount} objects");

        // Trigger garbage collection
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    /// <summary>
    /// Get active scene objects
    /// </summary>
    public List<GameObject> GetActiveObjects()
    {
        return new List<GameObject>(activeObjects);
    }

    /// <summary>
    /// Check if scene is loading
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }

    /// <summary>
    /// Get loading progress (0-1)
    /// </summary>
    public float GetLoadingProgress()
    {
        if (currentLoadOperation != null)
        {
            return currentLoadOperation.progress;
        }
        return isLoading ? 0f : 1f;
    }
}
