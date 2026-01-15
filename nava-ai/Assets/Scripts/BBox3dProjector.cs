using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;

/// <summary>
/// 3D Bounding Box Projector - Projects 2D VLM detections into 3D Unity world space.
/// VLA models output 2D image detections. This projects them into 3D using depth/raycasting.
/// </summary>
public class BBox3dProjector : MonoBehaviour
{
    [System.Serializable]
    public struct Detection
    {
        public string label;
        public float confidence;
        public Rect uvRect; // Normalized 0-1 coordinates
        public Vector3 worldPosition;
        public float distance;
    }

    [Header("Visualization")]
    [Tooltip("Prefab for bounding box wireframe")]
    public GameObject boundingBoxPrefab;
    
    [Tooltip("The 'Eyes' of the robot (vision camera)")]
    public Camera visionCamera;
    
    [Tooltip("Material for bounding boxes")]
    public Material bboxMaterial;
    
    [Header("Projection Settings")]
    [Tooltip("Default depth if raycast fails (meters)")]
    public float defaultDepth = 5.0f;
    
    [Tooltip("Bounding box scale factor")]
    public float boxScaleFactor = 1.0f;
    
    [Tooltip("Minimum confidence to display")]
    public float minConfidence = 0.5f;
    
    [Header("Label Display")]
    [Tooltip("Prefab for 3D text labels")]
    public GameObject labelPrefab;
    
    [Tooltip("Canvas for 2D labels (if using UI)")]
    public Canvas labelCanvas;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for detections")]
    public string detectionTopic = "/vlm/detections";
    
    [Header("Testing")]
    [Tooltip("Key to add test detection")]
    public KeyCode testKey = KeyCode.B;
    
    private ROSConnection ros;
    private List<Detection> activeDetections = new List<Detection>();
    private Dictionary<string, GameObject> activeBoxes = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> activeLabels = new Dictionary<string, GameObject>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to detections (would be custom message type in real system)
        // For now, using StringMsg as placeholder
        ros.Subscribe<StringMsg>(detectionTopic, OnDetectionReceived);
        
        // Get vision camera if not assigned
        if (visionCamera == null)
        {
            visionCamera = Camera.main;
        }
        
        // Create label canvas if not assigned
        if (labelCanvas == null)
        {
            GameObject canvasObj = new GameObject("DetectionLabels");
            labelCanvas = canvasObj.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            labelCanvas.worldCamera = visionCamera;
            canvasObj.transform.SetParent(transform);
        }
        
        // Create default bounding box prefab if not assigned
        if (boundingBoxPrefab == null)
        {
            boundingBoxPrefab = CreateDefaultBBoxPrefab();
        }
        
        Debug.Log("[BBox3dProjector] Initialized - 3D detection projection ready");
    }

    void Update()
    {
        // Test detection (for debugging)
        if (Input.GetKeyDown(testKey))
        {
            AddTestDetection("Pedestrian", 0.9f, new Rect(0.4f, 0.4f, 0.2f, 0.3f));
        }
        
        // Update all active detections
        foreach (var det in activeDetections)
        {
            UpdateDetection(det);
        }
        
        // Clean up old detections
        CleanupOldDetections();
    }

    void OnDetectionReceived(StringMsg msg)
    {
        // Parse detection from ROS message
        // Format: "label:confidence:x:y:width:height"
        string[] parts = msg.data.Split(':');
        if (parts.Length >= 6)
        {
            string label = parts[0];
            float confidence = float.Parse(parts[1]);
            float x = float.Parse(parts[2]);
            float y = float.Parse(parts[3]);
            float width = float.Parse(parts[4]);
            float height = float.Parse(parts[5]);
            
            Rect uvRect = new Rect(x, y, width, height);
            AddDetectionFromROS(label, confidence, uvRect);
        }
    }

    void AddTestDetection(string label, float confidence, Rect uvRect)
    {
        AddDetectionFromROS(label, confidence, uvRect);
    }

    /// <summary>
    /// Add detection from ROS or external source
    /// </summary>
    public void AddDetectionFromROS(string label, float conf, Rect uv)
    {
        if (conf < minConfidence) return;
        
        Detection det = new Detection
        {
            label = label,
            confidence = conf,
            uvRect = uv
        };
        
        activeDetections.Add(det);
        ProjectAndDraw(det);
    }

    void ProjectAndDraw(Detection det)
    {
        // A. Raycast to find depth (Assuming ground is flat at Y=0)
        // Center of detection in screen space
        Vector2 center = new Vector2(
            det.uvRect.center.x * Screen.width,
            (1f - det.uvRect.center.y) * Screen.height // Flip Y for Unity
        );
        
        Ray ray = visionCamera.ScreenPointToRay(center);
        
        RaycastHit hit;
        Vector3 worldPos;
        float distance;
        
        if (Physics.Raycast(ray, out hit, 100.0f))
        {
            worldPos = hit.point;
            distance = hit.distance;
        }
        else
        {
            // Use default depth if raycast fails
            worldPos = ray.GetPoint(defaultDepth);
            distance = defaultDepth;
        }
        
        // Update detection with world position
        det.worldPosition = worldPos;
        det.distance = distance;
        
        // B. Create/Snap Box
        string boxKey = $"{det.label}_{activeBoxes.Count}";
        GameObject box = GetOrCreateBox(boxKey);
        box.transform.position = worldPos;
        
        // C. Scale Box based on FOV distance and detection size
        // Closer objects appear larger, larger detections = larger boxes
        float distFactor = 10.0f / distance;
        float sizeFactor = Mathf.Max(det.uvRect.width, det.uvRect.height);
        Vector3 scale = Vector3.one * distFactor * sizeFactor * boxScaleFactor;
        box.transform.localScale = scale;
        
        // D. Color based on confidence
        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat == null)
            {
                mat = bboxMaterial != null ? new Material(bboxMaterial) : CreateDefaultMaterial();
                renderer.material = mat;
            }
            
            mat.color = Color.Lerp(Color.red, Color.green, det.confidence);
        }
        
        // E. Labeling
        DrawLabel(box.transform.position + Vector3.up * scale.y * 0.6f, det.label, det.confidence);
        
        // Update detection in list
        int index = activeDetections.IndexOf(det);
        if (index >= 0)
        {
            activeDetections[index] = det;
        }
    }

    GameObject GetOrCreateBox(string key)
    {
        if (activeBoxes.ContainsKey(key) && activeBoxes[key] != null)
        {
            return activeBoxes[key];
        }
        
        GameObject box;
        if (boundingBoxPrefab != null)
        {
            box = Instantiate(boundingBoxPrefab);
        }
        else
        {
            box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Make it wireframe
            Destroy(box.GetComponent<Collider>());
        }
        
        box.name = $"BBox_{key}";
        box.transform.SetParent(transform);
        activeBoxes[key] = box;
        
        return box;
    }

    void DrawLabel(Vector3 pos, string text, float confidence)
    {
        string labelKey = text;
        
        // Remove old label if exists
        if (activeLabels.ContainsKey(labelKey) && activeLabels[labelKey] != null)
        {
            Destroy(activeLabels[labelKey]);
        }
        
        // Create label
        GameObject labelObj;
        if (labelPrefab != null)
        {
            labelObj = Instantiate(labelPrefab);
        }
        else
        {
            labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(labelCanvas.transform);
            
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = $"{text} ({confidence:P0})";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            
            // Add background
            Image bg = labelObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
        }
        
        labelObj.transform.position = pos;
        labelObj.transform.LookAt(visionCamera.transform);
        labelObj.transform.Rotate(0, 180, 0); // Face camera
        
        // Scale based on distance
        float dist = Vector3.Distance(pos, visionCamera.transform.position);
        float scale = Mathf.Clamp(0.5f / dist, 0.1f, 2f);
        labelObj.transform.localScale = Vector3.one * scale;
        
        activeLabels[labelKey] = labelObj;
    }

    void UpdateDetection(Detection det)
    {
        // Update existing detection visualization
        // This could update position if object moves, etc.
    }

    void CleanupOldDetections()
    {
        // Remove detections older than threshold (simplified - would use timestamps in real system)
        // For now, keep all active detections
    }

    GameObject CreateDefaultBBoxPrefab()
    {
        GameObject prefab = new GameObject("DefaultBBox");
        
        // Create wireframe cube using LineRenderer
        LineRenderer lr = prefab.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = bboxMaterial != null ? bboxMaterial : CreateDefaultMaterial();
        lr.loop = true;
        
        // Create cube vertices
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
        };
        
        // Draw cube edges (simplified - would need proper edge connections)
        lr.positionCount = 16;
        // Front face
        lr.SetPosition(0, vertices[0]); lr.SetPosition(1, vertices[1]);
        lr.SetPosition(2, vertices[2]); lr.SetPosition(3, vertices[3]);
        lr.SetPosition(4, vertices[0]);
        // Back face
        lr.SetPosition(5, vertices[4]); lr.SetPosition(6, vertices[5]);
        lr.SetPosition(7, vertices[6]); lr.SetPosition(8, vertices[7]);
        lr.SetPosition(9, vertices[4]);
        // Connecting edges
        lr.SetPosition(10, vertices[0]); lr.SetPosition(11, vertices[4]);
        lr.SetPosition(12, vertices[1]); lr.SetPosition(13, vertices[5]);
        lr.SetPosition(14, vertices[2]); lr.SetPosition(15, vertices[6]);
        
        return prefab;
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.yellow;
        return mat;
    }

    /// <summary>
    /// Clear all detections
    /// </summary>
    public void ClearDetections()
    {
        activeDetections.Clear();
        
        foreach (var box in activeBoxes.Values)
        {
            if (box != null) Destroy(box);
        }
        activeBoxes.Clear();
        
        foreach (var label in activeLabels.Values)
        {
            if (label != null) Destroy(label);
        }
        activeLabels.Clear();
    }

    /// <summary>
    /// Get active detections
    /// </summary>
    public List<Detection> GetActiveDetections()
    {
        return new List<Detection>(activeDetections);
    }
}
