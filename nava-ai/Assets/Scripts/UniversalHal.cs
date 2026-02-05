using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Universal Hardware Abstraction Layer - Unified interface between VLA (Simulated) and Real Robot.
/// Acts as the "Notary" between Unity Dashboard and Jetson hardware.
/// </summary>
public class UniversalHal : MonoBehaviour
{
    [Header("Hardware Bridge Configuration")]
    public GameObject robotRoot; // The "Real" robot in Simulated World
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hardwareStatus;
    
    [Header("Connection Settings")]
    public string jetsonIP = "192.168.1.50"; // Default Jetson IP
    public int rosPort = 10000;
    public float connectionTimeout = 5.0f;
    
    [Header("Risk Management")]
    public float riskFactor = 1.0f; // Simulated Hardware Risk
    public float maxRiskFactor = 2.0f;
    
    private bool isConnected = false;
    private bool isSimulated = true; // For testing without hardware
    private ROS2DashboardManager rosManager;

    void Start()
    {
        // 1. Find ROS Manager
        rosManager = FindObjectOfType<ROS2DashboardManager>();
        if (rosManager == null)
        {
            Debug.LogWarning("[UNIVERSAL HAL] ROS2DashboardManager not found. Using simulated mode.");
            isSimulated = true;
        }

        // 2. Initialize UI
        if (statusText != null)
        {
            statusText.text = "HAL: INITIALIZING...";
            statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
        }

        if (hardwareStatus != null)
        {
            hardwareStatus.text = "HARDWARE: DISCONNECTED";
            hardwareStatus.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Danger);
        }

        // 3. Connect to Hardware (Simulated or Real)
        StartCoroutine(ConnectToHardware());
    }

    IEnumerator ConnectToHardware()
    {
        Debug.Log("[UNIVERSAL HAL] Connecting to Hardware...");
        
        if (isSimulated)
        {
            // Simulated connection (instant)
            yield return new WaitForSeconds(0.5f);
            isConnected = true;
            Debug.Log("[UNIVERSAL HAL] Connected (SIMULATED)");
        }
        else
        {
            // Real hardware connection via ROS
            float elapsed = 0.0f;
            while (elapsed < connectionTimeout && !isConnected)
            {
                // Attempt connection
                if (rosManager != null && rosManager.IsConnected())
                {
                    isConnected = true;
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Update UI
        if (isConnected)
        {
            if (statusText != null)
            {
                statusText.text = "HAL: CONNECTED";
                statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
            }
            if (hardwareStatus != null)
            {
                hardwareStatus.text = isSimulated ? "HARDWARE: SIMULATED" : $"HARDWARE: {jetsonIP}";
                hardwareStatus.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "HAL: CONNECTION FAILED";
                statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Danger);
            }
        }
    }

    void Update()
    {
        if (!isConnected) return;

        // 5. Simulate Telemetry Stream
        float velocity = Input.GetAxis("Vertical") * 2.0f; // W/S
        float turn = Input.GetAxis("Horizontal") * 90.0f; // A/D

        if (velocity != 0 || turn != 0)
        {
            // Send Twist (Simulated or Real)
            SendVelocityCommand(velocity, turn);
        }
    }

    public void SendVelocityCommand(float linear, float angular)
    {
        // Apply risk factor (higher risk = slower movement)
        float adjustedLinear = linear / riskFactor;
        float adjustedAngular = angular / riskFactor;

        if (isSimulated)
        {
            // Simulated command
            Debug.Log($"[UNIVERSAL HAL] Cmd Vel: Linear={adjustedLinear:F2}, Angular={adjustedAngular:F2} (Risk: {riskFactor:F2})");
        }
        else
        {
            // Send to ROS (via ROS2DashboardManager)
            if (rosManager != null)
            {
                // In production, this would publish to /nav/cmd_vel topic
                // rosManager.PublishVelocity(adjustedLinear, adjustedAngular);
            }
        }
    }

    public void SetRiskFactor(float risk)
    {
        riskFactor = Mathf.Clamp(risk, 1.0f, maxRiskFactor);
        Debug.Log($"[UNIVERSAL HAL] Risk Factor set to: {riskFactor:F2}");
        
        // Update VLA bias if available
        AdaptiveVlaManager vlaManager = FindObjectOfType<AdaptiveVlaManager>();
        if (vlaManager != null)
        {
            // Higher risk = more conservative (slower)
            float speedModifier = 1.0f / riskFactor;
            // vlaManager.SetSpeedModifier(speedModifier);
        }
    }

    public float GetRiskFactor()
    {
        return riskFactor;
    }

    public bool IsInSafe()
    {
        // Check if robot is in safe state
        NavlConsciousnessRigor rigor = FindObjectOfType<NavlConsciousnessRigor>();
        if (rigor != null)
        {
            float pScore = rigor.GetTotalScore();
            return pScore > 50.0f; // Safe if P-score > 50
        }
        return true; // Default to safe if no rigor found
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    public void Disconnect()
    {
        isConnected = false;
        if (statusText != null)
        {
            statusText.text = "HAL: DISCONNECTED";
            statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
        }
    }

    public void Reconnect()
    {
        StartCoroutine(ConnectToHardware());
    }
}
