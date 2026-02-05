using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Real-Time Voxel SLAM - Builds a 3D voxel-based mesh in real-time as the robot explores.
/// Allows inspection of exact geometry to verify perception matches reality.
/// </summary>
public class VoxelMapBuilder : MonoBehaviour
{
    [Header("Voxel Settings")]
    [Tooltip("Voxel grid resolution (power of 2 recommended)")]
    public int voxelResolution = 256;
    
    [Tooltip("Voxel size in meters (10cm = 0.1)")]
    public float voxelSize = 0.1f;
    
    [Tooltip("World bounds for voxel grid")]
    public Vector3 worldBounds = new Vector3(20f, 10f, 20f);
    
    [Header("Visualization")]
    [Tooltip("Material for rendering voxel mesh")]
    public Material voxelMaterial;
    
    [Tooltip("GameObject to render voxel visualization")]
    public GameObject voxelVisualization;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for point cloud data")]
    public string pointCloudTopic = "/camera/depth/points";
    
    [Header("Performance")]
    [Tooltip("Maximum points to process per frame")]
    public int maxPointsPerFrame = 10000;
    
    [Tooltip("Update rate throttling (Hz)")]
    public float updateRate = 10f;
    
    private ROSConnection ros;
    private ComputeShader voxelShader;
    private RenderTexture voxelTexture3D;
    private ComputeBuffer pointBuffer;
    private ComputeBuffer voxelBuffer;
    private Mesh voxelMesh;
    private List<Vector3> voxelPositions = new List<Vector3>();
    private float lastUpdateTime = 0f;
    private float updateInterval;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointCloud2Msg>(pointCloudTopic, OnPointCloudReceived);
        
        updateInterval = 1f / updateRate;
        
        // Load or create compute shader
        voxelShader = Resources.Load<ComputeShader>("VoxelCarve");
        if (voxelShader == null)
        {
            Debug.LogWarning("[VoxelMapBuilder] VoxelCarve.compute not found. Creating fallback implementation.");
        }
        
        // Initialize voxel grid
        InitializeVoxelGrid();
        
        // Create visualization object if not assigned
        if (voxelVisualization == null)
        {
            voxelVisualization = new GameObject("VoxelVisualization");
            voxelVisualization.transform.SetParent(transform);
            voxelVisualization.transform.localPosition = Vector3.zero;
        }
        
        Debug.Log($"[VoxelMapBuilder] Initialized {voxelResolution}x{voxelResolution}x{voxelResolution} voxel grid");
    }

    void InitializeVoxelGrid()
    {
        // Initialize 3D texture for voxel storage
        voxelTexture3D = new RenderTexture(voxelResolution, voxelResolution, 0, RenderTextureFormat.ARGBFloat);
        voxelTexture3D.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        voxelTexture3D.volumeDepth = voxelResolution;
        voxelTexture3D.enableRandomWrite = true;
        voxelTexture3D.Create();
        
        // Initialize compute buffers
        int maxPoints = maxPointsPerFrame;
        pointBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Structured);
        voxelBuffer = new ComputeBuffer(voxelResolution * voxelResolution * voxelResolution, sizeof(float), ComputeBufferType.Structured);
        
        // Create mesh for visualization
        voxelMesh = new Mesh();
        voxelMesh.name = "VoxelMesh";
    }

    void OnPointCloudReceived(PointCloud2Msg msg)
    {
        // Throttle updates
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        ProcessPointCloud(msg);
    }

    void ProcessPointCloud(PointCloud2Msg msg)
    {
        if (voxelShader == null)
        {
            // Fallback: CPU-based voxel carving
            ProcessPointCloudCPU(msg);
            return;
        }
        
        // Extract points from PointCloud2 message
        List<Vector3> points = ExtractPointsFromPointCloud(msg);
        if (points.Count == 0) return;
        
        int pointCount = Mathf.Min(points.Count, maxPointsPerFrame);
        
        // Convert to array for compute buffer
        Vector3[] pointArray = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            pointArray[i] = points[i];
        }
        
        // Update compute buffer
        pointBuffer.SetData(pointArray);
        
        // Dispatch compute shader
        int kernelIndex = voxelShader.FindKernel("VoxelCarve");
        voxelShader.SetBuffer(kernelIndex, "PointCloud", pointBuffer);
        voxelShader.SetTexture(kernelIndex, "VoxelGrid", voxelTexture3D);
        voxelShader.SetInt("PointCount", pointCount);
        voxelShader.SetFloat("VoxelSize", voxelSize);
        voxelShader.SetVector("WorldBounds", worldBounds);
        voxelShader.SetInt("Resolution", voxelResolution);
        
        int threadGroups = Mathf.CeilToInt(pointCount / 64f);
        voxelShader.Dispatch(kernelIndex, threadGroups, 1, 1);
        
        // Update visualization
        UpdateVisualization();
    }

    void ProcessPointCloudCPU(PointCloud2Msg msg)
    {
        // CPU fallback implementation
        List<Vector3> points = ExtractPointsFromPointCloud(msg);
        
        foreach (Vector3 point in points)
        {
            // Convert world position to voxel index
            Vector3 localPos = point - transform.position;
            int3 voxelIdx = WorldToVoxelIndex(localPos);
            
            // Check bounds
            if (voxelIdx.x >= 0 && voxelIdx.x < voxelResolution &&
                voxelIdx.y >= 0 && voxelIdx.y < voxelResolution &&
                voxelIdx.z >= 0 && voxelIdx.z < voxelResolution)
            {
                // Mark voxel as occupied
                Vector3 voxelWorldPos = VoxelIndexToWorld(voxelIdx);
                if (!voxelPositions.Contains(voxelWorldPos))
                {
                    voxelPositions.Add(voxelWorldPos);
                }
            }
        }
        
        UpdateVisualization();
    }

    List<Vector3> ExtractPointsFromPointCloud(PointCloud2Msg msg)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Find field indices
        int xIndex = -1, yIndex = -1, zIndex = -1;
        int pointStep = (int)msg.point_step;
        
        for (int i = 0; i < msg.fields.Length; i++)
        {
            string fieldName = msg.fields[i].name.ToLower();
            if (fieldName == "x") xIndex = (int)msg.fields[i].offset;
            else if (fieldName == "y") yIndex = (int)msg.fields[i].offset;
            else if (fieldName == "z") zIndex = (int)msg.fields[i].offset;
        }
        
        if (xIndex < 0 || yIndex < 0 || zIndex < 0) return points;
        
        // Extract points
        int pointCount = Mathf.Min((int)(msg.width * msg.height), maxPointsPerFrame);
        
        for (int i = 0; i < pointCount; i++)
        {
            int dataOffset = i * pointStep;
            if (dataOffset + zIndex + 4 > msg.data.Length) break;
            
            float x = System.BitConverter.ToSingle(msg.data, dataOffset + xIndex);
            float y = System.BitConverter.ToSingle(msg.data, dataOffset + yIndex);
            float z = System.BitConverter.ToSingle(msg.data, dataOffset + zIndex);
            
            // ROS Z-up to Unity Y-up
            Vector3 position = new Vector3(x, z, -y);
            points.Add(position);
        }
        
        return points;
    }

    struct int3
    {
        public int x, y, z;
        public int3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    int3 WorldToVoxelIndex(Vector3 worldPos)
    {
        Vector3 localPos = worldPos + worldBounds * 0.5f;
        int x = Mathf.FloorToInt(localPos.x / voxelSize);
        int y = Mathf.FloorToInt(localPos.y / voxelSize);
        int z = Mathf.FloorToInt(localPos.z / voxelSize);
        return new int3(x, y, z);
    }

    Vector3 VoxelIndexToWorld(int3 voxelIdx)
    {
        Vector3 localPos = new Vector3(
            voxelIdx.x * voxelSize,
            voxelIdx.y * voxelSize,
            voxelIdx.z * voxelSize
        );
        return localPos - worldBounds * 0.5f;
    }

    void UpdateVisualization()
    {
        if (voxelVisualization == null || voxelPositions.Count == 0) return;
        
        // Create mesh from voxel positions
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        
        foreach (Vector3 pos in voxelPositions)
        {
            int baseIndex = vertices.Count;
            float halfSize = voxelSize * 0.5f;
            
            // Create cube vertices
            vertices.Add(pos + new Vector3(-halfSize, -halfSize, -halfSize));
            vertices.Add(pos + new Vector3(halfSize, -halfSize, -halfSize));
            vertices.Add(pos + new Vector3(halfSize, halfSize, -halfSize));
            vertices.Add(pos + new Vector3(-halfSize, halfSize, -halfSize));
            vertices.Add(pos + new Vector3(-halfSize, -halfSize, halfSize));
            vertices.Add(pos + new Vector3(halfSize, -halfSize, halfSize));
            vertices.Add(pos + new Vector3(halfSize, halfSize, halfSize));
            vertices.Add(pos + new Vector3(-halfSize, halfSize, halfSize));
            
            // Create cube faces
            int[] cubeIndices = new int[]
            {
                0, 1, 2, 0, 2, 3, // front
                4, 7, 6, 4, 6, 5, // back
                0, 4, 5, 0, 5, 1, // bottom
                2, 6, 7, 2, 7, 3, // top
                0, 3, 7, 0, 7, 4, // left
                1, 5, 6, 1, 6, 2  // right
            };
            
            foreach (int idx in cubeIndices)
            {
                indices.Add(baseIndex + idx);
            }
        }
        
        voxelMesh.Clear();
        voxelMesh.vertices = vertices.ToArray();
        voxelMesh.triangles = indices.ToArray();
        voxelMesh.RecalculateNormals();
        
        // Update mesh renderer
        MeshFilter meshFilter = voxelVisualization.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = voxelVisualization.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = voxelMesh;
        
        MeshRenderer meshRenderer = voxelVisualization.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = voxelVisualization.AddComponent<MeshRenderer>();
        }
        if (voxelMaterial != null)
        {
            meshRenderer.material = voxelMaterial;
        }
    }

    void OnDestroy()
    {
        if (pointBuffer != null) pointBuffer.Release();
        if (voxelBuffer != null) voxelBuffer.Release();
        if (voxelTexture3D != null) voxelTexture3D.Release();
    }
}
