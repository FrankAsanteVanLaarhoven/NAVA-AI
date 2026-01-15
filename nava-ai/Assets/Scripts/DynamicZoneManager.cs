using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dynamic Zone Manager - Context-Aware God Mode.
/// The "God Mode" zones (SPARK logic) are not static. They "breathe" (expand/contract)
/// based on the Certainty (P) from the 7D Rigor.
/// High Certainty = Tight Control (Performance).
/// Low Certainty = Large Buffers (Safety).
/// </summary>
public class DynamicZoneManager : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("LineRenderer for zone ring")]
    public LineRenderer zoneRing;
    
    [Tooltip("Particle system for zone particles")]
    public ParticleSystem zoneParticles;
    
    [Tooltip("Material for zone ring")]
    public Material zoneMaterial;
    
    [Header("Zone Settings")]
    [Tooltip("Minimum zone radius (high certainty)")]
    public float minRadius = 2.0f;
    
    [Tooltip("Maximum zone radius (low certainty)")]
    public float maxRadius = 5.0f;
    
    [Tooltip("Zone animation speed")]
    public float animationSpeed = 2.0f;
    
    [Tooltip("Low certainty threshold (expand zone)")]
    public float lowCertaintyThreshold = 40.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to self-healing safety")]
    public SelfHealingSafety selfHealingSafety;
    
    [Tooltip("Reference to VNC verifier")]
    public Vnc7dVerifier vncVerifier;
    
    [Header("UI References")]
    [Tooltip("Text displaying zone status")]
    public Text zoneStatusText;
    
    private float currentRadius = 2.0f;
    private float targetRadius = 2.0f;
    private int zonePoints = 64;

    void Start()
    {
        // Get component references if not assigned
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (selfHealingSafety == null)
        {
            selfHealingSafety = GetComponent<SelfHealingSafety>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        // Create zone ring if not assigned
        if (zoneRing == null)
        {
            GameObject ringObj = new GameObject("ZoneRing");
            ringObj.transform.SetParent(transform);
            zoneRing = ringObj.AddComponent<LineRenderer>();
            zoneRing.useWorldSpace = true;
            zoneRing.startWidth = 0.2f;
            zoneRing.endWidth = 0.2f;
            zoneRing.material = zoneMaterial != null ? zoneMaterial : CreateDefaultMaterial();
            zoneRing.loop = true;
        }
        
        // Create zone particles if not assigned
        if (zoneParticles == null)
        {
            GameObject particlesObj = new GameObject("ZoneParticles");
            particlesObj.transform.SetParent(transform);
            zoneParticles = particlesObj.AddComponent<ParticleSystem>();
            
            var main = zoneParticles.main;
            main.startLifetime = 2.0f;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;
            main.startColor = Color.cyan;
            main.loop = true;
            main.playOnAwake = true;
            
            var shape = zoneParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = currentRadius;
            
            var emission = zoneParticles.emission;
            emission.rateOverTime = 20;
        }
        
        currentRadius = minRadius;
        targetRadius = minRadius;
        
        Debug.Log("[DynamicZoneManager] Initialized - Context-aware zones ready");
    }

    void Update()
    {
        // 1. Get "Certainty" from 7D Rigor (P-Score)
        float pScore = GetCertaintyScore();
        
        // 2. Map Certainty to Geometry (Adaptive Radius)
        // P = 100 (High Certainty) -> Radius 2.0
        // P = 0   (Low Certainty) -> Radius 5.0
        targetRadius = Mathf.Lerp(maxRadius, minRadius, pScore / 100.0f);
        
        // 3. Smooth animation
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * animationSpeed);
        
        // 4. Visualize "Breathing" Zone
        DrawZone(currentRadius);
        
        // 5. Logic: Low Certainty = "Safe Mode"
        if (pScore < lowCertaintyThreshold)
        {
            // We are uncertain. Expand buffer.
            if (selfHealingSafety != null)
            {
                selfHealingSafety.SetMargin(currentRadius);
            }
        }
        
        // 6. Update UI
        UpdateUI(pScore);
    }

    float GetCertaintyScore()
    {
        // Get P-score from consciousness rigor
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetTotalScore();
        }
        
        // Fallback: Use VNC barrier value
        if (vncVerifier != null)
        {
            float barrier = vncVerifier.GetBarrierValue();
            return (barrier + 1f) * 50f; // Convert to 0-100 scale
        }
        
        return 50f; // Default medium certainty
    }

    void DrawZone(float radius)
    {
        if (zoneRing == null) return;
        
        // Create circle points
        zoneRing.positionCount = zonePoints + 1;
        
        for (int i = 0; i <= zonePoints; i++)
        {
            float angle = (float)i / zonePoints * 360f * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0.1f, // Slightly above ground
                Mathf.Sin(angle) * radius
            );
            zoneRing.SetPosition(i, pos);
        }
        
        // Update particle system shape
        if (zoneParticles != null)
        {
            var shape = zoneParticles.shape;
            shape.radius = radius;
            
            // Color code based on certainty
            var main = zoneParticles.main;
            float certainty = GetCertaintyScore() / 100f;
            Color zoneColor = Color.Lerp(Color.red, Color.green, certainty);
            main.startColor = zoneColor;
        }
        
        // Color code zone ring
        if (zoneRing != null)
        {
            float certainty = GetCertaintyScore() / 100f;
            Color ringColor = Color.Lerp(Color.red, Color.green, certainty);
            ringColor.a = 0.5f;
            zoneRing.startColor = ringColor;
            zoneRing.endColor = new Color(ringColor.r, ringColor.g, ringColor.b, 0.1f);
        }
    }

    void UpdateUI(float pScore)
    {
        if (zoneStatusText == null) return;
        
        string certaintyLevel = pScore > 70f ? "HIGH" : (pScore > 40f ? "MEDIUM" : "LOW");
        string mode = pScore < lowCertaintyThreshold ? "SAFE MODE" : "PERFORMANCE MODE";
        
        zoneStatusText.text = $"ZONE: Radius={currentRadius:F2}m | Certainty={pScore:F1} ({certaintyLevel}) | {mode}";
        
        // Color code
        if (pScore < lowCertaintyThreshold)
        {
            zoneStatusText.color = Color.yellow; // Safe mode
        }
        else if (pScore > 70f)
        {
            zoneStatusText.color = Color.green; // High certainty
        }
        else
        {
            zoneStatusText.color = Color.cyan; // Medium certainty
        }
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.cyan;
        return mat;
    }

    /// <summary>
    /// Get current zone radius
    /// </summary>
    public float GetCurrentRadius()
    {
        return currentRadius;
    }

    /// <summary>
    /// Get target zone radius
    /// </summary>
    public float GetTargetRadius()
    {
        return targetRadius;
    }

    /// <summary>
    /// Set zone radius manually (for testing)
    /// </summary>
    public void SetZoneRadius(float radius)
    {
        targetRadius = Mathf.Clamp(radius, minRadius, maxRadius);
    }
}
