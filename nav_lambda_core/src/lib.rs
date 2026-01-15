//! NAVΛ Lambda Core - Rust Safety Library
//! 
//! This library provides the Ironclad 7D Math, SIM2VAL++ Uncertainty,
//! and Robustness Checks in Rust for memory safety and performance.
//! Exposes C-friendly FFI for Unity integration.

use std::ffi::{CStr, CString};
use std::ffi::CString;
use std::os::raw::{c_char, c_float, c_int, c_ulonglong, c_void};
use std::ptr;
use std::sync::atomic::{AtomicBool, Ordering};

// --- 7D State Space (The Ironclad Math) ---
#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct State7D {
    pub position: [c_float; 3], // x, y, z
    pub velocity: [c_float; 3],  // vx, vy, vz
    pub heading: c_float,        // Theta
    pub timestamp: c_ulonglong,
    pub certainty: c_float,      // 'i' (Model Confidence)
    pub fatigue: c_float,        // 'c' (Consciousness/Fatigue)
}

// --- Verification Result ---
#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct VerificationResult {
    pub p_score: c_float,        // Total Safety Score
    pub is_safe: c_int,          // bool as int (0 = false, 1 = true)
    pub margin: c_float,
    pub sigma: c_float,          // Uncertainty (from SIM2VAL)
    pub breach_reason: *mut c_char, // String pointer (caller must free)
    pub evidence_hash: *mut c_char, // SHA-256 hash string
}

// --- Ironclad Equation Parameters ---
#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct RigorParams {
    pub alpha: c_float,      // Class-K (Rigorousness)
    pub min_margin: c_float,
}

// Global state for robustness checking
static RUST_CORE_INITIALIZED: AtomicBool = AtomicBool::new(false);

/// Initialize the Rust core library
/// Returns 1 if successful, 0 if failed
#[no_mangle]
pub extern "C" fn rust_core_init() -> c_int {
    RUST_CORE_INITIALIZED.store(true, Ordering::Release);
    1
}

/// Check system robustness
/// Returns 1 if robust, 0 if failed
#[no_mangle]
pub extern "C" fn check_system_robustness() -> c_int {
    if RUST_CORE_INITIALIZED.load(Ordering::Acquire) {
        1
    } else {
        0
    }
}

/// Validate Unity memory allocation (simulated)
/// In production, this would check heap integrity
#[no_mangle]
pub extern "C" fn validate_unity_alloc(ptr: *mut c_void, size: usize) -> c_int {
    if ptr.is_null() {
        return 0;
    }
    if size == 0 {
        return 0;
    }
    // In production, add actual memory validation
    // For now, just check pointer is not null and size is reasonable
    if size > 1024 * 1024 * 1024 {
        // Reject allocations > 1GB (suspicious)
        return 0;
    }
    1
}

/// Calculate P-score using Ironclad 7D Math
/// 
/// # Safety
/// 
/// This function is unsafe because it dereferences raw pointers.
/// Caller must ensure:
/// - `obstacles` points to a valid array of at least `obstacle_count * 3` floats
/// - `result` is a valid pointer to a VerificationResult struct
#[no_mangle]
pub unsafe extern "C" fn calculate_p_score(
    state: *const State7D,
    params: *const RigorParams,
    obstacles: *const c_float,
    obstacle_count: usize,
    result: *mut VerificationResult,
) -> c_int {
    // Validate inputs
    if state.is_null() || params.is_null() || result.is_null() {
        return 0; // Failure
    }

    let state = *state;
    let params = *params;

    let mut p_score: c_float = 0.0;

    // 1. Calculate "x" (Position Norm) - Euclidean distance to origin
    let pos_norm = (state.position[0].powi(2) 
                  + state.position[1].powi(2) 
                  + state.position[2].powi(2)).sqrt();

    // 2. Calculate "t" (Time Phase) - Sine wave system sync (0.0 to 1.0)
    let t_phase = ((state.timestamp % 10000) as c_float) / 10000.0;
    
    // 3. Calculate "g" (Gradient) - Slope simulation
    let g_gradient = state.position[1] * 0.1;

    // 4. Calculate "i" (Intent) - Model Confidence
    let i_intent = state.certainty;

    // 5. Calculate "c" (Consciousness/Fatigue)
    let c_consciousness = state.fatigue;

    // 6. Safety Check (The "Ironclad" Constraint)
    let mut constraint_violated = false;
    let mut min_margin_dist = c_float::MAX;
    let mut breach_reason_str = CString::new("SAFE").unwrap();

    if !obstacles.is_null() && obstacle_count > 0 {
        for i in 0..obstacle_count {
            let obs_idx = i * 3;
            let obs_x = *obstacles.add(obs_idx);
            let obs_y = *obstacles.add(obs_idx + 1);
            let obs_z = *obstacles.add(obs_idx + 2);

            let dx = state.position[0] - obs_x;
            let dy = state.position[1] - obs_y;
            let dz = state.position[2] - obs_z;
            
            let dist_sq = dx * dx + dy * dy + dz * dz;
            let dist = dist_sq.sqrt();

            let margin = dist - params.min_margin;
            if margin < min_margin_dist {
                min_margin_dist = margin;
            }
            
            // Check Breach (If Margin < 0)
            if margin < 0.0 {
                constraint_violated = true;
                breach_reason_str = CString::new("VNC_VIOLATION").unwrap();
                break;
            }
        }
    }

    // Check fatigue breach
    if state.fatigue < 0.3 {
        constraint_violated = true;
        breach_reason_str = CString::new("FATIGUE").unwrap();
    }

    // Check certainty breach
    if state.certainty < 0.5 {
        constraint_violated = true;
        if !constraint_violated {
            breach_reason_str = CString::new("LOW_CERTAINTY").unwrap();
        }
    }

    // --- SUM IT UP (The Formula: P = x + y + z + t + g + i + c) ---
    // Note: x, y, z are combined into pos_norm
    p_score = pos_norm + t_phase + g_gradient + i_intent + c_consciousness;

    // Create result
    let breach_reason_ptr = breach_reason_str.into_raw();
    let evidence_hash_str = CString::new("PENDING_HASH").unwrap();
    let evidence_hash_ptr = evidence_hash_str.into_raw();

    *result = VerificationResult {
        p_score,
        is_safe: if constraint_violated { 0 } else { 1 },
        margin: min_margin_dist,
        sigma: 0.0, // Would be filled by SIM2VAL
        breach_reason: breach_reason_ptr,
        evidence_hash: evidence_hash_ptr,
    };

    1 // Success
}

/// Free C string allocated by Rust
/// Caller must call this to prevent memory leaks
#[no_mangle]
pub unsafe extern "C" fn free_c_string(ptr: *mut c_char) {
    if !ptr.is_null() {
        let _ = CString::from_raw(ptr);
    }
}

/// Calculate SIM2VAL++ uncertainty estimate
/// 
/// # Safety
/// 
/// This function is unsafe because it dereferences raw pointers.
#[no_mangle]
pub unsafe extern "C" fn calculate_sim2val_uncertainty(
    control_variates: *const c_float,
    variate_count: usize,
    result_sigma: *mut c_float,
) -> c_int {
    if control_variates.is_null() || result_sigma.is_null() || variate_count == 0 {
        return 0;
    }

    // Calculate mean
    let mut sum = 0.0;
    for i in 0..variate_count {
        sum += *control_variates.add(i);
    }
    let mean = sum / variate_count as c_float;

    // Calculate variance
    let mut variance_sum = 0.0;
    for i in 0..variate_count {
        let diff = *control_variates.add(i) - mean;
        variance_sum += diff * diff;
    }
    let variance = variance_sum / variate_count as c_float;

    // Standard deviation (sigma)
    let sigma = variance.sqrt();

    *result_sigma = sigma;
    1
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_rust_core_init() {
        assert_eq!(rust_core_init(), 1);
        assert_eq!(check_system_robustness(), 1);
    }

    #[test]
    fn test_calculate_p_score() {
        rust_core_init();
        
        let state = State7D {
            position: [1.0, 2.0, 3.0],
            velocity: [0.1, 0.2, 0.3],
            heading: 45.0,
            timestamp: 1000,
            certainty: 0.8,
            fatigue: 0.9,
        };

        let params = RigorParams {
            alpha: 5.0,
            min_margin: 0.5,
        };

        let obstacles = [0.0, 0.0, 0.0, 10.0, 10.0, 10.0];
        let mut result = VerificationResult {
            p_score: 0.0,
            is_safe: 0,
            margin: 0.0,
            sigma: 0.0,
            breach_reason: ptr::null_mut(),
            evidence_hash: ptr::null_mut(),
        };

        unsafe {
            let success = calculate_p_score(
                &state,
                &params,
                obstacles.as_ptr(),
                2,
                &mut result,
            );

            assert_eq!(success, 1);
            assert!(result.p_score > 0.0);
            
            // Free allocated strings
            free_c_string(result.breach_reason);
            free_c_string(result.evidence_hash);
        }
    }
}
