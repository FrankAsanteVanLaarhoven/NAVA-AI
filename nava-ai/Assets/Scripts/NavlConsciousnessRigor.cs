using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// NAVΛ Consciousness Rigor - Cognitive Safety Score calculation.
/// P(x) = Safety h(pos) + Goal Proximity (g) + Model Intent (i) + Consciousness (c)
/// 
/// This defines safety as a function of:
/// - Where you are (Goal)
/// - What you plan (Intent)
/// - How alert you are (Consciousness)
/// </summary>
public class NavlConsciousnessRigor : MonoBehaviour
{
    [Header("Cognitive Rigor: P = Safety + g + i + c")]
    [Tooltip("Target goal position")]
    public Transform targetGoal;
    
    [Tooltip("Maximum distance for goal proximity calculation")]
    public float maxGoalDistance = 10.0f;
    
    [Header("Safety Threshold")]
    [Tooltip("Minimum P-score for safe operation")]
    public float safetyThreshold = 50.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to VLA saliency overlay for intent confidence")]
    public VlaSaliencyOverlay vlaSaliency;
    
    [Tooltip("Reference to VNC 7D verifier for position safety")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to AdvancedEstimator for sensor state")]
    public AdvancedEstimator estimator;
    
    [Header("Visuals")]
    [Tooltip("LineRenderer for intent vector visualization")]
    public LineRenderer intentVector;
    
    [Tooltip("Light for consciousness visualization")]
    public Light consciousnessLight;
    
    [Tooltip("Text displaying rigor status")]
    public Text rigorStatus;
    
    [Header("Fatigue Settings")]
    [Tooltip("Enable fatigue simulation")]
    public bool enableFatigue = true;
    
    [Tooltip("Fatigue decay rate (per second)")]
    public float fatigueDecayRate = 0.1f;
    
    [Tooltip("Recovery rate (per second)")]
    public float recoveryRate = 0.05f;
    
    [Tooltip("Force fatigue key (for testing)")]
    public KeyCode forceFatigueKey = KeyCode.F;
    
    [Header("Obstacle Detection")]
    [Tooltip("Tag for obstacles")]
    public string obstacleTag = "Obstacle";
    
    // Cognitive Safety Components
    private float _g_goal = 0.0f;           // Goal Proximity (0-1)
    private float _i_intent = 0.8f;         // Model Intent/Confidence (0-1)
    private float _c_consciousness = 1.0f;   // Consciousness/Fatigue (0-1, start fully awake)
    private float _h_safety = 0.0f;         // Position Safety h(pos)
    
    private float totalP = 0f;
    private bool isConsciousnessFailure = false;
    private Rigidbody rb;

    void Start()
    {
        // Get references
        if (vlaSaliency == null)
        {
            vlaSaliency = GetComponent<VlaSaliencyOverlay>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        if (estimator == null)
        {
            estimator = GetComponent<AdvancedEstimator>();
        }
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
        
        // Create intent vector if not assigned
        if (intentVector == null)
        {
            GameObject intentObj = new GameObject("IntentVector");
            intentObj.transform.SetParent(transform);
            intentVector = intentObj.AddComponent<LineRenderer>();
            intentVector.startWidth = 0.2f;
            intentVector.endWidth = 0.05f;
            intentVector.material = new Material(Shader.Find("Sprites/Default"));
            intentVector.color = Color.blue;
            intentVector.useWorldSpace = true;
        }
        
        // Create consciousness light if not assigned
        if (consciousnessLight == null)
        {
            GameObject lightObj = new GameObject("ConsciousnessLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 1f;
            consciousnessLight = lightObj.AddComponent<Light>();
            consciousnessLight.type = LightType.Point;
            consciousnessLight.range = 5f;
            consciousnessLight.intensity = 2f;
            consciousnessLight.color = Color.white;
        }
        
        Debug.Log("[NavlConsciousnessRigor] Initialized - Cognitive safety calculation ready");
    }

    void Update()
    {
        // 1. Calculate g (Goal Proximity)
        CalculateGoalProximity();
        
        // 2. Calculate i (Intent / Model Confidence)
        CalculateModelIntent();
        
        // 3. Calculate c (Consciousness / Fatigue)
        if (enableFatigue)
        {
            SimulateFatigue();
        }
        
        // 4. Calculate h (Position Safety)
        CalculatePositionSafety();
        
        // 5. SUM IT UP (The Cognitive Safety Equation)
        totalP = _h_safety + _g_goal + _i_intent + _c_consciousness;
        
        // 6. ENFORCE THE RIGOR
        if (totalP < safetyThreshold)
        {
            TriggerConsciousnessFailure(totalP);
        }
        else
        {
            MaintainConsciousness(totalP);
        }
        
        // 7. Visualize Intent Vector
        DrawIntentVector();
    }

    void CalculateGoalProximity()
    {
        if (targetGoal == null)
        {
            _g_goal = 0.5f; // Default if no goal
            return;
        }
        
        // Normalized: 1.0f = At Goal, 0.0f = Far
        float distToGoal = Vector3.Distance(transform.position, targetGoal.position);
        _g_goal = 1.0f - Mathf.Clamp01(distToGoal / maxGoalDistance); // Closer = Higher
    }

    void CalculateModelIntent()
    {
        // Get intent confidence from VLA model
        if (vlaSaliency != null)
        {
            _i_intent = vlaSaliency.GetAverageConfidence();
        }
        else
        {
            // Fallback: Use UniversalModelManager confidence
            UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
            if (modelManager != null)
            {
                _i_intent = modelManager.GetModelConfidence();
            }
            else
            {
                _i_intent = 0.8f; // Default confidence
            }
        }
    }

    void CalculatePositionSafety()
    {
        // Get position safety from VNC 7D verifier
        if (vncVerifier != null)
        {
            float barrierValue = vncVerifier.GetBarrierValue();
            // Convert barrier value to safety score (0-100)
            // Barrier > 0 = safe, Barrier < 0 = unsafe
            _h_safety = Mathf.Clamp(barrierValue * 50f + 50f, 0f, 100f);
        }
        else
        {
            // Fallback: Calculate from obstacle distance
            float minDist = GetMinObstacleDistance();
            _h_safety = Mathf.Clamp(20.0f - minDist, 0f, 100f);
        }
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
        
        return minDist == float.MaxValue ? 10f : minDist;
    }

    void SimulateFatigue()
    {
        // Fatigue drops 'c' (Consciousness) over time
        if (Input.GetKey(forceFatigueKey)) // "Force Sleep" for testing
        {
            _c_consciousness -= Time.deltaTime * fatigueDecayRate;
        }
        else
        {
            // Natural recovery (wake up slowly)
            _c_consciousness = Mathf.Min(1.0f, _c_consciousness + Time.deltaTime * recoveryRate);
        }
        
        // Check sensor state (if estimator available)
        if (estimator != null)
        {
            // If RAIM fault detected, consciousness drops
            if (estimator.IsRAIMFaultDetected())
            {
                _c_consciousness -= Time.deltaTime * fatigueDecayRate * 2f; // Faster decay on sensor failure
            }
        }
        
        // Clamp consciousness
        _c_consciousness = Mathf.Clamp01(_c_consciousness);
    }

    void MaintainConsciousness(float pScore)
    {
        isConsciousnessFailure = false;
        
        if (rigorStatus != null)
        {
            rigorStatus.text = $"P-SCORE: {pScore:F1} (SAFE)\n" +
                              $"Safety: {_h_safety:F1} | Goal: {_g_goal:F2} | Intent: {_i_intent:F2} | Consciousness: {_c_consciousness:F2}";
            rigorStatus.color = Color.cyan;
        }
        
        // Update consciousness light
        if (consciousnessLight != null)
        {
            consciousnessLight.color = Color.Lerp(Color.red, Color.white, _c_consciousness);
            consciousnessLight.intensity = _c_consciousness * 2.0f; // Dim if tired
        }
    }

    void TriggerConsciousnessFailure(float pScore)
    {
        if (isConsciousnessFailure) return; // Already in failure state
        
        isConsciousnessFailure = true;
        
        if (rigorStatus != null)
        {
            rigorStatus.text = $"P-SCORE: {pScore:F1} (UNCONSCIOUS!)\n" +
                              $"Safety: {_h_safety:F1} | Goal: {_g_goal:F2} | Intent: {_i_intent:F2} | Consciousness: {_c_consciousness:F2}";
            rigorStatus.color = Color.red;
        }
        
        // Update consciousness light
        if (consciousnessLight != null)
        {
            consciousnessLight.color = Color.red;
            consciousnessLight.intensity = 5f;
        }
        
        // HARD BRAKE (Fatigue Override)
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
        
        Debug.LogError($"[NavlConsciousnessRigor] Consciousness Violation: P = {pScore:F1} (Threshold: {safetyThreshold})");
    }

    void DrawIntentVector()
    {
        if (intentVector == null) return;
        
        // Visualize where VLA model intends to go
        Vector3 intentDir = Vector3.zero;
        
        if (targetGoal != null)
        {
            intentDir = (targetGoal.position - transform.position).normalized;
        }
        else
        {
            // Use forward direction if no goal
            intentDir = transform.forward;
        }
        
        // Scale by intent confidence
        float intentLength = _i_intent * 3.0f;
        
        intentVector.positionCount = 2;
        Vector3 robotPos = transform.position + Vector3.up * 0.5f;
        intentVector.SetPosition(0, robotPos);
        intentVector.SetPosition(1, robotPos + intentDir * intentLength);
        
        // Color code intent
        if (_i_intent > 0.8f)
        {
            intentVector.startColor = Color.blue;
            intentVector.endColor = new Color(0, 0, 1, 0.3f);
        }
        else if (_i_intent > 0.5f)
        {
            intentVector.startColor = Color.yellow;
            intentVector.endColor = new Color(1, 1, 0, 0.3f);
        }
        else
        {
            intentVector.startColor = Color.red;
            intentVector.endColor = new Color(1, 0, 0, 0.3f);
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
    /// Get total P-score (alias for GetPScore for compatibility)
    /// </summary>
    public float GetTotalScore()
    {
        return totalP;
    }

    /// <summary>
    /// Get goal proximity (g)
    /// </summary>
    public float GetGoalProximity()
    {
        return _g_goal;
    }

    /// <summary>
    /// Get model intent (i)
    /// </summary>
    public float GetModelIntent()
    {
        return _i_intent;
    }

    /// <summary>
    /// Get consciousness level (c)
    /// </summary>
    public float GetConsciousness()
    {
        return _c_consciousness;
    }

    /// <summary>
    /// Check if consciousness failure is detected
    /// </summary>
    public bool IsConsciousnessFailure()
    {
        return isConsciousnessFailure;
    }

    /// <summary>
    /// Set consciousness level (for testing)
    /// </summary>
    public void SetConsciousness(float value)
    {
        _c_consciousness = Mathf.Clamp01(value);
    }
}
