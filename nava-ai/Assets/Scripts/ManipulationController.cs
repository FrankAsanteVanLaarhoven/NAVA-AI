using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manipulation Controller - Fine-grained control for research robots.
/// Supports Franka Panda gripper, Trossen ALOHA, and low-level joint control.
/// </summary>
public class ManipulationController : MonoBehaviour
{
    [Header("Low-Level Manipulation")]
    [Tooltip("Left finger position (0.0 = open, 1.0 = closed)")]
    [Range(0f, 1f)]
    public float finger1 = 0.0f;

    [Tooltip("Right finger position (0.0 = open, 1.0 = closed)")]
    [Range(0f, 1f)]
    public float finger2 = 0.0f;

    [Tooltip("Gripper aperture (0.0 = open, 1.0 = closed)")]
    [Range(0f, 1f)]
    public float gripperWidth = 0.0f;

    [Header("Franka Panda Settings")]
    [Tooltip("Use Franka-style single aperture control")]
    public bool useFrankaStyle = true;

    [Tooltip("Gripper speed (units per second)")]
    public float gripperSpeed = 2.0f;

    [Header("UI References")]
    [Tooltip("Text display for gripper status")]
    public Text gripperStatusText;

    [Tooltip("Slider for gripper control")]
    public Slider gripperSlider;

    [Header("Hardware Bridge")]
    [Tooltip("Send commands to hardware (ROS)")]
    public bool sendToHardware = false;

    private UniversalHal hal;
    private float targetAperture = 0.0f;

    void Start()
    {
        hal = GetComponent<UniversalHal>();
        
        if (gripperSlider != null)
        {
            gripperSlider.onValueChanged.AddListener(OnGripperSliderChanged);
        }
    }

    void Update()
    {
        // Process input (fine-grained control)
        ProcessInput();

        // Apply to robot
        ApplyGripperControl();

        // Update UI
        UpdateUI();
    }

    void ProcessInput()
    {
        // Keyboard controls for fine manipulation
        if (Input.GetKey(KeyCode.Keypad1))
        {
            finger1 = Mathf.Clamp01(finger1 + Time.deltaTime * gripperSpeed);
        }
        if (Input.GetKey(KeyCode.Keypad2))
        {
            finger1 = Mathf.Clamp01(finger1 - Time.deltaTime * gripperSpeed);
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            finger2 = Mathf.Clamp01(finger2 + Time.deltaTime * gripperSpeed);
        }
        if (Input.GetKey(KeyCode.Keypad4))
        {
            finger2 = Mathf.Clamp01(finger2 - Time.deltaTime * gripperSpeed);
        }

        // Unified gripper control
        if (Input.GetKey(KeyCode.G))
        {
            gripperWidth = Mathf.Clamp01(gripperWidth + Time.deltaTime * gripperSpeed);
        }
        if (Input.GetKey(KeyCode.H))
        {
            gripperWidth = Mathf.Clamp01(gripperWidth - Time.deltaTime * gripperSpeed);
        }
    }

    void ApplyGripperControl()
    {
        if (useFrankaStyle)
        {
            // Franka Panda uses single aperture value
            targetAperture = gripperWidth;
            
            // Map to finger positions (symmetric)
            finger1 = targetAperture;
            finger2 = targetAperture;
        }
        else
        {
            // Independent finger control
            targetAperture = (finger1 + finger2) / 2.0f;
        }

        // Send to hardware if enabled
        if (sendToHardware && hal != null)
        {
            // Send joint targets to hardware
            hal.SetTarget("LeftGripper", finger1);
            hal.SetTarget("RightGripper", finger2);
            hal.SetTarget("GripperAperture", targetAperture);
        }
    }

    /// <summary>
    /// Set Franka-style gripper aperture (0.0 = open, 1.0 = closed)
    /// </summary>
    [ContextMenu("Manipulation/Set Grip (Franka)")]
    public void SetFrankaGrip(float aperture)
    {
        gripperWidth = Mathf.Clamp01(aperture);
        targetAperture = aperture;

        Debug.Log($"[Manipulation] Franka Grip Set to: {aperture:F2}");

        // Update visual feedback
        UpdateGripperVisuals();
    }

    /// <summary>
    /// Open gripper fully
    /// </summary>
    public void OpenGripper()
    {
        SetFrankaGrip(0.0f);
    }

    /// <summary>
    /// Close gripper fully
    /// </summary>
    public void CloseGripper()
    {
        SetFrankaGrip(1.0f);
    }

    /// <summary>
    /// Set individual finger positions
    /// </summary>
    public void SetFingerPositions(float left, float right)
    {
        finger1 = Mathf.Clamp01(left);
        finger2 = Mathf.Clamp01(right);
        gripperWidth = (finger1 + finger2) / 2.0f;
    }

    void UpdateGripperVisuals()
    {
        // Change color to indicate state
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color gripColor = Color.Lerp(Color.green, Color.red, targetAperture);
            renderer.material.color = gripColor;
        }
    }

    void UpdateUI()
    {
        if (gripperStatusText != null)
        {
            gripperStatusText.text = $"Gripper: {(targetAperture * 100):F0}% | F1: {(finger1 * 100):F0}% | F2: {(finger2 * 100):F0}%";
        }

        if (gripperSlider != null && Mathf.Abs(gripperSlider.value - targetAperture) > 0.01f)
        {
            gripperSlider.value = targetAperture;
        }
    }

    void OnGripperSliderChanged(float value)
    {
        SetFrankaGrip(value);
    }

    /// <summary>
    /// Get current gripper state
    /// </summary>
    public GripperState GetGripperState()
    {
        return new GripperState
        {
            finger1 = finger1,
            finger2 = finger2,
            aperture = targetAperture,
            isOpen = targetAperture < 0.1f,
            isClosed = targetAperture > 0.9f
        };
    }

    [System.Serializable]
    public class GripperState
    {
        public float finger1;
        public float finger2;
        public float aperture;
        public bool isOpen;
        public bool isClosed;
    }
}
