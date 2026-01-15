using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fleet Geofence - Waymo-Style Dynamic Safety Envelopes.
/// Creates dynamic 3D exclusion zones for every agent. Unlike static geofencing,
/// these zones "breathe" (expand/contract) based on the Certainty (P) of that
/// specific agent's model (Waymo Standard).
/// </summary>
public class FleetGeofence : MonoBehaviour
{
    [System.Serializable]
    public class AgentEnvelope
    {
        public GameObject agent;
        public float currentRadius;
        public float targetRadius;
        public Color envelopeColor;
        public LineRenderer zoneRenderer;
        public bool isLeader;
    }

    [Header("Envelope Settings")]
    [Tooltip("Base radius for safety envelopes")]
    public float baseRadius = 5.0f;
    
    [Tooltip("Minimum radius (high certainty)")]
    public float minRadius = 3.0f;
    
    [Tooltip("Maximum radius (low certainty)")]
    public float maxRadius = 8.0f;
    
    [Tooltip("Radius transition speed")]
    public float transitionSpeed = 2.0f;
    
    [Header("Visualization")]
    [Tooltip("Enable envelope visualization")]
    public bool enableVisualization = true;
    
    [Tooltip("Material for envelope lines")]
    public Material envelopeMaterial;
    
    [Tooltip("Points per envelope circle")]
    public int circlePoints = 32;
    
    [Header("Agent Settings")]
    [Tooltip("Agent tag for auto-detection")]
    public string agentTag = "Agent";
    
    [Tooltip("Leader agent (different color)")]
    public GameObject leaderAgent;
    
    private Dictionary<GameObject, AgentEnvelope> agentEnvelopes = new Dictionary<GameObject, AgentEnvelope>();
    private List<GameObject> agents = new List<GameObject>();

    void Start()
    {
        // Auto-detect agents
        RefreshAgentList();
        
        // Create envelope visualizations
        CreateEnvelopes();
        
        Debug.Log($"[FleetGeofence] Initialized with {agents.Count} agents");
    }

    void Update()
    {
        // Refresh agent list periodically
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            RefreshAgentList();
        }
        
        // Update all envelopes
        UpdateEnvelopes();
    }

    void RefreshAgentList()
    {
        GameObject[] foundAgents = GameObject.FindGameObjectsWithTag(agentTag);
        agents.Clear();
        agents.AddRange(foundAgents);
        
        // Update envelopes dictionary
        foreach (var agent in agents)
        {
            if (!agentEnvelopes.ContainsKey(agent))
            {
                CreateEnvelopeForAgent(agent);
            }
        }
        
        // Remove envelopes for agents that no longer exist
        var keysToRemove = agentEnvelopes.Keys.Where(k => !agents.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            DestroyEnvelope(agentEnvelopes[key]);
            agentEnvelopes.Remove(key);
        }
    }

    void CreateEnvelopes()
    {
        foreach (var agent in agents)
        {
            CreateEnvelopeForAgent(agent);
        }
    }

    void CreateEnvelopeForAgent(GameObject agent)
    {
        if (agent == null) return;
        
        AgentEnvelope envelope = new AgentEnvelope
        {
            agent = agent,
            currentRadius = baseRadius,
            targetRadius = baseRadius,
            isLeader = (agent == leaderAgent || (leaderAgent == null && agents.IndexOf(agent) == 0))
        };
        
        // Set color based on leader status
        envelope.envelopeColor = envelope.isLeader ? Color.blue : Color.cyan;
        
        // Create LineRenderer for visualization
        if (enableVisualization)
        {
            GameObject envelopeObj = new GameObject($"Envelope_{agent.name}");
            envelopeObj.transform.SetParent(transform);
            LineRenderer lr = envelopeObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = envelopeMaterial != null ? envelopeMaterial : CreateDefaultMaterial();
            lr.startColor = envelope.envelopeColor;
            lr.endColor = new Color(envelope.envelopeColor.r, envelope.envelopeColor.g, envelope.envelopeColor.b, 0.3f);
            lr.positionCount = circlePoints;
            
            envelope.zoneRenderer = lr;
        }
        
        agentEnvelopes[agent] = envelope;
    }

    void UpdateEnvelopes()
    {
        foreach (var kvp in agentEnvelopes)
        {
            GameObject agent = kvp.Key;
            AgentEnvelope envelope = kvp.Value;
            
            if (agent == null || envelope == null) continue;
            
            // 1. Get Certainty from Agent
            float pScore = GetAgentPScore(agent);
            
            // 2. Dynamic Radius (Inversely proportional to Certainty)
            // High Certainty (P=100) -> Tight Zone (Radius 3m)
            // Low Certainty (P=30) -> Loose Zone (Radius 8m)
            float certaintyRatio = Mathf.Clamp01(pScore / 100.0f);
            envelope.targetRadius = Mathf.Lerp(maxRadius, minRadius, certaintyRatio);
            
            // 3. Smooth transition
            envelope.currentRadius = Mathf.Lerp(
                envelope.currentRadius, 
                envelope.targetRadius, 
                Time.deltaTime * transitionSpeed
            );
            
            // 4. Visualize (Wireframe Sphere)
            if (enableVisualization && envelope.zoneRenderer != null)
            {
                DrawEnvelope(agent.transform.position, envelope.currentRadius, envelope);
            }
            
            // 5. Update color based on certainty
            if (envelope.zoneRenderer != null)
            {
                Color certaintyColor = Color.Lerp(Color.red, Color.green, certaintyRatio);
                envelope.zoneRenderer.startColor = certaintyColor;
                envelope.zoneRenderer.endColor = new Color(certaintyColor.r, certaintyColor.g, certaintyColor.b, 0.3f);
            }
        }
    }

    float GetAgentPScore(GameObject agent)
    {
        if (agent == null) return 50f;
        
        // Try consciousness rigor first
        NavlConsciousnessRigor consciousness = agent.GetComponent<NavlConsciousnessRigor>();
        if (consciousness != null)
        {
            return consciousness.GetTotalScore();
        }
        
        // Try 7D rigor
        Navl7dRigor rigor = agent.GetComponent<Navl7dRigor>();
        if (rigor != null)
        {
            return rigor.GetTotalScore();
        }
        
        // Default
        return 50f;
    }

    void DrawEnvelope(Vector3 center, float radius, AgentEnvelope envelope)
    {
        if (envelope.zoneRenderer == null) return;
        
        // Create circle points
        for (int i = 0; i < circlePoints; i++)
        {
            float angle = (float)i / circlePoints * 360f * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0.1f, // Slightly above ground
                Mathf.Sin(angle) * radius
            );
            envelope.zoneRenderer.SetPosition(i, pos);
        }
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.cyan;
        return mat;
    }

    void DestroyEnvelope(AgentEnvelope envelope)
    {
        if (envelope.zoneRenderer != null)
        {
            Destroy(envelope.zoneRenderer.gameObject);
        }
    }

    /// <summary>
    /// Get envelope radius for an agent
    /// </summary>
    public float GetAgentEnvelopeRadius(GameObject agent)
    {
        if (agentEnvelopes.ContainsKey(agent))
        {
            return agentEnvelopes[agent].currentRadius;
        }
        return baseRadius;
    }

    /// <summary>
    /// Check if position is within any agent's envelope
    /// </summary>
    public bool IsPositionInEnvelope(Vector3 position)
    {
        foreach (var kvp in agentEnvelopes)
        {
            if (kvp.Key == null) continue;
            
            float distance = Vector3.Distance(position, kvp.Key.transform.position);
            if (distance <= kvp.Value.currentRadius)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get all agents whose envelopes contain a position
    /// </summary>
    public List<GameObject> GetAgentsAtPosition(Vector3 position)
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (var kvp in agentEnvelopes)
        {
            if (kvp.Key == null) continue;
            
            float distance = Vector3.Distance(position, kvp.Key.transform.position);
            if (distance <= kvp.Value.currentRadius)
            {
                result.Add(kvp.Key);
            }
        }
        
        return result;
    }
}
