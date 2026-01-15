using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System;

/// <summary>
/// Universal Hardware Abstraction Layer (HAL) - Hardware-agnostic interface.
/// The Dashboard doesn't care if it's a TurtleBot, Spot, or Drone. It talks to a Universal Interface.
/// This is how NASA/JPL operates.
/// </summary>
public class UniversalHal : MonoBehaviour
{
    [System.Serializable]
    public enum HardwareType
    {
        NullHardware,   // Simulation mode
        TurtleBot,      // Differential drive
        Spot,          // Quadruped
        Drone,          // Quadrotor
        Humanoid,       // Bipedal
        Custom          // User-defined
    }

    /// <summary>
    /// Hardware Abstraction Interface
    /// </summary>
    public interface IHardwareProfile
    {
        void SetMotor(Vector3 velocity);
        void SetLighting(float intensity);
        float GetBattery();
        string GetHardwareName();
        bool IsHealthy();
    }

    /// <summary>
    /// Null Hardware Profile (Simulation Mode)
    /// </summary>
    [System.Serializable]
    public class NullHardwareProfile : IHardwareProfile
    {
        private float simulatedBattery = 100f;

        public void SetMotor(Vector3 velocity)
        {
            // Simulated - no actual hardware
        }

        public void SetLighting(float intensity)
        {
            // Simulated lighting
        }

        public float GetBattery()
        {
            return simulatedBattery;
        }

        public string GetHardwareName()
        {
            return "Null Hardware (Simulation)";
        }

        public bool IsHealthy()
        {
            return true;
        }
    }

    /// <summary>
    /// TurtleBot Profile (Differential Drive)
    /// </summary>
    [System.Serializable]
    public class TurtleBotProfile : IHardwareProfile
    {
        private float battery = 100f;
        private ROSConnection ros;

        public TurtleBotProfile(ROSConnection rosConnection)
        {
            ros = rosConnection;
        }

        public void SetMotor(Vector3 velocity)
        {
            // Convert to differential drive commands
            // velocity.x = linear, velocity.z = angular
            TwistMsg cmd = new TwistMsg
            {
                linear = new Vector3Msg { x = velocity.x, y = 0, z = 0 },
                angular = new Vector3Msg { x = 0, y = 0, z = velocity.z }
            };
            
            if (ros != null)
            {
                ros.Publish("/cmd_vel", cmd);
            }
        }

        public void SetLighting(float intensity)
        {
            // TurtleBot doesn't have lighting
        }

        public float GetBattery()
        {
            return battery;
        }

        public string GetHardwareName()
        {
            return "TurtleBot (Differential Drive)";
        }

        public bool IsHealthy()
        {
            return battery > 10f;
        }
    }

    /// <summary>
    /// Drone Profile (Quadrotor)
    /// </summary>
    [System.Serializable]
    public class DroneProfile : IHardwareProfile
    {
        private float battery = 100f;
        private ROSConnection ros;

        public DroneProfile(ROSConnection rosConnection)
        {
            ros = rosConnection;
        }

        public void SetMotor(Vector3 velocity)
        {
            // Convert to quadrotor commands (thrust + attitude)
            // For drones, velocity is 3D (x, y, z)
            TwistMsg cmd = new TwistMsg
            {
                linear = new Vector3Msg { x = velocity.x, y = velocity.y, z = velocity.z },
                angular = new Vector3Msg { x = 0, y = 0, z = 0 }
            };
            
            if (ros != null)
            {
                ros.Publish("/drone/cmd_vel", cmd);
            }
        }

        public void SetLighting(float intensity)
        {
            // Drone LED control
            if (ros != null)
            {
                // Publish LED command
            }
        }

        public float GetBattery()
        {
            return battery;
        }

        public string GetHardwareName()
        {
            return "Quadrotor (Aerial)";
        }

        public bool IsHealthy()
        {
            return battery > 15f; // Drones need more battery
        }
    }

    [Header("HAL Settings")]
    [Tooltip("Hardware type (auto-detected or manual)")]
    public HardwareType hardwareType = HardwareType.NullHardware;
    
    [Tooltip("Enable auto-detection")]
    public bool autoDetect = true;
    
    [Header("UI References")]
    [Tooltip("Text displaying HAL status")]
    public Text halStatusText;
    
    [Tooltip("Text displaying battery level")]
    public Text batteryText;
    
    [Header("Health Monitoring")]
    [Tooltip("Low battery threshold (%)")]
    public float lowBatteryThreshold = 10.0f;
    
    [Tooltip("Enable low power warning")]
    public bool enableLowPowerWarning = true;
    
    private IHardwareProfile activeProfile;
    private ROSConnection ros;
    private float lastBatteryCheck = 0f;
    private float batteryCheckInterval = 1f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // 1. Auto-Detect or Simulate
        if (autoDetect)
        {
            DetectHardware();
        }
        else
        {
            SetHardwareType(hardwareType);
        }
        
        Debug.Log($"[UniversalHAL] Initialized - {activeProfile.GetHardwareName()}");
    }

    void DetectHardware()
    {
        // In Unity Editor, default to Null (Sim)
        #if UNITY_EDITOR
        SetHardwareType(HardwareType.NullHardware);
        return;
        #endif
        
        // In Build (Linux/Windows), try to detect hardware
        // Check for serial ports, ROS topics, etc.
        
        // For now, default to simulation
        SetHardwareType(HardwareType.NullHardware);
    }

    void SetHardwareType(HardwareType type)
    {
        hardwareType = type;
        
        switch (type)
        {
            case HardwareType.NullHardware:
                activeProfile = new NullHardwareProfile();
                break;
            case HardwareType.TurtleBot:
                activeProfile = new TurtleBotProfile(ros);
                break;
            case HardwareType.Drone:
                activeProfile = new DroneProfile(ros);
                break;
            default:
                activeProfile = new NullHardwareProfile();
                break;
        }
        
        UpdateHALStatus();
    }

    void Update()
    {
        // 2. Execute Commands (Unified)
        // The AI doesn't send "Move Left Wheel". It sends "Move X Axis".
        Vector3 input = GetInput();
        if (input != Vector3.zero)
        {
            activeProfile.SetMotor(input);
        }
        
        // 3. Health Check
        if (Time.time - lastBatteryCheck >= batteryCheckInterval)
        {
            float batt = activeProfile.GetBattery();
            UpdateBatteryUI(batt);
            
            if (batt < lowBatteryThreshold && enableLowPowerWarning)
            {
                TriggerLowPowerWarning(batt);
            }
            
            lastBatteryCheck = Time.time;
        }
        
        // Check hardware health
        if (!activeProfile.IsHealthy())
        {
            Debug.LogWarning("[UniversalHAL] Hardware health check failed");
        }
    }

    Vector3 GetInput()
    {
        // Get input from teleop or AI
        UnityTeleopController teleop = GetComponent<UnityTeleopController>();
        if (teleop != null && teleop.teleopEnabled)
        {
            return teleop.GetDesiredVelocity();
        }
        
        // Get from AI model (would come from UniversalModelManager)
        return Vector3.zero;
    }

    void UpdateHALStatus()
    {
        if (halStatusText != null)
        {
            halStatusText.text = $"HAL: {activeProfile.GetHardwareName()}\n" +
                                $"Status: {(activeProfile.IsHealthy() ? "HEALTHY" : "WARNING")}";
            halStatusText.color = activeProfile.IsHealthy() ? Color.green : Color.yellow;
        }
    }

    void UpdateBatteryUI(float battery)
    {
        if (batteryText != null)
        {
            batteryText.text = $"Battery: {battery:F1}%";
            batteryText.color = battery < lowBatteryThreshold ? Color.red : Color.green;
        }
    }

    void TriggerLowPowerWarning(float battery)
    {
        Debug.LogWarning($"[UniversalHAL] Low Power Warning: {battery:F1}%");
        
        // Visual warning
        if (halStatusText != null)
        {
            halStatusText.color = Color.red;
        }
        
        // Audio warning (if available)
        // Trigger emergency protocols
    }

    /// <summary>
    /// Set hardware type programmatically
    /// </summary>
    public void SetHardware(HardwareType type)
    {
        SetHardwareType(type);
    }

    /// <summary>
    /// Get current hardware profile
    /// </summary>
    public IHardwareProfile GetActiveProfile()
    {
        return activeProfile;
    }

    /// <summary>
    /// Get battery level
    /// </summary>
    public float GetBattery()
    {
        return activeProfile != null ? activeProfile.GetBattery() : 100f;
    }
}
