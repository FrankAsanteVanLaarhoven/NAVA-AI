using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Mcity Map Loader - Connects to Map APIs (OpenStreetMap/CityJSON) to build realistic 3D environments.
/// Creates "Metacity" instead of flat grids for real-world context.
/// </summary>
public class McityMapLoader : MonoBehaviour
{
    [System.Serializable]
    public class MapBuildingData
    {
        public float lat;
        public float lon;
        public float width;
        public float height;
        public float depth;
        public string buildingType;
    }

    [Header("Map Data Source")]
    [Tooltip("Map data API endpoint (OpenStreetMap, CityJSON, etc.)")]
    public string mapDataAPI = "https://api.openstreetmap.org/api/0.6/map";
    
    [Tooltip("Use local file instead of API (for stability)")]
    public bool useLocalFile = true;
    
    [Tooltip("Local map data file path")]
    public string localMapFilePath = "map_data.json";
    
    [Header("Materials")]
    [Tooltip("Material for buildings")]
    public Material buildingMaterial;
    
    [Tooltip("Material for roads")]
    public Material roadMaterial;
    
    [Tooltip("Material for terrain")]
    public Material terrainMaterial;
    
    [Header("Building Settings")]
    [Tooltip("Height scale multiplier for buildings")]
    public float buildingHeightScale = 10.0f;
    
    [Tooltip("Default building width if not specified")]
    public float defaultBuildingWidth = 5f;
    
    [Tooltip("Default building depth if not specified")]
    public float defaultBuildingDepth = 5f;
    
    [Header("Coordinate System")]
    [Tooltip("Origin latitude (center of map)")]
    public float originLat = 54.9783f; // Newcastle, UK
    
    [Tooltip("Origin longitude (center of map)")]
    public float originLon = -1.6178f;
    
    [Tooltip("Map scale (meters per degree)")]
    public float mapScale = 111320f; // Approximate meters per degree at equator
    
    [Header("Performance")]
    [Tooltip("Buildings per frame (for async loading)")]
    public int buildingsPerFrame = 5;
    
    private List<GameObject> createdBuildings = new List<GameObject>();
    private List<GameObject> createdRoads = new List<GameObject>();
    private Transform buildingsParent;
    private Transform roadsParent;

    void Start()
    {
        // Create parent objects
        buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.SetParent(transform);
        
        roadsParent = new GameObject("Roads").transform;
        roadsParent.SetParent(transform);
        
        // Start loading map
        StartCoroutine(LoadMapData());
    }

    IEnumerator LoadMapData()
    {
        List<MapBuildingData> cityData;
        
        if (useLocalFile)
        {
            cityData = LoadLocalMapData();
        }
        else
        {
            yield return StartCoroutine(FetchMapDataFromAPI());
            cityData = ParseMapData(); // Would parse from API response
        }
        
        if (cityData == null || cityData.Count == 0)
        {
            // Generate sample data for demonstration
            cityData = GenerateSampleCityData();
        }
        
        // Generate 3D meshes
        yield return StartCoroutine(BuildCityMeshes(cityData));
        
        Debug.Log($"[McityMapLoader] Loaded {cityData.Count} buildings");
    }

    IEnumerator FetchMapDataFromAPI()
    {
        // In production, use UnityWebRequest to fetch from API
        // For now, this is a placeholder
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(mapDataAPI))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonData = request.downloadHandler.text;
                // Parse JSON response
                Debug.Log("[McityMapLoader] Fetched map data from API");
            }
            else
            {
                Debug.LogWarning($"[McityMapLoader] API request failed: {request.error}");
            }
        }
    }

    List<MapBuildingData> LoadLocalMapData()
    {
        List<string> possiblePaths = new List<string> {
            localMapFilePath,
            Path.Combine(Application.persistentDataPath, localMapFilePath)
        };
        
        // Safely try to construct path from Application.dataPath
        try
        {
            string dataPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", localMapFilePath));
            possiblePaths.Insert(1, dataPath);
        }
        catch
        {
            // If path construction fails, skip this option
        }
        
        foreach (string path in possiblePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        MapDataContainer container = JsonUtility.FromJson<MapDataContainer>(json);
                        return container.buildings;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[McityMapLoader] Failed to load local file: {e.Message}");
                        continue;
                    }
                }
            }
            catch
            {
                // Skip invalid paths
                continue;
            }
        }
        
        return null;
    }

    List<MapBuildingData> ParseMapData()
    {
        // Placeholder - would parse actual API response format
        return new List<MapBuildingData>();
    }

    List<MapBuildingData> GenerateSampleCityData()
    {
        // Generate sample city layout for demonstration
        List<MapBuildingData> data = new List<MapBuildingData>();
        
        // Create a grid of buildings
        for (int x = -5; x <= 5; x++)
        {
            for (int z = -5; z <= 5; z++)
            {
                if (x == 0 && z == 0) continue; // Leave center empty
                
                data.Add(new MapBuildingData
                {
                    lat = originLat + (x * 0.001f),
                    lon = originLon + (z * 0.001f),
                    width = Random.Range(3f, 8f),
                    height = Random.Range(1f, 5f),
                    depth = Random.Range(3f, 8f),
                    buildingType = "residential"
                });
            }
        }
        
        return data;
    }

    IEnumerator BuildCityMeshes(List<MapBuildingData> cityData)
    {
        int processed = 0;
        
        foreach (var building in cityData)
        {
            CreateBuildingMesh(building);
            processed++;
            
            // Yield every N buildings to avoid freezing
            if (processed % buildingsPerFrame == 0)
            {
                yield return null;
            }
        }
    }

    void CreateBuildingMesh(MapBuildingData data)
    {
        // Convert geographic coordinates to Unity world space
        Vector3 pos = ConvertGeoToUnity(data.lat, data.lon);
        
        // Create building GameObject
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = $"Building_{data.lat:F4}_{data.lon:F4}";
        building.transform.SetParent(buildingsParent);
        building.transform.position = pos;
        
        // Set scale
        float width = data.width > 0 ? data.width : defaultBuildingWidth;
        float depth = data.depth > 0 ? data.depth : defaultBuildingDepth;
        float height = data.height > 0 ? data.height * buildingHeightScale : buildingHeightScale;
        
        building.transform.localScale = new Vector3(width, height, depth);
        
        // Apply material
        Renderer renderer = building.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = buildingMaterial != null ? buildingMaterial : CreateDefaultBuildingMaterial();
        }
        
        // Add collider for physics
        Collider collider = building.GetComponent<Collider>();
        if (collider == null)
        {
            building.AddComponent<BoxCollider>();
        }
        
        createdBuildings.Add(building);
    }

    Vector3 ConvertGeoToUnity(float lat, float lon)
    {
        // Mercator projection (simplified)
        // Convert lat/lon to local coordinates relative to origin
        
        float deltaLat = lat - originLat;
        float deltaLon = lon - originLon;
        
        // Convert to meters (approximate)
        float x = deltaLon * mapScale * Mathf.Cos(originLat * Mathf.Deg2Rad);
        float z = deltaLat * mapScale;
        
        // Unity uses Y-up, so Z is forward
        return new Vector3(x, 0, z);
    }

    Material CreateDefaultBuildingMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.7f, 0.7f, 0.7f);
        return mat;
    }

    /// <summary>
    /// Save current map data to file
    /// </summary>
    public void SaveMapData(List<MapBuildingData> data)
    {
        MapDataContainer container = new MapDataContainer { buildings = data };
        string json = JsonUtility.ToJson(container, true);
        
        string path = Path.Combine(Application.persistentDataPath, localMapFilePath);
        File.WriteAllText(path, json);
        
        Debug.Log($"[McityMapLoader] Saved map data to {path}");
    }

    /// <summary>
    /// Clear all generated buildings
    /// </summary>
    public void ClearMap()
    {
        foreach (GameObject building in createdBuildings)
        {
            if (building != null) Destroy(building);
        }
        createdBuildings.Clear();
        
        foreach (GameObject road in createdRoads)
        {
            if (road != null) Destroy(road);
        }
        createdRoads.Clear();
    }

    [System.Serializable]
    class MapDataContainer
    {
        public List<MapBuildingData> buildings;
    }
}
