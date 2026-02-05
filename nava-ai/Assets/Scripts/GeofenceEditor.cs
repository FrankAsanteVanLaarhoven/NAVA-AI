using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Visual editor for creating geofencing zones (no-go areas).
/// Allows dynamic safety rules without recompiling robot code.
/// </summary>
public class GeofenceEditor : MonoBehaviour
{
    [System.Serializable]
    public class GeofenceZone
    {
        public string name = "No-Go Zone";
        public List<Vector3> polygonPoints = new List<Vector3>();
        public bool active = true;
        public Color zoneColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
    }
    
    [Header("Geofence Settings")]
    [Tooltip("List of geofence zones")]
    public List<GeofenceZone> zones = new List<GeofenceZone>();
    
    [Tooltip("ROS2 topic to publish geofence boundaries")]
    public string geofenceTopic = "nav/safety_bounds";
    
    [Tooltip("Publish rate (Hz)")]
    public float publishRate = 1f;
    
    [Header("Visualization")]
    [Tooltip("Show zone boundaries in Scene view")]
    public bool showGizmos = true;
    
    [Tooltip("Height of geofence zones")]
    public float zoneHeight = 2f;
    
    private ROSConnection ros;
    private float lastPublishTime = 0f;
    private float publishInterval;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PolygonStampedMsg>(geofenceTopic, 10);
        
        publishInterval = 1f / publishRate;
        
        Debug.Log($"[GeofenceEditor] Initialized. Publishing to {geofenceTopic}");
    }

    void Update()
    {
        // Publish active zones to ROS
        if (Time.time - lastPublishTime >= publishInterval)
        {
            PublishActiveZones();
            lastPublishTime = Time.time;
        }
    }

    void PublishActiveZones()
    {
        foreach (var zone in zones)
        {
            if (!zone.active || zone.polygonPoints.Count < 3) continue;
            
            PolygonStampedMsg msg = new PolygonStampedMsg();
            msg.header.frame_id = "map"; // Adjust based on your frame
            msg.header.stamp = new RosMessageTypes.Std.TimeMsg();
            
            // Convert Unity Vector3 to ROS Point32
            msg.polygon.points = new RosMessageTypes.Geometry.Point32Msg[zone.polygonPoints.Count];
            
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                Vector3 point = zone.polygonPoints[i];
                // ROS uses Z-up, Unity uses Y-up
                msg.polygon.points[i] = new Point32Msg
                {
                    x = point.x,
                    y = -point.z, // Unity Z -> ROS Y
                    z = point.y   // Unity Y -> ROS Z
                };
            }
            
            ros.Publish(geofenceTopic, msg);
        }
    }
    
    /// <summary>
    /// Add a new geofence zone
    /// </summary>
    public void AddZone(string name, List<Vector3> points)
    {
        GeofenceZone zone = new GeofenceZone
        {
            name = name,
            polygonPoints = new List<Vector3>(points),
            active = true
        };
        zones.Add(zone);
        Debug.Log($"[GeofenceEditor] Added zone: {name} with {points.Count} points");
    }
    
    /// <summary>
    /// Remove a zone by index
    /// </summary>
    public void RemoveZone(int index)
    {
        if (index >= 0 && index < zones.Count)
        {
            zones.RemoveAt(index);
            Debug.Log($"[GeofenceEditor] Removed zone at index {index}");
        }
    }
    
    /// <summary>
    /// Toggle zone active state
    /// </summary>
    public void ToggleZone(int index)
    {
        if (index >= 0 && index < zones.Count)
        {
            zones[index].active = !zones[index].active;
            Debug.Log($"[GeofenceEditor] Zone {index} is now {(zones[index].active ? "active" : "inactive")}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        foreach (var zone in zones)
        {
            if (!zone.active || zone.polygonPoints.Count < 3) continue;
            
            // Draw zone outline
            Gizmos.color = zone.zoneColor;
            
            // Draw polygon
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                int next = (i + 1) % zone.polygonPoints.Count;
                Vector3 start = zone.polygonPoints[i];
                Vector3 end = zone.polygonPoints[next];
                
                // Draw bottom edge
                Gizmos.DrawLine(start, end);
                
                // Draw top edge
                Gizmos.DrawLine(start + Vector3.up * zoneHeight, end + Vector3.up * zoneHeight);
                
                // Draw vertical edges
                Gizmos.DrawLine(start, start + Vector3.up * zoneHeight);
            }
            
            // Draw filled polygon (simplified - draws center point)
            if (zone.polygonPoints.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var point in zone.polygonPoints)
                {
                    center += point;
                }
                center /= zone.polygonPoints.Count;
                
                Gizmos.DrawWireCube(center + Vector3.up * (zoneHeight / 2f), 
                    new Vector3(1f, zoneHeight, 1f));
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GeofenceEditor))]
public class GeofenceEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GeofenceEditor geofence = (GeofenceEditor)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Zone Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add New Zone"))
        {
            // Create a simple rectangular zone
            List<Vector3> points = new List<Vector3>
            {
                new Vector3(-2, 0, -2),
                new Vector3(2, 0, -2),
                new Vector3(2, 0, 2),
                new Vector3(-2, 0, 2)
            };
            geofence.AddZone($"Zone {geofence.zones.Count + 1}", points);
        }
        
        // List zones
        for (int i = 0; i < geofence.zones.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Zone {i}: {geofence.zones[i].name}");
            if (GUILayout.Button("Toggle", GUILayout.Width(60)))
            {
                geofence.ToggleZone(i);
            }
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                geofence.RemoveZone(i);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
