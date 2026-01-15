using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// QR Code Manager - Quick Response Code integration for warehouse/research lab navigation.
/// Handles scanning, validation, and command execution for zone-based navigation.
/// </summary>
public class QrCodeManager : MonoBehaviour
{
    [Header("QR Code Configuration")]
    [Tooltip("QR Scanner component (simulated)")]
    public QrScanner scanner;

    [Tooltip("Status text display")]
    public Text statusText;

    [Tooltip("Valid QR codes and their descriptions")]
    public Dictionary<string, string> validCodes = new Dictionary<string, string>();

    [Header("Visual Feedback")]
    [Tooltip("Success audio clip")]
    public AudioClip successSound;

    [Tooltip("Error audio clip")]
    public AudioClip errorSound;

    [Tooltip("Scan effect particle system")]
    public ParticleSystem scanEffect;

    private AudioSource audioSource;
    private float lastScanTime = 0f;
    private float scanCooldown = 1.0f;

    void Start()
    {
        // Initialize scanner
        if (scanner == null)
        {
            scanner = GetComponent<QrScanner>();
            if (scanner == null)
            {
                scanner = gameObject.AddComponent<QrScanner>();
            }
        }

        // Initialize audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Initialize valid codes (Newcastle Standard)
        InitializeValidCodes();

        if (statusText != null)
        {
            statusText.text = "QR: READY";
        }

        Debug.Log("[QRCode] QR Code Manager initialized");
    }

    void InitializeValidCodes()
    {
        validCodes = new Dictionary<string, string>
        {
            { "A", "Zone A: Warehouse" },
            { "B", "Zone B: Dock" },
            { "C", "Zone C: Loading Bay" },
            { "D", "Zone D: Maintenance" },
            { "1", "Return to Start" },
            { "2", "Slow Down" },
            { "3", "Emergency Stop" },
            { "START", "Start Mission" },
            { "STOP", "Stop Mission" },
            { "HOME", "Return Home" }
        };

        Debug.Log($"[QRCode] Loaded {validCodes.Count} valid codes");
    }

    /// <summary>
    /// Scan QR code
    /// </summary>
    public void ScanCode(string code)
    {
        if (Time.time - lastScanTime < scanCooldown)
        {
            return; // Cooldown
        }

        lastScanTime = Time.time;

        // Validate code
        bool isValid = validCodes.ContainsKey(code);

        // Visual feedback
        if (scanEffect != null)
        {
            scanEffect.Play();
        }

        // Audio feedback
        if (audioSource != null)
        {
            audioSource.clip = isValid ? successSound : errorSound;
            if (audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        // Update UI
        if (statusText != null)
        {
            if (isValid)
            {
                statusText.text = $"QR: {code} - {validCodes[code]}";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = $"QR: INVALID - {code}";
                statusText.color = Color.red;
            }
        }

        Debug.Log($"[QRCode] Scanned: {code} - {(isValid ? "VALID" : "INVALID")}");

        // Execute command if valid
        if (isValid)
        {
            ExecuteCommand(code);
        }
    }

    /// <summary>
    /// Execute command based on QR code
    /// </summary>
    public void ExecuteCommand(string code)
    {
        string action = "";

        // Map code to action (Newcastle Standard)
        switch (code)
        {
            case "A":
                action = "NAVIGATE_TO_ZONE_A";
                break;
            case "B":
                action = "NAVIGATE_TO_ZONE_B";
                break;
            case "C":
                action = "NAVIGATE_TO_ZONE_C";
                break;
            case "D":
                action = "NAVIGATE_TO_ZONE_D";
                break;
            case "1":
                action = "RETURN_START";
                break;
            case "2":
                action = "SLOW_SPEED";
                break;
            case "3":
                action = "EMERGENCY_STOP";
                break;
            case "START":
                action = "START_MISSION";
                break;
            case "STOP":
                action = "STOP_MISSION";
                break;
            case "HOME":
                action = "RETURN_HOME";
                break;
            default:
                Debug.LogWarning($"[QRCode] Unknown code: {code}");
                return;
        }

        Debug.Log($"[QRCode] Executing action: {action}");

        // Send to Universal Model Manager
        UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
        if (modelManager != null)
        {
            // ExecuteCustomAction would need to be added to UniversalModelManager
            // For now, we use a workaround
            ExecuteActionOnManager(modelManager, action);
        }
        else
        {
            // Try to find in scene
            modelManager = FindObjectOfType<UniversalModelManager>();
            if (modelManager != null)
            {
                ExecuteActionOnManager(modelManager, action);
            }
            else
            {
                Debug.LogWarning("[QRCode] UniversalModelManager not found");
            }
        }
    }

    void ExecuteActionOnManager(UniversalModelManager manager, string action)
    {
        // Execute action based on type
        switch (action)
        {
            case "NAVIGATE_TO_ZONE_A":
                NavigateToZone("Zone_A");
                break;
            case "NAVIGATE_TO_ZONE_B":
                NavigateToZone("Zone_B");
                break;
            case "NAVIGATE_TO_ZONE_C":
                NavigateToZone("Zone_C");
                break;
            case "NAVIGATE_TO_ZONE_D":
                NavigateToZone("Zone_D");
                break;
            case "RETURN_START":
                ReturnToStart();
                break;
            case "SLOW_SPEED":
                SetSpeedModifier(0.5f);
                break;
            case "EMERGENCY_STOP":
                EmergencyStop();
                break;
            case "START_MISSION":
                StartMission();
                break;
            case "STOP_MISSION":
                StopMission();
                break;
            case "RETURN_HOME":
                ReturnHome();
                break;
        }
    }

    void NavigateToZone(string zoneName)
    {
        GameObject zone = GameObject.Find(zoneName);
        if (zone != null)
        {
            // Set target position
            FleetManager fleetManager = FindObjectOfType<FleetManager>();
            if (fleetManager != null)
            {
                // Would set target via fleet manager
                Debug.Log($"[QRCode] Navigating to {zoneName}");
            }
        }
        else
        {
            Debug.LogWarning($"[QRCode] Zone {zoneName} not found in scene");
        }
    }

    void ReturnToStart()
    {
        GameObject startPoint = GameObject.Find("StartPoint");
        if (startPoint != null)
        {
            NavigateToZone("StartPoint");
        }
        else
        {
            Debug.Log("[QRCode] Returning to origin");
        }
    }

    void SetSpeedModifier(float modifier)
    {
        AdaptiveVlaManager vlaManager = FindObjectOfType<AdaptiveVlaManager>();
        if (vlaManager != null)
        {
            // Would set speed modifier
            Debug.Log($"[QRCode] Speed modifier set to {modifier}");
        }
    }

    void EmergencyStop()
    {
        // Trigger emergency stop
        OracleController oracle = FindObjectOfType<OracleController>();
        if (oracle != null)
        {
            oracle.sendEmergencyStop("robot-1");
        }

        Debug.Log("[QRCode] Emergency stop triggered");
    }

    void StartMission()
    {
        CurriculumRunner curriculum = FindObjectOfType<CurriculumRunner>();
        if (curriculum != null)
        {
            curriculum.StartCurriculum();
        }

        Debug.Log("[QRCode] Mission started");
    }

    void StopMission()
    {
        CurriculumRunner curriculum = FindObjectOfType<CurriculumRunner>();
        if (curriculum != null)
        {
            curriculum.StopCurriculum();
        }

        Debug.Log("[QRCode] Mission stopped");
    }

    void ReturnHome()
    {
        ReturnToStart();
    }

    /// <summary>
    /// Add valid code
    /// </summary>
    public void AddValidCode(string code, string description)
    {
        if (!validCodes.ContainsKey(code))
        {
            validCodes[code] = description;
            Debug.Log($"[QRCode] Added code: {code} - {description}");
        }
    }

    /// <summary>
    /// Remove valid code
    /// </summary>
    public void RemoveValidCode(string code)
    {
        if (validCodes.ContainsKey(code))
        {
            validCodes.Remove(code);
            Debug.Log($"[QRCode] Removed code: {code}");
        }
    }

    /// <summary>
    /// Check if code is valid
    /// </summary>
    public bool IsValidCode(string code)
    {
        return validCodes.ContainsKey(code);
    }
}

/// <summary>
/// QR Scanner - Simulated QR code scanner component
/// </summary>
public class QrScanner : MonoBehaviour
{
    [Header("Scanner Settings")]
    [Tooltip("Scan range in meters")]
    public float scanRange = 5.0f;

    [Tooltip("Scan angle in degrees")]
    public float scanAngle = 45f;

    private bool isScanning = false;

    /// <summary>
    /// Trigger feedback (visual/audio)
    /// </summary>
    public void TriggerFeedback(string type)
    {
        if (type == "Success")
        {
            // Success feedback
            Debug.Log("[QrScanner] Scan successful");
        }
        else if (type == "Error")
        {
            // Error feedback
            Debug.Log("[QrScanner] Scan error");
        }
    }

    /// <summary>
    /// Start scanning
    /// </summary>
    public void StartScanning()
    {
        isScanning = true;
    }

    /// <summary>
    /// Stop scanning
    /// </summary>
    public void StopScanning()
    {
        isScanning = false;
    }

    /// <summary>
    /// Check if scanner is active
    /// </summary>
    public bool IsScanning()
    {
        return isScanning;
    }
}
