using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Research Episode Manager - Manages episodic RL for research-grade benchmarks.
/// Handles state machine, reward logging, and success/failure tracking.
/// Compatible with Franka Panda, Trossen ALOHA, and standard RL benchmarks.
/// </summary>
public class ResearchEpisodeManager : MonoBehaviour
{
    [Header("Curriculum Configuration")]
    [Tooltip("Maximum episodes per run (Franka/Mujoco standard is 500)")]
    public int maxEpisodes = 500;

    [Tooltip("Maximum time per episode in seconds")]
    public float episodeTimeout = 60.0f;

    [Tooltip("Auto-reset environment between episodes")]
    public bool autoReset = true;

    [Tooltip("Success threshold for reward (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float successThreshold = 0.8f;

    [Header("UI References")]
    [Tooltip("Text display for episode statistics")]
    public Text statsText;

    [Tooltip("Text display for current reward")]
    public Text rewardText;

    [Header("Data Export")]
    [Tooltip("Export directory (relative to Assets)")]
    public string exportDirectory = "Research";

    [Tooltip("Auto-export after each episode")]
    public bool autoExport = false;

    // State Tracking
    private int currentEpisode = 0;
    private float currentReward = 0.0f;
    private float episodeStartTime = 0f;
    private bool isEpisodeActive = false;
    private List<EpisodeData> episodeLogs = new List<EpisodeData>();
    private int successCount = 0;
    private int failureCount = 0;

    [System.Serializable]
    public class EpisodeData
    {
        public string timestamp;
        public int episode;
        public string state;
        public float reward;
        public string outcome;
        public float duration;
        public bool success;
    }

    void Start()
    {
        InitializeExportDirectory();
        UpdateUI();
    }

    void Update()
    {
        if (isEpisodeActive)
        {
            // Check timeout
            if (Time.time - episodeStartTime > episodeTimeout)
            {
                Debug.LogWarning($"[Research] Episode {currentEpisode} timed out after {episodeTimeout}s");
                CompleteEpisode(currentReward, "TIMEOUT");
            }
        }
    }

    void InitializeExportDirectory()
    {
        string fullPath = Path.Combine(Application.dataPath, exportDirectory);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"[Research] Created export directory: {fullPath}");
        }
    }

    /// <summary>
    /// Start a new episode
    /// </summary>
    public void StartEpisode()
    {
        if (currentEpisode >= maxEpisodes)
        {
            Debug.Log("[Research] Maximum episodes reached. Exporting data...");
            ExportLogs();
            return;
        }

        currentEpisode++;
        currentReward = 0.0f;
        episodeStartTime = Time.time;
        isEpisodeActive = true;

        Debug.Log($"[Research] Starting Episode {currentEpisode}/{maxEpisodes}");
        UpdateUI();
    }

    /// <summary>
    /// Log a state transition (for RL training)
    /// </summary>
    public void LogTransition(string state, float reward, string details)
    {
        if (!isEpisodeActive) return;

        currentReward += reward;

        string entry = $"{System.DateTime.Now:O},{currentEpisode},{state},{reward:F4},{details}";
        string logPath = Path.Combine(Application.dataPath, exportDirectory, "research_log.csv");

        // Write to CSV
        try
        {
            File.AppendAllText(logPath, entry + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Research] Failed to write log: {e.Message}");
        }

        UpdateUI();
    }

    /// <summary>
    /// Complete current episode
    /// </summary>
    public void CompleteEpisode(float finalReward, string outcome)
    {
        if (!isEpisodeActive) return;

        isEpisodeActive = false;
        currentReward = finalReward;
        float duration = Time.time - episodeStartTime;

        // Evaluate success
        bool success = finalReward >= successThreshold;

        // Update statistics
        if (success)
        {
            successCount++;
        }
        else
        {
            failureCount++;
        }

        // Create episode data
        EpisodeData episodeData = new EpisodeData
        {
            timestamp = System.DateTime.Now.ToString("O"),
            episode = currentEpisode,
            state = "COMPLETE",
            reward = finalReward,
            outcome = outcome,
            duration = duration,
            success = success
        };

        episodeLogs.Add(episodeData);

        // Log completion
        string logMessage = success ? "EPISODE COMPLETE" : "EPISODE FAILED";
        LogTransition(logMessage, finalReward, outcome);

        Debug.Log($"[Research] Episode {currentEpisode} Complete: {outcome} (Reward: {finalReward:F4}, Success: {success})");

        // Auto-export if enabled
        if (autoExport)
        {
            ExportLogs();
        }

        // Auto-reset if enabled
        if (autoReset)
        {
            if (success)
            {
                Debug.Log($"[Research] Episode {currentEpisode} Success. Resetting...");
            }
            else
            {
                Debug.Log($"[Research] Episode {currentEpisode} Failed. Resetting...");
            }

            ResetEnvironment();
        }

        UpdateUI();
    }

    /// <summary>
    /// Reset environment for next episode
    /// </summary>
    public void ResetEnvironment()
    {
        // Reset robot pose
        UniversalModelManager modelManager = FindObjectOfType<UniversalModelManager>();
        if (modelManager != null)
        {
            // Hard reset (teleport to start)
            modelManager.HardReset();
        }

        // Reset obstacles/objects
        ResetObjects();

        // Start next episode
        if (currentEpisode < maxEpisodes)
        {
            StartEpisode();
        }
        else
        {
            Debug.Log("[Research] All episodes complete. Exporting data...");
            ExportLogs();
        }
    }

    void ResetObjects()
    {
        // Find all resettable objects
        GameObject[] resettableObjects = GameObject.FindGameObjectsWithTag("Resettable");
        foreach (GameObject obj in resettableObjects)
        {
            // Reset position/rotation
            ResetableObject resetable = obj.GetComponent<ResetableObject>();
            if (resetable != null)
            {
                resetable.Reset();
            }
        }
    }

    /// <summary>
    /// Export episode logs to JSON (for Franka/Mujoco plots)
    /// </summary>
    public void ExportLogs()
    {
        if (episodeLogs.Count == 0)
        {
            Debug.LogWarning("[Research] No episode data to export");
            return;
        }

        string jsonPath = Path.Combine(Application.dataPath, exportDirectory, "episode_data.json");
        string csvPath = Path.Combine(Application.dataPath, exportDirectory, "episode_data.csv");

        // Export JSON
        try
        {
            string json = JsonUtility.ToJson(new EpisodeDataList { episodes = episodeLogs }, true);
            File.WriteAllText(jsonPath, json);
            Debug.Log($"[Research] Exported {episodeLogs.Count} episodes to JSON: {jsonPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Research] Failed to export JSON: {e.Message}");
        }

        // Export CSV (for compatibility)
        try
        {
            using (StreamWriter writer = new StreamWriter(csvPath))
            {
                writer.WriteLine("timestamp,episode,state,reward,outcome,duration,success");
                foreach (EpisodeData data in episodeLogs)
                {
                    writer.WriteLine($"{data.timestamp},{data.episode},{data.state},{data.reward:F4},{data.outcome},{data.duration:F2},{data.success}");
                }
            }
            Debug.Log($"[Research] Exported {episodeLogs.Count} episodes to CSV: {csvPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Research] Failed to export CSV: {e.Message}");
        }

        // Calculate statistics
        float avgReward = episodeLogs.Average(e => e.reward);
        float successRate = (float)successCount / episodeLogs.Count;
        float avgDuration = episodeLogs.Average(e => e.duration);

        Debug.Log($"[Research] Statistics - Avg Reward: {avgReward:F4}, Success Rate: {successRate:P2}, Avg Duration: {avgDuration:F2}s");
    }

    /// <summary>
    /// Get current episode statistics
    /// </summary>
    public EpisodeStatistics GetStatistics()
    {
        if (episodeLogs.Count == 0)
        {
            return new EpisodeStatistics();
        }

        return new EpisodeStatistics
        {
            totalEpisodes = episodeLogs.Count,
            successCount = successCount,
            failureCount = failureCount,
            successRate = (float)successCount / episodeLogs.Count,
            averageReward = episodeLogs.Average(e => e.reward),
            averageDuration = episodeLogs.Average(e => e.duration)
        };
    }

    void UpdateUI()
    {
        if (statsText != null)
        {
            float successRate = episodeLogs.Count > 0 ? (float)successCount / episodeLogs.Count : 0f;
            statsText.text = $"RESEARCH MODE | Episode: {currentEpisode}/{maxEpisodes} | Success: {successCount}/{episodeLogs.Count} ({successRate:P0})";
        }

        if (rewardText != null)
        {
            rewardText.text = $"Reward: {currentReward:F4}";
        }
    }

    [System.Serializable]
    public class EpisodeDataList
    {
        public List<EpisodeData> episodes;
    }

    [System.Serializable]
    public class EpisodeStatistics
    {
        public int totalEpisodes;
        public int successCount;
        public int failureCount;
        public float successRate;
        public float averageReward;
        public float averageDuration;
    }
}

/// <summary>
/// Helper component for resettable objects
/// </summary>
public class ResetableObject : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Reset()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Reset rigidbody if present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
