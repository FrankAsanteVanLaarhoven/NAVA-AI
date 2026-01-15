using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using System.Collections.Generic;

/// <summary>
/// RWiFi SLAM Manager - Signal-strength based mapping for GPS-denied environments.
/// Handles scenarios where GPS is denied or jammed. Map quality relies on Signal Strength (RSSI).
/// </summary>
public class RwifiSlamManager : MonoBehaviour
{
    [Header("RWiFi Settings")]
    [Tooltip("Signal strength threshold (dBm). Below this, map is 'Fuzzy'")]
    public float signalThreshold = -60.0f;
    
    [Tooltip("Critical signal threshold (dBm). Below this, map is unreliable")]
    public float criticalThreshold = -80.0f;
    
    [Header("Visualization")]
    [Tooltip("LineRenderer for Wi-Fi map grid visualization")]
    public LineRenderer mapGridLines;
    
    [Tooltip("Material for grid lines")]
    public Material gridMaterial;
    
    [Tooltip("Text displaying map quality status")]
    public UnityEngine.UI.Text mapQualityText;
    
    [Header("Map Settings")]
    [Tooltip("Grid node spacing (meters)")]
    public float gridSpacing = 2.0f;
    
    [Tooltip("Maximum grid size")]
    public int maxGridSize = 50;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for signal strength")]
    public string signalStrengthTopic = "/rwifi/signal_strength";
    
    [Tooltip("ROS2 topic for SLAM pose")]
    public string slamPoseTopic = "/slam/pose";
    
    private ROSConnection ros;
    private bool mapStable = false;
    private float currentRSSI = -50.0f; // Default good signal
    private List<Vector3> gridNodes = new List<Vector3>();
    private Color currentMapColor = Color.green;
    private Queue<Vector3> trajectoryHistory = new Queue<Vector3>();
    private int maxHistorySize = 100;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to Signal Strength
        ros.Subscribe<Float32Msg>(signalStrengthTopic, UpdateMapQuality);
        
        // Subscribe to Robot Pose
        ros.Subscribe<PoseStampedMsg>(slamPoseTopic, UpdateGrid);
        
        // Create grid lines if not assigned
        if (mapGridLines == null)
        {
            GameObject gridObj = new GameObject("RWiFiGridLines");
            gridObj.transform.SetParent(transform);
            mapGridLines = gridObj.AddComponent<LineRenderer>();
            mapGridLines.useWorldSpace = true;
            mapGridLines.startWidth = 0.05f;
            mapGridLines.endWidth = 0.05f;
            mapGridLines.material = gridMaterial != null ? gridMaterial : CreateDefaultGridMaterial();
        }
        
        Debug.Log("[RWiFiSLAM] Initialized - Signal-strength based mapping ready");
    }

    void Update()
    {
        // Update grid visualization
        UpdateGridVisualization();
        
        // Update UI
        UpdateMapQualityUI();
    }

    void UpdateMapQuality(Float32Msg signalMsg)
    {
        currentRSSI = signalMsg.data;
        
        // Visual Feedback: Signal Quality
        if (currentRSSI < criticalThreshold)
        {
            currentMapColor = Color.red; // Critical
            mapStable = false;
            Debug.LogWarning($"[RWiFi] CRITICAL: Signal = {currentRSSI:F1} dBm - Map Unreliable");
        }
        else if (currentRSSI < signalThreshold)
        {
            currentMapColor = Color.yellow; // Weak signal
            mapStable = false;
            Debug.LogWarning($"[RWiFi] Signal Weak: {currentRSSI:F1} dBm - Map Confidence Degrading");
        }
        else
        {
            currentMapColor = Color.green; // Good signal
            mapStable = true;
        }
        
        if (mapGridLines != null)
        {
            mapGridLines.startColor = currentMapColor;
            mapGridLines.endColor = new Color(currentMapColor.r, currentMapColor.g, currentMapColor.b, 0.3f);
        }
    }

    void UpdateGrid(PoseStampedMsg pose)
    {
        // Convert ROS pose to Unity position
        Vector3 pos = Ros2Unity(pose.pose.position);
        
        // Add to trajectory history
        trajectoryHistory.Enqueue(pos);
        if (trajectoryHistory.Count > maxHistorySize)
        {
            trajectoryHistory.Dequeue();
        }
        
        // Add grid node if not too close to existing nodes
        if (ShouldAddGridNode(pos))
        {
            gridNodes.Add(pos);
            
            // Limit grid size
            if (gridNodes.Count > maxGridSize)
            {
                gridNodes.RemoveAt(0);
            }
        }
    }

    bool ShouldAddGridNode(Vector3 pos)
    {
        // Only add if far enough from existing nodes
        foreach (Vector3 node in gridNodes)
        {
            if (Vector3.Distance(pos, node) < gridSpacing)
            {
                return false;
            }
        }
        return true;
    }

    Vector3 Ros2Unity(RosMessageTypes.Geometry.PointMsg rosPoint)
    {
        // Convert ROS coordinate system (X forward, Y left, Z up) to Unity (X right, Y up, Z forward)
        return new Vector3((float)rosPoint.y, (float)rosPoint.z, (float)rosPoint.x);
    }

    void UpdateGridVisualization()
    {
        if (mapGridLines == null || gridNodes.Count < 2) return;
        
        // Draw grid connections
        int pointCount = gridNodes.Count * 2; // Each node connects to neighbors
        mapGridLines.positionCount = pointCount;
        
        int index = 0;
        for (int i = 0; i < gridNodes.Count; i++)
        {
            Vector3 node = gridNodes[i];
            
            // Connect to nearby nodes
            for (int j = i + 1; j < gridNodes.Count && index < pointCount - 1; j++)
            {
                float dist = Vector3.Distance(node, gridNodes[j]);
                if (dist < gridSpacing * 1.5f)
                {
                    mapGridLines.SetPosition(index++, node);
                    mapGridLines.SetPosition(index++, gridNodes[j]);
                }
            }
        }
        
        // Trim unused positions
        mapGridLines.positionCount = index;
    }

    void UpdateMapQualityUI()
    {
        if (mapQualityText != null)
        {
            string status = mapStable ? "STABLE" : "DEGRADED";
            mapQualityText.text = $"RWiFi Map: {status}\n" +
                                 $"Signal: {currentRSSI:F1} dBm\n" +
                                 $"Nodes: {gridNodes.Count}";
            mapQualityText.color = currentMapColor;
        }
    }

    Material CreateDefaultGridMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.green;
        return mat;
    }

    /// <summary>
    /// Get current map stability
    /// </summary>
    public bool IsMapStable()
    {
        return mapStable;
    }

    /// <summary>
    /// Get current signal strength
    /// </summary>
    public float GetSignalStrength()
    {
        return currentRSSI;
    }

    /// <summary>
    /// Get grid nodes (for other systems)
    /// </summary>
    public List<Vector3> GetGridNodes()
    {
        return new List<Vector3>(gridNodes);
    }
}
