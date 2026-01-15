using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Physics Interaction System - Handles doors, buttons, and physical interactions.
/// Uses raycasts to detect and manipulate kinematic bodies.
/// </summary>
public class PhysicsInteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Layer mask for interactable objects")]
    public LayerMask interactableLayers;

    [Tooltip("Maximum interaction distance")]
    public float interactionDistance = 3.0f;

    [Tooltip("Interaction cooldown in seconds")]
    public float interactionCooldown = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Debug ray line renderer")]
    public LineRenderer debugRay;

    [Tooltip("Highlight material for interactables")]
    public Material highlightMaterial;

    [Header("Audio")]
    [Tooltip("Audio source for interaction sounds")]
    public AudioSource interactionAudio;

    private float lastInteractionTime = 0f;
    private GameObject currentHighlight = null;
    private Dictionary<GameObject, InteractionState> interactionStates = new Dictionary<GameObject, InteractionState>();

    [System.Serializable]
    public class InteractionState
    {
        public bool isOpen;
        public bool isPressed;
        public float interactionTime;
    }

    void Start()
    {
        // Create debug ray if not assigned
        if (debugRay == null)
        {
            GameObject rayObj = new GameObject("DebugRay");
            rayObj.transform.SetParent(transform);
            debugRay = rayObj.AddComponent<LineRenderer>();
            debugRay.startWidth = 0.05f;
            debugRay.endWidth = 0.05f;
            debugRay.material = new Material(Shader.Find("Sprites/Default"));
            debugRay.material.color = Color.yellow;
            debugRay.enabled = false;
        }

        // Initialize audio
        if (interactionAudio == null)
        {
            interactionAudio = gameObject.AddComponent<AudioSource>();
            interactionAudio.playOnAwake = false;
        }
    }

    void Update()
    {
        // Check for interaction input
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

        // Highlight interactables on hover
        HighlightInteractable();
    }

    void TryInteract()
    {
        if (Time.time - lastInteractionTime < interactionCooldown)
        {
            return;
        }

        // Cast ray from camera
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check for interactive objects
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayers))
        {
            GameObject hitObject = hit.collider.gameObject;
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            // Handle interaction based on tag
            if (hitObject.CompareTag("Door"))
            {
                Debug.Log("[Interact] Door detected. Triggering Open.");
                AnimateDoor(hitObject);
                lastInteractionTime = Time.time;
            }
            else if (hitObject.CompareTag("Button"))
            {
                Debug.Log("[Interact] Button pressed. Triggering Click.");
                PressButton(hitObject);
                lastInteractionTime = Time.time;
            }
            else if (hitObject.CompareTag("Lever"))
            {
                Debug.Log("[Interact] Lever detected. Toggling.");
                ToggleLever(hitObject);
                lastInteractionTime = Time.time;
            }
            else if (rb != null && !rb.isKinematic)
            {
                // Push/pull physics objects
                Debug.Log("[Interact] Physics object detected. Applying force.");
                ApplyForce(rb, hit.point, ray.direction);
                lastInteractionTime = Time.time;
            }

            // Draw debug ray
            if (debugRay != null)
            {
                debugRay.enabled = true;
                debugRay.SetPosition(0, ray.origin);
                debugRay.SetPosition(1, hit.point);
                StartCoroutine(HideDebugRay());
            }

            // Play interaction sound
            PlayInteractionSound(hitObject.tag);
        }
    }

    void HighlightInteractable()
    {
        // Cast ray to find interactables
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayers))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Highlight if interactable
            if (hitObject.CompareTag("Door") || hitObject.CompareTag("Button") || hitObject.CompareTag("Lever"))
            {
                if (currentHighlight != hitObject)
                {
                    // Remove previous highlight
                    if (currentHighlight != null)
                    {
                        RemoveHighlight(currentHighlight);
                    }

                    // Add new highlight
                    AddHighlight(hitObject);
                    currentHighlight = hitObject;
                }
            }
            else
            {
                if (currentHighlight != null)
                {
                    RemoveHighlight(currentHighlight);
                    currentHighlight = null;
                }
            }
        }
        else
        {
            if (currentHighlight != null)
            {
                RemoveHighlight(currentHighlight);
                currentHighlight = null;
            }
        }
    }

    void AddHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null)
        {
            // Store original material
            if (!interactionStates.ContainsKey(obj))
            {
                interactionStates[obj] = new InteractionState();
            }

            // Apply highlight (outline effect)
            Material[] materials = renderer.materials;
            Material[] newMaterials = new Material[materials.Length + 1];
            System.Array.Copy(materials, newMaterials, materials.Length);
            newMaterials[materials.Length] = highlightMaterial;
            renderer.materials = newMaterials;
        }
    }

    void RemoveHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Remove highlight material
            Material[] materials = renderer.materials;
            if (materials.Length > 1)
            {
                Material[] newMaterials = new Material[materials.Length - 1];
                System.Array.Copy(materials, 0, newMaterials, 0, materials.Length - 1);
                renderer.materials = newMaterials;
            }
        }
    }

    void AnimateDoor(GameObject door)
    {
        if (!interactionStates.ContainsKey(door))
        {
            interactionStates[door] = new InteractionState();
        }

        InteractionState state = interactionStates[door];
        state.isOpen = !state.isOpen;
        state.interactionTime = Time.time;

        // Calculate target rotation
        Vector3 currentRot = door.transform.rotation.eulerAngles;
        Vector3 targetRot = state.isOpen ? currentRot + new Vector3(0, 90, 0) : currentRot - new Vector3(0, 90, 0);

        // Animate door
        StartCoroutine(DoorCoroutine(door, Quaternion.Euler(targetRot)));
    }

    IEnumerator DoorCoroutine(GameObject door, Quaternion targetRot)
    {
        float duration = 1.0f;
        Quaternion startRot = door.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            door.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        door.transform.rotation = targetRot;
    }

    void PressButton(GameObject button)
    {
        if (!interactionStates.ContainsKey(button))
        {
            interactionStates[button] = new InteractionState();
        }

        InteractionState state = interactionStates[button];
        state.isPressed = true;
        state.interactionTime = Time.time;

        // Visual feedback (press down effect)
        Vector3 originalScale = button.transform.localScale;
        StartCoroutine(ButtonPressCoroutine(button, originalScale * 0.9f, originalScale));

        // Trigger button event
        ButtonTrigger trigger = button.GetComponent<ButtonTrigger>();
        if (trigger != null)
        {
            trigger.OnButtonPressed();
        }

        Debug.Log($"[Interact] Button pressed at {button.transform.position}");
    }

    IEnumerator ButtonPressCoroutine(GameObject button, Vector3 pressedScale, Vector3 originalScale)
    {
        // Press down
        float pressDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            button.transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            yield return null;
        }

        // Release
        elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            button.transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
            yield return null;
        }

        button.transform.localScale = originalScale;

        // Reset pressed state after delay
        yield return new WaitForSeconds(0.2f);
        if (interactionStates.ContainsKey(button))
        {
            interactionStates[button].isPressed = false;
        }
    }

    void ToggleLever(GameObject lever)
    {
        if (!interactionStates.ContainsKey(lever))
        {
            interactionStates[lever] = new InteractionState();
        }

        InteractionState state = interactionStates[lever];
        state.isOpen = !state.isOpen;

        // Rotate lever
        Vector3 currentRot = lever.transform.localRotation.eulerAngles;
        Vector3 targetRot = state.isOpen ? currentRot + new Vector3(45, 0, 0) : currentRot - new Vector3(45, 0, 0);

        StartCoroutine(LeverCoroutine(lever, Quaternion.Euler(targetRot)));
    }

    IEnumerator LeverCoroutine(GameObject lever, Quaternion targetRot)
    {
        float duration = 0.5f;
        Quaternion startRot = lever.transform.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            lever.transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        lever.transform.localRotation = targetRot;
    }

    void ApplyForce(Rigidbody rb, Vector3 hitPoint, Vector3 direction)
    {
        // Apply force at hit point
        float force = 10f;
        rb.AddForceAtPosition(direction * force, hitPoint, ForceMode.Impulse);
    }

    void PlayInteractionSound(string tag)
    {
        if (interactionAudio == null) return;

        // Play different sounds based on interaction type
        // In production, load actual audio clips
        interactionAudio.pitch = Random.Range(0.9f, 1.1f);
        interactionAudio.Play();
    }

    IEnumerator HideDebugRay()
    {
        yield return new WaitForSeconds(0.5f);
        if (debugRay != null)
        {
            debugRay.enabled = false;
        }
    }

    /// <summary>
    /// Check if object is interactable
    /// </summary>
    public bool IsInteractable(GameObject obj)
    {
        return obj.CompareTag("Door") || obj.CompareTag("Button") || obj.CompareTag("Lever");
    }

    /// <summary>
    /// Get interaction state
    /// </summary>
    public InteractionState GetInteractionState(GameObject obj)
    {
        if (interactionStates.ContainsKey(obj))
        {
            return interactionStates[obj];
        }
        return new InteractionState();
    }
}

/// <summary>
/// Button Trigger - Component for buttons to trigger events
/// </summary>
public class ButtonTrigger : MonoBehaviour
{
    [Header("Button Events")]
    [Tooltip("Objects to activate on button press")]
    public GameObject[] objectsToActivate;

    [Tooltip("Objects to deactivate on button press")]
    public GameObject[] objectsToDeactivate;

    public void OnButtonPressed()
    {
        // Activate objects
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Deactivate objects
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        Debug.Log($"[ButtonTrigger] Button {gameObject.name} pressed");
    }
}
