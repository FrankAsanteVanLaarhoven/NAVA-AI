using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Swarm Certification Overlay - Fleet-Wide Certification View.
/// Upgrades single-robot God Mode to a Fleet Certification View. Shows the P-Score
/// (Safety) of every agent simultaneously and enforces Global Zones.
/// </summary>
public class SwarmCertificationOverlay : MonoBehaviour
{
    [System.Serializable]
    public class AgentStatus
    {
        public GameObject agent;
        public float pScore;
        public bool isSafe;
        public string breachReason;
        public GameObject threatReticle;
    }

    [Header("Overlay Settings")]
    [Tooltip("Canvas group for overlay panel")]
    public CanvasGroup overlayPanel;
    
    [Tooltip("Text displaying fleet status")]
    public Text fleetStatusText;
    
    [Tooltip("Text displaying P-score distribution")]
    public Text distributionText;
    
    [Header("Visualization")]
    [Tooltip("Prefab for threat reticle")]
    public GameObject threatReticlePrefab;
    
    [Tooltip("Material for threat visualization")]
    public Material threatMaterial;
    
    [Header("Agent Settings")]
    [Tooltip("Agent tag for auto-detection")]
    public string agentTag = "Agent";
    
    [Tooltip("Safety threshold for P-score")]
    public float safetyThreshold = 50.0f;
    
    [Tooltip("Update interval (seconds)")]
    public float updateInterval = 0.5f;
    
    [Header("Graph Visualization")]
    [Tooltip("LineRenderer for P-score distribution graph")]
    public LineRenderer distributionGraph;
    
    [Tooltip("Graph width")]
    public float graphWidth = 10f;
    
    [Tooltip("Graph height")]
    public float graphHeight = 5f;
    
    private List<GameObject> agents = new List<GameObject>();
    private Dictionary<GameObject, AgentStatus> agentStatuses = new Dictionary<GameObject, AgentStatus>();
    private float lastUpdateTime = 0f;
    private List<float> pScoreHistory = new List<float>();

    void Start()
    {
        // Auto-detect agents
        RefreshAgentList();
        
        // Create distribution graph if not assigned
        if (distributionGraph == null)
        {
            GameObject graphObj = new GameObject("DistributionGraph");
            graphObj.transform.SetParent(transform);
            distributionGraph = graphObj.AddComponent<LineRenderer>();
            distributionGraph.useWorldSpace = false;
            distributionGraph.startWidth = 0.1f;
            distributionGraph.endWidth = 0.1f;
            distributionGraph.material = new Material(Shader.Find("Sprites/Default"));
            distributionGraph.startColor = Color.cyan;
            distributionGraph.endColor = Color.cyan;
        }
        
        Debug.Log("[SwarmCertification] Fleet certification overlay initialized");
    }

    void Update()
    {
        // Throttle updates
        if (Time.time - lastUpdateTime < updateInterval) return;
        
        // Refresh agent list periodically
        if (Time.frameCount % 300 == 0)
        {
            RefreshAgentList();
        }
        
        // Update fleet status
        UpdateFleetStatus();
        
        // Update overlay
        UpdateOverlay();
        
        lastUpdateTime = Time.time;
    }

    void RefreshAgentList()
    {
        GameObject[] foundAgents = GameObject.FindGameObjectsWithTag(agentTag);
        agents.Clear();
        agents.AddRange(foundAgents);
        
        // Update statuses dictionary
        foreach (var agent in agents)
        {
            if (!agentStatuses.ContainsKey(agent))
            {
                agentStatuses[agent] = new AgentStatus { agent = agent };
            }
        }
        
        // Remove statuses for agents that no longer exist
        var keysToRemove = agentStatuses.Keys.Where(k => !agents.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            DestroyThreatReticle(agentStatuses[key]);
            agentStatuses.Remove(key);
        }
    }

    void UpdateFleetStatus()
    {
        bool anyUnsafe = false;
        int safeCount = 0;
        int unsafeCount = 0;
        float minPScore = float.MaxValue;
        float maxPScore = float.MinValue;
        float avgPScore = 0f;
        
        List<float> currentPScores = new List<float>();
        
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            
            // Get P-score
            float pScore = GetAgentPScore(agent);
            currentPScores.Add(pScore);
            
            // Update status
            AgentStatus status;
            if (!agentStatuses.ContainsKey(agent))
            {
                status = new AgentStatus { agent = agent };
                agentStatuses[agent] = status;
            }
            else
            {
                status = agentStatuses[agent];
            }
            
            status.pScore = pScore;
            status.isSafe = pScore >= safetyThreshold;
            
            if (!status.isSafe)
            {
                anyUnsafe = true;
                unsafeCount++;
                status.breachReason = GetBreachReason(agent, pScore);
                
                // Draw threat reticle
                DrawThreatReticle(status);
            }
            else
            {
                safeCount++;
                DestroyThreatReticle(status);
            }
            
            // Track min/max
            if (pScore < minPScore) minPScore = pScore;
            if (pScore > maxPScore) maxPScore = pScore;
        }
        
        // Calculate average
        if (currentPScores.Count > 0)
        {
            avgPScore = currentPScores.Average();
            pScoreHistory.Add(avgPScore);
            if (pScoreHistory.Count > 100)
            {
                pScoreHistory.RemoveAt(0);
            }
        }
        
        // Update UI
        UpdateFleetUI(anyUnsafe, safeCount, unsafeCount, minPScore, maxPScore, avgPScore);
        
        // Update distribution graph
        UpdateDistributionGraph(currentPScores);
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

    string GetBreachReason(GameObject agent, float pScore)
    {
        // Determine breach reason
        if (pScore < 30f)
        {
            return "CRITICAL";
        }
        else if (pScore < safetyThreshold)
        {
            return "UNSAFE";
        }
        return "SAFE";
    }

    void DrawThreatReticle(AgentStatus status)
    {
        if (status.agent == null) return;
        
        // Create or update threat reticle
        if (status.threatReticle == null)
        {
            if (threatReticlePrefab != null)
            {
                status.threatReticle = Instantiate(threatReticlePrefab);
            }
            else
            {
                // Create simple reticle
                status.threatReticle = new GameObject($"ThreatReticle_{status.agent.name}");
                LineRenderer lr = status.threatReticle.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth = 0.3f;
                lr.endWidth = 0.3f;
                lr.material = threatMaterial != null ? threatMaterial : CreateDefaultMaterial();
                lr.color = Color.red;
                lr.loop = true;
                lr.positionCount = 8;
                
                // Create box shape
                float size = 2f;
                Vector3[] positions = new Vector3[]
                {
                    new Vector3(-size, size, 0),
                    new Vector3(size, size, 0),
                    new Vector3(size, -size, 0),
                    new Vector3(-size, -size, 0),
                    new Vector3(-size, size, 0), // Close loop
                    new Vector3(-size, size, size),
                    new Vector3(size, size, size),
                    new Vector3(size, size, 0)
                };
                lr.SetPositions(positions);
            }
        }
        
        // Position reticle above agent
        status.threatReticle.transform.position = status.agent.transform.position + Vector3.up * 3f;
        status.threatReticle.transform.LookAt(Camera.main != null ? Camera.main.transform : transform);
    }

    void DestroyThreatReticle(AgentStatus status)
    {
        if (status.threatReticle != null)
        {
            Destroy(status.threatReticle);
            status.threatReticle = null;
        }
    }

    void UpdateFleetUI(bool anyUnsafe, int safeCount, int unsafeCount, 
                       float minP, float maxP, float avgP)
    {
        // Update overlay panel alpha
        if (overlayPanel != null)
        {
            float targetAlpha = anyUnsafe ? 1.0f : 0.0f;
            overlayPanel.alpha = Mathf.Lerp(overlayPanel.alpha, targetAlpha, Time.deltaTime * 2.0f);
        }
        
        // Update fleet status text
        if (fleetStatusText != null)
        {
            string status = anyUnsafe ? "⚠️ UNSAFE" : "✓ SAFE";
            Color statusColor = anyUnsafe ? Color.red : Color.green;
            
            fleetStatusText.text = $"FLEET STATUS: {status}\n" +
                                  $"Safe: {safeCount} | Unsafe: {unsafeCount}\n" +
                                  $"P-Score: Min={minP:F1} Max={maxP:F1} Avg={avgP:F1}";
            fleetStatusText.color = statusColor;
        }
    }

    void UpdateDistributionGraph(List<float> pScores)
    {
        if (distributionGraph == null || pScores.Count == 0) return;
        
        // Create histogram-style graph
        int bins = Mathf.Min(pScores.Count, 20);
        float[] histogram = new float[bins];
        float binSize = 100f / bins;
        
        foreach (float score in pScores)
        {
            int bin = Mathf.Clamp(Mathf.FloorToInt(score / binSize), 0, bins - 1);
            histogram[bin]++;
        }
        
        // Normalize
        float maxCount = histogram.Max();
        if (maxCount > 0)
        {
            for (int i = 0; i < bins; i++)
            {
                histogram[i] /= maxCount;
            }
        }
        
        // Update graph
        distributionGraph.positionCount = bins;
        for (int i = 0; i < bins; i++)
        {
            float x = (float)i / bins * graphWidth - graphWidth / 2f;
            float y = histogram[i] * graphHeight;
            distributionGraph.SetPosition(i, new Vector3(x, y, 0));
        }
        
        // Update distribution text
        if (distributionText != null)
        {
            distributionText.text = $"P-SCORE DISTRIBUTION:\n" +
                                    $"Bins: {bins} | Samples: {pScores.Count}";
        }
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.red;
        return mat;
    }

    /// <summary>
    /// Get agent status
    /// </summary>
    public AgentStatus GetAgentStatus(GameObject agent)
    {
        if (agentStatuses.ContainsKey(agent))
        {
            return agentStatuses[agent];
        }
        return null;
    }

    /// <summary>
    /// Get all unsafe agents
    /// </summary>
    public List<GameObject> GetUnsafeAgents()
    {
        return agentStatuses.Values
            .Where(s => s != null && !s.isSafe)
            .Select(s => s.agent)
            .Where(a => a != null)
            .ToList();
    }

    /// <summary>
    /// Get fleet safety rate
    /// </summary>
    public float GetFleetSafetyRate()
    {
        if (agentStatuses.Count == 0) return 1f;
        
        int safeCount = agentStatuses.Values.Count(s => s != null && s.isSafe);
        return (float)safeCount / agentStatuses.Count;
    }
}
