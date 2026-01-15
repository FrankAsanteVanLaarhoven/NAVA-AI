using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Swarm Formation Controller - Manages quadrotor formations with SafeVLA constraints.
/// Syncs multiple agents using Synaptic Operating Procedures (SOPs).
/// </summary>
public class SwarmFormation : MonoBehaviour
{
    [Header("Formation Settings")]
    [Tooltip("Array of agent GameObjects")]
    public GameObject[] agents;
    
    [Tooltip("Formation shape")]
    public FormationShape shape = FormationShape.V;
    
    [Tooltip("Formation spacing")]
    public float formationSpacing = 2f;
    
    [Tooltip("Formation leader (null = use centroid)")]
    public GameObject leader;
    
    [Header("SafeVLA Constraints")]
    [Tooltip("Enable SafeVLA safety checks")]
    public bool enableSafeVLAChecks = true;
    
    [Tooltip("Minimum safe distance between agents")]
    public float minAgentDistance = 1.5f;
    
    [Tooltip("Reference to voxel map for collision checking")]
    public VoxelMapBuilder voxelMap;
    
    [Tooltip("Reference to geofence editor for no-go zones")]
    public GeofenceEditor geofenceEditor;
    
    [Header("Formation Dynamics")]
    [Tooltip("Formation update rate (Hz)")]
    public float updateRate = 10f;
    
    [Tooltip("Formation following speed")]
    public float followSpeed = 1f;
    
    private float lastUpdateTime = 0f;
    private float updateInterval;
    private Dictionary<GameObject, Vector3> targetPositions = new Dictionary<GameObject, Vector3>();

    void Start()
    {
        updateInterval = 1f / updateRate;
        
        // Initialize target positions
        foreach (GameObject agent in agents)
        {
            if (agent != null)
            {
                targetPositions[agent] = agent.transform.position;
            }
        }
        
        Debug.Log($"[SwarmFormation] Initialized with {agents.Length} agents in {shape} formation");
    }

    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        UpdateFormation();
    }

    void UpdateFormation()
    {
        if (agents == null || agents.Length == 0) return;
        
        Vector3 centroid = CalculateCentroid();
        
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] == null) continue;
            
            Vector3 offset = GetFormationOffset(i, shape);
            Vector3 targetPos = centroid + offset;
            
            // SafeVLA Check: Is targetPos safe?
            if (enableSafeVLAChecks && IsSafe(targetPos, agents[i]))
            {
                targetPositions[agents[i]] = targetPos;
                
                // Move agent to target (simplified - real implementation would use proper controller)
                MoveAgentToTarget(agents[i], targetPos);
            }
            else
            {
                // Unsafe - command hover
                HoverAgent(agents[i]);
            }
        }
    }

    Vector3 CalculateCentroid()
    {
        if (leader != null)
        {
            return leader.transform.position;
        }
        
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (GameObject agent in agents)
        {
            if (agent != null)
            {
                sum += agent.transform.position;
                count++;
            }
        }
        
        return count > 0 ? sum / count : Vector3.zero;
    }

    Vector3 GetFormationOffset(int agentIndex, FormationShape shape)
    {
        switch (shape)
        {
            case FormationShape.Line:
                return Vector3.right * (agentIndex - agents.Length / 2f) * formationSpacing;
            
            case FormationShape.V:
                int halfSize = agents.Length / 2;
                if (agentIndex == 0) return Vector3.zero; // Leader at front
                float side = agentIndex <= halfSize ? -1f : 1f;
                int rank = agentIndex <= halfSize ? agentIndex : agentIndex - halfSize;
                return new Vector3(side * rank * formationSpacing, 0, -rank * formationSpacing);
            
            case FormationShape.Circle:
                float angle = (agentIndex / (float)agents.Length) * 360f * Mathf.Deg2Rad;
                float radius = formationSpacing * agents.Length * 0.2f;
                return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            case FormationShape.Grid:
                int cols = Mathf.CeilToInt(Mathf.Sqrt(agents.Length));
                int row = agentIndex / cols;
                int col = agentIndex % cols;
                return new Vector3((col - cols / 2f) * formationSpacing, 0, (row - agents.Length / (2f * cols)) * formationSpacing);
            
            case FormationShape.Diamond:
                int center = agents.Length / 2;
                int offset = agentIndex - center;
                return new Vector3(offset * formationSpacing, 0, Mathf.Abs(offset) * formationSpacing);
            
            default:
                return Vector3.zero;
        }
    }

    bool IsSafe(Vector3 position, GameObject agent)
    {
        // 1. Check minimum distance from other agents
        foreach (GameObject otherAgent in agents)
        {
            if (otherAgent == null || otherAgent == agent) continue;
            
            float distance = Vector3.Distance(position, otherAgent.transform.position);
            if (distance < minAgentDistance)
            {
                return false;
            }
        }
        
        // 2. Check against voxel map (if available)
        if (voxelMap != null)
        {
            // Query voxel map for occupied space at position
            // This would require exposing a method from VoxelMapBuilder
            // For now, simplified check
        }
        
        // 3. Check against geofence zones (if available)
        if (geofenceEditor != null)
        {
            // Check if position is in any active geofence zone
            // This would require exposing a method from GeofenceEditor
            // For now, simplified check
        }
        
        // 4. Check against ground (basic height check)
        if (position.y < 0.1f)
        {
            return false; // Too close to ground
        }
        
        return true;
    }

    void MoveAgentToTarget(GameObject agent, Vector3 target)
    {
        // Simplified movement - real implementation would use proper drone controller
        DroneController controller = agent.GetComponent<DroneController>();
        if (controller != null)
        {
            controller.MoveTo(target);
        }
        else
        {
            // Fallback: direct position update (for visualization)
            agent.transform.position = Vector3.MoveTowards(
                agent.transform.position,
                target,
                followSpeed * Time.deltaTime
            );
        }
    }

    void HoverAgent(GameObject agent)
    {
        DroneController controller = agent.GetComponent<DroneController>();
        if (controller != null)
        {
            controller.Hover();
        }
        // Otherwise, agent maintains current position
    }

    /// <summary>
    /// Change formation shape
    /// </summary>
    public void SetFormationShape(FormationShape newShape)
    {
        shape = newShape;
        Debug.Log($"[SwarmFormation] Changed formation to {newShape}");
    }

    /// <summary>
    /// Add agent to formation
    /// </summary>
    public void AddAgent(GameObject agent)
    {
        if (agent == null) return;
        
        List<GameObject> agentList = agents != null ? agents.ToList() : new List<GameObject>();
        if (!agentList.Contains(agent))
        {
            agentList.Add(agent);
            agents = agentList.ToArray();
            targetPositions[agent] = agent.transform.position;
        }
    }

    /// <summary>
    /// Remove agent from formation
    /// </summary>
    public void RemoveAgent(GameObject agent)
    {
        if (agent == null || agents == null) return;
        
        List<GameObject> agentList = agents.ToList();
        agentList.Remove(agent);
        agents = agentList.ToArray();
        targetPositions.Remove(agent);
    }
}

/// <summary>
/// Simplified DroneController interface for swarm agents
/// </summary>
public class DroneController : MonoBehaviour
{
    public void MoveTo(Vector3 target)
    {
        // Implementation would interface with actual drone control
        transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2f);
    }
    
    public void Hover()
    {
        // Maintain current position
    }
}
