using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Replays robot trajectory from CSV log files.
/// Allows post-mortem analysis of crashes and path validation.
/// </summary>
public class TrajectoryReplayer : MonoBehaviour
{
    [Header("Replay Settings")]
    [Tooltip("The GameObject that will replay the path (ghost robot)")]
    public GameObject replayGhost;
    
    [Tooltip("Playback speed multiplier (1.0 = real-time)")]
    public float playbackSpeed = 1.0f;
    
    [Tooltip("Path to CSV file (relative to project root or absolute)")]
    public string csvFilePath = "nava_telemetry.csv";
    
    [Header("Visualization")]
    [Tooltip("Draw the path as a line in the scene")]
    public bool drawPath = true;
    
    [Tooltip("Material for path line (optional)")]
    public Material pathLineMaterial;
    
    private List<Vector3> recordedPath = new List<Vector3>();
    private List<float> recordedTimestamps = new List<float>();
    private int currentIndex = 0;
    private bool isPlaying = false;
    private float replayStartTime = 0f;
    private LineRenderer pathLine;

    void Start()
    {
        // Create line renderer for path visualization
        if (drawPath && replayGhost != null)
        {
            pathLine = replayGhost.AddComponent<LineRenderer>();
            pathLine.material = pathLineMaterial != null ? pathLineMaterial : CreateDefaultLineMaterial();
            pathLine.startWidth = 0.1f;
            pathLine.endWidth = 0.1f;
            pathLine.useWorldSpace = true;
        }
    }

    /// <summary>
    /// Load trajectory data from CSV file
    /// </summary>
    public void LoadData()
    {
        recordedPath.Clear();
        recordedTimestamps.Clear();
        
        // Try multiple possible file paths
        string[] possiblePaths = {
            csvFilePath,
            Path.Combine(Application.dataPath, "..", csvFilePath),
            Path.Combine(Application.persistentDataPath, csvFilePath),
            csvFilePath // Absolute path if provided
        };
        
        string filePath = null;
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                filePath = path;
                break;
            }
        }
        
        if (filePath == null)
        {
            Debug.LogWarning($"[TrajectoryReplayer] CSV file not found: {csvFilePath}. Tried: {string.Join(", ", possiblePaths)}");
            return;
        }
        
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            bool isFirstLine = true;
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Skip header line
                if (isFirstLine && (line.Contains("timestamp") || line.Contains("epoch")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;
                
                // Parse CSV: Format may vary, but we expect: timestamp, x, y, z, margin, velocity, etc.
                string[] parts = line.Split(',');
                
                if (parts.Length < 3) continue; // Need at least timestamp, x, y
                
                // Try to parse timestamp (first column)
                float timestamp = 0f;
                if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out timestamp))
                {
                    recordedTimestamps.Add(timestamp);
                }
                
                // Try to parse position (x, y, z or just x, y)
                float x = 0f, y = 0f, z = 0f;
                bool hasX = parts.Length > 1 && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                bool hasY = parts.Length > 2 && float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                bool hasZ = parts.Length > 3 && float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                
                if (hasX && hasY)
                {
                    // Unity uses Y as up, ROS typically uses Z as up
                    // Adjust based on your coordinate system
                    Vector3 position = new Vector3(x, z, y); // ROS (x,y,z) -> Unity (x,z,y)
                    recordedPath.Add(position);
                }
            }
            
            Debug.Log($"[TrajectoryReplayer] Loaded {recordedPath.Count} waypoints from {filePath}");
            
            // Update path visualization
            if (pathLine != null && recordedPath.Count > 0)
            {
                pathLine.positionCount = recordedPath.Count;
                pathLine.SetPositions(recordedPath.ToArray());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrajectoryReplayer] Error loading CSV: {e.Message}");
        }
    }

    void Update()
    {
        if (!isPlaying || recordedPath.Count == 0 || replayGhost == null) return;

        // Time-based replay (more accurate than distance-based)
        float currentTime = (Time.time - replayStartTime) * playbackSpeed;
        
        // Find current waypoint based on timestamp
        while (currentIndex < recordedTimestamps.Count - 1 && 
               recordedTimestamps[currentIndex + 1] <= currentTime)
        {
            currentIndex++;
        }
        
        if (currentIndex < recordedPath.Count)
        {
            // Interpolate between waypoints for smooth movement
            if (currentIndex < recordedPath.Count - 1)
            {
                float t = (currentTime - recordedTimestamps[currentIndex]) / 
                         (recordedTimestamps[currentIndex + 1] - recordedTimestamps[currentIndex]);
                t = Mathf.Clamp01(t);
                
                replayGhost.transform.position = Vector3.Lerp(
                    recordedPath[currentIndex],
                    recordedPath[currentIndex + 1],
                    t
                );
            }
            else
            {
                replayGhost.transform.position = recordedPath[currentIndex];
            }
        }
        else
        {
            // Replay complete
            StopReplay();
        }
    }
    
    /// <summary>
    /// Start replaying the trajectory
    /// </summary>
    public void StartReplay()
    {
        if (recordedPath.Count == 0)
        {
            Debug.LogWarning("[TrajectoryReplayer] No trajectory data loaded. Call LoadData() first.");
            return;
        }
        
        if (replayGhost == null)
        {
            Debug.LogError("[TrajectoryReplayer] Replay Ghost not assigned!");
            return;
        }
        
        isPlaying = true;
        currentIndex = 0;
        replayStartTime = Time.time;
        
        // Reset ghost position to start
        if (recordedPath.Count > 0)
        {
            replayGhost.transform.position = recordedPath[0];
            replayGhost.SetActive(true);
        }
        
        Debug.Log($"[TrajectoryReplayer] Started replaying {recordedPath.Count} waypoints at {playbackSpeed}x speed");
    }
    
    /// <summary>
    /// Stop the replay
    /// </summary>
    public void StopReplay()
    {
        isPlaying = false;
        currentIndex = 0;
        Debug.Log("[TrajectoryReplayer] Replay stopped");
    }
    
    /// <summary>
    /// Reset replay to beginning
    /// </summary>
    public void ResetReplay()
    {
        StopReplay();
        if (recordedPath.Count > 0 && replayGhost != null)
        {
            replayGhost.transform.position = recordedPath[0];
        }
    }
    
    /// <summary>
    /// Create default material for path line if none provided
    /// </summary>
    Material CreateDefaultLineMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange semi-transparent
        return mat;
    }
}
