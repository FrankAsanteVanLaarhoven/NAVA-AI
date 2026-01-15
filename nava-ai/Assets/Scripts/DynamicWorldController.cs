using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Dynamic World Controller - Manages living world attributes.
/// Handles day/night cycles, weather effects, and reactive agents (crowds).
/// </summary>
public class DynamicWorldController : MonoBehaviour
{
    [Header("Environment Configuration")]
    [Tooltip("Enable dynamic agents (crowds)")]
    public bool enableAgents = true;

    [Tooltip("Day cycle duration in seconds (2 minutes = 120s)")]
    public float dayCycleDuration = 120.0f;

    [Tooltip("Current time of day (0 = midnight, 0.5 = noon, 1 = midnight)")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.5f;

    [Tooltip("Is currently night time")]
    public bool isNight = false;

    [Header("Weather System")]
    [Tooltip("Rain intensity (0.0 = Dry, 1.0 = Heavy Rain)")]
    [Range(0f, 1f)]
    public float rainIntensity = 0.0f;

    [Tooltip("Surface wetness (0.0 = Dry, 1.0 = Slippery Mud)")]
    [Range(0f, 1f)]
    public float wetness = 0.0f;

    [Tooltip("Fog density")]
    [Range(0f, 1f)]
    public float fogDensity = 0.0f;

    [Tooltip("Wind intensity")]
    [Range(0f, 1f)]
    public float windIntensity = 0.0f;

    [Header("Interaction System")]
    [Tooltip("Layer mask for agents")]
    public LayerMask agentLayer;

    [Tooltip("Layer mask for doors")]
    public LayerMask doorLayer;

    [Tooltip("Layer mask for buttons")]
    public LayerMask buttonLayer;

    [Header("UI References")]
    [Tooltip("Time of day text display")]
    public Text timeText;

    [Tooltip("Weather status text")]
    public Text weatherText;

    [Header("Visual Effects")]
    [Tooltip("Rain particle system")]
    public ParticleSystem rainSystem;

    [Tooltip("Fog particle system")]
    public ParticleSystem fogSystem;

    [Tooltip("Directional light (sun/moon)")]
    public Light directionalLight;

    private float cycleTimer = 0f;
    private AudioSource weatherAudio;
    private Dictionary<GameObject, Vector3> agentVelocities = new Dictionary<GameObject, Vector3>();

    void Start()
    {
        // Initialize weather system
        if (rainSystem == null)
        {
            rainSystem = GetComponentInChildren<ParticleSystem>();
        }

        // Initialize audio
        weatherAudio = GetComponent<AudioSource>();
        if (weatherAudio == null)
        {
            weatherAudio = gameObject.AddComponent<AudioSource>();
            weatherAudio.loop = true;
            weatherAudio.playOnAwake = false;
        }

        // Initialize directional light
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }

        // Set initial state
        UpdateEnvironmentState();

        Debug.Log("[DynamicWorld] Initialized: Agents Active, Day/Night Cycle Ready");
    }

    void Update()
    {
        // 1. Manage Day/Night Cycle
        cycleTimer += Time.deltaTime;
        timeOfDay = (cycleTimer / dayCycleDuration) % 1.0f;

        if (timeOfDay < 0.25f || timeOfDay > 0.75f)
        {
            if (!isNight)
            {
                ToggleDayNight();
            }
        }
        else
        {
            if (isNight)
            {
                ToggleDayNight();
            }
        }

        // 2. Update Agents (React to Robot)
        if (enableAgents)
        {
            UpdateAgents();
        }

        // 3. Update Environment (Visuals)
        UpdateWeatherVisuals();

        // 4. Update UI
        UpdateUI();
    }

    void ToggleDayNight()
    {
        isNight = !isNight;

        // Update ambient lighting
        RenderSettings.ambientIntensity = isNight ? 0.1f : 0.8f;
        RenderSettings.ambientSkyColor = isNight ? Color.blue * 0.2f : Color.white * 0.8f;

        // Update directional light
        if (directionalLight != null)
        {
            directionalLight.intensity = isNight ? 0.3f : 1.0f;
            directionalLight.color = isNight ? Color.blue * 0.8f : Color.white;
        }

        Debug.Log($"[DynamicWorld] Cycle Toggled: {(isNight ? "Night" : "Day")}");

        // Trigger robot night mode
        UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
        if (modelManager != null)
        {
            // SetNightMode would need to be added to UniversalModelManager
            // For now, we just log
            Debug.Log($"[DynamicWorld] Robot night mode: {isNight}");
        }
    }

    void UpdateEnvironmentState()
    {
        // Update friction based on rain/wetness
        PhysicMaterial material = new PhysicMaterial("DynamicSurface");
        material.dynamicFriction = 0.5f + (wetness * 0.3f); // Muddy = Slippery
        material.staticFriction = 0.6f + (wetness * 0.2f);

        // Update audio (rain sound)
        if (rainIntensity > 0.5f)
        {
            if (weatherAudio != null && !weatherAudio.isPlaying)
            {
                weatherAudio.Play();
            }
        }
        else
        {
            if (weatherAudio != null && weatherAudio.isPlaying)
            {
                weatherAudio.Stop();
            }
        }
    }

    void UpdateAgents()
    {
        // Get agent config from VLA policy (simulated)
        UniversalModelManager modelManager = GetComponent<UniversalModelManager>();
        bool avoidActive = modelManager != null; // Simplified check

        // Find robot
        GameObject robot = GameObject.Find("RealRobot");
        if (robot == null)
        {
            robot = GameObject.FindGameObjectWithTag("Player");
        }

        if (robot == null) return;

        Vector3 robotPos = robot.transform.position;

        // Find all agents
        GameObject[] agents = GameObject.FindGameObjectsWithTag("Agent");
        if (agents.Length == 0) return;

        // Simple "Social Force" (Repulsion)
        foreach (GameObject agent in agents)
        {
            if (agent == null) continue;

            Vector3 agentPos = agent.transform.position;
            Vector3 direction = agentPos - robotPos;
            float dist = direction.magnitude;

            if (dist < 5.0f && dist > 0.1f) // 5m proximity threshold
            {
                // If avoiding, move away from robot
                if (avoidActive)
                {
                    Vector3 avoidanceForce = direction.normalized * (5.0f - dist) * 2.0f;
                    
                    // Get or create velocity
                    if (!agentVelocities.ContainsKey(agent))
                    {
                        agentVelocities[agent] = Vector3.zero;
                    }

                    // Update velocity
                    agentVelocities[agent] += avoidanceForce * Time.deltaTime;
                    agentVelocities[agent] = Vector3.ClampMagnitude(agentVelocities[agent], 3.0f);

                    // Apply movement
                    agent.transform.position += agentVelocities[agent] * Time.deltaTime;

                    // Face movement direction
                    if (agentVelocities[agent].magnitude > 0.1f)
                    {
                        agent.transform.rotation = Quaternion.LookRotation(agentVelocities[agent].normalized);
                    }
                }
            }
            else
            {
                // Reset velocity when far from robot
                if (agentVelocities.ContainsKey(agent))
                {
                    agentVelocities[agent] = Vector3.Lerp(agentVelocities[agent], Vector3.zero, Time.deltaTime * 2.0f);
                }
            }
        }
    }

    void UpdateWeatherVisuals()
    {
        // Update rain particle system
        if (rainSystem != null)
        {
            var emission = rainSystem.emission;
            emission.rateOverTime = rainIntensity * 1000f;

            var main = rainSystem.main;
            main.startSpeed = rainIntensity * 10f;
        }

        // Update fog
        RenderSettings.fog = fogDensity > 0.01f || rainIntensity > 0.5f;
        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = Mathf.Max(fogDensity, rainIntensity * 0.01f);
            RenderSettings.fogColor = Color.Lerp(Color.gray, Color.blue, rainIntensity);
        }

        // Update wind (affect particles)
        if (windIntensity > 0.1f && rainSystem != null)
        {
            var forceOverLifetime = rainSystem.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.x = windIntensity * 5f;
        }
    }

    void UpdateUI()
    {
        if (timeText != null)
        {
            int hours = Mathf.FloorToInt(timeOfDay * 24f);
            int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
            timeText.text = $"Time: {hours:D2}:{minutes:D2} {(isNight ? "Night" : "Day")}";
        }

        if (weatherText != null)
        {
            string weatherStatus = "Clear";
            if (rainIntensity > 0.7f)
            {
                weatherStatus = "Heavy Rain";
            }
            else if (rainIntensity > 0.3f)
            {
                weatherStatus = "Rain";
            }
            else if (fogDensity > 0.5f)
            {
                weatherStatus = "Foggy";
            }
            else if (wetness > 0.5f)
            {
                weatherStatus = "Wet";
            }

            weatherText.text = $"Weather: {weatherStatus}";
        }
    }

    /// <summary>
    /// Set rain intensity
    /// </summary>
    public void SetRainIntensity(float intensity)
    {
        rainIntensity = Mathf.Clamp01(intensity);
        wetness = Mathf.Min(1.0f, wetness + intensity * Time.deltaTime * 0.1f);
        UpdateEnvironmentState();
    }

    /// <summary>
    /// Set fog density
    /// </summary>
    public void SetFogDensity(float density)
    {
        fogDensity = Mathf.Clamp01(density);
        UpdateWeatherVisuals();
    }

    /// <summary>
    /// Get current friction modifier (for robot control)
    /// </summary>
    public float GetFrictionModifier()
    {
        return 1.0f - (wetness * 0.3f); // Reduce friction when wet
    }

    /// <summary>
    /// Get visibility modifier (for sensor simulation)
    /// </summary>
    public float GetVisibilityModifier()
    {
        float fogModifier = 1.0f - (fogDensity * 0.5f);
        float rainModifier = 1.0f - (rainIntensity * 0.3f);
        return Mathf.Min(fogModifier, rainModifier);
    }
}
