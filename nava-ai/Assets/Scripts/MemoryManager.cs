using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

/// <summary>
/// Memory Manager - Prevents Unity crashes from large file uploads.
/// Monitors memory usage and automatically unloads unused assets.
/// </summary>
public class MemoryManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text display for memory usage")]
    public Text memoryUsageText;

    [Tooltip("Text display for graphics memory")]
    public Text graphicsMemoryText;

    [Header("Memory Settings")]
    [Tooltip("Auto-unload threshold in MB")]
    [Range(100, 2048)]
    public float unloadThresholdMB = 512f;

    [Tooltip("Enable auto-unload when threshold exceeded")]
    public bool enableAutoUnload = true;

    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 1f;

    private float lastUpdateTime = 0f;
    private long lastTotalMemory = 0;

    void Start()
    {
        UpdateMemoryDisplay();
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMemoryDisplay();
            CheckMemoryThreshold();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateMemoryDisplay()
    {
        // 1. System Memory (Mono Heap)
        long currentAlloc = GC.GetTotalMemory(false);
        float usedMB = currentAlloc / (1024.0f * 1024.0f);

        // 2. Graphics Memory
        long graphicsMemory = SystemInfo.graphicsMemorySize;
        float graphicsMB = graphicsMemory / (1024.0f * 1024.0f);

        // 3. Total Used
        long totalUsed = currentAlloc;
        float totalMB = totalUsed / (1024.0f * 1024.0f);

        // Update UI
        if (memoryUsageText != null)
        {
            memoryUsageText.text = $"Memory: {usedMB:F2} MB / {unloadThresholdMB:F0} MB";
            
            // Color coding
            if (usedMB > unloadThresholdMB * 0.9f)
            {
                memoryUsageText.color = Color.red;
            }
            else if (usedMB > unloadThresholdMB * 0.7f)
            {
                memoryUsageText.color = Color.yellow;
            }
            else
            {
                memoryUsageText.color = Color.green;
            }
        }

        if (graphicsMemoryText != null)
        {
            graphicsMemoryText.text = $"Graphics: {graphicsMB:F0} MB";
        }

        lastTotalMemory = currentAlloc;
    }

    void CheckMemoryThreshold()
    {
        if (!enableAutoUnload) return;

        long currentAlloc = GC.GetTotalMemory(false);
        float usedMB = currentAlloc / (1024.0f * 1024.0f);

        // 4GB RAM Fix: Check for VRAM crash condition (Jetson Orin with 4GB VRAM)
        long graphicsMemoryMB = SystemInfo.graphicsMemorySize;
        if (graphicsMemoryMB > 4000) // Exceeds 4GB VRAM limit
        {
            Debug.LogError($"[MEMORY] CRASH! VRAM exceeds 4GB ({graphicsMemoryMB} MB). Forcing emergency unload...");
            ForceGC();
            
            // Unload unused assets via Addressables if available
            #if UNITY_ADDRESSABLES
            UnityEngine.AddressableAssets.Addressables.ReleaseAll();
            #endif
            
            // Reset to empty scene if available
            if (Application.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Empty");
            }
        }

        if (usedMB > unloadThresholdMB)
        {
            Debug.LogWarning($"[MemoryManager] Usage ({usedMB:F2} MB) exceeds threshold ({unloadThresholdMB:F0} MB). Unloading unused assets...");
            UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// Force garbage collection and unload unused assets
    /// </summary>
    [ContextMenu("Force Garbage Collection")]
    public void ForceGC()
    {
        Debug.Log("[MemoryManager] Forcing Garbage Collection...");
        
        // 1. Unload unused assets
        Resources.UnloadUnusedAssets();
        
        // 2. Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // 3. Update display
        UpdateMemoryDisplay();
        
        Debug.Log("[MemoryManager] Garbage Collection complete.");
    }

    /// <summary>
    /// Unload unused assets (non-blocking)
    /// </summary>
    public void UnloadUnusedAssets()
    {
        Debug.Log("[MemoryManager] Unloading unused assets...");
        Resources.UnloadUnusedAssets();
        UpdateMemoryDisplay();
    }

    /// <summary>
    /// Get current memory usage in MB
    /// </summary>
    public float GetMemoryUsageMB()
    {
        long currentAlloc = GC.GetTotalMemory(false);
        return currentAlloc / (1024.0f * 1024.0f);
    }

    /// <summary>
    /// Get graphics memory in MB
    /// </summary>
    public float GetGraphicsMemoryMB()
    {
        long graphicsMemory = SystemInfo.graphicsMemorySize;
        return graphicsMemory / (1024.0f * 1024.0f);
    }

    /// <summary>
    /// Check if memory usage is critical
    /// </summary>
    public bool IsMemoryCritical()
    {
        return GetMemoryUsageMB() > unloadThresholdMB * 0.9f;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Memory Manager Editor Window - Tools > Memory Management > Show Window
/// </summary>
public class MemoryManagerWindow : EditorWindow
{
    private MemoryManager memoryManager;
    private Vector2 scrollPosition;
    private bool autoRefresh = true;
    private float lastRefreshTime = 0f;

    [MenuItem("NAVA-AI Dashboard/Tools/Memory Management/Show Window")]
    public static void ShowWindow()
    {
        GetWindow<MemoryManagerWindow>("Memory Manager").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Memory Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Find MemoryManager in scene
        if (memoryManager == null)
        {
            memoryManager = FindObjectOfType<MemoryManager>();
        }

        if (memoryManager == null)
        {
            EditorGUILayout.HelpBox("No MemoryManager found in scene. Add one to a GameObject.", MessageType.Warning);
            if (GUILayout.Button("Create MemoryManager"))
            {
                GameObject obj = new GameObject("MemoryManager");
                memoryManager = obj.AddComponent<MemoryManager>();
            }
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Memory Usage Display
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Memory Usage", EditorStyles.boldLabel);
        
        float usedMB = memoryManager.GetMemoryUsageMB();
        float graphicsMB = memoryManager.GetGraphicsMemoryMB();
        float thresholdMB = memoryManager.unloadThresholdMB;

        EditorGUILayout.LabelField("System Memory:", $"{usedMB:F2} MB");
        EditorGUILayout.LabelField("Graphics Memory:", $"{graphicsMB:F0} MB");
        EditorGUILayout.LabelField("Threshold:", $"{thresholdMB:F0} MB");

        // Progress Bar
        float progress = usedMB / thresholdMB;
        Rect progressRect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, progress, $"{usedMB:F2} MB / {thresholdMB:F0} MB");

        // Color coding
        if (progress > 0.9f)
        {
            GUI.color = Color.red;
        }
        else if (progress > 0.7f)
        {
            GUI.color = Color.yellow;
        }
        else
        {
            GUI.color = Color.green;
        }
        GUI.color = Color.white;

        EditorGUILayout.Space();

        // Settings
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        memoryManager.unloadThresholdMB = EditorGUILayout.Slider("Unload Threshold (MB)", memoryManager.unloadThresholdMB, 100f, 2048f);
        memoryManager.enableAutoUnload = EditorGUILayout.Toggle("Auto-Unload", memoryManager.enableAutoUnload);
        memoryManager.updateInterval = EditorGUILayout.Slider("Update Interval (s)", memoryManager.updateInterval, 0.1f, 5f);

        EditorGUILayout.Space();

        // Actions
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        if (GUILayout.Button("Force Garbage Collection", GUILayout.Height(30)))
        {
            memoryManager.ForceGC();
        }

        if (GUILayout.Button("Unload Unused Assets", GUILayout.Height(30)))
        {
            memoryManager.UnloadUnusedAssets();
        }

        EditorGUILayout.Space();

        // Auto-refresh
        autoRefresh = EditorGUILayout.Toggle("Auto-Refresh", autoRefresh);
        if (autoRefresh && Time.realtimeSinceStartup - lastRefreshTime > 1f)
        {
            Repaint();
            lastRefreshTime = Time.realtimeSinceStartup;
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
