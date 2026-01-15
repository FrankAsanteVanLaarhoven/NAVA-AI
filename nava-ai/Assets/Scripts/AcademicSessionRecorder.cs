using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Academic Session Recorder - Academia Capability.
/// Records Student Sessions (Name, Start Time, Mission Profile) and saves
/// a replayable JSON file. Essential for Newcastle grading.
/// </summary>
public class AcademicSessionRecorder : MonoBehaviour
{
    [System.Serializable]
    public class SessionRecord
    {
        public string studentName;
        public string sessionID;
        public System.DateTime startTime;
        public System.DateTime endTime;
        public string missionType;
        public float duration;
        public Dictionary<string, object> sessionData;
        
        public SessionRecord()
        {
            sessionData = new Dictionary<string, object>();
        }
    }

    [Header("Session Settings")]
    [Tooltip("Student name")]
    public string studentName = "Student Name";
    
    [Tooltip("Session log directory")]
    public string logDirectory = "Assets/Research";
    
    [Tooltip("Auto-save on session end")]
    public bool autoSave = true;
    
    [Header("Current Session")]
    [Tooltip("Currently active session")]
    public SessionRecord currentSession;
    
    [Tooltip("All recorded sessions")]
    public List<SessionRecord> sessions = new List<SessionRecord>();
    
    private string logPath;
    private bool isRecording = false;

    void Start()
    {
        // Ensure log directory exists
        logPath = Path.Combine(Application.dataPath, "Research");
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }
        
        Debug.Log("[AcademicSession] Session recorder initialized");
    }

    /// <summary>
    /// Start a new session
    /// </summary>
    [ContextMenu("Academia/Start Session")]
    public void StartSession()
    {
        if (isRecording)
        {
            Debug.LogWarning("[AcademicSession] Session already in progress");
            return;
        }
        
        currentSession = new SessionRecord
        {
            studentName = studentName,
            sessionID = System.Guid.NewGuid().ToString(),
            startTime = System.DateTime.Now,
            missionType = "Baseline Navigation"
        };
        
        isRecording = true;
        
        Debug.Log($"[AcademicSession] Session Started: {currentSession.studentName} ({currentSession.sessionID})");
    }

    /// <summary>
    /// End current session
    /// </summary>
    [ContextMenu("Academia/End Session")]
    public void EndSession()
    {
        if (!isRecording || currentSession == null)
        {
            Debug.LogWarning("[AcademicSession] No active session to end");
            return;
        }
        
        currentSession.endTime = System.DateTime.Now;
        currentSession.duration = (float)(currentSession.endTime - currentSession.startTime).TotalSeconds;
        currentSession.missionType = "COMPLETED";
        
        // Add to sessions list
        sessions.Add(currentSession);
        
        // Auto-save if enabled
        if (autoSave)
        {
            SaveLog();
        }
        
        Debug.Log($"[AcademicSession] Session Ended: Duration {currentSession.duration:F1}s");
        
        currentSession = null;
        isRecording = false;
    }

    /// <summary>
    /// Save session log
    /// </summary>
    public void SaveLog()
    {
        try
        {
            string filename = Path.Combine(logPath, "session_log.json");
            
            // Convert to JSON
            string json = JsonConvert.SerializeObject(sessions, Formatting.Indented, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
            });
            
            File.WriteAllText(filename, json);
            
            Debug.Log($"[AcademicSession] Log Saved to {filename} ({sessions.Count} sessions)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AcademicSession] Error saving log: {e.Message}");
        }
    }

    /// <summary>
    /// Load session log
    /// </summary>
    public void LoadLog()
    {
        try
        {
            string filename = Path.Combine(logPath, "session_log.json");
            
            if (!File.Exists(filename))
            {
                Debug.LogWarning("[AcademicSession] No log file found");
                return;
            }
            
            string json = File.ReadAllText(filename);
            sessions = JsonConvert.DeserializeObject<List<SessionRecord>>(json);
            
            Debug.Log($"[AcademicSession] Loaded {sessions.Count} sessions from log");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AcademicSession] Error loading log: {e.Message}");
        }
    }

    /// <summary>
    /// Add data to current session
    /// </summary>
    public void AddSessionData(string key, object value)
    {
        if (currentSession != null)
        {
            currentSession.sessionData[key] = value;
        }
    }

    /// <summary>
    /// Get current session
    /// </summary>
    public SessionRecord GetCurrentSession()
    {
        return currentSession;
    }

    /// <summary>
    /// Check if recording
    /// </summary>
    public bool IsRecording()
    {
        return isRecording;
    }

    void Update()
    {
        // Update session duration if recording
        if (isRecording && currentSession != null)
        {
            currentSession.duration = (float)(System.DateTime.Now - currentSession.startTime).TotalSeconds;
        }
    }
}
