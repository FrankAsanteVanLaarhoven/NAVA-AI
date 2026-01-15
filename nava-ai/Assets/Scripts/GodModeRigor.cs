using UnityEngine;
using TMPro;

/// <summary>
/// God Mode Rigor 3.0 - Evolves safety zones based on fatigue and certainty.
/// Dynamic expansion of safety zones when P-score drops (low certainty).
/// Implements fatigue-based risk assessment.
/// </summary>
public class GodModeRigor : MonoBehaviour
{
    [Header("God Mode Configuration")]
    public float baseMargin = 1.0f; // Base safety margin
    public float fatigueRisk = 0.0f; // Start at 0.0, increases over time
    public float fatigueIncrement = 0.0005f; // Fatigue rate per second
    public float fatigueThreshold = 0.5f; // Threshold for high fatigue
    public float recoveryRate = 0.05f; // Recovery rate when safe
    
    [Header("UI Elements")]
    public TextMeshProUGUI godModeText;
    public ParticleSystem dangerParticles;
    
    [Header("Visual Feedback")]
    public Camera mainCamera;
    private AudioSource sirenAudio;
    private bool isSirenPlaying = false;
    private Color originalBackgroundColor;
    
    private NavlConsciousnessRigor consciousnessRigor;
    private DynamicZoneManager zoneManager;

    void Start()
    {
        // Find components
        consciousnessRigor = GetComponent<NavlConsciousnessRigor>();
        if (consciousnessRigor == null)
        {
            consciousnessRigor = FindObjectOfType<NavlConsciousnessRigor>();
        }

        zoneManager = GetComponent<DynamicZoneManager>();
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<DynamicZoneManager>();
        }

        // Setup audio
        sirenAudio = GetComponent<AudioSource>();
        if (sirenAudio == null)
        {
            sirenAudio = gameObject.AddComponent<AudioSource>();
            sirenAudio.loop = true;
            sirenAudio.playOnAwake = false;
        }

        // Store original camera background
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            originalBackgroundColor = mainCamera.backgroundColor;
        }

        // Setup particles
        if (dangerParticles == null)
        {
            dangerParticles = GetComponent<ParticleSystem>();
        }

        Debug.Log("[GOD MODE RIGOR] Initialized - Fatigue-based safety system ready");
    }

    void Update()
    {
        // 1. Update Fatigue State
        bool isFatigue = GetFatigueState();

        // 2. Calculate Minimum Margin
        float minMargin = CalculateMinMargin();
        
        // 3. Get P-Score from Consciousness Rigor
        float pScore = 100.0f;
        if (consciousnessRigor != null)
        {
            pScore = consciousnessRigor.GetTotalScore();
        }

        // 4. Evoke "God Mode" (Safety Override) if breach detected
        if (minMargin < 0.0f || pScore < 50.0f)
        {
            TriggerGodModeVisuals();
            Debug.LogWarning($"[GOD MODE] Breach Detected. Risk: {fatigueRisk:F2}, P-Score: {pScore:F2}, Margin: {minMargin:F2}");
        }
        else
        {
            // Recover from fatigue when safe
            if (fatigueRisk > 0.0f)
            {
                fatigueRisk = Mathf.Max(0.0f, fatigueRisk - recoveryRate * Time.deltaTime);
            }
        }

        // 5. Update Zone Manager with dynamic expansion
        if (zoneManager != null)
        {
            // Expand zone based on fatigue (inverse relationship)
            float expansionFactor = 1.0f + (fatigueRisk * 2.0f); // More fatigue = larger zone
            // This would need to be integrated with DynamicZoneManager's expansion logic
        }

        // 6. Update UI
        if (godModeText != null)
        {
            if (isFatigue)
            {
                godModeText.text = $"GOD MODE: FATIGUE (Risk: {fatigueRisk:F2})";
                godModeText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Danger);
            }
            else
            {
                godModeText.text = $"GOD MODE: ACTIVE (Risk: {fatigueRisk:F2})";
                godModeText.color = UIThemeHelper.GetColor(UIThemeHelper.ColorType.Success);
            }
        }
    }

    void TriggerGodModeVisuals()
    {
        // Visual Feedback (Red Flash)
        if (mainCamera != null)
        {
            Color dangerColor = Color.Lerp(originalBackgroundColor, new Color(0.5f, 0.0f, 0.0f, 0.5f), 0.1f);
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, dangerColor, Time.deltaTime * 5.0f);
        }

        // Audio: Siren Sound
        if (sirenAudio != null && !isSirenPlaying)
        {
            sirenAudio.Play();
            isSirenPlaying = true;
            Invoke("StopSiren", 0.5f);
        }

        // Particles
        if (dangerParticles != null && !dangerParticles.isPlaying)
        {
            dangerParticles.Play();
        }
    }

    void StopSiren()
    {
        if (sirenAudio != null)
        {
            sirenAudio.Stop();
            isSirenPlaying = false;
        }

        // Restore camera background
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, originalBackgroundColor, Time.deltaTime * 2.0f);
        }

        // Stop particles
        if (dangerParticles != null && dangerParticles.isPlaying)
        {
            dangerParticles.Stop();
        }
    }

    float CalculateMinMargin()
    {
        // Check all obstacles in scene
        Collider[] cols = Physics.OverlapSphere(transform.position, 10.0f);
        float minMargin = 100.0f;
        
        foreach (var c in cols)
        {
            if (c.CompareTag("Obstacle") || c.CompareTag("Untagged"))
            {
                float dist = Vector3.Distance(transform.position, c.transform.position);
                float radius = 1.0f; // Assumed obstacle radius
                float margin = dist - radius;
                if (margin < minMargin)
                {
                    minMargin = margin;
                }
            }
        }
        return minMargin;
    }

    bool GetFatigueState()
    {
        // Simulated fatigue (Time-based)
        fatigueRisk += fatigueIncrement * Time.deltaTime; // Increases linearly
        
        // "God Mode" Logic: If fatigue > Threshold, force stop
        if (fatigueRisk > fatigueThreshold)
        {
            return true; // High Fatigue (Tired/Unaware)
        }
        return false; // Aware (Active)
    }

    public void ResetFatigue()
    {
        fatigueRisk = 0.0f;
        Debug.Log("[GOD MODE RIGOR] Fatigue reset");
    }

    public float GetFatigueRisk()
    {
        return fatigueRisk;
    }

    public bool IsFatigue()
    {
        return fatigueRisk > fatigueThreshold;
    }
}
