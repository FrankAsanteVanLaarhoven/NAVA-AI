#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Asset Upload Helper - Upload files from Desktop to Unity Dashboard
/// Menu: NAVA-AI Dashboard > Tools > Upload Assets from Desktop
/// </summary>
public class AssetUploadHelper : EditorWindow
{
    private string selectedPath = "";
    private Vector2 scrollPosition;
    private bool useStreaming = false;
    private string serverUrl = "http://127.0.0.1:8080";

    [MenuItem("NAVA-AI Dashboard/Tools/Upload Assets from Desktop")]
    static void ShowWindow()
    {
        GetWindow<AssetUploadHelper>("Upload Assets").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Upload Assets to Unity Dashboard", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Desktop Path
        EditorGUILayout.LabelField("Desktop Path:", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop));
        GUILayout.Space(10);

        // Select Files/Folder
        if (GUILayout.Button("Select Files from Desktop", GUILayout.Height(30)))
        {
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string[] files = EditorUtility.OpenFilePanel(
                "Select Files to Upload",
                desktopPath,
                ""
            );

            if (files.Length > 0)
            {
                UploadFiles(files);
            }
        }

        if (GUILayout.Button("Select Folder from Desktop", GUILayout.Height(30)))
        {
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string folder = EditorUtility.OpenFolderPanel(
                "Select Folder to Upload",
                desktopPath,
                ""
            );

            if (!string.IsNullOrEmpty(folder))
            {
                UploadFolder(folder);
            }
        }

        GUILayout.Space(20);

        // Streaming Options
        EditorGUILayout.LabelField("Streaming Options", EditorStyles.boldLabel);
        useStreaming = EditorGUILayout.Toggle("Use Streaming (for large files)", useStreaming);
        serverUrl = EditorGUILayout.TextField("Server URL:", serverUrl);

        GUILayout.Space(10);

        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Copy Entire Desktop to Assets (Small Files Only)", GUILayout.Height(30)))
        {
            CopyDesktopToAssets();
        }

        GUILayout.Space(20);

        // Instructions
        EditorGUILayout.HelpBox(
            "Instructions:\n\n" +
            "• Small files (< 100 MB): Use direct copy\n" +
            "• Large files (> 100 MB): Use streaming\n" +
            "• Start Rust server: cd nav_lambda_server && cargo run --release\n" +
            "• Monitor memory: Tools > Memory Management",
            MessageType.Info
        );
    }

    void UploadFiles(string[] filePaths)
    {
        int uploaded = 0;
        int skipped = 0;
        int streaming = 0;

        foreach (string filePath in filePaths)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long fileSizeMB = fileInfo.Length / (1024 * 1024);

            if (fileSizeMB > 100 || useStreaming)
            {
                // Large file - use streaming
                Debug.Log($"[Upload] Large file detected ({fileSizeMB} MB): {fileInfo.Name}");
                Debug.Log($"[Upload] Copy to server: cp '{filePath}' nav_lambda_server/Assets/");
                Debug.Log($"[Upload] Then use StreamingAssetLoader in Unity to download");
                streaming++;
            }
            else
            {
                // Small file - direct copy
                try
                {
                    string destPath = Path.Combine(Application.dataPath, fileInfo.Name);
                    File.Copy(filePath, destPath, true);
                    AssetDatabase.Refresh();
                    Debug.Log($"[Upload] Copied {fileInfo.Name} to Assets ({fileSizeMB} MB)");
                    uploaded++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Upload] Failed to copy {fileInfo.Name}: {e.Message}");
                    skipped++;
                }
            }
        }

        EditorUtility.DisplayDialog(
            "Upload Complete",
            $"Uploaded: {uploaded}\nStreaming: {streaming}\nSkipped: {skipped}",
            "OK"
        );
    }

    void UploadFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Folder does not exist", "OK");
            return;
        }

        // Calculate total size
        long totalSize = 0;
        int fileCount = 0;
        foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            totalSize += new FileInfo(file).Length;
            fileCount++;
        }

        float totalSizeMB = totalSize / (1024.0f * 1024.0f);

        if (totalSizeMB > 100)
        {
            // Large folder - recommend streaming
            bool useStreaming = EditorUtility.DisplayDialog(
                "Large Folder Detected",
                $"Folder contains {fileCount} files ({totalSizeMB:F2} MB).\n\nUse streaming to prevent memory crashes?",
                "Yes (Streaming)",
                "No (Direct Copy)"
            );

            if (useStreaming)
            {
                Debug.Log($"[Upload] Large folder detected. Copy to server:");
                Debug.Log($"[Upload] cp -r '{folderPath}' nav_lambda_server/Assets/");
                EditorUtility.DisplayDialog(
                    "Streaming Required",
                    $"1. Start Rust server: cd nav_lambda_server && cargo run --release\n" +
                    $"2. Copy folder: cp -r '{folderPath}' nav_lambda_server/Assets/\n" +
                    $"3. Use StreamingAssetLoader in Unity to download files",
                    "OK"
                );
                return;
            }
        }

        // Copy folder to Assets
        string folderName = Path.GetFileName(folderPath);
        string destPath = Path.Combine(Application.dataPath, folderName);

        try
        {
            if (Directory.Exists(destPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Folder Exists",
                    $"Folder '{folderName}' already exists in Assets. Overwrite?",
                    "Yes",
                    "No"
                );

                if (!overwrite) return;

                Directory.Delete(destPath, true);
            }

            CopyDirectory(folderPath, destPath);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Upload Complete",
                $"Uploaded {fileCount} files ({totalSizeMB:F2} MB) to Assets/{folderName}",
                "OK"
            );
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to upload folder: {e.Message}", "OK");
        }
    }

    void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    void CopyDesktopToAssets()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        
        bool confirm = EditorUtility.DisplayDialog(
            "Copy Desktop to Assets",
            "This will copy ALL files from Desktop to Assets folder.\n\nContinue?",
            "Yes",
            "No"
        );

        if (!confirm) return;

        try
        {
            int fileCount = 0;
            foreach (string file in Directory.GetFiles(desktopPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                long fileSizeMB = fileInfo.Length / (1024 * 1024);

                if (fileSizeMB > 100)
                {
                    Debug.LogWarning($"[Upload] Skipping large file: {fileInfo.Name} ({fileSizeMB} MB) - Use streaming");
                    continue;
                }

                string destPath = Path.Combine(Application.dataPath, fileInfo.Name);
                File.Copy(file, destPath, true);
                fileCount++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Copied {fileCount} files from Desktop to Assets", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed: {e.Message}", "OK");
        }
    }
}
#endif
