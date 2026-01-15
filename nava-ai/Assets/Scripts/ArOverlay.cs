using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// AR/XR Overlay - Holographic Dashboard for AR Glasses (HoloLens/Quest Pro).
/// Don't look at a screen. Look at the Real Robot through AR Glasses and see the SPARK Zones floating in mid-air.
/// </summary>
public class ArOverlay : MonoBehaviour
{
    [Header("AR Settings")]
    [Tooltip("Enable AR overlay")]
    public bool enableAR = true;
    
    [Tooltip("Robot root transform")]
    public Transform robotRoot;
    
    [Tooltip("Zone prefab for AR visualization")]
    public GameObject zonePrefab;
    
    [Tooltip("AR overlay material")]
    public Material arMaterial;
    
    [Header("Tracking")]
    [Tooltip("Smooth tracking speed")]
    public float trackingSpeed = 10f;
    
    [Tooltip("Head offset from robot center")]
    public Vector3 headOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Visualization")]
    [Tooltip("Show SPARK zones in AR")]
    public bool showSparkZones = true;
    
    [Tooltip("Show safety boundaries")]
    public bool showSafetyBoundaries = true;
    
    [Tooltip("Show intent vectors")]
    public bool showIntentVectors = true;
    
    [Header("Component References")]
    [Tooltip("Reference to SPARK verifier")]
    public SparkTemporalVerifier sparkVerifier;
    
    [Tooltip("Reference to VNC verifier")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to intent visualizer")]
    public IntentVisualizer intentVisualizer;
    
    private bool isARActive = false;
    private Camera arCamera;
    private GameObject arOverlayRoot;
    private List<GameObject> arZones = new List<GameObject>();

    void Start()
    {
        // Check if XR is active
        isARActive = enableAR && XRSettings.isDeviceActive;
        
        if (!isARActive)
        {
            Debug.Log("[AROverlay] AR not active - running in standard mode");
            return;
        }
        
        // Get AR camera
        arCamera = Camera.main;
        if (arCamera == null)
        {
            arCamera = FindObjectOfType<Camera>();
        }
        
        // Create AR overlay root
        arOverlayRoot = new GameObject("AROverlayRoot");
        arOverlayRoot.transform.SetParent(transform);
        
        // Get component references
        if (sparkVerifier == null)
        {
            sparkVerifier = FindObjectOfType<SparkTemporalVerifier>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = FindObjectOfType<Vnc7dVerifier>();
        }
        
        if (intentVisualizer == null)
        {
            intentVisualizer = FindObjectOfType<IntentVisualizer>();
        }
        
        // Create AR visualizations
        CreateARVisualizations();
        
        Debug.Log("[AROverlay] Initialized - Holographic dashboard ready");
    }

    void Update()
    {
        if (!isARActive || robotRoot == null) return;
        
        // 1. Get Robot Head Position
        Vector3 headPos = GetRobotHeadPosition();
        
        // 2. Update AR overlay position (smooth tracking)
        UpdateARPosition(headPos);
        
        // 3. Visualize SPARK Constraints
        if (showSparkZones)
        {
            UpdateSparkZones();
        }
        
        // 4. Visualize Safety Boundaries
        if (showSafetyBoundaries)
        {
            UpdateSafetyBoundaries();
        }
        
        // 5. Visualize Intent Vectors
        if (showIntentVectors)
        {
            UpdateIntentVectors();
        }
    }

    Vector3 GetRobotHeadPosition()
    {
        if (robotRoot == null) return transform.position;
        
        // Try to find head transform
        Transform headTransform = robotRoot.Find("Head");
        if (headTransform != null)
        {
            return headTransform.position;
        }
        
        // Fallback: Use offset from robot center
        return robotRoot.position + headOffset;
    }

    void UpdateARPosition(Vector3 targetPos)
    {
        // Smooth tracking toward robot head
        if (Vector3.Distance(transform.position, targetPos) > 0.5f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * trackingSpeed);
        }
        else
        {
            // Close enough - maintain position
            transform.position = targetPos;
        }
        
        // Face AR camera
        if (arCamera != null)
        {
            transform.LookAt(arCamera.transform);
            transform.Rotate(0, 180, 0); // Face camera
        }
    }

    void CreateARVisualizations()
    {
        // Create AR zone visualizations
        if (sparkVerifier != null && zonePrefab != null)
        {
            foreach (var zone in sparkVerifier.zones)
            {
                if (!zone.active) continue;
                
                GameObject arZone = Instantiate(zonePrefab);
                arZone.name = $"ARZone_{zone.id}";
                arZone.transform.SetParent(arOverlayRoot.transform);
                arZone.transform.position = zone.bounds.center;
                arZone.transform.localScale = zone.bounds.size;
                
                // Make semi-transparent for AR
                Renderer renderer = arZone.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = arMaterial != null ? new Material(arMaterial) : CreateARMaterial();
                    mat.color = new Color(zone.zoneColor.r, zone.zoneColor.g, zone.zoneColor.b, 0.5f);
                    renderer.material = mat;
                }
                
                arZones.Add(arZone);
            }
        }
    }

    void UpdateSparkZones()
    {
        if (sparkVerifier == null) return;
        
        // Update zone colors based on violations
        for (int i = 0; i < sparkVerifier.zones.Count && i < arZones.Count; i++)
        {
            var zone = sparkVerifier.zones[i];
            GameObject arZone = arZones[i];
            
            if (arZone == null) continue;
            
            // Check if zone is violated
            bool violated = sparkVerifier.HasActiveViolation() && 
                           sparkVerifier.GetViolationHistory().Count > 0;
            
            Renderer renderer = arZone.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color c = violated ? Color.red : zone.zoneColor;
                c.a = 0.5f;
                renderer.material.color = c;
            }
        }
    }

    void UpdateSafetyBoundaries()
    {
        if (vncVerifier == null) return;
        
        // Visualize VNC safety hull in AR
        // This would create AR visualization of the safety boundary
    }

    void UpdateIntentVectors()
    {
        if (intentVisualizer == null) return;
        
        // Visualize intent vectors in AR space
        // This would show where the AI wants to go in 3D AR space
    }

    Material CreateARMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    /// <summary>
    /// Enable/disable AR overlay
    /// </summary>
    public void SetAREnabled(bool enabled)
    {
        enableAR = enabled;
        isARActive = enableAR && XRSettings.isDeviceActive;
        
        if (arOverlayRoot != null)
        {
            arOverlayRoot.SetActive(isARActive);
        }
    }

    /// <summary>
    /// Check if AR is active
    /// </summary>
    public bool IsARActive()
    {
        return isARActive;
    }
}
