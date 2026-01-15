using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;

/// <summary>
/// GPS-Denied PNT Manager - Dead Reckoning fallback when GPS is jammed/denied.
/// Switches to DR (Dead Reckoning) using Wheel Encoders + IMU.
/// </summary>
public class GpsDeniedPntManager : MonoBehaviour
{
    [System.Serializable]
    public enum PntMode
    {
        GPS,        // GPS lock active
        DR,         // Dead Reckoning (GPS denied)
        Fused       // GPS + DR fusion
    }

    [Header("UI References")]
    [Tooltip("Text displaying PNT status")]
    public Text pntStatusText;
    
    [Tooltip("GPS status light")]
    public Light gpsLight;
    
    [Tooltip("DR (Dead Reckoning) status light")]
    public Light drLight;
    
    [Header("PNT Settings")]
    [Tooltip("GPS fix quality threshold")]
    public sbyte minFixQuality = 1; // STATUS_SBAS_FIX
    
    [Tooltip("Position covariance threshold (m^2)")]
    public float maxCovariance = 100.0f;
    
    [Tooltip("Enable automatic mode switching")]
    public bool autoSwitch = true;
    
    [Header("Dead Reckoning Settings")]
    [Tooltip("DR drift rate (m/s)")]
    public float driftRate = 0.1f;
    
    [Tooltip("Enable DR position estimation")]
    public bool enableDREstimation = true;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for GPS fix")]
    public string gpsTopic = "/gps/fix";
    
    [Tooltip("ROS2 topic for IMU data")]
    public string imuTopic = "/imu/data";
    
    [Tooltip("ROS2 topic for odometry")]
    public string odomTopic = "/odom";
    
    private ROSConnection ros;
    private PntMode currentMode = PntMode.GPS;
    private Vector3 drPosition = Vector3.zero;
    private Vector3 drVelocity = Vector3.zero;
    private Quaternion drOrientation = Quaternion.identity;
    private float lastUpdateTime = 0f;
    private bool gpsHealthy = true;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // Subscribe to GPS Health
        ros.Subscribe<NavSatFixMsg>(gpsTopic, CheckGpsHealth);
        
        // Subscribe to IMU (Crucial for DR)
        ros.Subscribe<ImuMsg>(imuTopic, UpdateDrEstimation);
        
        // Subscribe to Odometry (for wheel encoder data)
        ros.Subscribe<OdometryMsg>(odomTopic, UpdateOdometry);
        
        // Create lights if not assigned
        CreateStatusLights();
        
        Debug.Log("[GpsDeniedPNT] Initialized - GPS-denied fallback ready");
    }

    void CreateStatusLights()
    {
        if (gpsLight == null)
        {
            GameObject gpsLightObj = new GameObject("GPSLight");
            gpsLightObj.transform.SetParent(transform);
            gpsLightObj.transform.localPosition = Vector3.up * 2f;
            gpsLight = gpsLightObj.AddComponent<Light>();
            gpsLight.type = LightType.Point;
            gpsLight.range = 5f;
            gpsLight.intensity = 2f;
            gpsLight.color = Color.green;
        }
        
        if (drLight == null)
        {
            GameObject drLightObj = new GameObject("DRLight");
            drLightObj.transform.SetParent(transform);
            drLightObj.transform.localPosition = Vector3.up * 2f + Vector3.right * 0.5f;
            drLight = drLightObj.AddComponent<Light>();
            drLight.type = LightType.Point;
            drLight.range = 5f;
            drLight.intensity = 0f;
            drLight.color = Color.yellow;
            drLight.enabled = false;
        }
    }

    void CheckGpsHealth(NavSatFixMsg msg)
    {
        // Check GPS fix quality
        bool hasGoodFix = msg.status.status >= minFixQuality;
        
        // Check position covariance (uncertainty)
        float positionCov = msg.position_covariance[0]; // XX element
        bool hasLowUncertainty = positionCov < maxCovariance;
        
        gpsHealthy = hasGoodFix && hasLowUncertainty;
        
        if (!gpsHealthy && autoSwitch)
        {
            if (currentMode == PntMode.GPS)
            {
                Debug.LogWarning($"[PNT] GPS Denied/Jammed. Status: {msg.status.status}, Cov: {positionCov:F2}. Switching to DR Mode.");
                currentMode = PntMode.DR;
                
                // Initialize DR from current position
                if (transform != null)
                {
                    drPosition = transform.position;
                    drOrientation = transform.rotation;
                }
            }
        }
        else if (gpsHealthy && currentMode == PntMode.DR)
        {
            Debug.Log("[PNT] GPS Lock Restored. Switching back to GPS Mode.");
            currentMode = PntMode.GPS;
        }
        
        UpdatePntUI();
    }

    void UpdateDrEstimation(ImuMsg imu)
    {
        if (currentMode != PntMode.DR && currentMode != PntMode.Fused) return;
        if (!enableDREstimation) return;
        
        float deltaTime = Time.time - lastUpdateTime;
        if (deltaTime <= 0) return;
        
        // Dead Reckoning Logic:
        // Integrate Acceleration (IMU) to get Velocity
        Vector3 linearAccel = new Vector3(
            (float)imu.linear_acceleration.x,
            (float)imu.linear_acceleration.y,
            (float)imu.linear_acceleration.z
        );
        
        // Convert ROS IMU frame to Unity frame
        Vector3 accelUnity = new Vector3(-linearAccel.y, linearAccel.z, linearAccel.x);
        
        // Integrate acceleration to velocity (simplified - assumes constant acceleration)
        drVelocity += accelUnity * deltaTime;
        
        // Apply drift (accumulating error)
        drVelocity += Random.insideUnitSphere * driftRate * deltaTime;
        
        // Integrate velocity to position
        drPosition += drVelocity * deltaTime;
        
        // Update orientation from angular velocity
        Vector3 angularVel = new Vector3(
            (float)imu.angular_velocity.x,
            (float)imu.angular_velocity.y,
            (float)imu.angular_velocity.z
        );
        Vector3 angularVelUnity = new Vector3(-angularVel.y, angularVel.z, angularVel.x);
        drOrientation *= Quaternion.Euler(angularVelUnity * Mathf.Rad2Deg * deltaTime);
        
        // Update transform (if this is the robot)
        if (transform != null && currentMode == PntMode.DR)
        {
            transform.position = drPosition;
            transform.rotation = drOrientation;
        }
        
        lastUpdateTime = Time.time;
    }

    void UpdateOdometry(OdometryMsg odom)
    {
        if (currentMode != PntMode.DR && currentMode != PntMode.Fused) return;
        
        // Use odometry for more accurate DR (wheel encoders)
        Vector3 odomPos = new Vector3(
            (float)odom.pose.pose.position.x,
            (float)odom.pose.pose.position.y,
            (float)odom.pose.pose.position.z
        );
        
        Vector3 odomVel = new Vector3(
            (float)odom.twist.twist.linear.x,
            (float)odom.twist.twist.linear.y,
            (float)odom.twist.twist.linear.z
        );
        
        // Convert ROS to Unity
        Vector3 posUnity = new Vector3(-odomPos.y, odomPos.z, odomPos.x);
        Vector3 velUnity = new Vector3(-odomVel.y, odomVel.z, odomVel.x);
        
        // Fuse with IMU-based DR (weighted average)
        if (currentMode == PntMode.Fused)
        {
            drPosition = Vector3.Lerp(drPosition, posUnity, 0.7f); // Trust odometry more
            drVelocity = Vector3.Lerp(drVelocity, velUnity, 0.7f);
        }
        else if (currentMode == PntMode.DR)
        {
            // Use odometry as primary source in DR mode
            drPosition = posUnity;
            drVelocity = velUnity;
        }
    }

    void UpdatePntUI()
    {
        if (pntStatusText != null)
        {
            switch (currentMode)
            {
                case PntMode.GPS:
                    pntStatusText.text = "MODE: GPS (LOCK)";
                    pntStatusText.color = Color.green;
                    break;
                case PntMode.DR:
                    pntStatusText.text = "MODE: DEAD RECKONING\n" +
                                       $"Position: ({drPosition.x:F2}, {drPosition.y:F2}, {drPosition.z:F2})\n" +
                                       $"Drift: {driftRate:F3} m/s";
                    pntStatusText.color = Color.yellow;
                    break;
                case PntMode.Fused:
                    pntStatusText.text = "MODE: FUSED (GPS + DR)";
                    pntStatusText.color = Color.cyan;
                    break;
            }
        }
        
        // Update lights
        if (gpsLight != null)
        {
            gpsLight.color = gpsHealthy ? Color.green : Color.gray;
            gpsLight.intensity = gpsHealthy ? 2f : 0.5f;
        }
        
        if (drLight != null)
        {
            drLight.enabled = (currentMode == PntMode.DR || currentMode == PntMode.Fused);
            drLight.color = Color.yellow; // DR has drift
            drLight.intensity = currentMode == PntMode.DR ? 3f : 1.5f;
        }
    }

    /// <summary>
    /// Get current PNT mode
    /// </summary>
    public PntMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Check if GPS is healthy
    /// </summary>
    public bool IsGpsHealthy()
    {
        return gpsHealthy;
    }

    /// <summary>
    /// Get DR position estimate
    /// </summary>
    public Vector3 GetDRPosition()
    {
        return drPosition;
    }
}
