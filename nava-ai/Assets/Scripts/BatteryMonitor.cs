using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

/// <summary>
/// Monitors battery and hardware health status.
/// Prevents "mystery shutdowns" and enables autonomous docking scheduling.
/// </summary>
public class BatteryMonitor : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("UI Slider showing battery percentage")]
    public Slider batterySlider;
    
    [Tooltip("Text displaying voltage")]
    public Text voltageText;
    
    [Tooltip("Text displaying percentage")]
    public Text percentageText;
    
    [Tooltip("Warning light/image that changes color based on battery state")]
    public Image warningLight;
    
    [Header("Battery Settings")]
    [Tooltip("ROS2 topic name for battery state")]
    public string batteryTopic = "battery_state";
    
    [Tooltip("Low voltage threshold (V) - triggers warning")]
    public float lowVoltageThreshold = 11.0f;
    
    [Tooltip("Critical voltage threshold (V) - triggers emergency stop")]
    public float criticalVoltageThreshold = 10.0f;
    
    [Header("Visual Settings")]
    [Tooltip("Color when battery is healthy")]
    public Color healthyColor = Color.green;
    
    [Tooltip("Color when battery is low")]
    public Color lowColor = Color.yellow;
    
    [Tooltip("Color when battery is critical")]
    public Color criticalColor = Color.red;
    
    [Tooltip("Enable flashing effect for warnings")]
    public bool enableFlashing = true;
    
    private ROSConnection ros;
    private float lastVoltage = 12.0f;
    private float lastPercentage = 100f;
    private float flashTimer = 0f;
    private bool isFlashing = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<BatteryStateMsg>(batteryTopic, UpdateBattery);
        
        // Initialize UI
        if (batterySlider != null)
        {
            batterySlider.minValue = 0f;
            batterySlider.maxValue = 100f;
            batterySlider.value = 100f;
        }
        
        if (warningLight != null)
        {
            warningLight.color = healthyColor;
        }
        
        Debug.Log($"[BatteryMonitor] Subscribed to {batteryTopic}");
    }

    void Update()
    {
        // Handle flashing effect
        if (enableFlashing && isFlashing && warningLight != null)
        {
            flashTimer += Time.deltaTime;
            float alpha = (Mathf.Sin(flashTimer * 5f) + 1f) / 2f; // 0 to 1
            Color currentColor = warningLight.color;
            currentColor.a = 0.3f + alpha * 0.7f;
            warningLight.color = currentColor;
        }
    }

    void UpdateBattery(BatteryStateMsg msg)
    {
        lastVoltage = msg.voltage;
        lastPercentage = msg.percentage;
        
        // Update UI elements
        if (batterySlider != null)
        {
            batterySlider.value = msg.percentage;
        }
        
        if (voltageText != null)
        {
            voltageText.text = $"{msg.voltage:F2} V";
        }
        
        if (percentageText != null)
        {
            percentageText.text = $"{msg.percentage:F1}%";
        }
        
        // Determine battery state and update warning light
        if (warningLight != null)
        {
            if (msg.voltage < criticalVoltageThreshold)
            {
                // Critical - Red, flashing
                warningLight.color = criticalColor;
                isFlashing = true;
                warningLight.gameObject.SetActive(true);
                Debug.LogWarning($"[BatteryMonitor] CRITICAL: {msg.voltage:F2}V - Emergency stop recommended!");
            }
            else if (msg.voltage < lowVoltageThreshold)
            {
                // Low - Yellow, flashing
                warningLight.color = lowColor;
                isFlashing = true;
                warningLight.gameObject.SetActive(true);
                Debug.LogWarning($"[BatteryMonitor] LOW: {msg.voltage:F2}V - Consider returning to dock");
            }
            else
            {
                // Healthy - Green, solid
                warningLight.color = healthyColor;
                isFlashing = false;
                flashTimer = 0f;
                warningLight.gameObject.SetActive(true);
            }
        }
        
        // Log state changes
        if (msg.voltage < criticalVoltageThreshold)
        {
            Debug.LogError($"[BatteryMonitor] CRITICAL VOLTAGE: {msg.voltage:F2}V");
        }
    }
    
    /// <summary>
    /// Get current battery voltage
    /// </summary>
    public float GetVoltage()
    {
        return lastVoltage;
    }
    
    /// <summary>
    /// Get current battery percentage
    /// </summary>
    public float GetPercentage()
    {
        return lastPercentage;
    }
    
    /// <summary>
    /// Check if battery is low
    /// </summary>
    public bool IsLow()
    {
        return lastVoltage < lowVoltageThreshold;
    }
    
    /// <summary>
    /// Check if battery is critical
    /// </summary>
    public bool IsCritical()
    {
        return lastVoltage < criticalVoltageThreshold;
    }
}
