using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

/// <summary>
/// Digital Twin Physics - Handles physics discrepancy between Unity (Simulation) and ROS (Real).
/// Supports Quadrotor (Rigidbody) and Humanoid (Kinematic/IK) form factors.
/// </summary>
public class DigitalTwinPhysics : MonoBehaviour
{
    [Header("Robot Configuration")]
    [Tooltip("Robot form factor type")]
    public RobotFormFactor robotType = RobotFormFactor.Ground;
    
    [Tooltip("Model type for AI behavior")]
    public ModelType modelType = ModelType.SafeVLA;
    
    [Header("Physics Components")]
    [Tooltip("Rigidbody component (for Quadrotor)")]
    public Rigidbody rb;
    
    [Tooltip("Animator component (for Humanoid)")]
    public Animator animator;
    
    [Tooltip("IK target transforms (for Humanoid)")]
    public Transform[] motorTargets;
    
    [Header("Quadrotor Settings")]
    [Tooltip("Drone mass (kg)")]
    public float droneMass = 1.5f;
    
    [Tooltip("Aerodynamic drag coefficient")]
    public float dragCoefficient = 0.5f;
    
    [Tooltip("Thrust force multiplier")]
    public float thrustMultiplier = 10f;
    
    [Header("Humanoid Settings")]
    [Tooltip("Root motion enabled")]
    public bool useRootMotion = true;
    
    [Tooltip("IK weight for target following")]
    public float ikWeight = 1f;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for motor commands")]
    public string motorCommandTopic = "/motor_commands";
    
    private ROSConnection ros;
    private Vector3 currentForce = Vector3.zero;
    private Vector3 currentTorque = Vector3.zero;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(motorCommandTopic, OnMotorCommand);
        
        // Get or add components
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();
        
        // Configure physics based on robot type
        ConfigurePhysics();
        
        Debug.Log($"[DigitalTwinPhysics] Initialized for {robotType} with {modelType} model");
    }

    void ConfigurePhysics()
    {
        if (rb == null) return;
        
        switch (robotType)
        {
            case RobotFormFactor.Aerial:
                // Quadrotor physics
                rb.mass = droneMass;
                rb.drag = dragCoefficient;
                rb.angularDrag = 2f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                break;
            
            case RobotFormFactor.Humanoid:
                // Humanoid physics (kinematic)
                rb.isKinematic = true;
                rb.useGravity = false;
                if (animator != null)
                {
                    animator.applyRootMotion = useRootMotion;
                }
                break;
            
            case RobotFormFactor.Ground:
                // Ground vehicle physics
                rb.mass = 10f;
                rb.drag = 1f;
                rb.useGravity = true;
                rb.isKinematic = false;
                break;
            
            case RobotFormFactor.Manipulator:
                // Manipulator arm (typically kinematic)
                rb.isKinematic = true;
                rb.useGravity = false;
                break;
        }
    }

    void OnMotorCommand(TwistMsg msg)
    {
        // Convert ROS Twist to force/torque
        Vector3 linear = new Vector3((float)msg.linear.x, (float)msg.linear.y, (float)msg.linear.z);
        Vector3 angular = new Vector3((float)msg.angular.x, (float)msg.angular.y, (float)msg.angular.z);
        
        ApplyMotorCommand(linear, angular);
    }

    /// <summary>
    /// Apply motor command (force and torque)
    /// </summary>
    public void ApplyMotorCommand(Vector3 force, Vector3 torque)
    {
        currentForce = force;
        currentTorque = torque;
        
        switch (robotType)
        {
            case RobotFormFactor.Aerial:
                ApplyQuadrotorPhysics(force, torque);
                break;
            
            case RobotFormFactor.Humanoid:
                ApplyHumanoidPhysics(force, torque);
                break;
            
            case RobotFormFactor.Ground:
                ApplyGroundVehiclePhysics(force, torque);
                break;
            
            case RobotFormFactor.Manipulator:
                ApplyManipulatorPhysics(force, torque);
                break;
        }
    }

    void ApplyQuadrotorPhysics(Vector3 force, Vector3 torque)
    {
        if (rb == null || rb.isKinematic) return;
        
        // Apply thrust (force in local up direction)
        Vector3 thrust = transform.up * force.y * thrustMultiplier;
        rb.AddRelativeForce(thrust, ForceMode.Acceleration);
        
        // Apply horizontal movement
        Vector3 horizontal = new Vector3(force.x, 0, force.z);
        rb.AddRelativeForce(horizontal, ForceMode.Acceleration);
        
        // Apply torque for rotation
        rb.AddRelativeTorque(torque, ForceMode.Acceleration);
        
        // Apply gravity compensation
        rb.AddForce(Vector3.up * Physics.gravity.magnitude * rb.mass, ForceMode.Force);
    }

    void ApplyHumanoidPhysics(Vector3 force, Vector3 torque)
    {
        // Humanoid uses animation/IK, not physics forces
        if (animator != null)
        {
            // Convert force to movement direction
            Vector3 lookTarget = transform.position + force;
            transform.LookAt(lookTarget);
            
            // Set animation parameters
            float speed = force.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("Direction", Vector3.SignedAngle(transform.forward, force, Vector3.up));
        }
        
        // Update IK targets if provided
        if (motorTargets != null && motorTargets.Length > 0)
        {
            // Simplified IK target positioning
            // In real implementation, this would use Unity's IK system
            foreach (Transform target in motorTargets)
            {
                if (target != null)
                {
                    // Update target positions based on force/torque
                    // This is a placeholder - real IK would be more complex
                }
            }
        }
    }

    void ApplyGroundVehiclePhysics(Vector3 force, Vector3 torque)
    {
        if (rb == null || rb.isKinematic) return;
        
        // Ground vehicle physics
        rb.AddRelativeForce(force, ForceMode.Acceleration);
        rb.AddRelativeTorque(new Vector3(0, torque.y, 0), ForceMode.Acceleration); // Only yaw for ground vehicles
    }

    void ApplyManipulatorPhysics(Vector3 force, Vector3 torque)
    {
        // Manipulator arms typically use IK, not physics
        // This would interface with IK solver
        if (motorTargets != null && motorTargets.Length > 0)
        {
            // Update joint targets based on force/torque
        }
    }

    void FixedUpdate()
    {
        // Apply continuous forces for quadrotor
        if (robotType == RobotFormFactor.Aerial && rb != null && !rb.isKinematic)
        {
            // Continuous application of forces (if needed)
            // Most forces are applied in ApplyMotorCommand, but this allows for continuous effects
        }
    }

    /// <summary>
    /// Switch robot type at runtime
    /// </summary>
    public void SwitchRobotType(RobotFormFactor newType)
    {
        robotType = newType;
        ConfigurePhysics();
        Debug.Log($"[DigitalTwinPhysics] Switched to {newType}");
    }

    /// <summary>
    /// Get current velocity
    /// </summary>
    public Vector3 GetVelocity()
    {
        return rb != null ? rb.velocity : Vector3.zero;
    }

    /// <summary>
    /// Get current angular velocity
    /// </summary>
    public Vector3 GetAngularVelocity()
    {
        return rb != null ? rb.angularVelocity : Vector3.zero;
    }
}
