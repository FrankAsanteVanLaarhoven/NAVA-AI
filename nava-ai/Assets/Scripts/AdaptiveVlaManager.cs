using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Adaptive VLA Manager - Online Training that adapts VLA weights based on Ironclad P-Score.
/// The VLA Policy adapts its weights based on the Ironclad P-Score:
/// - If P is High (>50), model becomes "Confident" (Aggressive exploration)
/// - If P is Low (<30), model becomes "Conservative" (Safety first)
/// </summary>
public class AdaptiveVlaManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying training status")]
    public Text trainingStatusText;
    
    [Tooltip("Text displaying confidence bias")]
    public Text confidenceBiasText;
    
    [Tooltip("Text displaying adaptive mode")]
    public Text modeText;
    
    [Header("Visual Feedback")]
    [Tooltip("Particle system for weight updates")]
    public ParticleSystem weightUpdateParticles;
    
    [Tooltip("Light for training indicator")]
    public Light trainingLight;
    
    [Header("Learning Parameters")]
    [Tooltip("Learning rate (how fast to adapt)")]
    public float learningRate = 0.01f;
    
    [Tooltip("High P-score threshold (become confident)")]
    public float highPThreshold = 50.0f;
    
    [Tooltip("Low P-score threshold (become conservative)")]
    public float lowPThreshold = 30.0f;
    
    [Tooltip("Sliding window size for P-score history")]
    public int windowSize = 50;
    
    [Tooltip("Minimum window size before learning")]
    public int minWindowSize = 10;
    
    [Header("Component References")]
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to universal model manager")]
    public UniversalModelManager modelManager;
    
    [Tooltip("Reference to VLA saliency overlay")]
    public VlaSaliencyOverlay vlaSaliency;
    
    [Tooltip("Reference to environment profiler")]
    public EnvironmentProfiler environmentProfiler;
    
    private float currentConfidenceBias = 0.0f; // Learned Parameter (-1 to 1)
    private List<float> recentPScores = new List<float>();
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.1f; // Update every 100ms
    private TrainingState currentState = TrainingState.Neutral;

    [System.Serializable]
    public enum TrainingState
    {
        Conservative,  // Low P-score - Safety first
        Neutral,       // Medium P-score - Balanced
        Confident      // High P-score - Aggressive exploration
    }

    void Start()
    {
        // Get component references if not assigned
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (modelManager == null)
        {
            modelManager = GetComponent<UniversalModelManager>();
        }
        
        if (vlaSaliency == null)
        {
            vlaSaliency = GetComponent<VlaSaliencyOverlay>();
        }
        
        if (environmentProfiler == null)
        {
            environmentProfiler = GetComponent<EnvironmentProfiler>();
        }
        
        // Create particles if not assigned
        if (weightUpdateParticles == null)
        {
            GameObject particlesObj = new GameObject("WeightUpdateParticles");
            particlesObj.transform.SetParent(transform);
            weightUpdateParticles = particlesObj.AddComponent<ParticleSystem>();
            
            var main = weightUpdateParticles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = Color.blue;
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = weightUpdateParticles.emission;
            emission.rateOverTime = 0; // Manual burst
        }
        
        Debug.Log("[AdaptiveVlaManager] Initialized - Online training ready");
    }

    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        // 1. Get Current "Ironclad" P-Score
        float currentP = GetCurrentPScore();
        
        // 2. Accumulate Data (Sliding Window)
        recentPScores.Add(currentP);
        if (recentPScores.Count > windowSize)
        {
            recentPScores.RemoveAt(0);
        }
        
        // 3. Calculate Learning Update (Simple Gradient Descent)
        if (recentPScores.Count >= minWindowSize)
        {
            UpdateModelWeights();
        }
        
        // 4. Environment-Based Risk Tuning
        UpdateEnvironmentRiskTuning();
        
        // 5. Visual Update
        UpdateTrainingVisuals();
    }
    
    void UpdateEnvironmentRiskTuning()
    {
        if (environmentProfiler == null) return;
        
        // 1. Get Environment Data
        float friction = environmentProfiler.GetFriction();
        float light = environmentProfiler.GetLightQuality();
        float riskFactor = environmentProfiler.GetRiskFactor();
        
        // 2. Adapt "Risk Appetite" (SOTA: Bayesian Optimization)
        // High Friction (Ice) = High Risk -> Reduce Speed
        // Low Light (Dark) = High Risk -> Increase Caution (Slower)
        
        float speedModifier = 1.0f;
        if (friction < 0.8f) speedModifier *= 0.5f; // Cut speed in half on slippery terrain
        if (light < 0.5f) speedModifier *= 0.8f; // Reduce speed in low light
        
        // Overall risk-based adjustment
        speedModifier *= (1.0f - riskFactor * 0.3f); // Reduce speed by up to 30% based on risk
        
        // 3. Update VLA Model (Online Tuning)
        if (modelManager != null)
        {
            modelManager.SetRiskFactor(speedModifier);
        }
        
        // 4. Update speed modifier for display
        this.speedModifier = speedModifier;
    }

    float GetCurrentPScore()
    {
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetPScore();
        }
        
        // Fallback: Use VNC barrier value
        Vnc7dVerifier vnc = GetComponent<Vnc7dVerifier>();
        if (vnc != null)
        {
            float barrier = vnc.GetBarrierValue();
            return barrier * 50f + 50f; // Convert to 0-100 scale
        }
        
        return 50f; // Default neutral
    }

    void UpdateModelWeights()
    {
        // Calculate average P-score over window
        float avgP = recentPScores.Average();
        
        // Adjust "Model Confidence" (Bias) based on P-score
        TrainingState newState;
        
        if (avgP > highPThreshold)
        {
            // High P-score - Become Bolder (Increase Confidence)
            currentConfidenceBias += learningRate;
            currentConfidenceBias = Mathf.Clamp(currentConfidenceBias, -1f, 1f);
            newState = TrainingState.Confident;
        }
        else if (avgP < lowPThreshold)
        {
            // Low P-score - Become Cautious (Decrease Confidence)
            currentConfidenceBias -= learningRate;
            currentConfidenceBias = Mathf.Clamp(currentConfidenceBias, -1f, 1f);
            newState = TrainingState.Conservative;
        }
        else
        {
            // Medium P-score - Maintain current bias
            newState = TrainingState.Neutral;
        }
        
        // Update state
        if (newState != currentState)
        {
            currentState = newState;
            OnStateChanged(newState);
        }
        
        // Apply bias to model (if VLA saliency available)
        if (vlaSaliency != null)
        {
            // Adjust VLA confidence based on bias
            // This would interface with actual VLA model weights
            // For now, this is a placeholder for the concept
        }
    }

    void OnStateChanged(TrainingState newState)
    {
        // Visual feedback on state change
        if (weightUpdateParticles != null)
        {
            weightUpdateParticles.Play();
        }
        
        Debug.Log($"[AdaptiveVlaManager] State changed to: {newState}, Bias: {currentConfidenceBias:F3}");
    }

    void UpdateTrainingVisuals()
    {
        // Update status text
        if (trainingStatusText != null)
        {
            float avgP = recentPScores.Count > 0 ? recentPScores.Average() : 50f;
            
            switch (currentState)
            {
                case TrainingState.Confident:
                    trainingStatusText.text = $"LEARNING: CONFIDENT (High P-Score: {avgP:F1})\n" +
                                            $"Bias: {currentConfidenceBias:F3} (Aggressive)";
                    trainingStatusText.color = Color.green;
                    break;
                
                case TrainingState.Conservative:
                    trainingStatusText.text = $"LEARNING: CONSERVATIVE (Low P-Score: {avgP:F1})\n" +
                                            $"Bias: {currentConfidenceBias:F3} (Cautious)";
                    trainingStatusText.color = Color.yellow;
                    break;
                
                case TrainingState.Neutral:
                    trainingStatusText.text = $"LEARNING: NEUTRAL (P-Score: {avgP:F1})\n" +
                                            $"Bias: {currentConfidenceBias:F3} (Balanced)";
                    trainingStatusText.color = Color.cyan;
                    break;
            }
        }
        
        // Update confidence bias text
        if (confidenceBiasText != null)
        {
            confidenceBiasText.text = $"Confidence Bias: {currentConfidenceBias:F3}\n" +
                                      $"Window: {recentPScores.Count}/{windowSize}";
            confidenceBiasText.color = Color.Lerp(Color.red, Color.green, (currentConfidenceBias + 1f) / 2f);
        }
        
        // Update training light
        if (trainingLight != null)
        {
            switch (currentState)
            {
                case TrainingState.Confident:
                    trainingLight.color = Color.green;
                    trainingLight.intensity = 2f;
                    break;
                case TrainingState.Conservative:
                    trainingLight.color = Color.yellow;
                    trainingLight.intensity = 3f;
                    break;
                case TrainingState.Neutral:
                    trainingLight.color = Color.cyan;
                    trainingLight.intensity = 1.5f;
                    break;
            }
        }
        
        // Update mode text (environment-based)
        if (modeText != null)
        {
            if (speedModifier < 0.9f)
            {
                modeText.text = $"MODE: ADAPTIVE (SAFE) | Speed: {speedModifier:P0}";
                modeText.color = Color.green;
            }
            else
            {
                modeText.text = $"MODE: ADAPTIVE (CAUTION) | Speed: {speedModifier:P0}";
                modeText.color = Color.yellow;
            }
        }
    }

    /// <summary>
    /// Get current confidence bias
    /// </summary>
    public float GetConfidenceBias()
    {
        return currentConfidenceBias;
    }

    /// <summary>
    /// Get current training state
    /// </summary>
    public TrainingState GetTrainingState()
    {
        return currentState;
    }

    /// <summary>
    /// Get average P-score over window
    /// </summary>
    public float GetAveragePScore()
    {
        return recentPScores.Count > 0 ? recentPScores.Average() : 50f;
    }

    /// <summary>
    /// Reset learning (clear history)
    /// </summary>
    public void ResetLearning()
    {
        recentPScores.Clear();
        currentConfidenceBias = 0f;
        currentState = TrainingState.Neutral;
        Debug.Log("[AdaptiveVlaManager] Learning reset");
    }
}
