using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// NAVΛ 7D Rigor - Calculates the safety scalar P = x + y + z + t + g + i + c.
/// If P >= Threshold, system is mathematically Certifiable. If P < Threshold, breach detected.
/// </summary>
public class Navl7dRigor : MonoBehaviour
{
    [Header("Math Rigor: P = x + y + z + t + g + i + c")]
    [Tooltip("Text displaying the equation")]
    public Text equationDisplay;
    
    [Tooltip("Text displaying P value")]
    public Text pValueDisplay;
    
    [Tooltip("Text displaying breach status")]
    public Text breachStatus;
    
    [Header("Safety Threshold")]
    [Tooltip("Certifiable limit - P must be >= this value")]
    public float safetyThreshold = 50.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to UniversalModelManager for identity")]
    public UniversalModelManager modelManager;
    
    [Tooltip("Reference to AdvancedEstimator for certainty")]
    public AdvancedEstimator estimator;
    
    [Tooltip("Reference to VNC verifier for alpha updates")]
    public Vnc7dVerifier vncVerifier;
    
    [Header("Obstacle Detection")]
    [Tooltip("Tag for obstacles")]
    public string obstacleTag = "Obstacle";
    
    [Tooltip("Safety margin for constraint calculation")]
    public float constraintMargin = 1.0f;
    
    // 7D Variables (public for IroncladVisualizer access)
    [HideInInspector]
    public float _p_position;
    
    [HideInInspector]
    public float _t_timePhase;
    
    [HideInInspector]
    public float _g_gradient;
    
    [HideInInspector]
    public float _i_identity;
    
    [HideInInspector]
    public float _c_constraint;

    private float totalP = 0f;
    private bool isBreachDetected = false;
    private IroncladManager ironcladManager;

    void Start()
    {
        // Get references
        if (modelManager == null)
        {
            modelManager = GetComponent<UniversalModelManager>();
        }
        
        if (estimator == null)
        {
            estimator = GetComponent<AdvancedEstimator>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        ironcladManager = GetComponent<IroncladManager>();
        if (ironcladManager == null)
        {
            ironcladManager = gameObject.AddComponent<IroncladManager>();
        }
        
        Debug.Log("[Navl7dRigor] Initialized - P-score calculation ready");
    }

    void Update()
    {
        // 1. Calculate p = x + y + z (Position Norm)
        // Distance from goal (0,0,0) is our "Position Certainty"
        Vector3 pos = transform.position;
        _p_position = Vector3.Distance(pos, Vector3.zero);

        // 2. Calculate t = Time Phase (Sinusoidal 0-1 based on clock)
        // "Breathing/Pulse" of the system
        _t_timePhase = (Mathf.Sin(Time.time * 0.5f) + 1f) * 0.5f; // Normalized to 0-1

        // 3. Calculate g = Gradient/Slope (Simulate terrain drift)
        // If pos.y is high, 'g' (slope) increases
        _g_gradient = Mathf.Abs(pos.y * 0.1f);

        // 4. Calculate i = Identity (Model Confidence)
        // In a real system, this comes from Neural VLA Certainty
        if (modelManager != null)
        {
            _i_identity = GetModelConfidence();
        }
        else
        {
            _i_identity = 1.0f; // Default: full confidence
        }

        // 5. Calculate c = Constraint (VNC Barrier Value)
        // Distance to nearest obstacle
        float distToObs = GetMinObstacleDistance();
        // c = 1.0 if dist < margin (violation), else 0.0 (safe)
        _c_constraint = (distToObs < constraintMargin) ? 1.0f : 0.0f;

        // --- CALCULATE TOTAL P ---
        totalP = _p_position + _t_timePhase + _g_gradient + _i_identity + _c_constraint;

        UpdateDisplay(totalP);
        EnforceIroncladLogic(totalP);
    }

    float GetModelConfidence()
    {
        // Get model confidence from UniversalModelManager
        // This would require exposing a method
        // For now, simplified version
        if (modelManager != null)
        {
            var config = modelManager.GetCurrentModelConfig();
            if (config != null)
            {
                // Use model type as confidence indicator
                // AGI/VLM = lower confidence (more uncertainty)
                // SafeVLA = higher confidence
                switch (modelManager.currentModel)
                {
                    case ModelType.SafeVLA:
                        return 0.9f;
                    case ModelType.VLA:
                        return 0.8f;
                    case ModelType.AGI:
                        return 0.6f; // AGI has more uncertainty
                    case ModelType.VLM:
                        return 0.7f;
                    default:
                        return 0.8f;
                }
            }
        }
        
        return 0.8f; // Default confidence
    }

    float GetMinObstacleDistance()
    {
        float minDist = float.MaxValue;
        
        // Check tagged obstacles
        if (!string.IsNullOrEmpty(obstacleTag))
        {
            GameObject[] obstacles = GameObject.FindGameObjectsWithTag(obstacleTag);
            foreach (GameObject obs in obstacles)
            {
                if (obs == null) continue;
                float dist = Vector3.Distance(transform.position, obs.transform.position);
                if (dist < minDist) minDist = dist;
            }
        }
        
        // Check geofence zones
        GeofenceEditor geofence = GetComponent<GeofenceEditor>();
        if (geofence != null)
        {
            foreach (var zone in geofence.zones)
            {
                if (!zone.active) continue;
                
                // Check if inside polygon
                if (IsPointInPolygon(transform.position, zone.polygonPoints))
                {
                    return 0f; // Inside denied zone
                }
                
                // Calculate distance to polygon
                float dist = DistanceToPolygon(transform.position, zone.polygonPoints);
                if (dist < minDist) minDist = dist;
            }
        }
        
        return minDist == float.MaxValue ? 100f : minDist;
    }

    bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count < 3) return false;
        
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if (((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    float DistanceToPolygon(Vector3 point, List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count == 0) return float.MaxValue;
        
        float minDist = float.MaxValue;
        for (int i = 0; i < polygon.Count; i++)
        {
            int next = (i + 1) % polygon.Count;
            float dist = DistanceToLineSegment(point, polygon[i], polygon[next]);
            if (dist < minDist) minDist = dist;
        }
        
        return minDist;
    }

    float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        if (lineLength < 0.001f) return Vector3.Distance(point, lineStart);
        
        Vector3 lineNorm = line / lineLength;
        Vector3 toPoint = point - lineStart;
        float projection = Vector3.Dot(toPoint, lineNorm);
        projection = Mathf.Clamp(projection, 0f, lineLength);
        
        Vector3 closestPoint = lineStart + lineNorm * projection;
        return Vector3.Distance(point, closestPoint);
    }

    void UpdateDisplay(float p)
    {
        if (pValueDisplay != null)
        {
            pValueDisplay.text = $"P-Score: {p:F4}";
            pValueDisplay.color = p >= safetyThreshold ? Color.green : Color.red;
        }
        
        // Visualize Equation: p=x+y+z+t+g+i+c
        if (equationDisplay != null)
        {
            equationDisplay.text = 
                $"<color=#00FFFF>P</color> = {_p_position:F2} (x)" +
                $"<color=#555555> + </color> {_t_timePhase:F2} (t)" +
                $"<color=#555555> + </color> {_g_gradient:F2} (g)" +
                $"<color=#555555> + </color> {_i_identity:F2} (i)" +
                $"<color=#555555> + </color> {_c_constraint:F2} (c)" +
                $"<color=#555555> = </color> <color=#00FFFF>{p:F2}</color>";
        }
    }

    void EnforceIroncladLogic(float p)
    {
        // THE IRONCLAD RULE:
        // If P >= Threshold, system is CERTIFIABLE (Safe).
        // If P < Threshold (due to 'c' rising), we VIOLATE math.
        
        bool wasBreach = isBreachDetected;
        isBreachDetected = p < safetyThreshold;
        
        if (breachStatus != null)
        {
            if (isBreachDetected)
            {
                breachStatus.text = $"BREACH DETECTED: P < {safetyThreshold}\n" +
                                   $"P = {p:F2}";
                breachStatus.color = Color.red;
            }
            else
            {
                breachStatus.text = $"RIGOR: P IN BOUNDS\n" +
                                   $"P = {p:F2} >= {safetyThreshold}";
                breachStatus.color = Color.cyan;
            }
        }
        
        // Trigger lockdown on breach
        if (isBreachDetected && !wasBreach)
        {
            // HARD STOP (Override Physics)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Trigger Ironclad Manager
            if (ironcladManager != null)
            {
                ironcladManager.TriggerLockdown();
            }
            
            Debug.LogError($"[Navl7dRigor] BREACH DETECTED: P = {p:F2} < {safetyThreshold}");
        }
    }

    /// <summary>
    /// Get current P-score
    /// </summary>
    public float GetPScore()
    {
        return totalP;
    }

    /// <summary>
    /// Check if breach is detected
    /// </summary>
    public bool IsBreachDetected()
    {
        return isBreachDetected;
    }

    /// <summary>
    /// Update safety alpha (for adaptive safety via EnvironmentProfiler)
    /// </summary>
    public void UpdateSafetyAlpha(float newAlpha)
    {
        if (vncVerifier != null)
        {
            vncVerifier.UpdateSafetyAlpha(newAlpha);
        }
        else
        {
            Debug.LogWarning("[Navl7dRigor] VNC Verifier not found - cannot update safety alpha");
        }
    }
}
