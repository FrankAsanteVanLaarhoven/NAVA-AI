using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Global Voxel Map - Tesla-Style Occupancy Network (Voxel SLAM).
/// Provides a Global Map that updates as all agents move, creating a Shared World Model.
/// Matches Tesla's visual richness with GPU-accelerated voxel carving.
/// </summary>
public class GlobalVoxelMap : MonoBehaviour
{
    [Header("Voxel Map Settings")]
    [Tooltip("Compute shader for voxel carving")]
    public ComputeShader voxelShader;
    
    [Tooltip("Global map texture (3D)")]
    public RenderTexture globalMapTexture;
    
    [Tooltip("Voxel resolution (cubic)")]
    public int voxelRes = 256;
    
    [Tooltip("World size in Unity units")]
    public float worldSize = 50f;
    
    [Header("Raycast Settings")]
    [Tooltip("Number of rays per agent")]
    public int raysPerAgent = 6;
    
    [Tooltip("Ray distance")]
    public float rayDistance = 10.0f;
    
    [Tooltip("Ray directions (6 = cardinal + up/down)")]
    public Vector3[] rayDirections = new Vector3[]
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down
    };
    
    [Header("Agent Settings")]
    [Tooltip("Agent tag for auto-detection")]
    public string agentTag = "Agent";
    
    [Tooltip("Layer mask for raycast")]
    public LayerMask raycastLayerMask = -1;
    
    [Header("Visualization")]
    [Tooltip("Material for map visualization")]
    public Material mapMaterial;
    
    [Tooltip("Enable debug visualization")]
    public bool enableDebugViz = true;
    
    private List<GameObject> agents = new List<GameObject>();
    private ComputeBuffer pointBuffer;
    private List<Vector3> occupiedVoxels = new List<Vector3>();
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.1f; // Update every 100ms

    void Start()
    {
        // Initialize global map texture
        InitializeMapTexture();
        
        // Auto-detect agents
        RefreshAgentList();
        
        Debug.Log("[GlobalVoxelMap] Tesla-style occupancy network initialized");
    }

    void InitializeMapTexture()
    {
        if (globalMapTexture == null)
        {
            globalMapTexture = new RenderTexture(voxelRes, voxelRes, 0, RenderTextureFormat.ARGBFloat);
            globalMapTexture.enableRandomWrite = true;
            globalMapTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            globalMapTexture.volumeDepth = voxelRes;
            globalMapTexture.Create();
        }
        
        // Create debug material if needed
        if (mapMaterial == null && enableDebugViz)
        {
            mapMaterial = new Material(Shader.Find("Standard"));
            mapMaterial.SetTexture("_MainTex", globalMapTexture);
        }
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
        
        // Update global map
        UpdateGlobalMap();
        
        lastUpdateTime = Time.time;
    }

    void RefreshAgentList()
    {
        GameObject[] foundAgents = GameObject.FindGameObjectsWithTag(agentTag);
        agents.Clear();
        agents.AddRange(foundAgents);
    }

    void UpdateGlobalMap()
    {
        if (agents.Count == 0) return;
        
        // Collect all hit points from all agents
        List<Vector3> hitPoints = new List<Vector3>();
        
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            
            // Cast rays in all directions
            for (int i = 0; i < raysPerAgent && i < rayDirections.Length; i++)
            {
                Vector3 direction = agent.transform.TransformDirection(rayDirections[i]);
                Ray ray = new Ray(agent.transform.position, direction);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, rayDistance, raycastLayerMask))
                {
                    // 2. Carve Voxel (Tesla Style)
                    Vector3 voxelIdx = WorldToVoxel(hit.point);
                    hitPoints.Add(voxelIdx);
                    
                    // Mark as Occupied
                    MarkVoxel(voxelIdx, 1);
                }
            }
        }
        
        // Update GPU buffer if using compute shader
        if (voxelShader != null && hitPoints.Count > 0)
        {
            UpdateComputeShader(hitPoints);
        }
    }

    Vector3 WorldToVoxel(Vector3 worldPos)
    {
        // Convert world position to voxel index
        // Normalize to [0, 1] range
        Vector3 normalized = (worldPos + Vector3.one * worldSize / 2f) / worldSize;
        
        // Clamp to valid range
        normalized.x = Mathf.Clamp01(normalized.x);
        normalized.y = Mathf.Clamp01(normalized.y);
        normalized.z = Mathf.Clamp01(normalized.z);
        
        // Convert to voxel index
        return new Vector3(
            normalized.x * voxelRes,
            normalized.y * voxelRes,
            normalized.z * voxelRes
        );
    }

    void MarkVoxel(Vector3 voxelIdx, int state)
    {
        // Store occupied voxel
        Vector3Int intIdx = new Vector3Int(
            Mathf.FloorToInt(voxelIdx.x),
            Mathf.FloorToInt(voxelIdx.y),
            Mathf.FloorToInt(voxelIdx.z)
        );
        
        // Add to occupied list (for visualization)
        if (!occupiedVoxels.Contains(intIdx))
        {
            occupiedVoxels.Add(intIdx);
            
            // Limit list size for performance
            if (occupiedVoxels.Count > 10000)
            {
                occupiedVoxels.RemoveAt(0);
            }
        }
    }

    void UpdateComputeShader(List<Vector3> points)
    {
        if (voxelShader == null || globalMapTexture == null) return;
        
        try
        {
            // Create point buffer
            if (pointBuffer == null || pointBuffer.count != points.Count)
            {
                if (pointBuffer != null) pointBuffer.Release();
                pointBuffer = new ComputeBuffer(points.Count, sizeof(float) * 3);
            }
            
            // Convert to float array
            float[] pointData = new float[points.Count * 3];
            for (int i = 0; i < points.Count; i++)
            {
                pointData[i * 3] = points[i].x;
                pointData[i * 3 + 1] = points[i].y;
                pointData[i * 3 + 2] = points[i].z;
            }
            
            pointBuffer.SetData(pointData);
            
            // Dispatch compute shader
            int kernelIndex = voxelShader.FindKernel("VoxelCarve");
            if (kernelIndex >= 0)
            {
                voxelShader.SetBuffer(kernelIndex, "PointCloud", pointBuffer);
                voxelShader.SetTexture(kernelIndex, "VoxelGrid", globalMapTexture);
                voxelShader.SetFloat("WorldSize", worldSize);
                voxelShader.SetInt("VoxelResolution", voxelRes);
                
                int threadGroups = Mathf.CeilToInt(points.Count / 64f);
                voxelShader.Dispatch(kernelIndex, threadGroups, 1, 1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GlobalVoxelMap] Compute shader update failed: {e.Message}");
        }
    }

    void OnDestroy()
    {
        // Cleanup compute buffer
        if (pointBuffer != null)
        {
            pointBuffer.Release();
            pointBuffer = null;
        }
    }

    /// <summary>
    /// Check if a position is occupied in the global map
    /// </summary>
    public bool IsPositionOccupied(Vector3 worldPos)
    {
        Vector3 voxelIdx = WorldToVoxel(worldPos);
        Vector3Int intIdx = new Vector3Int(
            Mathf.FloorToInt(voxelIdx.x),
            Mathf.FloorToInt(voxelIdx.y),
            Mathf.FloorToInt(voxelIdx.z)
        );
        
        return occupiedVoxels.Contains(intIdx);
    }

    /// <summary>
    /// Get global map texture for visualization
    /// </summary>
    public RenderTexture GetGlobalMapTexture()
    {
        return globalMapTexture;
    }

    /// <summary>
    /// Get number of occupied voxels
    /// </summary>
    public int GetOccupiedVoxelCount()
    {
        return occupiedVoxels.Count;
    }
}
