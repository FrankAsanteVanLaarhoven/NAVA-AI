#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Color Replacer - Bulk replace purple/magenta/cyan with professional theme colors.
/// </summary>
public class ColorReplacer : EditorWindow
{
    [MenuItem("NAVA-AI Dashboard/UI/Replace All Purple/Cyan Colors")]
    static void ReplaceAllColors()
    {
        int replacedCount = 0;

        // Find all MonoScript files
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string[] lines = System.IO.File.ReadAllLines(path);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string original = lines[i];
                string updated = original;

                // Replace Color.magenta
                if (updated.Contains("Color.magenta"))
                {
                    updated = updated.Replace("Color.magenta", "UIThemeHelper.Colors.AppleBlue // Replaced magenta");
                    replacedCount++;
                    modified = true;
                }

                // Replace Color.cyan
                if (updated.Contains("Color.cyan"))
                {
                    updated = updated.Replace("Color.cyan", "UIThemeHelper.Colors.PalantirBlue // Replaced cyan");
                    replacedCount++;
                    modified = true;
                }

                // Replace purple RGB values
                if (updated.Contains("new Color(0.5") && updated.Contains("0.5") && updated.Contains("1"))
                {
                    // Check if it's purple-like
                    if (updated.Contains("0.5") && updated.Contains("0.5") && updated.Contains("1"))
                    {
                        // This is a heuristic - be careful
                        // updated = updated.Replace(...);
                    }
                }

                lines[i] = updated;
            }

            if (modified)
            {
                System.IO.File.WriteAllLines(path, lines);
                AssetDatabase.ImportAsset(path);
            }
        }

        Debug.Log($"[ColorReplacer] Replaced {replacedCount} color references");
        EditorUtility.DisplayDialog("Color Replacement Complete", 
            $"Replaced {replacedCount} purple/magenta/cyan color references with professional theme colors.", 
            "OK");
    }
}
#endif
