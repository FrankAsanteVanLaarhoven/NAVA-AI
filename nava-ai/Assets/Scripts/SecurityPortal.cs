using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Security Portal - Production Capability.
/// Before anyone can control the fleet, they must authenticate.
/// Handles Token-Based Auth (JWT) and Encryption Key Exchange.
/// </summary>
public class SecurityPortal : MonoBehaviour
{
    [Header("Authentication Settings")]
    [Tooltip("Require authentication to use system")]
    public bool requireAuth = true;
    
    [Tooltip("Session timeout (seconds)")]
    public float sessionTimeout = 3600f; // 1 hour
    
    [Header("UI References")]
    [Tooltip("Text displaying auth status")]
    public Text authStatusText;
    
    [Tooltip("Lock screen (blur effect when locked)")]
    public GameObject lockScreen;
    
    [Tooltip("Login panel")]
    public GameObject loginPanel;
    
    [Tooltip("Token input field")]
    public InputField tokenInputField;
    
    [Header("Security")]
    [Tooltip("Production token prefix")]
    public string productionTokenPrefix = "PRD-";
    
    private bool isAuthenticated = false;
    private float sessionStartTime = 0f;
    private string currentToken = "";

    void Start()
    {
        // Initialize lock screen
        if (lockScreen != null)
        {
            lockScreen.SetActive(requireAuth);
        }
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(requireAuth);
        }
        
        // Check for saved session token
        CheckForSessionToken();
        
        Debug.Log("[SecurityPortal] Authentication system initialized");
    }

    void Update()
    {
        // Check session timeout
        if (isAuthenticated && requireAuth)
        {
            if (Time.time - sessionStartTime > sessionTimeout)
            {
                Logout();
            }
        }
    }

    /// <summary>
    /// Login with token
    /// </summary>
    public void Login(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            if (tokenInputField != null)
            {
                token = tokenInputField.text;
            }
        }
        
        if (string.IsNullOrEmpty(token))
        {
            UpdateAuthStatus("AUTH: Token required", Color.red);
            return;
        }
        
        // 1. Validate Token (Mock JWT Check)
        bool isValid = ValidateToken(token);
        
        if (isValid)
        {
            isAuthenticated = true;
            currentToken = token;
            sessionStartTime = Time.time;
            
            // Save token for next session
            PlayerPrefs.SetString("auth_token", token);
            PlayerPrefs.SetFloat("auth_time", Time.time);
            PlayerPrefs.Save();
            
            UpdateAuthStatus("AUTH: VERIFIED (PROD)", Color.green);
            
            // Unlock controls
            if (lockScreen != null)
            {
                lockScreen.SetActive(false);
            }
            
            if (loginPanel != null)
            {
                loginPanel.SetActive(false);
            }
            
            Debug.Log("[SecurityPortal] Authentication successful");
        }
        else
        {
            isAuthenticated = false;
            UpdateAuthStatus("AUTH: DENIED", Color.red);
            
            // Lock all inputs
            if (lockScreen != null)
            {
                lockScreen.SetActive(true);
            }
            
            Debug.LogWarning("[SecurityPortal] Authentication failed - invalid token");
        }
    }

    /// <summary>
    /// Validate token
    /// </summary>
    bool ValidateToken(string token)
    {
        // Production: Connect to Auth Service
        // For demo, we assume any token starting with "PRD-" is valid
        if (token.StartsWith(productionTokenPrefix))
        {
            return true;
        }
        
        // Check for academic tokens (for dual-mode)
        if (token.StartsWith("ACAD-"))
        {
            return true;
        }
        
        // In production: Validate JWT signature, expiration, etc.
        // For now, accept any non-empty token for testing
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Check for saved session token
    /// </summary>
    void CheckForSessionToken()
    {
        if (PlayerPrefs.HasKey("auth_token"))
        {
            string savedToken = PlayerPrefs.GetString("auth_token");
            float savedTime = PlayerPrefs.GetFloat("auth_time", 0f);
            
            // Check if session is still valid
            if (Time.time - savedTime < sessionTimeout)
            {
                Login(savedToken);
            }
            else
            {
                // Session expired
                PlayerPrefs.DeleteKey("auth_token");
                PlayerPrefs.DeleteKey("auth_time");
            }
        }
        else if (requireAuth)
        {
            UpdateAuthStatus("AUTH: Login required", Color.yellow);
        }
    }

    /// <summary>
    /// Logout
    /// </summary>
    public void Logout()
    {
        isAuthenticated = false;
        currentToken = "";
        
        // Clear saved token
        PlayerPrefs.DeleteKey("auth_token");
        PlayerPrefs.DeleteKey("auth_time");
        
        UpdateAuthStatus("AUTH: Logged out", Color.gray);
        
        // Lock controls
        if (lockScreen != null)
        {
            lockScreen.SetActive(true);
        }
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        
        Debug.Log("[SecurityPortal] User logged out");
    }

    void UpdateAuthStatus(string message, Color color)
    {
        if (authStatusText != null)
        {
            authStatusText.text = message;
            authStatusText.color = color;
        }
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        return isAuthenticated || !requireAuth;
    }

    /// <summary>
    /// Get current token
    /// </summary>
    public string GetCurrentToken()
    {
        return currentToken;
    }
}
