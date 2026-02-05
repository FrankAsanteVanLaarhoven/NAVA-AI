using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Mission Profile System - Production Capability.
/// Production robots need to switch roles (Cleaning vs. Delivery) quickly.
/// This system loads/saves JSON profiles to cloud/local disk.
/// </summary>
public class MissionProfileSystem : MonoBehaviour
{
    [System.Serializable]
    public class MissionProfile
    {
        public string profileName;
        public string robotType; // "TurtleBot", "Spot", "Drone"
        public float maxSpeed;
        public List<string> allowedUsers; // Auth
        public Dictionary<string, string> customParams; // Key-Value pairs
        
        public MissionProfile()
        {
            allowedUsers = new List<string>();
            customParams = new Dictionary<string, string>();
        }
    }

    [Header("Profile Settings")]
    [Tooltip("Current profile path")]
    public string currentProfilePath = "Assets/Profiles/default.json";
    
    [Tooltip("Profiles directory")]
    public string profilesDirectory = "Assets/Profiles";
    
    [Header("Current Profile")]
    [Tooltip("Currently loaded profile")]
    public MissionProfile currentProfile;
    
    private string profilesPath;

    void Start()
    {
        // Ensure profiles directory exists
        profilesPath = Path.Combine(Application.dataPath, "Profiles");
        if (!Directory.Exists(profilesPath))
        {
            Directory.CreateDirectory(profilesPath);
        }
        
        // Load or create default profile
        string fullPath = Path.Combine(profilesPath, "default.json");
        if (File.Exists(fullPath))
        {
            LoadProfile(fullPath);
        }
        else
        {
            CreateDefaultProfile();
        }
        
        Debug.Log("[MissionProfile] Profile system initialized");
    }

    /// <summary>
    /// Load profile from file
    /// </summary>
    public void LoadProfile(string path)
    {
        try
        {
            string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(profilesPath, path);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[MissionProfile] Profile not found: {fullPath}");
                return;
            }
            
            string json = File.ReadAllText(fullPath);
            currentProfile = JsonConvert.DeserializeObject<MissionProfile>(json);
            
            if (currentProfile == null)
            {
                Debug.LogError("[MissionProfile] Failed to deserialize profile");
                return;
            }
            
            Debug.Log($"[MissionProfile] Loaded Profile: {currentProfile.profileName}");
            
            // Apply settings to ROS (Jetson)
            ApplyProfileToSystem();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MissionProfile] Error loading profile: {e.Message}");
        }
    }

    /// <summary>
    /// Create default profile
    /// </summary>
    public void CreateDefaultProfile()
    {
        currentProfile = new MissionProfile
        {
            profileName = "Standard Delivery",
            robotType = "TurtleBot",
            maxSpeed = 1.0f,
            allowedUsers = new List<string> { "admin", "operator_1" },
            customParams = new Dictionary<string, string>
            {
                { "safety_alpha", "5.0" },
                { "camera_resolution", "720p" },
                { "battery_threshold", "20.0" }
            }
        };
        
        SaveProfile(currentProfile, "default.json");
        Debug.Log("[MissionProfile] Created default profile");
    }

    /// <summary>
    /// Save profile to file
    /// </summary>
    public void SaveProfile(MissionProfile profile, string filename)
    {
        try
        {
            string fullPath = Path.Combine(profilesPath, filename);
            
            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(fullPath, json);
            
            Debug.Log($"[MissionProfile] Profile Saved to {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MissionProfile] Error saving profile: {e.Message}");
        }
    }

    /// <summary>
    /// Apply profile settings to system
    /// </summary>
    void ApplyProfileToSystem()
    {
        if (currentProfile == null) return;
        
        // Apply max speed
        UnityTeleopController teleop = FindObjectOfType<UnityTeleopController>();
        if (teleop != null)
        {
            teleop.moveSpeed = currentProfile.maxSpeed;
        }
        
        // Apply safety alpha
        if (currentProfile.customParams.ContainsKey("safety_alpha"))
        {
            float alpha = float.Parse(currentProfile.customParams["safety_alpha"]);
            Vnc7dVerifier vnc = FindObjectOfType<Vnc7dVerifier>();
            if (vnc != null)
            {
                vnc.alpha = alpha;
            }
        }
        
        // Publish profile to ROS
        ROSConnection ros = ROSConnection.GetOrCreateInstance();
        if (ros != null)
        {
            // In production: Publish "/robot/config" with profile data
            StringMsg configMsg = new RosMessageTypes.Std.StringMsg
            {
                data = JsonConvert.SerializeObject(currentProfile)
            };
            ros.Publish("/robot/config", configMsg);
        }
        
        Debug.Log($"[MissionProfile] Applied profile: {currentProfile.profileName}");
    }

    /// <summary>
    /// Get list of available profiles
    /// </summary>
    public List<string> GetAvailableProfiles()
    {
        List<string> profiles = new List<string>();
        
        if (!Directory.Exists(profilesPath)) return profiles;
        
        string[] files = Directory.GetFiles(profilesPath, "*.json");
        foreach (string file in files)
        {
            profiles.Add(Path.GetFileNameWithoutExtension(file));
        }
        
        return profiles;
    }

    /// <summary>
    /// Switch to a different profile
    /// </summary>
    public void SwitchProfile(string profileName)
    {
        string profilePath = Path.Combine(profilesPath, profileName + ".json");
        LoadProfile(profilePath);
    }
}
