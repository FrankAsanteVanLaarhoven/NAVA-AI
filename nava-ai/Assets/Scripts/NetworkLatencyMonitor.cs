using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Network Latency Monitor - Monitors network latency using Unity 6.3 Profiler API.
/// Tracks WebSocket, P2P, and ROS2 connection latencies.
/// </summary>
public class NetworkLatencyMonitor : MonoBehaviour
{
    [Header("Monitoring Settings")]
    [Tooltip("Warning threshold in milliseconds")]
    public float warningThreshold = 20.0f;

    [Tooltip("Critical threshold in milliseconds")]
    public float criticalThreshold = 50.0f;

    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 0.5f;

    [Header("UI References")]
    [Tooltip("Latency text display")]
    public Text latencyText;

    [Tooltip("Network status text")]
    public Text networkStatusText;

    [Header("Connection Types")]
    [Tooltip("Monitor WebSocket latency")]
    public bool monitorWebSocket = true;

    [Tooltip("Monitor ROS2 latency")]
    public bool monitorROS2 = true;

    [Tooltip("Monitor WebRTC latency")]
    public bool monitorWebRTC = true;

    private Dictionary<string, float> latencies = new Dictionary<string, float>();
    private Dictionary<string, Stopwatch> stopwatches = new Dictionary<string, Stopwatch>();
    private float lastUpdateTime = 0f;

    void Start()
    {
        // Initialize stopwatches
        if (monitorWebSocket)
        {
            stopwatches["WebSocket"] = new Stopwatch();
        }

        if (monitorROS2)
        {
            stopwatches["ROS2"] = new Stopwatch();
        }

        if (monitorWebRTC)
        {
            stopwatches["WebRTC"] = new Stopwatch();
        }

        if (latencyText != null)
        {
            latencyText.text = "LATENCY: --";
        }

        if (networkStatusText != null)
        {
            networkStatusText.text = "NETWORK: MONITORING";
        }
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateLatencyDisplay();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateLatencyDisplay()
    {
        float maxLatency = 0f;
        string maxLatencyType = "";

        // Calculate average latency
        foreach (var kvp in latencies)
        {
            if (kvp.Value > maxLatency)
            {
                maxLatency = kvp.Value;
                maxLatencyType = kvp.Key;
            }
        }

        // Update UI
        if (latencyText != null)
        {
            if (maxLatency > 0)
            {
                latencyText.text = $"{maxLatencyType} LATENCY: {maxLatency:F2}ms";

                // Color coding
                if (maxLatency > criticalThreshold)
                {
                    latencyText.color = Color.red;
                }
                else if (maxLatency > warningThreshold)
                {
                    latencyText.color = Color.yellow;
                }
                else
                {
                    latencyText.color = Color.green;
                }
            }
            else
            {
                latencyText.text = "LATENCY: --";
                latencyText.color = Color.gray;
            }
        }

        // Update network status
        if (networkStatusText != null)
        {
            if (maxLatency > criticalThreshold)
            {
                networkStatusText.text = "NETWORK: CRITICAL";
                networkStatusText.color = Color.red;
            }
            else if (maxLatency > warningThreshold)
            {
                networkStatusText.text = "NETWORK: WARNING";
                networkStatusText.color = Color.yellow;
            }
            else
            {
                networkStatusText.text = "NETWORK: OK";
                networkStatusText.color = Color.green;
            }
        }

        // Log warnings
        if (maxLatency > warningThreshold)
        {
            UnityEngine.Debug.LogWarning($"[Network] High Latency Detected: {maxLatencyType} = {maxLatency:F2}ms");
        }
    }

    /// <summary>
    /// Start latency measurement for connection type
    /// </summary>
    public void StartMeasurement(string connectionType)
    {
        if (stopwatches.ContainsKey(connectionType))
        {
            stopwatches[connectionType].Restart();
        }
    }

    /// <summary>
    /// Stop latency measurement and record result
    /// </summary>
    public void StopMeasurement(string connectionType)
    {
        if (stopwatches.ContainsKey(connectionType))
        {
            stopwatches[connectionType].Stop();
            float latencyMs = (float)stopwatches[connectionType].Elapsed.TotalMilliseconds;
            latencies[connectionType] = latencyMs;
        }
    }

    /// <summary>
    /// Record latency directly (for external measurements)
    /// </summary>
    public void RecordLatency(string connectionType, float latencyMs)
    {
        latencies[connectionType] = latencyMs;
    }

    /// <summary>
    /// Get current latency for connection type
    /// </summary>
    public float GetLatency(string connectionType)
    {
        return latencies.ContainsKey(connectionType) ? latencies[connectionType] : 0f;
    }

    /// <summary>
    /// Get all latencies
    /// </summary>
    public Dictionary<string, float> GetAllLatencies()
    {
        return new Dictionary<string, float>(latencies);
    }

    /// <summary>
    /// Check if any connection has high latency
    /// </summary>
    public bool HasHighLatency()
    {
        foreach (var latency in latencies.Values)
        {
            if (latency > warningThreshold)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Reset all measurements
    /// </summary>
    public void ResetMeasurements()
    {
        latencies.Clear();
        foreach (var sw in stopwatches.Values)
        {
            sw.Reset();
        }
    }
}
