using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections.Generic;

/// <summary>
/// Reasoning HUD - Visualizes the "Inner Monologue" of LLM/AGI systems.
/// Shows Chain of Thought reasoning in real-time for safety transparency.
/// </summary>
public class ReasoningHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text log displaying reasoning steps")]
    public Text thoughtLog;
    
    [Tooltip("Confidence bar showing trust in reasoning")]
    public Image confidenceBar;
    
    [Tooltip("Text showing current confidence value")]
    public Text confidenceText;
    
    [Header("Visualization")]
    [Tooltip("Maximum number of log entries to display")]
    public int maxLogEntries = 20;
    
    [Tooltip("Color for high confidence")]
    public Color highConfidenceColor = Color.green;
    
    [Tooltip("Color for low confidence (hallucination warning)")]
    public Color lowConfidenceColor = Color.red;
    
    [Tooltip("Enable pulsing effect during deep thinking")]
    public bool enablePulsing = true;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for reasoning steps")]
    public string reasoningTopic = "/reasoning/chain_of_thought";
    
    [Tooltip("ROS2 topic for confidence scores")]
    public string confidenceTopic = "/reasoning/confidence";
    
    private ROSConnection ros;
    private Queue<string> logEntries = new Queue<string>();
    private float currentConfidence = 1.0f;
    private float pulseTimer = 0f;
    private bool isThinking = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<StringMsg>(reasoningTopic, OnReasoningStep);
        ros.Subscribe<Float32Msg>(confidenceTopic, OnConfidenceUpdate);
        
        // Initialize UI
        if (thoughtLog != null)
        {
            thoughtLog.text = "Reasoning HUD Ready...\n";
        }
        
        if (confidenceBar != null)
        {
            confidenceBar.fillAmount = 1.0f;
            confidenceBar.color = highConfidenceColor;
        }
        
        Debug.Log("[ReasoningHUD] Initialized - Ready for AGI/VLM reasoning visualization");
    }

    void OnReasoningStep(StringMsg msg)
    {
        LogReasoningStep(msg.data, currentConfidence);
    }

    void OnConfidenceUpdate(Float32Msg msg)
    {
        UpdateConfidence(msg.data);
    }

    /// <summary>
    /// Log a reasoning step with confidence
    /// </summary>
    public void LogReasoningStep(string step, float confidence)
    {
        if (thoughtLog == null) return;
        
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        string entry = $"[{timestamp}] {step} (Conf: {confidence:P0})";
        
        // Add to queue
        logEntries.Enqueue(entry);
        if (logEntries.Count > maxLogEntries)
        {
            logEntries.Dequeue();
        }
        
        // Update log text
        UpdateLogDisplay();
        
        // Update confidence
        UpdateConfidence(confidence);
        
        // Set thinking state
        isThinking = confidence < 0.8f;
        
        Debug.Log($"[ReasoningHUD] {entry}");
    }

    void UpdateLogDisplay()
    {
        if (thoughtLog == null) return;
        
        string fullLog = "";
        foreach (string entry in logEntries)
        {
            fullLog = entry + "\n" + fullLog; // Newest first
        }
        
        thoughtLog.text = fullLog;
    }

    void UpdateConfidence(float confidence)
    {
        currentConfidence = Mathf.Clamp01(confidence);
        
        if (confidenceBar != null)
        {
            confidenceBar.fillAmount = currentConfidence;
            confidenceBar.color = Color.Lerp(lowConfidenceColor, highConfidenceColor, currentConfidence);
        }
        
        if (confidenceText != null)
        {
            confidenceText.text = $"Confidence: {currentConfidence:P1}";
            confidenceText.color = Color.Lerp(lowConfidenceColor, highConfidenceColor, currentConfidence);
        }
        
        // Warning for low confidence (potential hallucination)
        if (currentConfidence < 0.5f)
        {
            Debug.LogWarning($"[ReasoningHUD] LOW CONFIDENCE WARNING: {currentConfidence:P1} - Potential hallucination detected!");
        }
    }

    void Update()
    {
        // Pulse effect during deep thinking
        if (enablePulsing && isThinking && confidenceBar != null)
        {
            pulseTimer += Time.deltaTime * 2f;
            float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
            Color baseColor = Color.Lerp(lowConfidenceColor, highConfidenceColor, currentConfidence);
            confidenceBar.color = Color.Lerp(baseColor, Color.white, pulse * 0.3f);
        }
        
        // Test with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LogReasoningStep("Re-evaluating environment state...", Random.Range(0.4f, 0.9f));
        }
    }

    /// <summary>
    /// Clear reasoning log
    /// </summary>
    public void ClearLog()
    {
        logEntries.Clear();
        if (thoughtLog != null)
        {
            thoughtLog.text = "Reasoning log cleared...\n";
        }
    }

    /// <summary>
    /// Get current confidence level
    /// </summary>
    public float GetConfidence()
    {
        return currentConfidence;
    }

    /// <summary>
    /// Check if system is in low confidence state (hallucination risk)
    /// </summary>
    public bool IsLowConfidence()
    {
        return currentConfidence < 0.5f;
    }
}
