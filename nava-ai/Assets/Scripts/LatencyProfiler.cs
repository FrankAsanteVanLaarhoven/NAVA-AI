using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Latency & Jitter Profiler - Oscilloscope-style graph tracking frame times, jitter, and spikes.
/// Provides benchmark metrics: "99.9% of control loops are < 5ms"
/// </summary>
public class LatencyProfiler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("LineRenderer for oscilloscope graph")]
    public LineRenderer graphLine;
    
    [Tooltip("Text displaying jitter statistics")]
    public Text jitterText;
    
    [Tooltip("Text displaying latency statistics")]
    public Text latencyText;
    
    [Tooltip("RawImage for graph background (optional)")]
    public RawImage graphBackground;
    
    [Header("Graph Settings")]
    [Tooltip("Number of samples to display")]
    public int historySize = 100;
    
    [Tooltip("Graph width in UI units")]
    public float graphWidth = 400f;
    
    [Tooltip("Graph height in UI units")]
    public float graphHeight = 200f;
    
    [Tooltip("Maximum latency to display (ms)")]
    public float maxLatencyMs = 20f;
    
    [Header("Benchmark Thresholds")]
    [Tooltip("Target latency (ms) - green below this")]
    public float targetLatencyMs = 5f;
    
    [Tooltip("High jitter threshold (ms) - red above this")]
    public float highJitterThreshold = 2f;
    
    [Tooltip("Percentile for benchmark (99.9 = 99.9th percentile)")]
    public float benchmarkPercentile = 99.9f;
    
    [Header("Visualization")]
    [Tooltip("Color for good latency")]
    public Color goodColor = Color.green;
    
    [Tooltip("Color for high latency")]
    public Color badColor = Color.red;
    
    [Tooltip("Color for warning latency")]
    public Color warningColor = Color.yellow;
    
    private Queue<float> latencyHistory = new Queue<float>();
    private Queue<float> frameTimeHistory = new Queue<float>();
    private float lastFrameTime = 0f;
    private float benchmarkStartTime = 0f;
    private List<float> benchmarkSamples = new List<float>();
    private bool isBenchmarking = false;

    void Start()
    {
        lastFrameTime = Time.realtimeSinceStartup;
        benchmarkStartTime = Time.realtimeSinceStartup;
        
        // Create LineRenderer if not assigned
        if (graphLine == null)
        {
            GameObject lineObj = new GameObject("LatencyGraph");
            lineObj.transform.SetParent(transform);
            graphLine = lineObj.AddComponent<LineRenderer>();
            graphLine.useWorldSpace = false;
            graphLine.startWidth = 2f;
            graphLine.endWidth = 2f;
            graphLine.material = new Material(Shader.Find("Sprites/Default"));
            graphLine.color = goodColor;
        }
        
        Debug.Log("[LatencyProfiler] Initialized - Monitoring frame times and jitter");
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        
        float currentTime = Time.realtimeSinceStartup;
        
        // Measure frame time
        if (lastFrameTime > 0)
        {
            float delta = currentTime - lastFrameTime;
            float latencyMs = delta * 1000f;
            
            // Add to history
            latencyHistory.Enqueue(latencyMs);
            frameTimeHistory.Enqueue(delta);
            
            if (latencyHistory.Count > historySize)
            {
                latencyHistory.Dequeue();
                frameTimeHistory.Dequeue();
            }
            
            // Add to benchmark samples
            if (isBenchmarking)
            {
                benchmarkSamples.Add(latencyMs);
            }
            
            // Update graph
            UpdateGraph();
            
            // Calculate statistics
            CalculateStatistics();
        }
        
        lastFrameTime = currentTime;
    }

    void UpdateGraph()
    {
        if (graphLine == null || latencyHistory.Count < 2) return;
        
        // Convert queue to array
        float[] samples = latencyHistory.ToArray();
        graphLine.positionCount = samples.Length;
        
        // Calculate positions
        for (int i = 0; i < samples.Length; i++)
        {
            float x = (float)i / (samples.Length - 1) * graphWidth - graphWidth * 0.5f;
            float y = (samples[i] / maxLatencyMs) * graphHeight - graphHeight * 0.5f;
            
            graphLine.SetPosition(i, new Vector3(x, y, 0));
        }
        
        // Update color based on current latency
        float currentLatency = samples[samples.Length - 1];
        if (currentLatency < targetLatencyMs)
        {
            graphLine.color = goodColor;
        }
        else if (currentLatency < targetLatencyMs * 2f)
        {
            graphLine.color = warningColor;
        }
        else
        {
            graphLine.color = badColor;
        }
    }

    void CalculateStatistics()
    {
        if (latencyHistory.Count == 0) return;
        
        float[] samples = latencyHistory.ToArray();
        
        // Calculate mean
        float mean = samples.Average();
        
        // Calculate standard deviation (jitter)
        float variance = samples.Sum(x => (x - mean) * (x - mean)) / samples.Length;
        float stdDev = Mathf.Sqrt(variance);
        
        // Calculate min/max
        float min = samples.Min();
        float max = samples.Max();
        
        // Calculate percentile
        System.Array.Sort(samples);
        int percentileIndex = Mathf.RoundToInt(samples.Length * (benchmarkPercentile / 100f));
        percentileIndex = Mathf.Clamp(percentileIndex, 0, samples.Length - 1);
        float percentileValue = samples[percentileIndex];
        
        // Update UI
        if (jitterText != null)
        {
            jitterText.text = $"Jitter: {stdDev:F3}ms\n" +
                             $"Max: {max:F2}ms\n" +
                             $"Min: {min:F2}ms\n" +
                             $"{benchmarkPercentile}th: {percentileValue:F2}ms";
            
            // Color based on jitter
            if (stdDev > highJitterThreshold)
            {
                jitterText.color = badColor;
            }
            else if (stdDev > highJitterThreshold * 0.5f)
            {
                jitterText.color = warningColor;
            }
            else
            {
                jitterText.color = goodColor;
            }
        }
        
        if (latencyText != null)
        {
            latencyText.text = $"Latency: {mean:F2}ms\n" +
                              $"Current: {samples[samples.Length - 1]:F2}ms\n" +
                              $"Target: <{targetLatencyMs}ms";
            
            // Color based on mean latency
            if (mean > targetLatencyMs * 2f)
            {
                latencyText.color = badColor;
            }
            else if (mean > targetLatencyMs)
            {
                latencyText.color = warningColor;
            }
            else
            {
                latencyText.color = goodColor;
            }
        }
        
        // Log benchmark status
        if (isBenchmarking && benchmarkSamples.Count > 0)
        {
            CheckBenchmarkStatus(percentileValue);
        }
    }

    void CheckBenchmarkStatus(float percentileValue)
    {
        // Check if benchmark criteria met
        bool meetsTarget = percentileValue < targetLatencyMs;
        
        if (meetsTarget && benchmarkSamples.Count > 1000)
        {
            Debug.Log($"[LatencyProfiler] ✅ BENCHMARK MET: {benchmarkPercentile}th percentile = {percentileValue:F2}ms (target: <{targetLatencyMs}ms)");
        }
    }

    /// <summary>
    /// Start benchmarking mode
    /// </summary>
    public void StartBenchmark()
    {
        isBenchmarking = true;
        benchmarkSamples.Clear();
        benchmarkStartTime = Time.realtimeSinceStartup;
        Debug.Log("[LatencyProfiler] Benchmark started");
    }

    /// <summary>
    /// Stop benchmarking and get results
    /// </summary>
    public BenchmarkResults StopBenchmark()
    {
        isBenchmarking = false;
        
        if (benchmarkSamples.Count == 0)
        {
            return null;
        }
        
        float[] samples = benchmarkSamples.ToArray();
        System.Array.Sort(samples);
        
        float mean = samples.Average();
        float variance = samples.Sum(x => (x - mean) * (x - mean)) / samples.Length;
        float stdDev = Mathf.Sqrt(variance);
        
        int percentileIndex = Mathf.RoundToInt(samples.Length * (benchmarkPercentile / 100f));
        percentileIndex = Mathf.Clamp(percentileIndex, 0, samples.Length - 1);
        float percentileValue = samples[percentileIndex];
        
        BenchmarkResults results = new BenchmarkResults
        {
            sampleCount = benchmarkSamples.Count,
            meanLatency = mean,
            stdDev = stdDev,
            minLatency = samples.Min(),
            maxLatency = samples.Max(),
            percentileLatency = percentileValue,
            meetsTarget = percentileValue < targetLatencyMs,
            duration = Time.realtimeSinceStartup - benchmarkStartTime
        };
        
        Debug.Log($"[LatencyProfiler] Benchmark complete: {results.percentileLatency:F2}ms ({benchmarkPercentile}th percentile)");
        
        return results;
    }

    /// <summary>
    /// Clear history
    /// </summary>
    public void ClearHistory()
    {
        latencyHistory.Clear();
        frameTimeHistory.Clear();
        if (graphLine != null)
        {
            graphLine.positionCount = 0;
        }
    }

    [System.Serializable]
    public class BenchmarkResults
    {
        public int sampleCount;
        public float meanLatency;
        public float stdDev;
        public float minLatency;
        public float maxLatency;
        public float percentileLatency;
        public bool meetsTarget;
        public float duration;
    }
}
