using UnityEngine;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System;
using Newtonsoft.Json;

/// <summary>
/// Secure Data Logger - Cryptography Core.
/// Encrypts every log entry using AES-256 before saving to disk or sending to ROS.
/// Generates SHA-256 Fingerprint for Tamper Evidence.
/// </summary>
public class SecureDataLogger : MonoBehaviour
{
    [Header("AES-256 Encryption")]
    [Tooltip("Encryption key (32 bytes for AES-256)")]
    public string encryptionKeyString = "NavLambdaKey_2026_32ByteKey!!";
    
    [Tooltip("Key file path (if using file-based key)")]
    public string keyFilePath = "Assets/Secure/encryption.key";
    
    [Tooltip("Generate new key on startup")]
    public bool generateNewKey = false;
    
    [Header("Visual Feedback")]
    [Tooltip("Light for security status")]
    public Light securityLight;
    
    [Header("Logging")]
    [Tooltip("Enable secure logging")]
    public bool enableLogging = true;
    
    [Tooltip("Secure log file path")]
    public string secureLogPath = "Assets/Secure/evidence_chain.json";
    
    [Tooltip("Enable tamper detection")]
    public bool enableTamperDetection = true;
    
    private byte[] encryptionKey;
    private byte[] initializationVector;
    private int logEntryCount = 0;

    // The Evidence Pack (Encrypted Container)
    [System.Serializable]
    public class EvidencePack
    {
        public string timestamp;
        public string signature; // SHA-256 Hash
        public string encryptedPayload; // AES-256 Ciphertext
        public string keyId; // Key Reference
        public string dataType;
        public string rawContentHash; // Hash of original content (for verification)
    }

    void Start()
    {
        // Initialize encryption key
        InitializeEncryption();
        
        // Create security light if not assigned
        if (securityLight == null)
        {
            GameObject lightObj = new GameObject("SecurityLight");
            lightObj.transform.SetParent(transform);
            securityLight = lightObj.AddComponent<Light>();
            securityLight.type = LightType.Point;
            securityLight.range = 5f;
            securityLight.intensity = 2f;
        }
        
        securityLight.color = Color.green;
        
        // Create secure directory
        string directory = Path.GetDirectoryName(secureLogPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        Debug.Log("[SECURITY] AES-256 Engine Initialized. Evidence Pack Signing Active.");
    }

    void InitializeEncryption()
    {
        if (generateNewKey || !File.Exists(keyFilePath))
        {
            // Generate new 32-byte key for AES-256
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();
                
                encryptionKey = aes.Key;
                initializationVector = aes.IV;
                
                // Save key to file (in production, use secure key management)
                string keyDir = Path.GetDirectoryName(keyFilePath);
                if (!string.IsNullOrEmpty(keyDir) && !Directory.Exists(keyDir))
                {
                    Directory.CreateDirectory(keyDir);
                }
                
                File.WriteAllBytes(keyFilePath, encryptionKey);
                File.WriteAllBytes(keyFilePath + ".iv", initializationVector);
                
                Debug.LogWarning("[SECURITY] New encryption key generated. Store securely!");
            }
        }
        else
        {
            // Load existing key
            encryptionKey = File.ReadAllBytes(keyFilePath);
            if (File.Exists(keyFilePath + ".iv"))
            {
                initializationVector = File.ReadAllBytes(keyFilePath + ".iv");
            }
            else
            {
                // Generate IV if missing
                using (Aes aes = Aes.Create())
                {
                    aes.GenerateIV();
                    initializationVector = aes.IV;
                    File.WriteAllBytes(keyFilePath + ".iv", initializationVector);
                }
            }
        }
        
        // Ensure key is 32 bytes (AES-256)
        if (encryptionKey == null || encryptionKey.Length != 32)
        {
            // Generate from string if needed
            using (SHA256 sha = SHA256.Create())
            {
                encryptionKey = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKeyString));
            }
        }
    }

    /// <summary>
    /// Logs data securely. Encrypts payload and generates Hash.
    /// </summary>
    public void LogSecure(string type, string data)
    {
        if (!enableLogging) return;
        
        try
        {
            // 1. Create Evidence Pack
            EvidencePack pack = new EvidencePack
            {
                timestamp = DateTime.Now.ToString("o"),
                keyId = "KEY_001",
                dataType = type,
                rawContent = data
            };

            // 2. Compute hash of raw content (for tamper detection)
            pack.rawContentHash = ComputeSHA256(data);

            // 3. Encrypt (AES-256)
            byte[] encrypted = EncryptAES256(data, encryptionKey, initializationVector);
            pack.encryptedPayload = Convert.ToBase64String(encrypted);
            
            // 4. Sign (SHA-256 Fingerprint for Integrity)
            // Sign the entire pack (timestamp + encrypted payload + hash)
            string packData = $"{pack.timestamp}{pack.encryptedPayload}{pack.rawContentHash}";
            pack.signature = ComputeSHA256(packData);

            // 5. Write to "Vault" (File System)
            string json = JsonConvert.SerializeObject(pack, Formatting.Indented);
            File.AppendAllText(secureLogPath, json + "\n");
            
            logEntryCount++;
            
            // 6. Visual Tamper Alert
            UpdateSecurityLight(type);
            
            Debug.Log($"[SECURITY] Secure log entry #{logEntryCount} written: {type}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SECURITY] Failed to log securely: {e.Message}");
            if (securityLight != null)
            {
                securityLight.color = Color.red;
                securityLight.intensity = 10f;
            }
        }
    }

    byte[] EncryptAES256(string plainText, byte[] key, byte[] iv)
    {
        if (string.IsNullOrEmpty(plainText)) return new byte[0];
        
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC; // CBC is more secure than ECB
            aes.Padding = PaddingMode.PKCS7;
            
            ICryptoTransform encryptor = aes.CreateEncryptor();
            
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            return encrypted;
        }
    }

    string DecryptAES256(string base64Ciphertext, byte[] key, byte[] iv)
    {
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(base64Ciphertext);
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                
                return Encoding.UTF8.GetString(decrypted);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SECURITY] Decryption failed: {e.Message}");
            return null;
        }
    }

    string ComputeSHA256(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);
            
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    void UpdateSecurityLight(string type)
    {
        if (securityLight == null) return;
        
        if (type.Contains("VIOLATION") || type.Contains("BREACH") || type.Contains("TAMPER"))
        {
            securityLight.color = Color.red;
            securityLight.intensity = 10.0f; // Flash Red
        }
        else if (type.Contains("WARNING") || type.Contains("ALERT"))
        {
            securityLight.color = Color.yellow;
            securityLight.intensity = 5.0f;
        }
        else
        {
            securityLight.color = Color.green;
            securityLight.intensity = 2.0f;
        }
    }

    /// <summary>
    /// Verify integrity of a log entry
    /// </summary>
    public bool VerifyIntegrity(EvidencePack pack)
    {
        if (!enableTamperDetection) return true;
        
        try
        {
            // Recompute signature
            string packData = $"{pack.timestamp}{pack.encryptedPayload}{pack.rawContentHash}";
            string computedSignature = ComputeSHA256(packData);
            
            // Compare signatures
            bool isValid = computedSignature == pack.signature;
            
            if (!isValid)
            {
                Debug.LogError("[SECURITY] Tamper detected! Signature mismatch.");
                if (securityLight != null)
                {
                    securityLight.color = Color.red;
                    securityLight.intensity = 15f;
                }
            }
            
            return isValid;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SECURITY] Integrity verification failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Decrypt and verify a log entry
    /// </summary>
    public string DecryptAndVerify(EvidencePack pack)
    {
        // Verify integrity first
        if (!VerifyIntegrity(pack))
        {
            return null;
        }
        
        // Decrypt
        string decrypted = DecryptAES256(pack.encryptedPayload, encryptionKey, initializationVector);
        
        if (decrypted != null)
        {
            // Verify content hash
            string computedHash = ComputeSHA256(decrypted);
            if (computedHash != pack.rawContentHash)
            {
                Debug.LogError("[SECURITY] Content hash mismatch! Data may be corrupted.");
                return null;
            }
        }
        
        return decrypted;
    }

    /// <summary>
    /// Get log entry count
    /// </summary>
    public int GetLogEntryCount()
    {
        return logEntryCount;
    }
}
