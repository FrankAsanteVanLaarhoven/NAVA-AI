using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Environment Profiler - Terrain & Sensor Adaptation.
/// Detects terrain friction (slipperiness), lighting quality, and obstacle density,
/// and auto-adjusts the Safety Alpha (α) in the Ironclad equation.
/// </summary>
public class EnvironmentProfiler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying terrain status")]
    public Text terrainStatusText;
    
    [Tooltip("Text displaying light status")]
    public Text lightStatusText;
    
    [Tooltip("Text displaying obstacle density")]
    public Text densityStatusText;
    
    [Header("Adaptive Physics")]
    [Tooltip("Slippery threshold (below this, reduce max speed)")]
    public float slipperyThreshold = 0.6f;
    
    [Tooltip("Dark threshold (below this, increase visual noise uncertainty)")]
    public float darkThreshold = 0.3f;
    
    [Tooltip("High density threshold (obstacles per m²)")]
    public float highDensityThreshold = 2.0f;
    
    [Header("Safety Alpha Range")]
    [Tooltip("Minimum safety alpha (loose control)")]
    public float minAlpha = 5.0f;
    
    [Tooltip("Maximum safety alpha (strict control)")]
    public float maxAlpha = 10.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to 7D rigor for alpha updates")]
    public Navl7dRigor navlRigor;
    
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Header("Raycast Settings")]
    [Tooltip("Terrain raycast distance")]
    public float terrainRayDistance = 2.0f;
    
    [Tooltip("Obstacle detection radius")]
    public float obstacleDetectionRadius = 5.0f;
    
    [Tooltip("Layer mask for terrain detection")]
    public LayerMask terrainLayerMask = -1;
    
    [Tooltip("Layer mask for obstacle detection")]
    public LayerMask obstacleLayerMask = -1;
    
    // Environmental Factors
    private float _friction = 1.0f; // 1.0 = Standard, 0.6 = Ice
    private float _lightQuality = 1.0f; // 1.0 = Bright, 0.3 = Dark
    private float _density = 0.0f; // Obstacle count per m²
    private float _currentAlpha = 5.0f;
    
    // Material name mappings for friction detection
    private Dictionary<string, float> materialFrictionMap = new Dictionary<string, float>
    {
        { "Ice", 0.3f },
        { "Ice (Instance)", 0.3f },
        { "Gravel", 0.5f },
        { "Gravel (Instance)", 0.5f },
        { "Wet", 0.6f },
        { "Wet (Instance)", 0.6f },
        { "Asphalt", 0.9f },
        { "Asphalt (Instance)", 0.9f },
        { "Concrete", 0.95f },
        { "Concrete (Instance)", 0.95f }
    };

    void Start()
    {
        // Get component references if not assigned
        if (navlRigor == null)
        {
            navlRigor = GetComponent<Navl7dRigor>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        _currentAlpha = minAlpha;
        
        Debug.Log("[EnvironmentProfiler] Initialized - Terrain and sensor adaptation ready");
    }

    void Update()
    {
        // 1. Raycast for Terrain Analysis
        AnalyzeTerrain();
        
        // 2. Sensor Quality Check (Light)
        AnalyzeLighting();
        
        // 3. Obstacle Density Analysis
        AnalyzeObstacleDensity();
        
        // 4. Calculate Adaptive Safety Alpha
        CalculateAdaptiveAlpha();
        
        // 5. Update UI
        UpdateUI();
    }

    void AnalyzeTerrain()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, terrainRayDistance, terrainLayerMask))
        {
            // Detect material type for friction estimation
            string materialName = "";
            
            if (hit.collider.sharedMaterial != null)
            {
                materialName = hit.collider.sharedMaterial.name;
            }
            else if (hit.collider.material != null)
            {
                materialName = hit.collider.material.name;
            }
            
            // Get friction from material map
            if (materialFrictionMap.ContainsKey(materialName))
            {
                _friction = materialFrictionMap[materialName];
            }
            else
            {
                // Default friction based on material name keywords
                string lowerName = materialName.ToLower();
                if (lowerName.Contains("ice") || lowerName.Contains("slippery"))
                {
                    _friction = 0.4f;
                }
                else if (lowerName.Contains("gravel") || lowerName.Contains("loose"))
                {
                    _friction = 0.5f;
                }
                else if (lowerName.Contains("wet") || lowerName.Contains("water"))
                {
                    _friction = 0.6f;
                }
                else if (lowerName.Contains("asphalt") || lowerName.Contains("road"))
                {
                    _friction = 0.9f;
                }
                else if (lowerName.Contains("concrete") || lowerName.Contains("floor"))
                {
                    _friction = 0.95f;
                }
                else
                {
                    _friction = 0.8f; // Default medium friction
                }
            }
            
            // Clamp friction
            _friction = Mathf.Clamp01(_friction);
        }
        else
        {
            // No terrain detected, assume standard friction
            _friction = 0.8f;
        }
    }

    void AnalyzeLighting()
    {
        // Check ambient light intensity
        float ambientIntensity = RenderSettings.ambientIntensity;
        
        // Check main light (if exists)
        Light mainLight = RenderSettings.sun;
        float mainLightIntensity = mainLight != null ? mainLight.intensity : 0f;
        
        // Combined light quality
        float combinedLight = (ambientIntensity + mainLightIntensity) / 2f;
        
        if (combinedLight < darkThreshold)
        {
            _lightQuality = 0.3f; // Low Confidence
        }
        else if (combinedLight < 0.6f)
        {
            _lightQuality = 0.6f; // Medium Confidence
        }
        else
        {
            _lightQuality = 1.0f; // Optimal
        }
        
        _lightQuality = Mathf.Clamp01(_lightQuality);
    }

    void AnalyzeObstacleDensity()
    {
        // Count obstacles in detection radius
        Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleDetectionRadius, obstacleLayerMask);
        
        // Filter out self
        List<Collider> validObstacles = new List<Collider>();
        foreach (var col in obstacles)
        {
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                validObstacles.Add(col);
            }
        }
        
        // Calculate density (obstacles per m²)
        float area = Mathf.PI * obstacleDetectionRadius * obstacleDetectionRadius;
        _density = validObstacles.Count / area;
    }

    void CalculateAdaptiveAlpha()
    {
        // Base alpha calculation
        // If friction is LOW (Ice), we need HIGHER Alpha (Strictness)
        // If friction is HIGH (Asphalt), we need LOWER Alpha (Speed)
        float frictionAlpha = Mathf.Lerp(maxAlpha, minAlpha, _friction);
        
        // Light quality adjustment
        // Low light = Higher alpha (more cautious)
        float lightAlpha = Mathf.Lerp(maxAlpha, minAlpha, _lightQuality);
        
        // Density adjustment
        // High density = Higher alpha (more cautious)
        float densityFactor = Mathf.Clamp01(_density / highDensityThreshold);
        float densityAlpha = Mathf.Lerp(minAlpha, maxAlpha, densityFactor);
        
        // Combine factors (weighted average)
        float combinedAlpha = (frictionAlpha * 0.5f) + (lightAlpha * 0.3f) + (densityAlpha * 0.2f);
        
        // Smooth transition
        _currentAlpha = Mathf.Lerp(_currentAlpha, combinedAlpha, Time.deltaTime * 2.0f);
        _currentAlpha = Mathf.Clamp(_currentAlpha, minAlpha, maxAlpha);
        
        // Update Navl7dRigor
        if (navlRigor != null)
        {
            navlRigor.UpdateSafetyAlpha(_currentAlpha);
        }
    }

    void UpdateUI()
    {
        // Update terrain status
        if (terrainStatusText != null)
        {
            string frictionLevel = _friction < slipperyThreshold ? "SLIPPERY" : "NORMAL";
            terrainStatusText.text = $"TERRAIN: Friction={_friction:F2} ({frictionLevel}) | Alpha={_currentAlpha:F1}";
            terrainStatusText.color = _friction < slipperyThreshold ? Color.yellow : Color.green;
        }
        
        // Update light status
        if (lightStatusText != null)
        {
            if (_lightQuality < darkThreshold)
            {
                lightStatusText.text = "SENSOR: POOR LIGHT";
                lightStatusText.color = Color.yellow;
            }
            else if (_lightQuality < 0.6f)
            {
                lightStatusText.text = "SENSOR: MODERATE LIGHT";
                lightStatusText.color = Color.cyan;
            }
            else
            {
                lightStatusText.text = "SENSOR: OPTIMAL";
                lightStatusText.color = Color.green;
            }
        }
        
        // Update density status
        if (densityStatusText != null)
        {
            string densityLevel = _density > highDensityThreshold ? "HIGH" : "NORMAL";
            densityStatusText.text = $"OBSTACLES: {_density:F2}/m² ({densityLevel})";
            densityStatusText.color = _density > highDensityThreshold ? Color.yellow : Color.green;
        }
    }

    /// <summary>
    /// Get current friction value
    /// </summary>
    public float GetFriction()
    {
        return _friction;
    }

    /// <summary>
    /// Get current light quality
    /// </summary>
    public float GetLightQuality()
    {
        return _lightQuality;
    }

    /// <summary>
    /// Get current obstacle density
    /// </summary>
    public float GetObstacleDensity()
    {
        return _density;
    }

    /// <summary>
    /// Get current safety alpha
    /// </summary>
    public float GetCurrentAlpha()
    {
        return _currentAlpha;
    }

    /// <summary>
    /// Get overall environment risk factor (0-1, higher = more risky)
    /// </summary>
    public float GetRiskFactor()
    {
        float frictionRisk = 1.0f - _friction; // Low friction = high risk
        float lightRisk = 1.0f - _lightQuality; // Low light = high risk
        float densityRisk = Mathf.Clamp01(_density / highDensityThreshold); // High density = high risk
        
        return (frictionRisk * 0.4f) + (lightRisk * 0.3f) + (densityRisk * 0.3f);
    }
}
