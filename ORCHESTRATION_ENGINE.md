# Ultimate SOTA Orchestration Engine

## Overview

The **Orchestration Engine** completes the transition from a static simulator to a **Living World** system with curriculum learning, fatigue-based safety, and hardware abstraction. This adds **5 Final Modules** that integrate seamlessly with the existing NAVΛ Dashboard architecture.

---

## The 5 Final Modules

### 1. OrchestrationManager.cs

**Purpose**: Manages curriculum learning progression automatically, transitioning from simple tasks (Push) to complex tasks (Stacking Blocks → Door).

**Features**:
- **Automatic Stage Progression**: Simple Push → Stack Block → Franka Kitchen → Door
- **Episode Management**: Integrates with `ResearchEpisodeManager` for episode tracking
- **Scene Loading**: Uses `ResearchSceneManager` for non-blocking scene loading
- **Timeout Handling**: Auto-reset on episode timeout
- **UI Integration**: Real-time curriculum status display

**Key Methods**:
- `RunStage(int stageIndex)` - Load and run a specific curriculum stage
- `NextStage()` - Advance to next stage automatically
- `StartCurriculum()` - Begin curriculum from stage 0
- `ResetCurrentStage()` - Reset current stage for retry

**Integration**:
- Works with `ResearchEpisodeManager` for episode statistics
- Uses `ResearchSceneManager` for additive scene loading
- Updates UI via `curriculumStatusText`

---

### 2. GodModeRigor.cs

**Purpose**: Evolves "God Mode" 3.0 with fatigue-based safety zone expansion. Dynamic expansion of safety zones when P-score drops (low certainty).

**Features**:
- **Fatigue System**: Time-based fatigue that increases risk over time
- **Dynamic Zone Expansion**: Safety zones expand based on fatigue level
- **Visual Feedback**: Red flash, siren audio, particle effects on breach
- **Recovery System**: Automatic fatigue recovery when safe
- **P-Score Integration**: Uses `NavlConsciousnessRigor` for safety calculation

**Key Methods**:
- `GetFatigueState()` - Check if robot is in high fatigue state
- `CalculateMinMargin()` - Calculate minimum safety margin to obstacles
- `TriggerGodModeVisuals()` - Visual/audio feedback on safety breach
- `ResetFatigue()` - Reset fatigue to zero

**Integration**:
- Integrates with `NavlConsciousnessRigor` for P-score
- Updates `DynamicZoneManager` for zone expansion
- Uses `UIThemeHelper` for consistent theming

---

### 3. UniversalHal.cs

**Purpose**: Unified Hardware Abstraction Layer between VLA (Simulated) and Real Robot. Acts as the "Notary" between Unity Dashboard and Jetson hardware.

**Features**:
- **Hardware Bridge**: Connects to Jetson via ROS 2 or simulated mode
- **Risk Factor Management**: Adjusts robot speed based on risk level
- **Connection Management**: Auto-connect with timeout handling
- **Telemetry Streaming**: Sends velocity commands to hardware
- **Safety Checks**: Integrates with `NavlConsciousnessRigor` for safety state

**Key Methods**:
- `ConnectToHardware()` - Establish connection to Jetson
- `SendVelocityCommand(float linear, float angular)` - Send movement commands
- `SetRiskFactor(float risk)` - Adjust risk-based speed modifier
- `IsInSafe()` - Check if robot is in safe state (P-score > 50)

**Integration**:
- Uses `ROS2DashboardManager` for ROS 2 communication
- Integrates with `AdaptiveVlaManager` for speed adjustment
- Updates UI with connection status

---

### 4. MemoryManager.cs (Updated)

**Purpose**: 4GB RAM fix with VRAM crash detection for Jetson Orin deployment.

**Enhancements**:
- **VRAM Monitoring**: Detects when graphics memory exceeds 4GB
- **Emergency Unload**: Automatic asset unloading on VRAM overflow
- **Addressables Support**: Releases Addressables when available
- **Scene Reset**: Loads empty scene on critical memory condition

**Key Features**:
- Monitors both system memory and graphics memory
- Color-coded UI feedback (Green/Yellow/Red)
- Editor window for manual memory management
- Auto-unload threshold configuration

---

### 5. DynamicWorldController.cs (Already Exists)

**Purpose**: Manages living world attributes (day/night cycles, weather, reactive agents).

**Integration Points**:
- Works with `OrchestrationManager` for environment setup
- Provides dynamic obstacles for `GodModeRigor` safety checks
- Spawns crowd agents for realistic testing

---

## System Integration

### Data Flow

```
OrchestrationManager
    ↓
    Loads Scene (ResearchSceneManager)
    ↓
    Starts Episode (ResearchEpisodeManager)
    ↓
    Robot Moves (UniversalHal → ROS 2)
    ↓
    Safety Check (GodModeRigor → NavlConsciousnessRigor)
    ↓
    Memory Monitor (MemoryManager)
    ↓
    Next Stage (if success) or Reset (if failure)
```

### Component Relationships

```
OrchestrationManager
├── ResearchEpisodeManager (Episode tracking)
├── ResearchSceneManager (Scene loading)
└── CurriculumTask[] (Stage definitions)

GodModeRigor
├── NavlConsciousnessRigor (P-score calculation)
├── DynamicZoneManager (Zone expansion)
└── Camera (Visual feedback)

UniversalHal
├── ROS2DashboardManager (ROS 2 connection)
├── AdaptiveVlaManager (Speed adjustment)
└── NavlConsciousnessRigor (Safety checks)

MemoryManager
├── System Memory (GC monitoring)
├── Graphics Memory (VRAM monitoring)
└── Addressables (Asset management)
```

---

## Deployment Instructions

### Scene Setup

1. **Create Orchestration Controller**:
   - Create empty GameObject: `OrchestrationController`
   - Attach `OrchestrationManager.cs`
   - Assign `curriculumStatusText` UI element

2. **Add God Mode Rigor**:
   - Attach `GodModeRigor.cs` to robot GameObject
   - Assign `godModeText` UI element
   - Assign `dangerParticles` (optional)

3. **Setup Hardware Bridge**:
   - Attach `UniversalHal.cs` to robot GameObject
   - Assign `statusText` and `hardwareStatus` UI elements
   - Configure `jetsonIP` (default: 192.168.1.50)

4. **Memory Manager**:
   - Attach `MemoryManager.cs` to scene manager GameObject
   - Assign `memoryUsageText` and `graphicsMemoryText` UI elements
   - Set `unloadThresholdMB` (default: 512 MB)

### Curriculum Configuration

1. **Define Stages**:
   ```csharp
   curriculum.Add(new CurriculumTask { 
       name = "Simple Push", 
       difficulty = 1.0f, 
       scenePath = "Assets/Scenes/SimplePush.unity" 
   });
   ```

2. **Start Curriculum**:
   - Call `OrchestrationManager.StartCurriculum()` from UI button
   - Or use `RunStage(0)` to start from specific stage

3. **Monitor Progress**:
   - Watch `curriculumStatusText` for current stage
   - Check `ResearchEpisodeManager` for episode statistics

---

## Testing Scenarios

### Scenario 1: Simple Push → Stack

1. **Start**: `OrchestrationManager.StartCurriculum()`
2. **Observe**: Robot attempts "Simple Push" task
3. **Success**: Automatically advances to "Stack Block"
4. **Failure**: Resets and retries (if `autoReset = true`)

### Scenario 2: Fatigue-Based Safety

1. **Run**: Robot in "Franka Kitchen" (high difficulty)
2. **Observe**: `GodModeRigor` fatigue increases over time
3. **Trigger**: When fatigue > 0.5, safety zones expand
4. **Visual**: Red flash + siren when P-score < 50

### Scenario 3: Hardware Connection

1. **Simulated**: `UniversalHal` connects instantly (for testing)
2. **Real Hardware**: Connects to Jetson via ROS 2
3. **Risk Adjustment**: `SetRiskFactor(2.0f)` slows robot down
4. **Safety Check**: `IsInSafe()` returns false if P-score < 50

### Scenario 4: Memory Crash Prevention

1. **Monitor**: `MemoryManager` watches VRAM usage
2. **Threshold**: When VRAM > 4GB, triggers emergency unload
3. **Recovery**: Unloads Addressables and resets scene
4. **Prevention**: Prevents Unity crash on Jetson Orin

---

## Key Features Summary

✅ **Curriculum Learning**: Automatic progression from simple to complex tasks  
✅ **Fatigue-Based Safety**: Dynamic zone expansion based on time and risk  
✅ **Hardware Abstraction**: Unified interface for simulated and real hardware  
✅ **Memory Management**: 4GB VRAM crash prevention for Jetson deployment  
✅ **Living World**: Dynamic environment with reactive agents and weather  
✅ **Episode Tracking**: Integration with research-grade episode management  
✅ **Scene Loading**: Non-blocking additive scene loading  
✅ **Safety Integration**: Full integration with P-score and consciousness rigor  

---

## Integration with Existing Systems

### Swarm-AGI System
- `OrchestrationManager` can manage multiple agents through curriculum
- `GodModeRigor` provides fleet-wide safety monitoring
- `UniversalHal` supports multi-robot hardware abstraction

### Dual-Mode Platform
- **Academia Mode**: `OrchestrationManager` for student experiments
- **Production Mode**: `UniversalHal` for real hardware deployment
- Both modes use `MemoryManager` for resource management

### Research-Grade Tools
- `OrchestrationManager` integrates with `ResearchEpisodeManager`
- `CurriculumRunner` can work alongside `OrchestrationManager`
- Episode statistics logged for academic analysis

### XR-Ready Features
- `GodModeRigor` visual feedback works in XR viewport
- `UniversalHal` telemetry visible in XR headset
- `MemoryManager` prevents XR performance issues

---

## Performance Considerations

### Memory
- **4GB VRAM Limit**: `MemoryManager` prevents crashes on Jetson Orin
- **Auto-Unload**: Automatic asset cleanup when threshold exceeded
- **Addressables**: Efficient asset management for large scenes

### CPU
- **Fatigue Calculation**: Lightweight time-based increment
- **Safety Checks**: Efficient obstacle distance calculation
- **Scene Loading**: Non-blocking async loading prevents UI freeze

### Network
- **Hardware Connection**: 5-second timeout prevents hanging
- **ROS 2 Integration**: Efficient topic-based communication
- **Simulated Mode**: Zero network overhead for testing

---

## Future Enhancements

1. **Advanced Curriculum**: Machine learning-based difficulty adjustment
2. **Multi-Robot Orchestration**: Coordinate multiple robots through curriculum
3. **Real-Time Adaptation**: Dynamic difficulty based on success rate
4. **Cloud Integration**: Remote curriculum management and logging
5. **Visual Curriculum Editor**: Unity Editor tool for curriculum design

---

**© 2024 Newcastle University - NAVΛ Dashboard**

*Complete SOTA Orchestration Engine for Living World Simulation and Curriculum Learning*
