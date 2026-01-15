using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// Simular Controller - Simulated hardware controller for Sim-to-Real transitions.
/// Allows switching between Unity control and remote control seamlessly.
/// </summary>
public class SimularController : MonoBehaviour
{
    [Header("Hardware Configuration")]
    [Tooltip("Device ID for this controller")]
    public string deviceId = "SIM_001";

    [Tooltip("Is currently simulated (Unity controls)")]
    public bool isSimulated = true;

    [Tooltip("Movement speed")]
    public float moveSpeed = 5.0f;

    [Tooltip("Rotation speed")]
    public float rotationSpeed = 90f;

    [Header("Control Settings")]
    [Tooltip("Use keyboard input")]
    public bool useKeyboardInput = true;

    [Tooltip("Smooth movement")]
    public bool smoothMovement = true;

    [Header("Visual Feedback")]
    [Tooltip("Visual indicator for control mode")]
    public GameObject controlIndicator;

    private float3 targetPosition;
    private float3 targetRotation;
    private PeripheralBridge bridge;
    private Rigidbody rb;

    void Start()
    {
        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // Find Peripheral Bridge
        bridge = GetComponent<PeripheralBridge>();
        if (bridge == null)
        {
            bridge = FindObjectOfType<PeripheralBridge>();
        }

        // Initialize target
        targetPosition = transform.position;
        targetRotation = transform.eulerAngles;

        // Create control indicator
        if (controlIndicator == null)
        {
            CreateControlIndicator();
        }

        UpdateControlIndicator();

        Debug.Log($"[Simular] Controller initialized: {deviceId}, Simulated: {isSimulated}");
    }

    void Update()
    {
        if (isSimulated)
        {
            // Unity Controls
            HandleUnityInput();
        }
        else
        {
            // Remote Control (Bridge sends data to Unity)
            HandleRemoteControl();
        }

        // Apply movement
        ApplyMovement();

        // Send state to bridge
        if (bridge != null)
        {
            Vector3 velocity = rb != null ? rb.velocity : Vector3.zero;
            bridge.SendSimulatedState(deviceId, transform.position, velocity);
        }
    }

    void HandleUnityInput()
    {
        if (!useKeyboardInput) return;

        // Get input
        float moveForward = Input.GetAxis("Vertical");
        float moveTurn = Input.GetAxis("Horizontal");

        // Calculate movement
        Vector3 move = transform.forward * moveForward * moveSpeed * Time.deltaTime;
        float turn = moveTurn * rotationSpeed * Time.deltaTime;

        // Update target
        targetPosition = transform.position + move;
        targetRotation = transform.eulerAngles + new Vector3(0, turn, 0);

        // Apply to rigidbody
        if (rb != null)
        {
            if (smoothMovement)
            {
                rb.MovePosition(transform.position + move);
                rb.MoveRotation(Quaternion.Euler(0, turn, 0) * transform.rotation);
            }
            else
            {
                rb.velocity = move / Time.deltaTime;
                rb.angularVelocity = new Vector3(0, turn / Time.deltaTime, 0);
            }
        }
        else
        {
            transform.position += move;
            transform.Rotate(0, turn, 0);
        }
    }

    void HandleRemoteControl()
    {
        // Remote control updates come via ReceiveCommand
        // Position is updated externally
        if (smoothMovement && rb != null)
        {
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f));
            rb.MoveRotation(Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * 5f));
        }
    }

    void ApplyMovement()
    {
        // Movement is handled in HandleUnityInput or HandleRemoteControl
        // This is a placeholder for additional movement logic
    }

    /// <summary>
    /// Receive command from bridge (Remote Control)
    /// </summary>
    public void ReceiveCommand(float3 linear, float3 angular)
    {
        if (isSimulated) return; // Ignore if in Unity control mode

        // Update target position
        targetPosition += linear * Time.deltaTime;

        // Update rotation
        targetRotation += new Vector3(0, angular.y * Time.deltaTime, 0);

        // Apply to transform
        if (rb != null)
        {
            rb.velocity = linear;
            rb.angularVelocity = angular;
        }
        else
        {
            transform.position += (Vector3)linear * Time.deltaTime;
            transform.Rotate(0, angular.y * Time.deltaTime, 0);
        }

        // Visual feedback
        // Could play sound to acknowledge hardware command
        Debug.Log($"[Simular] Command received: Linear={linear}, Angular={angular}");
    }

    /// <summary>
    /// Set target position (for navigation)
    /// </summary>
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }

    /// <summary>
    /// Toggle control mode (Simulated vs Remote)
    /// </summary>
    public void ToggleControlMode()
    {
        isSimulated = !isSimulated;
        UpdateControlIndicator();
        Debug.Log($"[Simular] Control mode: {(isSimulated ? "Simulated" : "Remote")}");
    }

    /// <summary>
    /// Set control mode
    /// </summary>
    public void SetControlMode(bool simulated)
    {
        isSimulated = simulated;
        UpdateControlIndicator();
    }

    void CreateControlIndicator()
    {
        controlIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        controlIndicator.name = "ControlIndicator";
        controlIndicator.transform.SetParent(transform);
        controlIndicator.transform.localPosition = Vector3.up * 2f;
        controlIndicator.transform.localScale = Vector3.one * 0.3f;
        controlIndicator.GetComponent<Collider>().enabled = false;
    }

    void UpdateControlIndicator()
    {
        if (controlIndicator != null)
        {
            Renderer renderer = controlIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isSimulated ? Color.green : Color.blue;
            }
        }
    }

    /// <summary>
    /// Get current position
    /// </summary>
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Get current rotation
    /// </summary>
    public Quaternion GetRotation()
    {
        return transform.rotation;
    }

    /// <summary>
    /// Check if in simulated mode
    /// </summary>
    public bool IsSimulated()
    {
        return isSimulated;
    }
}
