using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Evolution HUD - Visualizes the "Training Loop" and "Ironclad" dataset growth.
/// Shows the Ironclad dataset growing in real-time and the VLA model adapting.
/// </summary>
public class EvolutionHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("RawImage for P-score distribution heatmap")]
    public RawImage dataHeatmap;
    
    [Tooltip("Text displaying generation/datapoint count")]
    public Text generationCount;
    
    [Tooltip("Text displaying training statistics")]
    public Text trainingStatsText;
    
    [Tooltip("Text displaying success rate")]
    public Text successRateText;
    
    [Header("Component References")]
    [Tooltip("Reference to consciousness rigor for P-score")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to data logger")]
    public IroncladDataLogger dataLogger;
    
    [Tooltip("Reference to adaptive VLA manager")]
    public AdaptiveVlaManager adaptiveVla;
    
    [Header("Visualization Settings")]
    [Tooltip("Heatmap update rate (Hz)")]
    public float heatmapUpdateRate = 1f;
    
    [Tooltip("Statistics update rate (Hz)")]
    public float statsUpdateRate = 0.5f;
    
    [Header("Heatmap Settings")]
    [Tooltip("Heatmap texture size")]
    public int heatmapSize = 64;
    
    private Texture2D heatmapTexture;
    private float lastHeatmapUpdate = 0f;
    private float lastStatsUpdate = 0f;
    private float heatmapInterval;
    private float statsInterval;
    private Queue<float> pScoreHistory = new Queue<float>();
    private int maxHistorySize = 100;

    void Start()
    {
        heatmapInterval = 1f / heatmapUpdateRate;
        statsInterval = 1f / statsUpdateRate;
        
        // Get component references if not assigned
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (dataLogger == null)
        {
            dataLogger = GetComponent<IroncladDataLogger>();
        }
        
        if (adaptiveVla == null)
        {
            adaptiveVla = GetComponent<AdaptiveVlaManager>();
        }
        
        // Create heatmap texture
        CreateHeatmapTexture();
        
        Debug.Log("[EvolutionHUD] Initialized - Training visualization ready");
    }

    void Update()
    {
        // Update heatmap
        if (Time.time - lastHeatmapUpdate >= heatmapInterval)
        {
            UpdateHeatmap();
            lastHeatmapUpdate = Time.time;
        }
        
        // Update statistics
        if (Time.time - lastStatsUpdate >= statsInterval)
        {
            UpdateStatistics();
            lastStatsUpdate = Time.time;
        }
    }

    void CreateHeatmapTexture()
    {
        if (dataHeatmap == null) return;
        
        heatmapTexture = new Texture2D(heatmapSize, heatmapSize, TextureFormat.RGB24, false);
        heatmapTexture.filterMode = FilterMode.Bilinear;
        
        // Initialize with neutral color
        Color[] pixels = new Color[heatmapSize * heatmapSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.gray;
        }
        heatmapTexture.SetPixels(pixels);
        heatmapTexture.Apply();
        
        dataHeatmap.texture = heatmapTexture;
    }

    void UpdateHeatmap()
    {
        if (dataHeatmap == null || heatmapTexture == null) return;
        
        // Get current P-score
        float pScore = GetCurrentPScore();
        
        // Add to history
        pScoreHistory.Enqueue(pScore);
        if (pScoreHistory.Count > maxHistorySize)
        {
            pScoreHistory.Dequeue();
        }
        
        // Update heatmap color based on P-score
        // Green = High P (Success), Red = Low P (Failure)
        Color heatColor = Color.Lerp(Color.red, Color.green, pScore / 100.0f);
        
        // Create gradient heatmap
        Color[] pixels = new Color[heatmapSize * heatmapSize];
        float[] historyArray = pScoreHistory.ToArray();
        
        for (int y = 0; y < heatmapSize; y++)
        {
            for (int x = 0; x < heatmapSize; x++)
            {
                // Map position to history index
                int historyIndex = (int)((float)x / heatmapSize * historyArray.Length);
                if (historyIndex < historyArray.Length)
                {
                    float score = historyArray[historyIndex];
                    Color c = Color.Lerp(Color.red, Color.green, score / 100.0f);
                    pixels[y * heatmapSize + x] = c;
                }
                else
                {
                    pixels[y * heatmapSize + x] = Color.gray;
                }
            }
        }
        
        heatmapTexture.SetPixels(pixels);
        heatmapTexture.Apply();
        
        // Update main heatmap color
        dataHeatmap.color = heatColor;
    }

    float GetCurrentPScore()
    {
        if (consciousnessRigor != null)
        {
            return consciousnessRigor.GetPScore();
        }
        return 50f; // Default
    }

    void UpdateStatistics()
    {
        // Update generation count
        if (generationCount != null)
        {
            int datapointCount = GetDatapointCount();
            generationCount.text = $"DATAPOINTS: {datapointCount}";
        }
        
        // Update training statistics
        if (trainingStatsText != null)
        {
            StringBuilder stats = new StringBuilder();
            
            if (adaptiveVla != null)
            {
                stats.AppendLine($"Training State: {adaptiveVla.GetTrainingState()}");
                stats.AppendLine($"Confidence Bias: {adaptiveVla.GetConfidenceBias():F3}");
                stats.AppendLine($"Avg P-Score: {adaptiveVla.GetAveragePScore():F1}");
            }
            
            if (dataLogger != null)
            {
                var datasetStats = dataLogger.GetDatasetStats();
                stats.AppendLine($"Success Rate: {datasetStats.successRate:P1}");
                stats.AppendLine($"Success: {datasetStats.successCount} | Fail: {datasetStats.failureCount}");
            }
            
            trainingStatsText.text = stats.ToString();
        }
        
        // Update success rate
        if (successRateText != null && dataLogger != null)
        {
            var datasetStats = dataLogger.GetDatasetStats();
            successRateText.text = $"Success Rate: {datasetStats.successRate:P1}";
            successRateText.color = Color.Lerp(Color.red, Color.green, datasetStats.successRate);
        }
    }

    int GetDatapointCount()
    {
        if (dataLogger != null && dataLogger.datasetPath != null)
        {
            try
            {
                if (File.Exists(dataLogger.datasetPath))
                {
                    string[] lines = File.ReadAllLines(dataLogger.datasetPath);
                    return lines.Length - 1; // Subtract header
                }
            }
            catch
            {
                // File might be locked or not accessible
            }
        }
        
        return 0;
    }
}
