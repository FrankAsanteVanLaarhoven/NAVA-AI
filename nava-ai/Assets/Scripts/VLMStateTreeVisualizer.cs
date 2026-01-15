using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// VLM State Tree Visualizer - 3D visualization of AI decision trees.
/// Unity 6.3 feature: Uses TreeView UI and Debug.DrawLine for VLM chain-of-thought visualization.
/// </summary>
public class VLMStateTreeVisualizer : MonoBehaviour
{
    [Header("Tree Configuration")]
    [Tooltip("Parent GameObject for tree nodes")]
    public GameObject debugNodeParent;

    [Tooltip("Node prefab (sphere)")]
    public GameObject nodePrefab;

    [Tooltip("Line material for connections")]
    public Material lineMaterial;

    [Tooltip("Tree status text display")]
    public Text treeStatusText;

    [Header("Visualization Settings")]
    [Tooltip("Node scale")]
    public float nodeScale = 0.5f;

    [Tooltip("Line width")]
    public float lineWidth = 0.1f;

    [Tooltip("Tree depth limit")]
    [Range(1, 10)]
    public int maxDepth = 5;

    [Tooltip("Auto-update tree")]
    public bool autoUpdate = true;

    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 1f;

    private List<TreeNode> treeNodes = new List<TreeNode>();
    private List<LineRenderer> treeLines = new List<LineRenderer>();
    private float lastUpdateTime = 0f;
    private bool isVisible = true;

    [System.Serializable]
    public class TreeNode
    {
        public string label;
        public string state;
        public Color color;
        public Vector3 position;
        public GameObject gameObject;
        public TreeNode parent;
        public List<TreeNode> children = new List<TreeNode>();
        public float confidence;
    }

    void Start()
    {
        // Create parent if not assigned
        if (debugNodeParent == null)
        {
            debugNodeParent = new GameObject("StateTreeParent");
            debugNodeParent.transform.SetParent(transform);
        }

        // Initialize tree
        InitializeTree();

        if (treeStatusText != null)
        {
            treeStatusText.text = "VLM TREE: READY";
        }
    }

    void Update()
    {
        if (autoUpdate && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateTree();
            lastUpdateTime = Time.time;
        }

        // Draw lines in Update (Debug.DrawLine only works in Scene view)
        DrawTreeLines();
    }

    void InitializeTree()
    {
        // Create root node
        TreeNode root = CreateNode("Decision", "Navigate to Target", Color.green, Vector3.zero, null);
        treeNodes.Add(root);

        // Create example child nodes
        TreeNode analysis = CreateNode("Analysis", "VLM Analysis: Path Clear", Color.blue, Vector3.forward, root);
        TreeNode safety = CreateNode("Safety", "Safety Check: Margin > 0.5", Color.cyan, Vector3.right, root);
        TreeNode constraint = CreateNode("Constraint", "Kitchen Door Open", Color.yellow, Vector3.back, root);

        treeNodes.Add(analysis);
        treeNodes.Add(safety);
        treeNodes.Add(constraint);

        root.children.Add(analysis);
        root.children.Add(safety);
        root.children.Add(constraint);
    }

    TreeNode CreateNode(string label, string state, Color color, Vector3 offset, TreeNode parent)
    {
        Vector3 position = transform.position + offset;

        // Create node GameObject
        GameObject nodeObj;
        if (nodePrefab != null)
        {
            nodeObj = Instantiate(nodePrefab, position, Quaternion.identity);
        }
        else
        {
            nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.transform.position = position;
            nodeObj.transform.localScale = Vector3.one * nodeScale;
        }

        nodeObj.name = $"Node_{label}";
        nodeObj.transform.SetParent(debugNodeParent.transform);

        // Set color
        Renderer renderer = nodeObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        // Add label text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(nodeObj.transform);
        labelObj.transform.localPosition = Vector3.up * 0.5f;
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.fontSize = 20;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;

        TreeNode node = new TreeNode
        {
            label = label,
            state = state,
            color = color,
            position = position,
            gameObject = nodeObj,
            parent = parent,
            confidence = Random.Range(0.5f, 1.0f) // Simulated confidence
        };

        return node;
    }

    void UpdateTree()
    {
        // In production, this would subscribe to VLM thought chain
        // For now, we simulate updates

        // Update node colors based on confidence
        foreach (TreeNode node in treeNodes)
        {
            if (node.gameObject != null)
            {
                Renderer renderer = node.gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Fade color based on confidence
                    Color baseColor = node.color;
                    Color fadedColor = Color.Lerp(Color.gray, baseColor, node.confidence);
                    renderer.material.color = fadedColor;
                }
            }
        }

        if (treeStatusText != null)
        {
            treeStatusText.text = $"VLM TREE: {treeNodes.Count} nodes | Depth: {GetTreeDepth()}";
        }
    }

    void DrawTreeLines()
    {
        // Draw lines between parent and children
        foreach (TreeNode node in treeNodes)
        {
            if (node.parent != null && node.gameObject != null && node.parent.gameObject != null)
            {
                Vector3 start = node.parent.gameObject.transform.position;
                Vector3 end = node.gameObject.transform.position;
                Color lineColor = Color.Lerp(node.parent.color, node.color, 0.5f);

                // Use Debug.DrawLine (visible in Scene view)
                Debug.DrawLine(start, end, lineColor);

                // Also create LineRenderer for Game view
                CreateLineRenderer(start, end, lineColor);
            }
        }
    }

    void CreateLineRenderer(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("TreeLine");
        lineObj.transform.SetParent(debugNodeParent.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial != null ? lineMaterial : CreateDefaultLineMaterial();
        lr.startColor = color;
        lr.endColor = color;

        treeLines.Add(lr);
    }

    Material CreateDefaultLineMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        return mat;
    }

    int GetTreeDepth()
    {
        int maxDepth = 0;
        foreach (TreeNode node in treeNodes)
        {
            int depth = GetNodeDepth(node);
            if (depth > maxDepth)
            {
                maxDepth = depth;
            }
        }
        return maxDepth;
    }

    int GetNodeDepth(TreeNode node)
    {
        int depth = 1;
        TreeNode current = node;
        while (current.parent != null)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }

    /// <summary>
    /// Add node to tree (for VLM integration)
    /// </summary>
    public void AddNode(string label, string state, Color color, Vector3 position, string parentLabel = null)
    {
        TreeNode parent = null;
        if (!string.IsNullOrEmpty(parentLabel))
        {
            parent = treeNodes.Find(n => n.label == parentLabel);
        }

        TreeNode newNode = CreateNode(label, state, color, position, parent);
        treeNodes.Add(newNode);

        if (parent != null)
        {
            parent.children.Add(newNode);
        }
    }

    /// <summary>
    /// Clear tree
    /// </summary>
    public void ClearTree()
    {
        foreach (TreeNode node in treeNodes)
        {
            if (node.gameObject != null)
            {
                Destroy(node.gameObject);
            }
        }

        foreach (LineRenderer lr in treeLines)
        {
            if (lr != null)
            {
                Destroy(lr.gameObject);
            }
        }

        treeNodes.Clear();
        treeLines.Clear();
    }

    /// <summary>
    /// Toggle tree visibility
    /// </summary>
    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        debugNodeParent.SetActive(isVisible);
    }

    /// <summary>
    /// Update node confidence (for VLM integration)
    /// </summary>
    public void UpdateNodeConfidence(string label, float confidence)
    {
        TreeNode node = treeNodes.Find(n => n.label == label);
        if (node != null)
        {
            node.confidence = Mathf.Clamp01(confidence);
        }
    }
}
