using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// World Editor / Scenario Builder - "God Mode" for creating test scenarios.
/// Allows click-and-drag placement of obstacles for reproducible testing.
/// </summary>
public class WorldEditor : EditorWindow
{
    [MenuItem("NAVA-AI Dashboard/World Editor")]
    static void ShowWindow()
    {
        GetWindow<WorldEditor>("Scenario Builder");
    }

    private enum ObstacleType
    {
        Wall,
        Pedestrian,
        Box,
        Cone,
        Door
    }

    private ObstacleType selectedObstacleType = ObstacleType.Wall;
    private GameObject wallPrefab;
    private GameObject pedestrianPrefab;
    private GameObject boxPrefab;
    private GameObject conePrefab;
    private GameObject doorPrefab;
    
    private List<GameObject> placedObstacles = new List<GameObject>();
    private bool isPlacing = false;

    void OnGUI()
    {
        GUILayout.Label("Scenario Builder - World Editor", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Click 'Place' buttons to add obstacles.\n" +
            "Then click in Scene View to place them.\n" +
            "Use this to create reproducible test scenarios.",
            MessageType.Info);

        GUILayout.Space(10);

        // Obstacle type selection
        EditorGUILayout.LabelField("Obstacle Type", EditorStyles.boldLabel);
        selectedObstacleType = (ObstacleType)EditorGUILayout.EnumPopup("Type", selectedObstacleType);

        GUILayout.Space(10);

        // Prefab assignment
        EditorGUILayout.LabelField("Prefab Assignments", EditorStyles.boldLabel);
        wallPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", wallPrefab, typeof(GameObject), false);
        pedestrianPrefab = (GameObject)EditorGUILayout.ObjectField("Pedestrian Prefab", pedestrianPrefab, typeof(GameObject), false);
        boxPrefab = (GameObject)EditorGUILayout.ObjectField("Box Prefab", boxPrefab, typeof(GameObject), false);
        conePrefab = (GameObject)EditorGUILayout.ObjectField("Cone Prefab", conePrefab, typeof(GameObject), false);
        doorPrefab = (GameObject)EditorGUILayout.ObjectField("Door Prefab", doorPrefab, typeof(GameObject), false);

        GUILayout.Space(10);

        // Place buttons
        EditorGUILayout.LabelField("Place Obstacles", EditorStyles.boldLabel);
        if (GUILayout.Button("Place Wall", GUILayout.Height(30)))
        {
            PlaceObstacle(ObstacleType.Wall);
        }
        if (GUILayout.Button("Place Pedestrian", GUILayout.Height(30)))
        {
            PlaceObstacle(ObstacleType.Pedestrian);
        }
        if (GUILayout.Button("Place Box", GUILayout.Height(30)))
        {
            PlaceObstacle(ObstacleType.Box);
        }
        if (GUILayout.Button("Place Cone", GUILayout.Height(30)))
        {
            PlaceObstacle(ObstacleType.Cone);
        }
        if (GUILayout.Button("Place Door", GUILayout.Height(30)))
        {
            PlaceObstacle(ObstacleType.Door);
        }

        GUILayout.Space(10);

        // Management
        EditorGUILayout.LabelField("Obstacle Management", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Placed Obstacles: {placedObstacles.Count}");
        
        if (GUILayout.Button("Clear All Obstacles", GUILayout.Height(25)))
        {
            ClearAllObstacles();
        }
        
        if (GUILayout.Button("Save Scenario", GUILayout.Height(25)))
        {
            SaveScenario();
        }
        
        if (GUILayout.Button("Load Scenario", GUILayout.Height(25)))
        {
            LoadScenario();
        }
    }

    void PlaceObstacle(ObstacleType type)
    {
        isPlacing = true;
        
        // Get prefab for this type
        GameObject prefab = GetPrefabForType(type);
        
        if (prefab == null)
        {
            // Create primitive if no prefab assigned
            prefab = CreatePrimitiveObstacle(type);
        }
        
        // Create obstacle at origin (user will position it)
        GameObject obstacle = Instantiate(prefab);
        obstacle.name = $"{type}_{placedObstacles.Count + 1}";
        obstacle.transform.position = Vector3.zero;
        
        placedObstacles.Add(obstacle);
        
        // Select it so user can move it
        Selection.activeGameObject = obstacle;
        
        Debug.Log($"[WorldEditor] Placed {type} obstacle. Position it in Scene View.");
    }

    GameObject GetPrefabForType(ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.Wall: return wallPrefab;
            case ObstacleType.Pedestrian: return pedestrianPrefab;
            case ObstacleType.Box: return boxPrefab;
            case ObstacleType.Cone: return conePrefab;
            case ObstacleType.Door: return doorPrefab;
            default: return null;
        }
    }

    GameObject CreatePrimitiveObstacle(ObstacleType type)
    {
        GameObject obj = null;
        
        switch (type)
        {
            case ObstacleType.Wall:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = new Vector3(2f, 2f, 0.2f);
                obj.name = "Wall";
                break;
            case ObstacleType.Pedestrian:
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                obj.name = "Pedestrian";
                break;
            case ObstacleType.Box:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = Vector3.one;
                obj.name = "Box";
                break;
            case ObstacleType.Cone:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                obj.name = "Cone";
                break;
            case ObstacleType.Door:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = new Vector3(1f, 2f, 0.1f);
                obj.name = "Door";
                break;
        }
        
        // Add collider if not present
        if (obj != null && obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
        }
        
        return obj;
    }

    void ClearAllObstacles()
    {
        foreach (var obstacle in placedObstacles)
        {
            if (obstacle != null)
            {
                DestroyImmediate(obstacle);
            }
        }
        placedObstacles.Clear();
        Debug.Log("[WorldEditor] Cleared all obstacles");
    }

    void SaveScenario()
    {
        // Save obstacle positions to a scriptable object or JSON
        // This is a simplified version - you can enhance it
        string path = EditorUtility.SaveFilePanel("Save Scenario", "", "scenario", "json");
        if (string.IsNullOrEmpty(path)) return;
        
        // Create scenario data
        List<ObstacleData> data = new List<ObstacleData>();
        foreach (var obstacle in placedObstacles)
        {
            if (obstacle != null)
            {
                data.Add(new ObstacleData
                {
                    name = obstacle.name,
                    position = obstacle.transform.position,
                    rotation = obstacle.transform.rotation,
                    scale = obstacle.transform.localScale
                });
            }
        }
        
        string json = JsonUtility.ToJson(new ScenarioData { obstacles = data }, true);
        System.IO.File.WriteAllText(path, json);
        
        Debug.Log($"[WorldEditor] Saved scenario to {path}");
    }

    void LoadScenario()
    {
        string path = EditorUtility.OpenFilePanel("Load Scenario", "", "json");
        if (string.IsNullOrEmpty(path)) return;
        
        if (!System.IO.File.Exists(path)) return;
        
        string json = System.IO.File.ReadAllText(path);
        ScenarioData data = JsonUtility.FromJson<ScenarioData>(json);
        
        ClearAllObstacles();
        
        foreach (var obstacleData in data.obstacles)
        {
            GameObject obj = GameObject.Find(obstacleData.name);
            if (obj == null)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = obstacleData.name;
            }
            
            obj.transform.position = obstacleData.position;
            obj.transform.rotation = obstacleData.rotation;
            obj.transform.localScale = obstacleData.scale;
            
            placedObstacles.Add(obj);
        }
        
        Debug.Log($"[WorldEditor] Loaded scenario from {path}");
    }

    [System.Serializable]
    class ObstacleData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    class ScenarioData
    {
        public List<ObstacleData> obstacles;
    }
}
