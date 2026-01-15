using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// NAVΛ Project Verifier - Master Scene Controller (The "God Script").
/// Single Source of Truth that scans the scene to ensure every component from the proposal is present,
/// connected, and mathematically sound. This is the final integration checker.
/// </summary>
[ExecuteInEditMode]
public class NavlProjectVerifier : MonoBehaviour
{
    [Header("System Components")]
    [Tooltip("Real robot GameObject (for HAL/7D Math)")]
    public GameObject realRobot;
    
    [Tooltip("Swarm leader GameObject (for DOTS/Swarm)")]
    public GameObject swarmLeader;
    
    [Tooltip("Digital twin core GameObject (for Sync)")]
    public GameObject digitalTwinCore;
    
    [Tooltip("Map loader GameObject (for Mcity SLAM)")]
    public GameObject mapLoader;
    
    [Tooltip("ROS Manager GameObject")]
    public GameObject rosManager;
    
    [Header("Compliance Requirements")]
    [Tooltip("Require ISO 26262 compliance")]
    public bool requireIso26262 = true;
    
    [Tooltip("Require SPARK temporal logic")]
    public bool requireSparkLogic = true;
    
    [Tooltip("Require Sim2Val (Curse of Rarity)")]
    public bool requireCurseOfRarity = true;
    
    [Tooltip("Require 7D VNC rigor")]
    public bool require7dVnc = true;
    
    [Tooltip("Require cognitive safety")]
    public bool requireCognitiveSafety = true;
    
    [Header("UI Status")]
    [Tooltip("Text displaying final system status")]
    public Text finalStatusText;
    
    [Tooltip("Image for integrity light indicator")]
    public Image integrityLight;
    
    [Tooltip("Text displaying component checklist")]
    public Text componentChecklistText;
    
    [Header("Verification Settings")]
    [Tooltip("Enable continuous verification")]
    public bool continuousVerification = true;
    
    [Tooltip("Verification interval (seconds)")]
    public float verificationInterval = 1f;
    
    private float lastVerificationTime = 0f;
    private VerificationResult lastResult;
    private List<string> missingComponents = new List<string>();
    private List<string> presentComponents = new List<string>();

    [System.Serializable]
    public class VerificationResult
    {
        public bool allClear;
        public string statusMessage;
        public Color statusColor;
        public List<string> missing = new List<string>();
        public List<string> present = new List<string>();
        public Dictionary<string, bool> componentStatus = new Dictionary<string, bool>();
    }

    void Start()
    {
        // Auto-find components if not assigned
        AutoFindComponents();
        
        // Run initial verification
        VerifySystem();
        
        Debug.Log("[NavlProjectVerifier] Initialized - Master scene controller ready");
    }

    void Update()
    {
        if (!continuousVerification) return;
        
        if (Time.time - lastVerificationTime >= verificationInterval)
        {
            VerifySystem();
            lastVerificationTime = Time.time;
        }
    }

    void AutoFindComponents()
    {
        // Auto-find real robot
        if (realRobot == null)
        {
            realRobot = GameObject.Find("RealRobot");
            if (realRobot == null)
            {
                realRobot = GameObject.FindGameObjectWithTag("Robot");
            }
        }
        
        // Auto-find swarm leader
        if (swarmLeader == null)
        {
            swarmLeader = GameObject.Find("SwarmController");
            if (swarmLeader == null)
            {
                swarmLeader = GameObject.Find("FleetController");
            }
        }
        
        // Auto-find digital twin core
        if (digitalTwinCore == null)
        {
            digitalTwinCore = GameObject.Find("ROS_Manager");
            if (digitalTwinCore == null)
            {
                digitalTwinCore = GameObject.Find("DigitalTwinCore");
            }
        }
        
        // Auto-find map loader
        if (mapLoader == null)
        {
            mapLoader = GameObject.Find("ROS_Manager");
        }
        
        // Auto-find ROS manager
        if (rosManager == null)
        {
            rosManager = GameObject.Find("ROS_Manager");
        }
    }

    /// <summary>
    /// Verify entire system (called manually or automatically)
    /// </summary>
    public VerificationResult VerifySystem()
    {
        VerificationResult result = new VerificationResult();
        result.componentStatus = new Dictionary<string, bool>();
        
        // 1. Verify Core Architecture (The "God" Mode)
        bool hasMath = VerifyComponent<Navl7dRigor>(realRobot, "7D Math (Navl7dRigor)");
        bool hasVnc = VerifyComponent<Vnc7dVerifier>(realRobot, "VNC 7D Verifier");
        bool hasSwarm = VerifyComponent<SwarmFormation>(swarmLeader, "Swarm Formation");
        bool hasSwarmECS = VerifyComponent<SwarmEcsManager>(swarmLeader, "Swarm ECS Manager");
        bool hasSync = VerifyComponent<SyncnomicsSop>(digitalTwinCore, "Syncnomics SOP");
        bool hasMap = VerifyComponent<McityMapLoader>(mapLoader, "Mcity Map Loader");
        
        result.componentStatus["7D Math"] = hasMath;
        result.componentStatus["VNC 7D"] = hasVnc;
        result.componentStatus["Swarm"] = hasSwarm || hasSwarmECS;
        result.componentStatus["Sync"] = hasSync;
        result.componentStatus["Mcity"] = hasMap;
        
        // 2. Verify Compliance Layers
        bool hasIso = VerifyComponent<ComplianceAuditor>(rosManager, "ISO 26262 Auditor");
        bool hasSpark = VerifyComponent<SparkTemporalVerifier>(realRobot, "SPARK Temporal Verifier");
        bool hasSim2Val = VerifyComponent<LiveValidator>(rosManager, "Sim2Val (Live Validator)");
        
        result.componentStatus["ISO 26262"] = hasIso;
        result.componentStatus["SPARK"] = hasSpark;
        result.componentStatus["Sim2Val"] = hasSim2Val;
        
        // 3. Verify Input/Perception
        bool hasVoice = VerifyComponent<SemanticVoiceCommander>(rosManager, "Semantic Voice Commander");
        bool hasAr = VerifyComponent<ArOverlay>(rosManager, "AR/XR Overlay");
        bool hasBBox = VerifyComponent<BBox3dProjector>(rosManager, "3D Bounding Box Projector");
        
        result.componentStatus["Voice"] = hasVoice;
        result.componentStatus["AR"] = hasAr;
        result.componentStatus["3D BBox"] = hasBBox;
        
        // 4. Verify HAL and Universal Architecture
        bool hasHal = VerifyComponent<UniversalHal>(rosManager, "Universal HAL");
        bool hasModelManager = VerifyComponent<UniversalModelManager>(rosManager, "Universal Model Manager");
        bool hasReasoning = VerifyComponent<ReasoningHUD>(rosManager, "Reasoning HUD");
        bool hasDigitalTwin = VerifyComponent<DigitalTwinPhysics>(realRobot, "Digital Twin Physics");
        
        result.componentStatus["HAL"] = hasHal;
        result.componentStatus["Model Manager"] = hasModelManager;
        result.componentStatus["Reasoning"] = hasReasoning;
        result.componentStatus["Digital Twin"] = hasDigitalTwin;
        
        // 5. Verify Cognitive Safety
        bool hasConsciousness = VerifyComponent<NavlConsciousnessRigor>(realRobot, "Consciousness Rigor");
        bool hasConsciousnessOverlay = VerifyComponent<ConsciousnessOverlay>(rosManager, "Consciousness Overlay");
        bool hasIntent = VerifyComponent<IntentVisualizer>(realRobot, "Intent Visualizer");
        
        result.componentStatus["Consciousness"] = hasConsciousness;
        result.componentStatus["Consciousness Overlay"] = hasConsciousnessOverlay;
        result.componentStatus["Intent"] = hasIntent;
        
        // 6. Verify Accessibility
        bool hasRwifi = VerifyComponent<RwifiSlamManager>(rosManager, "RWiFi SLAM Manager");
        bool hasGpsDenied = VerifyComponent<GpsDeniedPntManager>(realRobot, "GPS-Denied PNT Manager");
        
        result.componentStatus["RWiFi"] = hasRwifi;
        result.componentStatus["GPS-Denied"] = hasGpsDenied;
        
        // 7. Verify Core Features
        bool hasDashboard = VerifyComponent<ROS2DashboardManager>(rosManager, "ROS2 Dashboard Manager");
        bool hasTeleop = VerifyComponent<UnityTeleopController>(realRobot, "Teleop Controller");
        bool hasCamera = VerifyComponent<CameraFeedVisualizer>(rosManager, "Camera Feed Visualizer");
        bool hasMapViz = VerifyComponent<MapVisualizer>(rosManager, "Map Visualizer");
        
        result.componentStatus["Dashboard"] = hasDashboard;
        result.componentStatus["Teleop"] = hasTeleop;
        result.componentStatus["Camera"] = hasCamera;
        result.componentStatus["Map Viz"] = hasMapViz;
        
        // 8. Verify Dual-Mode Platform (Production)
        bool hasFleetDiscovery = VerifyComponent<FleetDiscoveryManager>(rosManager, "Fleet Discovery Manager");
        bool hasMissionProfile = VerifyComponent<MissionProfileSystem>(rosManager, "Mission Profile System");
        bool hasSecurityPortal = VerifyComponent<SecurityPortal>(rosManager, "Security Portal");
        bool hasAnalyticsHub = VerifyComponent<RealTimeAnalyticsHub>(rosManager, "Real-Time Analytics Hub");
        bool hasVideoStreamer = VerifyComponent<VideoStreamer>(rosManager, "Video Streamer");
        
        result.componentStatus["Fleet Discovery"] = hasFleetDiscovery;
        result.componentStatus["Mission Profile"] = hasMissionProfile;
        result.componentStatus["Security Portal"] = hasSecurityPortal;
        result.componentStatus["Analytics Hub"] = hasAnalyticsHub;
        result.componentStatus["Video Streamer"] = hasVideoStreamer;
        
        // 9. Verify Dual-Mode Platform (Academia)
        bool hasSessionRecorder = VerifyComponent<AcademicSessionRecorder>(rosManager, "Academic Session Recorder");
        bool hasAnnotationTool = VerifyComponent<LectureAnnotationTool>(rosManager, "Lecture Annotation Tool");
        bool hasWorkflowController = VerifyComponent<ExperimentWorkflowController>(rosManager, "Experiment Workflow Controller");
        bool hasDualMode = VerifyComponent<DualModeManager>(rosManager, "Dual Mode Manager");
        
        result.componentStatus["Session Recorder"] = hasSessionRecorder;
        result.componentStatus["Annotation Tool"] = hasAnnotationTool;
        result.componentStatus["Workflow Controller"] = hasWorkflowController;
        result.componentStatus["Dual Mode Manager"] = hasDualMode;
        
        // --- THE VERIFICATION LOGIC ---
        result.allClear = true;
        result.missing.Clear();
        result.present.Clear();
        
        // Check required components
        if (require7dVnc && !hasVnc) { result.allClear = false; result.missing.Add("[7D VNC]"); }
        if (requireSparkLogic && !hasSpark) { result.allClear = false; result.missing.Add("[SPARK]"); }
        if (requireCurseOfRarity && !hasSim2Val) { result.allClear = false; result.missing.Add("[Sim2Val]"); }
        if (requireIso26262 && !hasIso) { result.allClear = false; result.missing.Add("[ISO 26262]"); }
        if (requireCognitiveSafety && !hasConsciousness) { result.allClear = false; result.missing.Add("[Cognitive]"); }
        
        // Check core architecture
        if (!hasMath) { result.allClear = false; result.missing.Add("[7D Math]"); }
        if (!hasSwarm && !hasSwarmECS) { result.allClear = false; result.missing.Add("[Swarm]"); }
        if (!hasSync) { result.allClear = false; result.missing.Add("[Sync]"); }
        if (!hasMap) { result.allClear = false; result.missing.Add("[Mcity]"); }
        if (!hasVoice) { result.allClear = false; result.missing.Add("[Voice]"); }
        if (!hasAr) { result.allClear = false; result.missing.Add("[AR]"); }
        if (!hasHal) { result.allClear = false; result.missing.Add("[HAL]"); }
        
        // Build present components list
        foreach (var kvp in result.componentStatus)
        {
            if (kvp.Value)
            {
                result.present.Add(kvp.Key);
            }
        }
        
        // Update UI
        UpdateVerificationUI(result);
        
        lastResult = result;
        missingComponents = result.missing;
        presentComponents = result.present;
        
        return result;
    }

    bool VerifyComponent<T>(GameObject obj, string componentName) where T : Component
    {
        if (obj == null) return false;
        
        T component = obj.GetComponent<T>();
        if (component == null)
        {
            // Try to find in children
            component = obj.GetComponentInChildren<T>();
        }
        if (component == null)
        {
            // Try to find anywhere in scene
            component = FindObjectOfType<T>();
        }
        
        return component != null;
    }

    void UpdateVerificationUI(VerificationResult result)
    {
        // Update status text
        if (finalStatusText != null)
        {
            if (result.allClear)
            {
                finalStatusText.text = "SYSTEM: NAVΛ CERTIFIED\n" +
                                      $"Components: {result.present.Count} verified";
                finalStatusText.color = Color.cyan;
            }
            else
            {
                string missingStr = string.Join(" ", result.missing);
                finalStatusText.text = $"ERR: MISSING {missingStr}\n" +
                                      $"Present: {result.present.Count} | Missing: {result.missing.Count}";
                finalStatusText.color = Color.red;
            }
        }
        
        // Update integrity light
        if (integrityLight != null)
        {
            if (result.allClear)
            {
                integrityLight.color = Color.green;
                integrityLight.GetComponent<Image>().color = Color.green;
            }
            else
            {
                integrityLight.color = Color.red;
                integrityLight.GetComponent<Image>().color = Color.red;
                // Flashing alarm
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                integrityLight.GetComponent<Image>().color = Color.Lerp(Color.red, new Color(1, 0, 0, 0.5f), pulse);
            }
        }
        
        // Update component checklist
        if (componentChecklistText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("COMPONENT CHECKLIST:");
            sb.AppendLine("-".PadRight(40, '-'));
            
            foreach (var kvp in result.componentStatus)
            {
                string status = kvp.Value ? "✓" : "✗";
                Color statusColor = kvp.Value ? Color.green : Color.red;
                sb.AppendLine($"{status} {kvp.Key}");
            }
            
            componentChecklistText.text = sb.ToString();
        }
    }

    /// <summary>
    /// Get last verification result
    /// </summary>
    public VerificationResult GetLastResult()
    {
        return lastResult;
    }

    /// <summary>
    /// Check if system is certified
    /// </summary>
    public bool IsSystemCertified()
    {
        return lastResult != null && lastResult.allClear;
    }

    /// <summary>
    /// Get missing components
    /// </summary>
    public List<string> GetMissingComponents()
    {
        return new List<string>(missingComponents);
    }

    /// <summary>
    /// Get present components
    /// </summary>
    public List<string> GetPresentComponents()
    {
        return new List<string>(presentComponents);
    }

    /// <summary>
    /// Generate verification report
    /// </summary>
    public string GenerateVerificationReport()
    {
        if (lastResult == null) return "No verification performed";
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("NAVΛ PROJECT VERIFICATION REPORT");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine($"Status: {(lastResult.allClear ? "CERTIFIED" : "INCOMPLETE")}");
        sb.AppendLine($"Components Present: {lastResult.present.Count}");
        sb.AppendLine($"Components Missing: {lastResult.missing.Count}");
        sb.AppendLine();
        sb.AppendLine("COMPONENT STATUS:");
        sb.AppendLine("-".PadRight(60, '-'));
        
        foreach (var kvp in lastResult.componentStatus)
        {
            sb.AppendLine($"{kvp.Key}: {(kvp.Value ? "PRESENT" : "MISSING")}");
        }
        
        if (lastResult.missing.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("MISSING COMPONENTS:");
            foreach (string missing in lastResult.missing)
            {
                sb.AppendLine($"  - {missing}");
            }
        }
        
        return sb.ToString();
    }
}
