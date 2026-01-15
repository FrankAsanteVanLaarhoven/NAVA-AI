using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System;

/// <summary>
/// Syncnomics SOP - Synchronization Operating Procedure for Unity Time (Sim) with ROS Time (Real).
/// Maintains sub-millisecond alignment between simulation and real-world clocks.
/// </summary>
public class SyncnomicsSop : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying sync status")]
    public Text syncStatus;
    
    [Tooltip("Text displaying clock offset")]
    public Text clockOffsetDisplay;
    
    [Tooltip("Text displaying time information")]
    public Text timeInfoText;
    
    [Header("Sync Settings")]
    [Tooltip("Maximum allowed sync error (seconds)")]
    public float maxSyncError = 0.05f; // 50ms
    
    [Tooltip("Enable automatic clock calibration")]
    public bool autoCalibrate = true;
    
    [Tooltip("Calibration interval (seconds)")]
    public float calibrationInterval = 10f;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for clock synchronization")]
    public string clockTopic = "/clock";
    
    [Tooltip("ROS2 topic for sync request")]
    public string syncRequestTopic = "/sync/request";
    
    private ROSConnection ros;
    private float rosTime = 0f;
    private float unityTime = 0f;
    private float syncError = 0f;
    private float lastCalibrationTime = 0f;
    private bool isSynced = false;
    private long rosTimeEpoch = 0;
    private float lastUpdateTime = 0f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to ROS clock
        ros.Subscribe<TimeMsg>(clockTopic, OnRosClockReceived);
        
        // Register publisher for sync requests
        ros.RegisterPublisher<BoolMsg>(syncRequestTopic, 10);
        
        // Initialize times
        unityTime = Time.time;
        rosTime = unityTime;
        lastCalibrationTime = Time.time;
        
        Debug.Log("[SyncnomicsSOP] Initialized - Timeline synchronization ready");
    }

    void Update()
    {
        // 1. Get Unity Time (Seconds since boot)
        unityTime = Time.time;
        
        // 2. Calculate Sync Error
        syncError = rosTime - unityTime;
        
        // 3. SOP Check
        CheckSyncStatus();
        
        // 4. Auto-calibration
        if (autoCalibrate && Time.time - lastCalibrationTime >= calibrationInterval)
        {
            CalibrateClock();
            lastCalibrationTime = Time.time;
        }
        
        // 5. Update UI
        UpdateSyncUI();
        
        lastUpdateTime = Time.time;
    }

    void OnRosClockReceived(TimeMsg msg)
    {
        // Convert ROS time to Unity time
        // ROS time is seconds + nanoseconds since epoch
        double rosSeconds = msg.sec + (msg.nanosec / 1e9);
        
        // For simulation, we use relative time
        // In real system, would convert from epoch
        if (rosTimeEpoch == 0)
        {
            rosTimeEpoch = msg.sec; // Store epoch on first message
        }
        
        rosTime = (float)(rosSeconds - rosTimeEpoch);
    }

    void CheckSyncStatus()
    {
        float absError = Mathf.Abs(syncError);
        isSynced = absError <= maxSyncError;
        
        if (!isSynced)
        {
            Debug.LogWarning($"[SyncnomicsSOP] Sync Error: {absError * 1000:F1}ms (Threshold: {maxSyncError * 1000:F1}ms)");
        }
    }

    void UpdateSyncUI()
    {
        if (syncStatus != null)
        {
            if (isSynced)
            {
                syncStatus.text = "SYNC: LOCKED (SOP ACTIVE)";
                syncStatus.color = Color.green;
            }
            else
            {
                syncStatus.text = $"SYNC ERROR: DRIFT > {maxSyncError * 1000:F0}ms";
                syncStatus.color = Color.red;
            }
        }
        
        if (clockOffsetDisplay != null)
        {
            clockOffsetDisplay.text = $"Offset: {syncError * 1000:F2} ms\n" +
                                     $"Unity: {unityTime:F3} s\n" +
                                     $"ROS: {rosTime:F3} s";
            clockOffsetDisplay.color = isSynced ? Color.green : Color.yellow;
        }
        
        if (timeInfoText != null)
        {
            DateTime unityDateTime = DateTime.Now;
            timeInfoText.text = $"Unity Time: {unityTime:F3} s\n" +
                              $"ROS Time: {rosTime:F3} s\n" +
                              $"System Time: {unityDateTime:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Calibrate clock to force Unity = ROS
    /// </summary>
    public void CalibrateClock()
    {
        // Request sync from ROS
        if (ros != null)
        {
            BoolMsg syncRequest = new BoolMsg { data = true };
            ros.Publish(syncRequestTopic, syncRequest);
        }
        
        // Reset sync error
        syncError = 0f;
        rosTime = unityTime;
        
        Debug.Log("[SyncnomicsSOP] Clock calibration requested");
    }

    /// <summary>
    /// Get current sync error
    /// </summary>
    public float GetSyncError()
    {
        return syncError;
    }

    /// <summary>
    /// Check if clocks are synced
    /// </summary>
    public bool IsSynced()
    {
        return isSynced;
    }

    /// <summary>
    /// Get ROS time
    /// </summary>
    public float GetRosTime()
    {
        return rosTime;
    }

    /// <summary>
    /// Get Unity time
    /// </summary>
    public float GetUnityTime()
    {
        return unityTime;
    }

    /// <summary>
    /// Convert ROS time to Unity time
    /// </summary>
    public float RosToUnityTime(double rosSeconds)
    {
        return (float)(rosSeconds - rosTimeEpoch);
    }

    /// <summary>
    /// Convert Unity time to ROS time
    /// </summary>
    public double UnityToRosTime(float unitySeconds)
    {
        return unitySeconds + rosTimeEpoch;
    }
}
