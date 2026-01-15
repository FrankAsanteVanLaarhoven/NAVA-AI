using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Temporal Fusion Visualizer - Three-State Visualization (History + Present + Shadow).
/// Shows "What the robot did" (History), "What it is doing" (Present), and "What it will do" (Shadow) all in one frame.
/// </summary>
public class TemporalFusionVisualizer : MonoBehaviour
{
    [Header("Robot References")]
    [Tooltip("Current robot (solid, present state)")]
    public GameObject currentRobot;
    
    [Tooltip("Shadow robot (transparent, future prediction)")]
    public GameObject shadowRobot;
    
    [Tooltip("History ghost (fading, past trajectory)")]
    public GameObject historyGhost;
    
    [Header("Temporal Settings")]
    [Tooltip("Number of history samples to keep")]
    public int historySize = 50;
    
    [Tooltip("Update rate for history recording (Hz)")]
    public float historyUpdateRate = 10f;
    
    [Header("Visualization")]
    [Tooltip("Trail renderer for temporal path")]
    public TrailRenderer temporalTrail;
    
    [Tooltip("Color for current state (blue)")]
    public Color currentColor = Color.blue;
    
    [Tooltip("Color for shadow state (purple)")]
    public Color shadowColor = new Color(1f, 0f, 1f, 0.5f);
    
    [Tooltip("Color for history (white, fading)")]
    public Color historyColor = Color.white;
    
    [Tooltip("Fade speed for history ghost")]
    public float fadeSpeed = 0.1f;
    
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Queue<Quaternion> rotationHistory = new Queue<Quaternion>();
    private float lastHistoryUpdate = 0f;
    private float historyUpdateInterval;
    private Material historyMaterial;
    private float historyAlpha = 1f;

    void Start()
    {
        historyUpdateInterval = 1f / historyUpdateRate;
        
        // Create history ghost if not assigned
        if (historyGhost == null && currentRobot != null)
        {
            historyGhost = Instantiate(currentRobot);
            historyGhost.name = "HistoryGhost";
            historyGhost.transform.SetParent(transform);
            
            // Make it semi-transparent
            Renderer renderer = historyGhost.GetComponent<Renderer>();
            if (renderer != null)
            {
                historyMaterial = new Material(renderer.material);
                historyMaterial.color = new Color(historyColor.r, historyColor.g, historyColor.b, 0.3f);
                renderer.material = historyMaterial;
            }
        }
        
        // Create trail renderer if not assigned
        if (temporalTrail == null && currentRobot != null)
        {
            temporalTrail = currentRobot.AddComponent<TrailRenderer>();
            temporalTrail.time = historySize / historyUpdateRate;
            temporalTrail.startWidth = 0.2f;
            temporalTrail.endWidth = 0.05f;
            temporalTrail.material = new Material(Shader.Find("Sprites/Default"));
            temporalTrail.color = currentColor;
        }
        
        Debug.Log("[TemporalFusionVisualizer] Initialized - Recording temporal history");
    }

    void Update()
    {
        if (currentRobot == null) return;
        
        // Record history at specified rate
        if (Time.time - lastHistoryUpdate >= historyUpdateInterval)
        {
            RecordHistory();
            lastHistoryUpdate = Time.time;
        }
        
        // Update visualizations
        UpdateHistoryGhost();
        UpdateTemporalTrail();
    }

    void RecordHistory()
    {
        // Record current position and rotation
        positionHistory.Enqueue(currentRobot.transform.position);
        rotationHistory.Enqueue(currentRobot.transform.rotation);
        
        // Limit history size
        if (positionHistory.Count > historySize)
        {
            positionHistory.Dequeue();
            rotationHistory.Dequeue();
        }
    }

    void UpdateHistoryGhost()
    {
        if (historyGhost == null || positionHistory.Count == 0) return;
        
        // Calculate average position (centroid of history)
        Vector3 avgPos = Vector3.zero;
        Quaternion avgRot = Quaternion.identity;
        
        Vector3[] positions = positionHistory.ToArray();
        Quaternion[] rotations = rotationHistory.ToArray();
        
        foreach (Vector3 pos in positions)
        {
            avgPos += pos;
        }
        avgPos /= positions.Length;
        
        // Average rotation (simplified - use most recent)
        if (rotations.Length > 0)
        {
            avgRot = rotations[rotations.Length - 1];
        }
        
        // Update ghost position
        historyGhost.transform.position = avgPos;
        historyGhost.transform.rotation = avgRot;
        
        // Fade based on history age
        float gradient = (float)positionHistory.Count / historySize;
        historyAlpha = Mathf.Lerp(0.1f, 0.5f, gradient);
        
        if (historyMaterial != null)
        {
            Color c = historyMaterial.color;
            c.a = historyAlpha;
            historyMaterial.color = c;
        }
    }

    void UpdateTemporalTrail()
    {
        if (temporalTrail == null) return;
        
        // Update trail color gradient based on history
        float gradient = positionHistory.Count > 0 ? (float)positionHistory.Count / historySize : 0f;
        
        // Create gradient: Blue (now) -> White (past)
        Color trailColor = Color.Lerp(historyColor, currentColor, gradient);
        temporalTrail.color = trailColor;
        
        // Update trail width based on recency
        temporalTrail.startWidth = 0.2f * gradient;
    }

    /// <summary>
    /// Get average position from history
    /// </summary>
    public Vector3 GetAveragePosition()
    {
        if (positionHistory.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in positionHistory)
        {
            sum += pos;
        }
        return sum / positionHistory.Count;
    }

    /// <summary>
    /// Get predicted future position (extrapolation)
    /// </summary>
    public Vector3 GetPredictedPosition(float secondsAhead)
    {
        if (positionHistory.Count < 2) return currentRobot != null ? currentRobot.transform.position : Vector3.zero;
        
        Vector3[] positions = positionHistory.ToArray();
        Vector3 velocity = (positions[positions.Length - 1] - positions[positions.Length - 2]) / historyUpdateInterval;
        
        return positions[positions.Length - 1] + velocity * secondsAhead;
    }

    /// <summary>
    /// Clear history
    /// </summary>
    public void ClearHistory()
    {
        positionHistory.Clear();
        rotationHistory.Clear();
        if (temporalTrail != null)
        {
            temporalTrail.Clear();
        }
    }

    /// <summary>
    /// Get history as array for analysis
    /// </summary>
    public Vector3[] GetHistoryPositions()
    {
        return positionHistory.ToArray();
    }
}
