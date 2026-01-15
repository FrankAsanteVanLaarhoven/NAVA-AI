using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System.Collections;

/// <summary>
/// Ironclad Manager - Handles breach detection and lockdown mode.
/// When P-score drops below threshold, engages "Lockdown Mode" (Hardware Brake).
/// </summary>
public class IroncladManager : MonoBehaviour
{
    [Header("Visual Effects")]
    [Tooltip("Particle system for breach visualization")]
    public ParticleSystem breachParticles;
    
    [Tooltip("Audio source for alarm sound")]
    public AudioSource alarmSound;
    
    [Tooltip("Emergency light for visual warning")]
    public Light emergencyLight;
    
    [Header("Lockdown Settings")]
    [Tooltip("Lockdown duration (seconds, 0 = until manually released)")]
    public float lockdownDuration = 0f;
    
    [Tooltip("Enable automatic lockdown release")]
    public bool autoRelease = false;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for emergency stop")]
    public string emergencyStopTopic = "/nav/emergency_stop";
    
    private ROSConnection ros;
    private bool isLockedDown = false;
    private float lockdownStartTime = 0f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(emergencyStopTopic, 10);
        
        // Create emergency light if not assigned
        if (emergencyLight == null)
        {
            GameObject lightObj = new GameObject("EmergencyLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 2f;
            emergencyLight = lightObj.AddComponent<Light>();
            emergencyLight.type = LightType.Point;
            emergencyLight.range = 10f;
            emergencyLight.intensity = 0f;
            emergencyLight.enabled = false;
        }
        
        // Create breach particles if not assigned
        if (breachParticles == null)
        {
            GameObject particlesObj = new GameObject("BreachParticles");
            particlesObj.transform.SetParent(transform);
            particlesObj.transform.localPosition = Vector3.zero;
            breachParticles = particlesObj.AddComponent<ParticleSystem>();
            
            var main = breachParticles.main;
            main.startColor = Color.red;
            main.startSize = 0.5f;
            main.startLifetime = 2f;
            main.maxParticles = 100;
        }
        
        Debug.Log("[IroncladManager] Initialized - Lockdown system ready");
    }

    void Update()
    {
        // Auto-release lockdown if duration set
        if (isLockedDown && autoRelease && lockdownDuration > 0f)
        {
            if (Time.time - lockdownStartTime >= lockdownDuration)
            {
                ReleaseLockdown();
            }
        }
    }

    /// <summary>
    /// Trigger lockdown mode (God Math Violation)
    /// </summary>
    public void TriggerLockdown()
    {
        if (isLockedDown) return; // Already locked down
        
        isLockedDown = true;
        lockdownStartTime = Time.time;
        
        // 1. Visual Warning
        if (emergencyLight != null)
        {
            emergencyLight.color = Color.red;
            emergencyLight.intensity = 10.0f;
            emergencyLight.enabled = true;
            StartCoroutine(PulseEmergencyLight());
        }
        
        if (breachParticles != null)
        {
            breachParticles.Play();
        }
        
        // 2. Audio Feedback
        if (alarmSound != null && !alarmSound.isPlaying)
        {
            alarmSound.Play();
        }
        
        // 3. Log "God Math" Violation
        Debug.LogError("[IRONCLAD] Equation P violated. Safety Override Engaged.");
        
        // 4. Send Command to ROS (STOP)
        PublishEmergencyStop();
        
        // 5. Stop robot physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    IEnumerator PulseEmergencyLight()
    {
        while (isLockedDown && emergencyLight != null)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            emergencyLight.intensity = 5f + pulse * 5f;
            yield return null;
        }
    }

    void PublishEmergencyStop()
    {
        if (ros == null) return;
        
        TwistMsg stopMsg = new TwistMsg();
        // Zero velocity = emergency stop
        ros.Publish(emergencyStopTopic, stopMsg);
        
        Debug.Log("[IroncladManager] Published emergency stop to ROS");
    }

    /// <summary>
    /// Release lockdown manually
    /// </summary>
    public void ReleaseLockdown()
    {
        if (!isLockedDown) return;
        
        isLockedDown = false;
        
        // Stop visual effects
        if (emergencyLight != null)
        {
            emergencyLight.enabled = false;
            emergencyLight.intensity = 0f;
        }
        
        if (breachParticles != null)
        {
            breachParticles.Stop();
        }
        
        if (alarmSound != null)
        {
            alarmSound.Stop();
        }
        
        Debug.Log("[IroncladManager] Lockdown released");
    }

    /// <summary>
    /// Check if system is in lockdown
    /// </summary>
    public bool IsLockedDown()
    {
        return isLockedDown;
    }
}
