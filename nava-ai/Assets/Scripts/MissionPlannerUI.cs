using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using System.Collections.Generic;

/// <summary>
/// Mission Planner UI for task queue management.
/// Allows high-level autonomy with waypoint-based navigation.
/// </summary>
public class MissionPlannerUI : MonoBehaviour
{
    [System.Serializable]
    public class MissionTask
    {
        public string name = "Task";
        public Vector3 targetPosition;
        public bool completed = false;
        public int priority = 0;
    }
    
    [Header("UI References")]
    [Tooltip("Scrollable list container for tasks")]
    public Transform taskListContainer;
    
    [Tooltip("Task item prefab (optional - will create if null)")]
    public GameObject taskItemPrefab;
    
    [Tooltip("Text showing current task")]
    public Text currentTaskText;
    
    [Header("Waypoint Settings")]
    [Tooltip("Array of waypoint transforms (flags)")]
    public Transform[] wayPoints;
    
    [Tooltip("Waypoint marker prefab")]
    public GameObject waypointMarkerPrefab;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for publishing navigation goals")]
    public string goalTopic = "goal_pose";
    
    [Tooltip("ROS2 topic for goal status feedback")]
    public string goalStatusTopic = "goal_status";
    
    [Header("Mission Settings")]
    [Tooltip("List of mission tasks")]
    public List<MissionTask> missionTasks = new List<MissionTask>();
    
    [Tooltip("Auto-advance to next task when current completes")]
    public bool autoAdvance = true;
    
    private ROSConnection ros;
    private int currentTaskIndex = 0;
    private bool isExecuting = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goalTopic, 10);
        ros.Subscribe<GoalStatusMsg>(goalStatusTopic, OnGoalStatusUpdate);
        
        // Create waypoints from transforms
        if (wayPoints != null && wayPoints.Length > 0)
        {
            CreateWaypointMarkers();
        }
        
        // Initialize UI
        UpdateTaskListUI();
        
        Debug.Log($"[MissionPlanner] Initialized with {missionTasks.Count} tasks");
    }

    void CreateWaypointMarkers()
    {
        for (int i = 0; i < wayPoints.Length; i++)
        {
            if (wayPoints[i] == null) continue;
            
            // Create visual marker if prefab provided
            if (waypointMarkerPrefab != null)
            {
                GameObject marker = Instantiate(waypointMarkerPrefab, wayPoints[i].position, Quaternion.identity);
                marker.name = $"Waypoint_{i}";
            }
            
            // Create task if not already in list
            bool exists = false;
            foreach (var task in missionTasks)
            {
                if (Vector3.Distance(task.targetPosition, wayPoints[i].position) < 0.1f)
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                MissionTask task = new MissionTask
                {
                    name = $"Waypoint {i + 1}",
                    targetPosition = wayPoints[i].position,
                    priority = i
                };
                missionTasks.Add(task);
            }
        }
    }

    void UpdateTaskListUI()
    {
        if (taskListContainer == null) return;
        
        // Clear existing items
        foreach (Transform child in taskListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create UI items for each task
        for (int i = 0; i < missionTasks.Count; i++)
        {
            CreateTaskUIItem(i);
        }
        
        // Update current task text
        if (currentTaskText != null && currentTaskIndex < missionTasks.Count)
        {
            currentTaskText.text = $"Current: {missionTasks[currentTaskIndex].name}";
        }
    }

    void CreateTaskUIItem(int index)
    {
        GameObject item;
        
        if (taskItemPrefab != null)
        {
            item = Instantiate(taskItemPrefab, taskListContainer);
        }
        else
        {
            // Create simple UI item
            item = new GameObject($"TaskItem_{index}");
            item.transform.SetParent(taskListContainer, false);
            
            // Add layout
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 30);
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            text.text = $"{index + 1}. {missionTasks[index].name}";
        }
        
        // Highlight current task
        if (index == currentTaskIndex)
        {
            Image img = item.GetComponent<Image>();
            if (img == null) img = item.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        }
    }

    void OnGoalStatusUpdate(GoalStatusMsg msg)
    {
        // Handle goal status updates from navigation stack
        // Status values: 0=Pending, 1=Active, 2=Preempted, 3=Succeeded, 4=Aborted, 5=Rejected
        
        if (msg.status == 3) // Succeeded
        {
            CompleteCurrentTask();
        }
        else if (msg.status == 4 || msg.status == 5) // Aborted or Rejected
        {
            Debug.LogWarning($"[MissionPlanner] Goal failed with status {msg.status}");
            isExecuting = false;
        }
    }

    /// <summary>
    /// Send next goal to navigation stack
    /// </summary>
    public void SendNextGoal()
    {
        if (currentTaskIndex >= missionTasks.Count)
        {
            Debug.Log("[MissionPlanner] All tasks completed!");
            return;
        }
        
        if (isExecuting)
        {
            Debug.LogWarning("[MissionPlanner] Already executing a task");
            return;
        }
        
        MissionTask task = missionTasks[currentTaskIndex];
        SendGoalToROS(task.targetPosition);
        
        isExecuting = true;
        Debug.Log($"[MissionPlanner] Sending goal: {task.name} to {task.targetPosition}");
    }

    void SendGoalToROS(Vector3 target)
    {
        PoseStampedMsg goal = new PoseStampedMsg();
        goal.header.frame_id = "map"; // Adjust based on your frame
        goal.header.stamp = new RosMessageTypes.Std.TimeMsg();
        
        // Convert Unity position to ROS pose
        goal.pose.position.x = target.x;
        goal.pose.position.y = -target.z; // Unity Z -> ROS Y
        goal.pose.position.z = target.y;  // Unity Y -> ROS Z
        
        // Default orientation (facing forward)
        goal.pose.orientation.w = 1.0;
        
        ros.Publish(goalTopic, goal);
    }

    void CompleteCurrentTask()
    {
        if (currentTaskIndex < missionTasks.Count)
        {
            missionTasks[currentTaskIndex].completed = true;
            Debug.Log($"[MissionPlanner] Task completed: {missionTasks[currentTaskIndex].name}");
        }
        
        isExecuting = false;
        currentTaskIndex++;
        
        UpdateTaskListUI();
        
        if (autoAdvance && currentTaskIndex < missionTasks.Count)
        {
            SendNextGoal();
        }
    }

    /// <summary>
    /// Add a new task to the mission
    /// </summary>
    public void AddTask(string name, Vector3 position, int priority = 0)
    {
        MissionTask task = new MissionTask
        {
            name = name,
            targetPosition = position,
            priority = priority
        };
        missionTasks.Add(task);
        UpdateTaskListUI();
    }

    /// <summary>
    /// Remove a task by index
    /// </summary>
    public void RemoveTask(int index)
    {
        if (index >= 0 && index < missionTasks.Count)
        {
            missionTasks.RemoveAt(index);
            if (currentTaskIndex >= missionTasks.Count)
            {
                currentTaskIndex = Mathf.Max(0, missionTasks.Count - 1);
            }
            UpdateTaskListUI();
        }
    }

    /// <summary>
    /// Start mission execution
    /// </summary>
    public void StartMission()
    {
        currentTaskIndex = 0;
        isExecuting = false;
        SendNextGoal();
    }

    /// <summary>
    /// Stop current mission
    /// </summary>
    public void StopMission()
    {
        isExecuting = false;
        Debug.Log("[MissionPlanner] Mission stopped");
    }
}
