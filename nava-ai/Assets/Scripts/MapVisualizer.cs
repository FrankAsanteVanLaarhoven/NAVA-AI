using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;
using UnityEngine.UI;

/// <summary>
/// Visualizes SLAM occupancy grid map from ROS2.
/// Displays the map showing free space (white), walls (black), and unknown areas (gray).
/// Provides "God View" of the robot's understanding of the environment.
/// </summary>
public class MapVisualizer : MonoBehaviour
{
    [Header("Map Settings")]
    [Tooltip("UI RawImage component to display the map")]
    public RawImage mapDisplay;
    
    [Tooltip("ROS2 topic name for occupancy grid")]
    public string mapTopic = "map";
    
    [Header("Visualization")]
    [Tooltip("Highlight danger zones in red when shadow mode is active")]
    public bool highlightDangerZones = true;
    
    [Tooltip("Reference to shadow mode status (optional)")]
    public ROS2DashboardManager dashboardManager;
    
    private Texture2D mapTexture;
    private Color[] mapPixels;
    private ROSConnection ros;
    private bool mapInitialized = false;
    private int mapWidth = 0;
    private int mapHeight = 0;

    void Start()
    {
        if (mapDisplay == null)
        {
            Debug.LogError("[MapVisualizer] Map Display not assigned! Map visualization will not work.");
            return;
        }
        
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<OccupancyGridMsg>(mapTopic, UpdateMap);
        
        Debug.Log($"[MapVisualizer] Subscribed to {mapTopic}. Waiting for map data...");
    }

    void UpdateMap(OccupancyGridMsg msg)
    {
        if (mapDisplay == null) return;
        
        // 1. Resize texture if map dimensions change
        if (!mapInitialized || 
            mapTexture.width != msg.info.width || 
            mapTexture.height != msg.info.height)
        {
            mapWidth = (int)msg.info.width;
            mapHeight = (int)msg.info.height;
            
            if (mapTexture != null)
            {
                Destroy(mapTexture);
            }
            
            mapTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
            mapPixels = new Color[mapWidth * mapHeight];
            mapDisplay.texture = mapTexture;
            mapInitialized = true;
            
            Debug.Log($"[MapVisualizer] Initialized map: {mapWidth}x{mapHeight}, Resolution: {msg.info.resolution} m/pixel");
        }

        // 2. Parse Bytes to Colors
        // Note: OccupancyGrid data is row-major, starting from (0,0) at bottom-left
        // Unity textures are row-major, starting from (0,0) at top-left
        // We need to flip vertically
        bool shadowModeActive = IsShadowModeActive();
        
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // ROS map: bottom-left origin, Unity texture: top-left origin
                int rosIndex = y * mapWidth + x;
                int unityIndex = (mapHeight - 1 - y) * mapWidth + x;
                
                if (rosIndex >= 0 && rosIndex < msg.data.Length)
                {
                    sbyte val = msg.data[rosIndex];
                    
                    // Occupancy values:
                    // -1 = Unknown (Gray)
                    // 0 = Free space (White)
                    // 100 = Occupied/Wall (Black)
                    if (val == -1)
                    {
                        mapPixels[unityIndex] = Color.gray; // Unknown
                    }
                    else if (val == 0)
                    {
                        mapPixels[unityIndex] = Color.white; // Free
                    }
                    else if (val == 100)
                    {
                        // Wall - check if in danger zone during shadow mode
                        if (highlightDangerZones && shadowModeActive && IsInDangerZone(x, y, mapWidth, mapHeight))
                        {
                            mapPixels[unityIndex] = Color.red; // Danger zone
                        }
                        else
                        {
                            mapPixels[unityIndex] = Color.black; // Wall
                        }
                    }
                    else
                    {
                        // Intermediate values (0-100) - gradient from white to black
                        float normalized = val / 100f;
                        mapPixels[unityIndex] = Color.Lerp(Color.white, Color.black, normalized);
                    }
                }
            }
        }

        // 3. Apply to Texture
        mapTexture.SetPixels(mapPixels);
        mapTexture.Apply();
    }
    
    /// <summary>
    /// Check if pixel is in danger zone (near robot or in critical path)
    /// This is a simplified check - you can enhance this based on robot position
    /// </summary>
    bool IsInDangerZone(int x, int y, int width, int height)
    {
        // Example: Highlight center region (where robot might be)
        int centerX = width / 2;
        int centerY = height / 2;
        int dangerRadius = Mathf.Min(width, height) / 4;
        
        float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
        return distance < dangerRadius;
    }
    
    /// <summary>
    /// Check if shadow mode is currently active
    /// </summary>
    bool IsShadowModeActive()
    {
        if (dashboardManager != null)
        {
            return dashboardManager.IsShadowModeActive;
        }
        return false;
    }
    
    void OnDestroy()
    {
        if (mapTexture != null)
        {
            Destroy(mapTexture);
        }
    }
}
