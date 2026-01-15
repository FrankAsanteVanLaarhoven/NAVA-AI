using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ironclad Visualizer - Visualizes the 7 Dimensions as sliders.
/// Allows real-time tweaking of the math to test Ironclad rigor.
/// </summary>
public class IroncladVisualizer : MonoBehaviour
{
    [Header("UI Sliders")]
    [Tooltip("Array of 5 sliders for x, t, g, i, c dimensions")]
    public Slider[] dimSliders = new Slider[5];
    
    [Tooltip("Labels for each dimension")]
    public Text[] dimLabels = new Text[5];
    
    [Header("Visual Feedback")]
    [Tooltip("Enable color coding based on values")]
    public bool enableColorCoding = true;
    
    [Tooltip("Color for safe values")]
    public Color safeColor = Color.green;
    
    [Tooltip("Color for warning values")]
    public Color warningColor = Color.yellow;
    
    [Tooltip("Color for violation values")]
    public Color violationColor = Color.red;
    
    private Navl7dRigor rigor;
    private bool slidersInitialized = false;

    void Start()
    {
        rigor = GetComponent<Navl7dRigor>();
        if (rigor == null)
        {
            rigor = GetComponentInParent<Navl7dRigor>();
        }
        
        InitializeLabels();
        
        Debug.Log("[IroncladVisualizer] Initialized - 7D dimension visualization ready");
    }

    void InitializeLabels()
    {
        if (dimLabels == null || dimLabels.Length < 5) return;
        
        string[] labels = { "x (Position)", "t (Time)", "g (Gradient)", "i (Identity)", "c (Constraint)" };
        for (int i = 0; i < 5 && i < dimLabels.Length; i++)
        {
            if (dimLabels[i] != null)
            {
                dimLabels[i].text = labels[i];
            }
        }
    }

    void Update()
    {
        if (rigor == null) return;
        
        // Bind Sliders to Internal Math Variables
        // This allows you to "Break" the robot by moving 'c' slider up
        UpdateSliders();
    }

    void UpdateSliders()
    {
        if (dimSliders == null || dimSliders.Length < 5) return;
        
        // Update slider values (read-only visualization)
        // Note: In a real system, you might want to make these interactive
        // to test the math by manually adjusting values
        
        if (dimSliders[0] != null)
        {
            dimSliders[0].value = Mathf.Clamp01(rigor._p_position / 50f); // Normalize
        }
        
        if (dimSliders[1] != null)
        {
            dimSliders[1].value = rigor._t_timePhase; // Already 0-1
        }
        
        if (dimSliders[2] != null)
        {
            dimSliders[2].value = Mathf.Clamp01(rigor._g_gradient / 5f); // Normalize
        }
        
        if (dimSliders[3] != null)
        {
            dimSliders[3].value = rigor._i_identity; // Already 0-1
        }
        
        if (dimSliders[4] != null)
        {
            dimSliders[4].value = rigor._c_constraint; // 0 or 1
        }
        
        // Visual Feedback - Colorize based on values
        if (enableColorCoding)
        {
            ColorizeSliders();
        }
    }

    void ColorizeSliders()
    {
        // Color constraint slider (c) - most critical
        if (dimSliders.Length > 4 && dimSliders[4] != null)
        {
            Color c = rigor._c_constraint > 0.5f ? violationColor : safeColor;
            ColorizeSlider(4, c);
        }
        
        // Color identity slider (i) - model confidence
        if (dimSliders.Length > 3 && dimSliders[3] != null)
        {
            Color c = Color.Lerp(violationColor, safeColor, rigor._i_identity);
            ColorizeSlider(3, c);
        }
        
        // Color gradient slider (g) - terrain
        if (dimSliders.Length > 2 && dimSliders[2] != null)
        {
            float normalized = Mathf.Clamp01(rigor._g_gradient / 5f);
            Color c = Color.Lerp(safeColor, warningColor, normalized);
            ColorizeSlider(2, c);
        }
    }

    void ColorizeSlider(int index, Color color)
    {
        if (index >= dimSliders.Length || dimSliders[index] == null) return;
        
        Slider slider = dimSliders[index];
        
        // Color the fill area
        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }
        
        // Color the handle
        if (slider.targetGraphic != null)
        {
            slider.targetGraphic.color = color;
        }
    }

    /// <summary>
    /// Enable interactive mode (allows manual slider adjustment for testing)
    /// </summary>
    public void EnableInteractiveMode(bool enable)
    {
        if (dimSliders == null) return;
        
        foreach (Slider slider in dimSliders)
        {
            if (slider != null)
            {
                slider.interactable = enable;
            }
        }
    }
}
