# NAVΛ Lambda Core - Rust Safety Library

## Overview

The **NAVΛ Lambda Core** is a Rust library that provides the Ironclad 7D Math, SIM2VAL++ Uncertainty, and Robustness Checks for the NAVΛ Dashboard. It exposes a C-friendly FFI (Foreign Function Interface) for Unity integration via P/Invoke.

## Features

- **Ironclad 7D Math**: Calculates P = x + y + z + t + g + i + c
- **Memory Safety**: Rust's borrow checker ensures no memory leaks or crashes
- **Performance**: Optimized for deterministic execution (WCET < 20ms)
- **SIM2VAL++**: Uncertainty calculation with variance reduction
- **Robustness Checks**: System integrity verification

## Building

### Prerequisites

- Rust toolchain (install from https://rustup.rs/)
- Cargo (comes with Rust)

### Build Commands

```bash
# Navigate to the library directory
cd nav_lambda_core

# Build release version (optimized)
cargo build --release

# Build debug version (for development)
cargo build
```

### Output

The build produces:
- **Windows**: `target/release/nav_lambda_core.dll`
- **macOS**: `target/release/libnav_lambda_core.dylib`
- **Linux**: `target/release/libnav_lambda_core.so`

## Deployment

### Unity Integration

1. **Copy Library to Unity**:
   - Copy the compiled library to `nava-ai/Assets/Plugins/`
   - Ensure platform-specific folders are used:
     - Windows: `Plugins/x86_64/nav_lambda_core.dll`
     - macOS: `Plugins/libnav_lambda_core.dylib`
     - Linux: `Plugins/x86_64/libnav_lambda_core.so`

2. **Unity Plugin Settings**:
   - Select the library file in Unity
   - Set "Load on Startup" to true
   - Set "CPU" to x86_64 (or appropriate architecture)

3. **Verify Integration**:
   - Run Unity scene
   - Check `CertificationCompiler` status text
   - Should display "RUST CORE LOCKED & READY" if successful

## FFI Functions

### Core Functions

- `rust_core_init()`: Initialize the Rust core library
- `check_system_robustness()`: Check system integrity
- `validate_unity_alloc(ptr, size)`: Validate Unity memory allocation
- `calculate_p_score(state, params, obstacles, count, result)`: Calculate P-score
- `calculate_sim2val_uncertainty(variates, count, result_sigma)`: Calculate uncertainty
- `free_c_string(ptr)`: Free C string allocated by Rust

### Data Structures

- `State7D`: 7D state vector (position, velocity, heading, timestamp, certainty, fatigue)
- `RigorParams`: Safety parameters (alpha, min_margin)
- `VerificationResult`: Verification result (p_score, is_safe, margin, sigma, breach_reason, evidence_hash)

## Testing

```bash
# Run unit tests
cargo test

# Run with output
cargo test -- --nocapture
```

## Performance

- **WCET**: < 20ms for P-score calculation
- **Memory**: Zero-allocation in hot path (after initialization)
- **Thread Safety**: All functions are thread-safe

## Safety Guarantees

- **Memory Safety**: Rust's ownership system prevents use-after-free, double-free, and data races
- **Type Safety**: Compile-time guarantees prevent type errors
- **Deterministic**: No undefined behavior, predictable execution

## License

See main project license.
