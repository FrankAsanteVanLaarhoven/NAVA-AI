using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Live Validator - Sim2Val (Simulation-to-Validation) with SVR (Safety Verification Report) 
/// and DVR (Digital Video Recorder) logging.
/// Computes rare event probabilities using variance reduction techniques.
/// </summary>
public class LiveValidator : MonoBehaviour
{
    [Header("Sim2Val Parameters")]
    [Tooltip("Base failure probability (rare event)")]
    public float baseFailureProb = 1e-6f;
    
    [Tooltip("Control variates (near-miss data)")]
    public List<float> controlVariates = new List<float>();
    
    [Tooltip("Maximum control variates to keep")]
    public int maxControlVariates = 100;
    
    [Header("SVR Settings")]
    [Tooltip("SVR confidence threshold (95% = 0.95)")]
    public float svrConfidenceThreshold = 0.95f;
    
    [Tooltip("Enable SVR trend prediction")]
    public bool enableSVR = true;
    
    [Header("DVR Settings")]
    [Tooltip("DVR log file path")]
    public string dvrLogPath = "simulation_dvr.csv";
    
    [Tooltip("Logging rate (Hz)")]
    public float loggingRate = 10f;
    
    [Header("UI References")]
    [Tooltip("Text displaying Sim2Val status")]
    public Text sim2valStatusText;
    
    [Tooltip("Text displaying failure rate estimate")]
    public Text failureRateText;
    
    [Tooltip("Text displaying SVR confidence")]
    public Text svrConfidenceText;
    
    [Header("Visualization")]
    [Tooltip("Color for safe state")]
    public Color safeColor = Color.green;
    
    [Tooltip("Color for warning state")]
    public Color warningColor = Color.yellow;
    
    [Tooltip("Color for unsafe state")]
    public Color unsafeColor = Color.red;
    
    private float lastLogTime = 0f;
    private float logInterval;
    private float currentFailureRate = 0f;
    private float svrConfidence = 1f;
    private bool isUncertain = false;
    private Queue<float> recentMargins = new Queue<float>();
    private Queue<float> recentVelocities = new Queue<float>();

    void Start()
    {
        logInterval = 1f / loggingRate;
        
        // Initialize DVR file
        InitializeDVRFile();
        
        Debug.Log("[LiveValidator] Initialized - Sim2Val + SVR/DVR ready");
    }

    void InitializeDVRFile()
    {
        string path = GetDVRPath();
        string directory = Path.GetDirectoryName(path);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Write header if file doesn't exist
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Timestamp,Margin,Velocity,FailureRate,SVRConfidence,Uncertainty\n");
        }
    }

    string GetDVRPath()
    {
        string[] possiblePaths = {
            dvrLogPath,
            Path.Combine(Application.dataPath, "..", "Data", dvrLogPath),
            Path.Combine(Application.persistentDataPath, "Data", dvrLogPath)
        };
        
        foreach (string path in possiblePaths)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || Directory.Exists(directory) || path == dvrLogPath)
            {
                return path;
            }
        }
        
        return Path.Combine(Application.persistentDataPath, dvrLogPath);
    }

    /// <summary>
    /// Update Sim2Val with new safety margin and velocity data
    /// </summary>
    public void UpdateSim2Val(float safetyMargin, float velocity)
    {
        // 1. Control Variate (The Near-Miss)
        controlVariates.Add(safetyMargin);
        recentMargins.Enqueue(safetyMargin);
        recentVelocities.Enqueue(velocity);
        
        if (controlVariates.Count > maxControlVariates)
        {
            controlVariates.RemoveAt(0);
        }
        if (recentMargins.Count > 50)
        {
            recentMargins.Dequeue();
            recentVelocities.Dequeue();
        }
        
        // 2. Importance Sampling (Accelerate validation)
        // Instead of running 1,000,000 sims, use near-miss data
        float varianceReduction = CalculateVarianceReduction();
        
        // Estimate failure rate using control variates
        float estimatedFailRate = EstimateFailureRate(safetyMargin, varianceReduction);
        currentFailureRate = estimatedFailRate;
        
        // 3. SVR: Support Vector Regression (Trend Prediction)
        if (enableSVR)
        {
            svrConfidence = PredictSVR(safetyMargin, velocity);
        }
        
        // 4. Check uncertainty
        isUncertain = estimatedFailRate > 5e-5f || svrConfidence < svrConfidenceThreshold;
        
        // 5. DVR: Log Data
        if (Time.time - lastLogTime >= logInterval)
        {
            LogDVR(safetyMargin, velocity, estimatedFailRate, svrConfidence);
            lastLogTime = Time.time;
        }
        
        // 6. Update UI
        UpdateUI();
        
        // 7. Visualize Sim2Val Confidence
        if (isUncertain)
        {
            Debug.LogWarning($"[LiveValidator] Sim2Val: High Uncertainty Detected - Failure Rate: {estimatedFailRate:E}, Confidence: {svrConfidence:P1}");
        }
    }

    float CalculateVarianceReduction()
    {
        if (controlVariates.Count < 2) return 0f;
        
        // Calculate variance of control variates
        float mean = controlVariates.Average();
        float variance = controlVariates.Sum(v => (v - mean) * (v - mean)) / controlVariates.Count;
        
        // Variance reduction is proportional to variance
        // Higher variance in near-misses = more information for importance sampling
        return variance;
    }

    float EstimateFailureRate(float currentMargin, float varianceReduction)
    {
        // Importance sampling: Use near-miss data to estimate rare event probability
        // Simplified implementation - real Sim2Val would be more sophisticated
        
        if (controlVariates.Count == 0) return baseFailureProb;
        
        // Count near-misses (margins below threshold)
        float threshold = 0.5f;
        int nearMissCount = controlVariates.Count(m => m < threshold);
        
        // Estimate failure rate based on near-miss frequency
        float nearMissRate = (float)nearMissCount / controlVariates.Count;
        
        // Adjust base probability using variance reduction
        float adjustedRate = baseFailureProb - (varianceReduction * 0.1f);
        adjustedRate = Mathf.Max(adjustedRate, nearMissRate * 0.01f); // At least some probability based on near-misses
        
        return Mathf.Clamp(adjustedRate, 1e-9f, 1f);
    }

    /// <summary>
    /// SVR: Support Vector Regression (Used for trend prediction)
    /// Simplified RBF kernel implementation
    /// </summary>
    public float PredictSVR(float margin, float velocity)
    {
        if (recentMargins.Count < 3) return 1f;
        
        // Simplified kernel function: RBF (Radial Basis Function)
        // Predict confidence based on trend
        
        float[] margins = recentMargins.ToArray();
        float[] velocities = recentVelocities.ToArray();
        
        // Calculate trend (slope)
        float marginTrend = 0f;
        float velocityTrend = 0f;
        
        for (int i = 1; i < margins.Length; i++)
        {
            marginTrend += margins[i] - margins[i - 1];
            velocityTrend += velocities[i] - velocities[i - 1];
        }
        
        marginTrend /= (margins.Length - 1);
        velocityTrend /= (velocities.Length - 1);
        
        // Predict confidence based on trends
        // Negative margin trend = decreasing safety = lower confidence
        // High velocity with low margin = higher risk = lower confidence
        
        float confidence = 1f;
        
        if (marginTrend < 0)
        {
            confidence -= Mathf.Abs(marginTrend) * 0.5f; // Decreasing margin reduces confidence
        }
        
        if (velocity > 1f && margin < 1f)
        {
            confidence -= 0.2f; // High speed + low margin = risk
        }
        
        // Kernel-based prediction (simplified)
        float kernelValue = Mathf.Exp(-Mathf.Pow(margin - 2f, 2f) / (2f * 0.5f)); // RBF kernel
        confidence *= kernelValue;
        
        return Mathf.Clamp01(confidence);
    }

    void LogDVR(float margin, float vel, float failRate, float confidence)
    {
        string path = GetDVRPath();
        string line = $"{System.DateTime.Now:O},{margin:F4},{vel:F2},{failRate:E},{confidence:F4},{isUncertain}\n";
        
        try
        {
            File.AppendAllText(path, line);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LiveValidator] Failed to write DVR log: {e.Message}");
        }
    }

    void UpdateUI()
    {
        if (sim2valStatusText != null)
        {
            string status = isUncertain ? "UNCERTAIN" : "VALIDATED";
            sim2valStatusText.text = $"Sim2Val: {status}";
            sim2valStatusText.color = isUncertain ? warningColor : safeColor;
        }
        
        if (failureRateText != null)
        {
            failureRateText.text = $"Failure Rate: {currentFailureRate:E}\n" +
                                   $"Base Prob: {baseFailureProb:E}";
            failureRateText.color = currentFailureRate > 5e-5f ? unsafeColor : safeColor;
        }
        
        if (svrConfidenceText != null)
        {
            svrConfidenceText.text = $"SVR Confidence: {svrConfidence:P1}";
            svrConfidenceText.color = svrConfidence < svrConfidenceThreshold ? warningColor : safeColor;
        }
    }

    /// <summary>
    /// Generate Safety Verification Report (SVR)
    /// </summary>
    public void GenerateSVR()
    {
        string svrPath = Path.Combine(Application.persistentDataPath, $"SVR_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        using (StreamWriter writer = new StreamWriter(svrPath))
        {
            writer.WriteLine("=== Safety Verification Report (SVR) ===");
            writer.WriteLine($"Generated: {System.DateTime.Now}");
            writer.WriteLine($"Total Samples: {controlVariates.Count}");
            writer.WriteLine($"Base Failure Probability: {baseFailureProb:E}");
            writer.WriteLine($"Estimated Failure Rate: {currentFailureRate:E}");
            writer.WriteLine($"SVR Confidence: {svrConfidence:P1}");
            writer.WriteLine($"Uncertainty Detected: {isUncertain}");
            writer.WriteLine($"Validation Status: {(isUncertain ? "UNCERTAIN" : "VALIDATED")}");
            
            if (controlVariates.Count > 0)
            {
                writer.WriteLine($"\nControl Variate Statistics:");
                writer.WriteLine($"  Mean: {controlVariates.Average():F4}");
                writer.WriteLine($"  Min: {controlVariates.Min():F4}");
                writer.WriteLine($"  Max: {controlVariates.Max():F4}");
                writer.WriteLine($"  StdDev: {CalculateStdDev(controlVariates):F4}");
            }
        }
        
        Debug.Log($"[LiveValidator] SVR generated: {svrPath}");
    }

    float CalculateStdDev(List<float> values)
    {
        if (values.Count == 0) return 0f;
        float mean = values.Average();
        float variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return Mathf.Sqrt(variance);
    }

    /// <summary>
    /// Get current failure rate estimate
    /// </summary>
    public float GetFailureRate()
    {
        return currentFailureRate;
    }

    /// <summary>
    /// Get SVR confidence
    /// </summary>
    public float GetSVRConfidence()
    {
        return svrConfidence;
    }

    /// <summary>
    /// Check if system is in uncertain state
    /// </summary>
    public bool IsUncertain()
    {
        return isUncertain;
    }
}
