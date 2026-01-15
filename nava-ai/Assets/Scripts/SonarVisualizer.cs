using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

/// <summary>
/// Visualizes sonar/radar scan data as lines shooting out from the robot.
/// Essential for debugging sensors that work in darkness where cameras fail.
/// </summary>
public class SonarVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Line Renderer component for drawing sonar rays")]
    public LineRenderer sonarLines;
    
    [Tooltip("Material for sonar lines")]
    public Material lineMaterial;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic name for sonar/radar scan")]
    public string sonarTopic = "sonar_scan";
    
    [Header("Visual Settings")]
    [Tooltip("Color of sonar lines")]
    public Color lineColor = Color.cyan;
    
    [Tooltip("Line width")]
    public float lineWidth = 0.02f;
    
    [Tooltip("Maximum range to visualize")]
    public float maxRange = 10f;
    
    [Tooltip("Fade out lines based on distance")]
    public bool fadeByDistance = true;
    
    [Header("Performance")]
    [Tooltip("Maximum number of rays to draw")]
    public int maxRays = 360;
    
    [Tooltip("Skip rays for performance (1 = all, 2 = every other, etc.)")]
    public int raySkip = 1;
    
    private ROSConnection ros;
    private Vector3[] linePositions;
    private Color[] lineColors;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<LaserScanMsg>(sonarTopic, UpdateSonar);
        
        // Create LineRenderer if not assigned
        if (sonarLines == null)
        {
            GameObject lineObj = new GameObject("SonarLines");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            sonarLines = lineObj.AddComponent<LineRenderer>();
        }
        
        // Configure LineRenderer
        sonarLines.useWorldSpace = true;
        sonarLines.startWidth = lineWidth;
        sonarLines.endWidth = lineWidth;
        sonarLines.material = lineMaterial != null ? lineMaterial : CreateDefaultMaterial();
        sonarLines.color = lineColor;
        sonarLines.positionCount = 0;
        
        Debug.Log($"[SonarVisualizer] Subscribed to {sonarTopic}");
    }

    void UpdateSonar(LaserScanMsg msg)
    {
        if (sonarLines == null) return;
        
        // Calculate number of rays
        int rayCount = Mathf.Min((int)((msg.angle_max - msg.angle_min) / msg.angle_increment), maxRays);
        rayCount = (rayCount / raySkip) * raySkip; // Ensure divisible by skip
        
        if (rayCount <= 0) return;
        
        // Allocate arrays
        if (linePositions == null || linePositions.Length < rayCount * 2)
        {
            linePositions = new Vector3[rayCount * 2];
            lineColors = new Color[rayCount * 2];
        }
        
        sonarLines.positionCount = rayCount * 2;
        
        int positionIndex = 0;
        Vector3 robotPosition = transform.position;
        
        // Draw rays
        for (int i = 0; i < rayCount; i += raySkip)
        {
            float angle = (float)(msg.angle_min + (i * msg.angle_increment));
            float distance = i < msg.ranges.Length ? (float)msg.ranges[i] : maxRange;
            
            // Clamp distance
            if (distance > maxRange || distance < msg.range_min)
            {
                distance = maxRange;
            }
            
            // Calculate direction (ROS uses standard math angles)
            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0,
                Mathf.Sin(angle)
            );
            
            // Start position (robot position)
            linePositions[positionIndex] = robotPosition;
            
            // End position (hit point)
            Vector3 endPos = robotPosition + (direction * distance);
            linePositions[positionIndex + 1] = endPos;
            
            // Color based on distance (if enabled)
            if (fadeByDistance)
            {
                float normalizedDistance = Mathf.Clamp01(distance / maxRange);
                Color rayColor = Color.Lerp(lineColor, Color.clear, normalizedDistance);
                lineColors[positionIndex] = rayColor;
                lineColors[positionIndex + 1] = rayColor;
            }
            else
            {
                lineColors[positionIndex] = lineColor;
                lineColors[positionIndex + 1] = lineColor;
            }
            
            positionIndex += 2;
        }
        
        // Apply to LineRenderer
        sonarLines.SetPositions(linePositions);
        
        // Set colors if using gradient
        if (fadeByDistance)
        {
            sonarLines.colorGradient = CreateGradientFromColors();
        }
        
        Debug.Log($"[SonarVisualizer] Updated {rayCount} rays");
    }

    Gradient CreateGradientFromColors()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        
        colorKeys[0] = new GradientColorKey(lineColor, 0f);
        colorKeys[1] = new GradientColorKey(Color.clear, 1f);
        
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = lineColor;
        return mat;
    }
    
    void OnDestroy()
    {
        if (sonarLines != null)
        {
            sonarLines.positionCount = 0;
        }
    }
}
