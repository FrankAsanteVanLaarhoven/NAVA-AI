using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dual Mode Manager - Switches between Academia and Production modes.
/// Ensures NO capabilities are lost when switching modes.
/// </summary>
public class DualModeManager : MonoBehaviour
{
    public enum SystemMode
    {
        Academia,
        Production,
        Hybrid
    }

    [Header("Mode Settings")]
    [Tooltip("Current system mode")]
    public SystemMode currentMode = SystemMode.Academia;
    
    [Tooltip("Allow hybrid mode (both enabled)")]
    public bool allowHybrid = true;
    
    [Header("UI References")]
    [Tooltip("Mode toggle dropdown")]
    public Dropdown modeDropdown;
    
    [Tooltip("Text displaying current mode")]
    public Text modeStatusText;
    
    [Header("Component References")]
    [Tooltip("Academia components")]
    public MonoBehaviour[] academiaComponents;
    
    [Tooltip("Production components")]
    public MonoBehaviour[] productionComponents;
    
    private FleetDiscoveryManager fleetDiscovery;
    private SecurityPortal securityPortal;
    private RealTimeAnalyticsHub analyticsHub;
    private VideoStreamer videoStreamer;
    private AcademicSessionRecorder sessionRecorder;
    private LectureAnnotationTool annotationTool;
    private ExperimentWorkflowController workflowController;

    void Start()
    {
        // Auto-detect components
        AutoDetectComponents();
        
        // Setup UI
        SetupUI();
        
        // Apply initial mode
        SetMode(currentMode);
        
        Debug.Log($"[DualMode] Manager initialized - Mode: {currentMode}");
    }

    void AutoDetectComponents()
    {
        // Production components
        fleetDiscovery = FindObjectOfType<FleetDiscoveryManager>();
        securityPortal = FindObjectOfType<SecurityPortal>();
        analyticsHub = FindObjectOfType<RealTimeAnalyticsHub>();
        videoStreamer = FindObjectOfType<VideoStreamer>();
        
        // Academia components
        sessionRecorder = FindObjectOfType<AcademicSessionRecorder>();
        annotationTool = FindObjectOfType<LectureAnnotationTool>();
        workflowController = FindObjectOfType<ExperimentWorkflowController>();
    }

    void SetupUI()
    {
        // Setup dropdown
        if (modeDropdown != null)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Academia",
                "Production",
                "Hybrid"
            });
            
            modeDropdown.value = (int)currentMode;
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
        }
    }

    void OnModeChanged(int value)
    {
        SetMode((SystemMode)value);
    }

    /// <summary>
    /// Set system mode
    /// </summary>
    public void SetMode(SystemMode mode)
    {
        currentMode = mode;
        
        switch (mode)
        {
            case SystemMode.Academia:
                EnableAcademiaMode();
                DisableProductionMode();
                break;
                
            case SystemMode.Production:
                EnableProductionMode();
                DisableAcademiaMode();
                break;
                
            case SystemMode.Hybrid:
                if (allowHybrid)
                {
                    EnableAcademiaMode();
                    EnableProductionMode();
                }
                else
                {
                    Debug.LogWarning("[DualMode] Hybrid mode not allowed");
                    SetMode(SystemMode.Academia);
                }
                break;
        }
        
        UpdateUI();
        Debug.Log($"[DualMode] Switched to {mode} mode");
    }

    void EnableAcademiaMode()
    {
        // Enable academia components
        if (sessionRecorder != null)
        {
            sessionRecorder.enabled = true;
        }
        
        if (annotationTool != null)
        {
            annotationTool.enabled = true;
        }
        
        if (workflowController != null)
        {
            workflowController.enabled = true;
        }
        
        // Enable custom academia components
        foreach (var component in academiaComponents)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }
        
        Debug.Log("[DualMode] Academia mode enabled");
    }

    void DisableAcademiaMode()
    {
        // Disable academia components (but don't destroy)
        if (sessionRecorder != null)
        {
            sessionRecorder.enabled = false;
        }
        
        if (annotationTool != null)
        {
            annotationTool.enabled = false;
        }
        
        if (workflowController != null)
        {
            workflowController.enabled = false;
        }
        
        foreach (var component in academiaComponents)
        {
            if (component != null)
            {
                component.enabled = false;
            }
        }
    }

    void EnableProductionMode()
    {
        // Enable production components
        if (fleetDiscovery != null)
        {
            fleetDiscovery.enabled = true;
        }
        
        if (securityPortal != null)
        {
            securityPortal.enabled = true;
        }
        
        if (analyticsHub != null)
        {
            analyticsHub.enabled = true;
        }
        
        if (videoStreamer != null)
        {
            videoStreamer.enabled = true;
        }
        
        // Enable custom production components
        foreach (var component in productionComponents)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }
        
        Debug.Log("[DualMode] Production mode enabled");
    }

    void DisableProductionMode()
    {
        // Disable production components (but don't destroy)
        if (fleetDiscovery != null)
        {
            fleetDiscovery.enabled = false;
        }
        
        if (securityPortal != null)
        {
            securityPortal.enabled = false;
        }
        
        if (analyticsHub != null)
        {
            analyticsHub.enabled = false;
        }
        
        if (videoStreamer != null)
        {
            videoStreamer.enabled = false;
        }
        
        foreach (var component in productionComponents)
        {
            if (component != null)
            {
                component.enabled = false;
            }
        }
    }

    void UpdateUI()
    {
        if (modeStatusText != null)
        {
            modeStatusText.text = $"MODE: {currentMode.ToString().ToUpper()}";
            
            switch (currentMode)
            {
                case SystemMode.Academia:
                    modeStatusText.color = Color.cyan;
                    break;
                case SystemMode.Production:
                    modeStatusText.color = Color.green;
                    break;
                case SystemMode.Hybrid:
                    modeStatusText.color = Color.yellow;
                    break;
            }
        }
    }

    /// <summary>
    /// Get current mode
    /// </summary>
    public SystemMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Toggle between Academia and Production
    /// </summary>
    public void ToggleMode()
    {
        if (currentMode == SystemMode.Academia)
        {
            SetMode(SystemMode.Production);
        }
        else if (currentMode == SystemMode.Production)
        {
            SetMode(SystemMode.Academia);
        }
        else
        {
            SetMode(SystemMode.Academia);
        }
    }
}
