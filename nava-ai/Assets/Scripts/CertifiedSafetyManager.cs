using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

/// <summary>
/// Certified Safety Manager - Bridge between UniversalModelManager and Vnc7dVerifier.
/// Enforces "Ironclad" 7D rigor: if VNC 7D rejects, we OVERRIDE the model (Safety > AI).
/// </summary>
public class CertifiedSafetyManager : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("7D Verifier for safety certification")]
    public Vnc7dVerifier verifier;
    
    [Tooltip("Universal Model Manager for AI actions")]
    public UniversalModelManager modelManager;
    
    [Tooltip("Reference to robot Rigidbody")]
    public Rigidbody robotRigidbody;
    
    [Header("Safety Settings")]
    [Tooltip("Enable safety override (Safety > AI)")]
    public bool enableSafetyOverride = true;
    
    [Tooltip("Emergency stop deceleration")]
    public float emergencyDeceleration = 10f;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for violation reports")]
    public string violationTopic = "/nav/safety_violation";
    
    [Tooltip("ROS2 topic for emergency stop")]
    public string emergencyStopTopic = "/nav/emergency_stop";
    
    private ROSConnection ros;
    private Vector3 lastProposedAction = Vector3.zero;
    private bool isInLockdown = false;
    private int violationCount = 0;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<StringMsg>(violationTopic, 10);
        ros.RegisterPublisher<TwistMsg>(emergencyStopTopic, 10);
        
        // Get references if not assigned
        if (verifier == null)
        {
            verifier = GetComponent<Vnc7dVerifier>();
        }
        
        if (modelManager == null)
        {
            modelManager = GetComponent<UniversalModelManager>();
        }
        
        if (robotRigidbody == null)
        {
            robotRigidbody = GetComponent<Rigidbody>();
        }
        
        Debug.Log("[CertifiedSafetyManager] Initialized - Ironclad safety enforcement ready");
    }

    void Update()
    {
        if (!enableSafetyOverride) return;
        
        // 1. Check current state certification
        if (verifier != null && !verifier.IsCertifiedSafe())
        {
            // Current state is unsafe - emergency stop
            ExecuteSafeStop();
            PublishViolationToROS("7D_STATE_VIOLATION");
            return;
        }
        
        // 2. Check Model Output (if available)
        Vector3 proposedAction = GetProposedAction();
        
        if (proposedAction != Vector3.zero)
        {
            // 3. Apply "Ironclad" 7D Rigor to proposed action
            if (!verifier.IsCertified(proposedAction))
            {
                // "God Intervention" - VNC 7D Rejects
                ExecuteSafeStop();
                PublishViolationToROS("7D_ACTION_VIOLATION");
            }
            else
            {
                // Allowed to execute
                if (isInLockdown)
                {
                    // Release from lockdown if action is now safe
                    ReleaseLockdown();
                }
            }
        }
    }

    Vector3 GetProposedAction()
    {
        // Get proposed action from model manager or teleop
        // This is a simplified version - real implementation would get from actual AI model
        
        // Check teleop controller
        UnityTeleopController teleop = GetComponent<UnityTeleopController>();
        if (teleop != null && teleop.teleopEnabled)
        {
            // Get input direction
            float forward = Input.GetAxis("Vertical");
            float turn = Input.GetAxis("Horizontal");
            
            if (forward != 0 || turn != 0)
            {
                Vector3 move = transform.forward * forward + transform.right * turn;
                return transform.position + move * Time.deltaTime * 2f;
            }
        }
        
        // Check if model manager has proposed action
        // This would require exposing a method from UniversalModelManager
        // For now, return zero (no action)
        
        return Vector3.zero;
    }

    /// <summary>
    /// Execute safe stop (God Intervention)
    /// </summary>
    public void ExecuteSafeStop()
    {
        if (isInLockdown) return; // Already in lockdown
        
        isInLockdown = true;
        
        // Instantaneous braking (Newton's First Law override)
        if (robotRigidbody != null)
        {
            robotRigidbody.velocity = Vector3.zero;
            robotRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Visual Feedback
        CreateViolationMarker();
        
        // Trigger Ironclad Manager
        IroncladManager ironclad = GetComponent<IroncladManager>();
        if (ironclad != null)
        {
            ironclad.TriggerLockdown();
        }
        
        // Publish emergency stop to ROS
        PublishEmergencyStop();
        
        Debug.LogError("[CertifiedSafetyManager] GOD INTERVENTION: Safe stop executed - 7D violation detected");
    }

    void CreateViolationMarker()
    {
        // Create visual marker at violation location
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"ViolationMarker_{violationCount}";
        marker.transform.position = transform.position;
        marker.transform.localScale = Vector3.one * 0.5f;
        
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            renderer.material = mat;
        }
        
        // Destroy after 5 seconds
        Destroy(marker, 5f);
        
        violationCount++;
    }

    void ReleaseLockdown()
    {
        isInLockdown = false;
        Debug.Log("[CertifiedSafetyManager] Lockdown released - system certified safe");
    }

    void PublishViolationToROS(string violationType)
    {
        if (ros == null) return;
        
        StringMsg msg = new StringMsg
        {
            data = $"{violationType}:{Time.time}:{transform.position}"
        };
        
        ros.Publish(violationTopic, msg);
    }

    void PublishEmergencyStop()
    {
        if (ros == null) return;
        
        TwistMsg stopMsg = new TwistMsg();
        // Zero velocity = stop
        ros.Publish(emergencyStopTopic, stopMsg);
    }

    /// <summary>
    /// Check if system is in lockdown
    /// </summary>
    public bool IsInLockdown()
    {
        return isInLockdown;
    }

    /// <summary>
    /// Get violation count
    /// </summary>
    public int GetViolationCount()
    {
        return violationCount;
    }
}
