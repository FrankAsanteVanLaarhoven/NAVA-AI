using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// Procedural Audio Manager - Generates sound procedurally based on events.
/// Reactive audio system for collisions, footsteps, rain, and environmental sounds.
/// </summary>
public class ProceduralAudioManager : MonoBehaviour
{
    [Header("Audio Configuration")]
    [Tooltip("Audio mixer for mixing")]
    public AudioMixer mixer;

    [Tooltip("Master volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1.0f;

    [Header("Volume Settings")]
    [Tooltip("Footstep volume")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.1f;

    [Tooltip("Rain volume")]
    [Range(0f, 1f)]
    public float rainVolume = 0.3f;

    [Tooltip("Impact/collision volume")]
    [Range(0f, 1f)]
    public float impactVolume = 1.0f;

    [Tooltip("Ambient volume")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.2f;

    [Header("Audio Sources")]
    [Tooltip("Footstep audio source")]
    public AudioSource footstepSource;

    [Tooltip("Rain audio source")]
    public AudioSource rainSource;

    [Tooltip("Impact audio source")]
    public AudioSource impactSource;

    [Tooltip("Ambient audio source")]
    public AudioSource ambientSource;

    private Dictionary<string, AudioSource> audioGenerators = new Dictionary<string, AudioSource>();
    private Dictionary<GameObject, AudioSource> objectAudioSources = new Dictionary<GameObject, AudioSource>();

    void Start()
    {
        // Initialize audio mixer
        if (mixer == null)
        {
            mixer = Resources.Load<AudioMixer>("MasterMixer");
        }

        // Create audio sources if not assigned
        CreateAudioSources();

        // Register generators
        RegisterGenerator("Footsteps", footstepSource);
        RegisterGenerator("Rain", rainSource);
        RegisterGenerator("Collision", impactSource);
        RegisterGenerator("Ambient", ambientSource);

        Debug.Log("[ProceduralAudio] Initialized audio system");
    }

    void CreateAudioSources()
    {
        // Create footstep source
        if (footstepSource == null)
        {
            GameObject footstepObj = new GameObject("FootstepAudio");
            footstepObj.transform.SetParent(transform);
            footstepSource = footstepObj.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 1.0f; // 3D sound
            footstepSource.volume = footstepVolume;
        }

        // Create rain source
        if (rainSource == null)
        {
            GameObject rainObj = new GameObject("RainAudio");
            rainObj.transform.SetParent(transform);
            rainSource = rainObj.AddComponent<AudioSource>();
            rainSource.playOnAwake = false;
            rainSource.loop = true;
            rainSource.spatialBlend = 0.0f; // 2D sound
            rainSource.volume = rainVolume;
        }

        // Create impact source
        if (impactSource == null)
        {
            GameObject impactObj = new GameObject("ImpactAudio");
            impactObj.transform.SetParent(transform);
            impactSource = impactObj.AddComponent<AudioSource>();
            impactSource.playOnAwake = false;
            impactSource.loop = false;
            impactSource.spatialBlend = 1.0f; // 3D sound
            impactSource.volume = impactVolume;
        }

        // Create ambient source
        if (ambientSource == null)
        {
            GameObject ambientObj = new GameObject("AmbientAudio");
            ambientObj.transform.SetParent(transform);
            ambientSource = ambientObj.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0.0f; // 2D sound
            ambientSource.volume = ambientVolume;
        }
    }

    void RegisterGenerator(string eventName, AudioSource source)
    {
        if (source != null)
        {
            audioGenerators[eventName] = source;
        }
    }

    /// <summary>
    /// Play footstep sound at position
    /// </summary>
    public void PlayFootstep(GameObject agent, Vector3 position)
    {
        if (!audioGenerators.ContainsKey("Footsteps")) return;

        AudioSource source = audioGenerators["Footsteps"];

        // Get or create audio source for this agent
        AudioSource agentSource = GetOrCreateAgentAudioSource(agent, source);

        // Position audio source
        agentSource.transform.position = position;

        // Pitch variation (audio panning)
        float pan = Mathf.Clamp(position.x / 100.0f, -1f, 1f); // -1 to 1 map width
        agentSource.panStereoPan = pan;

        // Pitch variation for realism
        agentSource.pitch = Random.Range(0.8f, 1.2f);

        // Volume based on distance (if 3D)
        float distance = Vector3.Distance(position, Camera.main.transform.position);
        agentSource.volume = footstepVolume * Mathf.Clamp01(1.0f - distance / 20.0f);

        // Play sound
        if (!agentSource.isPlaying)
        {
            agentSource.Play();
        }
    }

    /// <summary>
    /// Play rain sound
    /// </summary>
    public void PlayRain(float intensity)
    {
        if (!audioGenerators.ContainsKey("Rain")) return;

        AudioSource source = audioGenerators["Rain"];
        source.volume = rainVolume * intensity;

        if (intensity > 0.1f && !source.isPlaying)
        {
            source.Play();
        }
        else if (intensity <= 0.1f && source.isPlaying)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// Play impact/collision sound
    /// </summary>
    public void PlayImpact(Vector3 position, float intensity = 1.0f)
    {
        if (!audioGenerators.ContainsKey("Collision")) return;

        AudioSource source = audioGenerators["Collision"];

        // Position audio source
        source.transform.position = position;

        // Volume based on intensity
        source.volume = impactVolume * intensity;

        // Pitch variation
        source.pitch = Random.Range(0.7f, 1.3f);

        // Play one-shot
        source.PlayOneShot(source.clip);
    }

    /// <summary>
    /// Play ambient sound
    /// </summary>
    public void PlayAmbient()
    {
        if (!audioGenerators.ContainsKey("Ambient")) return;

        AudioSource source = audioGenerators["Ambient"];

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    /// <summary>
    /// Stop ambient sound
    /// </summary>
    public void StopAmbient()
    {
        if (!audioGenerators.ContainsKey("Ambient")) return;

        AudioSource source = audioGenerators["Ambient"];
        if (source.isPlaying)
        {
            source.Stop();
        }
    }

    AudioSource GetOrCreateAgentAudioSource(GameObject agent, AudioSource template)
    {
        if (objectAudioSources.ContainsKey(agent))
        {
            return objectAudioSources[agent];
        }

        // Create new audio source for this agent
        GameObject audioObj = new GameObject($"Audio_{agent.name}");
        audioObj.transform.SetParent(agent.transform);
        audioObj.transform.localPosition = Vector3.zero;

        AudioSource newSource = audioObj.AddComponent<AudioSource>();
        newSource.playOnAwake = template.playOnAwake;
        newSource.loop = template.loop;
        newSource.spatialBlend = template.spatialBlend;
        newSource.volume = template.volume;
        newSource.clip = template.clip;

        objectAudioSources[agent] = newSource;
        return newSource;
    }

    /// <summary>
    /// Generate procedural sound (synthesis placeholder)
    /// </summary>
    public void GenerateProceduralSound(string soundType, float frequency, float duration)
    {
        // In production, this would use AudioClip.Create() to generate sound
        // For now, we use existing clips with pitch modification

        AudioSource source = null;
        switch (soundType)
        {
            case "Beep":
                source = impactSource;
                break;
            case "Tone":
                source = ambientSource;
                break;
        }

        if (source != null)
        {
            source.pitch = frequency / 440f; // Normalize to A4
            source.Play();
            StartCoroutine(StopSoundAfterDelay(source, duration));
        }
    }

    System.Collections.IEnumerator StopSoundAfterDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// Set master volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
    }

    /// <summary>
    /// Update audio based on weather
    /// </summary>
    public void UpdateWeatherAudio(float rainIntensity, float windIntensity)
    {
        PlayRain(rainIntensity);

        // Wind sound (if implemented)
        if (windIntensity > 0.5f)
        {
            // Would play wind sound here
        }
    }
}
