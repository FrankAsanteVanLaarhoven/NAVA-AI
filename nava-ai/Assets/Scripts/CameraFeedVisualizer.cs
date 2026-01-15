using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;

/// <summary>
/// Visualizes live camera feed from the robot's camera.
/// Subscribes to ROS2 Image topic and displays it in Unity UI.
/// Essential for debugging perception failures.
/// </summary>
public class CameraFeedVisualizer : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("UI RawImage component to display the camera feed")]
    public RawImage displayImage;
    
    [Tooltip("ROS2 topic name for camera images")]
    public string cameraTopic = "camera/image_raw";
    
    [Tooltip("Expected image width (will auto-resize if different)")]
    public int expectedWidth = 640;
    
    [Tooltip("Expected image height (will auto-resize if different)")]
    public int expectedHeight = 480;
    
    private Texture2D texture;
    private ROSConnection ros;
    private bool textureInitialized = false;

    void Start()
    {
        if (displayImage == null)
        {
            Debug.LogError("[CameraFeed] Display Image not assigned! Camera feed will not work.");
            return;
        }
        
        ros = ROSConnection.GetOrCreateInstance();
        // Subscribe to Camera Topic (Standard on Jetson)
        ros.Subscribe<ImageMsg>(cameraTopic, UpdateImage);
        
        Debug.Log($"[CameraFeed] Subscribed to {cameraTopic}. Waiting for images...");
    }

    void UpdateImage(ImageMsg msg)
    {
        if (displayImage == null) return;
        
        // Initialize or resize texture if dimensions changed
        if (!textureInitialized || 
            texture.width != msg.width || 
            texture.height != msg.height)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
            
            texture = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGB24, false);
            displayImage.texture = texture;
            textureInitialized = true;
            
            Debug.Log($"[CameraFeed] Initialized texture: {msg.width}x{msg.height}");
        }

        // Convert ROS image data to Unity texture
        // ROS ImageMsg uses different encoding, we need to handle it
        try
        {
            // For RGB8 encoding (most common)
            if (msg.encoding == "rgb8" || msg.encoding == "RGB8")
            {
                texture.LoadRawTextureData(msg.data);
                texture.Apply();
            }
            // For BGR8 (OpenCV default) - need to swap R and B channels
            else if (msg.encoding == "bgr8" || msg.encoding == "BGR8")
            {
                byte[] rgbData = new byte[msg.data.Length];
                for (int i = 0; i < msg.data.Length; i += 3)
                {
                    // Swap B and R channels
                    rgbData[i] = msg.data[i + 2];     // R
                    rgbData[i + 1] = msg.data[i + 1]; // G
                    rgbData[i + 2] = msg.data[i];     // B
                }
                texture.LoadRawTextureData(rgbData);
                texture.Apply();
            }
            else
            {
                // Try direct load for other formats
                texture.LoadRawTextureData(msg.data);
                texture.Apply();
                Debug.LogWarning($"[CameraFeed] Unsupported encoding: {msg.encoding}. Attempting direct load.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CameraFeed] Error processing image: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}
