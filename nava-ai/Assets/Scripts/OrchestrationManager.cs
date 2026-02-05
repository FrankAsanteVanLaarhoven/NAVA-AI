using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Orchestration Manager - Manages curriculum learning progression automatically.
/// Transitions from simple tasks (Push) to complex tasks (Stacking Blocks -> Door).
/// Integrates with ResearchEpisodeManager for episode tracking.
/// </summary>
public class OrchestrationManager : MonoBehaviour
{
    [Header("Curriculum Configuration")]
    public int numEpisodes = 500; // Franka/Mujoco standard
    public float episodeTimeout = 60.0f;
    public float difficultyModifier = 1.0f;
    public bool autoReset = true;
    public TextMeshProUGUI curriculumStatusText;
    
    // Stage Definitions
    [System.Serializable]
    public class CurriculumTask
    {
        public string name; // "Push Block", "Stack Block", "Franka Kitchen", "Door"
        public string scenePath; // Path to Unity scene for the task
        public float difficulty = 1.0f;
        public bool isLoaded = false;
    }

    public List<CurriculumTask> curriculum = new List<CurriculumTask>();
    private int currentStage = 0;
    private float stageTimer = 0.0f;
    private ResearchEpisodeManager episodeManager;
    private bool isRunning = false;

    void Start()
    {
        // 1. Define Curriculum (Newcastle Lab Style)
        if (curriculum.Count == 0)
        {
            curriculum.Add(new CurriculumTask { 
                name = "Simple Push", 
                difficulty = 1.0f, 
                scenePath = "Assets/Scenes/SimplePush.unity" 
            });
            curriculum.Add(new CurriculumTask { 
                name = "Stack Block", 
                difficulty = 1.5f, 
                scenePath = "Assets/Scenes/ComplexStack.unity" 
            });
            curriculum.Add(new CurriculumTask { 
                name = "Franka Kitchen", 
                difficulty = 2.0f, 
                scenePath = "Assets/Scenes/FrankaKitchen.unity" 
            });
            curriculum.Add(new CurriculumTask { 
                name = "Door", 
                difficulty = 1.5f, 
                scenePath = "Assets/Scenes/ComplexHall.unity" 
            });
        }

        // 2. Find Research Episode Manager
        episodeManager = FindObjectOfType<ResearchEpisodeManager>();
        if (episodeManager == null)
        {
            Debug.LogWarning("[ORCHESTRATION] ResearchEpisodeManager not found. Episode tracking disabled.");
        }

        // 3. Initialize UI
        if (curriculumStatusText != null)
        {
            curriculumStatusText.text = $"CURRICULUM: {curriculum[0].name.ToUpper()} (STAGE 0)";
            curriculumStatusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Accent);
        }

        Debug.Log($"[ORCHESTRATION] Initialized: {curriculum.Count} stages loaded.");
    }

    void Update()
    {
        if (!isRunning) return;

        // Update stage timer
        stageTimer += Time.deltaTime;

        // Check for timeout
        if (stageTimer > episodeTimeout)
        {
            Debug.LogWarning($"[ORCHESTRATION] Stage {currentStage} timeout after {episodeTimeout}s");
            if (autoReset)
            {
                ResetCurrentStage();
            }
        }
    }

    public void RunStage(int stageIndex)
    {
        if (stageIndex >= curriculum.Count)
        {
            Debug.Log("[ORCHESTRATION] All Stages Complete.");
            if (curriculumStatusText != null)
            {
                curriculumStatusText.text = "CURRICULUM: COMPLETE";
                curriculumStatusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
            }
            return;
        }

        currentStage = stageIndex;
        stageTimer = 0.0f;
        isRunning = true;

        Debug.Log($"[ORCHESTRATION] Stage {stageIndex + 1}: {curriculum[stageIndex].name}...");
        
        // Load Scene
        var task = curriculum[stageIndex];
        StartCoroutine(LoadSceneAsync(task));
        
        // Sync with Research Episode Manager
        if (episodeManager != null)
        {
            episodeManager.currentStage = stageIndex;
        }

        // Update UI
        if (curriculumStatusText != null)
        {
            curriculumStatusText.text = $"CURRICULUM: STAGE {stageIndex + 1} - {task.name.ToUpper()}";
            curriculumStatusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Accent);
        }
    }

    IEnumerator LoadSceneAsync(CurriculumTask task)
    {
        // Use ResearchSceneManager if available
        ResearchSceneManager sceneManager = FindObjectOfType<ResearchSceneManager>();
        if (sceneManager != null)
        {
            yield return StartCoroutine(sceneManager.LoadEnvironmentAsync(task.scenePath));
        }
        else
        {
            // Fallback to standard scene loading
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(task.scenePath, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        task.isLoaded = true;
        Debug.Log($"[ORCHESTRATION] Scene loaded: {task.name}");
    }

    public void NextStage()
    {
        if (currentStage < curriculum.Count - 1)
        {
            currentStage++;
            Debug.Log($"[ORCHESTRATION] Advancing to Stage {currentStage + 1}");
            RunStage(currentStage);
        }
        else
        {
            Debug.Log("[ORCHESTRATION] All stages complete!");
        }
    }

    public void ResetCurrentStage()
    {
        stageTimer = 0.0f;
        if (episodeManager != null)
        {
            episodeManager.ResetEnvironment();
        }
        Debug.Log($"[ORCHESTRATION] Reset Stage {currentStage + 1}");
    }

    public void StartCurriculum()
    {
        isRunning = true;
        RunStage(0);
    }

    public void StopCurriculum()
    {
        isRunning = false;
        if (curriculumStatusText != null)
        {
            curriculumStatusText.text = "CURRICULUM: STOPPED";
            curriculumStatusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
        }
    }

    public int GetCurrentStage()
    {
        return currentStage;
    }

    public string GetCurrentStageName()
    {
        if (currentStage >= 0 && currentStage < curriculum.Count)
        {
            return curriculum[currentStage].name;
        }
        return "Unknown";
    }
}
