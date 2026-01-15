using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Experiment Workflow Controller - Academia Capability.
/// The Conductor. Connects Crowd Simulation to the Experiment Manager.
/// Starts/restarts batches automatically and syncs with Session Recorder.
/// </summary>
public class ExperimentWorkflowController : MonoBehaviour
{
    [Header("Workflow Settings")]
    [Tooltip("Auto-start experiment on play")]
    public bool autoStart = false;
    
    [Tooltip("Auto-restart on completion")]
    public bool autoRestart = false;
    
    [Tooltip("Restart delay (seconds)")]
    public float restartDelay = 5.0f;
    
    [Header("UI References")]
    [Tooltip("Text displaying experiment status")]
    public Text experimentStatusText;
    
    [Tooltip("Button to toggle experiment")]
    public Button toggleButton;
    
    [Header("Component References")]
    [Tooltip("Reference to session recorder")]
    public AcademicSessionRecorder sessionRecorder;
    
    [Tooltip("Reference to crowd simulation (if exists)")]
    public MonoBehaviour crowdSimulation;
    
    private bool isRunning = false;
    private bool isPaused = false;

    void Start()
    {
        // Get component references
        if (sessionRecorder == null)
        {
            sessionRecorder = FindObjectOfType<AcademicSessionRecorder>();
        }
        
        // Setup toggle button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleExperiment);
        }
        
        // Auto-start if enabled
        if (autoStart)
        {
            StartExperiment();
        }
        
        Debug.Log("[ExperimentWorkflow] Workflow controller initialized");
    }

    void Update()
    {
        // Keyboard shortcut: 'E' to toggle
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleExperiment();
        }
        
        // Update UI
        UpdateUI();
    }

    /// <summary>
    /// Toggle experiment
    /// </summary>
    public void ToggleExperiment()
    {
        if (isRunning)
        {
            StopExperiment();
        }
        else
        {
            StartExperiment();
        }
    }

    /// <summary>
    /// Start experiment
    /// </summary>
    public void StartExperiment()
    {
        if (isRunning)
        {
            Debug.LogWarning("[ExperimentWorkflow] Experiment already running");
            return;
        }
        
        isRunning = true;
        isPaused = false;
        
        Debug.Log("[ExperimentWorkflow] Starting Experiment Batch...");
        
        // Start session recording
        if (sessionRecorder != null)
        {
            sessionRecorder.StartSession();
        }
        
        // Start crowd simulation (if exists)
        if (crowdSimulation != null)
        {
            // Try to set active density or start simulation
            var crowdType = crowdSimulation.GetType();
            var densityProp = crowdType.GetProperty("activeDensity");
            if (densityProp != null)
            {
                densityProp.SetValue(crowdSimulation, 20);
            }
            
            var startMethod = crowdType.GetMethod("StartSimulation");
            if (startMethod != null)
            {
                startMethod.Invoke(crowdSimulation, null);
            }
        }
        
        // Start research experiment manager (if exists)
        MonoBehaviour experimentManager = FindObjectOfType<MonoBehaviour>();
        if (experimentManager != null)
        {
            var runMethod = experimentManager.GetType().GetMethod("RunProtocol");
            if (runMethod != null)
            {
                runMethod.Invoke(experimentManager, null);
            }
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Stop experiment
    /// </summary>
    public void StopExperiment()
    {
        if (!isRunning)
        {
            Debug.LogWarning("[ExperimentWorkflow] No experiment running");
            return;
        }
        
        isRunning = false;
        
        Debug.Log("[ExperimentWorkflow] Stopping Experiment...");
        
        // Stop research experiment manager
        MonoBehaviour experimentManager = FindObjectOfType<MonoBehaviour>();
        if (experimentManager != null)
        {
            var stopMethod = experimentManager.GetType().GetMethod("StopExperiment");
            if (stopMethod != null)
            {
                stopMethod.Invoke(experimentManager, null);
            }
        }
        
        // End session recording
        if (sessionRecorder != null)
        {
            sessionRecorder.EndSession();
        }
        
        // Auto-restart if enabled
        if (autoRestart)
        {
            Invoke(nameof(StartExperiment), restartDelay);
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Pause experiment
    /// </summary>
    public void PauseExperiment()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Time.timeScale = 0f;
            Debug.Log("[ExperimentWorkflow] Experiment paused");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("[ExperimentWorkflow] Experiment resumed");
        }
        
        UpdateUI();
    }

    void UpdateUI()
    {
        if (experimentStatusText != null)
        {
            if (isRunning)
            {
                experimentStatusText.text = isPaused ? "EXPERIMENT: PAUSED" : "EXPERIMENT: RUNNING";
                experimentStatusText.color = isPaused ? Color.yellow : Color.green;
            }
            else
            {
                experimentStatusText.text = "EXPERIMENT: STOPPED";
                experimentStatusText.color = Color.gray;
            }
        }
        
        if (toggleButton != null)
        {
            var buttonText = toggleButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isRunning ? "Stop Experiment" : "Start Experiment";
            }
        }
    }

    /// <summary>
    /// Check if experiment is running
    /// </summary>
    public bool IsRunning()
    {
        return isRunning;
    }

    /// <summary>
    /// Check if experiment is paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}
