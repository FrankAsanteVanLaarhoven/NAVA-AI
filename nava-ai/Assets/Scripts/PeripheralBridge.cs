using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Peripheral Bridge - Connects QR code scanner and biometric devices to Universal HAL.
/// Acts as the central hub for hardware peripherals without loading heavy prefabs.
/// </summary>
public class PeripheralBridge : MonoBehaviour
{
    [Header("Peripherals")]
    [Tooltip("QR Code Manager")]
    public QrCodeManager qrManager;

    [Tooltip("Biometric Authenticator")]
    public BiometricAuthenticator bioAuth;

    [Tooltip("Simular Controller")]
    public SimularController simularController;

    [Header("Device Management")]
    [Tooltip("Poll rate for device detection")]
    public float pollRate = 2.0f;

    [Tooltip("Auto-detect peripherals")]
    public bool autoDetect = true;

    private Dictionary<string, HardwareDevice> devices = new Dictionary<string, HardwareDevice>();
    private bool isMonitoring = false;

    [System.Serializable]
    public class HardwareDevice
    {
        public string deviceId;
        public string deviceType;
        public bool isConnected;
        public float lastUpdateTime;
    }

    void Start()
    {
        // Register QR Manager
        if (qrManager == null)
        {
            qrManager = GetComponent<QrCodeManager>();
        }

        // Register Biometric Authenticator
        if (bioAuth == null)
        {
            bioAuth = GetComponent<BiometricAuthenticator>();
        }

        // Register Simular Controller
        if (simularController == null)
        {
            simularController = GetComponent<SimularController>();
        }

        // Start peripheral detection
        if (autoDetect)
        {
            StartCoroutine(DetectPeripherals());
        }

        Debug.Log("[PeripheralBridge] Initialized");
    }

    IEnumerator DetectPeripherals()
    {
        isMonitoring = true;

        while (isMonitoring)
        {
            // Simulate hardware detection
            // In production, this would poll USB/Bluetooth ports

            // Simulate FaceID scanner (F key)
            if (Input.GetKeyDown(KeyCode.F))
            {
                string simulatedId = "FACE_ID_001";
                bool isValid = true;

                if (qrManager != null)
                {
                    qrManager.ScanCode(simulatedId);
                }

                if (bioAuth != null)
                {
                    bioAuth.UpdateUser(simulatedId, 0.95f, 70f);
                }

                // Check if QR scan passed
                if (qrManager != null && qrManager.IsValidCode(simulatedId))
                {
                    // Update consciousness in safety systems
                    NavlConsciousnessRigor consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
                    if (consciousnessRigor != null)
                    {
                        // Would update consciousness
                        Debug.Log("[PeripheralBridge] Consciousness updated via FaceID");
                    }

                    // Execute return to start
                    UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
                    if (modelManager != null)
                    {
                        // ExecuteCustomAction would need to be implemented
                        Debug.Log("[PeripheralBridge] Executing RETURN_START via FaceID");
                    }
                }
            }

            // Simulate QR Code scanner (Q key)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                string simulatedCode = "A"; // Zone A

                if (qrManager != null)
                {
                    qrManager.ScanCode(simulatedCode);
                }

                if (bioAuth != null)
                {
                    bioAuth.UpdateUser(simulatedCode, 0.8f, 60f);
                }

                // Check if QR scan passed
                if (qrManager != null && qrManager.IsValidCode(simulatedCode))
                {
                    if (bioAuth != null)
                    {
                        bioAuth.UpdateUser(simulatedCode, 0.95f, 50f);
                    }

                    // Execute navigation
                    UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
                    if (modelManager != null)
                    {
                        Debug.Log("[PeripheralBridge] Executing NAVIGATE_TO_ZONE_A via QR Code");
                    }
                }
            }

            yield return new WaitForSeconds(pollRate);
        }
    }

    /// <summary>
    /// Register hardware device
    /// </summary>
    public void RegisterDevice(string deviceId, string deviceType)
    {
        if (!devices.ContainsKey(deviceId))
        {
            devices[deviceId] = new HardwareDevice
            {
                deviceId = deviceId,
                deviceType = deviceType,
                isConnected = true,
                lastUpdateTime = Time.time
            };

            Debug.Log($"[PeripheralBridge] Device registered: {deviceId} ({deviceType})");
        }
    }

    /// <summary>
    /// Unregister hardware device
    /// </summary>
    public void UnregisterDevice(string deviceId)
    {
        if (devices.ContainsKey(deviceId))
        {
            devices[deviceId].isConnected = false;
            devices.Remove(deviceId);
            Debug.Log($"[PeripheralBridge] Device unregistered: {deviceId}");
        }
    }

    /// <summary>
    /// Send simulated state to bridge (for network publishing)
    /// </summary>
    public void SendSimulatedState(string deviceId, Vector3 position, Vector3 velocity)
    {
        // In production, this would publish to ROS2 or WebSocket
        // For now, we just log
        Debug.Log($"[PeripheralBridge] Simulated state from {deviceId}: Pos={position}, Vel={velocity}");
    }

    /// <summary>
    /// Get connected devices
    /// </summary>
    public Dictionary<string, HardwareDevice> GetConnectedDevices()
    {
        return new Dictionary<string, HardwareDevice>(devices);
    }

    /// <summary>
    /// Check if device is connected
    /// </summary>
    public bool IsDeviceConnected(string deviceId)
    {
        return devices.ContainsKey(deviceId) && devices[deviceId].isConnected;
    }

    void OnDestroy()
    {
        isMonitoring = false;
    }
}
