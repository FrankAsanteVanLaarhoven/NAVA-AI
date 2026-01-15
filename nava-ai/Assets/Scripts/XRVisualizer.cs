using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// XR Visualizer - Visualizes 3D Safety Zones (Ironclad Hulls) in XR viewport.
/// Projects the "God Mode" safety visualization into XR space with dynamic breathing zones.
/// </summary>
public class XRVisualizer : MonoBehaviour
{
    [Header("Visualization")]
    public GameObject safetyZonePrefab; // A 3D Cube (Wireframe)
    public LineRenderer zoneLines; // 3D lines for wireframe visualization
    public Camera xrCamera; // The main AR/MR Camera
    
    [Header("Safety Zone")]
    public float baseRadius = 5.0f; // Base radius of safety zone
    public float minRadius = 2.0f; // Minimum radius (high certainty)
    public float maxRadius = 15.0f; // Maximum radius (low certainty)
    
    private GameObject safetyZone;
    private NavlConsciousnessRigor consciousnessRigor;
    
    void Start()
    {
        // 1. Enable XR Subsystem
        // Unity 6.3+ requires "God Mode" to be enabled in XR Settings.
        if (!XRSettings.enabled)
        {
            Debug.LogWarning("[XR] XR must be enabled for Safety Zone Visualization.");
        }
        
        // 2. Find consciousness rigor component
        consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        if (consciousnessRigor == null)
        {
            consciousnessRigor = FindObjectOfType<NavlConsciousnessRigor>();
        }
        
        // 3. Initialize Visuals
        if (safetyZonePrefab != null && safetyZone == null)
        {
            // Create the safety zone mesh
            safetyZone = Instantiate(safetyZonePrefab);
            safetyZone.transform.position = Vector3.zero; // Start at origin
            safetyZone.transform.localScale = Vector3.one * baseRadius;
            safetyZone.SetActive(true);
        }
        
        // 4. Initialize Line Renderer if not assigned
        if (zoneLines == null)
        {
            GameObject lineObj = new GameObject("ZoneLines");
            lineObj.transform.SetParent(transform);
            zoneLines = lineObj.AddComponent<LineRenderer>();
            zoneLines.material = new Material(Shader.Find("Sprites/Default"));
            zoneLines.startWidth = 0.1f;
            zoneLines.endWidth = 0.1f;
            zoneLines.useWorldSpace = false;
            zoneLines.loop = true;
        }
    }

    void Update()
    {
        // 1. Get User Pose (In a real setup, we query XRHMD)
        Vector3 hmdPosition = Vector3.zero;
        bool hasHmdPose = false;
        
        if (XRSettings.enabled)
        {
            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count > 0)
            {
                if (devices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                {
                    hmdPosition = pos;
                    hasHmdPose = true;
                }
            }
        }
        
        // Fallback to camera
        if (!hasHmdPose && Camera.main != null)
        {
            hmdPosition = Camera.main.transform.position;
            hasHmdPose = true;
        }
        
        if (hasHmdPose && safetyZone != null)
        {
            // 2. Move "God Mode" Visuals to HMD view
            safetyZone.transform.position = hmdPosition;
        }

        // 3. Update Safety Zone based on P-Score (Certainty)
        float pScore = 100.0f;
        if (consciousnessRigor != null)
        {
            pScore = consciousnessRigor.GetTotalScore();
        }
        
        // Calculate Dynamic Radius: Low P-Score (High Certainty) -> Tight Zone (Small)
        // High P-Score (Low Certainty) -> Loose Zone (Large)
        // Inverse relationship: Lower certainty = larger safety zone
        float normalizedP = Mathf.Clamp01(pScore / 100.0f);
        float dynamicRadius = Mathf.Lerp(minRadius, maxRadius, normalizedP);
        
        // Update safety zone scale
        if (safetyZone != null)
        {
            safetyZone.transform.localScale = Vector3.one * dynamicRadius;
        }

        // 4. Draw 3D Lines (Wireframe Hull) in HMD view
        if (zoneLines != null)
        {
            DrawWireframeSphere(dynamicRadius);
        }
    }

    void DrawWireframeSphere(float radius)
    {
        // Create a wireframe sphere using LineRenderer
        int segments = 32;
        zoneLines.positionCount = segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360.0f / segments) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * radius,
                0.0f,
                Mathf.Sin(angle) * radius
            );
            
            zoneLines.SetPosition(i, pos);
        }
        
        // Set color based on safety level
        float pScore = consciousnessRigor != null ? consciousnessRigor.GetTotalScore() : 100.0f;
        Color zoneColor;
        if (pScore > 80.0f)
        {
            zoneColor = Color.green; // Safe
        }
        else if (pScore > 50.0f)
        {
            zoneColor = Color.yellow; // Caution
        }
        else
        {
            zoneColor = Color.red; // Danger
        }
        
        zoneLines.startColor = zoneColor;
        zoneLines.endColor = zoneColor;
    }
}
