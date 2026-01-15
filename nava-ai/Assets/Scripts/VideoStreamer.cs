using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Video Streamer - Production Capability.
/// Production robots often need visual inspection. This streamer renders the
/// robot's WebCam feed (or simulated texture) onto a Picture-in-Picture UI
/// for remote operators.
/// </summary>
public class VideoStreamer : MonoBehaviour
{
    [Header("Stream Settings")]
    [Tooltip("Requested webcam width")]
    public int requestedWidth = 1280;
    
    [Tooltip("Requested webcam height")]
    public int requestedHeight = 720;
    
    [Tooltip("Requested FPS")]
    public int requestedFPS = 30;
    
    [Header("UI References")]
    [Tooltip("RawImage to display video")]
    public RawImage videoDisplay;
    
    [Tooltip("Toggle to enable/disable streaming")]
    public Toggle streamToggle;
    
    [Header("Camera Selection")]
    [Tooltip("Camera device index (0 = first camera)")]
    public int cameraIndex = 0;
    
    private WebCamTexture webCamTexture;
    private bool isStreaming = false;

    void Start()
    {
        // 1. Request Webcam Permissions
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            StartCoroutine(RequestWebcamPermission());
        }
        else
        {
            InitializeWebcam();
        }
        
        // Setup toggle
        if (streamToggle != null)
        {
            streamToggle.onValueChanged.AddListener(OnStreamToggle);
        }
    }

    IEnumerator RequestWebcamPermission()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            InitializeWebcam();
        }
        else
        {
            Debug.LogWarning("[VideoStreamer] Webcam permission denied");
        }
    }

    void InitializeWebcam()
    {
        if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("[VideoStreamer] No webcam devices found");
            return;
        }
        
        // Select camera device
        int deviceIndex = Mathf.Clamp(cameraIndex, 0, WebCamTexture.devices.Length - 1);
        WebCamDevice device = WebCamTexture.devices[deviceIndex];
        
        // Create webcam texture
        webCamTexture = new WebCamTexture(device.name, requestedWidth, requestedHeight, requestedFPS);
        
        // Assign to display
        if (videoDisplay != null)
        {
            videoDisplay.texture = webCamTexture;
        }
        
        Debug.Log($"[VideoStreamer] Initialized webcam: {device.name}");
    }

    void Update()
    {
        // Update texture if streaming
        if (isStreaming && webCamTexture != null && webCamTexture.isPlaying)
        {
            // Texture updates automatically
            // In production: Could compress and stream over network
        }
    }

    void OnStreamToggle(bool enabled)
    {
        if (enabled)
        {
            StartStreaming();
        }
        else
        {
            StopStreaming();
        }
    }

    /// <summary>
    /// Start video streaming
    /// </summary>
    public void StartStreaming()
    {
        if (webCamTexture == null)
        {
            InitializeWebcam();
        }
        
        if (webCamTexture != null && !webCamTexture.isPlaying)
        {
            webCamTexture.Play();
            isStreaming = true;
            Debug.Log("[VideoStreamer] Streaming started");
        }
    }

    /// <summary>
    /// Stop video streaming
    /// </summary>
    public void StopStreaming()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            isStreaming = false;
            Debug.Log("[VideoStreamer] Streaming stopped");
        }
    }

    /// <summary>
    /// Switch camera
    /// </summary>
    public void SwitchCamera(int index)
    {
        if (isStreaming)
        {
            StopStreaming();
        }
        
        cameraIndex = index;
        InitializeWebcam();
        
        if (isStreaming)
        {
            StartStreaming();
        }
    }

    void OnDestroy()
    {
        StopStreaming();
        
        if (webCamTexture != null)
        {
            Destroy(webCamTexture);
        }
    }

    /// <summary>
    /// Get available cameras
    /// </summary>
    public string[] GetAvailableCameras()
    {
        if (WebCamTexture.devices == null)
        {
            return new string[0];
        }
        
        string[] names = new string[WebCamTexture.devices.Length];
        for (int i = 0; i < WebCamTexture.devices.Length; i++)
        {
            names[i] = WebCamTexture.devices[i].name;
        }
        
        return names;
    }
}
