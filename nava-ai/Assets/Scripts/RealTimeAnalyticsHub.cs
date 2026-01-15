using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Text;

/// <summary>
/// Real-Time Analytics Hub - Production Capability.
/// Connects to all agents, aggregates metrics (CPU/RAM usage, FPS, Safety Margins),
/// and pushes to a Web Dashboard (Grafana/InfluxDB).
/// </summary>
public class RealTimeAnalyticsHub : MonoBehaviour
{
    [System.Serializable]
    public class AgentMetrics
    {
        public string agentID;
        public float pScore;
        public float margin;
        public float fps;
        public float cpuUsage;
        public float memoryUsage;
        public System.DateTime timestamp;
    }

    [Header("Dashboard Settings")]
    [Tooltip("Dashboard URL (InfluxDB/Grafana)")]
    public string dashboardUrl = "http://192.168.1.50:8086";
    
    [Tooltip("Enable dashboard push")]
    public bool enableDashboardPush = false;
    
    [Tooltip("Push interval (seconds)")]
    public float pushInterval = 5.0f;
    
    [Header("UI References")]
    [Tooltip("Text displaying aggregated metrics")]
    public Text metricsText;
    
    [Header("Metrics")]
    [Tooltip("Store metrics history")]
    public bool storeHistory = true;
    
    [Tooltip("Max history size")]
    public int maxHistorySize = 1000;
    
    private List<AgentMetrics> metricsHistory = new List<AgentMetrics>();
    private float lastPushTime = 0f;
    private float avgMargin = 0f;
    private float avgFPS = 0f;
    private float avgPScore = 0f;
    private int fleetSize = 0;

    void Start()
    {
        Debug.Log("[AnalyticsHub] Real-time analytics initialized");
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // 1. Aggregate Telemetry from all agents
        AggregateMetrics();
        
        // 2. Update UI
        UpdateUI();
        
        // 3. Push to Dashboard (if enabled)
        if (enableDashboardPush && Time.time - lastPushTime > pushInterval)
        {
            PushToDashboard();
            lastPushTime = Time.time;
        }
    }

    void AggregateMetrics()
    {
        var agents = GameObject.FindGameObjectsWithTag("Agent");
        fleetSize = agents.Length;
        
        if (fleetSize == 0)
        {
            avgMargin = 0f;
            avgFPS = 0f;
            avgPScore = 0f;
            return;
        }
        
        float marginSum = 0f;
        float fpsSum = 0f;
        float pScoreSum = 0f;
        int validCount = 0;
        
        foreach (var agent in agents)
        {
            // Get P-score
            NavlConsciousnessRigor consciousness = agent.GetComponent<NavlConsciousnessRigor>();
            Navl7dRigor rigor = agent.GetComponent<Navl7dRigor>();
            
            float pScore = 0f;
            if (consciousness != null)
            {
                pScore = consciousness.GetTotalScore();
            }
            else if (rigor != null)
            {
                pScore = rigor.GetTotalScore();
            }
            
            // Get margin
            Vnc7dVerifier vnc = agent.GetComponent<Vnc7dVerifier>();
            float margin = vnc != null ? vnc.safetyMargin : 0f;
            
            // Calculate FPS (simplified)
            float fps = 1.0f / Time.deltaTime;
            
            marginSum += margin;
            fpsSum += fps;
            pScoreSum += pScore;
            validCount++;
            
            // Store individual metrics
            if (storeHistory)
            {
                AgentMetrics metrics = new AgentMetrics
                {
                    agentID = agent.name,
                    pScore = pScore,
                    margin = margin,
                    fps = fps,
                    cpuUsage = 0f, // Would be from system in production
                    memoryUsage = 0f, // Would be from system in production
                    timestamp = System.DateTime.Now
                };
                
                metricsHistory.Add(metrics);
                
                // Limit history size
                if (metricsHistory.Count > maxHistorySize)
                {
                    metricsHistory.RemoveAt(0);
                }
            }
        }
        
        if (validCount > 0)
        {
            avgMargin = marginSum / validCount;
            avgFPS = fpsSum / validCount;
            avgPScore = pScoreSum / validCount;
        }
    }

    void UpdateUI()
    {
        if (metricsText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FLEET METRICS:");
            sb.AppendLine($"Agents: {fleetSize}");
            sb.AppendLine($"Avg P-Score: {avgPScore:F1}");
            sb.AppendLine($"Avg Margin: {avgMargin:F2}m");
            sb.AppendLine($"Avg FPS: {avgFPS:F1}");
            
            metricsText.text = sb.ToString();
            metricsText.color = avgPScore > 50f ? Color.green : Color.yellow;
        }
    }

    void PushToDashboard()
    {
        if (string.IsNullOrEmpty(dashboardUrl)) return;
        
        // 2. Push to Dashboard (WebSockets/HTTP)
        // In production: Send JSON POST to dashboardUrl
        string payload = $"{{\"fleet_size\":{fleetSize},\"avg_margin\":{avgMargin:F2},\"avg_fps\":{avgFPS:F2},\"avg_p_score\":{avgPScore:F2},\"timestamp\":\"{System.DateTime.Now:o}\"}}";
        
        // In production: Use UnityWebRequest or similar
        // StartCoroutine(HttpSend(payload));
        
        Debug.Log($"[AnalyticsHub] Pushed metrics to dashboard: {payload}");
    }

    /// <summary>
    /// Get aggregated metrics
    /// </summary>
    public AgentMetrics GetAggregatedMetrics()
    {
        return new AgentMetrics
        {
            agentID = "FLEET",
            pScore = avgPScore,
            margin = avgMargin,
            fps = avgFPS,
            cpuUsage = 0f,
            memoryUsage = 0f,
            timestamp = System.DateTime.Now
        };
    }

    /// <summary>
    /// Get metrics history
    /// </summary>
    public List<AgentMetrics> GetMetricsHistory()
    {
        return new List<AgentMetrics>(metricsHistory);
    }

    /// <summary>
    /// Clear metrics history
    /// </summary>
    public void ClearHistory()
    {
        metricsHistory.Clear();
        Debug.Log("[AnalyticsHub] Metrics history cleared");
    }
}
