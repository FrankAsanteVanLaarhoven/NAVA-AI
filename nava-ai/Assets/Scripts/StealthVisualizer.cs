using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NASA Grade Stealth Visualizer - Radar Evasion and Thermal Camouflage.
/// Enables selective sensor visibility (e.g., invisible to Radar, visible to LiDAR).
/// </summary>
public class StealthVisualizer : MonoBehaviour
{
    [Header("Stealth Modes")]
    [Tooltip("Invisible to radar sensors")]
    public bool radarInvisible = false;
    
    [Tooltip("Visible to LiDAR sensors")]
    public bool lidarVisible = true;
    
    [Tooltip("Invisible to thermal sensors")]
    public bool thermalInvisible = false;
    
    [Tooltip("Invisible to visual cameras")]
    public bool visualInvisible = false;
    
    [Header("Camouflage Materials")]
    [Tooltip("Material for radar cloaking")]
    public Material radarCloakMat;
    
    [Tooltip("Material for thermal cloaking")]
    public Material thermalCloakMat;
    
    [Tooltip("Material for visual cloaking")]
    public Material visualCloakMat;
    
    [Header("Stealth Settings")]
    [Tooltip("Radar opacity when invisible")]
    [Range(0f, 1f)]
    public float radarOpacity = 0.1f;
    
    [Tooltip("Thermal opacity when invisible")]
    [Range(0f, 1f)]
    public float thermalOpacity = 0.1f;
    
    [Tooltip("Visual opacity when invisible")]
    [Range(0f, 1f)]
    public float visualOpacity = 0.1f;
    
    [Tooltip("Stealth transition speed")]
    public float transitionSpeed = 2.0f;
    
    [Header("Layer Masks")]
    [Tooltip("Layer for radar sensors")]
    public int radarLayer = 8;
    
    [Tooltip("Layer for LiDAR sensors")]
    public int lidarLayer = 9;
    
    [Tooltip("Layer for thermal sensors")]
    public int thermalLayer = 10;
    
    private Renderer[] renderers;
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private bool isInitialized = false;

    void Start()
    {
        // Get all renderers
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original materials and colors
        foreach (Renderer r in renderers)
        {
            if (r.material != null)
            {
                originalMaterials[r] = r.material;
                originalColors[r] = r.material.color;
            }
        }
        
        isInitialized = true;
        
        // Apply initial stealth state
        UpdateStealth();
        
        Debug.Log("[STEALTH] Stealth Visualizer initialized");
    }

    void Update()
    {
        if (!isInitialized) return;
        
        // Dynamic Cloaking (NASA Grade Stealth)
        UpdateStealth();
    }

    void UpdateStealth()
    {
        foreach (Renderer r in renderers)
        {
            if (r == null || !originalMaterials.ContainsKey(r)) continue;
            
            Material mat = r.material;
            Color originalColor = originalColors[r];
            
            // 1. Radar Stealth
            if (radarInvisible)
            {
                // Fade opacity for radar
                Color c = originalColor;
                float targetAlpha = Mathf.Lerp(c.a, radarOpacity, Time.deltaTime * transitionSpeed);
                mat.color = new Color(c.r, c.g, c.b, targetAlpha);
                
                // Apply radar cloak material if available
                if (radarCloakMat != null)
                {
                    mat = radarCloakMat;
                }
                
                // Set layer to radar-invisible layer
                r.gameObject.layer = radarLayer;
            }
            else
            {
                // Restore original
                mat.color = originalColor;
                r.gameObject.layer = 0; // Default layer
            }
            
            // 2. Thermal Stealth
            if (thermalInvisible)
            {
                // Apply thermal cloak material
                if (thermalCloakMat != null)
                {
                    mat = thermalCloakMat;
                }
                else
                {
                    // Fade opacity
                    Color c = mat.color;
                    float targetAlpha = Mathf.Lerp(c.a, thermalOpacity, Time.deltaTime * transitionSpeed);
                    mat.color = new Color(c.r, c.g, c.b, targetAlpha);
                }
                
                r.gameObject.layer = thermalLayer;
            }
            
            // 3. Visual Stealth
            if (visualInvisible)
            {
                // Apply visual cloak material
                if (visualCloakMat != null)
                {
                    mat = visualCloakMat;
                }
                else
                {
                    // Fade opacity
                    Color c = mat.color;
                    float targetAlpha = Mathf.Lerp(c.a, visualOpacity, Time.deltaTime * transitionSpeed);
                    mat.color = new Color(c.r, c.g, c.b, targetAlpha);
                }
            }
            
            r.material = mat;
        }
    }

    /// <summary>
    /// Toggle radar stealth
    /// </summary>
    public void ToggleRadarStealth()
    {
        radarInvisible = !radarInvisible;
        Debug.Log($"[STEALTH] Radar Visibility: {(!radarInvisible)}");
    }

    /// <summary>
    /// Toggle thermal stealth
    /// </summary>
    public void ToggleThermalStealth()
    {
        thermalInvisible = !thermalInvisible;
        Debug.Log($"[STEALTH] Thermal Visibility: {(!thermalInvisible)}");
    }

    /// <summary>
    /// Toggle visual stealth
    /// </summary>
    public void ToggleVisualStealth()
    {
        visualInvisible = !visualInvisible;
        Debug.Log($"[STEALTH] Visual Visibility: {(!visualInvisible)}");
    }

    /// <summary>
    /// Enable full stealth (all modes)
    /// </summary>
    public void EnableFullStealth()
    {
        radarInvisible = true;
        thermalInvisible = true;
        visualInvisible = true;
        Debug.Log("[STEALTH] Full stealth mode enabled");
    }

    /// <summary>
    /// Disable all stealth
    /// </summary>
    public void DisableStealth()
    {
        radarInvisible = false;
        thermalInvisible = false;
        visualInvisible = false;
        Debug.Log("[STEALTH] All stealth modes disabled");
    }

    /// <summary>
    /// Check if robot is visible to a specific sensor type
    /// </summary>
    public bool IsVisibleToSensor(string sensorType)
    {
        switch (sensorType.ToLower())
        {
            case "radar":
                return !radarInvisible;
            case "lidar":
                return lidarVisible;
            case "thermal":
                return !thermalInvisible;
            case "visual":
            case "camera":
                return !visualInvisible;
            default:
                return true;
        }
    }
}
