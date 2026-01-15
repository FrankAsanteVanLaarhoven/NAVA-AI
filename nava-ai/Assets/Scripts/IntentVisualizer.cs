using UnityEngine;

/// <summary>
/// Intent Visualizer - Visualizes the VLA/Neural Model's "Intent" separate from actual movement.
/// Shows where the AI model wants to go vs. where the robot actually is (Reality).
/// </summary>
public class IntentVisualizer : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("LineRenderer for intent visualization")]
    public LineRenderer intentLine;
    
    [Tooltip("Intent visualization length multiplier")]
    public float intentLengthMultiplier = 5.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to teleop controller for desired velocity")]
    public UnityTeleopController teleopController;
    
    [Tooltip("Reference to VLA saliency for confidence")]
    public VlaSaliencyOverlay vlaSaliency;
    
    [Tooltip("Reference to consciousness rigor for intent value")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Header("Color Coding")]
    [Tooltip("Color for high confidence intent (>0.8)")]
    public Color highConfidenceColor = Color.blue;
    
    [Tooltip("Color for medium confidence intent (0.5-0.8)")]
    public Color mediumConfidenceColor = Color.yellow;
    
    [Tooltip("Color for low confidence intent (<0.5)")]
    public Color lowConfidenceColor = Color.red;
    
    private Vector3 lastIntentPoint = Vector3.zero;
    private float lastConfidence = 1f;

    void Start()
    {
        // Get references if not assigned
        if (teleopController == null)
        {
            teleopController = GetComponent<UnityTeleopController>();
        }
        
        if (vlaSaliency == null)
        {
            vlaSaliency = GetComponent<VlaSaliencyOverlay>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        // Create intent line if not assigned
        if (intentLine == null)
        {
            GameObject intentObj = new GameObject("IntentLine");
            intentObj.transform.SetParent(transform);
            intentLine = intentObj.AddComponent<LineRenderer>();
            intentLine.startWidth = 0.3f;
            intentLine.endWidth = 0.1f;
            intentLine.material = new Material(Shader.Find("Sprites/Default"));
            intentLine.color = highConfidenceColor;
            intentLine.useWorldSpace = true;
        }
        
        Debug.Log("[IntentVisualizer] Initialized - Intent visualization ready");
    }

    void Update()
    {
        // Get "I" from VLA Model or teleop
        Vector3 desiredVel = GetDesiredVelocity();
        
        if (desiredVel == Vector3.zero)
        {
            // No intent - hide line
            if (intentLine != null)
            {
                intentLine.positionCount = 0;
            }
            return;
        }
        
        // Get confidence (intent value)
        float confidence = GetIntentConfidence();
        
        // Draw line from Robot Center -> Future Intent Point
        Vector3 robotPos = transform.position + Vector3.up * 0.5f;
        float intentLength = confidence * intentLengthMultiplier;
        Vector3 intentPoint = robotPos + desiredVel.normalized * intentLength;
        
        if (intentLine != null)
        {
            intentLine.positionCount = 2;
            intentLine.SetPosition(0, robotPos);
            intentLine.SetPosition(1, intentPoint);
            
            // Color Code Intent based on confidence
            Color intentColor;
            if (confidence > 0.8f)
            {
                intentColor = highConfidenceColor;
            }
            else if (confidence > 0.5f)
            {
                intentColor = mediumConfidenceColor;
            }
            else
            {
                intentColor = lowConfidenceColor;
            }
            
            intentLine.startColor = intentColor;
            intentLine.endColor = new Color(intentColor.r, intentColor.g, intentColor.b, 0.3f);
        }
        
        lastIntentPoint = intentPoint;
        lastConfidence = confidence;
    }

    Vector3 GetDesiredVelocity()
    {
        // Try to get from teleop controller
        if (teleopController != null)
        {
            Vector3 vel = teleopController.GetDesiredVelocity();
            if (vel != Vector3.zero) return vel;
        }
        
        // Try to get from consciousness rigor (goal direction)
        if (consciousnessRigor != null && consciousnessRigor.targetGoal != null)
        {
            Vector3 toGoal = (consciousnessRigor.targetGoal.position - transform.position).normalized;
            return toGoal * 1f; // Normalized direction
        }
        
        // Fallback: Use forward direction
        return transform.forward;
    }

    float GetIntentConfidence()
    {
        // Get from consciousness rigor (i value)
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetModelIntent();
        }
        
        // Get from VLA saliency
        if (vlaSaliency != null)
        {
            return vlaSaliency.GetAverageConfidence();
        }
        
        // Fallback
        return 0.8f;
    }

    /// <summary>
    /// Get last intent point (for other visualizations)
    /// </summary>
    public Vector3 GetLastIntentPoint()
    {
        return lastIntentPoint;
    }

    /// <summary>
    /// Get last confidence value
    /// </summary>
    public float GetLastConfidence()
    {
        return lastConfidence;
    }
}
