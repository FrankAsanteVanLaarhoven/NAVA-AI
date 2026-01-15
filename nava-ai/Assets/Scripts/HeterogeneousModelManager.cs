using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Heterogeneous Model Manager - The "Waymo Killer".
/// Manages the Hybrid Swarm by loading VLA, RL, and SSM models and assigning them
/// dynamically based on the Task derived from the AGI Commander.
/// </summary>
public class HeterogeneousModelManager : MonoBehaviour
{
    [System.Serializable]
    public class ModelPool
    {
        public string modelType;
        public GameObject modelPrefab;
        public int poolSize;
        public List<GameObject> pool = new List<GameObject>();
        public int activeCount = 0;
    }

    [Header("Model Pools")]
    [Tooltip("VLA model pool (for Surveillance)")]
    public ModelPool vlaPool = new ModelPool { modelType = "VLA", poolSize = 5 };
    
    [Tooltip("RL model pool (for Logistics)")]
    public ModelPool rlPool = new ModelPool { modelType = "RL", poolSize = 5 };
    
    [Tooltip("SSM model pool (for Maintenance)")]
    public ModelPool ssmPool = new ModelPool { modelType = "SSM", poolSize = 5 };
    
    [Header("Model Prefabs")]
    [Tooltip("VLA model prefab")]
    public GameObject vlaModelPrefab;
    
    [Tooltip("RL model prefab")]
    public GameObject rlModelPrefab;
    
    [Tooltip("SSM model prefab")]
    public GameObject ssmModelPrefab;
    
    [Header("UI References")]
    [Tooltip("Text displaying pool status")]
    public UnityEngine.UI.Text poolStatusText;
    
    private Dictionary<GameObject, GameObject> agentModelMap = new Dictionary<GameObject, GameObject>();
    private Dictionary<string, ModelPool> poolMap = new Dictionary<string, ModelPool>();

    void Start()
    {
        // Initialize pools
        InitializePools();
        
        Debug.Log("[HeterogeneousModelManager] Initialized - Model pools ready");
    }

    void InitializePools()
    {
        // Setup pool references
        vlaPool.modelPrefab = vlaModelPrefab;
        rlPool.modelPrefab = rlModelPrefab;
        ssmPool.modelPrefab = ssmModelPrefab;
        
        // Create pool map
        poolMap["VLA"] = vlaPool;
        poolMap["RL"] = rlPool;
        poolMap["SSM"] = ssmPool;
        
        // Pre-load Models (Pool Optimization)
        PreloadPool(vlaPool);
        PreloadPool(rlPool);
        PreloadPool(ssmPool);
    }

    void PreloadPool(ModelPool pool)
    {
        if (pool.modelPrefab == null)
        {
            Debug.LogWarning($"[HeterogeneousModelManager] {pool.modelType} prefab not assigned. Pool will be empty.");
            return;
        }
        
        for (int i = 0; i < pool.poolSize; i++)
        {
            GameObject model = Instantiate(pool.modelPrefab);
            model.name = $"{pool.modelType}_Model_{i}";
            model.SetActive(false);
            model.transform.SetParent(transform);
            pool.pool.Add(model);
        }
        
        Debug.Log($"[HeterogeneousModelManager] Pre-loaded {pool.poolSize} {pool.modelType} models");
    }

    /// <summary>
    /// Assign model to agent based on type
    /// </summary>
    public GameObject AssignModel(GameObject agent, string modelType)
    {
        if (agent == null)
        {
            Debug.LogError("[HeterogeneousModelManager] Agent is null");
            return null;
        }
        
        // Return existing model if already assigned
        if (agentModelMap.ContainsKey(agent))
        {
            GameObject existingModel = agentModelMap[agent];
            if (existingModel != null && existingModel.activeSelf)
            {
                // Check if it's the right type
                string existingType = GetModelType(existingModel);
                if (existingType == modelType.ToUpper())
                {
                    return existingModel; // Already has correct model
                }
                else
                {
                    // Return old model to pool
                    ReturnModelToPool(agent, existingModel);
                }
            }
        }
        
        // Get pool for model type
        ModelPool pool = GetPoolForType(modelType);
        if (pool == null)
        {
            Debug.LogError($"[HeterogeneousModelManager] Unknown model type: {modelType}");
            return null;
        }
        
        // Retrieve from Pool (Reusing GameObjects)
        GameObject model = GetModelFromPool(pool);
        
        if (model == null)
        {
            // Pool exhausted, create new one
            if (pool.modelPrefab != null)
            {
                model = Instantiate(pool.modelPrefab);
                model.name = $"{pool.modelType}_Model_Dynamic";
                pool.pool.Add(model);
            }
            else
            {
                Debug.LogError($"[HeterogeneousModelManager] Cannot create {modelType} model - prefab not assigned");
                return null;
            }
        }
        
        // Attach & Enable
        model.transform.SetParent(agent.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.SetActive(true);
        
        // Reset Model State (Critical for Hybrid)
        ResetModelState(model);
        
        // Track assignment
        agentModelMap[agent] = model;
        pool.activeCount++;
        
        Debug.Log($"[HeterogeneousModelManager] Assigned {modelType} model to {agent.name}");
        
        return model;
    }

    ModelPool GetPoolForType(string modelType)
    {
        string upperType = modelType.ToUpper();
        if (poolMap.ContainsKey(upperType))
        {
            return poolMap[upperType];
        }
        return null;
    }

    GameObject GetModelFromPool(ModelPool pool)
    {
        // Find inactive model in pool
        GameObject model = pool.pool.FirstOrDefault(g => g != null && !g.activeSelf);
        return model;
    }

    string GetModelType(GameObject model)
    {
        if (model == null) return "";
        
        string name = model.name.ToUpper();
        if (name.Contains("VLA")) return "VLA";
        if (name.Contains("RL")) return "RL";
        if (name.Contains("SSM")) return "SSM";
        
        return "";
    }

    void ResetModelState(GameObject model)
    {
        if (model == null) return;
        
        // Reset UniversalModelManager if present
        UniversalModelManager umm = model.GetComponent<UniversalModelManager>();
        if (umm != null)
        {
            // Reset to default state
            // This would reset model weights, state, etc. in production
        }
        
        // Reset any other model-specific components
        // In production, this would reset neural network state, buffers, etc.
    }

    /// <summary>
    /// Return model to pool
    /// </summary>
    public void ReturnModelToPool(GameObject agent, GameObject model)
    {
        if (agent == null || model == null) return;
        
        // Deactivate model
        model.SetActive(false);
        model.transform.SetParent(transform);
        
        // Remove from agent mapping
        if (agentModelMap.ContainsKey(agent))
        {
            agentModelMap.Remove(agent);
        }
        
        // Update pool count
        string modelType = GetModelType(model);
        ModelPool pool = GetPoolForType(modelType);
        if (pool != null)
        {
            pool.activeCount = Mathf.Max(0, pool.activeCount - 1);
        }
        
        Debug.Log($"[HeterogeneousModelManager] Returned {modelType} model to pool from {agent.name}");
    }

    /// <summary>
    /// Get model assigned to agent
    /// </summary>
    public GameObject GetAgentModel(GameObject agent)
    {
        if (agentModelMap.ContainsKey(agent))
        {
            return agentModelMap[agent];
        }
        return null;
    }

    /// <summary>
    /// Get pool statistics
    /// </summary>
    public PoolStatistics GetPoolStatistics()
    {
        PoolStatistics stats = new PoolStatistics();
        
        stats.vlaTotal = vlaPool.poolSize;
        stats.vlaActive = vlaPool.activeCount;
        stats.vlaAvailable = vlaPool.pool.Count(g => g != null && !g.activeSelf);
        
        stats.rlTotal = rlPool.poolSize;
        stats.rlActive = rlPool.activeCount;
        stats.rlAvailable = rlPool.pool.Count(g => g != null && !g.activeSelf);
        
        stats.ssmTotal = ssmPool.poolSize;
        stats.ssmActive = ssmPool.activeCount;
        stats.ssmAvailable = ssmPool.pool.Count(g => g != null && !g.activeSelf);
        
        return stats;
    }

    void Update()
    {
        // Update UI periodically
        if (Time.frameCount % 60 == 0 && poolStatusText != null)
        {
            PoolStatistics stats = GetPoolStatistics();
            poolStatusText.text = $"MODELS: VLA({stats.vlaActive}/{stats.vlaTotal}) " +
                                 $"RL({stats.rlActive}/{stats.rlTotal}) " +
                                 $"SSM({stats.ssmActive}/{stats.ssmTotal})";
        }
    }

    [System.Serializable]
    public class PoolStatistics
    {
        public int vlaTotal;
        public int vlaActive;
        public int vlaAvailable;
        public int rlTotal;
        public int rlActive;
        public int rlAvailable;
        public int ssmTotal;
        public int ssmActive;
        public int ssmAvailable;
    }
}
