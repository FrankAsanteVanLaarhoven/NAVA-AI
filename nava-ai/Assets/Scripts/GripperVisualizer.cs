using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Control;

/// <summary>
/// Visualizes gripper/end-effector state for manipulator arms.
/// Shows open/close state and grip force through visual feedback.
/// </summary>
public class GripperVisualizer : MonoBehaviour
{
    [Header("Gripper References")]
    [Tooltip("Mesh renderer for the gripper (hand)")]
    public Renderer gripperRenderer;
    
    [Tooltip("Left finger GameObject (optional)")]
    public GameObject leftFinger;
    
    [Tooltip("Right finger GameObject (optional)")]
    public GameObject rightFinger;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic name for gripper status")]
    public string gripperTopic = "gripper/status";
    
    [Header("Visual Settings")]
    [Tooltip("Color when gripper is open")]
    public Color openColor = Color.blue;
    
    [Tooltip("Color when gripper is closed (low force)")]
    public Color closedColor = Color.yellow;
    
    [Tooltip("Color when gripper is holding object (high force)")]
    public Color holdingColor = Color.red;
    
    [Tooltip("Enable emission glow effect")]
    public bool useEmission = true;
    
    [Header("Animation")]
    [Tooltip("Maximum finger opening distance")]
    public float maxFingerDistance = 0.1f;
    
    [Tooltip("Animation speed")]
    public float animationSpeed = 2f;
    
    private ROSConnection ros;
    private Material gripperMaterial;
    private float currentPosition = 0f;
    private float currentEffort = 0f;
    private float targetPosition = 0f;
    private Vector3 leftFingerStartPos;
    private Vector3 rightFingerStartPos;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<GripperCommandMsg>(gripperTopic, UpdateGripperState);
        
        // Get or create material
        if (gripperRenderer != null)
        {
            gripperMaterial = gripperRenderer.material;
            if (gripperMaterial == null)
            {
                gripperMaterial = new Material(Shader.Find("Standard"));
                gripperRenderer.material = gripperMaterial;
            }
        }
        
        // Store initial finger positions
        if (leftFinger != null)
        {
            leftFingerStartPos = leftFinger.transform.localPosition;
        }
        if (rightFinger != null)
        {
            rightFingerStartPos = rightFinger.transform.localPosition;
        }
        
        Debug.Log($"[GripperVisualizer] Subscribed to {gripperTopic}");
    }

    void Update()
    {
        // Animate finger movement
        if (leftFinger != null || rightFinger != null)
        {
            currentPosition = Mathf.Lerp(currentPosition, targetPosition, Time.deltaTime * animationSpeed);
            
            float fingerOffset = currentPosition * maxFingerDistance;
            
            if (leftFinger != null)
            {
                Vector3 pos = leftFingerStartPos;
                pos.x -= fingerOffset; // Move left finger left
                leftFinger.transform.localPosition = pos;
            }
            
            if (rightFinger != null)
            {
                Vector3 pos = rightFingerStartPos;
                pos.x += fingerOffset; // Move right finger right
                rightFinger.transform.localPosition = pos;
            }
        }
    }

    void UpdateGripperState(GripperCommandMsg msg)
    {
        currentEffort = msg.effort;
        targetPosition = msg.position;
        
        UpdateGripperVisuals();
    }

    void UpdateGripperVisuals()
    {
        if (gripperMaterial == null) return;
        
        Color targetColor;
        bool shouldEmit = false;
        
        // Determine state based on position and effort
        if (targetPosition > 0.8f)
        {
            // Open
            targetColor = openColor;
            shouldEmit = true;
        }
        else if (currentEffort > 10.0f)
        {
            // High force - holding object
            targetColor = holdingColor;
            shouldEmit = true;
        }
        else
        {
            // Closed (low force)
            targetColor = closedColor;
            shouldEmit = false;
        }
        
        // Update material color
        gripperMaterial.color = targetColor;
        
        if (useEmission)
        {
            if (shouldEmit)
            {
                gripperMaterial.EnableKeyword("_EMISSION");
                gripperMaterial.SetColor("_EmissionColor", targetColor * 0.5f);
            }
            else
            {
                gripperMaterial.DisableKeyword("_EMISSION");
            }
        }
        
        // Log state changes
        if (targetPosition > 0.8f)
        {
            Debug.Log($"[GripperVisualizer] Gripper OPEN (Position: {targetPosition:F2})");
        }
        else if (currentEffort > 10.0f)
        {
            Debug.Log($"[GripperVisualizer] Gripper HOLDING (Effort: {currentEffort:F2} N)");
        }
        else
        {
            Debug.Log($"[GripperVisualizer] Gripper CLOSED (Position: {targetPosition:F2}, Effort: {currentEffort:F2} N)");
        }
    }
    
    /// <summary>
    /// Get current gripper position (0 = closed, 1 = open)
    /// </summary>
    public float GetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Get current gripper effort (force in Newtons)
    /// </summary>
    public float GetEffort()
    {
        return currentEffort;
    }
    
    /// <summary>
    /// Check if gripper is open
    /// </summary>
    public bool IsOpen()
    {
        return targetPosition > 0.8f;
    }
    
    /// <summary>
    /// Check if gripper is holding an object
    /// </summary>
    public bool IsHolding()
    {
        return currentEffort > 10.0f;
    }
}
