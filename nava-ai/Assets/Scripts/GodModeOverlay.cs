using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// God Mode Overlay - Visualizes the "7 Dimensions" of robot state.
/// Tesla/SpaceX style telemetry showing full phase space.
/// </summary>
public class GodModeOverlay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("UI Panel for God Mode overlay")]
    public RectTransform uiPanel;
    
    [Tooltip("Array of 7 sliders for dimensions")]
    public Slider[] dimSliders = new Slider[7];
    
    [Tooltip("Labels for each dimension")]
    public Text[] dimLabels = new Text[7];
    
    [Tooltip("Text showing 7D state summary")]
    public Text stateSummaryText;
    
    [Header("Visualization")]
    [Tooltip("The 'God View' overhead camera")]
    public Camera overheadCamera;
    
    [Tooltip("Enable God Mode by default")]
    public bool godModeEnabled = false;
    
    [Tooltip("Toggle key for God Mode")]
    public KeyCode toggleKey = KeyCode.G;
    
    [Header("Phase Space Visualization")]
    [Tooltip("LineRenderer for future cone")]
    public LineRenderer futureCone;
    
    [Tooltip("LineRenderer for safe cone")]
    public LineRenderer safeCone;
    
    [Tooltip("Cone visualization length")]
    public float coneLength = 5f;
    
    private Vnc7dVerifier verifier;
    private Rigidbody rb;
    private AdvancedEstimator estimator;
    private bool isGodModeActive = false;

    void Start()
    {
        // Get references
        verifier = GetComponent<Vnc7dVerifier>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
        estimator = GetComponent<AdvancedEstimator>();
        
        // Create cone visualizations
        CreateConeVisualizations();
        
        // Initialize UI
        InitializeUI();
        
        // Set initial state
        isGodModeActive = godModeEnabled;
        if (uiPanel != null)
        {
            uiPanel.gameObject.SetActive(isGodModeActive);
        }
        
        Debug.Log("[GodModeOverlay] Initialized - Press G to toggle God Mode");
    }

    void CreateConeVisualizations()
    {
        // Future Cone (Physics-allowed trajectory)
        if (futureCone == null)
        {
            GameObject futureObj = new GameObject("FutureCone");
            futureObj.transform.SetParent(transform);
            futureCone = futureObj.AddComponent<LineRenderer>();
            futureCone.startWidth = 0.2f;
            futureCone.endWidth = 0.05f;
            futureCone.material = new Material(Shader.Find("Sprites/Default"));
            futureCone.color = Color.blue;
            futureCone.useWorldSpace = true;
        }
        
        // Safe Cone (7D-certified trajectory)
        if (safeCone == null)
        {
            GameObject safeObj = new GameObject("SafeCone");
            safeObj.transform.SetParent(transform);
            safeCone = safeObj.AddComponent<LineRenderer>();
            safeCone.startWidth = 0.2f;
            safeCone.endWidth = 0.05f;
            safeCone.material = new Material(Shader.Find("Sprites/Default"));
            safeCone.color = UIThemeHelper.Colors.AppleBlue; // Replaced magenta
            safeCone.useWorldSpace = true;
        }
    }

    void InitializeUI()
    {
        // Initialize dimension labels if not assigned
        if (dimLabels != null && dimLabels.Length >= 7)
        {
            string[] labels = { "X", "Y", "Z", "Vx", "Vy", "Vz", "σ" };
            for (int i = 0; i < 7 && i < dimLabels.Length; i++)
            {
                if (dimLabels[i] != null)
                {
                    dimLabels[i].text = labels[i];
                }
            }
        }
    }

    void Update()
    {
        // Toggle God Mode
        if (Input.GetKeyDown(toggleKey))
        {
            isGodModeActive = !isGodModeActive;
            if (uiPanel != null)
            {
                uiPanel.gameObject.SetActive(isGodModeActive);
            }
        }
        
        if (!isGodModeActive) return;
        
        // 1. Capture 7D State
        Vector3 pos = transform.position;
        Vector3 vel = rb != null ? rb.velocity : Vector3.zero;
        float heading = transform.eulerAngles.y;
        float cert = estimator != null ? estimator.GetUncertainty() : 1f;
        cert = 1f / (1f + cert); // Convert to certainty
        
        // 2. Update UI Sliders (Visual Debugging)
        UpdateSliders(pos, vel, heading, cert);
        
        // 3. Visual Vectors (Mathematical Projections)
        DrawPhaseVectors(vel);
        
        // 4. Update Summary
        UpdateSummary(pos, vel, heading, cert);
    }

    void UpdateSliders(Vector3 pos, Vector3 vel, float heading, float cert)
    {
        if (dimSliders == null) return;
        
        // Normalize values for sliders (assuming reasonable ranges)
        if (dimSliders.Length > 0 && dimSliders[0] != null) dimSliders[0].value = NormalizeForSlider(pos.x, -10f, 10f);
        if (dimSliders.Length > 1 && dimSliders[1] != null) dimSliders[1].value = NormalizeForSlider(pos.y, 0f, 5f);
        if (dimSliders.Length > 2 && dimSliders[2] != null) dimSliders[2].value = NormalizeForSlider(pos.z, -10f, 10f);
        if (dimSliders.Length > 3 && dimSliders[3] != null) dimSliders[3].value = NormalizeForSlider(vel.x, -5f, 5f);
        if (dimSliders.Length > 4 && dimSliders[4] != null) dimSliders[4].value = NormalizeForSlider(vel.z, -5f, 5f);
        if (dimSliders.Length > 5 && dimSliders[5] != null) dimSliders[5].value = NormalizeForSlider(heading, 0f, 360f);
        if (dimSliders.Length > 6 && dimSliders[6] != null) dimSliders[6].value = cert;
    }

    float NormalizeForSlider(float value, float min, float max)
    {
        return Mathf.Clamp01((value - min) / (max - min));
    }

    void DrawPhaseVectors(Vector3 velocity)
    {
        Vector3 robotPos = transform.position;
        
        // Draw the "Future Cone" (Where physics allows robot to be)
        if (futureCone != null)
        {
            futureCone.positionCount = 2;
            futureCone.SetPosition(0, robotPos);
            futureCone.SetPosition(1, robotPos + velocity.normalized * coneLength);
            futureCone.color = Color.blue;
        }
        
        // Draw the "Safe Cone" (Where 7D Certifier allows robot to be)
        // This visualizes the intersection of Math and Physics
        if (safeCone != null && verifier != null)
        {
            Vector3 forward = transform.forward;
            float safeLength = verifier.IsCertifiedSafe() ? coneLength : coneLength * 0.3f;
            
            safeCone.positionCount = 2;
            safeCone.SetPosition(0, robotPos);
            safeCone.SetPosition(1, robotPos + forward * safeLength);
            safeCone.color = verifier.IsCertifiedSafe() ? UIThemeHelper.Colors.AppleBlue : UIThemeHelper.Colors.Error; // Replaced magenta
        }
        
        // Debug rays
        Debug.DrawRay(robotPos, velocity * 2.0f, Color.blue);
        Debug.DrawRay(robotPos, transform.forward * 5.0f, UIThemeHelper.Colors.AppleBlue); // Replaced magenta
    }

    void UpdateSummary(Vector3 pos, Vector3 vel, float heading, float cert)
    {
        if (stateSummaryText == null) return;
        
        string summary = $"7D STATE VECTOR\n" +
                        $"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})\n" +
                        $"Velocity: ({vel.x:F2}, {vel.y:F2}, {vel.z:F2})\n" +
                        $"Heading: {heading:F1}°\n" +
                        $"Certainty (σ): {cert:F3}\n";
        
        if (verifier != null)
        {
            summary += $"CBF Status: {(verifier.IsCertifiedSafe() ? "CERTIFIED" : "VIOLATION")}\n";
            summary += $"Barrier: {verifier.GetBarrierValue():F3}";
        }
        
        stateSummaryText.text = summary;
    }

    /// <summary>
    /// Enable God Mode programmatically
    /// </summary>
    public void EnableGodMode()
    {
        isGodModeActive = true;
        if (uiPanel != null)
        {
            uiPanel.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Disable God Mode
    /// </summary>
    public void DisableGodMode()
    {
        isGodModeActive = false;
        if (uiPanel != null)
        {
            uiPanel.gameObject.SetActive(false);
        }
    }
}
