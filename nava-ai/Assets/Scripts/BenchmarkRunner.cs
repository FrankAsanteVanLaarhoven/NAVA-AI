using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Automated Benchmark Suite - JUnit-style regression testing for autonomous navigation.
/// Runs scenarios automatically and generates benchmark reports.
/// </summary>
public class BenchmarkRunner : MonoBehaviour
{
    [System.Serializable]
    public class Scenario
    {
        public string name = "Scenario";
        public Vector3[] obstaclePositions;
        public Vector3 robotStart;
        public Vector3 robotGoal;
        public float timeout = 60f;
        public float successRadius = 0.5f;
    }

    [System.Serializable]
    public class BenchmarkResult
    {
        public int runNumber;
        public string scenarioName;
        public bool success;
        public bool crashed;
        public float minMargin;
        public float averageLatency;
        public float maxLatency;
        public float completionTime;
        public float distanceTraveled;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class BenchmarkReport
    {
        public string version = "NAVΛ-Bench v1.0";
        public System.DateTime startTime;
        public System.DateTime endTime;
        public int totalRuns;
        public int successfulRuns;
        public int crashedRuns;
        public float successRate;
        public float averageMinMargin;
        public float averageLatency;
        public List<BenchmarkResult> results = new List<BenchmarkResult>();
    }

    [Header("Scenario Settings")]
    [Tooltip("Scenario file path (JSON)")]
    public string scenarioFilePath = "scenarios/standard_set_a.json";
    
    [Tooltip("Number of times to repeat each scenario")]
    public int scenarioRepeats = 10;
    
    [Tooltip("Default scenario if file not found")]
    public Scenario defaultScenario;

    [Header("UI References")]
    [Tooltip("Text displaying benchmark status")]
    public Text statusText;
    
    [Tooltip("Text displaying current run statistics")]
    public Text statsText;

    [Header("Robot References")]
    [Tooltip("Robot GameObject to control")]
    public GameObject robot;
    
    [Tooltip("Reference to ROS2DashboardManager for margin data")]
    public ROS2DashboardManager dashboardManager;
    
    [Tooltip("Reference to LatencyProfiler for timing data")]
    public LatencyProfiler latencyProfiler;

    [Header("Obstacle Management")]
    [Tooltip("Prefab for obstacles")]
    public GameObject obstaclePrefab;
    
    [Tooltip("Parent for spawned obstacles")]
    public Transform obstacleParent;

    private int currentRun = 0;
    private Scenario currentScenario;
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private BenchmarkReport report = new BenchmarkReport();
    private bool isRunning = false;
    private Vector3 robotStartPosition;
    private float scenarioStartTime;
    private float totalDistanceTraveled = 0f;
    private Vector3 lastRobotPosition;

    void Start()
    {
        // Load scenario
        LoadScenario();
        
        // Initialize report
        report.startTime = System.DateTime.Now;
        report.version = "NAVΛ-Bench v1.0";
        
        // Store robot start position
        if (robot != null)
        {
            robotStartPosition = robot.transform.position;
            lastRobotPosition = robotStartPosition;
        }
        
        Debug.Log("[BenchmarkRunner] Initialized - Ready to run benchmarks");
    }

    void LoadScenario()
    {
        // Try to load from file
        List<string> possiblePaths = new List<string> {
            scenarioFilePath,
            Path.Combine(Application.persistentDataPath, scenarioFilePath)
        };
        
        // Safely try to construct path from Application.dataPath
        try
        {
            string dataPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenarioFilePath));
            possiblePaths.Insert(1, dataPath);
        }
        catch
        {
            // If path construction fails, skip this option
        }
        
        string filePath = null;
        foreach (string path in possiblePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }
            catch
            {
                // Skip invalid paths
                continue;
            }
        }
        
        if (filePath != null)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                currentScenario = JsonUtility.FromJson<Scenario>(json);
                Debug.Log($"[BenchmarkRunner] Loaded scenario from {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BenchmarkRunner] Failed to load scenario: {e.Message}. Using default.");
                currentScenario = defaultScenario;
            }
        }
        else
        {
            Debug.LogWarning($"[BenchmarkRunner] Scenario file not found. Using default scenario.");
            currentScenario = defaultScenario;
        }
        
        if (currentScenario == null)
        {
            // Create minimal default scenario
            currentScenario = new Scenario
            {
                name = "Default",
                robotStart = Vector3.zero,
                robotGoal = new Vector3(5f, 0, 5f),
                obstaclePositions = new Vector3[]
                {
                    new Vector3(2.5f, 0.5f, 2.5f)
                },
                timeout = 60f
            };
        }
    }

    /// <summary>
    /// Start benchmark run
    /// </summary>
    public void StartBenchmark()
    {
        if (isRunning)
        {
            Debug.LogWarning("[BenchmarkRunner] Benchmark already running");
            return;
        }
        
        isRunning = true;
        currentRun = 0;
        report.results.Clear();
        report.startTime = System.DateTime.Now;
        
        if (latencyProfiler != null)
        {
            latencyProfiler.StartBenchmark();
        }
        
        StartCoroutine(RunBenchmarkSuite());
    }

    /// <summary>
    /// Stop benchmark run
    /// </summary>
    public void StopBenchmark()
    {
        isRunning = false;
        StopAllCoroutines();
        
        if (latencyProfiler != null)
        {
            LatencyProfiler.BenchmarkResults results = latencyProfiler.StopBenchmark();
            if (results != null)
            {
                report.averageLatency = results.meanLatency;
            }
        }
        
        FinalizeReport();
    }

    IEnumerator RunBenchmarkSuite()
    {
        while (currentRun < scenarioRepeats && isRunning)
        {
            yield return StartCoroutine(RunScenario());
            currentRun++;
            yield return new WaitForSeconds(2f); // Pause between runs
        }
        
        FinalizeReport();
    }

    IEnumerator RunScenario()
    {
        // Reset
        ResetScenario();
        
        // Update status
        if (statusText != null)
        {
            statusText.text = $"Running Scenario {currentRun + 1}/{scenarioRepeats}: {currentScenario.name}";
        }
        
        // Spawn obstacles
        SpawnObstacles(currentScenario.obstaclePositions);
        
        // Reset robot
        if (robot != null)
        {
            robot.transform.position = currentScenario.robotStart;
            lastRobotPosition = currentScenario.robotStart;
            totalDistanceTraveled = 0f;
        }
        
        // Enable ROS/AI
        EnableROS(true);
        
        // Run scenario
        scenarioStartTime = Time.time;
        bool success = false;
        bool crashed = false;
        float minMargin = 100f;
        List<float> latencySamples = new List<float>();
        
        while (Time.time - scenarioStartTime < currentScenario.timeout && isRunning)
        {
            // Check for collision
            if (CheckCollision())
            {
                crashed = true;
                break;
            }
            
            // Monitor safety margin
            if (dashboardManager != null)
            {
                // Note: This requires exposing margin value from ROS2DashboardManager
                // For now, we'll use a placeholder
                float margin = GetCurrentMargin();
                if (margin < minMargin) minMargin = margin;
            }
            
            // Check if goal reached
            if (robot != null)
            {
                float distanceToGoal = Vector3.Distance(robot.transform.position, currentScenario.robotGoal);
                if (distanceToGoal < currentScenario.successRadius)
                {
                    success = true;
                    break;
                }
                
                // Track distance traveled
                totalDistanceTraveled += Vector3.Distance(robot.transform.position, lastRobotPosition);
                lastRobotPosition = robot.transform.position;
            }
            
            // Sample latency
            if (latencyProfiler != null)
            {
                // Get current latency from profiler
                // This would require exposing current latency value
            }
            
            yield return null;
        }
        
        // Record result
        float completionTime = Time.time - scenarioStartTime;
        BenchmarkResult result = new BenchmarkResult
        {
            runNumber = currentRun + 1,
            scenarioName = currentScenario.name,
            success = success,
            crashed = crashed,
            minMargin = minMargin,
            averageLatency = latencySamples.Count > 0 ? latencySamples.Average() : 0f,
            maxLatency = latencySamples.Count > 0 ? latencySamples.Max() : 0f,
            completionTime = completionTime,
            distanceTraveled = totalDistanceTraveled,
            timestamp = System.DateTime.Now
        };
        
        report.results.Add(result);
        
        // Update stats
        UpdateStats();
        
        // Cleanup
        ClearObstacles();
        EnableROS(false);
    }

    void ResetScenario()
    {
        ClearObstacles();
        if (robot != null)
        {
            robot.transform.position = robotStartPosition;
        }
    }

    void SpawnObstacles(Vector3[] positions)
    {
        if (positions == null) return;
        
        foreach (Vector3 pos in positions)
        {
            GameObject obstacle;
            if (obstaclePrefab != null)
            {
                obstacle = Instantiate(obstaclePrefab, pos, Quaternion.identity);
            }
            else
            {
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.transform.position = pos;
                obstacle.transform.localScale = Vector3.one;
            }
            
            if (obstacleParent != null)
            {
                obstacle.transform.SetParent(obstacleParent);
            }
            
            spawnedObstacles.Add(obstacle);
        }
    }

    void ClearObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null) Destroy(obstacle);
        }
        spawnedObstacles.Clear();
    }

    bool CheckCollision()
    {
        // Simple collision check - can be enhanced
        if (robot == null) return false;
        
        Collider robotCollider = robot.GetComponent<Collider>();
        if (robotCollider == null) return false;
        
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle == null) continue;
            
            Collider obstacleCollider = obstacle.GetComponent<Collider>();
            if (obstacleCollider != null && robotCollider.bounds.Intersects(obstacleCollider.bounds))
            {
                return true;
            }
        }
        
        return false;
    }

    float GetCurrentMargin()
    {
        // This would need to be exposed from ROS2DashboardManager
        // For now, return a placeholder
        return 2.0f;
    }

    float DistanceToGoal()
    {
        if (robot == null) return float.MaxValue;
        return Vector3.Distance(robot.transform.position, currentScenario.robotGoal);
    }

    void EnableROS(bool enable)
    {
        // Enable/disable ROS components
        // This would control the ROS connection
        if (dashboardManager != null)
        {
            // Could add enable/disable functionality
        }
    }

    void UpdateStats()
    {
        if (statsText == null) return;
        
        int successful = report.results.Count(r => r.success);
        int crashed = report.results.Count(r => r.crashed);
        float successRate = report.results.Count > 0 ? (successful / (float)report.results.Count) * 100f : 0f;
        
        statsText.text = $"Runs: {report.results.Count}/{scenarioRepeats}\n" +
                        $"Success: {successful} ({successRate:F1}%)\n" +
                        $"Crashed: {crashed}\n" +
                        $"Avg Margin: {report.results.Average(r => r.minMargin):F2}m";
    }

    void FinalizeReport()
    {
        isRunning = false;
        report.endTime = System.DateTime.Now;
        report.totalRuns = report.results.Count;
        report.successfulRuns = report.results.Count(r => r.success);
        report.crashedRuns = report.results.Count(r => r.crashed);
        report.successRate = report.totalRuns > 0 ? (report.successfulRuns / (float)report.totalRuns) * 100f : 0f;
        report.averageMinMargin = report.results.Count > 0 ? report.results.Average(r => r.minMargin) : 0f;
        
        // Save report
        SaveReport();
        
        if (statusText != null)
        {
            statusText.text = $"Benchmark Complete!\nSuccess Rate: {report.successRate:F1}%";
        }
        
        Debug.Log($"[BenchmarkRunner] Benchmark complete: {report.successRate:F1}% success rate ({report.successfulRuns}/{report.totalRuns})");
    }

    void SaveReport()
    {
        // Save JSON report
        string jsonReport = JsonUtility.ToJson(report, true);
        string reportPath = Path.Combine(Application.persistentDataPath, $"BenchmarkReport_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(reportPath, jsonReport);
        
        // Save CSV
        string csvPath = Path.Combine(Application.persistentDataPath, $"BenchmarkResults_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using (StreamWriter writer = new StreamWriter(csvPath))
        {
            writer.WriteLine("Run,Scenario,Success,Crashed,MinMargin,AvgLatency,MaxLatency,CompletionTime,Distance,Timestamp");
            foreach (var result in report.results)
            {
                writer.WriteLine($"{result.runNumber},{result.scenarioName},{result.success},{result.crashed}," +
                               $"{result.minMargin:F4},{result.averageLatency:F2},{result.maxLatency:F2}," +
                               $"{result.completionTime:F2},{result.distanceTraveled:F2},{result.timestamp}");
            }
        }
        
        Debug.Log($"[BenchmarkRunner] Report saved to {reportPath}");
        Debug.Log($"[BenchmarkRunner] CSV saved to {csvPath}");
    }
}
