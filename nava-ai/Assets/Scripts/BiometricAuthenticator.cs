using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Biometric Authenticator - FaceID/biometrics integration for user verification.
/// Updates consciousness (c) variable in Ironclad 7D equation based on biometric data.
/// </summary>
public class BiometricAuthenticator : MonoBehaviour
{
    [Header("Biometric Configuration")]
    [Tooltip("User status text display")]
    public Text userStatusText;

    [Tooltip("Authentication threshold")]
    [Range(0f, 1f)]
    public float authThreshold = 0.7f;

    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 1f;

    [Header("Biometric Data")]
    [Tooltip("Current liveness score (0.0 = Dead, 1.0 = Live)")]
    [Range(0f, 1f)]
    public float liveness = 1.0f;

    [Tooltip("Current heart rate (BPM)")]
    [Range(40f, 200f)]
    public float heartRate = 70f;

    [Tooltip("Motion confidence (0.0 = Still, 1.0 = Agitated)")]
    [Range(0f, 1f)]
    public float motionConfidence = 0.0f;

    [Header("Consciousness Calculation")]
    [Tooltip("Current consciousness score")]
    [Range(0f, 1f)]
    public float consciousness = 1.0f;

    private string currentUserId = "";
    private bool isAuthenticated = false;
    private float lastUpdateTime = 0f;

    void Start()
    {
        if (userStatusText != null)
        {
            userStatusText.text = "BIOMETRIC: READY";
        }

        // Start biometric monitoring
        StartCoroutine(BiometricMonitoringCoroutine());
    }

    void Update()
    {
        // Simulate biometric reads (using input for demo)
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateBiometricData();
            CalculateConsciousness();
            UpdateSafetySystems();
            UpdateUI();
            lastUpdateTime = Time.time;
        }
    }

    IEnumerator BiometricMonitoringCoroutine()
    {
        while (true)
        {
            // In production, this would connect to hardware (Jetson API)
            // For now, we simulate with input

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void UpdateBiometricData()
    {
        // Simulate liveness (day/night cycle)
        bool isDayTime = System.DateTime.Now.Hour > 8 && System.DateTime.Now.Hour < 20;
        liveness = isDayTime ? 1.0f : 0.3f;

        // Simulate heart rate (stress detection)
        if (Input.GetKey(KeyCode.Space))
        {
            heartRate = Mathf.Lerp(heartRate, 120f, Time.deltaTime * 2f); // Stress
        }
        else
        {
            heartRate = Mathf.Lerp(heartRate, 70f, Time.deltaTime * 2f); // Rest
        }

        // Simulate motion confidence
        if (Input.GetMouseButton(1))
        {
            motionConfidence = Mathf.Lerp(motionConfidence, 1.0f, Time.deltaTime * 3f); // Agitated
        }
        else
        {
            motionConfidence = Mathf.Lerp(motionConfidence, 0.0f, Time.deltaTime * 3f); // Still
        }
    }

    void CalculateConsciousness()
    {
        // Calculate consciousness score based on biometrics
        // c = f(liveness, heart_rate, motion_conf)

        float livenessScore = liveness;
        float heartRateScore = Mathf.Clamp01(1.0f - Mathf.Abs(heartRate - 70f) / 50f); // Optimal at 70 BPM
        float motionScore = 1.0f - motionConfidence; // Less motion = higher consciousness

        // Weighted average
        consciousness = (livenessScore * 0.5f + heartRateScore * 0.3f + motionScore * 0.2f);

        // Clamp to valid range
        consciousness = Mathf.Clamp01(consciousness);
    }

    void UpdateSafetySystems()
    {
        // Get base P-Score from NavlConsciousnessRigor
        NavlConsciousnessRigor rigor = GetComponent<NavlConsciousnessRigor>();
        float pBase = 50.0f;

        if (rigor != null)
        {
            // Would get total score from rigor
            pBase = 50.0f; // Placeholder
        }

        // Modify P-Score based on biometrics
        float pBio = pBase;

        if (liveness < 0.5f)
        {
            pBio *= 0.8f; // Unconscious/Dead: Penalty
        }
        else if (liveness > 0.8f)
        {
            pBio *= 1.2f; // High Liveness: Bonus
        }

        if (heartRate > 100f)
        {
            pBio *= 0.9f; // Stress: Penalty
        }

        if (motionConfidence > 0.5f)
        {
            pBio *= 0.7f; // Agitated: Penalty
        }

        // Update Navl7dRigor with consciousness
        Navl7dRigor navlRigor = GetComponent<Navl7dRigor>();
        if (navlRigor != null)
        {
            // UpdateConsciousness would need to be added to Navl7dRigor
            // For now, we log
            Debug.Log($"[Biometric] Consciousness updated: {consciousness:F2}, P-Score modifier: {pBio / pBase:F2}");
        }
    }

    void UpdateUI()
    {
        if (userStatusText != null)
        {
            string status = isAuthenticated ? $"AUTH: {currentUserId}" : "AUTH: NONE";
            status += $" | Liveness: {liveness:P0} | HR: {heartRate:F0} BPM | C: {consciousness:F2}";

            userStatusText.text = status;

            // Color coding
            if (consciousness < 0.3f)
            {
                userStatusText.color = Color.red; // Low consciousness
            }
            else if (consciousness < 0.7f)
            {
                userStatusText.color = Color.yellow; // Medium consciousness
            }
            else
            {
                userStatusText.color = Color.green; // High consciousness
            }
        }
    }

    /// <summary>
    /// Update user biometric data
    /// </summary>
    public void UpdateUser(string userId, float livenessValue, float heartRateValue)
    {
        currentUserId = userId;
        liveness = Mathf.Clamp01(livenessValue);
        heartRate = Mathf.Clamp(heartRateValue, 40f, 200f);

        // Check authentication
        isAuthenticated = liveness > authThreshold && heartRate > 50f && heartRate < 150f;

        Debug.Log($"[Biometric] User updated: {userId}, Liveness: {liveness:F2}, HR: {heartRate:F0}, Auth: {isAuthenticated}");
    }

    /// <summary>
    /// Authenticate user
    /// </summary>
    public bool AuthenticateUser(string userId)
    {
        // In production, this would verify against database
        // For now, we check biometric thresholds
        bool authenticated = liveness > authThreshold && consciousness > 0.5f;

        if (authenticated)
        {
            currentUserId = userId;
            isAuthenticated = true;
            Debug.Log($"[Biometric] User {userId} authenticated");
        }
        else
        {
            Debug.LogWarning($"[Biometric] User {userId} authentication failed");
        }

        return authenticated;
    }

    /// <summary>
    /// Get current consciousness score
    /// </summary>
    public float GetConsciousness()
    {
        return consciousness;
    }

    /// <summary>
    /// Get current liveness
    /// </summary>
    public float GetLiveness()
    {
        return liveness;
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        return isAuthenticated;
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    public string GetCurrentUserId()
    {
        return currentUserId;
    }
}
