using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Theme Helper - Provides static methods for consistent Palantir/Tesla color usage.
/// Replaces all purple/magenta/cyan with professional NASA theme colors.
/// </summary>
public static class UIThemeHelper
{
    // Palantir/Tesla Professional Color Palette
    public static class Colors
    {
        // Primary Colors
        public static Color AppleBlue => new Color(0f, 0.478f, 1f, 1f);
        public static Color PalantirBlue => new Color(0.2f, 0.6f, 1f, 1f);
        public static Color TeslaGreen => new Color(0f, 0.8f, 0.4f, 1f);
        public static Color TeslaOrange => new Color(1f, 0.6f, 0f, 1f);

        // Background Colors
        public static Color CrispyWhite => new Color(1f, 1f, 1f, 1f);
        public static Color GalaxyBlack => new Color(0.05f, 0.05f, 0.1f, 1f);

        // Text Colors
        public static Color TextLight => new Color(1f, 1f, 1f, 1f); // Crispy White
        public static Color TextDark => new Color(0.05f, 0.05f, 0.1f, 1f); // Galaxy Black

        // Status Colors
        public static Color Success => new Color(0f, 0.8f, 0.4f, 1f); // Tesla Green
        public static Color Warning => new Color(1f, 0.6f, 0f, 1f); // Tesla Orange
        public static Color Error => new Color(1f, 0.2f, 0.2f, 1f); // Error Red
        public static Color Info => new Color(0.2f, 0.6f, 1f, 1f); // Palantir Blue (replaces cyan)

        // Glassmorphism
        public static Color GlassLight => new Color(1f, 1f, 1f, 0.15f);
        public static Color GlassDark => new Color(0f, 0f, 0f, 0.3f);
        public static Color GlassBorder => new Color(1f, 1f, 1f, 0.3f);

        // Legacy Color Replacements (for migration)
        public static Color ReplaceMagenta => AppleBlue; // Replace magenta with Apple Blue
        public static Color ReplaceCyan => PalantirBlue; // Replace cyan with Palantir Blue
        public static Color ReplacePurple => AppleBlue; // Replace purple with Apple Blue
    }

    /// <summary>
    /// Get theme-aware color based on current theme
    /// </summary>
    public static Color GetThemeColor(ThemeManager.Theme theme, string colorName)
    {
        ThemeManager themeManager = Object.FindObjectOfType<ThemeManager>();
        if (themeManager != null)
        {
            return themeManager.GetColor(colorName);
        }

        // Fallback
        return theme == ThemeManager.Theme.Light ? Colors.TextDark : Colors.TextLight;
    }

    /// <summary>
    /// Apply glassmorphism to an image
    /// </summary>
    public static void ApplyGlassmorphism(Image img, bool isDark = false)
    {
        if (img == null) return;

        Color glassColor = isDark ? Colors.GlassDark : Colors.GlassLight;
        img.color = glassColor;

        // Add subtle border effect
        // In production, use custom shader for true glassmorphism
    }

    /// <summary>
    /// Apply professional typography to text
    /// </summary>
    public static void ApplyTypography(Text text, bool isBold = false)
    {
        if (text == null) return;

        // Ensure minimum readable size
        if (text.fontSize < 12)
        {
            text.fontSize = 12;
        }

        // Professional font styling
        text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
    }

    /// <summary>
    /// Replace legacy purple/magenta/cyan colors with professional theme
    /// </summary>
    public static Color ReplaceLegacyColor(Color originalColor)
    {
        // Check if color is purple/magenta
        if (IsPurple(originalColor))
        {
            return Colors.AppleBlue;
        }

        // Check if color is cyan
        if (IsCyan(originalColor))
        {
            return Colors.PalantirBlue;
        }

        return originalColor;
    }

    static bool IsPurple(Color color)
    {
        float r = color.r;
        float g = color.g;
        float b = color.b;
        return (r > 0.5f && b > 0.5f && g < 0.5f) || (r > 0.7f && b > 0.7f);
    }

    static bool IsCyan(Color color)
    {
        float g = color.g;
        float b = color.b;
        float r = color.r;
        return (g > 0.7f && b > 0.7f && r < 0.3f);
    }
}
