using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Visualizes 3D LiDAR point cloud data from ROS2.
/// Displays raw point cloud as particles, allowing debugging of perception "hallucinations."
/// </summary>
public class LiDARVisualizer : MonoBehaviour
{
    [Header("Point Cloud Settings")]
    [Tooltip("Particle System component for rendering points")]
    public ParticleSystem pointCloudParticles;
    
    [Tooltip("ROS2 topic name for point cloud data")]
    public string pointCloudTopic = "point_cloud";
    
    [Tooltip("Point size in Unity units")]
    public float pointSize = 0.1f;
    
    [Tooltip("Color of point cloud points")]
    public Color pointColor = Color.red;
    
    [Tooltip("Maximum number of points to render (for performance)")]
    public int maxPoints = 100000;
    
    [Header("Performance")]
    [Tooltip("Throttle updates to reduce CPU load")]
    public float updateThrottle = 0.1f; // Update every 100ms
    
    private ROSConnection ros;
    private float lastUpdateTime = 0f;
    private ParticleSystem.Particle[] particles;
    private List<Vector3> pointPositions = new List<Vector3>();
    private List<Color> pointColors = new List<Color>();

    void Start()
    {
        if (pointCloudParticles == null)
        {
            // Auto-create particle system if not assigned
            GameObject psObj = new GameObject("PointCloudParticles");
            psObj.transform.SetParent(transform);
            psObj.transform.localPosition = Vector3.zero;
            pointCloudParticles = psObj.AddComponent<ParticleSystem>();
            
            // Configure particle system
            var main = pointCloudParticles.main;
            main.startSize = pointSize;
            main.startColor = pointColor;
            main.maxParticles = maxPoints;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = pointCloudParticles.emission;
            emission.enabled = false; // We'll emit manually
            
            Debug.Log("[LiDARVisualizer] Created particle system automatically");
        }
        
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointCloud2Msg>(pointCloudTopic, OnPointCloudReceived);
        
        particles = new ParticleSystem.Particle[maxPoints];
        
        Debug.Log($"[LiDARVisualizer] Subscribed to {pointCloudTopic}");
    }

    void OnPointCloudReceived(PointCloud2Msg msg)
    {
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateThrottle) return;
        lastUpdateTime = Time.time;
        
        ProcessPointCloud(msg);
    }

    void ProcessPointCloud(PointCloud2Msg msg)
    {
        if (pointCloudParticles == null) return;
        
        // Parse PointCloud2 message
        // PointCloud2 uses a more complex structure than simple xyz arrays
        // We need to extract fields and calculate point count
        
        int pointCount = (int)(msg.width * msg.height);
        if (pointCount == 0 || pointCount > maxPoints)
        {
            pointCount = Mathf.Min(pointCount, maxPoints);
        }
        
        // Find field indices
        int xIndex = -1, yIndex = -1, zIndex = -1;
        int pointStep = (int)msg.point_step;
        
        for (int i = 0; i < msg.fields.Length; i++)
        {
            string fieldName = msg.fields[i].name.ToLower();
            if (fieldName == "x") xIndex = (int)msg.fields[i].offset;
            else if (fieldName == "y") yIndex = (int)msg.fields[i].offset;
            else if (fieldName == "z") zIndex = (int)msg.fields[i].offset;
        }
        
        if (xIndex < 0 || yIndex < 0 || zIndex < 0)
        {
            Debug.LogWarning("[LiDARVisualizer] Could not find x, y, z fields in point cloud");
            return;
        }
        
        // Clear previous points
        pointCloudParticles.Clear();
        pointPositions.Clear();
        pointColors.Clear();
        
        // Extract points from binary data
        for (int i = 0; i < pointCount; i++)
        {
            int dataOffset = i * pointStep;
            
            if (dataOffset + zIndex + 4 > msg.data.Length) break;
            
            // Extract coordinates (assuming 32-bit float)
            float x = System.BitConverter.ToSingle(msg.data, dataOffset + xIndex);
            float y = System.BitConverter.ToSingle(msg.data, dataOffset + yIndex);
            float z = System.BitConverter.ToSingle(msg.data, dataOffset + zIndex);
            
            // ROS uses Z-up, Unity uses Y-up
            Vector3 position = new Vector3(x, z, -y);
            
            pointPositions.Add(position);
            
            // Optional: Color based on height or distance
            float distance = Vector3.Distance(Vector3.zero, position);
            Color color = Color.Lerp(Color.blue, Color.red, Mathf.Clamp01(distance / 10f));
            pointColors.Add(color);
        }
        
        // Update particle system
        UpdateParticleSystem();
    }

    void UpdateParticleSystem()
    {
        if (pointCloudParticles == null || pointPositions.Count == 0) return;
        
        int count = Mathf.Min(pointPositions.Count, maxPoints);
        
        // Set particle properties
        for (int i = 0; i < count; i++)
        {
            particles[i].position = pointPositions[i];
            particles[i].startSize = pointSize;
            particles[i].startColor = pointColors.Count > i ? pointColors[i] : pointColor;
            particles[i].remainingLifetime = float.MaxValue;
        }
        
        // Apply to particle system
        pointCloudParticles.SetParticles(particles, count);
        
        Debug.Log($"[LiDARVisualizer] Updated {count} points");
    }
    
    void OnDestroy()
    {
        if (pointCloudParticles != null)
        {
            pointCloudParticles.Clear();
        }
    }
}
