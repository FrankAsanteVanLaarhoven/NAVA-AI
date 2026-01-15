using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Streaming Asset Loader - Downloads large files in chunks to prevent memory crashes.
/// Replaces direct file uploads with streaming from Rust Hub server.
/// </summary>
public class StreamingAssetLoader : MonoBehaviour
{
    [Header("Server Configuration")]
    [Tooltip("Rust Hub server URL")]
    public string serverUrl = "http://127.0.0.1:8080";

    [Tooltip("Chunk size in MB for streaming")]
    [Range(1, 10)]
    public float chunkSizeMB = 2f;

    [Header("UI References")]
    [Tooltip("Status text display")]
    public UnityEngine.UI.Text statusText;

    [Tooltip("Progress bar (0-1)")]
    public UnityEngine.UI.Slider progressBar;

    [Header("Settings")]
    [Tooltip("Assets folder path (relative to project root)")]
    public string assetsPath = "Assets/StreamedAssets";

    [Tooltip("Auto-import after download")]
    public bool autoImport = true;

    private string currentDownloadPath = "";
    private bool isDownloading = false;

    void Start()
    {
        if (statusText != null)
        {
            statusText.text = "READY TO RECEIVE";
        }

        // Create assets directory if it doesn't exist
        string fullPath = Path.Combine(Application.dataPath, "..", assetsPath);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"[StreamingAssetLoader] Created directory: {fullPath}");
        }
    }

    /// <summary>
    /// Download large file from Rust Hub server
    /// </summary>
    public void DownloadLargeFile(string fileName)
    {
        if (isDownloading)
        {
            Debug.LogWarning("[StreamingAssetLoader] Download already in progress");
            return;
        }

        StartCoroutine(DownloadFileCoroutine(fileName));
    }

    /// <summary>
    /// Download file with streaming (prevents memory crash)
    /// </summary>
    IEnumerator DownloadFileCoroutine(string fileName)
    {
        isDownloading = true;
        string url = $"{serverUrl}/Assets/{fileName}";
        string localPath = Path.Combine(Application.dataPath, "..", assetsPath, fileName);
        currentDownloadPath = localPath;

        Debug.Log($"[StreamingAssetLoader] Starting download: {url}");

        if (statusText != null)
        {
            statusText.text = $"DOWNLOADING: {fileName}";
        }

        // Use UnityWebRequest for streaming download
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Configure for streaming
            request.downloadHandler = new DownloadHandlerFile(localPath);
            
            // Send request
            var operation = request.SendWebRequest();

            // Update progress
            while (!operation.isDone)
            {
                float progress = operation.progress;
                if (progressBar != null)
                {
                    progressBar.value = progress;
                }

                if (statusText != null)
                {
                    statusText.text = $"DOWNLOADING: {fileName} ({progress * 100:F1}%)";
                }

                yield return null;
            }

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[StreamingAssetLoader] Download failed: {request.error}");
                if (statusText != null)
                {
                    statusText.text = $"ERROR: {request.error}";
                }
                isDownloading = false;
                yield break;
            }

            // Download complete
            Debug.Log($"[StreamingAssetLoader] Download complete: {localPath}");
            if (statusText != null)
            {
                statusText.text = "COMPLETE & IMPORTED";
            }

            if (progressBar != null)
            {
                progressBar.value = 1f;
            }

            // Import asset if enabled
            if (autoImport)
            {
                yield return new WaitForSeconds(0.5f); // Wait for file to be written
                ImportAsset(localPath);
            }
        }

        isDownloading = false;
    }

    /// <summary>
    /// Import downloaded asset into Unity project
    /// </summary>
    void ImportAsset(string filePath)
    {
#if UNITY_EDITOR
        // Convert to relative path
        string relativePath = "Assets" + filePath.Replace(Application.dataPath, "").Replace("\\", "/");
        
        Debug.Log($"[StreamingAssetLoader] Importing asset: {relativePath}");
        
        // Refresh asset database
        AssetDatabase.Refresh();
        
        // Import asset
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        
        Debug.Log($"[StreamingAssetLoader] Asset imported: {relativePath}");
#else
        Debug.Log("[StreamingAssetLoader] Asset import only available in Editor");
#endif
    }

    /// <summary>
    /// Check if file should use streaming (based on size)
    /// </summary>
    public bool ShouldUseStreaming(string fileName, long fileSizeBytes)
    {
        const long STREAMING_THRESHOLD = 100 * 1024 * 1024; // 100 MB
        return fileSizeBytes > STREAMING_THRESHOLD;
    }

    /// <summary>
    /// Get download progress (0-1)
    /// </summary>
    public float GetDownloadProgress()
    {
        return progressBar != null ? progressBar.value : 0f;
    }

    /// <summary>
    /// Check if currently downloading
    /// </summary>
    public bool IsDownloading()
    {
        return isDownloading;
    }

    /// <summary>
    /// Cancel current download
    /// </summary>
    public void CancelDownload()
    {
        if (isDownloading)
        {
            StopAllCoroutines();
            isDownloading = false;
            
            // Clean up partial file
            if (!string.IsNullOrEmpty(currentDownloadPath) && File.Exists(currentDownloadPath))
            {
                File.Delete(currentDownloadPath);
                Debug.Log($"[StreamingAssetLoader] Cancelled and deleted: {currentDownloadPath}");
            }

            if (statusText != null)
            {
                statusText.text = "DOWNLOAD CANCELLED";
            }
        }
    }
}

/// <summary>
/// Download Handler that writes directly to file (streaming)
/// </summary>
public class DownloadHandlerFile : DownloadHandlerScript
{
    private string filePath;
    private FileStream fileStream;
    private long totalBytes = 0;
    private long receivedBytes = 0;

    public DownloadHandlerFile(string path) : base()
    {
        filePath = path;
        
        // Create file stream for writing
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
    }

    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        totalBytes = (long)contentLength;
        Debug.Log($"[DownloadHandlerFile] Expected size: {totalBytes / (1024 * 1024)} MB");
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (fileStream != null && data != null && dataLength > 0)
        {
            // Write chunk directly to file (prevents RAM accumulation)
            fileStream.Write(data, 0, dataLength);
            receivedBytes += dataLength;
            
            // Log progress for large files
            if (totalBytes > 0 && receivedBytes % (2 * 1024 * 1024) == 0) // Every 2MB
            {
                float progress = (float)receivedBytes / totalBytes;
                Debug.Log($"[DownloadHandlerFile] Progress: {progress * 100:F1}% ({receivedBytes / (1024 * 1024)} MB / {totalBytes / (1024 * 1024)} MB)");
            }
        }
        return true;
    }

    protected override void CompleteContent()
    {
        if (fileStream != null)
        {
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
            
            Debug.Log($"[DownloadHandlerFile] Download complete: {filePath} ({receivedBytes / (1024 * 1024)} MB)");
        }
    }

    protected override void Cleanup()
    {
        if (fileStream != null)
        {
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
        }
        base.Cleanup();
    }
}
