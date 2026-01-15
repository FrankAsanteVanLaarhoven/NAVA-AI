# NAVΛ Dashboard - System Architecture & Summary

## System Overview

The **NAVΛ Dashboard** is a **Unity 6.3 LTS** based platform-grade web application that provides real-time robot visualization, control, and safety monitoring. It integrates **Rust Core** safety logic, **XR capabilities**, **PWA/WebRTC** for mobile access, and **Research-Grade** tools for academic deployment.

---

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         NAVΛ DASHBOARD SYSTEM                                │
│                    (Unity 6.3 LTS + Rust Core + Web)                          │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           HARDWARE LAYER                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │   Jetson     │    │   QR Code    │    │  Biometric   │                  │
│  │  (ROS 2)     │    │   Scanner    │    │  (FaceID)    │                  │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘                  │
│         │                   │                   │                            │
│         └───────────────────┴───────────────────┘                            │
│                           │                                                    │
│                    ┌──────▼───────┐                                          │
│                    │ Peripheral   │                                          │
│                    │   Bridge     │                                          │
│                    └──────┬───────┘                                          │
└───────────────────────────┼───────────────────────────────────────────────────┘
                            │
                            │ ROS 2 DDS / TCP
                            │
┌───────────────────────────▼───────────────────────────────────────────────────┐
│                         RUST CORE LAYER                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │              nav_lambda_core (lib.rs)                      │            │
│  │  • 7D State Space (x,y,z,vx,vy,vz,t,g,i,c)                 │            │
│  │  • P-Score Calculation (Ironclad Math)                     │            │
│  │  • SIM2VAL++ Uncertainty Reduction                         │            │
│  │  • Control Barrier Functions (CBF)                         │            │
│  │  • ISO 26262 Compliance                                    │            │
│  └─────────────────────────────────────────────────────────────┘            │
│                            │                                                  │
│  ┌─────────────────────────▼──────────────────────────────┐                │
│  │         nav_lambda_server (main.rs)                      │                │
│  │  • Asset Streaming (Chunked Transfer)                    │                │
│  │  • HTTP Server (Port 8080)                               │                │
│  │  • Large File Handling (GB+)                            │                │
│  └──────────────────────────────────────────────────────────┘                │
└───────────────────────────┬───────────────────────────────────────────────────┘
                            │
                            │ P/Invoke (FFI)
                            │
┌───────────────────────────▼───────────────────────────────────────────────────┐
│                      UNITY DASHBOARD LAYER                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    CORE SAFETY MODULES                               │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • RustCoreBridge.cs          → FFI to Rust Core                    │  │
│  │  • Navl7dRigor.cs             → 7D State Calculation                 │  │
│  │  • NavlConsciousnessRigor.cs  → Cognitive Safety (P = h+g+i+c)     │  │
│  │  • CertificationCompiler.cs  → Notary (Safety Verification)        │  │
│  │  • DynamicZoneManager.cs      → Adaptive Safety Zones              │  │
│  │  • AdaptiveVlaManager.cs      → Risk-Aware VLA Control              │  │
│  │  • SelfHealingSafety.cs       → Auto-Recovery System                │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    SWARM-AGI MODULES                                 │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • SwarmAgiCommander.cs       → Multi-Agent Task Delegation         │  │
│  │  • FleetGeofence.cs          → Dynamic 3D Exclusion Zones          │  │
│  │  • GlobalVoxelMap.cs          → Shared 3D Occupancy Map             │  │
│  │  • HeterogeneousModelManager.cs → VLA/RL/SSM Model Pooling          │  │
│  │  • SwarmCertificationOverlay.cs → Fleet-Wide P-Score View           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    DUAL-MODE PLATFORM                                │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  PRODUCTION MODE:                                                    │  │
│  │  • FleetDiscoveryManager.cs  → ROS 2 DDS Auto-Discovery            │  │
│  │  • MissionProfileSystem.cs   → JSON Mission Profiles               │  │
│  │  • SecurityPortal.cs          → JWT Token Authentication            │  │
│  │  • RealTimeAnalyticsHub.cs   → Fleet Metrics Aggregation           │  │
│  │  • VideoStreamer.cs           → Webcam Telepresence                 │  │
│  │                                                                      │  │
│  │  ACADEMIA MODE:                                                      │  │
│  │  • AcademicSessionRecorder.cs → Student Session Logging             │  │
│  │  • LectureAnnotationTool.cs  → 3D Digital Sticky Notes              │  │
│  │  • ExperimentWorkflowController.cs → Experiment Orchestration      │  │
│  │                                                                      │  │
│  │  • DualModeManager.cs         → Mode Switching                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    RESEARCH-GRADE MODULES                            │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • ResearchEpisodeManager.cs → Episodic RL Logging                 │  │
│  │  • BenchmarkImporter.cs       → Standardized Environment Loading    │  │
│  │  • ManipulationController.cs → Fine-Grained Joint Control          │  │
│  │  • CurriculumRunner.cs        → Automated Curriculum Learning       │  │
│  │  • VLMStateTreeVisualizer.cs  → 3D AI Decision Visualization        │  │
│  │  • FleetDOTSAgent.cs          → DOTS-Based Swarm (10k+ agents)      │  │
│  │  • ResearchAssetBundleLoader.cs → Fast Asset Bundle Loading         │  │
│  │  • NetworkLatencyMonitor.cs   → Built-in Network Profiling          │  │
│  │  • ResearchSceneManager.cs   → Non-Blocking Scene Loading          │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    LIVING WORLD MODULES                               │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • DynamicWorldController.cs  → Day/Night Cycles, Weather          │  │
│  │  • PhysicsInteractionSystem.cs → Raycast-Based Interactions         │  │
│  │  • ProceduralAudioManager.cs  → Reactive Audio Generation         │  │
│  │  • LiveTexturePainter.cs       → Real-Time Texture Painting         │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    HARDWARE INTEGRATION                               │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • QrCodeManager.cs           → QR Code Scanning & Execution        │  │
│  │  • BiometricAuthenticator.cs  → FaceID/Biometrics, Consciousness     │  │
│  │  • PeripheralBridge.cs        → Generic Hardware Interface           │  │
│  │  • SimularController.cs      → Simulated Hardware Control           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    XR-READY MODULES                                   │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • XRDeviceManager.cs         → XR Head Tracking & Focus Zones      │  │
│  │  • XRVisualizer.cs            → 3D Safety Zone Visualization        │  │
│  │  • XRNetworkMonitor.cs        → <20ms Latency Monitoring             │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    PLATFORM-GRADE WEB APP                             │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • MemoryManager.cs           → Unity Addressables & Unloading      │  │
│  │  • StreamingAssetLoader.cs    → Chunked Asset Streaming              │  │
│  │  • PWA Service Worker         → Offline Access & Background Sync    │  │
│  │  • WebRTC Manager              → P2P Live Streaming                 │  │
│  │  • IndexedDB Manager          → High-Frequency Telemetry Logging    │  │
│  │  • Crypto Utils               → AES-256/SHA-256 Encryption          │  │
│  │  • Responsive UI Shell        → Mobile Touch Gestures               │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    BATTLEFIELD-GRADE TACTICAL                       │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • Seer Network (Transformer) → Predictive AI (Danger Score)       │  │
│  │  • Dynamic Enveloper          → Adaptive Safety Boundaries           │  │
│  │  • Battle Damage Calculator  → Kinetic Energy Prediction            │  │
│  │  • Oracle Controller         → Supervisor Integration               │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    UI & THEMING                                     │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • ThemeManager.cs            → Light/Dark Theme Switching          │  │
│  │  • GlassmorphismUI.cs         → Translucent Modal Effects            │  │
│  │  • UIThemeHelper.cs           → Static Color Helper                 │  │
│  │  • ROS2DashboardManager.cs    → ROS 2 Connection & Visualization    │  │
│  │  • GodModeOverlay.cs          → 7D State Visualization              │  │
│  │  • OmnipotentAiMode.cs        → God Mode Safety Overrides           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────┬───────────────────────────────────────────────────┘
                            │
                            │ WebGL Build / PWA
                            │
┌───────────────────────────▼───────────────────────────────────────────────────┐
│                         WEB CLIENT LAYER                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    PWA CAPABILITIES                                 │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • Service Worker (service-worker.js)                                │  │
│  │    - Offline Access                                                 │  │
│  │    - Background Telemetry Logging                                   │  │
│  │    - PWA Manifest Registration                                      │  │
│  │                                                                      │  │
│  │  • WebRTC Manager (webrtc-manager.js)                                │  │
│  │    - P2P Live Video Streaming                                        │  │
│  │    - Real-Time Telemetry (<20ms latency)                            │  │
│  │                                                                      │  │
│  │  • IndexedDB Manager (indexeddb-manager.js)                          │  │
│  │    - High-Frequency Log Storage                                     │  │
│  │    - VNC Verification Logs                                          │  │
│  │    - SIM2VAL++ Data Persistence                                      │  │
│  │                                                                      │  │
│  │  • Crypto Utils (crypto-utils.js)                                    │  │
│  │    - AES-256 GCM Encryption                                         │  │
│  │    - SHA-256 Hashing                                                │  │
│  │                                                                      │  │
│  │  • Responsive UI Shell (ui-shell.js)                                │  │
│  │    - Touch Gesture Handlers                                         │  │
│  │    - Mobile-Friendly Layout                                         │  │
│  │    - Mode Switching (Research/Production)                           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    PREDICTIVE AI (BATTLEFIELD-GRADE)                 │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │  • Seer Network (seer-network.js)                                    │  │
│  │    - Transformer Model (Simulated)                                   │  │
│  │    - Danger Score Prediction                                         │  │
│  │    - Optimal Action Forecasting                                      │  │
│  │                                                                      │  │
│  │  • Dynamic Enveloper (dynamic-enveloper.js)                          │  │
│  │    - Adaptive Safety Boundaries                                      │  │
│  │    - 3D Wireframe Visualization                                      │  │
│  │                                                                      │  │
│  │  • Battle Damage Calculator (battle-calculator.js)                   │  │
│  │    - Kinetic Energy Calculation                                      │  │
│  │    - Pre-Crash Warning System                                       │  │
│  │                                                                      │  │
│  │  • Oracle Controller (oracle-controller.js)                          │  │
│  │    - Supervisor Integration                                        │  │
│  │    - Emergency Stop Commands                                        │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         DATA FLOW                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  Hardware (Jetson)                                                           │
│       │                                                                       │
│       ├─→ ROS 2 DDS ──→ Unity Dashboard ──→ Rust Core (P-Score)             │
│       │                                                                       │
│       ├─→ WebRTC ──→ PWA Dashboard ──→ IndexedDB (Logs)                    │
│       │                                                                       │
│       └─→ Asset Streaming ──→ Rust Server ──→ Unity (Chunked)                │
│                                                                               │
│  User Interaction:                                                            │
│       │                                                                       │
│       ├─→ XR Head Tracking ──→ XRDeviceManager ──→ Focus Zones              │
│       │                                                                       │
│       ├─→ QR Code Scan ──→ QrCodeManager ──→ PeripheralBridge               │
│       │                                                                       │
│       └─→ FaceID ──→ BiometricAuthenticator ──→ Consciousness Update         │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Dashboard Summary

### Core Capabilities

#### 1. **Safety & Certification**
- **Ironclad 7D Math**: Position, velocity, heading, timestamp, certainty, fatigue
- **P-Score Calculation**: Real-time safety score (P = x + y + z + t + g + i + c)
- **Rust Core Integration**: Memory-safe safety logic via P/Invoke
- **Certification Compiler**: Notary system for ISO 26262 compliance
- **Dynamic Safety Zones**: Adaptive exclusion zones based on certainty
- **Self-Healing Safety**: Auto-recovery from collisions

#### 2. **Swarm-AGI System**
- **Multi-Agent Coordination**: NASA-style swarm management
- **Heterogeneous Task Delegation**: Dynamic model assignment (VLA/RL/SSM)
- **Global Voxel Map**: Shared 3D occupancy mapping
- **Fleet Certification View**: Simultaneous P-score monitoring

#### 3. **Dual-Mode Platform**
- **Production Mode**: Fleet discovery, mission profiles, security portal, analytics
- **Academia Mode**: Session recording, annotation tools, experiment workflow
- **Seamless Switching**: No capability loss between modes

#### 4. **Research-Grade Tools**
- **Episodic RL**: Reward logging and success/failure tracking
- **Standardized Benchmarks**: Unity Package/Scene loading
- **Fine-Grained Control**: Joint-level manipulation (Franka Panda, ALOHA)
- **Curriculum Learning**: Automated task progression
- **DOTS Swarm**: 10k+ agent simulation with Burst Compiler
- **3D AI Visualization**: VLM state tree debugging

#### 5. **Living World Simulation**
- **Dynamic Environment**: Day/night cycles, weather effects
- **Reactive Agents**: Crowd simulation with social forces
- **Physics Interactions**: Raycast-based doors, buttons
- **Procedural Audio**: Event-driven sound generation
- **Live Texture Painting**: Real-time data asset creation

#### 6. **Hardware Integration**
- **QR Code Scanning**: Zone navigation and command execution
- **Biometric Authentication**: FaceID with consciousness tracking
- **Peripheral Bridge**: Generic hardware interface
- **Simular Controller**: Sim-to-real transition support

#### 7. **XR-Ready Features**
- **Head Tracking**: HMD pose tracking with camera fallback
- **Focus Zones**: 3D target visualization in XR
- **Dynamic Safety Zones**: Breathing wireframe spheres
- **Latency Monitoring**: <20ms loop requirement tracking

#### 8. **Platform-Grade Web App**
- **PWA**: Offline access, background sync, installable
- **WebRTC**: P2P live streaming (<20ms latency)
- **IndexedDB**: High-frequency telemetry logging
- **AES-256/SHA-256**: Military-grade encryption
- **Responsive UI**: Mobile touch gestures, swipe modes

#### 9. **Battlefield-Grade Tactical**
- **Predictive AI**: Transformer-based danger forecasting
- **Dynamic Envelopment**: Adaptive safety boundaries
- **Battle Damage Calculator**: Kinetic energy prediction
- **Oracle Controller**: Supervisor with emergency stop

#### 10. **Memory & Performance**
- **Memory Manager**: Unity Addressables with auto-unload
- **Chunked Streaming**: Large file handling (GB+) via Rust server
- **Network Profiling**: Built-in latency monitoring
- **Non-Blocking Loading**: Additive scene loading

---

## Technology Stack

### Backend
- **Rust**: Core safety logic (`nav_lambda_core`, `nav_lambda_server`)
- **ROS 2**: Robot communication (DDS auto-discovery)
- **HTTP Server**: Asset streaming (Tokio, Hyper)

### Frontend
- **Unity 6.3 LTS**: Main dashboard engine
- **WebGL**: Browser deployment
- **PWA**: Progressive Web App capabilities
- **WebRTC**: Real-time streaming
- **IndexedDB**: Client-side storage

### Safety & Compliance
- **ISO 26262**: Automotive functional safety
- **Control Barrier Functions (CBF)**: Rigorous safety verification
- **SIM2VAL++**: Uncertainty reduction techniques
- **7D State Space**: Comprehensive safety modeling

---

## Deployment Architecture

### Development
- **Unity Editor**: Scene setup, testing, debugging
- **Rust Cargo**: Core library compilation
- **Local Server**: Python HTTP server for WebGL testing

### Production
- **Jetson**: Hardware deployment (ROS 2 bridge)
- **Unity Dashboard**: Laptop/Desktop visualization
- **PWA Dashboard**: Mobile/Web access
- **Rust Server**: Asset streaming and safety computation

### Academia
- **Newcastle University**: Research testbed deployment
- **Session Recording**: Student activity logging
- **Annotation Tools**: Professor feedback system
- **Experiment Orchestration**: Automated research workflows

---

## Key Features Summary

✅ **Real-Time Safety**: <20ms latency, P-score calculation every frame  
✅ **Multi-Agent Swarm**: 10k+ agents with DOTS optimization  
✅ **XR Immersive**: Head tracking, focus zones, 3D visualization  
✅ **Dual-Mode**: Production + Academia without capability loss  
✅ **Hardware-Grade**: QR codes, FaceID, peripheral integration  
✅ **Platform-Grade Web**: PWA, WebRTC, IndexedDB, encryption  
✅ **Predictive AI**: Transformer-based danger forecasting  
✅ **Memory-Safe**: Rust core, chunked streaming, auto-unload  
✅ **Research-Ready**: Episodic RL, benchmarks, curriculum learning  
✅ **Living World**: Dynamic environment, reactive agents, procedural audio  

---

## System Requirements

### Unity
- Unity 6.3 LTS or later
- ROS-TCP-Connector package
- XR Subsystems (optional, for XR features)

### Rust
- Rust 1.70+ (for core safety logic)
- Cargo build system

### Web
- Modern browser (Chrome, Firefox, Safari, Edge)
- WebRTC support
- IndexedDB support
- Service Worker support (for PWA)

### Hardware
- Jetson (for ROS 2 deployment)
- Optional: XR headset (Quest, Vision Pro, etc.)
- Optional: QR code scanner, biometric reader

---

## Documentation Files

- `FINAL_DEPLOYMENT_GUIDE.md` - Complete deployment checklist
- `DEPLOYMENT_GUIDE.md` - Step-by-step deployment instructions
- `SWARM_AGI_SYSTEM.md` - Swarm-AGI architecture details
- `DUAL_MODE_PLATFORM.md` - Dual-mode system documentation
- `RESEARCH_PLATFORM.md` - Research-grade features
- `UNITY_6_3_FEATURES.md` - Unity 6.3 LTS integration
- `LIVING_WORLD.md` - Living world simulation
- `MEMORY_OPTIMIZATION.md` - Memory management strategies
- `UPLOAD_GUIDE.md` - Asset upload instructions
- `WEBGL_BUILD_GUIDE.md` - WebGL build process
- `PLATFORM_WEB_APP.md` - PWA/WebRTC documentation
- `RUST_UNITY_INTEGRATION.md` - Rust-Unity FFI guide

---

**© 2024 Newcastle University - NAVΛ Dashboard**

*Complete SOTA System for Real-Time Robotics Safety, Swarm Coordination, and Research-Grade Experimentation*
