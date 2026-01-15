using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Theme Manager - Palantir/Tesla Quality UI System.
/// Provides light/dark theme toggle with galaxy black/crispy white colors,
/// Palantir/Apple/Tesla typography, and glassmorphism effects.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public enum Theme
    {
        Light,
        Dark
    }

    [System.Serializable]
    public class ThemeColors
    {
        public Color background;
        public Color text;
        public Color primary;
        public Color secondary;
        public Color accent;
        public Color success;
        public Color warning;
        public Color error;
        public Color glassBackground;
        public Color glassBorder;
    }

    [Header("Current Theme")]
    [Tooltip("Current theme mode")]
    public Theme currentTheme = Theme.Light;

    [Header("Light Theme (Crispy White Background)")]
    public ThemeColors lightTheme = new ThemeColors
    {
        background = new Color(1f, 1f, 1f, 1f), // Crispy White
        text = new Color(0.05f, 0.05f, 0.1f, 1f), // Galaxy Black
        primary = new Color(0f, 0.478f, 1f, 1f), // Apple Blue
        secondary = new Color(0.2f, 0.2f, 0.25f, 1f),
        accent = new Color(0f, 0.8f, 0.4f, 1f), // Tesla Green
        success = new Color(0f, 0.8f, 0.4f, 1f),
        warning = new Color(1f, 0.6f, 0f, 1f),
        error = new Color(1f, 0.2f, 0.2f, 1f),
        glassBackground = new Color(1f, 1f, 1f, 0.15f), // Glassmorphism
        glassBorder = new Color(1f, 1f, 1f, 0.3f)
    };

    [Header("Dark Theme (Galaxy Black Background)")]
    public ThemeColors darkTheme = new ThemeColors
    {
        background = new Color(0.05f, 0.05f, 0.1f, 1f), // Galaxy Black
        text = new Color(1f, 1f, 1f, 1f), // Crispy White
        primary = new Color(0.2f, 0.6f, 1f, 1f), // Palantir Blue
        secondary = new Color(0.3f, 0.3f, 0.35f, 1f),
        accent = new Color(0f, 0.9f, 0.5f, 1f), // Tesla Green
        success = new Color(0f, 0.9f, 0.5f, 1f),
        warning = new Color(1f, 0.7f, 0f, 1f),
        error = new Color(1f, 0.3f, 0.3f, 1f),
        glassBackground = new Color(0f, 0f, 0f, 0.3f), // Glassmorphism
        glassBorder = new Color(1f, 1f, 1f, 0.2f)
    };

    [Header("UI References")]
    [Tooltip("Main canvas background")]
    public Image canvasBackground;

    [Tooltip("All text elements to update")]
    public Text[] allTextElements;

    [Tooltip("All image elements for glassmorphism")]
    public Image[] allImageElements;

    [Tooltip("Theme toggle button")]
    public Button themeToggleButton;

    [Tooltip("Theme toggle text")]
    public Text themeToggleText;

    private List<Text> registeredTexts = new List<Text>();
    private List<Image> registeredImages = new List<Image>();

    void Start()
    {
        // Auto-find UI elements if not assigned
        if (allTextElements == null || allTextElements.Length == 0)
        {
            allTextElements = FindObjectsOfType<Text>();
        }

        if (allImageElements == null || allImageElements.Length == 0)
        {
            allImageElements = FindObjectsOfType<Image>();
        }

        // Register all UI elements
        RegisterUIElements();

        // Setup theme toggle
        if (themeToggleButton != null)
        {
            themeToggleButton.onClick.AddListener(ToggleTheme);
        }

        // Apply initial theme
        ApplyTheme(currentTheme);
    }

    void RegisterUIElements()
    {
        registeredTexts.Clear();
        registeredImages.Clear();

        if (allTextElements != null)
        {
            registeredTexts.AddRange(allTextElements);
        }

        if (allImageElements != null)
        {
            registeredImages.AddRange(allImageElements);
        }

        // Also find dynamically
        registeredTexts.AddRange(FindObjectsOfType<Text>());
        registeredImages.AddRange(FindObjectsOfType<Image>());
    }

    /// <summary>
    /// Toggle between light and dark theme
    /// </summary>
    public void ToggleTheme()
    {
        currentTheme = currentTheme == Theme.Light ? Theme.Dark : Theme.Light;
        ApplyTheme(currentTheme);
    }

    /// <summary>
    /// Set theme explicitly
    /// </summary>
    public void SetTheme(Theme theme)
    {
        currentTheme = theme;
        ApplyTheme(theme);
    }

    /// <summary>
    /// Apply theme to all UI elements
    /// </summary>
    public void ApplyTheme(Theme theme)
    {
        ThemeColors colors = theme == Theme.Light ? lightTheme : darkTheme;

        // Update canvas background
        if (canvasBackground != null)
        {
            canvasBackground.color = colors.background;
        }

        // Update all text elements
        foreach (Text text in registeredTexts)
        {
            if (text == null) continue;

            // Skip theme toggle text (it has special handling)
            if (text == themeToggleText) continue;

            // Apply text color
            text.color = colors.text;

            // Apply professional typography
            ApplyTypography(text);
        }

        // Update all image elements (glassmorphism)
        foreach (Image img in registeredImages)
        {
            if (img == null) continue;

            // Skip canvas background
            if (img == canvasBackground) continue;

            // Apply glassmorphism to panels and buttons
            if (img.GetComponent<Button>() != null || img.name.Contains("Panel") || img.name.Contains("Modal"))
            {
                ApplyGlassmorphism(img, colors);
            }
        }

        // Update theme toggle button text
        if (themeToggleText != null)
        {
            themeToggleText.text = theme == Theme.Light ? "🌙 DARK" : "☀️ LIGHT";
            themeToggleText.color = colors.text;
        }

        Debug.Log($"[ThemeManager] Applied {theme} theme");
    }

    void ApplyTypography(Text text)
    {
        // Palantir/Apple/Tesla typography
        if (text.font == null)
        {
            // Use system font or default
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Professional font styling
        if (text.fontSize < 12)
        {
            text.fontSize = 12; // Minimum readable size
        }

        // Crisp text rendering
        text.fontStyle = FontStyle.Normal;
    }

    void ApplyGlassmorphism(Image img, ThemeColors colors)
    {
        // Glassmorphism effect: translucent with blur
        Color glassColor = colors.glassBackground;
        glassColor.a = 0.15f; // Translucent

        img.color = glassColor;

        // Add border effect (simulated with outline or shadow)
        // In production, use shader for true glassmorphism
        if (img.material == null)
        {
            Material glassMaterial = new Material(Shader.Find("UI/Default"));
            glassMaterial.SetFloat("_Glossiness", 0.5f);
            img.material = glassMaterial;
        }
    }

    /// <summary>
    /// Get current theme colors
    /// </summary>
    public ThemeColors GetCurrentColors()
    {
        return currentTheme == Theme.Light ? lightTheme : darkTheme;
    }

    /// <summary>
    /// Get color by name
    /// </summary>
    public Color GetColor(string colorName)
    {
        ThemeColors colors = GetCurrentColors();

        switch (colorName.ToLower())
        {
            case "background": return colors.background;
            case "text": return colors.text;
            case "primary": return colors.primary;
            case "secondary": return colors.secondary;
            case "accent": return colors.accent;
            case "success": return colors.success;
            case "warning": return colors.warning;
            case "error": return colors.error;
            case "glass": return colors.glassBackground;
            default: return colors.text;
        }
    }
}
