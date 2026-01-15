#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// UI Style Updater - Applies Palantir/Tesla quality styling to all UI elements.
/// Removes purple colors and applies professional NASA/Mission Impossible theme.
/// </summary>
public class UIStyleUpdater : EditorWindow
{
    [MenuItem("NAVA-AI Dashboard/UI/Apply Palantir/Tesla Theme")]
    static void ApplyTheme()
    {
        // Find ThemeManager
        ThemeManager themeManager = FindObjectOfType<ThemeManager>();
        if (themeManager == null)
        {
            GameObject themeObj = new GameObject("ThemeManager");
            themeManager = themeObj.AddComponent<ThemeManager>();
        }

        // Find all UI elements
        Text[] allTexts = FindObjectsOfType<Text>();
        Image[] allImages = FindObjectsOfType<Image>();
        Button[] allButtons = FindObjectsOfType<Button>();

        // Remove purple colors and apply new theme
        int updatedCount = 0;

        foreach (Text text in allTexts)
        {
            if (text == null) continue;

            // Remove purple
            if (IsPurple(text.color))
            {
                text.color = new Color(0.05f, 0.05f, 0.1f, 1f); // Galaxy Black
                updatedCount++;
            }

            // Apply typography
            text.fontSize = Mathf.Max(text.fontSize, 12);
        }

        foreach (Image img in allImages)
        {
            if (img == null) continue;

            // Remove purple backgrounds
            if (IsPurple(img.color))
            {
                img.color = new Color(1f, 1f, 1f, 0.15f); // Glassmorphism
                updatedCount++;
            }
        }

        foreach (Button btn in allButtons)
        {
            if (btn == null) continue;

            ColorBlock colors = btn.colors;
            
            // Remove purple from button colors
            if (IsPurple(colors.normalColor))
            {
                colors.normalColor = new Color(0f, 0.478f, 1f, 1f); // Apple Blue
                colors.highlightedColor = new Color(0.2f, 0.6f, 1f, 1f);
                colors.pressedColor = new Color(0f, 0.3f, 0.8f, 1f);
                colors.selectedColor = new Color(0f, 0.478f, 1f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                
                btn.colors = colors;
                updatedCount++;
            }
        }

        Debug.Log($"[UIStyleUpdater] Updated {updatedCount} UI elements with Palantir/Tesla theme");
        EditorUtility.DisplayDialog("Theme Applied", 
            $"Updated {updatedCount} UI elements.\n\nRemoved all purple colors.\nApplied professional Palantir/Tesla styling.", 
            "OK");
    }

    static bool IsPurple(Color color)
    {
        // Check if color is purple/magenta
        float r = color.r;
        float g = color.g;
        float b = color.b;

        // Purple: high red and blue, low green
        return (r > 0.5f && b > 0.5f && g < 0.5f) || 
               (r > 0.7f && b > 0.7f); // Magenta
    }

    [MenuItem("NAVA-AI Dashboard/UI/Create Theme Manager")]
    static void CreateThemeManager()
    {
        GameObject themeObj = new GameObject("ThemeManager");
        ThemeManager themeManager = themeObj.AddComponent<ThemeManager>();
        
        // Find canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            themeObj.transform.SetParent(canvas.transform, false);
        }

        Selection.activeGameObject = themeObj;
        Debug.Log("[UIStyleUpdater] Created ThemeManager");
    }
}
#endif
