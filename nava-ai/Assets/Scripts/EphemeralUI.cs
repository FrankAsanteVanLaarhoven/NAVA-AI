using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Ephemeral UI - Context-Aware HUD with Glassmorphism and Voice-Triggered Contexts.
/// UI dissolves into background when idle and "explodes" into detailed data when triggered.
/// </summary>
public class EphemeralUI : MonoBehaviour
{
    [Header("UI Groups")]
    [Tooltip("Main HUD panel (always visible, fades when idle)")]
    public CanvasGroup hudGroup;
    
    [Tooltip("Detail panel (hidden by default, expands on command)")]
    public CanvasGroup detailGroup;
    
    [Tooltip("Background blur/glassmorphism effect")]
    public Image backgroundBlur;
    
    [Header("Visual Effects")]
    [Tooltip("Particle system for UI activation effect")]
    public ParticleSystem uiParticles;
    
    [Tooltip("Animation curve for fade transitions")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Ephemeral Settings")]
    [Tooltip("Fade out delay when idle (seconds)")]
    public float idleFadeDelay = 5f;
    
    [Tooltip("Fade speed")]
    public float fadeSpeed = 2f;
    
    [Tooltip("Minimum alpha when faded (0 = invisible, 0.3 = subtle)")]
    public float minAlpha = 0.3f;
    
    [Header("Glassmorphism")]
    [Tooltip("Enable glassmorphism blur effect")]
    public bool enableGlassmorphism = true;
    
    [Tooltip("Blur intensity")]
    public float blurIntensity = 5f;
    
    [Tooltip("Background transparency")]
    public float backgroundAlpha = 0.7f;
    
    private bool isExpanded = false;
    private bool isFading = false;
    private float lastInteractionTime = 0f;
    private Coroutine fadeCoroutine;
    private Material blurMaterial;

    void Start()
    {
        // Initialize UI states
        if (hudGroup != null)
        {
            hudGroup.alpha = 1f;
        }
        
        if (detailGroup != null)
        {
            detailGroup.alpha = 0f;
            detailGroup.gameObject.SetActive(false);
        }
        
        // Create blur material if needed
        if (enableGlassmorphism && backgroundBlur != null)
        {
            blurMaterial = new Material(Shader.Find("UI/Default"));
            blurMaterial.SetFloat("_Blur", blurIntensity);
            backgroundBlur.material = blurMaterial;
            
            Color bgColor = backgroundBlur.color;
            bgColor.a = backgroundAlpha;
            backgroundBlur.color = bgColor;
        }
        
        // Start idle fade monitoring
        StartCoroutine(MonitorIdleFade());
        
        Debug.Log("[EphemeralUI] Initialized - Context-aware HUD ready");
    }

    void Update()
    {
        // Update last interaction time on any input
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            lastInteractionTime = Time.time;
            if (hudGroup != null && hudGroup.alpha < 1f)
            {
                ExpandHUD();
            }
        }
    }

    /// <summary>
    /// Expand HUD (show details) - called by voice command or interaction
    /// </summary>
    public void ExpandHUD()
    {
        if (isExpanded || isFading) return;
        
        StartCoroutine(ExpandHUDCoroutine());
    }

    IEnumerator ExpandHUDCoroutine()
    {
        isExpanded = true;
        isFading = true;
        
        // 1. Flash Effect
        if (uiParticles != null)
        {
            uiParticles.Play();
        }
        
        // 2. Fade in main HUD if faded
        if (hudGroup != null && hudGroup.alpha < 1f)
        {
            yield return StartCoroutine(FadeCanvasGroup(hudGroup, hudGroup.alpha, 1f, fadeSpeed));
        }
        
        // 3. Show and fade in detail group
        if (detailGroup != null)
        {
            detailGroup.gameObject.SetActive(true);
            detailGroup.alpha = 0f;
            yield return StartCoroutine(FadeCanvasGroup(detailGroup, 0f, 1f, fadeSpeed));
        }
        
        // 4. Update blur
        if (enableGlassmorphism && backgroundBlur != null)
        {
            yield return StartCoroutine(FadeBlur(backgroundBlur.color.a, backgroundAlpha, fadeSpeed));
        }
        
        isFading = false;
        lastInteractionTime = Time.time;
    }

    /// <summary>
    /// Collapse HUD (hide details) - called automatically after idle
    /// </summary>
    public void CollapseHUD()
    {
        if (!isExpanded || isFading) return;
        
        StartCoroutine(CollapseHUDCoroutine());
    }

    IEnumerator CollapseHUDCoroutine()
    {
        isFading = true;
        
        // 1. Fade out detail group
        if (detailGroup != null && detailGroup.alpha > 0f)
        {
            yield return StartCoroutine(FadeCanvasGroup(detailGroup, detailGroup.alpha, 0f, fadeSpeed));
            detailGroup.gameObject.SetActive(false);
        }
        
        // 2. Fade out main HUD to minimum alpha
        if (hudGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(hudGroup, hudGroup.alpha, minAlpha, fadeSpeed));
        }
        
        // 3. Reduce blur
        if (enableGlassmorphism && backgroundBlur != null)
        {
            yield return StartCoroutine(FadeBlur(backgroundBlur.color.a, backgroundAlpha * 0.3f, fadeSpeed));
        }
        
        isFading = false;
        isExpanded = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float targetAlpha, float speed)
    {
        float elapsed = 0f;
        float duration = Mathf.Abs(targetAlpha - startAlpha) / speed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        group.alpha = targetAlpha;
    }

    IEnumerator FadeBlur(float startAlpha, float targetAlpha, float speed)
    {
        if (backgroundBlur == null) yield break;
        
        float elapsed = 0f;
        float duration = Mathf.Abs(targetAlpha - startAlpha) / speed;
        Color startColor = backgroundBlur.color;
        Color targetColor = startColor;
        targetColor.a = targetAlpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / duration);
            Color currentColor = Color.Lerp(startColor, targetColor, t);
            backgroundBlur.color = currentColor;
            yield return null;
        }
        
        backgroundBlur.color = targetColor;
    }

    IEnumerator MonitorIdleFade()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Check if idle
            if (Time.time - lastInteractionTime > idleFadeDelay)
            {
                if (isExpanded)
                {
                    CollapseHUD();
                }
                else if (hudGroup != null && hudGroup.alpha > minAlpha)
                {
                    // Fade main HUD
                    if (fadeCoroutine != null)
                    {
                        StopCoroutine(fadeCoroutine);
                    }
                    fadeCoroutine = StartCoroutine(FadeCanvasGroup(hudGroup, hudGroup.alpha, minAlpha, fadeSpeed));
                }
            }
        }
    }

    /// <summary>
    /// Triggered by voice command
    /// </summary>
    public void OnVoiceCommand(string command)
    {
        lastInteractionTime = Time.time;
        
        // Parse command
        command = command.ToLower();
        
        if (command.Contains("expand") || command.Contains("show") || command.Contains("details"))
        {
            ExpandHUD();
        }
        else if (command.Contains("collapse") || command.Contains("hide") || command.Contains("minimize"))
        {
            CollapseHUD();
        }
        else
        {
            // Default: expand on any voice command
            ExpandHUD();
        }
    }

    /// <summary>
    /// Force show UI (no fade)
    /// </summary>
    public void ForceShow()
    {
        if (hudGroup != null) hudGroup.alpha = 1f;
        if (detailGroup != null)
        {
            detailGroup.alpha = 1f;
            detailGroup.gameObject.SetActive(true);
        }
        isExpanded = true;
        lastInteractionTime = Time.time;
    }

    /// <summary>
    /// Force hide UI
    /// </summary>
    public void ForceHide()
    {
        if (hudGroup != null) hudGroup.alpha = minAlpha;
        if (detailGroup != null)
        {
            detailGroup.alpha = 0f;
            detailGroup.gameObject.SetActive(false);
        }
        isExpanded = false;
    }
}
