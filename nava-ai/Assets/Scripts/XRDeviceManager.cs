using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.InputDevices;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// XR Device Manager - Handles XR head tracking, focus zones, and safety visualization.
/// Integrates with "God Mode" system for immersive safety monitoring.
/// </summary>
public class XRDeviceManager : MonoBehaviour
{
    [Header("XR Configuration")]
    public GameObject robotRoot; // The main robot rig in 3D scene
    public TextMeshProUGUI statusText;
    public LayerMask interactionLayer; // For interaction detection (e.g., grabbing handles)
    
    [Header("Focus System")]
    public float focusDistance = 2.0f; // Distance to focus point (Target Object)
    public GameObject focusZonePrefab; // 3D Cube (Wireframe) for focus visualization
    public LineRenderer zoneLines; // 3D Lines (Wireframe Hull)
    
    private bool focusMode = false;
    private GameObject currentFocusZone;
    
    void Start()
    {
        // 1. Enable XR Subsystem
        // Unity's OpenXRSubsystems (OpenXRSettings) manages per-device tracking
        bool tracking = XRSettings.enabled;
        
        // 2. Initialize UI
        if (statusText != null)
        {
            statusText.text = "XR: READY (SIMULATED)";
            statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Accent);
        }
        
        if (!tracking)
        {
            Debug.LogWarning("[XR] Tracking Disabled. Robot may be free-moving (Simulated).");
        }
        
        // 3. Create focus zone if prefab is assigned
        if (focusZonePrefab != null && currentFocusZone == null)
        {
            currentFocusZone = Instantiate(focusZonePrefab);
            currentFocusZone.SetActive(false);
        }
    }

    void Update()
    {
        // 1. Get User Head Pose (In a real setup, use InputDevices.GetCenterEyePosition())
        // For simulation, we'll use Camera.main as fallback
        Vector3 hmdPosition = Vector3.zero;
        Quaternion hmdRotation = Quaternion.identity;
        bool hasHmdPose = false;
        
        if (XRSettings.enabled)
        {
            // Try to get actual XR device pose
            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count > 0)
            {
                if (devices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos) &&
                    devices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rot))
                {
                    hmdPosition = pos;
                    hmdRotation = rot;
                    hasHmdPose = true;
                }
            }
        }
        
        // Fallback to main camera if no XR device
        if (!hasHmdPose && Camera.main != null)
        {
            hmdPosition = Camera.main.transform.position;
            hmdRotation = Camera.main.transform.rotation;
            hasHmdPose = true;
        }
        
        if (hasHmdPose)
        {
            // 2. Update Gaze and Safety Zone
            UpdateGaze(hmdRotation * Vector3.forward, hmdRotation, focusDistance);
            UpdateSafetyZone(hmdPosition, focusMode);
        }
    }

    void UpdateGaze(Vector3 dir, Quaternion headRotation, float viewDistance)
    {
        // 3D Visualization in HMD
        Vector3 facingDir = dir;
        Vector3 targetPos = transform.position + (facingDir * focusDistance * 2.0f);
        
        // Move visualizers
        // In production, this would use a billboard or 3D sprite to guide the user's gaze
        Debug.DrawLine(transform.position, targetPos, Color.green);
        Debug.DrawSphere(targetPos, 0.2f, Color.green); // Green "God Mode" sphere indicator
    }

    void UpdateSafetyZone(Vector3 position, bool isFocused)
    {
        // Draw Target Zone (Green Box) in HMD view
        // In a real app, we'd use a collider at target position
        if (robotRoot != null)
        {
            Collider[] cols = robotRoot.GetComponentsInChildren<Collider>();
            foreach (Collider c in cols)
            {
                if (c.CompareTag("FocusZone"))
                {
                    // Visualize the green zone
                    // In production, this would use a Mesh or Gooch Ball
                    Debug.DrawWireSphere(c.transform.position, 2.0f, Color.green);
                }
            }
        }
        
        // Update focus zone visualization
        if (currentFocusZone != null)
        {
            currentFocusZone.SetActive(isFocused);
            if (isFocused)
            {
                currentFocusZone.transform.position = position;
            }
        }
    }
    
    public void SetFocus(bool isFocused)
    {
        focusMode = isFocused;
        if (statusText != null)
        {
            if (isFocused)
            {
                statusText.text = "XR: FOCUS LOCKED";
                statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Accent);
            }
            else
            {
                statusText.text = "XR: FREE LOOKING";
                statusText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Warning);
            }
        }
    }
    
    public bool IsFocused()
    {
        return focusMode;
    }
}
