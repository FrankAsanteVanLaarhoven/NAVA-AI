using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Lecture Annotation Tool - Academia Capability.
/// Allows a Professor to leave "digital sticky notes" on the 3D scene.
/// These notes persist with the Replay and can be exported for grading.
/// </summary>
public class LectureAnnotationTool : MonoBehaviour
{
    [System.Serializable]
    public class Annotation
    {
        public string annotationID;
        public Vector3 position;
        public string text;
        public System.DateTime timestamp;
        public string author;
        public Color color;
    }

    [Header("Annotation Settings")]
    [Tooltip("Note prefab (3D Text or Icon)")]
    public GameObject notePrefab;
    
    [Tooltip("Annotation color")]
    public Color annotationColor = Color.yellow;
    
    [Tooltip("Author name")]
    public string authorName = "Professor";
    
    [Header("UI References")]
    [Tooltip("Input field for annotation text")]
    public InputField annotationInputField;
    
    [Tooltip("Panel for annotation UI")]
    public GameObject annotationPanel;
    
    [Header("Persistence")]
    [Tooltip("Save annotations to file")]
    public bool saveAnnotations = true;
    
    [Tooltip("Annotations file path")]
    public string annotationsPath = "Assets/Research/annotations.json";
    
    private List<Annotation> annotations = new List<Annotation>();
    private List<GameObject> annotationObjects = new List<GameObject>();
    private Annotation currentAnnotation;
    private bool isPlacingAnnotation = false;

    void Start()
    {
        // Load existing annotations
        if (saveAnnotations)
        {
            LoadAnnotations();
        }
        
        Debug.Log("[AnnotationTool] Lecture annotation tool initialized");
    }

    void Update()
    {
        // Left click to place annotation
        if (Input.GetMouseButtonDown(0) && !isPlacingAnnotation)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                StartPlacingAnnotation(hit.point);
            }
        }
        
        // Right click to cancel
        if (Input.GetMouseButtonDown(1) && isPlacingAnnotation)
        {
            CancelPlacingAnnotation();
        }
    }

    /// <summary>
    /// Start placing annotation
    /// </summary>
    void StartPlacingAnnotation(Vector3 position)
    {
        isPlacingAnnotation = true;
        currentAnnotation = new Annotation
        {
            annotationID = System.Guid.NewGuid().ToString(),
            position = position,
            text = "",
            timestamp = System.DateTime.Now,
            author = authorName,
            color = annotationColor
        };
        
        // Show input panel
        if (annotationPanel != null)
        {
            annotationPanel.SetActive(true);
        }
        
        if (annotationInputField != null)
        {
            annotationInputField.text = "";
            annotationInputField.Select();
            annotationInputField.ActivateInputField();
        }
        
        Debug.Log($"[AnnotationTool] Placing annotation at {position}");
    }

    /// <summary>
    /// Confirm annotation
    /// </summary>
    public void ConfirmAnnotation()
    {
        if (currentAnnotation == null) return;
        
        // Get text from input field
        if (annotationInputField != null)
        {
            currentAnnotation.text = annotationInputField.text;
        }
        
        if (string.IsNullOrEmpty(currentAnnotation.text))
        {
            currentAnnotation.text = "Double click to edit note.";
        }
        
        // Create note object
        CreateNote(currentAnnotation);
        
        // Add to list
        annotations.Add(currentAnnotation);
        
        // Save if enabled
        if (saveAnnotations)
        {
            SaveAnnotations();
        }
        
        // Store text before reset
        string annotationText = currentAnnotation.text;
        
        // Reset
        currentAnnotation = null;
        isPlacingAnnotation = false;
        
        if (annotationPanel != null)
        {
            annotationPanel.SetActive(false);
        }
        
        Debug.Log($"[AnnotationTool] Annotation created: {annotationText}");
    }

    /// <summary>
    /// Cancel placing annotation
    /// </summary>
    void CancelPlacingAnnotation()
    {
        currentAnnotation = null;
        isPlacingAnnotation = false;
        
        if (annotationPanel != null)
        {
            annotationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Create note object in scene
    /// </summary>
    void CreateNote(Annotation annotation)
    {
        GameObject note;
        
        if (notePrefab != null)
        {
            note = Instantiate(notePrefab, annotation.position, Quaternion.identity);
        }
        else
        {
            // Create simple text object
            note = new GameObject($"Annotation_{annotation.annotationID}");
            note.transform.position = annotation.position;
            
            // Add text component
            TextMesh textMesh = note.AddComponent<TextMesh>();
            textMesh.text = annotation.text;
            textMesh.color = annotation.color;
            textMesh.fontSize = 20;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }
        
        note.name = $"Annotation_{annotation.annotationID}";
        annotationObjects.Add(note);
        
        // Make note face camera
        note.transform.LookAt(Camera.main.transform);
        note.transform.Rotate(0, 180, 0);
    }

    /// <summary>
    /// Save annotations to file
    /// </summary>
    void SaveAnnotations()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "Research", "annotations.json");
            string directory = Path.GetDirectoryName(fullPath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string json = JsonConvert.SerializeObject(annotations, Formatting.Indented, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
            });
            
            File.WriteAllText(fullPath, json);
            
            Debug.Log($"[AnnotationTool] Saved {annotations.Count} annotations");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AnnotationTool] Error saving annotations: {e.Message}");
        }
    }

    /// <summary>
    /// Load annotations from file
    /// </summary>
    void LoadAnnotations()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "Research", "annotations.json");
            
            if (!File.Exists(fullPath))
            {
                return;
            }
            
            string json = File.ReadAllText(fullPath);
            annotations = JsonConvert.DeserializeObject<List<Annotation>>(json);
            
            // Recreate note objects
            foreach (var annotation in annotations)
            {
                CreateNote(annotation);
            }
            
            Debug.Log($"[AnnotationTool] Loaded {annotations.Count} annotations");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AnnotationTool] Error loading annotations: {e.Message}");
        }
    }

    /// <summary>
    /// Clear all annotations
    /// </summary>
    public void ClearAnnotations()
    {
        foreach (var obj in annotationObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        annotationObjects.Clear();
        annotations.Clear();
        
        if (saveAnnotations)
        {
            SaveAnnotations();
        }
        
        Debug.Log("[AnnotationTool] All annotations cleared");
    }

    /// <summary>
    /// Get all annotations
    /// </summary>
    public List<Annotation> GetAnnotations()
    {
        return new List<Annotation>(annotations);
    }
}
