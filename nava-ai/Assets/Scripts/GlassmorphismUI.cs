using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Glassmorphism UI - Translucent modals, buttons, and widgets.
/// Creates Palantir/Tesla quality glassmorphism effects with blur and transparency.
/// </summary>
public class GlassmorphismUI : MonoBehaviour
{
    [Header("Glassmorphism Settings")]
    [Tooltip("Blur intensity")]
    [Range(0f, 10f)]
    public float blurIntensity = 5f;

    [Tooltip("Transparency level")]
    [Range(0f, 1f)]
    public float transparency = 0.15f;

    [Tooltip("Border width")]
    [Range(0f, 5f)]
    public float borderWidth = 1f;

    [Tooltip("Border color")]
    public Color borderColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("UI Elements")]
    [Tooltip("Elements to apply glassmorphism")]
    public Image[] glassElements;

    [Tooltip("Buttons to style")]
    public Button[] glassButtons;

    private List<Image> allGlassElements = new List<Image>();
    private Material glassMaterial;

    void Start()
    {
        // Create glassmorphism material
        CreateGlassMaterial();

        // Auto-find glass elements
        if (glassElements == null || glassElements.Length == 0)
        {
            FindGlassElements();
        }

        // Apply glassmorphism
        ApplyGlassmorphism();
    }

    void CreateGlassMaterial()
    {
        // Create material with glassmorphism shader
        // Note: Unity doesn't have built-in glassmorphism, so we simulate it
        glassMaterial = new Material(Shader.Find("UI/Default"));
        
        // In production, use custom shader for true glassmorphism
        // For now, we use alpha transparency and visual effects
    }

    void FindGlassElements()
    {
        allGlassElements.Clear();

        // Find panels, modals, cards
        Image[] allImages = FindObjectsOfType<Image>();
        foreach (Image img in allImages)
        {
            if (img.name.Contains("Panel") || 
                img.name.Contains("Modal") || 
                img.name.Contains("Card") ||
                img.name.Contains("Widget"))
            {
                allGlassElements.Add(img);
            }
        }

        // Add explicitly assigned elements
        if (glassElements != null)
        {
            allGlassElements.AddRange(glassElements);
        }
    }

    void ApplyGlassmorphism()
    {
        ThemeManager themeManager = FindObjectOfType<ThemeManager>();
        Color glassColor = themeManager != null ? themeManager.GetColor("glass") : new Color(1f, 1f, 1f, transparency);

        // Apply to all glass elements
        foreach (Image img in allGlassElements)
        {
            if (img == null) continue;

            ApplyGlassEffect(img, glassColor);
        }

        // Apply to buttons
        foreach (Button btn in glassButtons)
        {
            if (btn == null) continue;

            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
            {
                ApplyGlassEffect(btnImage, glassColor);
            }
        }
    }

    void ApplyGlassEffect(Image img, Color baseColor)
    {
        // Set translucent background
        Color glassBg = baseColor;
        glassBg.a = transparency;
        img.color = glassBg;

        // Apply material
        if (glassMaterial != null)
        {
            img.material = glassMaterial;
        }

        // Add border (simulated with outline)
        // In production, use Outline component or custom shader
    }

    void Update()
    {
        // Re-apply if theme changes
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            ApplyGlassmorphism();
        }
    }
}
