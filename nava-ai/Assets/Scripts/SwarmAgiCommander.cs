using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Swarm AGI Commander - Global Planning with Heterogeneous Task Delegation.
/// Replaces simple VLA input with Multi-Agent Reasoning. Dynamically assigns
/// different models (VLA, RL, SSM) to different agents based on their specific roles.
/// </summary>
public class SwarmAgiCommander : MonoBehaviour
{
    [System.Serializable]
    public class AgentTask
    {
        public GameObject agent;
        public string task;
        public string modelType;
        public float priority;
        public Vector3 targetPosition;
        public bool isActive;
    }

    [Header("AGI Configuration")]
    [Tooltip("Reasoning HUD for VLM visualization")]
    public ReasoningHUD reasoningInterface;
    
    [Tooltip("Text displaying swarm status")]
    public Text swarmStatusText;
    
    [Tooltip("Text displaying current mission")]
    public Text missionText;
    
    [Header("Fleet State")]
    [Tooltip("List of agents in the swarm")]
    public List<GameObject> agents = new List<GameObject>();
    
    [Tooltip("Agent tag for auto-detection")]
    public string agentTag = "Agent";
    
    [Tooltip("Current active tasks")]
    public List<AgentTask> activeTasks = new List<AgentTask>();
    
    [Header("Model Assignment")]
    [Tooltip("Enable heterogeneous model assignment")]
    public bool enableHeterogeneousAssignment = true;
    
    [Tooltip("Default model if task type unknown")]
    public string defaultModelType = "VLA";
    
    [Header("Component References")]
    [Tooltip("Reference to heterogeneous model manager")]
    public HeterogeneousModelManager modelManager;
    
    [Tooltip("Reference to fleet geofence")]
    public FleetGeofence fleetGeofence;
    
    private int missionCounter = 0;
    private string currentMission = "";

    void Start()
    {
        // Auto-detect all agents in scene
        if (agents.Count == 0)
        {
            GameObject[] foundAgents = GameObject.FindGameObjectsWithTag(agentTag);
            agents.AddRange(foundAgents);
        }
        
        // Get component references if not assigned
        if (reasoningInterface == null)
        {
            reasoningInterface = GetComponent<ReasoningHUD>();
        }
        
        if (modelManager == null)
        {
            modelManager = GetComponent<HeterogeneousModelManager>();
        }
        
        if (fleetGeofence == null)
        {
            fleetGeofence = GetComponent<FleetGeofence>();
        }
        
        UpdateSwarmStatus();
        
        Debug.Log($"[SwarmAGI] Initialized with {agents.Count} agents");
    }

    void Update()
    {
        // Update active tasks
        UpdateActiveTasks();
        
        // Update UI
        UpdateUI();
    }

    /// <summary>
    /// Execute global mission with heterogeneous task delegation
    /// </summary>
    public void ExecuteGlobalMission(string mission)
    {
        currentMission = mission;
        missionCounter++;
        
        // 1. Pass Mission to Reasoning Engine (VLM)
        if (reasoningInterface != null)
        {
            reasoningInterface.LogReasoningStep(
                $"Mission #{missionCounter}: Analyzing '{mission}'. Generating Task Delegation Plan...", 
                0.9f
            );
        }
        
        // 2. Parse Natural Language (Simulated)
        // "Clear Zone B with Drone 1 (Surveillance) and Bot 2 (RL) for Logistics"
        string[] intents = ParseMissionIntent(mission);
        
        // 3. Clear previous tasks
        activeTasks.Clear();
        
        // 4. Delegated Execution (The Unique Feature)
        for (int i = 0; i < intents.Length && i < agents.Count; i++)
        {
            GameObject agent = agents[i];
            if (agent == null) continue;
            
            string task = intents[i].Trim();
            if (string.IsNullOrEmpty(task)) continue;
            
            string modelType = IdentifyModelForTask(task);
            Vector3 targetPos = ExtractTargetPosition(task);
            float priority = CalculatePriority(task, i);
            
            // Create task
            AgentTask agentTask = new AgentTask
            {
                agent = agent,
                task = task,
                modelType = modelType,
                priority = priority,
                targetPosition = targetPos,
                isActive = true
            };
            
            activeTasks.Add(agentTask);
            
            // Assign model and execute
            ExecuteTask(agentTask);
            
            Debug.Log($"[SwarmAGI] Delegated Task '{task}' to Agent_{i} using Model '{modelType}'");
        }
        
        UpdateSwarmStatus();
    }

    string[] ParseMissionIntent(string mission)
    {
        // Simulated NLP
        // In production, this would call the VLM/LLM directly
        // Split by common conjunctions
        char[] separators = new char[] { ' ', ',', ';', '|' };
        string[] parts = mission.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
        
        // Group into task phrases
        List<string> tasks = new List<string>();
        StringBuilder currentTask = new StringBuilder();
        
        foreach (string part in parts)
        {
            string lower = part.ToLower();
            
            // Check for task keywords
            if (lower.Contains("surveillance") || lower.Contains("logistics") || 
                lower.Contains("maintenance") || lower.Contains("clear") ||
                lower.Contains("patrol") || lower.Contains("deliver"))
            {
                if (currentTask.Length > 0)
                {
                    tasks.Add(currentTask.ToString().Trim());
                    currentTask.Clear();
                }
                currentTask.Append(part + " ");
            }
            else if (lower == "with" || lower == "and" || lower == "for")
            {
                if (currentTask.Length > 0)
                {
                    tasks.Add(currentTask.ToString().Trim());
                    currentTask.Clear();
                }
            }
            else
            {
                currentTask.Append(part + " ");
            }
        }
        
        if (currentTask.Length > 0)
        {
            tasks.Add(currentTask.ToString().Trim());
        }
        
        // If no tasks found, create default tasks
        if (tasks.Count == 0)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                tasks.Add($"Task_{i + 1}");
            }
        }
        
        return tasks.ToArray();
    }

    string IdentifyModelForTask(string task)
    {
        if (!enableHeterogeneousAssignment)
        {
            return defaultModelType;
        }
        
        string lower = task.ToLower();
        
        // Heterogeneous Logic:
        // "Surveillance" -> VLA (High Visual Reqs)
        // "Logistics" -> RL (High Speed Reqs)
        // "Maintenance" -> SSM (Low Power Reqs)
        // "Patrol" -> VLA (Visual monitoring)
        // "Deliver" -> RL (Fast navigation)
        
        if (lower.Contains("surveillance") || lower.Contains("patrol") || lower.Contains("monitor"))
        {
            return "VLA";
        }
        if (lower.Contains("logistics") || lower.Contains("deliver") || lower.Contains("transport"))
        {
            return "RL";
        }
        if (lower.Contains("maintenance") || lower.Contains("inspect") || lower.Contains("repair"))
        {
            return "SSM";
        }
        
        return defaultModelType;
    }

    Vector3 ExtractTargetPosition(string task)
    {
        // Try to extract position from task string
        // In production, this would use NLP to extract coordinates
        // For now, return a default position based on task index
        
        int taskIndex = activeTasks.Count;
        float angle = (taskIndex * 360f / agents.Count) * Mathf.Deg2Rad;
        float radius = 10f;
        
        return new Vector3(
            Mathf.Cos(angle) * radius,
            0,
            Mathf.Sin(angle) * radius
        );
    }

    float CalculatePriority(string task, int agentIndex)
    {
        // Priority based on task type and agent index
        float basePriority = 1.0f;
        
        string lower = task.ToLower();
        if (lower.Contains("urgent") || lower.Contains("critical"))
        {
            basePriority = 10.0f;
        }
        else if (lower.Contains("high"))
        {
            basePriority = 5.0f;
        }
        
        // Leader agent gets higher priority
        if (agentIndex == 0)
        {
            basePriority *= 1.5f;
        }
        
        return basePriority;
    }

    void ExecuteTask(AgentTask task)
    {
        if (task.agent == null) return;
        
        // Assign model via HeterogeneousModelManager
        if (modelManager != null)
        {
            modelManager.AssignModel(task.agent, task.modelType);
        }
        
        // Execute task based on model type
        switch (task.modelType.ToUpper())
        {
            case "VLA":
                VlaSaliencyOverlay vla = task.agent.GetComponent<VlaSaliencyOverlay>();
                if (vla != null)
                {
                    // VLA-specific task execution
                    Debug.Log($"[SwarmAGI] Agent {task.agent.name} executing VLA task: {task.task}");
                }
                break;
                
            case "RL":
                UnityTeleopController teleop = task.agent.GetComponent<UnityTeleopController>();
                if (teleop != null)
                {
                    // RL-specific task execution
                    Debug.Log($"[SwarmAGI] Agent {task.agent.name} executing RL task: {task.task}");
                }
                break;
                
            case "SSM":
                // SSM-specific task execution
                Debug.Log($"[SwarmAGI] Agent {task.agent.name} executing SSM task: {task.task}");
                break;
        }
        
        // Update UniversalModelManager if present
        UniversalModelManager umm = task.agent.GetComponent<UniversalModelManager>();
        if (umm != null)
        {
            // Switch to appropriate model type (if method exists)
            // Note: UniversalModelManager may have different API
            Debug.Log($"[SwarmAGI] Agent {task.agent.name} has UniversalModelManager - model assignment handled by HeterogeneousModelManager");
        }
    }


    void UpdateActiveTasks()
    {
        // Remove completed or inactive tasks
        activeTasks.RemoveAll(t => t == null || t.agent == null || !t.isActive);
    }

    void UpdateSwarmStatus()
    {
        if (swarmStatusText != null)
        {
            swarmStatusText.text = $"SWARM-AGI: {agents.Count} AGENTS | {activeTasks.Count} ACTIVE TASKS";
        }
        
        if (missionText != null)
        {
            missionText.text = $"MISSION: {currentMission}";
        }
    }

    void UpdateUI()
    {
        // Update status periodically
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            UpdateSwarmStatus();
        }
    }

    /// <summary>
    /// Get active task for an agent
    /// </summary>
    public AgentTask GetAgentTask(GameObject agent)
    {
        return activeTasks.FirstOrDefault(t => t.agent == agent);
    }

    /// <summary>
    /// Cancel task for an agent
    /// </summary>
    public void CancelAgentTask(GameObject agent)
    {
        AgentTask task = activeTasks.FirstOrDefault(t => t.agent == agent);
        if (task != null)
        {
            task.isActive = false;
            activeTasks.Remove(task);
        }
    }

    /// <summary>
    /// Get all active tasks
    /// </summary>
    public List<AgentTask> GetActiveTasks()
    {
        return new List<AgentTask>(activeTasks);
    }
}
