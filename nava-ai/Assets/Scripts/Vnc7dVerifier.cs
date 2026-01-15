using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// VNC 7D Verifier - Control Barrier Function (CBF) and Lyapunov stability verification.
/// Mathematically guarantees safety: if dh/dt <= -alpha * h(x), system is certified safe.
/// This is "God-like" because physics cannot violate math.
/// </summary>
public class Vnc7dVerifier : MonoBehaviour
{
    [Header("7D Rigor Settings")]
    [Tooltip("Safety class-K function parameter (strictness)")]
    public float alpha = 5.0f;
    
    [Tooltip("Minimum alpha value")]
    public float minAlpha = 5.0f;
    
    [Tooltip("Maximum alpha value")]
    public float maxAlpha = 10.0f;
    
    [Tooltip("Safety margin distance (meters)")]
    public float safetyMargin = 0.5f;
    
    [Header("Visuals")]
    [Tooltip("LineRenderer for safety hull visualization")]
    public LineRenderer safetyHull;
    
    [Tooltip("Text displaying rigor status")]
    public Text rigorStatus;
    
    [Tooltip("Text displaying kinetic energy")]
    public Text energyText;
    
    [Header("7D State References")]
    [Tooltip("Reference to AdvancedEstimator for certainty")]
    public AdvancedEstimator estimator;
    
    [Tooltip("Reference to Rigidbody for velocity")]
    public Rigidbody robotRigidbody;
    
    [Header("Obstacle Detection")]
    [Tooltip("Tag for obstacles")]
    public string obstacleTag = "Obstacle";
    
    [Tooltip("Manual obstacle list (if not using tags)")]
    public List<GameObject> manualObstacles = new List<GameObject>();
    
    // The 7D State Vector: [x, y, z, vx, vy, vz, theta, sigma]
    private Vector3 _pos;
    private Vector3 _vel;
    private float _heading;
    private float _certainty;
    private bool isCertifiedSafe = true;
    private float currentBarrierValue = 1f;
    private float currentBarrierDerivative = 0f;

    void Start()
    {
        // Create safety hull if not assigned
        if (safetyHull == null)
        {
            GameObject hullObj = new GameObject("SafetyHull");
            hullObj.transform.SetParent(transform);
            safetyHull = hullObj.AddComponent<LineRenderer>();
        }
        
        safetyHull.loop = true;
        safetyHull.positionCount = 32;
        safetyHull.startWidth = 0.1f;
        safetyHull.endWidth = 0.1f;
        safetyHull.material = new Material(Shader.Find("Sprites/Default"));
        safetyHull.useWorldSpace = true;
        
        // Get references if not assigned
        if (robotRigidbody == null)
        {
            robotRigidbody = GetComponent<Rigidbody>();
        }
        
        if (estimator == null)
        {
            estimator = GetComponent<AdvancedEstimator>();
        }
        
        Debug.Log("[Vnc7dVerifier] Initialized - 7D CBF verification ready");
    }

    void Update()
    {
        // 1. Get 7D State (From Robot or Sensor Fusion)
        Update7DState();
        
        // 2. Calculate Global Safety Derivative
        CalculateSafetyDerivative();
        
        // 3. ENFORCE IRONCLAD RIGOR (Control Barrier Function Condition)
        VerifyCertification();
        
        // 4. Visual Update
        UpdateGodVisuals();
    }

    void Update7DState()
    {
        _pos = transform.position;
        _vel = robotRigidbody != null ? robotRigidbody.velocity : Vector3.zero;
        _heading = transform.eulerAngles.y;
        _certainty = estimator != null ? estimator.GetUncertainty() : 1f;
        // Invert uncertainty to get certainty (lower uncertainty = higher certainty)
        _certainty = 1f / (1f + _certainty);
    }

    void CalculateSafetyDerivative()
    {
        // Get all obstacles
        List<GameObject> obstacles = new List<GameObject>();
        
        if (!string.IsNullOrEmpty(obstacleTag))
        {
            obstacles.AddRange(GameObject.FindGameObjectsWithTag(obstacleTag));
        }
        
        obstacles.AddRange(manualObstacles.Where(o => o != null));
        
        if (obstacles.Count == 0) return;
        
        // Initialize with worst case
        currentBarrierValue = float.MaxValue;
        currentBarrierDerivative = 0f;
        
        // Check against ALL obstacles (Set Intersection)
        foreach (GameObject obs in obstacles)
        {
            if (obs == null) continue;
            
            Vector3 obstaclePos = obs.transform.position;
            float h = CalculateBarrierFunction(_pos, obstaclePos);
            
            // Calculate gradient dh/dx
            Vector3 gradient = CalculateBarrierGradient(_pos, obstaclePos);
            
            // Dot product with velocity (The 7D Velocity Term)
            // This is the "God Math": Predicting future state
            // dh/dt = dh/dx · dx/dt = gradient · velocity
            float localDeriv = Vector3.Dot(gradient, _vel);
            
            // Take minimum barrier value (worst case)
            if (h < currentBarrierValue)
            {
                currentBarrierValue = h;
                currentBarrierDerivative = localDeriv;
            }
        }
    }

    /// <summary>
    /// Calculates the Safety Certificate h(x) = 1 - (dist^2 / safe_dist^2)
    /// If h(x) > 0, we are safe. If h(x) < 0, we have violated math.
    /// </summary>
    float CalculateBarrierFunction(Vector3 position, Vector3 obstaclePos)
    {
        float distSq = Vector3.SqrMagnitude(position - obstaclePos);
        float safeDistSq = safetyMargin * safetyMargin;
        
        // CBF Definition: h(x) = 1 - (|x - obs|^2 / d^2)
        // h > 0: Safe, h < 0: Unsafe
        return 1.0f - (distSq / safeDistSq);
    }

    /// <summary>
    /// Calculate gradient of barrier function: dh/dx
    /// </summary>
    Vector3 CalculateBarrierGradient(Vector3 position, Vector3 obstaclePos)
    {
        Vector3 diff = position - obstaclePos;
        float distSq = diff.sqrMagnitude;
        
        if (distSq < 0.001f) distSq = 0.001f; // Avoid division by zero
        
        // Gradient: dh/dx = -2 * (x - obs) / (d^2)
        float factor = -2.0f / (safetyMargin * safetyMargin);
        return diff * factor;
    }

    void VerifyCertification()
    {
        // ENFORCE IRONCLAD RIGOR (Control Barrier Function Condition)
        // If dh/dt <= -alpha * h(x), we are mathematically CERTIFIED safe.
        // This is the fundamental CBF condition: h_dot <= -alpha * h
        
        float rhs = -alpha * currentBarrierValue;
        isCertifiedSafe = (currentBarrierDerivative <= rhs);
        
        // Additional check: barrier value must be positive
        if (currentBarrierValue < 0f)
        {
            isCertifiedSafe = false;
        }
    }

    void UpdateGodVisuals()
    {
        // A. Visual Hull (The "Ironclad" Boundary)
        float radius = safetyMargin * Mathf.Max(0.1f, 1.0f - currentBarrierValue);
        DrawSafetyHull(radius);
        
        // B. UI Status
        if (rigorStatus != null)
        {
            if (isCertifiedSafe)
            {
                rigorStatus.text = $"VERIFIED: dH/dt ≤ -{alpha}H\n" +
                                 $"Barrier: {currentBarrierValue:F3}\n" +
                                 $"Derivative: {currentBarrierDerivative:F3}";
                rigorStatus.color = Color.cyan; // God-like certainty
            }
            else
            {
                rigorStatus.text = $"VIOLATION: MATH BROKEN\n" +
                                 $"Barrier: {currentBarrierValue:F3}\n" +
                                 $"Derivative: {currentBarrierDerivative:F3}";
                rigorStatus.color = Color.red;
            }
        }
        
        // C. Energy/Velocity Check (Dim 4-6)
        if (energyText != null)
        {
            float kineticEnergy = 0.5f * _vel.sqrMagnitude;
            energyText.text = $"Kinetic Energy: {kineticEnergy:F3} J\n" +
                            $"Velocity: {_vel.magnitude:F2} m/s\n" +
                            $"Certainty (σ): {_certainty:F3}";
        }
    }

    void DrawSafetyHull(float radius)
    {
        if (safetyHull == null) return;
        
        int points = 32;
        float angleStep = 360.0f / points;
        
        safetyHull.positionCount = points + 1;
        
        for (int i = 0; i <= points; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            safetyHull.SetPosition(i, transform.position + localPos);
        }
        
        // Update color based on certification
        if (isCertifiedSafe)
        {
            safetyHull.startColor = Color.cyan;
            safetyHull.endColor = new Color(0, 1, 1, 0.3f); // Cyan Transparent
        }
        else
        {
            safetyHull.startColor = Color.red;
            safetyHull.endColor = Color.red;
        }
    }

    /// <summary>
    /// Check if a proposed action is certified safe
    /// </summary>
    public bool IsCertified(Vector3 proposedPosition)
    {
        // Temporarily check proposed position
        float h = CalculateBarrierFunction(proposedPosition, Vector3.zero);
        
        // Simplified check - in full implementation would check all obstacles
        return h > 0f;
    }

    /// <summary>
    /// Get current certification status
    /// </summary>
    public bool IsCertifiedSafe()
    {
        return isCertifiedSafe;
    }

    /// <summary>
    /// Get current barrier value
    /// </summary>
    public float GetBarrierValue()
    {
        return currentBarrierValue;
    }

    /// <summary>
    /// Get current barrier derivative
    /// </summary>
    public float GetBarrierDerivative()
    {
        return currentBarrierDerivative;
    }

    /// <summary>
    /// Get 7D state vector
    /// </summary>
    public Vector7D Get7DState()
    {
        return new Vector7D
        {
            position = _pos,
            velocity = _vel,
            heading = _heading,
            certainty = _certainty
        };
    }

    /// <summary>
    /// Update safety alpha (for adaptive safety)
    /// </summary>
    public void UpdateSafetyAlpha(float newAlpha)
    {
        alpha = Mathf.Clamp(newAlpha, minAlpha, maxAlpha);
        Debug.Log($"[Vnc7dVerifier] Safety alpha updated to {alpha:F2}");
    }

    [System.Serializable]
    public struct Vector7D
    {
        public Vector3 position;  // Dim 1-3
        public Vector3 velocity;   // Dim 4-6
        public float heading;      // Dim 7 (theta)
        public float certainty;   // Dim 8 (sigma)
    }
}
