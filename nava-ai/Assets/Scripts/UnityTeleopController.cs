using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

/// <summary>
/// Teleoperation controller for driving the robot using WASD keys or gamepad.
/// Publishes velocity commands to ROS2 that override the navigation stack.
/// Essential for hardware testing and emergency stops.
/// </summary>
public class UnityTeleopController : MonoBehaviour
{
    [Header("Teleop Settings")]
    [Tooltip("Forward/backward speed in m/s")]
    public float moveSpeed = 1.0f;
    
    [Tooltip("Rotation speed in rad/s")]
    public float turnSpeed = 1.0f;
    
    [Tooltip("Enable/disable teleop control")]
    public bool teleopEnabled = true;
    
    [Header("Throttle Settings")]
    [Tooltip("Publish rate limit (Hz). Lower = less network traffic")]
    public float publishRate = 20f;
    
    private ROSConnection ros;
    private TwistMsg twistMsg = new TwistMsg();
    private float lastPublishTime = 0f;
    private float publishInterval;
    private Vector3 lastCommand = Vector3.zero;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        // We are now PUBLISHING to the Jetson
        ros.RegisterPublisher<TwistMsg>("unity/cmd_vel", 10);
        
        publishInterval = 1f / publishRate;
        
        Debug.Log("[Teleop] Unity teleoperation controller initialized. Use WASD or Arrow Keys to drive.");
    }

    void Update()
    {
        if (!teleopEnabled) 
        {
            // Send zero velocity when disabled
            if (Time.time - lastPublishTime >= publishInterval)
            {
                twistMsg.linear.x = 0;
                twistMsg.angular.z = 0;
                ros.Publish("unity/cmd_vel", twistMsg);
                lastPublishTime = Time.time;
            }
            return;
        }

        // 1. Get Input
        float forward = Input.GetAxis("Vertical"); // W/S or Up/Down Arrow
        float turn = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow

        // 2. Map Input to ROS Speed
        twistMsg.linear.x = forward * moveSpeed;
        twistMsg.angular.z = turn * turnSpeed;

        // 3. Send to ROS (Throttled to reduce network traffic)
        if (Time.time - lastPublishTime >= publishInterval)
        {
            ros.Publish("unity/cmd_vel", twistMsg);
            lastPublishTime = Time.time;
            
            // Store last command for causal graph
            if (forward != 0 || turn != 0)
            {
                lastCommand = new Vector3(forward * moveSpeed, 0, turn * turnSpeed);
                Debug.Log($"[Teleop] Sending: Linear={twistMsg.linear.x:F2} m/s, Angular={twistMsg.angular.z:F2} rad/s");
            }
            else
            {
                lastCommand = Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Public method to enable/disable teleop from UI
    /// </summary>
    public void SetTeleopEnabled(bool enabled)
    {
        teleopEnabled = enabled;
        Debug.Log($"[Teleop] Teleoperation {(enabled ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Get desired velocity for intent visualization
    /// </summary>
    public Vector3 GetDesiredVelocity()
    {
        if (!teleopEnabled) return Vector3.zero;
        
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        
        if (forward == 0 && turn == 0) return Vector3.zero;
        
        Vector3 move = transform.forward * forward * moveSpeed;
        Vector3 turnVec = transform.right * turn * turnSpeed;
        
        return move + turnVec;
    }

    /// <summary>
    /// Get last command for causal graph
    /// </summary>
    public Vector3 GetLastCommand()
    {
        return lastCommand;
    }
}
