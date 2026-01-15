using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// SPARK Temporal Verifier - Synchronization of Past and Reasoning with Knowledge.
/// Adds Temporal Logic to 7D Math. "If I enter Zone A, I MUST have exited Zone A within 5 seconds."
/// This certifies Sequence Safety.
/// </summary>
public class SparkTemporalVerifier : MonoBehaviour
{
    [System.Serializable]
    public class TemporalZone
    {
        [Tooltip("Zone identifier")]
        public string id;
        
        [Tooltip("Zone bounds")]
        public Bounds bounds;
        
        [Tooltip("Maximum time allowed inside (seconds)")]
        public float maxDuration;
        
        [Tooltip("Zone color for visualization")]
        public Color zoneColor = Color.yellow;
        
        [Tooltip("Zone active")]
        public bool active = true;
    }

    [Header("Temporal Zones")]
    [Tooltip("List of temporal constraint zones")]
    public List<TemporalZone> zones = new List<TemporalZone>();
    
    [Header("Visualization")]
    [Tooltip("Prefab for zone visualization")]
    public GameObject zonePrefab;
    
    [Tooltip("Text displaying temporal status")]
    public Text temporalStatusText;
    
    [Header("Logging")]
    [Tooltip("Enable formal log generation")]
    public bool enableFormalLogging = true;
    
    [Tooltip("Formal log file path")]
    public string logFilePath = "Assets/FormalLogs/spark_audit.txt";
    
    [Header("Violation Response")]
    [Tooltip("Enable hard stop on violation")]
    public bool enableHardStop = true;
    
    [Tooltip("Enable visual alerts")]
    public bool enableVisualAlerts = true;
    
    private Dictionary<string, float> entryTimes = new Dictionary<string, float>();
    private Dictionary<string, GameObject> zoneVisualizations = new Dictionary<string, GameObject>();
    private List<string> violationHistory = new List<string>();
    private bool hasActiveViolation = false;

    void Start()
    {
        // Create zone visualizations
        CreateZoneVisualizations();
        
        // Initialize log file
        if (enableFormalLogging)
        {
            InitializeLogFile();
        }
        
        Debug.Log("[SPARK] Initialized - Temporal logic verification ready");
    }

    void Update()
    {
        Vector3 pos = transform.position;
        hasActiveViolation = false;
        
        foreach (var zone in zones)
        {
            if (!zone.active) continue;
            
            bool inside = zone.bounds.Contains(pos);
            
            // SPARK LOGIC: Temporal Constraint
            if (inside)
            {
                if (!entryTimes.ContainsKey(zone.id))
                {
                    // Entered zone
                    entryTimes[zone.id] = Time.time;
                    LogZoneEntry(zone.id);
                }
                else
                {
                    // Still inside - check duration
                    float duration = Time.time - entryTimes[zone.id];
                    
                    // VIOLATION: Exceeded Max Duration
                    if (duration > zone.maxDuration)
                    {
                        TriggerTemporalFault(zone.id, duration, zone.maxDuration);
                        hasActiveViolation = true;
                    }
                    else
                    {
                        // Update visualization (approaching limit)
                        UpdateZoneVisualization(zone, duration / zone.maxDuration);
                    }
                }
            }
            else
            {
                // Exited zone safely
                if (entryTimes.ContainsKey(zone.id))
                {
                    float duration = Time.time - entryTimes[zone.id];
                    LogZoneExit(zone.id, duration);
                    entryTimes.Remove(zone.id);
                    
                    // Reset zone visualization
                    UpdateZoneVisualization(zone, 0f);
                }
            }
        }
        
        UpdateTemporalStatus();
    }

    void CreateZoneVisualizations()
    {
        foreach (var zone in zones)
        {
            if (zoneVisualizations.ContainsKey(zone.id)) continue;
            
            GameObject zoneObj;
            if (zonePrefab != null)
            {
                zoneObj = Instantiate(zonePrefab);
            }
            else
            {
                zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(zoneObj.GetComponent<Collider>()); // Remove collider for visualization only
            }
            
            zoneObj.name = $"TemporalZone_{zone.id}";
            zoneObj.transform.position = zone.bounds.center;
            zoneObj.transform.localScale = zone.bounds.size;
            
            Renderer renderer = zoneObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(zone.zoneColor.r, zone.zoneColor.g, zone.zoneColor.b, 0.3f);
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                renderer.material = mat;
            }
            
            zoneVisualizations[zone.id] = zoneObj;
        }
    }

    void UpdateZoneVisualization(TemporalZone zone, float progress)
    {
        if (!zoneVisualizations.ContainsKey(zone.id)) return;
        
        GameObject zoneObj = zoneVisualizations[zone.id];
        Renderer renderer = zoneObj.GetComponent<Renderer>();
        
        if (renderer != null)
        {
            Color c = Color.Lerp(zone.zoneColor, Color.red, progress);
            c.a = 0.3f;
            renderer.material.color = c;
        }
    }

    void TriggerTemporalFault(string zoneId, float duration, float maxDuration)
    {
        string violationMsg = $"[SPARK VIOLATION] Zone:{zoneId} Duration:{duration:F2}s > Limit:{maxDuration:F2}s";
        
        // 1. Hard Stop (Safety Override)
        if (enableHardStop)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Trigger Ironclad Manager
            IroncladManager ironclad = GetComponent<IroncladManager>();
            if (ironclad != null)
            {
                ironclad.TriggerLockdown();
            }
        }
        
        // 2. Log Formal Evidence
        if (enableFormalLogging)
        {
            LogViolation(violationMsg);
        }
        
        // 3. Visual Alert
        if (enableVisualAlerts)
        {
            CreateViolationMarker(zoneId);
        }
        
        // 4. Add to violation history
        if (!violationHistory.Contains(violationMsg))
        {
            violationHistory.Add(violationMsg);
        }
        
        Debug.LogError(violationMsg);
    }

    void CreateViolationMarker(string zoneId)
    {
        // Create Red "X" in air
        GameObject marker = new GameObject($"ViolationMarker_{zoneId}");
        marker.transform.position = transform.position + Vector3.up * 2f;
        
        // Create X shape using LineRenderer
        LineRenderer lr = marker.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.2f;
        lr.endWidth = 0.2f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.color = Color.red;
        lr.positionCount = 4;
        
        float size = 1f;
        lr.SetPosition(0, marker.transform.position + new Vector3(-size, size, 0));
        lr.SetPosition(1, marker.transform.position + new Vector3(size, -size, 0));
        lr.SetPosition(2, marker.transform.position + new Vector3(-size, -size, 0));
        lr.SetPosition(3, marker.transform.position + new Vector3(size, size, 0));
        
        // Destroy after 5 seconds
        Destroy(marker, 5f);
    }

    void LogZoneEntry(string zoneId)
    {
        if (!enableFormalLogging) return;
        
        string log = $"[{DateTime.Now:O}] ENTER Zone:{zoneId}";
        AppendToLog(log);
    }

    void LogZoneExit(string zoneId, float duration)
    {
        if (!enableFormalLogging) return;
        
        string log = $"[{DateTime.Now:O}] EXIT Zone:{zoneId} Duration:{duration:F2}s";
        AppendToLog(log);
    }

    void LogViolation(string violation)
    {
        if (!enableFormalLogging) return;
        
        string log = $"[{DateTime.Now:O}] {violation}";
        AppendToLog(log);
    }

    void AppendToLog(string log)
    {
        try
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.AppendAllText(logFilePath, log + "\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SPARK] Failed to write log: {e.Message}");
        }
    }

    void InitializeLogFile()
    {
        try
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, $"# SPARK Temporal Logic Audit Log\n" +
                                              $"# Generated: {DateTime.Now}\n" +
                                              $"# System: NAVA-AI Dashboard\n\n");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SPARK] Failed to initialize log file: {e.Message}");
        }
    }

    void UpdateTemporalStatus()
    {
        if (temporalStatusText != null)
        {
            if (hasActiveViolation)
            {
                temporalStatusText.text = "SPARK: VIOLATION DETECTED";
                temporalStatusText.color = Color.red;
            }
            else if (entryTimes.Count > 0)
            {
                temporalStatusText.text = $"SPARK: MONITORING ({entryTimes.Count} zones)";
                temporalStatusText.color = Color.yellow;
            }
            else
            {
                temporalStatusText.text = "SPARK: NO ACTIVE CONSTRAINTS";
                temporalStatusText.color = Color.green;
            }
        }
    }

    /// <summary>
    /// Add temporal zone programmatically
    /// </summary>
    public void AddTemporalZone(string id, Bounds bounds, float maxDuration)
    {
        zones.Add(new TemporalZone
        {
            id = id,
            bounds = bounds,
            maxDuration = maxDuration,
            active = true
        });
        
        CreateZoneVisualizations();
    }

    /// <summary>
    /// Check if there are active violations
    /// </summary>
    public bool HasActiveViolation()
    {
        return hasActiveViolation;
    }

    /// <summary>
    /// Get violation history
    /// </summary>
    public List<string> GetViolationHistory()
    {
        return new List<string>(violationHistory);
    }
}
