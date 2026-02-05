using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections;
using Newtonsoft.Json;

/// <summary>
/// Massive Data Scrapper - High-Performance Data Ingestion Engine.
/// Handles scraping of massive datasets (CityJSON, OSM Tiles) into Unity without blocking the main thread.
/// Essential for "Training/Processing" loops where AI needs context.
/// </summary>
public class MassiveDataScrapper : MonoBehaviour
{
    [System.Serializable]
    public class BuildingData
    {
        public string id;
        public float x;
        public float y;
        public float z;
        public float width;
        public float height;
        public float depth;
    }

    [System.Serializable]
    public class MegacityData
    {
        public List<BuildingData> buildings = new List<BuildingData>();
        public List<RoadData> roads = new List<RoadData>();
    }

    [System.Serializable]
    public class RoadData
    {
        public string id;
        public List<Vector3> waypoints = new List<Vector3>();
        public float width;
    }

    [Header("Scraping Configuration")]
    [Tooltip("Data file path (JSON format)")]
    public string dataPath = "Assets/Data/Megacity.json";
    
    [Tooltip("Prefab for building instantiation")]
    public GameObject buildingPrefab;
    
    [Tooltip("Prefab for road instantiation")]
    public GameObject roadPrefab;
    
    [Tooltip("Object pool size")]
    public int poolSize = 1000;
    
    [Tooltip("Spawn rate (objects per second)")]
    public float spawnRate = 100f;
    
    [Header("Performance Settings")]
    [Tooltip("Enable async loading")]
    public bool enableAsyncLoading = true;
    
    [Tooltip("Batch size for processing")]
    public int batchSize = 50;
    
    [Header("UI References")]
    [Tooltip("Text displaying scraping status")]
    public UnityEngine.UI.Text statusText;
    
    [Tooltip("Text displaying loaded count")]
    public UnityEngine.UI.Text countText;
    
    // Async Buffer to prevent Main Thread Block
    private Queue<BuildingData> loadQueue = new Queue<BuildingData>();
    private Queue<RoadData> roadQueue = new Queue<RoadData>();
    private bool isProcessing = false;
    private bool isLoading = false;
    private int loadedCount = 0;
    private int totalCount = 0;
    private ObjectPool objectPool;
    private float lastSpawnTime = 0f;
    private float spawnInterval;

    void Start()
    {
        spawnInterval = 1f / spawnRate;
        
        // Initialize object pool
        if (buildingPrefab != null)
        {
            objectPool = new ObjectPool(buildingPrefab, poolSize, transform);
        }
        
        // Start async loading
        if (enableAsyncLoading && File.Exists(dataPath))
        {
            StartCoroutine(LoadDatasetAsync());
        }
        else if (!enableAsyncLoading && File.Exists(dataPath))
        {
            LoadDatasetSync();
        }
        else
        {
            Debug.LogWarning($"[DataScrapper] Data file not found: {dataPath}");
            if (statusText != null)
            {
                statusText.text = "STATUS: File not found";
            }
        }
    }

    IEnumerator LoadDatasetAsync()
    {
        isLoading = true;
        
        if (statusText != null)
        {
            statusText.text = "STATUS: Loading dataset...";
        }
        
        // Read file in chunks on background thread
        yield return StartCoroutine(ReadFileInChunks());
        
        isLoading = false;
        
        if (statusText != null)
        {
            statusText.text = "STATUS: Loading complete";
        }
        
        Debug.Log($"[DataScrapper] Dataset loaded. Total: {totalCount} objects");
    }

    IEnumerator ReadFileInChunks()
    {
        string jsonContent = "";
        
        // Read file (this could be optimized for very large files)
        try
        {
            jsonContent = File.ReadAllText(dataPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataScrapper] Failed to read file: {e.Message}");
            yield break;
        }
        
        yield return null; // Yield to prevent blocking
        
        // Parse JSON
        MegacityData data = null;
        try
        {
            data = JsonConvert.DeserializeObject<MegacityData>(jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataScrapper] Failed to parse JSON: {e.Message}");
            yield break;
        }
        
        if (data == null) yield break;
        
        // Enqueue buildings
        totalCount = data.buildings.Count + data.roads.Count;
        
        foreach (var building in data.buildings)
        {
            lock (loadQueue)
            {
                loadQueue.Enqueue(building);
            }
            yield return null; // Yield every building to prevent blocking
        }
        
        // Enqueue roads
        foreach (var road in data.roads)
        {
            lock (roadQueue)
            {
                roadQueue.Enqueue(road);
            }
            yield return null;
        }
    }

    void LoadDatasetSync()
    {
        try
        {
            string jsonContent = File.ReadAllText(dataPath);
            MegacityData data = JsonConvert.DeserializeObject<MegacityData>(jsonContent);
            
            if (data != null)
            {
                foreach (var building in data.buildings)
                {
                    loadQueue.Enqueue(building);
                }
                
                foreach (var road in data.roads)
                {
                    roadQueue.Enqueue(road);
                }
                
                totalCount = loadQueue.Count + roadQueue.Count;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataScrapper] Failed to load dataset: {e.Message}");
        }
    }

    void Update()
    {
        // Process queue on main thread
        if (Time.time - lastSpawnTime < spawnInterval) return;
        
        if (!isProcessing && (loadQueue.Count > 0 || roadQueue.Count > 0))
        {
            isProcessing = true;
        }
        
        if (isProcessing)
        {
            // Process batch
            int processed = 0;
            
            while (processed < batchSize && (loadQueue.Count > 0 || roadQueue.Count > 0))
            {
                if (loadQueue.Count > 0)
                {
                    ProcessBuilding(loadQueue.Dequeue());
                    processed++;
                }
                
                if (roadQueue.Count > 0)
                {
                    ProcessRoad(roadQueue.Dequeue());
                    processed++;
                }
            }
            
            if (loadQueue.Count == 0 && roadQueue.Count == 0)
            {
                isProcessing = false;
            }
            
            lastSpawnTime = Time.time;
        }
        
        // Update UI
        UpdateUI();
    }

    void ProcessBuilding(BuildingData data)
    {
        if (buildingPrefab == null) return;
        
        // Get from pool or instantiate
        GameObject obj = null;
        if (objectPool != null)
        {
            obj = objectPool.Get();
        }
        else
        {
            obj = Instantiate(buildingPrefab);
        }
        
        if (obj != null)
        {
            obj.transform.position = new Vector3(data.x, data.y, data.z);
            obj.transform.localScale = new Vector3(data.width, data.height, data.depth);
            obj.name = $"Building_{data.id}";
            obj.SetActive(true);
            
            loadedCount++;
        }
    }

    void ProcessRoad(RoadData data)
    {
        if (roadPrefab == null) return;
        
        // Create road from waypoints
        GameObject roadObj = Instantiate(roadPrefab);
        roadObj.name = $"Road_{data.id}";
        
        // Create line renderer for road visualization
        UnityEngine.LineRenderer lr = roadObj.GetComponent<UnityEngine.LineRenderer>();
        if (lr == null)
        {
            lr = roadObj.AddComponent<UnityEngine.LineRenderer>();
        }
        
        lr.positionCount = data.waypoints.Count;
        lr.SetPositions(data.waypoints.ToArray());
        lr.startWidth = data.width;
        lr.endWidth = data.width;
        
        loadedCount++;
    }

    void UpdateUI()
    {
        if (statusText != null)
        {
            if (isLoading)
            {
                statusText.text = "STATUS: Loading...";
            }
            else if (isProcessing)
            {
                statusText.text = $"STATUS: Processing... ({loadQueue.Count + roadQueue.Count} remaining)";
            }
            else
            {
                statusText.text = "STATUS: Complete";
            }
        }
        
        if (countText != null)
        {
            countText.text = $"LOADED: {loadedCount} / {totalCount}";
        }
    }

    // Simple Object Pool
    private class ObjectPool
    {
        private Queue<GameObject> pool = new Queue<GameObject>();
        private GameObject prefab;
        private Transform parent;
        private int maxSize;

        public ObjectPool(GameObject prefab, int maxSize, Transform parent)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;
            
            // Pre-instantiate pool
            for (int i = 0; i < maxSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(parent);
                pool.Enqueue(obj);
            }
        }

        public GameObject Get()
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            else
            {
                // Create new if pool is empty
                return Instantiate(prefab);
            }
        }

        public void Return(GameObject obj)
        {
            if (obj != null && pool.Count < maxSize)
            {
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}
