using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Collections;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

/// <summary>
/// Certification Compiler - Acts as the "Notary" of the system.
/// Takes raw output of VLA/CMDP model, passes it through VNC 7D Rigor,
/// validates it against SIM2VAL++ logic, and issues a Certificate.
/// Synthesizes Rigor (P) and Uncertainty (p) into a single Verified Log Entry.
/// </summary>
public class CertificationCompiler : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Reference to 7D Rigor for P-score calculation")]
    public Navl7dRigor rigorRigor;
    
    [Tooltip("Reference to Sim2Val validator for uncertainty")]
    public LiveValidator sim2valValidator;
    
    [Tooltip("Reference to VNC verifier for barrier values")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Header("Compiler Settings")]
    [Tooltip("Sliding window size for variance calculation")]
    public int slidingWindowSize = 100;
    
    [Tooltip("Confidence threshold for 'High Certainty'")]
    public float confidenceThreshold = 0.8f;
    
    [Tooltip("Compilation rate (Hz) - 0 = every frame")]
    public float compilationRate = 10f;
    
    [Header("Logging")]
    [Tooltip("Enable certification logging")]
    public bool enableLogging = true;
    
    [Tooltip("Log file path (JSON format)")]
    public string logFilePath = "Assets/Data/cert_chain.json";
    
    [Tooltip("CSV export path (alternative format)")]
    public string csvFilePath = "Assets/Data/cert_chain.csv";
    
    [Tooltip("Enable CSV export")]
    public bool enableCsvExport = true;
    
    [Header("UI References")]
    [Tooltip("Text displaying compile status")]
    public Text compileStatusText;
    
    [Tooltip("Text displaying certificate count")]
    public Text certificateCountText;
    
    // Internal State for SIM2VAL
    private Queue<float> controlVariates = new Queue<float>();
    private float historicalMeanX = 50.0f; // Mean of P-Score
    private float lastCompilationTime = 0f;
    private float compilationInterval;
    private int certificateCount = 0;
    private bool isInitialized = false;
    private StringBuilder csvBuilder = new StringBuilder();
    
    // Rust Core State
    private bool isRustCoreLoaded = false;
    private bool isRustCoreInitialized = false;
    private float lastRobustnessCheck = 0f;

    void Start()
    {
        compilationInterval = compilationRate > 0 ? 1f / compilationRate : 0f;
        
        // Get component references if not assigned
        if (rigorRigor == null)
        {
            rigorRigor = GetComponent<Navl7dRigor>();
        }
        
        if (sim2valValidator == null)
        {
            sim2valValidator = GetComponent<LiveValidator>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (realRobot == null)
        {
            realRobot = gameObject;
        }
        
        // Initialize Rust Core
        if (useRustCore)
        {
            StartCoroutine(InitializeRustCore());
        }
        
        // Initialize log files
        if (enableLogging)
        {
            InitializeLogFiles();
        }
        
        // Initialize UI
        if (compileStatusText != null)
        {
            compileStatusText.text = "COMPILER: READY";
            compileStatusText.color = Color.green;
        }
        
        if (certificateCountText != null)
        {
            certificateCountText.text = "CERTIFICATES: 0";
        }
        
        isInitialized = true;
        
        Debug.Log("[CertificationCompiler] Initialized - Certification compiler ready");
    }

    IEnumerator InitializeRustCore()
    {
        yield return new WaitForSeconds(0.5f); // Wait for system init
        
        // Check if Rust core is available
        isRustCoreLoaded = RustCoreBridge.IsRustCoreAvailable();
        
        if (isRustCoreLoaded)
        {
            // Initialize Rust core
            isRustCoreInitialized = RustCoreBridge.InitializeRustCore();
            
            if (isRustCoreInitialized)
            {
                if (compileStatusText != null)
                {
                    compileStatusText.text = "COMPILER: RUST CORE LOCKED & READY";
                    compileStatusText.color = UIThemeHelper.Colors.PalantirBlue; // Replaced cyan
                }
                Debug.Log("[RustCore] Core Integrity: VERIFIED");
            }
            else
            {
                if (compileStatusText != null)
                {
                    compileStatusText.text = "COMPILER: RUST INIT FAILED";
                    compileStatusText.color = Color.yellow;
                }
                Debug.LogWarning("[RustCore] Initialization failed, using fallback");
            }
        }
        else
        {
            if (compileStatusText != null)
            {
                compileStatusText.text = "COMPILER: RUST CORE MISSING (Using C# Fallback)";
                compileStatusText.color = Color.yellow;
            }
            Debug.LogWarning("[RustCore] Library not found. Using C# fallback.");
        }
    }

    void InitializeLogFiles()
    {
        try
        {
            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Initialize JSON log file
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "[\n"); // Start JSON array
            }
            
            // Initialize CSV log file
            if (enableCsvExport)
            {
                directory = Path.GetDirectoryName(csvFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                if (!File.Exists(csvFilePath))
                {
                    // Write CSV header
                    csvBuilder.AppendLine("timestamp,log_type,equation,p_score,margin_state,sim2val_y,p_estimate,sigma,status,verified");
                    File.WriteAllText(csvFilePath, csvBuilder.ToString());
                    csvBuilder.Clear();
                }
            }
            
            // Log initialization entry
            var initialLog = new LogEntry
            {
                timestamp = DateTime.Now.ToString("o"),
                log_type = "SYSTEM_INIT",
                equation = "P=x+y+z+t+g+i+c",
                message = "Certification Compiler Initialized",
                p_score = 0f,
                margin_state = 0f,
                sim2val_y = 0f,
                p_estimate = 0f,
                sigma = 0f,
                status = "INITIALIZED",
                verified = true
            };
            AppendToLog(initialLog);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CertificationCompiler] Failed to initialize log files: {e.Message}");
        }
    }

    void Update()
    {
        if (!Application.isPlaying || !isInitialized) return;
        
        // Throttle compilation if rate specified
        if (compilationRate > 0 && Time.time - lastCompilationTime < compilationInterval) return;
        
        // --- THE COMPILATION CYCLE ---
        CompileCertificate();
        
        lastCompilationTime = Time.time;
    }

    void CompileCertificate()
    {
        // Robustness Check (Periodic)
        if (Time.time - lastRobustnessCheck > robustnessCheckInterval)
        {
            lastRobustnessCheck = Time.time;
            if (useRustCore && isRustCoreLoaded)
            {
                bool robust = RustCoreBridge.IsRustCoreAvailable();
                if (!robust)
                {
                    TriggerRobustnessFailure();
                    return;
                }
            }
        }
        
        // Use Rust Core if available, otherwise fallback to C#
        if (useRustCore && isRustCoreLoaded && isRustCoreInitialized)
        {
            CompileCertificateRust();
        }
        else if (enableFallback)
        {
            CompileCertificateCSharp();
        }
    }

    void CompileCertificateRust()
    {
        // Gather State for Rust
        Vector3 pos = realRobot != null ? realRobot.transform.position : transform.position;
        Vector3 vel = Vector3.zero;
        Rigidbody rb = realRobot != null ? realRobot.GetComponent<Rigidbody>() : GetComponent<Rigidbody>();
        if (rb != null)
        {
            vel = rb.velocity;
        }
        
        RustCoreBridge.State7D state = new RustCoreBridge.State7D();
        state.position = new float[3] { pos.x, pos.y, pos.z };
        state.velocity = new float[3] { vel.x, vel.y, vel.z };
        state.heading = realRobot != null ? realRobot.transform.eulerAngles.y : transform.eulerAngles.y;
        state.timestamp = (ulong)(Time.time * 1000);
        state.certainty = GetCertainty();
        state.fatigue = GetFatigue();

        // Gather Parameters
        RustCoreBridge.RigorParams parameters = new RustCoreBridge.RigorParams
        {
            alpha = vncVerifier != null ? vncVerifier.alpha : 5.0f,
            min_margin = GetMinMargin()
        };

        // Gather Obstacles (Flatten to float array for Rust)
        GameObject[] obsList = GameObject.FindGameObjectsWithTag("Obstacle");
        float[] obsArray = new float[obsList.Length * 3];
        for (int i = 0; i < obsList.Length; i++)
        {
            Vector3 obsPos = obsList[i].transform.position;
            obsArray[i * 3] = obsPos.x;
            obsArray[i * 3 + 1] = obsPos.y;
            obsArray[i * 3 + 2] = obsPos.z;
        }

        // Call Rust (P/Invoke)
        RustCoreBridge.VerificationResult result;
        int success = RustCoreBridge.calculate_p_score(
            ref state,
            ref parameters,
            obsArray,
            obsList.Length,
            out result
        );

        if (success != 0)
        {
            // Convert to C# format
            RustCoreBridge.VerificationResultCSharp resultCSharp = RustCoreBridge.ConvertResult(result);
            ProcessVerificationRust(resultCSharp);
        }
        else
        {
            Debug.LogError("[RustCore] Verification call failed. Falling back to C#.");
            if (enableFallback)
            {
                CompileCertificateCSharp();
            }
        }
    }

    void CompileCertificateCSharp()
    {
        // STEP 1: Get Rigor Data (The 7D P-Score) - C# Fallback
        // p_score = x + y + z + t + g + i + c
        float pScore = 0f;
        if (rigorRigor != null)
        {
            pScore = rigorRigor.GetTotalScore();
        }
        else if (consciousnessRigor != null)
        {
            pScore = consciousnessRigor.GetTotalScore();
        }
        
        // STEP 2: Get Physics State (Margin/Dist)
        float margin = GetMinMargin();
        
        // STEP 3: Calculate "Control Variate" (Near Miss Y) for SIM2VAL
        // In SIM2VAL++, Y is the "Control Variate"
        // We simulate Y based on proximity to safety boundary
        // If Margin is small, Variate is HIGH.
        // If Margin is large, Variate is ZERO.
        float y_sim = Mathf.Clamp01(1.0f - (margin / 5.0f)); // Normalized 0-1
        
        // STEP 4: Update Sliding Window (Statistics)
        controlVariates.Enqueue(y_sim);
        if (controlVariates.Count > slidingWindowSize)
        {
            controlVariates.Dequeue();
        }
        
        // Calculate Mean X (Historical P)
        float meanX = 0;
        if (controlVariates.Count > 0)
        {
            foreach (var y in controlVariates)
            {
                meanX += (50.0f + y * 50f); // Assume baseline P=50, scale Y to 0-50
            }
            meanX /= controlVariates.Count;
            historicalMeanX = meanX;
        }
        else
        {
            meanX = historicalMeanX;
        }
        
        // Calculate Standard Deviation (Sigma)
        float variance = 0;
        if (controlVariates.Count > 1)
        {
            foreach (var y in controlVariates)
            {
                float scaledY = 50.0f + y * 50f;
                variance += Mathf.Pow(scaledY - meanX, 2);
            }
            variance /= controlVariates.Count;
        }
        float sigma = Mathf.Sqrt(variance);
        
        // STEP 5: Calculate SIM2VAL Estimate (p-hat)
        // Formula: p_hat = X_bar + Beta * (Y_i - Y_bar)
        // Beta (Gain) usually calculated to minimize variance. Simplified here:
        float currentY = y_sim;
        float meanY = 0;
        if (controlVariates.Count > 0)
        {
            foreach (var v in controlVariates)
            {
                meanY += v;
            }
            meanY /= controlVariates.Count;
        }
        
        float beta = 0.5f; // Simple gain (could be optimized)
        float varianceY = 0;
        if (controlVariates.Count > 1)
        {
            foreach (var v in controlVariates)
            {
                varianceY += Mathf.Pow(v - meanY, 2);
            }
            varianceY /= controlVariates.Count;
        }
        
        float p_estimate = meanX + (beta * (currentY - meanY) * 50f); // Scale back to P-score range
        
        // STEP 6: Determine Safety Threshold
        float safetyThreshold = 50.0f;
        if (rigorRigor != null)
        {
            safetyThreshold = rigorRigor.safetyThreshold;
        }
        else if (consciousnessRigor != null)
        {
            safetyThreshold = consciousnessRigor.safetyThreshold;
        }
        
        // STEP 7: Generate Certificate
        bool verified = pScore >= safetyThreshold;
        LogEntry entry = new LogEntry
        {
            timestamp = DateTime.Now.ToString("o"),
            log_type = "VNC_CERTIFICATION_CSHARP",
            equation = "P=x+y+z+t+g+i+c",
            p_score = pScore,
            margin_state = margin,
            sim2val_y = currentY,
            p_estimate = p_estimate,
            sigma = sigma,
            status = verified ? "VERIFIED_SAFE" : "UNSAFE",
            verified = verified
        };
        
        AppendToLog(entry);
        certificateCount++;
        
        // UI Feedback
        UpdateUI(pScore, verified);
    }

    void ProcessVerificationRust(RustCoreBridge.VerificationResultCSharp result)
    {
        // Calculate SIM2VAL uncertainty if needed
        float sigma = result.sigma;
        if (sigma == 0.0f && controlVariates.Count > 0)
        {
            float[] variates = controlVariates.ToArray();
            RustCoreBridge.calculate_sim2val_uncertainty(variates, variates.Length, out sigma);
        }
        
        // Generate Certificate
        LogEntry entry = new LogEntry
        {
            timestamp = DateTime.Now.ToString("o"),
            log_type = "VNC_CERTIFICATION_RUST",
            equation = "P=x+y+z+t+g+i+c",
            p_score = result.p_score,
            margin_state = result.margin,
            sim2val_y = controlVariates.Count > 0 ? controlVariates.Peek() : 0f,
            p_estimate = result.p_score, // Rust already computed this
            sigma = sigma,
            status = result.is_safe ? "VERIFIED_SAFE" : result.breach_reason,
            verified = result.is_safe
        };
        
        AppendToLog(entry);
        certificateCount++;
        
        // Visual Feedback
        if (result.is_safe)
        {
            // Cyan Light = Ironclad Active
            if (compileStatusText != null)
            {
                compileStatusText.text = $"COMPILING: SAFE (P={result.p_score:F1}) [RUST]";
                compileStatusText.color = UIThemeHelper.Colors.PalantirBlue; // Replaced cyan
            }
        }
        else
        {
            // Red Light = Breach
            if (compileStatusText != null)
            {
                compileStatusText.text = $"COMPILING: UNSAFE ({result.breach_reason}) [RUST]";
                compileStatusText.color = Color.red;
            }
            
            // Hard Stop (Safety Override)
            Rigidbody rb = realRobot != null ? realRobot.GetComponent<Rigidbody>() : GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        UpdateUI(result.p_score, result.is_safe);
    }

    float GetCertainty()
    {
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetModelIntent();
        }
        if (rigorRigor != null && rigorRigor.modelManager != null)
        {
            return rigorRigor.modelManager.GetModelConfidence();
        }
        return 0.8f; // Default
    }

    float GetFatigue()
    {
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetConsciousness();
        }
        return 1.0f; // Default (no fatigue)
    }

    void TriggerRobustnessFailure()
    {
        if (compileStatusText != null)
        {
            compileStatusText.text = "CRITICAL: RUST CORE CRASHED";
                compileStatusText.color = UIThemeHelper.Colors.AppleBlue; // Replaced magenta
        }
        
        Debug.LogError("[RustCore] Robustness check failed. Rust core may have crashed.");
        
        // Memory Safety: Stop allocation to prevent cascading failure
        if (enableFallback)
        {
            useRustCore = false;
            isRustCoreLoaded = false;
            Debug.LogWarning("[RustCore] Switched to C# fallback mode");
        }
        else
        {
            enabled = false; // Disable script
        }
    }

    float GetMinMargin()
    {
        // Try to get margin from various sources
        if (vncVerifier != null)
        {
            return vncVerifier.safetyMargin;
        }
        
        // Try ROS2DashboardManager
        ROS2DashboardManager dashboard = GetComponent<ROS2DashboardManager>();
        if (dashboard != null)
        {
            return dashboard.GetMargin();
        }
        
        // Try SelfHealingSafety
        SelfHealingSafety selfHealing = GetComponent<SelfHealingSafety>();
        if (selfHealing != null)
        {
            return selfHealing.GetCurrentMargin();
        }
        
        // Default
        return 1.0f;
    }

    void UpdateUI(float pScore, bool verified)
    {
        if (compileStatusText != null)
        {
            if (verified)
            {
                compileStatusText.text = $"COMPILING: SAFE (P={pScore:F1})";
                compileStatusText.color = UIThemeHelper.Colors.PalantirBlue; // Replaced cyan
            }
            else
            {
                compileStatusText.text = $"COMPILING: UNSAFE (P={pScore:F1})";
                compileStatusText.color = Color.red;
            }
        }
        
        if (certificateCountText != null)
        {
            certificateCountText.text = $"CERTIFICATES: {certificateCount}";
        }
    }

    /// <summary>
    /// Writes log entry to Disk (JSON and CSV)
    /// </summary>
    void AppendToLog(LogEntry entry)
    {
        if (!enableLogging) return;
        
        try
        {
            // JSON Export
            string json = JsonConvert.SerializeObject(entry, Formatting.Indented);
            
            // Append to JSON array (remove last closing bracket, add entry, close bracket)
            if (File.Exists(logFilePath))
            {
                string existingContent = File.ReadAllText(logFilePath);
                if (existingContent.EndsWith("]\n") || existingContent.EndsWith("]"))
                {
                    existingContent = existingContent.TrimEnd('\n').TrimEnd(']');
                    File.WriteAllText(logFilePath, existingContent);
                }
                
                // Add comma if not first entry
                if (certificateCount > 0)
                {
                    File.AppendAllText(logFilePath, ",\n");
                }
                
                // Append entry
                File.AppendAllText(logFilePath, json);
                
                // Close array
                File.AppendAllText(logFilePath, "\n]");
            }
            
            // CSV Export
            if (enableCsvExport)
            {
                string csvLine = $"{entry.timestamp}," +
                                $"{entry.log_type}," +
                                $"\"{entry.equation}\"," +
                                $"{entry.p_score:F4}," +
                                $"{entry.margin_state:F4}," +
                                $"{entry.sim2val_y:F4}," +
                                $"{entry.p_estimate:F4}," +
                                $"{entry.sigma:F4}," +
                                $"{entry.status}," +
                                $"{(entry.verified ? 1 : 0)}";
                
                File.AppendAllText(csvFilePath, csvLine + "\n");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CertificationCompiler] Failed to write log entry: {e.Message}");
        }
    }

    void OnDestroy()
    {
        // Ensure JSON array is properly closed
        if (enableLogging && File.Exists(logFilePath))
        {
            try
            {
                string content = File.ReadAllText(logFilePath);
                if (!content.TrimEnd().EndsWith("]"))
                {
                    File.AppendAllText(logFilePath, "\n]");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CertificationCompiler] Failed to close JSON array: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Get certificate statistics
    /// </summary>
    public CertificateStats GetStats()
    {
        CertificateStats stats = new CertificateStats();
        stats.totalCertificates = certificateCount;
        stats.windowSize = controlVariates.Count;
        stats.historicalMean = historicalMeanX;
        
        if (controlVariates.Count > 0)
        {
            float variance = 0;
            foreach (var v in controlVariates)
            {
                variance += Mathf.Pow(v - (historicalMeanX / 50f - 1f), 2);
            }
            variance /= controlVariates.Count;
            stats.currentVariance = variance;
            stats.currentSigma = Mathf.Sqrt(variance);
        }
        
        return stats;
    }

    [System.Serializable]
    public class CertificateStats
    {
        public int totalCertificates;
        public int windowSize;
        public float historicalMean;
        public float currentVariance;
        public float currentSigma;
    }
}

[System.Serializable]
public class LogEntry
{
    public string timestamp;
    public string log_type;
    public string equation;
    public float p_score;
    public float margin_state;
    public float sim2val_y; // The Control Variate
    public float p_estimate; // The SIM2VAL result
    public float sigma; // Uncertainty
    public string status; // VERIFIED_SAFE or UNSAFE
    public bool verified;
    
    [Newtonsoft.Json.JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string message; // Optional message for system init
}
