using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Live Texture Painter - Real-time texture painting for data collection.
/// Allows researchers to paint dirt, mud, or oil onto objects in real-time.
/// </summary>
public class LiveTexturePainter : MonoBehaviour
{
    [Header("Painter Configuration")]
    [Tooltip("Layer mask for paintable objects")]
    public LayerMask paintLayer;

    [Tooltip("Current paint color")]
    public Color paintColor = Color.brown; // Default: mud/dirt

    [Tooltip("Brush size in world units")]
    [Range(0.1f, 5f)]
    public float brushSize = 0.5f;

    [Tooltip("Paint intensity (0-1)")]
    [Range(0f, 1f)]
    public float paintIntensity = 0.5f;

    [Tooltip("Enable painting")]
    public bool paintingEnabled = true;

    [Header("UI References")]
    [Tooltip("Paint status text")]
    public Text paintStatusText;

    [Tooltip("Color picker UI")]
    public Image colorPreview;

    [Header("Visual Feedback")]
    [Tooltip("Brush cursor GameObject")]
    public GameObject brushCursor;

    [Tooltip("Paint decal prefab")]
    public GameObject paintDecalPrefab;

    private Dictionary<Renderer, Texture2D> paintedTextures = new Dictionary<Renderer, Texture2D>();
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Camera mainCamera;
    private bool isPainting = false;

    // Preset colors for common materials
    public Color[] presetColors = new Color[]
    {
        new Color(0.4f, 0.2f, 0.1f), // Mud
        new Color(0.3f, 0.3f, 0.3f), // Dirt
        new Color(0.1f, 0.1f, 0.1f), // Oil
        new Color(0.8f, 0.6f, 0.2f), // Rust
        new Color(0.2f, 0.4f, 0.1f)  // Grass stain
    };

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        // Create brush cursor if not assigned
        if (brushCursor == null)
        {
            CreateBrushCursor();
        }

        // Update UI
        UpdateUI();

        Debug.Log("[LiveTexturePainter] Ready to paint assets.");
    }

    void Update()
    {
        // Check for painting input (Ctrl + Left Mouse)
        bool paintInput = Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

        if (paintingEnabled && paintInput)
        {
            Paint();
        }
        else
        {
            isPainting = false;
        }

        // Update brush cursor position
        UpdateBrushCursor();

        // Color selection (number keys)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetColor(presetColors[0]);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetColor(presetColors[1]);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetColor(presetColors[2]);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetColor(presetColors[3]);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetColor(presetColors[4]);
    }

    void Paint()
    {
        if (mainCamera == null) return;

        // Cast ray from camera
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && IsInPaintLayer(hit.collider.gameObject))
        {
            isPainting = true;

            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null) return;

            // Get or create painted texture
            Texture2D paintedTexture = GetOrCreatePaintedTexture(renderer);

            // Paint on texture
            PaintOnTexture(paintedTexture, hit.textureCoord, brushSize);

            // Apply texture to material
            ApplyPaintedTexture(renderer, paintedTexture);

            // Create paint decal (visual feedback)
            if (paintDecalPrefab != null)
            {
                CreatePaintDecal(hit.point, hit.normal);
            }
        }
    }

    bool IsInPaintLayer(GameObject obj)
    {
        return ((1 << obj.layer) & paintLayer.value) != 0;
    }

    Texture2D GetOrCreatePaintedTexture(Renderer renderer)
    {
        if (paintedTextures.ContainsKey(renderer))
        {
            return paintedTextures[renderer];
        }

        // Get original texture
        Texture2D originalTexture = renderer.material.mainTexture as Texture2D;
        if (originalTexture == null)
        {
            // Create default texture if none exists
            originalTexture = Texture2D.whiteTexture;
        }

        // Create copy for painting
        Texture2D paintedTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        paintedTexture.SetPixels(originalTexture.GetPixels());
        paintedTexture.Apply();

        // Store original material
        if (!originalMaterials.ContainsKey(renderer))
        {
            originalMaterials[renderer] = renderer.material;
        }

        // Create new material instance
        Material newMaterial = new Material(renderer.material);
        renderer.material = newMaterial;

        paintedTextures[renderer] = paintedTexture;
        return paintedTexture;
    }

    void PaintOnTexture(Texture2D texture, Vector2 uv, float size)
    {
        // Convert UV to pixel coordinates
        int x = Mathf.FloorToInt(uv.x * texture.width);
        int y = Mathf.FloorToInt(uv.y * texture.height);
        int radius = Mathf.FloorToInt(size * texture.width / 10f); // Scale brush size

        // Paint circular brush
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int px = x + dx;
                int py = y + dy;

                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance <= radius)
                    {
                        // Get current pixel color
                        Color currentColor = texture.GetPixel(px, py);

                        // Blend with paint color
                        float blendFactor = (1.0f - distance / radius) * paintIntensity;
                        Color blendedColor = Color.Lerp(currentColor, paintColor, blendFactor);

                        texture.SetPixel(px, py, blendedColor);
                    }
                }
            }
        }

        texture.Apply();
    }

    void ApplyPaintedTexture(Renderer renderer, Texture2D texture)
    {
        renderer.material.mainTexture = texture;
    }

    void CreatePaintDecal(Vector3 position, Vector3 normal)
    {
        if (paintDecalPrefab == null) return;

        // Instantiate decal
        GameObject decal = Instantiate(paintDecalPrefab, position, Quaternion.LookRotation(normal));
        decal.transform.localScale = Vector3.one * brushSize;

        // Set decal color
        Renderer decalRenderer = decal.GetComponent<Renderer>();
        if (decalRenderer != null)
        {
            decalRenderer.material.color = paintColor;
        }

        // Auto-destroy after delay
        Destroy(decal, 5f);
    }

    void CreateBrushCursor()
    {
        brushCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        brushCursor.name = "BrushCursor";
        brushCursor.transform.localScale = Vector3.one * brushSize;
        brushCursor.GetComponent<Renderer>().material.color = new Color(paintColor.r, paintColor.g, paintColor.b, 0.5f);
        brushCursor.GetComponent<Collider>().enabled = false;
        brushCursor.SetActive(false);
    }

    void UpdateBrushCursor()
    {
        if (brushCursor == null || mainCamera == null) return;

        // Show cursor when painting is enabled
        bool showCursor = paintingEnabled && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

        if (showCursor)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && IsInPaintLayer(hit.collider.gameObject))
            {
                brushCursor.SetActive(true);
                brushCursor.transform.position = hit.point + hit.normal * 0.01f;
                brushCursor.transform.rotation = Quaternion.LookRotation(hit.normal);
                brushCursor.transform.localScale = Vector3.one * brushSize;
            }
            else
            {
                brushCursor.SetActive(false);
            }
        }
        else
        {
            brushCursor.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (paintStatusText != null)
        {
            paintStatusText.text = paintingEnabled ? $"PAINTING: {paintColor} | Size: {brushSize:F1}" : "PAINTING: DISABLED";
        }

        if (colorPreview != null)
        {
            colorPreview.color = paintColor;
        }
    }

    /// <summary>
    /// Set paint color
    /// </summary>
    public void SetColor(Color color)
    {
        paintColor = color;
        UpdateUI();
        Debug.Log($"[LiveTexturePainter] Color set to {color}");
    }

    /// <summary>
    /// Set brush size
    /// </summary>
    public void SetBrushSize(float size)
    {
        brushSize = Mathf.Clamp(size, 0.1f, 5f);
        UpdateUI();
    }

    /// <summary>
    /// Clear all paint from object
    /// </summary>
    public void ClearPaint(Renderer renderer)
    {
        if (originalMaterials.ContainsKey(renderer))
        {
            renderer.material = originalMaterials[renderer];
        }

        if (paintedTextures.ContainsKey(renderer))
        {
            Destroy(paintedTextures[renderer]);
            paintedTextures.Remove(renderer);
        }
    }

    /// <summary>
    /// Clear all paint from all objects
    /// </summary>
    public void ClearAllPaint()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }

        foreach (var texture in paintedTextures.Values)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        paintedTextures.Clear();
        Debug.Log("[LiveTexturePainter] All paint cleared");
    }

    /// <summary>
    /// Export painted texture to file
    /// </summary>
    public void ExportTexture(Renderer renderer, string filePath)
    {
        if (!paintedTextures.ContainsKey(renderer))
        {
            Debug.LogWarning("[LiveTexturePainter] No painted texture found for this object");
            return;
        }

        Texture2D texture = paintedTextures[renderer];
        byte[] pngData = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, pngData);
        Debug.Log($"[LiveTexturePainter] Texture exported to {filePath}");
    }
}
