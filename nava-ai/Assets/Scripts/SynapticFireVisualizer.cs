using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Synaptic Fire Visualizer - Visualizes model switching and "brain" state changes.
/// Provides visual feedback when AI models switch (SOP - Synaptic Operating Procedures).
/// </summary>
public class SynapticFireVisualizer : MonoBehaviour
{
    [Header("Visual Effects")]
    [Tooltip("Particle system for synaptic sparks")]
    public ParticleSystem synapticSparks;
    
    [Tooltip("Light for neural pulse effect")]
    public Light neuralPulse;
    
    [Tooltip("Model indicator text")]
    public UnityEngine.UI.Text modelIndicatorText;
    
    [Header("Animation")]
    [Tooltip("Pulse animation duration")]
    public float pulseDuration = 1f;
    
    [Tooltip("Scale animation intensity")]
    public float scaleIntensity = 1.5f;
    
    [Header("Model Colors")]
    [Tooltip("Color mapping for different models")]
    public ModelColorMapping[] modelColors;

    [System.Serializable]
    public class ModelColorMapping
    {
        public string modelName;
        public Color color;
    }

    private Vector3 originalScale;
    private Color originalLightColor;
    private float originalLightIntensity;

    void Start()
    {
        // Store original values
        originalScale = transform.localScale;
        if (neuralPulse != null)
        {
            originalLightColor = neuralPulse.color;
            originalLightIntensity = neuralPulse.intensity;
        }
        
        // Initialize default color mappings
        if (modelColors == null || modelColors.Length == 0)
        {
            InitializeDefaultColors();
        }
        
        Debug.Log("[SynapticFireVisualizer] Initialized - Ready for model switching visualization");
    }

    void InitializeDefaultColors()
    {
        // Palantir/Tesla color scheme - Professional NASA theme
        modelColors = new ModelColorMapping[]
        {
            new ModelColorMapping { modelName = "SafeVLA", color = new Color(0.2f, 0.6f, 1f, 1f) }, // Palantir Blue
            new ModelColorMapping { modelName = "VLA", color = new Color(0f, 0.478f, 1f, 1f) }, // Apple Blue
            new ModelColorMapping { modelName = "VLM", color = new Color(0f, 0.8f, 0.4f, 1f) }, // Tesla Green
            new ModelColorMapping { modelName = "AGI", color = new Color(0f, 0.478f, 1f, 1f) }, // Apple Blue (replaced magenta)
            new ModelColorMapping { modelName = "RL", color = new Color(1f, 0.6f, 0f, 1f) }, // Tesla Orange
            new ModelColorMapping { modelName = "Quadrotor", color = new Color(1f, 0.2f, 0.2f, 1f) }, // Error Red
            new ModelColorMapping { modelName = "Humanoid", color = new Color(1f, 1f, 1f, 1f) } // Crispy White
        };
    }

    /// <summary>
    /// Called when model switches - provides visual feedback
    /// </summary>
    public void OnModelSwitch(string modelName)
    {
        Debug.Log($"[SynapticFireVisualizer] Model switched to: {modelName}");
        
        // 1. Play particle effect
        if (synapticSparks != null)
        {
            synapticSparks.Play();
        }
        
        // 2. Change light color based on model
        Color modelColor = GetModelColor(modelName);
        if (neuralPulse != null)
        {
            StartCoroutine(PulseLightColor(modelColor));
        }
        
        // 3. Update model indicator text
        if (modelIndicatorText != null)
        {
            modelIndicatorText.text = modelName;
            modelIndicatorText.color = modelColor;
        }
        
        // 4. Animate scale to simulate "Loading Weights"
        StartCoroutine(PulseScale());
    }

    Color GetModelColor(string modelName)
    {
        foreach (var mapping in modelColors)
        {
            if (mapping.modelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
            {
                return mapping.color;
            }
        }
        
        // Default color
        return Color.white;
    }

    IEnumerator PulseLightColor(Color targetColor)
    {
        if (neuralPulse == null) yield break;
        
        float elapsed = 0f;
        Color startColor = neuralPulse.color;
        float startIntensity = neuralPulse.intensity;
        float targetIntensity = originalLightIntensity * 2f;
        
        // Fade to target color
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            
            neuralPulse.color = Color.Lerp(startColor, targetColor, t);
            neuralPulse.intensity = Mathf.Lerp(startIntensity, targetIntensity, Mathf.Sin(t * Mathf.PI));
            
            yield return null;
        }
        
        // Fade back
        elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            
            neuralPulse.color = Color.Lerp(targetColor, originalLightColor, t);
            neuralPulse.intensity = Mathf.Lerp(targetIntensity, originalLightIntensity, t);
            
            yield return null;
        }
        
        neuralPulse.color = originalLightColor;
        neuralPulse.intensity = originalLightIntensity;
    }

    IEnumerator PulseScale()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * scaleIntensity;
        
        // Scale up
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (pulseDuration * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (pulseDuration * 0.5f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Set model color manually
    /// </summary>
    public void SetModelColor(string modelName, Color color)
    {
        bool found = false;
        for (int i = 0; i < modelColors.Length; i++)
        {
            if (modelColors[i].modelName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
            {
                modelColors[i].color = color;
                found = true;
                break;
            }
        }
        
        if (!found)
        {
            System.Array.Resize(ref modelColors, modelColors.Length + 1);
            modelColors[modelColors.Length - 1] = new ModelColorMapping { modelName = modelName, color = color };
        }
    }
}
