using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Cloud Sync Manager - Handles remote synchronization, Mcity updates, and cloud bridge.
/// Enables low-latency field testing with remote control override.
/// </summary>
public class CloudSyncManager : MonoBehaviour
{
    [Header("Remote Connection")]
    [Tooltip("Remote server IP address")]
    public string remoteIP = "192.168.1.50";
    
    [Tooltip("Remote server port")]
    public int remotePort = 8080;
    
    [Tooltip("Enable remote sync")]
    public bool enableRemoteSync = true;
    
    [Header("Sync Settings")]
    [Tooltip("Telemetry sync rate (Hz)")]
    public float telemetryRate = 20f;
    
    [Tooltip("Mcity update check interval (seconds)")]
    public float mcityUpdateInterval = 60f;
    
    [Tooltip("Enable remote command override")]
    public bool allowRemoteOverride = true;
    
    [Header("References")]
    [Tooltip("Reference to McityMapLoader for map updates")]
    public McityMapLoader mcityLoader;
    
    [Tooltip("Reference to ROS2DashboardManager for telemetry")]
    public ROS2DashboardManager dashboardManager;
    
    [Header("UI References")]
    [Tooltip("Text displaying sync status")]
    public UnityEngine.UI.Text syncStatusText;
    
    [Tooltip("Text displaying remote connection status")]
    public UnityEngine.UI.Text remoteStatusText;
    
    private ROSConnection ros;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private bool isConnected = false;
    private float lastTelemetryTime = 0f;
    private float lastMcityCheck = 0f;
    private float telemetryInterval;
    private Thread receiveThread;
    private bool isReceiving = false;
    private string lastRemoteCommand = "";
    private Queue<string> commandQueue = new Queue<string>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        telemetryInterval = 1f / telemetryRate;
        
        // Initialize UDP client for low-latency communication
        if (enableRemoteSync)
        {
            InitializeRemoteConnection();
        }
        
        // Subscribe to remote command topic
        ros.Subscribe<StringMsg>("/remote/command", OnRemoteCommand);
        
        Debug.Log($"[CloudSyncManager] Initialized - Remote sync: {enableRemoteSync}");
    }

    void InitializeRemoteConnection()
    {
        try
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            udpClient = new UdpClient();
            udpClient.Connect(remoteEndPoint);
            isConnected = true;
            
            // Start receive thread
            isReceiving = true;
            receiveThread = new Thread(ReceiveRemoteData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            Debug.Log($"[CloudSyncManager] Connected to remote: {remoteIP}:{remotePort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSyncManager] Failed to connect: {e.Message}");
            isConnected = false;
        }
    }

    void Update()
    {
        if (!enableRemoteSync || !isConnected) return;
        
        // 1. Sync Telemetry to Remote
        if (Time.time - lastTelemetryTime >= telemetryInterval)
        {
            SendTelemetry();
            lastTelemetryTime = Time.time;
        }
        
        // 2. Check for Mcity Updates
        if (Time.time - lastMcityCheck >= mcityUpdateInterval)
        {
            CheckMcityUpdates();
            lastMcityCheck = Time.time;
        }
        
        // 3. Process Remote Commands (from queue)
        ProcessQueuedCommands();
        
        // 4. Process Remote Command
        ProcessRemoteCommand();
        
        // 5. Update UI
        UpdateUI();
    }

    void SendTelemetry()
    {
        if (udpClient == null || !isConnected) return;
        
        try
        {
            // Collect telemetry data
            TelemetryData data = new TelemetryData
            {
                timestamp = Time.time,
                position = dashboardManager != null && dashboardManager.realRobot != null 
                    ? dashboardManager.realRobot.transform.position 
                    : Vector3.zero,
                margin = dashboardManager != null ? dashboardManager.GetMargin() : 2f,
                velocity = Vector3.zero, // Would get from actual robot
                modelType = "SafeVLA" // Would get from UniversalModelManager
            };
            
            // Serialize and send
            string json = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            udpClient.Send(bytes, bytes.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSyncManager] Failed to send telemetry: {e.Message}");
            isConnected = false;
        }
    }

    void CheckMcityUpdates()
    {
        // In production, this would check API for map updates
        // For now, this is a placeholder
        if (mcityLoader != null)
        {
            // Could trigger map reload if updates detected
            Debug.Log("[CloudSyncManager] Checking for Mcity updates...");
        }
    }

    void ReceiveRemoteData()
    {
        while (isReceiving && udpClient != null)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                
                // Process received data on main thread
                // Use Unity's main thread dispatcher if available, otherwise queue for Update()
                lock (commandQueue)
                {
                    commandQueue.Enqueue(message);
                }
            }
            catch (System.Exception e)
            {
                if (isReceiving)
                {
                    Debug.LogError($"[CloudSyncManager] Receive error: {e.Message}");
                }
            }
        }
    }

    void ProcessReceivedData(string data)
    {
        // Parse received command/data
        try
        {
            RemoteCommand command = JsonUtility.FromJson<RemoteCommand>(data);
            lastRemoteCommand = command.command;
            
            // Handle command
            HandleRemoteCommand(command);
        }
        catch
        {
            // Simple string command
            lastRemoteCommand = data;
            HandleSimpleCommand(data);
        }
    }

    void OnRemoteCommand(StringMsg msg)
    {
        HandleSimpleCommand(msg.data);
    }

    void HandleRemoteCommand(RemoteCommand command)
    {
        switch (command.command.ToUpper())
        {
            case "OVERRIDE":
                if (allowRemoteOverride)
                {
                    EnableRemoteOverride();
                }
                break;
            case "RELEASE":
                DisableRemoteOverride();
                break;
            case "UPDATE_MCITY":
                if (mcityLoader != null)
                {
                    mcityLoader.StartCoroutine(mcityLoader.LoadMapData());
                }
                break;
        }
    }

    void HandleSimpleCommand(string command)
    {
        HandleRemoteCommand(new RemoteCommand { command = command });
    }

    void ProcessQueuedCommands()
    {
        lock (commandQueue)
        {
            while (commandQueue.Count > 0)
            {
                string command = commandQueue.Dequeue();
                ProcessReceivedData(command);
            }
        }
    }

    void ProcessRemoteCommand()
    {
        if (string.IsNullOrEmpty(lastRemoteCommand)) return;
        
        // Commands are processed in HandleRemoteCommand
        // This method can be used for continuous command processing
    }

    void EnableRemoteOverride()
    {
        Debug.Log("[CloudSyncManager] Remote override enabled - Remote control active");
        // Disable local control, enable remote control
    }

    void DisableRemoteOverride()
    {
        Debug.Log("[CloudSyncManager] Remote override disabled - Local control active");
        // Re-enable local control
    }

    void UpdateUI()
    {
        if (syncStatusText != null)
        {
            syncStatusText.text = $"Sync: {(isConnected ? "CONNECTED" : "DISCONNECTED")}\n" +
                                 $"Rate: {telemetryRate}Hz";
            syncStatusText.color = isConnected ? Color.green : Color.red;
        }
        
        if (remoteStatusText != null)
        {
            remoteStatusText.text = $"Remote: {remoteIP}\n" +
                                   $"Status: {(isConnected ? "ONLINE" : "OFFLINE")}";
            remoteStatusText.color = isConnected ? Color.green : Color.red;
        }
    }

    void OnDestroy()
    {
        isReceiving = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
        }
        
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    [System.Serializable]
    class TelemetryData
    {
        public float timestamp;
        public Vector3 position;
        public float margin;
        public Vector3 velocity;
        public string modelType;
    }

    [System.Serializable]
    class RemoteCommand
    {
        public string command;
        public string parameters;
    }
}

