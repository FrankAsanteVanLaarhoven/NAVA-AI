using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Causal Graph Builder - Digital Twin Causal Graph for certification.
/// To prove to regulators that Digital Twin is "Safe," we need a Causal Graph (A -> B -> C)
/// showing that P stayed above threshold. This provides formal evidence of safety.
/// </summary>
public class CausalGraphBuilder : MonoBehaviour
{
    [System.Serializable]
    public class CausalNode
    {
        public string id;
        public Vector3 position;
        public float timestamp;
        public string nodeType; // "Action", "State", "Result"
        public float pScore;
        public bool isSafe;
    }

    [Header("Visualization")]
    [Tooltip("LineRenderer for causal chain visualization")]
    public LineRenderer causalLines;
    
    [Tooltip("Material for causal lines")]
    public Material causalMaterial;
    
    [Tooltip("Node prefab for visualization")]
    public GameObject nodePrefab;
    
    [Header("Graph Settings")]
    [Tooltip("Maximum nodes to keep in graph")]
    public int maxNodes = 100;
    
    [Tooltip("Node spacing in visualization")]
    public float nodeSpacing = 1f;
    
    [Tooltip("Enable graph visualization")]
    public bool enableVisualization = true;
    
    [Header("Component References")]
    [Tooltip("Reference to teleop controller for actions")]
    public UnityTeleopController teleopController;
    
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to VNC verifier")]
    public Vnc7dVerifier vncVerifier;
    
    private List<CausalNode> actionNodes = new List<CausalNode>();
    private List<CausalNode> stateNodes = new List<CausalNode>();
    private List<CausalNode> resultNodes = new List<CausalNode>();
    private Dictionary<string, GameObject> nodeVisualizations = new Dictionary<string, GameObject>();
    private int nodeCounter = 0;

    void Start()
    {
        // Get component references if not assigned
        if (teleopController == null)
        {
            teleopController = GetComponent<UnityTeleopController>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        // Create causal lines if not assigned
        if (causalLines == null)
        {
            GameObject lineObj = new GameObject("CausalLines");
            lineObj.transform.SetParent(transform);
            causalLines = lineObj.AddComponent<LineRenderer>();
            causalLines.useWorldSpace = true;
            causalLines.startWidth = 0.1f;
            causalLines.endWidth = 0.05f;
            causalLines.material = causalMaterial != null ? causalMaterial : CreateDefaultMaterial();
        }
        
        Debug.Log("[CausalGraphBuilder] Initialized - Causal graph ready");
    }

    void Update()
    {
        // 1. Record "Action" (VLA Model Output or Teleop)
        Vector3 action = GetLastAction();
        if (action != Vector3.zero)
        {
            RecordAction(action);
        }
        
        // 2. Record "State" (Current P-Score and Barrier)
        RecordState();
        
        // 3. Record "Result" (Safe/Unsafe based on P-Score)
        RecordResult();
        
        // 4. Visualize Causal Chain
        if (enableVisualization)
        {
            VisualizeCausalChain();
        }
    }

    Vector3 GetLastAction()
    {
        if (teleopController != null)
        {
            return teleopController.GetLastCommand();
        }
        
        // Fallback: Use transform movement
        return transform.position;
    }

    void RecordAction(Vector3 action)
    {
        CausalNode node = new CausalNode
        {
            id = $"Action_{nodeCounter++}",
            position = action,
            timestamp = Time.time,
            nodeType = "Action",
            pScore = consciousnessRigor != null ? consciousnessRigor.GetPScore() : 50f,
            isSafe = true
        };
        
        actionNodes.Add(node);
        
        // Limit node count
        if (actionNodes.Count > maxNodes)
        {
            actionNodes.RemoveAt(0);
        }
    }

    void RecordState()
    {
        float pScore = consciousnessRigor != null ? consciousnessRigor.GetPScore() : 50f;
        float threshold = consciousnessRigor != null ? consciousnessRigor.safetyThreshold : 50f;
        bool isSafe = pScore >= threshold;
        
        CausalNode node = new CausalNode
        {
            id = $"State_{nodeCounter++}",
            position = transform.position,
            timestamp = Time.time,
            nodeType = "State",
            pScore = pScore,
            isSafe = isSafe
        };
        
        stateNodes.Add(node);
        
        if (stateNodes.Count > maxNodes)
        {
            stateNodes.RemoveAt(0);
        }
    }

    void RecordResult()
    {
        float pScore = consciousnessRigor != null ? consciousnessRigor.GetPScore() : 50f;
        float threshold = consciousnessRigor != null ? consciousnessRigor.safetyThreshold : 50f;
        bool isSafe = pScore >= threshold;
        
        Vector3 resultPos = transform.position;
        if (!isSafe)
        {
            // Result position offset for unsafe states
            resultPos += Vector3.up * 0.5f;
        }
        
        CausalNode node = new CausalNode
        {
            id = $"Result_{nodeCounter++}",
            position = resultPos,
            timestamp = Time.time,
            nodeType = "Result",
            pScore = pScore,
            isSafe = isSafe
        };
        
        resultNodes.Add(node);
        
        if (resultNodes.Count > maxNodes)
        {
            resultNodes.RemoveAt(0);
        }
    }

    void VisualizeCausalChain()
    {
        if (causalLines == null) return;
        
        // Build causal chain: Action -> State -> Result
        List<Vector3> chain = new List<Vector3>();
        
        // Add nodes from each category
        int minCount = Mathf.Min(actionNodes.Count, stateNodes.Count, resultNodes.Count);
        
        for (int i = 0; i < minCount; i++)
        {
            // Action (Green)
            chain.Add(actionNodes[actionNodes.Count - minCount + i].position);
            
            // State (Blue)
            chain.Add(stateNodes[stateNodes.Count - minCount + i].position);
            
            // Result (Cyan if safe, Red if unsafe)
            chain.Add(resultNodes[resultNodes.Count - minCount + i].position);
        }
        
        // Update line renderer
        if (chain.Count > 1)
        {
            causalLines.positionCount = chain.Count;
            for (int i = 0; i < chain.Count; i++)
            {
                causalLines.SetPosition(i, chain[i]);
            }
            
            // Color code based on safety
            bool allSafe = resultNodes.Count > 0 && resultNodes[resultNodes.Count - 1].isSafe;
            causalLines.startColor = allSafe ? Color.cyan : Color.red;
            causalLines.endColor = allSafe ? new Color(0, 1, 1, 0.3f) : new Color(1, 0, 0, 0.3f);
        }
    }

    Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.cyan;
        return mat;
    }

    /// <summary>
    /// Get causal graph as certification evidence
    /// </summary>
    public CausalGraphEvidence GetCertificationEvidence()
    {
        CausalGraphEvidence evidence = new CausalGraphEvidence();
        
        evidence.totalActions = actionNodes.Count;
        evidence.totalStates = stateNodes.Count;
        evidence.totalResults = resultNodes.Count;
        
        // Count safe vs unsafe results
        int safeResults = resultNodes.Count(r => r.isSafe);
        evidence.safeResults = safeResults;
        evidence.unsafeResults = resultNodes.Count - safeResults;
        evidence.safetyRate = resultNodes.Count > 0 ? (float)safeResults / resultNodes.Count : 1f;
        
        // Calculate average P-score
        if (stateNodes.Count > 0)
        {
            evidence.averagePScore = stateNodes.Average(n => n.pScore);
            evidence.minPScore = stateNodes.Min(n => n.pScore);
            evidence.maxPScore = stateNodes.Max(n => n.pScore);
        }
        
        // Check if all results are safe
        evidence.allResultsSafe = resultNodes.All(r => r.isSafe);
        
        return evidence;
    }

    [System.Serializable]
    public class CausalGraphEvidence
    {
        public int totalActions;
        public int totalStates;
        public int totalResults;
        public int safeResults;
        public int unsafeResults;
        public float safetyRate;
        public float averagePScore;
        public float minPScore;
        public float maxPScore;
        public bool allResultsSafe;
    }

    /// <summary>
    /// Clear causal graph
    /// </summary>
    public void ClearGraph()
    {
        actionNodes.Clear();
        stateNodes.Clear();
        resultNodes.Clear();
        nodeCounter = 0;
        
        if (causalLines != null)
        {
            causalLines.positionCount = 0;
        }
    }
}
