using UnityEngine;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using UnityEngine.UI;

/// <summary>
/// Self-Healing Safety - Auto-Recovery System.
/// If a collision occurs (due to slipping or drowsiness), the system increases
/// the Safety Margin Buffer dynamically to prevent recurrence.
/// </summary>
public class SelfHealingSafety : MonoBehaviour
{
    [Header("Visual Feedback")]
    [Tooltip("LineRenderer for healing vector visualization")]
    public LineRenderer healingVector;
    
    [Tooltip("Particle system for healing effect")]
    public ParticleSystem healingEffect;
    
    [Tooltip("Audio source for repair sound")]
    public AudioSource repairSound;
    
    [Header("Component References")]
    [Tooltip("Reference to environment profiler")]
    public EnvironmentProfiler environmentProfiler;
    
    [Tooltip("Reference to universal model manager")]
    public UniversalModelManager modelManager;
    
    [Tooltip("Reference to reasoning HUD")]
    public ReasoningHUD reasoningHUD;
    
    [Header("Healing Parameters")]
    [Tooltip("Base safety margin")]
    public float baseMargin = 1.0f;
    
    [Tooltip("Margin increment per collision")]
    public float marginIncrement = 0.2f;
    
    [Tooltip("Maximum safety margin")]
    public float maxMargin = 3.0f;
    
    [Tooltip("Collision count threshold for fatigue detection")]
    public int fatigueCollisionThreshold = 3;
    
    [Tooltip("Speed reduction factor for fatigue")]
    public float fatigueSpeedReduction = 0.5f;
    
    [Header("Recovery Settings")]
    [Tooltip("Pause duration after collision (seconds)")]
    public float recoveryPauseDuration = 1.0f;
    
    [Tooltip("Enable automatic recovery")]
    public bool enableAutoRecovery = true;
    
    [Header("ROS Settings")]
    [Tooltip("ROS topic for margin updates")]
    public string marginTopic = "/nav/set_margin";
    
    [Header("UI References")]
    [Tooltip("Text displaying healing status")]
    public Text healingStatusText;
    
    private int _collisionCount = 0;
    private float _currentMargin = 1.0f;
    private bool _isHealing = false;
    private ROSConnection ros;
    private float lastCollisionTime = 0f;
    private float collisionCooldown = 2.0f; // Prevent duplicate collision detection

    void Start()
    {
        _currentMargin = baseMargin;
        
        // Get component references if not assigned
        if (environmentProfiler == null)
        {
            environmentProfiler = GetComponent<EnvironmentProfiler>();
        }
        
        if (modelManager == null)
        {
            modelManager = GetComponent<UniversalModelManager>();
        }
        
        if (reasoningHUD == null)
        {
            reasoningHUD = GetComponent<ReasoningHUD>();
        }
        
        // Initialize ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<Float32Msg>(marginTopic, 10);
        
        // Create healing vector if not assigned
        if (healingVector == null)
        {
            GameObject lineObj = new GameObject("HealingVector");
            lineObj.transform.SetParent(transform);
            healingVector = lineObj.AddComponent<LineRenderer>();
            healingVector.useWorldSpace = true;
            healingVector.startWidth = 0.2f;
            healingVector.endWidth = 0.1f;
            healingVector.material = new Material(Shader.Find("Sprites/Default"));
            healingVector.startColor = Color.green;
            healingVector.endColor = new Color(0, 1, 0, 0.3f);
            healingVector.positionCount = 2;
            healingVector.enabled = false;
        }
        
        // Create healing effect if not assigned
        if (healingEffect == null)
        {
            GameObject effectObj = new GameObject("HealingEffect");
            effectObj.transform.SetParent(transform);
            healingEffect = effectObj.AddComponent<ParticleSystem>();
            
            var main = healingEffect.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 2f;
            main.startSize = 0.2f;
            main.startColor = Color.green;
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = healingEffect.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 50) });
        }
        
        Debug.Log("[SelfHealingSafety] Initialized - Auto-recovery system ready");
    }

    void OnCollisionEnter(Collision collision)
    {
        // Prevent duplicate collision detection
        if (Time.time - lastCollisionTime < collisionCooldown) return;
        lastCollisionTime = Time.time;
        
        // Ignore collisions with ground or self
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            collision.gameObject == gameObject)
        {
            return;
        }
        
        // 1. Detect "Unsafe Event"
        _collisionCount++;
        
        Debug.LogWarning($"[SelfHealingSafety] Collision detected! Count: {_collisionCount}");
        
        // 2. Trigger Self-Healing Protocol
        if (enableAutoRecovery && !_isHealing)
        {
            StartCoroutine(HealingRoutine(collision));
        }
    }

    IEnumerator HealingRoutine(Collision collision)
    {
        _isHealing = true;
        
        // A. Pause Robot (Wait for Physics to settle)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (healingStatusText != null)
        {
            healingStatusText.text = "HEALING: Analyzing collision...";
            healingStatusText.color = Color.yellow;
        }
        
        yield return new WaitForSeconds(recoveryPauseDuration);

        // B. Analyze Context (Why did we crash?)
        string reasoning = "";
        float confidence = 0.5f;
        bool shouldIncreaseMargin = false;
        bool shouldReduceSpeed = false;
        
        // Check terrain friction
        float friction = environmentProfiler != null ? environmentProfiler.GetFriction() : 1.0f;
        
        if (friction < 0.8f)
        {
            // Terrain was bad. Increase Margin Buffer.
            _currentMargin = Mathf.Min(_currentMargin + marginIncrement, maxMargin);
            shouldIncreaseMargin = true;
            reasoning = $"Terrain Slippery detected (Friction: {friction:F2}). Increasing Safety Margin Buffer to {_currentMargin:F2}m.";
            confidence = 0.9f;
        }
        else if (_collisionCount > fatigueCollisionThreshold)
        {
            // Driver tired. Decrease Speed Limit.
            shouldReduceSpeed = true;
            reasoning = $"High collision frequency detected ({_collisionCount} collisions). Reducing capability due to fatigue.";
            confidence = 0.7f;
        }
        else
        {
            // One-off error. Small margin increase.
            _currentMargin = Mathf.Min(_currentMargin + marginIncrement * 0.5f, maxMargin);
            shouldIncreaseMargin = true;
            reasoning = $"Collision detected. Slightly increasing margin buffer to {_currentMargin:F2}m.";
            confidence = 0.6f;
        }
        
        // C. Apply Healed Parameters
        if (shouldIncreaseMargin)
        {
            // Send new margin to ROS
            if (ros != null)
            {
                Float32Msg marginMsg = new Float32Msg { data = _currentMargin };
                ros.Publish(marginTopic, marginMsg);
            }
            
            // Update consciousness rigor if available
            NavlConsciousnessRigor consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
            if (consciousnessRigor != null)
            {
                // Increase safety threshold slightly
                consciousnessRigor.safetyThreshold = Mathf.Min(
                    consciousnessRigor.safetyThreshold + 5f, 
                    100f
                );
            }
        }
        
        if (shouldReduceSpeed)
        {
            // Reduce max speed via model manager
            if (modelManager != null)
            {
                modelManager.ReduceMaxSpeed(fatigueSpeedReduction);
            }
        }
        
        // D. Visual Feedback (The "Fix")
        DrawHealingVector();
        
        if (healingEffect != null)
        {
            healingEffect.Play();
        }
        
        if (repairSound != null)
        {
            repairSound.Play();
        }
        
        // E. Log Reasoning
        LogReasoning(reasoning, confidence);
        
        if (healingStatusText != null)
        {
            healingStatusText.text = $"HEALED: Margin={_currentMargin:F2}m | Collisions={_collisionCount}";
            healingStatusText.color = Color.green;
        }
        
        Debug.Log($"[SelfHealingSafety] Healing complete. New margin: {_currentMargin:F2}m");
        
        yield return new WaitForSeconds(1.0f);
        
        if (healingStatusText != null)
        {
            healingStatusText.text = "";
        }
        
        _isHealing = false;
    }

    void DrawHealingVector()
    {
        if (healingVector == null) return;
        
        // Visualize "Up and Right" (Margin increased)
        Vector3 startPos = transform.position + Vector3.down * 0.5f;
        Vector3 endPos = startPos + Vector3.up * 1.0f;
        
        healingVector.SetPosition(0, startPos);
        healingVector.SetPosition(1, endPos);
        healingVector.enabled = true;
        
        // Fade out after 2 seconds
        StartCoroutine(FadeHealingVector());
    }

    IEnumerator FadeHealingVector()
    {
        yield return new WaitForSeconds(2.0f);
        
        if (healingVector != null)
        {
            healingVector.enabled = false;
        }
    }

    void LogReasoning(string msg, float confidence)
    {
        // Send to Reasoning HUD (Visualizes "Thought")
        if (reasoningHUD != null)
        {
            reasoningHUD.LogReasoningStep(msg, confidence);
        }
        
        Debug.Log($"[SelfHealingSafety] {msg}");
    }

    /// <summary>
    /// Set safety margin manually
    /// </summary>
    public void SetMargin(float margin)
    {
        _currentMargin = Mathf.Clamp(margin, baseMargin, maxMargin);
        
        if (ros != null)
        {
            Float32Msg marginMsg = new Float32Msg { data = _currentMargin };
            ros.Publish(marginTopic, marginMsg);
        }
    }

    /// <summary>
    /// Get current safety margin
    /// </summary>
    public float GetCurrentMargin()
    {
        return _currentMargin;
    }

    /// <summary>
    /// Get collision count
    /// </summary>
    public int GetCollisionCount()
    {
        return _collisionCount;
    }

    /// <summary>
    /// Reset collision count
    /// </summary>
    public void ResetCollisionCount()
    {
        _collisionCount = 0;
        _currentMargin = baseMargin;
    }
}
