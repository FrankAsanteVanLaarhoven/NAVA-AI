using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Consciousness Overlay - Visualizes fatigue state and consciousness level.
/// Creates HUD elements that pulse when robot is "losing consciousness" (Fatigue/Sensor Failure).
/// </summary>
public class ConsciousnessOverlay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Canvas group for fatigue vignette effect")]
    public CanvasGroup fatigueOverlay;
    
    [Tooltip("Text displaying fatigue status")]
    public Text fatigueText;
    
    [Tooltip("Reticle/crosshair for focus visualization")]
    public RectTransform reticle;
    
    [Header("Visual Settings")]
    [Tooltip("Vignette color when fatigued")]
    public Color vignetteColor = new Color(0, 0, 0, 0.5f);
    
    [Tooltip("Enable pulsing effect when fatigued")]
    public bool enablePulsing = true;
    
    [Tooltip("Pulse speed")]
    public float pulseSpeed = 2f;
    
    [Header("Thresholds")]
    [Tooltip("Consciousness level for fatigue warning")]
    public float fatigueThreshold = 0.3f;
    
    [Tooltip("Consciousness level for distracted warning")]
    public float distractedThreshold = 0.6f;
    
    [Header("Component References")]
    [Tooltip("Reference to consciousness rigor for c value")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    private Image vignetteImage;
    private Coroutine pulseCoroutine;
    private float currentConsciousness = 1f;

    void Start()
    {
        // Get consciousness rigor if not assigned
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
            if (consciousnessRigor == null)
            {
                consciousnessRigor = FindObjectOfType<NavlConsciousnessRigor>();
            }
        }
        
        // Create vignette image if not assigned
        if (fatigueOverlay != null && fatigueOverlay.GetComponent<Image>() == null)
        {
            vignetteImage = fatigueOverlay.gameObject.AddComponent<Image>();
            vignetteImage.color = vignetteColor;
        }
        else if (fatigueOverlay != null)
        {
            vignetteImage = fatigueOverlay.GetComponent<Image>();
        }
        
        // Initialize overlay
        if (fatigueOverlay != null)
        {
            fatigueOverlay.alpha = 0; // Starts clear
        }
        
        Debug.Log("[ConsciousnessOverlay] Initialized - Fatigue visualization ready");
    }

    void Update()
    {
        // Get consciousness value
        if (consciousnessRigor != null)
        {
            currentConsciousness = consciousnessRigor.GetConsciousness();
        }
        
        // Update overlay
        UpdateConsciousness(currentConsciousness);
    }

    /// <summary>
    /// Update consciousness visualization
    /// </summary>
    public void UpdateConsciousness(float c_value)
    {
        // c_value is 0.0 to 1.0
        // 1.0 = Fully Awake. 0.0 = Sleep/Unconscious.
        
        currentConsciousness = Mathf.Clamp01(c_value);
        
        // 1. Visual Vignette (Tunnel Vision)
        if (fatigueOverlay != null)
        {
            float vignetteAlpha = 1.0f - currentConsciousness; // Higher C = Clearer View
            fatigueOverlay.alpha = vignetteAlpha;
            
            // Update color based on consciousness
            if (vignetteImage != null)
            {
                Color c = vignetteColor;
                if (currentConsciousness < fatigueThreshold)
                {
                    c = Color.red; // Red vignette when unconscious
                }
                else if (currentConsciousness < distractedThreshold)
                {
                    c = Color.yellow; // Yellow when distracted
                }
                c.a = vignetteAlpha;
                vignetteImage.color = c;
            }
        }
        
        // 2. Visual Reticle (Focus)
        if (reticle != null)
        {
            float focus = currentConsciousness;
            reticle.localScale = Vector3.one * focus; // Shrinks if tired
            
            // Pulse effect when fatigued
            if (enablePulsing && currentConsciousness < distractedThreshold)
            {
                if (pulseCoroutine == null)
                {
                    pulseCoroutine = StartCoroutine(PulseReticle());
                }
            }
            else
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
            }
        }
        
        // 3. UI Text
        if (fatigueText != null)
        {
            if (currentConsciousness < fatigueThreshold)
            {
                fatigueText.text = "WARNING: FATIGUE DETECTED";
                fatigueText.color = Color.red;
            }
            else if (currentConsciousness < distractedThreshold)
            {
                fatigueText.text = "STATUS: DISTRACTED";
                fatigueText.color = Color.yellow;
            }
            else
            {
                fatigueText.text = "SYSTEM: CONSCIOUS";
                fatigueText.color = Color.green;
            }
        }
    }

    IEnumerator PulseReticle()
    {
        while (currentConsciousness < distractedThreshold && reticle != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scale = currentConsciousness + pulse * 0.2f;
            reticle.localScale = Vector3.one * scale;
            yield return null;
        }
        pulseCoroutine = null;
    }

    /// <summary>
    /// Get current consciousness level
    /// </summary>
    public float GetConsciousness()
    {
        return currentConsciousness;
    }
}
