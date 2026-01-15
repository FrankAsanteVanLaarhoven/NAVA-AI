using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;

/// <summary>
/// Neural Saliency Overlay - Visualizes what the VLA (Vision-Language-Action) policy is attending to.
/// Shows confidence heatmaps and attention maps to diagnose AI decision-making.
/// </summary>
public class VlaSaliencyOverlay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("RawImage to display saliency heatmap")]
    public RawImage saliencyMap;
    
    [Tooltip("Text showing average confidence")]
    public Text confidenceText;
    
    [Header("3D Visualization")]
    [Tooltip("Objects to overlay saliency on (will be colorized by confidence)")]
    public GameObject[] targetObjects;
    
    [Tooltip("Prefab for attention reticle visualization")]
    public GameObject reticlePrefab;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for saliency/attention map")]
    public string saliencyTopic = "/vla/attention_map";
    
    [Tooltip("ROS2 topic for confidence scores (alternative)")]
    public string confidenceTopic = "/vla/confidence";
    
    [Header("Visualization")]
    [Tooltip("Heat gradient texture (blue=low, red=high confidence)")]
    public Texture2D heatGradient;
    
    [Tooltip("Minimum confidence to show reticle")]
    public float reticleThreshold = 0.8f;
    
    [Tooltip("Color for low confidence")]
    public Color lowConfidenceColor = Color.red;
    
    [Tooltip("Color for high confidence")]
    public Color highConfidenceColor = Color.green;
    
    private ROSConnection ros;
    private Texture2D saliencyTexture;
    private List<GameObject> activeReticles = new List<GameObject>();
    private float currentAverageConfidence = 0.8f; // Default confidence
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private List<float> confidenceHistory = new List<float>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to saliency map (as Image message)
        ros.Subscribe<ImageMsg>(saliencyTopic, UpdateSaliencyFromImage);
        
        // Subscribe to confidence scores (as Float32MultiArray)
        ros.Subscribe<Float32MultiArrayMsg>(confidenceTopic, UpdateConfidenceScores);
        
        // Create heat gradient if not assigned
        if (heatGradient == null)
        {
            CreateHeatGradient();
        }
        
        // Store original materials
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    originalMaterials[obj] = renderer.material;
                }
            }
        }
        
        Debug.Log($"[VlaSaliencyOverlay] Subscribed to {saliencyTopic} and {confidenceTopic}");
    }

    void UpdateSaliencyFromImage(ImageMsg msg)
    {
        if (saliencyMap == null) return;
        
        try
        {
            // Create texture from image data
            if (saliencyTexture == null || 
                saliencyTexture.width != msg.width || 
                saliencyTexture.height != msg.height)
            {
                if (saliencyTexture != null) Destroy(saliencyTexture);
                saliencyTexture = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGB24, false);
            }
            
            // Load image data
            if (msg.encoding == "rgb8" || msg.encoding == "RGB8")
            {
                saliencyTexture.LoadRawTextureData(msg.data);
            }
            else if (msg.encoding == "mono8" || msg.encoding == "MONO8")
            {
                // Convert grayscale to RGB
                Color[] colors = new Color[msg.data.Length];
                for (int i = 0; i < msg.data.Length; i++)
                {
                    float intensity = msg.data[i] / 255f;
                    colors[i] = new Color(intensity, intensity, intensity);
                }
                saliencyTexture.SetPixels(colors);
            }
            
            saliencyTexture.Apply();
            saliencyMap.texture = saliencyTexture;
            
            // Calculate average confidence
            CalculateAverageConfidence(saliencyTexture);
            
            // Update 3D object colors
            UpdateObjectColors();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VlaSaliencyOverlay] Error processing saliency image: {e.Message}");
        }
    }

    void UpdateConfidenceScores(Float32MultiArrayMsg msg)
    {
        if (msg.data == null || msg.data.Length == 0) return;
        
        // Calculate average from confidence array
        float sum = 0f;
        foreach (float val in msg.data)
        {
            sum += val;
        }
        currentAverageConfidence = sum / msg.data.Length;
        
        UpdateUI();
        UpdateObjectColors();
    }

    void CalculateAverageConfidence(Texture2D texture)
    {
        if (texture == null) return;
        
        Color[] pixels = texture.GetPixels();
        float sum = 0f;
        
        foreach (Color pixel in pixels)
        {
            // Use grayscale intensity as confidence
            sum += pixel.grayscale;
        }
        
        currentAverageConfidence = sum / pixels.Length;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (confidenceText != null)
        {
            confidenceText.text = $"Confidence: {currentAverageConfidence:P1}";
            confidenceText.color = Color.Lerp(lowConfidenceColor, highConfidenceColor, currentAverageConfidence);
        }
    }

    void UpdateObjectColors()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) continue;
            
            // Create or get material
            Material mat = renderer.material;
            if (originalMaterials.ContainsKey(obj))
            {
                mat = new Material(originalMaterials[obj]);
                renderer.material = mat;
            }
            
            // Set emission color based on confidence
            Color emissionColor = Color.Lerp(lowConfidenceColor, highConfidenceColor, currentAverageConfidence);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * currentAverageConfidence);
            
            // Show reticle if confidence is high
            if (currentAverageConfidence > reticleThreshold)
            {
                ShowReticle(obj.transform.position + Vector3.up * 1.5f);
            }
        }
    }

    void ShowReticle(Vector3 position)
    {
        // Remove old reticles
        ClearReticles();
        
        // Create new reticle
        GameObject reticle;
        if (reticlePrefab != null)
        {
            reticle = Instantiate(reticlePrefab, position, Quaternion.identity);
        }
        else
        {
            // Create simple reticle
            reticle = new GameObject("AttentionReticle");
            reticle.transform.position = position;
            
            // Create crosshair
            LineRenderer lr = reticle.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.color = Color.yellow;
            lr.positionCount = 4;
            
            float size = 0.5f;
            lr.SetPosition(0, position + Vector3.left * size);
            lr.SetPosition(1, position + Vector3.right * size);
            lr.SetPosition(2, position + Vector3.up * size);
            lr.SetPosition(3, position + Vector3.down * size);
        }
        
        activeReticles.Add(reticle);
    }

    void ClearReticles()
    {
        foreach (GameObject reticle in activeReticles)
        {
            if (reticle != null) Destroy(reticle);
        }
        activeReticles.Clear();
    }

    void CreateHeatGradient()
    {
        heatGradient = new Texture2D(256, 1, TextureFormat.RGB24, false);
        Color[] colors = new Color[256];
        
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            colors[i] = Color.Lerp(Color.blue, Color.red, t);
        }
        
        heatGradient.SetPixels(colors);
        heatGradient.Apply();
    }

    /// <summary>
    /// Get average confidence for intent calculation
    /// </summary>
    public float GetAverageConfidence()
    {
        return currentAverageConfidence;
    }

    void OnDestroy()
    {
        ClearReticles();
        if (saliencyTexture != null) Destroy(saliencyTexture);
    }
}
