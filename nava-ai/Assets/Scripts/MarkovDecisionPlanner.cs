using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Constrained Markov Decision Process (CMDP) Planner.
/// Visualizes decision graph (States -> Actions) while respecting GSN (Global Sensor Network) 
/// denied areas and TTP (Temporal Tactical Planning).
/// </summary>
public class MarkovDecisionPlanner : MonoBehaviour
{
    [System.Serializable]
    public class DecisionNode
    {
        public Vector3 position;
        public float cost;
        public float value;
        public DecisionNode parent;
        public int depth;
        public List<DecisionNode> children = new List<DecisionNode>();
        
        public DecisionNode(Vector3 pos, float c, DecisionNode p = null)
        {
            position = pos;
            cost = c;
            parent = p;
            depth = p != null ? p.depth + 1 : 0;
        }
    }

    [Header("Visualization")]
    [Tooltip("LineRenderer for visualizing decision graph transitions")]
    public LineRenderer transitionLines;
    
    [Tooltip("Node GameObjects for visualization")]
    public List<GameObject> nodeObjects = new List<GameObject>();
    
    [Tooltip("Node prefab for creating visualization nodes")]
    public GameObject nodePrefab;
    
    [Header("GSN Constraints")]
    [Tooltip("Global Sensor Network denied zones (GSN constraints)")]
    public Bounds[] deniedZones;
    
    [Tooltip("Reference to geofence editor for dynamic denied zones")]
    public GeofenceEditor geofenceEditor;
    
    [Header("Planning Parameters")]
    [Tooltip("Maximum planning depth")]
    public int maxDepth = 50;
    
    [Tooltip("Node expansion radius")]
    public float expansionRadius = 1f;
    
    [Tooltip("Number of actions per state")]
    public int actionsPerState = 8;
    
    [Tooltip("Cost weight for distance")]
    public float distanceWeight = 1f;
    
    [Tooltip("Cost weight for risk")]
    public float riskWeight = 10f;
    
    [Header("TTP Settings")]
    [Tooltip("Time-to-Plan timeout (seconds)")]
    public float ttpTimeout = 5f;
    
    [Tooltip("Enable temporal tactical planning")]
    public bool enableTTP = true;
    
    private List<DecisionNode> allNodes = new List<DecisionNode>();
    private DecisionNode bestPath = null;
    private bool isPlanning = false;

    void Start()
    {
        // Create LineRenderer if not assigned
        if (transitionLines == null)
        {
            GameObject lineObj = new GameObject("DecisionGraphLines");
            lineObj.transform.SetParent(transform);
            transitionLines = lineObj.AddComponent<LineRenderer>();
            transitionLines.useWorldSpace = true;
            transitionLines.startWidth = 0.1f;
            transitionLines.endWidth = 0.1f;
            transitionLines.material = new Material(Shader.Find("Sprites/Default"));
            transitionLines.color = Color.cyan;
        }
        
        Debug.Log("[MarkovDecisionPlanner] Initialized - CMDP planning ready");
    }

    /// <summary>
    /// Plan path using Constrained Markov Decision Process
    /// </summary>
    public void PlanPath(Vector3 start, Vector3 goal)
    {
        if (isPlanning)
        {
            Debug.LogWarning("[MarkovDecisionPlanner] Planning already in progress");
            return;
        }
        
        StartCoroutine(PlanPathCoroutine(start, goal));
    }

    System.Collections.IEnumerator PlanPathCoroutine(Vector3 start, Vector3 goal)
    {
        isPlanning = true;
        float startTime = Time.time;
        
        // Clear previous planning
        ClearNodes();
        
        // 1. Graph Search (Value Iteration for CMDP)
        List<DecisionNode> openSet = new List<DecisionNode>();
        Dictionary<Vector3, DecisionNode> closedSet = new Dictionary<Vector3, DecisionNode>();
        
        DecisionNode startNode = new DecisionNode(start, 0f);
        openSet.Add(startNode);
        allNodes.Add(startNode);
        
        DecisionNode bestNode = null;
        float bestScore = float.MaxValue;
        
        while (openSet.Count > 0 && Time.time - startTime < ttpTimeout)
        {
            // Sort by cost (best first)
            openSet = openSet.OrderBy(n => n.cost).ToList();
            DecisionNode current = openSet[0];
            openSet.RemoveAt(0);
            
            // Check if goal reached
            if (Vector3.Distance(current.position, goal) < 0.5f)
            {
                bestNode = current;
                bestScore = current.cost;
                break;
            }
            
            // Add to closed set
            Vector3 key = RoundPosition(current.position);
            if (!closedSet.ContainsKey(key))
            {
                closedSet[key] = current;
            }
            
            // 2. Check GSN Constraints (Is this node in a denied zone?)
            if (!IsInGSNDeniedZone(current.position))
            {
                // 3. TTP (Time-to-Plan): Calculate cost and expand
                ExpandNode(current, goal, openSet, closedSet);
            }
            
            // Yield occasionally to avoid freezing
            if (allNodes.Count % 10 == 0)
            {
                yield return null;
            }
        }
        
        // 4. Visualize Path
        if (bestNode != null)
        {
            DrawDecisionGraph(bestNode);
            bestPath = bestNode;
            Debug.Log($"[MarkovDecisionPlanner] Path found: {bestNode.depth} steps, cost: {bestNode.cost:F2}");
        }
        else
        {
            Debug.LogWarning("[MarkovDecisionPlanner] No path found within constraints");
        }
        
        isPlanning = false;
    }

    void ExpandNode(DecisionNode node, Vector3 goal, List<DecisionNode> openSet, Dictionary<Vector3, DecisionNode> closedSet)
    {
        if (node.depth >= maxDepth) return;
        
        // Generate action set (directions to explore)
        for (int i = 0; i < actionsPerState; i++)
        {
            float angle = (360f / actionsPerState) * i * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 newPos = node.position + direction * expansionRadius;
            
            Vector3 key = RoundPosition(newPos);
            
            // Skip if already explored
            if (closedSet.ContainsKey(key)) continue;
            
            // Calculate cost (distance + risk)
            float distanceCost = Vector3.Distance(node.position, newPos) * distanceWeight;
            float riskCost = CalculateRisk(newPos) * riskWeight;
            float heuristic = Vector3.Distance(newPos, goal) * distanceWeight;
            float totalCost = node.cost + distanceCost + riskCost + heuristic;
            
            // Create new node
            DecisionNode newNode = new DecisionNode(newPos, totalCost, node);
            node.children.Add(newNode);
            
            // Add to open set if not already there with better cost
            bool addToOpen = true;
            for (int j = openSet.Count - 1; j >= 0; j--)
            {
                if (Vector3.Distance(openSet[j].position, newPos) < 0.1f)
                {
                    if (openSet[j].cost <= totalCost)
                    {
                        addToOpen = false;
                    }
                    else
                    {
                        openSet.RemoveAt(j);
                    }
                    break;
                }
            }
            
            if (addToOpen)
            {
                openSet.Add(newNode);
                allNodes.Add(newNode);
            }
        }
    }

    float CalculateRisk(Vector3 position)
    {
        float risk = 0f;
        
        // Check against denied zones
        foreach (Bounds zone in deniedZones)
        {
            if (zone.Contains(position))
            {
                risk += 100f; // High risk in denied zone
            }
            else
            {
                // Risk increases as we approach denied zone
                float distance = DistanceToBounds(zone, position);
                if (distance < 2f)
                {
                    risk += (2f - distance) * 10f;
                }
            }
        }
        
        // Check against geofence zones
        if (geofenceEditor != null)
        {
            foreach (var zone in geofenceEditor.zones)
            {
                if (zone.active && IsPointInPolygon(position, zone.polygonPoints))
                {
                    risk += 100f;
                }
            }
        }
        
        return risk;
    }

    bool IsInGSNDeniedZone(Vector3 pos)
    {
        // Check static denied zones
        foreach (Bounds zone in deniedZones)
        {
            if (zone.Contains(pos)) return true;
        }
        
        // Check geofence zones
        if (geofenceEditor != null)
        {
            foreach (var zone in geofenceEditor.zones)
            {
                if (zone.active && IsPointInPolygon(pos, zone.polygonPoints))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count < 3) return false;
        
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if (((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    float DistanceToBounds(Bounds bounds, Vector3 point)
    {
        Vector3 closest = bounds.ClosestPoint(point);
        return Vector3.Distance(point, closest);
    }

    Vector3 RoundPosition(Vector3 pos)
    {
        // Round to grid for dictionary key
        return new Vector3(
            Mathf.Round(pos.x * 10f) / 10f,
            Mathf.Round(pos.y * 10f) / 10f,
            Mathf.Round(pos.z * 10f) / 10f
        );
    }

    void DrawDecisionGraph(DecisionNode endNode)
    {
        if (transitionLines == null || endNode == null) return;
        
        // Backtrack to build path
        List<Vector3> path = new List<Vector3>();
        DecisionNode current = endNode;
        
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        
        path.Reverse();
        
        // Draw path
        transitionLines.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            transitionLines.SetPosition(i, path[i]);
        }
        
        // Create node visualization objects
        CreateNodeVisualizations(path);
    }

    void CreateNodeVisualizations(List<Vector3> path)
    {
        // Clear existing nodes
        foreach (GameObject nodeObj in nodeObjects)
        {
            if (nodeObj != null) Destroy(nodeObj);
        }
        nodeObjects.Clear();
        
        // Create nodes for path
        foreach (Vector3 pos in path)
        {
            GameObject nodeObj;
            if (nodePrefab != null)
            {
                nodeObj = Instantiate(nodePrefab, pos, Quaternion.identity);
            }
            else
            {
                nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.transform.position = pos;
                nodeObj.transform.localScale = Vector3.one * 0.2f;
                nodeObj.GetComponent<Renderer>().material.color = Color.cyan;
            }
            
            nodeObj.transform.SetParent(transform);
            nodeObjects.Add(nodeObj);
        }
    }

    void ClearNodes()
    {
        allNodes.Clear();
        bestPath = null;
        
        if (transitionLines != null)
        {
            transitionLines.positionCount = 0;
        }
        
        foreach (GameObject nodeObj in nodeObjects)
        {
            if (nodeObj != null) Destroy(nodeObj);
        }
        nodeObjects.Clear();
    }

    /// <summary>
    /// Get best path as list of positions
    /// </summary>
    public List<Vector3> GetBestPath()
    {
        if (bestPath == null) return new List<Vector3>();
        
        List<Vector3> path = new List<Vector3>();
        DecisionNode current = bestPath;
        
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        
        path.Reverse();
        return path;
    }
}
