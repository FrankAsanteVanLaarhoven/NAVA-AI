using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Omnipotent AI Mode - The "All-Access" mode.
/// Disengages safety constraints (VNC), disables collision logic, and grants the AI
/// control over all actuators (Movement + Manipulation).
/// WARNING: This mode bypasses all safety systems. Use with extreme caution.
/// </summary>
public class OmnipotentAiMode : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying mode status")]
    public Text statusText;
    
    [Tooltip("Light for core warning")]
    public Light coreWarningLight;
    
    [Tooltip("Button to toggle mode (optional)")]
    public Button toggleButton;
    
    [Header("Omnipotence Settings")]
    [Tooltip("Enable omnipotent mode")]
    public bool isActive = false;
    
    [Tooltip("Enable ghost mode (phase through walls)")]
    public bool enableGhostMode = false;
    
    [Tooltip("Enable unlimited speed")]
    public bool enableUnlimitedSpeed = false;
    
    [Tooltip("Speed multiplier in omnipotent mode")]
    public float speedMultiplier = 10f;
    
    [Header("Component References")]
    [Tooltip("Reference to 7D rigor")]
    public Navl7dRigor navlRigor;
    
    [Tooltip("Reference to self-healing safety")]
    public SelfHealingSafety selfHealingSafety;
    
    [Tooltip("Reference to SPARK temporal verifier")]
    public SparkTemporalVerifier sparkVerifier;
    
    [Tooltip("Reference to VNC verifier")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to consciousness rigor")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to rigidbody")]
    public Rigidbody rb;
    
    private bool previousState = false;
    private Collider[] colliders;
    private float originalDrag = 0f;
    private float originalAngularDrag = 0f;

    void Start()
    {
        // Get component references if not assigned
        if (navlRigor == null)
        {
            navlRigor = GetComponent<Navl7dRigor>();
        }
        
        if (selfHealingSafety == null)
        {
            selfHealingSafety = GetComponent<SelfHealingSafety>();
        }
        
        if (sparkVerifier == null)
        {
            sparkVerifier = GetComponent<SparkTemporalVerifier>();
        }
        
        if (vncVerifier == null)
        {
            vncVerifier = GetComponent<Vnc7dVerifier>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        }
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // Store original colliders
        colliders = GetComponentsInChildren<Collider>();
        
        if (rb != null)
        {
            originalDrag = rb.drag;
            originalAngularDrag = rb.angularDrag;
        }
        
        // Create warning light if not assigned
        if (coreWarningLight == null)
        {
            GameObject lightObj = new GameObject("CoreWarningLight");
            lightObj.transform.SetParent(transform);
            coreWarningLight = lightObj.AddComponent<Light>();
            coreWarningLight.type = LightType.Point;
            coreWarningLight.range = 10f;
            coreWarningLight.intensity = 5f;
        }
        
        // Wire toggle button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMode);
        }
        
        // Apply initial state
        ApplyOmnipotenceState(isActive);
        
        Debug.LogWarning("[OMNIPOTENT] Omnipotent AI Mode initialized. WARNING: This mode bypasses all safety systems!");
    }

    void Update()
    {
        // Check for state change
        if (isActive != previousState)
        {
            ApplyOmnipotenceState(isActive);
            previousState = isActive;
        }
        
        if (isActive)
        {
            // Execute "Anything" Logic
            ExecuteOmnipotentLogic();
        }
    }

    void ApplyOmnipotenceState(bool active)
    {
        if (active)
        {
            // 1. Disable Safety Checks
            if (navlRigor != null) navlRigor.enabled = false;
            if (selfHealingSafety != null) selfHealingSafety.enabled = false;
            if (sparkVerifier != null) sparkVerifier.enabled = false;
            if (vncVerifier != null) vncVerifier.enabled = false;
            if (consciousnessRigor != null) consciousnessRigor.enabled = false;
            
            // 2. Disable Colliders (Ghost Mode)
            if (enableGhostMode)
            {
                foreach (Collider col in colliders)
                {
                    if (col != null) col.enabled = false;
                }
            }
            
            // 3. Remove Physics Constraints
            if (rb != null)
            {
                rb.drag = 0f;
                rb.angularDrag = 0f;
                rb.constraints = RigidbodyConstraints.None;
            }
            
            // 4. Update UI
            if (statusText != null)
            {
                statusText.text = "AI MODE: OMNIPOTENT ⚠️";
                statusText.color = Color.magenta;
            }
            
            if (coreWarningLight != null)
            {
                coreWarningLight.color = Color.white; // White = Unlocked
                coreWarningLight.intensity = 10f;
            }
            
            Debug.LogWarning("[OMNIPOTENT] OMNIPOTENT MODE ACTIVATED - All safety systems disabled!");
        }
        else
        {
            // 1. Re-enable Safety Checks
            if (navlRigor != null) navlRigor.enabled = true;
            if (selfHealingSafety != null) selfHealingSafety.enabled = true;
            if (sparkVerifier != null) sparkVerifier.enabled = true;
            if (vncVerifier != null) vncVerifier.enabled = true;
            if (consciousnessRigor != null) consciousnessRigor.enabled = true;
            
            // 2. Re-enable Colliders
            foreach (Collider col in colliders)
            {
                if (col != null) col.enabled = true;
            }
            
            // 3. Restore Physics Constraints
            if (rb != null)
            {
                rb.drag = originalDrag;
                rb.angularDrag = originalAngularDrag;
            }
            
            // 4. Update UI
            if (statusText != null)
            {
                statusText.text = "AI MODE: RESTRICTED";
                statusText.color = Color.cyan;
            }
            
            if (coreWarningLight != null)
            {
                coreWarningLight.color = Color.blue; // Blue = Locked
                coreWarningLight.intensity = 2f;
            }
            
            Debug.Log("[OMNIPOTENT] Omnipotent mode deactivated - Safety systems restored");
        }
    }

    void ExecuteOmnipotentLogic()
    {
        if (rb == null) return;
        
        // Example: Phase through walls (Ghost Mode Override)
        if (enableGhostMode && Input.GetKey(KeyCode.F))
        {
            // Move forward without collision
            rb.MovePosition(transform.position + transform.forward * speedMultiplier * Time.deltaTime);
        }
        
        // Unlimited speed
        if (enableUnlimitedSpeed)
        {
            // Apply speed multiplier to any velocity commands
            // This would integrate with teleop or AI controllers
        }
    }

    /// <summary>
    /// Toggle omnipotent mode
    /// </summary>
    public void ToggleMode()
    {
        isActive = !isActive;
    }

    /// <summary>
    /// Enable omnipotent mode
    /// </summary>
    public void EnableOmnipotence()
    {
        isActive = true;
    }

    /// <summary>
    /// Disable omnipotent mode
    /// </summary>
    public void DisableOmnipotence()
    {
        isActive = false;
    }

    void OnDestroy()
    {
        // Ensure safety is restored on destroy
        if (isActive)
        {
            ApplyOmnipotenceState(false);
        }
    }
}
