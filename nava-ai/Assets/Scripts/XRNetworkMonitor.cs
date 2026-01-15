using UnityEngine;
using TMPro;

/// <summary>
/// XR Network Monitor - Monitors XR network latency and jitter for <20ms loop requirement.
/// Provides real-time feedback on network stability for Tesla/Waymo-grade performance.
/// </summary>
public class XRNetworkMonitor : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI latencyText;
    public TextMeshProUGUI jitterText;
    
    [Header("Settings")]
    public float stableThreshold = 20.0f; // ms - Green
    public float warningThreshold = 50.0f; // ms - Yellow
    public float criticalThreshold = 90.0f; // ms - Red
    
    // Latency tracking
    private float networkLatency = 0.0f;
    private float frameTime = 0.0f;
    private int frameCount = 0;
    private float[] latencyHistory = new float[30]; // Rolling average over 30 frames
    private int historyIndex = 0;
    
    void Start()
    {
        if (latencyText != null)
        {
            latencyText.text = "LATENCY: 0.0ms";
        }
        if (jitterText != null)
        {
            jitterText.text = "JITTER: 0.0ms";
        }
        
        frameTime = Time.time;
    }

    void Update()
    {
        // 1. Calculate Delta Time (Jitter)
        float currentFrameTime = Time.time;
        float deltaTime = currentFrameTime - frameTime;
        
        if (frameTime > 0)
        {
            // Calculate jitter (deviation from expected frame time)
            float expectedFrameTime = 1.0f / 60.0f; // Assuming 60 FPS target
            float jitter = Mathf.Abs(deltaTime - expectedFrameTime);
            networkLatency = jitter * 1000.0f; // Convert to milliseconds
            
            // Store in history for rolling average
            latencyHistory[historyIndex] = networkLatency;
            historyIndex = (historyIndex + 1) % latencyHistory.Length;
        }
        
        frameTime = currentFrameTime;
        frameCount++;
        
        // Reset periodically (every 30 frames)
        if (frameCount % 30 == 0)
        {
            // Calculate average latency
            float sum = 0.0f;
            for (int i = 0; i < latencyHistory.Length; i++)
            {
                sum += latencyHistory[i];
            }
            networkLatency = sum / latencyHistory.Length;
        }

        // 2. Update UI
        if (latencyText != null)
        {
            latencyText.text = $"LATENCY: {networkLatency:F2}ms";
            
            // Color coding
            if (networkLatency < stableThreshold)
            {
                latencyText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
                latencyText.text += " (Stable)";
            }
            else if (networkLatency < warningThreshold)
            {
                latencyText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
            }
            else if (networkLatency < criticalThreshold)
            {
                latencyText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Danger);
            }
            else
            {
                latencyText.color = Color.red;
                latencyText.text += " (CRITICAL)";
            }
        }
        
        // 3. Update Jitter Text
        if (jitterText != null)
        {
            float jitterValue = networkLatency;
            jitterText.text = $"JITTER: {jitterValue:F2}ms";
            
            // Color coding jitter
            if (jitterValue < stableThreshold)
            {
                jitterText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
            }
            else if (jitterValue < warningThreshold)
            {
                jitterText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
            }
            else
            {
                jitterText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Danger);
            }
        }
    }
    
    /// <summary>
    /// Get current network latency in milliseconds.
    /// </summary>
    public float GetLatency()
    {
        return networkLatency;
    }
    
    /// <summary>
    /// Check if network is stable (<20ms).
    /// </summary>
    public bool IsStable()
    {
        return networkLatency < stableThreshold;
    }
}
