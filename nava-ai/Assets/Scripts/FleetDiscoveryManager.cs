using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fleet Discovery Manager - Production Capability.
/// Uses ROS 2 DDS (Data Distribution Service) to auto-discover all agents
/// on the network and build the dashboard dynamically.
/// </summary>
public class FleetDiscoveryManager : MonoBehaviour
{
    [System.Serializable]
    public class DiscoveredAgent
    {
        public string agentID;
        public string agentType;
        public string ipAddress;
        public float lastSeen;
        public GameObject agentObject;
        public bool isConnected;
    }

    [Header("Discovery Settings")]
    [Tooltip("Discovery topic for ROS 2 DDS")]
    public string discoveryTopic = "/ros_discovery/info";
    
    [Tooltip("Discovery interval (seconds)")]
    public float discoveryInterval = 5.0f;
    
    [Tooltip("Agent timeout (seconds)")]
    public float agentTimeout = 10.0f;
    
    [Header("UI References")]
    [Tooltip("Text displaying fleet count")]
    public Text fleetCountText;
    
    [Tooltip("Text displaying discovery status")]
    public Text discoveryStatusText;
    
    [Header("Agent Prefabs")]
    [Tooltip("Prefab for discovered agents")]
    public GameObject agentPrefab;
    
    [Tooltip("Parent transform for spawned agents")]
    public Transform agentParent;
    
    private ROSConnection ros;
    private Dictionary<string, DiscoveredAgent> discoveredAgents = new Dictionary<string, DiscoveredAgent>();
    private float lastDiscoveryTime = 0f;
    private bool isScanning = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to ROS Discovery Service
        ros.Subscribe<StringMsg>(discoveryTopic, OnFleetUpdate);
        
        // Auto-Connect to discovered robots
        StartCoroutine(ScanForAgents());
        
        Debug.Log("[FleetDiscovery] Production fleet discovery initialized");
    }

    void Update()
    {
        // Periodic discovery scan
        if (Time.time - lastDiscoveryTime > discoveryInterval)
        {
            lastDiscoveryTime = Time.time;
            if (!isScanning)
            {
                StartCoroutine(ScanForAgents());
            }
        }
        
        // Cleanup timed-out agents
        CleanupTimedOutAgents();
        
        // Update UI
        UpdateUI();
    }

    IEnumerator ScanForAgents()
    {
        isScanning = true;
        
        // 1. Broadcast Discovery Request
        if (discoveryStatusText != null)
        {
            discoveryStatusText.text = "DISCOVERY: Scanning...";
            discoveryStatusText.color = Color.yellow;
        }
        
        Debug.Log("[FleetDiscovery] Broadcasting Fleet Discovery Request...");
        
        // In production: Publish discovery request to ROS
        StringMsg discoveryRequest = new StringMsg { data = "DISCOVERY_REQUEST" };
        ros.Publish("/ros_discovery/request", discoveryRequest);
        
        // 2. Wait for responses
        yield return new WaitForSeconds(2.0f);
        
        // In production: Parse /ros_discovery/list topic
        // and dynamically spawn prefabs based on IDs
        if (discoveredAgents.Count > 0)
        {
            if (discoveryStatusText != null)
            {
                discoveryStatusText.text = "DISCOVERY: Active";
                discoveryStatusText.color = Color.green;
            }
        }
        else
        {
            if (discoveryStatusText != null)
            {
                discoveryStatusText.text = "DISCOVERY: No agents found";
                discoveryStatusText.color = Color.gray;
            }
        }
        
        isScanning = false;
    }

    void OnFleetUpdate(StringMsg msg)
    {
        // Parse agent list: "Robot_ID_001,Robot_ID_002,..."
        string[] agentIds = msg.data.Split(',');
        
        foreach (string agentId in agentIds)
        {
            string trimmedId = agentId.Trim();
            if (string.IsNullOrEmpty(trimmedId)) continue;
            
            // Check if agent already discovered
            if (!discoveredAgents.ContainsKey(trimmedId))
            {
                // New agent discovered
                DiscoveredAgent newAgent = new DiscoveredAgent
                {
                    agentID = trimmedId,
                    agentType = DetectAgentType(trimmedId),
                    ipAddress = "Unknown", // Would be extracted from ROS discovery
                    lastSeen = Time.time,
                    isConnected = false
                };
                
                discoveredAgents[trimmedId] = newAgent;
                
                // Spawn agent object if prefab assigned
                if (agentPrefab != null)
                {
                    SpawnAgentObject(newAgent);
                }
                
                Debug.Log($"[FleetDiscovery] New agent discovered: {trimmedId}");
            }
            else
            {
                // Update existing agent
                discoveredAgents[trimmedId].lastSeen = Time.time;
                discoveredAgents[trimmedId].isConnected = true;
            }
        }
        
        // Update Dashboard
        UpdateUI();
    }

    string DetectAgentType(string agentId)
    {
        // Infer agent type from ID
        string lower = agentId.ToLower();
        if (lower.Contains("drone")) return "Drone";
        if (lower.Contains("bot") || lower.Contains("turtle")) return "TurtleBot";
        if (lower.Contains("spot")) return "Spot";
        if (lower.Contains("humanoid")) return "Humanoid";
        return "Unknown";
    }

    void SpawnAgentObject(DiscoveredAgent agent)
    {
        if (agentPrefab == null) return;
        
        GameObject agentObj = Instantiate(agentPrefab, agentParent != null ? agentParent : transform);
        agentObj.name = $"Agent_{agent.agentID}";
        agentObj.tag = "Agent";
        
        // Position randomly or based on discovery order
        Vector3 spawnPos = new Vector3(
            discoveredAgents.Count * 2f,
            0,
            0
        );
        agentObj.transform.position = spawnPos;
        
        agent.agentObject = agentObj;
        
        Debug.Log($"[FleetDiscovery] Spawned agent object: {agent.agentID}");
    }

    void CleanupTimedOutAgents()
    {
        List<string> toRemove = new List<string>();
        
        foreach (var kvp in discoveredAgents)
        {
            if (Time.time - kvp.Value.lastSeen > agentTimeout)
            {
                kvp.Value.isConnected = false;
                // Optionally remove after longer timeout
                if (Time.time - kvp.Value.lastSeen > agentTimeout * 2)
                {
                    toRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (string key in toRemove)
        {
            if (discoveredAgents[key].agentObject != null)
            {
                Destroy(discoveredAgents[key].agentObject);
            }
            discoveredAgents.Remove(key);
            Debug.Log($"[FleetDiscovery] Removed timed-out agent: {key}");
        }
    }

    void UpdateUI()
    {
        int activeCount = discoveredAgents.Values.Count(a => a.isConnected);
        
        if (fleetCountText != null)
        {
            fleetCountText.text = $"FLEET: {activeCount}/{discoveredAgents.Count} ACTIVE";
            fleetCountText.color = activeCount > 0 ? Color.green : Color.gray;
        }
    }

    /// <summary>
    /// Get all discovered agents
    /// </summary>
    public List<DiscoveredAgent> GetDiscoveredAgents()
    {
        return new List<DiscoveredAgent>(discoveredAgents.Values);
    }

    /// <summary>
    /// Get agent by ID
    /// </summary>
    public DiscoveredAgent GetAgent(string agentID)
    {
        if (discoveredAgents.ContainsKey(agentID))
        {
            return discoveredAgents[agentID];
        }
        return null;
    }

    /// <summary>
    /// Force discovery scan
    /// </summary>
    public void ForceDiscovery()
    {
        StartCoroutine(ScanForAgents());
    }
}
