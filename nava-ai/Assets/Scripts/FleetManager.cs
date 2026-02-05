using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fleet Swarm Command Center - Multi-agent visualization and management.
/// Manages 5+ robots from one dashboard, assigning missions and visualizing individual safety margins.
/// </summary>
public class FleetManager : MonoBehaviour
{
    [Header("Fleet Configuration")]
    [Tooltip("Robot prefab to instantiate (must have ROS2DashboardManager)")]
    public GameObject robotPrefab;
    
    [Tooltip("Number of robots in fleet")]
    public int fleetSize = 5;
    
    [Tooltip("Origin point for fleet spawn")]
    public Transform fleetOrigin;
    
    [Tooltip("Spacing between robots")]
    public float robotSpacing = 2f;
    
    [Header("Fleet Layout")]
    [Tooltip("Layout pattern: Grid, Line, Circle")]
    public FleetLayout layout = FleetLayout.Line;
    
    [Header("Visualization")]
    [Tooltip("Color agents by health status")]
    public bool colorByHealth = true;
    
    [Tooltip("Show fleet status UI")]
    public bool showFleetUI = true;
    
    [Tooltip("Fleet status text (auto-created if null)")]
    public UnityEngine.UI.Text fleetStatusText;
    
    [Header("Collision Avoidance")]
    [Tooltip("Enable Unity-side swarm collision detection")]
    public bool enableSwarmCollisionAvoidance = true;
    
    [Tooltip("Minimum distance between agents")]
    public float minAgentDistance = 1.5f;
    
    private List<ROS2DashboardManager> agents = new List<ROS2DashboardManager>();
    private List<GameObject> agentObjects = new List<GameObject>();
    private Dictionary<ROS2DashboardManager, Color> originalColors = new Dictionary<ROS2DashboardManager, Color>();

    public enum FleetLayout
    {
        Line,
        Grid,
        Circle
    }

    void Start()
    {
        if (robotPrefab == null)
        {
            Debug.LogError("[FleetManager] Robot prefab not assigned!");
            return;
        }
        
        if (fleetOrigin == null)
        {
            fleetOrigin = transform;
        }
        
        // Instantiate Fleet
        InstantiateFleet();
        
        // Create fleet UI
        if (showFleetUI)
        {
            CreateFleetUI();
        }
        
        Debug.Log($"[FleetManager] Initialized fleet with {fleetSize} agents");
    }

    void InstantiateFleet()
    {
        for (int i = 0; i < fleetSize; i++)
        {
            Vector3 spawnPosition = CalculateSpawnPosition(i);
            GameObject bot = Instantiate(robotPrefab, spawnPosition, Quaternion.identity);
            bot.name = $"Agent_{i}";
            
            // Get or add ROS2DashboardManager
            ROS2DashboardManager manager = bot.GetComponent<ROS2DashboardManager>();
            if (manager == null)
            {
                manager = bot.AddComponent<ROS2DashboardManager>();
            }
            
            // Assign unique robot ID
            manager.robotID = i;
            
            // Store original color
            Renderer renderer = bot.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                originalColors[manager] = renderer.material.color;
            }
            
            agents.Add(manager);
            agentObjects.Add(bot);
        }
    }

    Vector3 CalculateSpawnPosition(int index)
    {
        Vector3 basePos = fleetOrigin != null ? fleetOrigin.position : Vector3.zero;
        
        switch (layout)
        {
            case FleetLayout.Line:
                return basePos + Vector3.right * (index * robotSpacing);
            
            case FleetLayout.Grid:
                int cols = Mathf.CeilToInt(Mathf.Sqrt(fleetSize));
                int row = index / cols;
                int col = index % cols;
                return basePos + new Vector3(col * robotSpacing, 0, row * robotSpacing);
            
            case FleetLayout.Circle:
                float angle = (index / (float)fleetSize) * 360f * Mathf.Deg2Rad;
                float radius = robotSpacing * fleetSize * 0.2f;
                return basePos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            default:
                return basePos + Vector3.right * (index * robotSpacing);
        }
    }

    void Update()
    {
        if (agents.Count == 0) return;
        
        // 1. Swarm Collision Avoidance (Unity-side visual backup)
        if (enableSwarmCollisionAvoidance)
        {
            CheckSwarmCollisions();
        }
        
        // 2. Fleet Health Overview (Color coding agents)
        if (colorByHealth)
        {
            ColorFleetByHealth();
        }
        
        // 3. Update Fleet UI
        if (showFleetUI && fleetStatusText != null)
        {
            UpdateFleetUI();
        }
    }

    void CheckSwarmCollisions()
    {
        for (int i = 0; i < agentObjects.Count; i++)
        {
            for (int j = i + 1; j < agentObjects.Count; j++)
            {
                if (agentObjects[i] == null || agentObjects[j] == null) continue;
                
                float distance = Vector3.Distance(
                    agentObjects[i].transform.position,
                    agentObjects[j].transform.position
                );
                
                if (distance < minAgentDistance)
                {
                    // Visual warning - flash red
                    FlashWarning(agentObjects[i]);
                    FlashWarning(agentObjects[j]);
                    
                    Debug.LogWarning($"[FleetManager] Agents {i} and {j} too close: {distance:F2}m");
                }
            }
        }
    }

    void FlashWarning(GameObject agent)
    {
        Renderer renderer = agent.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            StartCoroutine(FlashColor(renderer, Color.red, 0.2f));
        }
    }

    System.Collections.IEnumerator FlashColor(Renderer renderer, Color flashColor, float duration)
    {
        Color original = renderer.material.color;
        renderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        renderer.material.color = original;
    }

    void ColorFleetByHealth()
    {
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            
            GameObject agentObj = agentObjects[agents.IndexOf(agent)];
            if (agentObj == null) continue;
            
            Renderer renderer = agentObj.GetComponentInChildren<Renderer>();
            if (renderer == null || renderer.material == null) continue;
            
            float margin = agent.GetMargin();
            Color targetColor;
            
            if (margin < 0.3f)
            {
                targetColor = Color.red; // Critical
            }
            else if (margin < 0.5f)
            {
                targetColor = Color.yellow; // Warning
            }
            else if (margin < 1.0f)
            {
                targetColor = Color.Lerp(Color.yellow, Color.green, (margin - 0.5f) / 0.5f);
            }
            else
            {
                targetColor = originalColors.ContainsKey(agent) ? originalColors[agent] : Color.white;
            }
            
            renderer.material.color = targetColor;
        }
    }

    void CreateFleetUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        
        if (fleetStatusText == null)
        {
            GameObject textObj = new GameObject("FleetStatusText");
            textObj.transform.SetParent(canvas.transform, false);
            fleetStatusText = textObj.AddComponent<UnityEngine.UI.Text>();
            fleetStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fleetStatusText.fontSize = 14;
            fleetStatusText.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -150);
            rect.sizeDelta = new Vector2(300, 100);
        }
    }

    void UpdateFleetUI()
    {
        if (fleetStatusText == null) return;
        
        int healthy = agents.Count(a => a != null && a.GetMargin() > 0.5f);
        int warning = agents.Count(a => a != null && a.GetMargin() > 0.3f && a.GetMargin() <= 0.5f);
        int critical = agents.Count(a => a != null && a.GetMargin() <= 0.3f);
        
        float avgMargin = agents.Where(a => a != null).Average(a => a.GetMargin());
        
        fleetStatusText.text = $"Fleet Status: {agents.Count} Agents\n" +
                              $"Healthy: {healthy} | Warning: {warning} | Critical: {critical}\n" +
                              $"Avg Margin: {avgMargin:F2}m";
        
        // Color based on fleet health
        if (critical > 0)
        {
            fleetStatusText.color = Color.red;
        }
        else if (warning > 0)
        {
            fleetStatusText.color = Color.yellow;
        }
        else
        {
            fleetStatusText.color = Color.green;
        }
    }

    /// <summary>
    /// Get all active agents
    /// </summary>
    public List<ROS2DashboardManager> GetAgents()
    {
        return agents.Where(a => a != null).ToList();
    }

    /// <summary>
    /// Get agent by ID
    /// </summary>
    public ROS2DashboardManager GetAgent(int id)
    {
        return agents.FirstOrDefault(a => a != null && a.robotID == id);
    }

    /// <summary>
    /// Assign mission to specific agent
    /// </summary>
    public void AssignMission(int agentID, Vector3 targetPosition)
    {
        ROS2DashboardManager agent = GetAgent(agentID);
        if (agent != null)
        {
            // This would integrate with MissionPlannerUI
            Debug.Log($"[FleetManager] Assigned mission to Agent {agentID}: {targetPosition}");
        }
    }
}
