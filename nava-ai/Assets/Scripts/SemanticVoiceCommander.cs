using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;

/// <summary>
/// Semantic Voice Commander - "Dola-Style" semantic voice parsing.
/// Standard voice (e.g. "Stop") is brittle. Semantic Parsing (e.g. "Go to the red chair") 
/// requires understanding context. This enables Open-Source style reasoning.
/// </summary>
public class SemanticVoiceCommander : MonoBehaviour
{
    [System.Serializable]
    public enum SemanticAction
    {
        Goto,       // Navigate to target
        Stop,       // Emergency stop
        Avoid,      // Avoid obstacle
        Explain,    // Explain reasoning
        Toggle,     // Toggle mode
        Follow,     // Follow target
        Search      // Search for object
    }

    [System.Serializable]
    public class VoiceCommand
    {
        public string phrase;
        public SemanticAction action;
        public float confidence;
    }

    [Header("UI References")]
    [Tooltip("Text displaying voice transcript log")]
    public Text transcriptLog;
    
    [Tooltip("Text displaying recognized intent")]
    public Text intentText;
    
    [Header("Voice Settings")]
    [Tooltip("Enable voice recognition")]
    public bool enableVoiceRecognition = true;
    
    [Tooltip("Minimum confidence for command execution")]
    public float minConfidence = 0.7f;
    
    [Header("Semantic Commands")]
    [Tooltip("List of voice commands and their semantic actions")]
    public List<VoiceCommand> voiceCommands = new List<VoiceCommand>();
    
    [Header("Component References")]
    [Tooltip("Reference to ReasoningHUD for explain commands")]
    public ReasoningHUD reasoningHUD;
    
    [Tooltip("Reference to MissionPlannerUI for navigation")]
    public MissionPlannerUI missionPlanner;
    
    [Tooltip("Reference to ROS2DashboardManager")]
    public ROS2DashboardManager dashboardManager;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for voice commands")]
    public string voiceCommandTopic = "/voice/command";
    
    [Tooltip("ROS2 topic for semantic intent")]
    public string intentTopic = "/voice/intent";
    
    [Header("Audio Feedback")]
    [Tooltip("Audio source for voice feedback")]
    public AudioSource audioSource;
    
    [Tooltip("Audio clip for command acknowledgment")]
    public AudioClip ackClip;
    
    [Tooltip("Audio clip for error")]
    public AudioClip errorClip;
    
    private ROSConnection ros;
    private Queue<string> commandHistory = new Queue<string>();
    private int maxHistorySize = 10;
    private Dictionary<string, SemanticAction> keywordMap = new Dictionary<string, SemanticAction>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<StringMsg>(voiceCommandTopic, OnVoiceCommandReceived);
        ros.RegisterPublisher<StringMsg>(intentTopic, 10);
        
        // Initialize default commands
        InitializeDefaultCommands();
        
        // Get component references if not assigned
        if (reasoningHUD == null)
        {
            reasoningHUD = GetComponent<ReasoningHUD>();
        }
        
        if (missionPlanner == null)
        {
            missionPlanner = GetComponent<MissionPlannerUI>();
        }
        
        if (dashboardManager == null)
        {
            dashboardManager = GetComponent<ROS2DashboardManager>();
        }
        
        Debug.Log("[SemanticVoiceCommander] Initialized - Semantic voice parsing ready");
    }

    void InitializeDefaultCommands()
    {
        // Define default semantic commands
        if (voiceCommands.Count == 0)
        {
            voiceCommands.Add(new VoiceCommand { phrase = "go to", action = SemanticAction.Goto, confidence = 0.9f });
            voiceCommands.Add(new VoiceCommand { phrase = "stop", action = SemanticAction.Stop, confidence = 0.95f });
            voiceCommands.Add(new VoiceCommand { phrase = "avoid", action = SemanticAction.Avoid, confidence = 0.85f });
            voiceCommands.Add(new VoiceCommand { phrase = "explain why", action = SemanticAction.Explain, confidence = 0.8f });
            voiceCommands.Add(new VoiceCommand { phrase = "toggle mode", action = SemanticAction.Toggle, confidence = 0.9f });
            voiceCommands.Add(new VoiceCommand { phrase = "follow", action = SemanticAction.Follow, confidence = 0.85f });
            voiceCommands.Add(new VoiceCommand { phrase = "search for", action = SemanticAction.Search, confidence = 0.8f });
        }
        
        // Build keyword map
        foreach (var cmd in voiceCommands)
        {
            keywordMap[cmd.phrase.ToLower()] = cmd.action;
        }
    }

    void Update()
    {
        // Simulate voice input for testing (press V key)
        if (Input.GetKeyDown(KeyCode.V))
        {
            SimulateVoiceCommand("go to the red chair");
        }
    }

    void OnVoiceCommandReceived(StringMsg msg)
    {
        ProcessVoiceCommand(msg.data);
    }

    /// <summary>
    /// Process voice command (called from ROS or directly)
    /// </summary>
    public void ProcessVoiceCommand(string phrase)
    {
        if (string.IsNullOrEmpty(phrase)) return;
        
        phrase = phrase.ToLower().Trim();
        
        // Add to history
        commandHistory.Enqueue(phrase);
        if (commandHistory.Count > maxHistorySize)
        {
            commandHistory.Dequeue();
        }
        
        // 1. Semantic Parsing (Extract Intent)
        SemanticAction intent = ExtractSemanticIntent(phrase);
        
        // 2. Extract Target (if applicable)
        string target = ExtractTarget(phrase);
        
        // 3. Execute Semantic Action
        ExecuteSemanticAction(intent, target, phrase);
        
        // 4. Log to HUD
        LogToHUD($"Command: {phrase}\nIntent: {intent}\nTarget: {target}", GetActionColor(intent));
        
        // 5. Publish intent to ROS
        PublishIntent(intent, target);
        
        // 6. Audio feedback
        PlayAcknowledgment();
    }

    SemanticAction ExtractSemanticIntent(string phrase)
    {
        // Match against known commands
        foreach (var cmd in voiceCommands)
        {
            if (phrase.Contains(cmd.phrase.ToLower()))
            {
                return cmd.action;
            }
        }
        
        // Fallback: Try keyword matching
        foreach (var kvp in keywordMap)
        {
            if (phrase.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        // Default: Unknown
        return SemanticAction.Stop; // Safe default
    }

    string ExtractTarget(string phrase)
    {
        // Simple NLP: Extract object name
        // "Go to the red chair" -> "red chair"
        // "Avoid the wall" -> "wall"
        
        var objectWords = new List<string> { "chair", "table", "door", "wall", "person", "robot", "box", "cone" };
        var colorWords = new List<string> { "red", "blue", "green", "yellow", "white", "black" };
        
        string[] words = phrase.Split(' ');
        List<string> targetWords = new List<string>();
        
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i].ToLower();
            
            // Check for color + object
            if (colorWords.Contains(word) && i + 1 < words.Length)
            {
                string nextWord = words[i + 1].ToLower();
                if (objectWords.Contains(nextWord))
                {
                    return $"{word} {nextWord}";
                }
            }
            
            // Check for object alone
            if (objectWords.Contains(word))
            {
                return word;
            }
        }
        
        return "unknown";
    }

    void ExecuteSemanticAction(SemanticAction intent, string target, string phrase)
    {
        switch (intent)
        {
            case SemanticAction.Goto:
                NavigateTo(target);
                break;
            
            case SemanticAction.Stop:
                EmergencyBrake();
                break;
            
            case SemanticAction.Avoid:
                EnableAvoidance(target);
                break;
            
            case SemanticAction.Explain:
                TriggerVLMReasoning(phrase);
                break;
            
            case SemanticAction.Toggle:
                ToggleSystemMode();
                break;
            
            case SemanticAction.Follow:
                FollowTarget(target);
                break;
            
            case SemanticAction.Search:
                SearchFor(target);
                break;
        }
    }

    void NavigateTo(string target)
    {
        Debug.Log($"[SemanticVoice] Navigating to: {target}");
        
        // Find target in scene
        GameObject targetObj = FindTargetObject(target);
        if (targetObj != null && missionPlanner != null)
        {
            // Add waypoint to mission planner
            // This would require exposing a method from MissionPlannerUI
            Debug.Log($"[SemanticVoice] Found target: {targetObj.name}");
        }
        else
        {
            Debug.LogWarning($"[SemanticVoice] Target not found: {target}");
        }
    }

    void EmergencyBrake()
    {
        Debug.Log("[SemanticVoice] Executing: Emergency Stop");
        
        // Stop robot
        if (dashboardManager != null)
        {
            // Publish stop command
            ros.Publish<TwistMsg>("/nav/emergency_stop", new TwistMsg());
        }
        
        // Trigger Ironclad Manager
        IroncladManager ironclad = GetComponent<IroncladManager>();
        if (ironclad != null)
        {
            ironclad.TriggerLockdown();
        }
    }

    void EnableAvoidance(string target)
    {
        Debug.Log($"[SemanticVoice] Dynamic Obstacle Avoidance Requested for: {target}");
        // Enable dynamic obstacle avoidance
    }

    void TriggerVLMReasoning(string phrase)
    {
        Debug.Log($"[SemanticVoice] Triggering VLM Reasoning for: {phrase}");
        
        if (reasoningHUD != null)
        {
            reasoningHUD.LogReasoningStep($"Analyzing voice command: '{phrase}'", 0.85f);
        }
    }

    void ToggleSystemMode()
    {
        Debug.Log("[SemanticVoice] Toggling System Mode");
        
        UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
        if (modelManager != null)
        {
            // Toggle between modes (simplified)
            // Would cycle through available modes
        }
    }

    void FollowTarget(string target)
    {
        Debug.Log($"[SemanticVoice] Following: {target}");
        // Enable follow mode
    }

    void SearchFor(string target)
    {
        Debug.Log($"[SemanticVoice] Searching for: {target}");
        // Enable search mode
    }

    GameObject FindTargetObject(string target)
    {
        // Simple object finding (would be more sophisticated in real system)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains(target.ToLower()))
            {
                return obj;
            }
        }
        
        return null;
    }

    void LogToHUD(string msg, Color color)
    {
        if (transcriptLog != null)
        {
            string colorHex = ColorUtility.ToHtmlStringRGBA(color);
            transcriptLog.text = $"<color=#{colorHex}>{msg}</color>\n{transcriptLog.text}";
            
            // Limit log size
            if (transcriptLog.text.Length > 1000)
            {
                transcriptLog.text = transcriptLog.text.Substring(0, 1000);
            }
        }
        
        if (intentText != null)
        {
            intentText.text = msg;
            intentText.color = color;
        }
    }

    Color GetActionColor(SemanticAction action)
    {
        switch (action)
        {
            case SemanticAction.Goto:
            case SemanticAction.Follow:
            case SemanticAction.Search:
                return Color.blue;
            case SemanticAction.Stop:
                return Color.red;
            case SemanticAction.Avoid:
                return Color.yellow;
            case SemanticAction.Explain:
                return Color.cyan;
            case SemanticAction.Toggle:
                return Color.magenta;
            default:
                return Color.white;
        }
    }

    void PublishIntent(SemanticAction intent, string target)
    {
        if (ros == null) return;
        
        StringMsg msg = new StringMsg
        {
            data = $"{intent}:{target}"
        };
        
        ros.Publish(intentTopic, msg);
    }

    void PlayAcknowledgment()
    {
        if (audioSource != null && ackClip != null)
        {
            audioSource.PlayOneShot(ackClip);
        }
    }

    void SimulateVoiceCommand(string phrase)
    {
        ProcessVoiceCommand(phrase);
    }

    /// <summary>
    /// Get command history
    /// </summary>
    public Queue<string> GetCommandHistory()
    {
        return new Queue<string>(commandHistory);
    }
}
