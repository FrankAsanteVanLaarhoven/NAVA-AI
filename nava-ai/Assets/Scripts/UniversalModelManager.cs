using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections.Generic;

/// <summary>
/// Universal Model Manager - The "Conductor" that switches between VLA, VLM, RL, SSM, AGI models dynamically.
/// Central brain for heterogeneous model management.
/// </summary>
public class UniversalModelManager : MonoBehaviour
{
    [Header("Current Model")]
    [Tooltip("Currently active model type")]
    public ModelType currentModel = ModelType.SafeVLA;
    
    [Header("UI References")]
    [Tooltip("Text displaying current model status")]
    public Text modelStatusText;
    
    [Tooltip("Dropdown for model selection")]
    public Dropdown modelDropdown;
    
    [Header("Visualization Components")]
    [Tooltip("Reasoning HUD for VLM/AGI (Chain of Thought)")]
    public GameObject reasoningHUD;
    
    [Tooltip("Attention mesh for VLA (Visual Attention)")]
    public GameObject attentionMesh;
    
    [Tooltip("Q-Value visualizer for RL")]
    public GameObject qValueVisualizer;
    
    [Tooltip("Synaptic fire visualizer")]
    public SynapticFireVisualizer synapticVisualizer;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for model switching commands")]
    public string modelSwitchTopic = "/model_switch";
    
    [Tooltip("ROS2 topic for current model status")]
    public string modelStatusTopic = "/model_status";
    
    [Header("Model Configuration")]
    [Tooltip("Available models and their configurations")]
    public List<ModelConfig> availableModels = new List<ModelConfig>();
    
    private ROSConnection ros;
    private Dictionary<ModelType, ModelConfig> modelConfigs = new Dictionary<ModelType, ModelConfig>();

    [System.Serializable]
    public class ModelConfig
    {
        public ModelType type;
        public string displayName;
        public bool requiresReasoningHUD;
        public bool requiresAttentionMesh;
        public bool requiresQValues;
        public Color modelColor = Color.white;
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<StringMsg>(modelSwitchTopic, OnModelSwitchCommand);
        ros.RegisterPublisher<StringMsg>(modelStatusTopic, 10);
        
        // Initialize model configurations
        InitializeModelConfigs();
        
        // Setup UI
        SetupModelDropdown();
        
        // Apply initial model
        SwitchModel(currentModel);
        
        Debug.Log($"[UniversalModelManager] Initialized - Current model: {currentModel}");
    }

    void InitializeModelConfigs()
    {
        // Default configurations
        if (availableModels.Count == 0)
        {
            availableModels.Add(new ModelConfig { type = ModelType.VLA, displayName = "VLA", requiresAttentionMesh = true, modelColor = Color.blue });
            availableModels.Add(new ModelConfig { type = ModelType.SafeVLA, displayName = "SafeVLA", requiresAttentionMesh = true, modelColor = Color.cyan });
            availableModels.Add(new ModelConfig { type = ModelType.VLM, displayName = "VLM", requiresReasoningHUD = true, modelColor = Color.green });
            availableModels.Add(new ModelConfig { type = ModelType.AGI, displayName = "AGI", requiresReasoningHUD = true, modelColor = Color.magenta });
            availableModels.Add(new ModelConfig { type = ModelType.RL, displayName = "RL", requiresQValues = true, modelColor = Color.yellow });
            availableModels.Add(new ModelConfig { type = ModelType.Quadrotor, displayName = "Quadrotor", modelColor = Color.red });
            availableModels.Add(new ModelConfig { type = ModelType.Humanoid, displayName = "Humanoid", modelColor = Color.white });
        }
        
        // Build dictionary
        foreach (var config in availableModels)
        {
            modelConfigs[config.type] = config;
        }
    }

    void SetupModelDropdown()
    {
        if (modelDropdown != null)
        {
            modelDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (var config in availableModels)
            {
                options.Add(config.displayName);
            }
            modelDropdown.AddOptions(options);
            modelDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    void OnDropdownChanged(int index)
    {
        if (index >= 0 && index < availableModels.Count)
        {
            SwitchModel(availableModels[index].type);
        }
    }

    void OnModelSwitchCommand(StringMsg msg)
    {
        // Parse model name from ROS message
        string modelName = msg.data.ToUpper();
        ModelType targetModel = ParseModelType(modelName);
        
        if (targetModel != currentModel)
        {
            SwitchModel(targetModel);
        }
    }

    ModelType ParseModelType(string name)
    {
        switch (name.ToUpper())
        {
            case "VLA": return ModelType.VLA;
            case "SAFEVLA": return ModelType.SafeVLA;
            case "VLM": return ModelType.VLM;
            case "AGI": return ModelType.AGI;
            case "RL": return ModelType.RL;
            case "SSM": return ModelType.SSM;
            case "QUADROTOR": return ModelType.Quadrotor;
            case "HUMANOID": return ModelType.Humanoid;
            default: return currentModel;
        }
    }

    /// <summary>
    /// Switch to a different model type
    /// </summary>
    public void SwitchModel(ModelType newModel)
    {
        if (!modelConfigs.ContainsKey(newModel))
        {
            Debug.LogWarning($"[UniversalModelManager] Model {newModel} not configured");
            return;
        }
        
        ModelConfig config = modelConfigs[newModel];
        
        // Disable all visualizers
        if (reasoningHUD != null) reasoningHUD.SetActive(false);
        if (attentionMesh != null) attentionMesh.SetActive(false);
        if (qValueVisualizer != null) qValueVisualizer.SetActive(false);
        
        // Enable appropriate visualizers based on model type
        if (currentModel == ModelType.VLA || currentModel == ModelType.SafeVLA)
        {
            if (attentionMesh != null) attentionMesh.SetActive(true);
            if (reasoningHUD != null) reasoningHUD.SetActive(false);
        }
        else if (currentModel == ModelType.VLM || currentModel == ModelType.AGI)
        {
            if (attentionMesh != null) attentionMesh.SetActive(false);
            if (reasoningHUD != null) reasoningHUD.SetActive(true);
        }
        else if (currentModel == ModelType.RL)
        {
            if (qValueVisualizer != null) qValueVisualizer.SetActive(true);
        }
        else if (currentModel == ModelType.Quadrotor)
        {
            // Enable aerodynamics visualizer
            Debug.Log("[UniversalModelManager] Quadrotor mode - Aerodynamics enabled");
        }
        else if (currentModel == ModelType.Humanoid)
        {
            // Enable IK targets visualizer
            Debug.Log("[UniversalModelManager] Humanoid mode - IK Targets enabled");
        }
        
        // Update status
        currentModel = newModel;
        UpdateModelStatus();
        
        // Trigger synaptic visualization
        if (synapticVisualizer != null)
        {
            synapticVisualizer.OnModelSwitch(config.displayName);
        }
        
        // Publish status to ROS
        PublishModelStatus();
        
        Debug.Log($"[UniversalModelManager] Switched to model: {config.displayName}");
    }

    void UpdateModelStatus()
    {
        if (modelStatusText == null) return;
        
        if (modelConfigs.ContainsKey(currentModel))
        {
            ModelConfig config = modelConfigs[currentModel];
            modelStatusText.text = $"MODEL: {config.displayName}";
            modelStatusText.color = config.modelColor;
        }
        else
        {
            modelStatusText.text = $"MODEL: {currentModel}";
        }
    }

    void PublishModelStatus()
    {
        if (ros == null) return;
        
        StringMsg msg = new StringMsg { data = currentModel.ToString() };
        ros.Publish(modelStatusTopic, msg);
    }

    void Update()
    {
        // Handle keyboard shortcuts for model switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchModel(ModelType.VLA);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchModel(ModelType.SafeVLA);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchModel(ModelType.VLM);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchModel(ModelType.AGI);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchModel(ModelType.RL);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchModel(ModelType.Quadrotor);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchModel(ModelType.Humanoid);
    }

    /// <summary>
    /// Get current model configuration
    /// </summary>
    public ModelConfig GetCurrentModelConfig()
    {
        return modelConfigs.ContainsKey(currentModel) ? modelConfigs[currentModel] : null;
    }

    /// <summary>
    /// Get model confidence (for 7D rigor calculation)
    /// </summary>
    public float GetModelConfidence()
    {
        // Return confidence based on model type
        // SafeVLA = highest, AGI = lowest (more uncertainty)
        switch (currentModel)
        {
            case ModelType.SafeVLA:
                return 0.9f;
            case ModelType.VLA:
                return 0.8f;
            case ModelType.VLM:
                return 0.7f;
            case ModelType.AGI:
                return 0.6f; // AGI has inherent uncertainty
            case ModelType.RL:
                return 0.75f;
            default:
                return 0.8f;
        }
    }

    /// <summary>
    /// Get proposed action from model (simplified - would get from actual AI)
    /// </summary>
    public Vector3 GetProposedAction()
    {
        // This is a placeholder - real implementation would get from actual AI model
        // For now, return zero (no action proposed)
        return Vector3.zero;
    }

    private float riskFactor = 1.0f;
    private float maxSpeed = 1.0f;
    private float baseMaxSpeed = 1.0f;

    void Awake()
    {
        baseMaxSpeed = maxSpeed;
    }

    /// <summary>
    /// Set risk factor (speed modifier) for adaptive safety
    /// </summary>
    public void SetRiskFactor(float factor)
    {
        riskFactor = Mathf.Clamp01(factor);
        maxSpeed = baseMaxSpeed * riskFactor;
    }

    /// <summary>
    /// Reduce max speed (for fatigue/collision recovery)
    /// </summary>
    public void ReduceMaxSpeed(float reductionFactor)
    {
        baseMaxSpeed *= reductionFactor;
        baseMaxSpeed = Mathf.Max(baseMaxSpeed, 0.1f); // Minimum speed
        maxSpeed = baseMaxSpeed * riskFactor;
        Debug.Log($"[UniversalModelManager] Max speed reduced to {maxSpeed:F2} m/s");
    }

    /// <summary>
    /// Get current max speed
    /// </summary>
    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    /// <summary>
    /// Get current risk factor
    /// </summary>
    public float GetRiskFactor()
    {
        return riskFactor;
    }
}
