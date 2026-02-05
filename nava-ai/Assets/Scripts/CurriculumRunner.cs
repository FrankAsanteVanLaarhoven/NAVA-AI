using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Curriculum Runner - Domain adaptation through progressive difficulty.
/// Implements curriculum learning: Simple Task -> Hard Task -> Complex Task.
/// </summary>
public class CurriculumRunner : MonoBehaviour
{
    [System.Serializable]
    public class CurriculumTask
    {
        [Tooltip("Task name (e.g., 'Push Block', 'Stack Block', 'Open Box')")]
        public string name;

        [Tooltip("Unity Scene path to load")]
        public string scenePath;

        [Tooltip("Difficulty modifier (1.0 = Easy, 2.0 = Hard)")]
        [Range(0.5f, 3.0f)]
        public float difficultyModifier = 1.0f;

        [Tooltip("Number of episodes to run for this task")]
        public int episodesPerTask = 1;

        [Tooltip("Success threshold for advancing")]
        [Range(0f, 1f)]
        public float successThreshold = 0.8f;
    }

    [Header("Curriculum Configuration")]
    [Tooltip("List of tasks in curriculum order")]
    public List<CurriculumTask> curriculum = new List<CurriculumTask>();

    [Tooltip("Auto-advance to next stage on success")]
    public bool autoAdvance = true;

    [Tooltip("Repeat failed tasks")]
    public bool repeatOnFailure = true;

    [Header("Components")]
    [Tooltip("Benchmark Importer component")]
    public BenchmarkImporter benchmarkImporter;

    [Tooltip("Research Episode Manager component")]
    public ResearchEpisodeManager episodeManager;

    [Header("UI References")]
    [Tooltip("Text display for current curriculum stage")]
    public UnityEngine.UI.Text curriculumStatusText;

    private int currentStage = 0;
    private bool isRunning = false;
    private int currentTaskEpisodes = 0;
    private int taskSuccessCount = 0;

    void Start()
    {
        // Auto-find components if not assigned
        if (benchmarkImporter == null)
        {
            benchmarkImporter = GetComponent<BenchmarkImporter>();
        }

        if (episodeManager == null)
        {
            episodeManager = GetComponent<ResearchEpisodeManager>();
        }

        // Initialize default curriculum if empty
        if (curriculum.Count == 0)
        {
            InitializeDefaultCurriculum();
        }

        UpdateUI();
    }

    void InitializeDefaultCurriculum()
    {
        Debug.Log("[Curriculum] Initializing default curriculum...");

        curriculum.Add(new CurriculumTask
        {
            name = "Simple Push",
            scenePath = "Assets/Scenes/SimplePush.unity",
            difficultyModifier = 1.0f,
            episodesPerTask = 1,
            successThreshold = 0.7f
        });

        curriculum.Add(new CurriculumTask
        {
            name = "Complex Stack",
            scenePath = "Assets/Scenes/ComplexStack.unity",
            difficultyModifier = 1.5f,
            episodesPerTask = 1,
            successThreshold = 0.8f
        });

        curriculum.Add(new CurriculumTask
        {
            name = "Franka Kitchen",
            scenePath = "Assets/Environments/Franka_kitchen.unity",
            difficultyModifier = 2.0f,
            episodesPerTask = 1,
            successThreshold = 0.9f
        });

        Debug.Log($"[Curriculum] Initialized {curriculum.Count} stages.");
    }

    /// <summary>
    /// Start curriculum learning
    /// </summary>
    public void StartCurriculum()
    {
        if (isRunning)
        {
            Debug.LogWarning("[Curriculum] Curriculum already running");
            return;
        }

        if (curriculum.Count == 0)
        {
            Debug.LogError("[Curriculum] No curriculum tasks defined");
            return;
        }

        isRunning = true;
        currentStage = 0;
        currentTaskEpisodes = 0;
        taskSuccessCount = 0;

        Debug.Log("[Curriculum] Starting curriculum learning...");
        StartCoroutine(RunCurriculumCoroutine());
    }

    /// <summary>
    /// Stop curriculum learning
    /// </summary>
    public void StopCurriculum()
    {
        isRunning = false;
        StopAllCoroutines();
        Debug.Log("[Curriculum] Curriculum stopped");
    }

    IEnumerator RunCurriculumCoroutine()
    {
        while (isRunning && currentStage < curriculum.Count)
        {
            CurriculumTask task = curriculum[currentStage];
            Debug.Log($"[Curriculum] Stage {currentStage + 1}/{curriculum.Count}: {task.name}");

            // 1. Load Environment
            yield return StartCoroutine(LoadEnvironmentCoroutine(task));

            // 2. Configure Difficulty
            ConfigureTaskDifficulty(task);

            // 3. Run Episodes
            yield return StartCoroutine(RunTaskEpisodesCoroutine(task));

            // 4. Evaluate and Advance
            if (ShouldAdvance(task))
            {
                currentStage++;
                taskSuccessCount = 0;
                currentTaskEpisodes = 0;
            }
            else if (repeatOnFailure)
            {
                Debug.Log($"[Curriculum] Task '{task.name}' failed. Repeating...");
                taskSuccessCount = 0;
                currentTaskEpisodes = 0;
            }
            else
            {
                Debug.Log($"[Curriculum] Task '{task.name}' failed. Moving to next stage...");
                currentStage++;
            }

            UpdateUI();
        }

        if (currentStage >= curriculum.Count)
        {
            Debug.Log("[Curriculum] All stages complete!");
            if (curriculumStatusText != null)
            {
                curriculumStatusText.text = "CURRICULUM COMPLETE";
            }
        }

        isRunning = false;
    }

    IEnumerator LoadEnvironmentCoroutine(CurriculumTask task)
    {
        if (benchmarkImporter != null)
        {
            benchmarkImporter.environmentName = task.name;
            benchmarkImporter.scenePath = task.scenePath;
            benchmarkImporter.LoadEnvironment();

            // Wait for environment to load
            yield return new WaitForSeconds(1.0f);

            // Wait until environment is loaded
            float timeout = 10f;
            float elapsed = 0f;
            while (!benchmarkImporter.IsLoaded() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!benchmarkImporter.IsLoaded())
            {
                Debug.LogWarning($"[Curriculum] Environment '{task.name}' failed to load. Using current scene.");
            }
        }
        else
        {
            Debug.LogWarning("[Curriculum] BenchmarkImporter not found. Skipping environment load.");
        }
    }

    void ConfigureTaskDifficulty(CurriculumTask task)
    {
        if (episodeManager != null)
        {
            // Adjust episode timeout based on difficulty
            episodeManager.episodeTimeout = 60.0f * task.difficultyModifier;
            episodeManager.successThreshold = task.successThreshold;
            episodeManager.maxEpisodes = task.episodesPerTask;

            Debug.Log($"[Curriculum] Configured difficulty: Timeout={episodeManager.episodeTimeout}s, Threshold={task.successThreshold}");
        }
    }

    IEnumerator RunTaskEpisodesCoroutine(CurriculumTask task)
    {
        if (episodeManager == null)
        {
            Debug.LogError("[Curriculum] ResearchEpisodeManager not found");
            yield break;
        }

        currentTaskEpisodes = 0;

        while (currentTaskEpisodes < task.episodesPerTask && isRunning)
        {
            // Start episode
            episodeManager.StartEpisode();

            // Wait for episode to complete (or timeout)
            float episodeStartTime = Time.time;
            while (episodeManager.isEpisodeActive && (Time.time - episodeStartTime) < episodeManager.episodeTimeout)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Get episode result
            ResearchEpisodeManager.EpisodeStatistics stats = episodeManager.GetStatistics();
            if (stats.totalEpisodes > 0)
            {
                var lastEpisode = episodeManager.episodeLogs.LastOrDefault();
                if (lastEpisode != null && lastEpisode.success)
                {
                    taskSuccessCount++;
                }
            }

            currentTaskEpisodes++;
            yield return new WaitForSeconds(0.5f); // Brief pause between episodes
        }
    }

    bool ShouldAdvance(CurriculumTask task)
    {
        if (!autoAdvance)
        {
            return false;
        }

        float successRate = currentTaskEpisodes > 0 ? (float)taskSuccessCount / currentTaskEpisodes : 0f;
        bool shouldAdvance = successRate >= task.successThreshold;

        Debug.Log($"[Curriculum] Task '{task.name}' Success Rate: {successRate:P0} (Threshold: {task.successThreshold:P0})");

        return shouldAdvance;
    }

    void UpdateUI()
    {
        if (curriculumStatusText != null)
        {
            if (currentStage < curriculum.Count)
            {
                CurriculumTask task = curriculum[currentStage];
                curriculumStatusText.text = $"Stage {currentStage + 1}/{curriculum.Count}: {task.name} | Episodes: {currentTaskEpisodes}/{task.episodesPerTask} | Success: {taskSuccessCount}";
            }
            else
            {
                curriculumStatusText.text = "Curriculum Complete";
            }
        }
    }

    /// <summary>
    /// Get current curriculum progress
    /// </summary>
    public CurriculumProgress GetProgress()
    {
        return new CurriculumProgress
        {
            currentStage = currentStage,
            totalStages = curriculum.Count,
            isRunning = isRunning,
            currentTaskName = currentStage < curriculum.Count ? curriculum[currentStage].name : "Complete",
            taskSuccessCount = taskSuccessCount
        };
    }

    [System.Serializable]
    public class CurriculumProgress
    {
        public int currentStage;
        public int totalStages;
        public bool isRunning;
        public string currentTaskName;
        public int taskSuccessCount;
    }
}
