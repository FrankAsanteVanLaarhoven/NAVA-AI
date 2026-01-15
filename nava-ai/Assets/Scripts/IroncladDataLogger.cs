using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;

/// <summary>
/// Ironclad Data Logger - Records "Ironclad Data" (CBF violations vs. Successes) for Neural Model training.
/// Turns "Safety Monitoring" into "Adaptive Training" by collecting high-frequency training data.
/// </summary>
public class IroncladDataLogger : MonoBehaviour
{
    [Header("Data Collection Settings")]
    [Tooltip("Dataset file path (CSV format)")]
    public string datasetPath = "Assets/Data/ironclad_dataset.csv";
    
    [Tooltip("Enable data logging")]
    public bool enableLogging = true;
    
    [Tooltip("Logging rate (Hz) - 0 = every frame")]
    public float loggingRate = 10f;
    
    [Tooltip("Maximum dataset size (0 = unlimited)")]
    public int maxDatasetSize = 0;
    
    [Header("Component References")]
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to VNC verifier for barrier values")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to teleop controller for actions")]
    public UnityTeleopController teleopController;
    
    [Header("Visual Feedback")]
    [Tooltip("Enable visual feedback for logging")]
    public bool enableVisualFeedback = true;
    
    [Tooltip("Particle system for success logging")]
    public ParticleSystem successParticles;
    
    [Tooltip("Particle system for failure logging")]
    public ParticleSystem failureParticles;
    
    private StringBuilder csvBuilder = new StringBuilder();
    private bool initialized = false;
    private float lastLogTime = 0f;
    private float logInterval;
    private int logCount = 0;
    private Queue<string> logBuffer = new Queue<string>();
    private int bufferSize = 100;

    void Start()
    {
        logInterval = loggingRate > 0 ? 1f / loggingRate : 0f;
        
        // Get component references if not assigned
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        if (teleopController == null)
        {
            teleopController = GetComponent<UnityTeleopController>();
        }
        
        // Initialize dataset file
        InitializeDataset();
        
        Debug.Log("[IroncladDataLogger] Initialized - Training data collection ready");
    }

    void InitializeDataset()
    {
        if (!enableLogging) return;
        
        try
        {
            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(datasetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Setup CSV Header (Machine Readable)
            if (!File.Exists(datasetPath))
            {
                csvBuilder.AppendLine("timestamp,score_p,score_x,score_y,score_z,score_t,score_g,score_i,score_c,barrier_h,barrier_deriv,success_action,action_x,action_y,action_z");
                File.WriteAllText(datasetPath, csvBuilder.ToString());
                csvBuilder.Clear();
            }
            
            initialized = true;
            Debug.Log($"[IroncladDataLogger] Dataset initialized: {datasetPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[IroncladDataLogger] Failed to initialize dataset: {e.Message}");
            initialized = false;
        }
    }

    void Update()
    {
        if (!enableLogging || !initialized) return;
        
        // Throttle logging if rate specified
        if (loggingRate > 0 && Time.time - lastLogTime < logInterval) return;
        
        // Collect data
        CollectDataCycle();
        
        lastLogTime = Time.time;
    }

    void CollectDataCycle()
    {
        // Get current P-score and components
        float pScore = 0f;
        float scoreX = 0f, scoreY = 0f, scoreZ = 0f;
        float scoreT = 0f, scoreG = 0f, scoreI = 0f, scoreC = 0f;
        float barrierH = 0f;
        float barrierDeriv = 0f;
        bool success = true;
        string actionTaken = "none";
        Vector3 action = Vector3.zero;
        
        // Get P-score components
        if (consciousnessRigor != null)
        {
            pScore = consciousnessRigor.GetPScore();
            scoreG = consciousnessRigor.GetGoalProximity();
            scoreI = consciousnessRigor.GetModelIntent();
            scoreC = consciousnessRigor.GetConsciousness();
            
            // Get position for x, y, z
            Vector3 pos = transform.position;
            scoreX = pos.x;
            scoreY = pos.y;
            scoreZ = pos.z;
            
            // Time phase (simplified)
            scoreT = (Mathf.Sin(Time.time * 0.5f) + 1f) * 0.5f;
            
            // Check if successful (P-score above threshold)
            success = pScore >= consciousnessRigor.safetyThreshold;
        }
        
        // Get barrier values
        if (vncVerifier != null)
        {
            barrierH = vncVerifier.GetBarrierValue();
            barrierDeriv = vncVerifier.GetBarrierDerivative();
        }
        
        // Get action
        if (teleopController != null)
        {
            action = teleopController.GetDesiredVelocity();
            if (action != Vector3.zero)
            {
                actionTaken = "teleop";
            }
        }
        
        // Log the cycle
        LogCycle(pScore, scoreX, scoreY, scoreZ, scoreT, scoreG, scoreI, scoreC, 
                 barrierH, barrierDeriv, success, actionTaken, action);
    }

    /// <summary>
    /// Log a complete cycle with all data
    /// </summary>
    public void LogCycle(float pScore, float scoreX, float scoreY, float scoreZ, 
                        float scoreT, float scoreG, float scoreI, float scoreC,
                        float barrierH, float barrierDeriv, bool success, 
                        string actionTaken, Vector3 action)
    {
        if (!initialized) return;
        
        try
        {
            // Build CSV line
            string line = $"{DateTime.Now:O}," +
                         $"{pScore:F4}," +
                         $"{scoreX:F4},{scoreY:F4},{scoreZ:F4}," +
                         $"{scoreT:F4},{scoreG:F4},{scoreI:F4},{scoreC:F4}," +
                         $"{barrierH:F4},{barrierDeriv:F4}," +
                         $"{(success ? 1 : 0)}," +
                         $"{actionTaken}," +
                         $"{action.x:F4},{action.y:F4},{action.z:F4}";
            
            // Add to buffer
            logBuffer.Enqueue(line);
            
            // Flush buffer periodically
            if (logBuffer.Count >= bufferSize)
            {
                FlushBuffer();
            }
            
            // Check dataset size limit
            if (maxDatasetSize > 0 && logCount >= maxDatasetSize)
            {
                enableLogging = false;
                Debug.LogWarning("[IroncladDataLogger] Dataset size limit reached");
                return;
            }
            
            logCount++;
            
            // Visual Feedback
            if (enableVisualFeedback)
            {
                if (success)
                {
                    if (successParticles != null) successParticles.Play();
                    Debug.Log($"[IRONCLAD TRAINING] Success Logged: P={pScore:F2}, Count={logCount}");
                }
                else
                {
                    if (failureParticles != null) failureParticles.Play();
                    Debug.LogWarning($"[IRONCLAD TRAINING] Failure Logged: P={pScore:F2}, Count={logCount}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IroncladDataLogger] Failed to log cycle: {e.Message}");
        }
    }

    void FlushBuffer()
    {
        if (logBuffer.Count == 0) return;
        
        try
        {
            StringBuilder batch = new StringBuilder();
            while (logBuffer.Count > 0)
            {
                batch.AppendLine(logBuffer.Dequeue());
            }
            
            File.AppendAllText(datasetPath, batch.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[IroncladDataLogger] Failed to flush buffer: {e.Message}");
        }
    }

    void OnDestroy()
    {
        // Flush remaining buffer
        FlushBuffer();
    }

    void OnApplicationQuit()
    {
        // Flush remaining buffer
        FlushBuffer();
    }

    /// <summary>
    /// Get dataset statistics
    /// </summary>
    public DatasetStats GetDatasetStats()
    {
        DatasetStats stats = new DatasetStats();
        
        try
        {
            if (File.Exists(datasetPath))
            {
                string[] lines = File.ReadAllLines(datasetPath);
                stats.totalEntries = lines.Length - 1; // Subtract header
                
                int successCount = 0;
                foreach (string line in lines)
                {
                    if (line.StartsWith("timestamp")) continue; // Skip header
                    
                    string[] parts = line.Split(',');
                    if (parts.Length > 11)
                    {
                        if (parts[11] == "1") successCount++;
                    }
                }
                
                stats.successCount = successCount;
                stats.failureCount = stats.totalEntries - successCount;
                stats.successRate = stats.totalEntries > 0 ? (float)successCount / stats.totalEntries : 0f;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[IroncladDataLogger] Failed to get stats: {e.Message}");
        }
        
        return stats;
    }

    [System.Serializable]
    public class DatasetStats
    {
        public int totalEntries;
        public int successCount;
        public int failureCount;
        public float successRate;
    }
}
