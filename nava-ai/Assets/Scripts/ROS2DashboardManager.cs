using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class ROS2DashboardManager : MonoBehaviour
{
    [Header("Connection Settings")]
    [Tooltip("ROS IP Address. Use 127.0.0.1 for local testing, or Jetson IP (e.g., 192.168.1.50) for real hardware")]
    public string rosIP = "127.0.0.1"; // localhost for laptop testing
    public int rosPort = 10000;

    [Header("Scene References")]
    public GameObject realRobot;
    public GameObject shadowRobot;

    [Header("UI References")]
    public UnityEngine.UI.Text velocityText;
    public UnityEngine.UI.Text marginText;
    public UnityEngine.UI.Text statusText;
    public UnityEngine.UI.Image connectionIndicator;

    private ROSConnection ros;
    private bool shadowModeActive = false;
    private float currentMargin = 2.0f; // Track current safety margin
    
    [Header("Fleet Settings")]
    [Tooltip("Unique robot ID for fleet management (0 = single robot)")]
    public int robotID = 0;
    
    /// <summary>
    /// Public property to check if shadow mode is currently active
    /// Used by other components like MapVisualizer
    /// </summary>
    public bool IsShadowModeActive => shadowModeActive;
    
    /// <summary>
    /// Get current safety margin
    /// </summary>
    public float GetMargin()
    {
        return currentMargin;
    }

    void Start()
    {
        // 1. Setup ROS Connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.Configure(rosIP, rosPort);

        // 2. Subscribe to Topics (Listening to ROS)
        // Use robotID to create unique topics for fleet management
        string topicPrefix = robotID > 0 ? $"agent_{robotID}/" : "";
        ros.Subscribe<TwistMsg>($"{topicPrefix}nav/cmd_vel", UpdateRobotMotion);
        ros.Subscribe<Float32Msg>($"{topicPrefix}nav/margin", UpdateMarginUI);
        ros.Subscribe<BoolMsg>($"{topicPrefix}nav/shadow_toggle", UpdateShadowVisuals);

        Debug.Log($"[Unity] Attempting to connect to ROS at {rosIP}:{rosPort}");
    }

    // --- Callbacks from ROS ---

    void UpdateRobotMotion(TwistMsg msg)
    {
        // Move the "Real" robot based on ROS Twist message
        // Note: Simple local translation. In real sim, use Physics or Odometry
        Vector3 move = new Vector3((float)msg.linear.x, 0, (float)msg.linear.z); 
        realRobot.transform.Translate(move * Time.deltaTime);
        
        // Rotate
        float turn = (float)msg.angular.y * Time.deltaTime;
        realRobot.transform.Rotate(0, turn * Mathf.Rad2Deg, 0);

        velocityText.text = $"Speed: {msg.linear.x:F2} m/s";
    }

    void UpdateMarginUI(Float32Msg msg)
    {
        currentMargin = msg.data; // Store for fleet manager
        marginText.text = $"Safety Margin: {msg.data:F2} m";
        if (msg.data < 0.5f) marginText.color = Color.red;
        else marginText.color = Color.green;
    }

    void UpdateShadowVisuals(BoolMsg msg)
    {
        shadowModeActive = msg.data;
        shadowRobot.SetActive(shadowModeActive);
        
        if(shadowModeActive) 
        {
            statusText.text = "SHADOW MODE: ACTIVE";
            connectionIndicator.color = Color.magenta;
        }
        else 
        {
            statusText.text = "MODE: STANDARD";
            connectionIndicator.color = Color.green;
        }
    }

    // --- UI Button Functions (Called by OnClick events in Unity) ---
    public void RequestToggleShadow()
    {
        // This sends a message BACK to ROS (Jetson)
        ros.Publish<BoolMsg>("nav/cmd/toggle_shadow", new BoolMsg { data = true });
        Debug.Log("[Unity] Sent request to toggle Shadow Mode");
    }
}
