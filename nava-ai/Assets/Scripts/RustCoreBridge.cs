using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;

/// <summary>
/// Rust Core Bridge - P/Invoke interface to Rust safety library.
/// Provides C# structs matching Rust FFI structs and wrapper methods.
/// </summary>
public static class RustCoreBridge
{
    // Library name (platform-specific)
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string RUST_LIB_NAME = "nav_lambda_core";
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string RUST_LIB_NAME = "libnav_lambda_core";
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        private const string RUST_LIB_NAME = "libnav_lambda_core";
    #else
        private const string RUST_LIB_NAME = "nav_lambda_core";
    #endif

    // --- Rust Struct Definitions (Must match Rust exactly) ---
    
    [StructLayout(LayoutKind.Sequential)]
    public struct State7D
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] position; // x, y, z
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] velocity; // vx, vy, vz
        
        public float heading;    // Theta
        public ulong timestamp;
        public float certainty;  // 'i' (Model Confidence)
        public float fatigue;    // 'c' (Consciousness/Fatigue)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VerificationResult
    {
        public float p_score;    // Total Safety Score
        public int is_safe;      // bool as int (0 = false, 1 = true)
        public float margin;
        public float sigma;      // Uncertainty (from SIM2VAL)
        public IntPtr breach_reason; // String pointer
        public IntPtr evidence_hash; // SHA-256 hash string
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RigorParams
    {
        public float alpha;      // Class-K (Rigorousness)
        public float min_margin;
    }

    // --- FFI Function Declarations ---
    
    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rust_core_init();

    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int check_system_robustness();

    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int validate_unity_alloc(IntPtr ptr, ulong size);

    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int calculate_p_score(
        ref State7D state,
        ref RigorParams parameters,
        [MarshalAs(UnmanagedType.LPArray)] float[] obstacles,
        int obstacle_count,
        out VerificationResult result
    );

    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int calculate_sim2val_uncertainty(
        [MarshalAs(UnmanagedType.LPArray)] float[] control_variates,
        int variate_count,
        out float result_sigma
    );

    [DllImport(RUST_LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void free_c_string(IntPtr ptr);

    // --- Helper Methods ---
    
    /// <summary>
    /// Convert VerificationResult to C#-friendly format
    /// </summary>
    public static VerificationResultCSharp ConvertResult(VerificationResult result)
    {
        string breachReason = "";
        string evidenceHash = "";

        if (result.breach_reason != IntPtr.Zero)
        {
            breachReason = Marshal.PtrToStringAnsi(result.breach_reason);
            free_c_string(result.breach_reason);
        }

        if (result.evidence_hash != IntPtr.Zero)
        {
            evidenceHash = Marshal.PtrToStringAnsi(result.evidence_hash);
            free_c_string(result.evidence_hash);
        }

        return new VerificationResultCSharp
        {
            p_score = result.p_score,
            is_safe = result.is_safe != 0,
            margin = result.margin,
            sigma = result.sigma,
            breach_reason = breachReason,
            evidence_hash = evidenceHash
        };
    }

    /// <summary>
    /// C#-friendly verification result
    /// </summary>
    public struct VerificationResultCSharp
    {
        public float p_score;
        public bool is_safe;
        public float margin;
        public float sigma;
        public string breach_reason;
        public string evidence_hash;
    }

    /// <summary>
    /// Check if Rust core is available
    /// </summary>
    public static bool IsRustCoreAvailable()
    {
        try
        {
            int robustness = check_system_robustness();
            return robustness != 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Initialize Rust core
    /// </summary>
    public static bool InitializeRustCore()
    {
        try
        {
            int result = rust_core_init();
            return result != 0;
        }
        catch (DllNotFoundException)
        {
            Debug.LogError("[RustCore] Library not found. Place nav_lambda_core.dll/.so in Assets/Plugins/");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RustCore] Initialization failed: {e.Message}");
            return false;
        }
    }
}
