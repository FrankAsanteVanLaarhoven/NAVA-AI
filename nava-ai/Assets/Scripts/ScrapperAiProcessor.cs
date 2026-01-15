using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quick AI Processor - High-frequency processing for Sim2Val++ logic.
/// Runs on data ingested by MassiveDataScrapper to filter noise, generate training vectors,
/// and process massive volumes instantly.
/// </summary>
public class ScrapperAiProcessor : MonoBehaviour
{
    [Header("Processing Settings")]
    [Tooltip("Input data buffer size")]
    public int bufferSize = 10000;
    
    [Tooltip("Processing rate (Hz)")]
    public float processingRate = 60f;
    
    [Tooltip("Noise threshold")]
    public float noiseThreshold = 0.1f;
    
    [Header("Component References")]
    [Tooltip("Reference to data scrapper")]
    public MassiveDataScrapper dataScrapper;
    
    [Header("UI References")]
    [Tooltip("Text displaying processing status")]
    public UnityEngine.UI.Text statusText;
    
    [Tooltip("Text displaying processed count")]
    public UnityEngine.UI.Text processedCountText;
    
    // Input/Output Buffers
    private List<Vector3> inputVectorData = new List<Vector3>();
    private List<Vector3> outputTrainingData = new List<Vector3>();
    private Queue<Vector3> processingQueue = new Queue<Vector3>();
    
    private float lastProcessTime = 0f;
    private float processInterval;
    private int processedCount = 0;
    private int filteredCount = 0;

    void Start()
    {
        processInterval = 1f / processingRate;
        
        // Get data scrapper reference
        if (dataScrapper == null)
        {
            dataScrapper = GetComponent<MassiveDataScrapper>();
        }
        
        Debug.Log("[ScrapperAI] Quick AI Processor initialized");
    }

    void Update()
    {
        // Throttle processing
        if (Time.time - lastProcessTime < processInterval) return;
        
        // Process input buffer
        ProcessInputBuffer();
        
        lastProcessTime = Time.time;
        
        // Update UI
        UpdateUI();
    }

    /// <summary>
    /// Add input data to processing queue
    /// </summary>
    public void AddInputData(Vector3 data)
    {
        if (inputVectorData.Count >= bufferSize)
        {
            inputVectorData.RemoveAt(0); // Remove oldest
        }
        
        inputVectorData.Add(data);
        processingQueue.Enqueue(data);
    }

    /// <summary>
    /// Add multiple input data points
    /// </summary>
    public void AddInputDataBatch(List<Vector3> dataBatch)
    {
        foreach (var data in dataBatch)
        {
            AddInputData(data);
        }
    }

    void ProcessInputBuffer()
    {
        int processed = 0;
        int maxPerFrame = Mathf.CeilToInt(processingRate / 60f); // Process based on rate
        
        while (processingQueue.Count > 0 && processed < maxPerFrame)
        {
            Vector3 input = processingQueue.Dequeue();
            
            // 1. Filter Noise (Sim2Val++ Logic)
            if (IsNoise(input))
            {
                filteredCount++;
                continue; // Skip bad data
            }
            
            // 2. Generate Training Vector (Supervised)
            Vector3 trainingVector = ComputeDesiredVector(input);
            
            // 3. Buffer Output
            if (outputTrainingData.Count >= bufferSize)
            {
                outputTrainingData.RemoveAt(0);
            }
            
            outputTrainingData.Add(trainingVector);
            processedCount++;
            processed++;
        }
    }

    /// <summary>
    /// Check if data point is noise
    /// </summary>
    bool IsNoise(Vector3 pos)
    {
        // Simple Heuristic: Check against Ground Plane
        // If y < 0, it's underground (Noise)
        if (pos.y < -noiseThreshold) return true;
        
        // Check for NaN or Infinity
        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z)) return true;
        if (float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z)) return true;
        
        // Check for extreme values (likely sensor error)
        if (Mathf.Abs(pos.x) > 1000f || Mathf.Abs(pos.y) > 1000f || Mathf.Abs(pos.z) > 1000f) return true;
        
        return false;
    }

    /// <summary>
    /// Compute desired vector from input
    /// </summary>
    Vector3 ComputeDesiredVector(Vector3 current)
    {
        // Example: Move towards origin (Scraping center)
        // In production, this would use actual AI model
        Vector3 target = Vector3.zero - current;
        
        // Normalize and scale
        if (target.magnitude > 0.01f)
        {
            return target.normalized * 5.0f; // Velocity
        }
        
        return Vector3.zero;
    }

    void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"PROCESSING: {processingQueue.Count} queued | {filteredCount} filtered";
        }
        
        if (processedCountText != null)
        {
            processedCountText.text = $"PROCESSED: {processedCount} | OUTPUT: {outputTrainingData.Count}";
        }
    }

    /// <summary>
    /// Get processed training data
    /// </summary>
    public List<Vector3> GetTrainingData()
    {
        return new List<Vector3>(outputTrainingData);
    }

    /// <summary>
    /// Get processed training data (cleared after retrieval)
    /// </summary>
    public List<Vector3> PopTrainingData()
    {
        List<Vector3> data = new List<Vector3>(outputTrainingData);
        outputTrainingData.Clear();
        return data;
    }

    /// <summary>
    /// Clear all buffers
    /// </summary>
    public void ClearBuffers()
    {
        inputVectorData.Clear();
        outputTrainingData.Clear();
        processingQueue.Clear();
        processedCount = 0;
        filteredCount = 0;
    }

    /// <summary>
    /// Get processing statistics
    /// </summary>
    public ProcessingStats GetStats()
    {
        ProcessingStats stats = new ProcessingStats();
        stats.totalProcessed = processedCount;
        stats.totalFiltered = filteredCount;
        stats.inputBufferSize = inputVectorData.Count;
        stats.outputBufferSize = outputTrainingData.Count;
        stats.queueSize = processingQueue.Count;
        stats.filterRate = processedCount > 0 ? (float)filteredCount / (processedCount + filteredCount) : 0f;
        
        return stats;
    }

    [System.Serializable]
    public class ProcessingStats
    {
        public int totalProcessed;
        public int totalFiltered;
        public int inputBufferSize;
        public int outputBufferSize;
        public int queueSize;
        public float filterRate;
    }
}
