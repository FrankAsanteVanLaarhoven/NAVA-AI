# Dual-Mode Platform - Academia & Production

## Overview

The NAVΛ Dashboard now includes a **Dual-Mode Platform** that seamlessly switches between **Academia Mode** (Research/Experiments) and **Production Mode** (Fleet Deployment) while ensuring **NO capabilities are lost**.

## Architecture

### Mode Switching

```
[Academia Mode] ←→ [Dual Mode Manager] ←→ [Production Mode]
     ↓                                           ↓
[Session Recorder]                        [Fleet Discovery]
[Annotation Tool]                         [Security Portal]
[Workflow Controller]                     [Analytics Hub]
                                          [Video Streamer]
```

## Production Capabilities

### 1. Fleet Discovery Manager (`FleetDiscoveryManager.cs`)

**Purpose**: Auto-discover all agents on the network using ROS 2 DDS.

**Features**:
- **Auto-Discovery**: Broadcasts discovery requests
- **Dynamic Agent Spawning**: Creates agent objects based on discovery
- **Agent Tracking**: Monitors agent health and timeout
- **Real-Time Updates**: Continuous fleet status monitoring

**API**:
```csharp
// Force discovery scan
fleetDiscovery.ForceDiscovery();

// Get discovered agents
List<DiscoveredAgent> agents = fleetDiscovery.GetDiscoveredAgents();

// Get agent by ID
DiscoveredAgent agent = fleetDiscovery.GetAgent("Robot_ID_001");
```

### 2. Mission Profile System (`MissionProfileSystem.cs`)

**Purpose**: Load/save JSON profiles for different robot roles (Cleaning vs. Delivery).

**Features**:
- **Profile Management**: Create, load, save profiles
- **Role Switching**: Quick role changes
- **Configuration Persistence**: JSON-based storage
- **System Integration**: Auto-applies settings to ROS

**API**:
```csharp
// Load profile
missionProfile.LoadProfile("delivery_profile.json");

// Switch profile
missionProfile.SwitchProfile("cleaning");

// Get available profiles
List<string> profiles = missionProfile.GetAvailableProfiles();
```

### 3. Security Portal (`SecurityPortal.cs`)

**Purpose**: Token-based authentication (JWT) before fleet control.

**Features**:
- **Token Validation**: JWT-style token checking
- **Session Management**: Auto-logout on timeout
- **Lock Screen**: UI lock when unauthenticated
- **Persistent Sessions**: Save/restore login state

**API**:
```csharp
// Login
securityPortal.Login("PRD-TOKEN-123");

// Logout
securityPortal.Logout();

// Check auth status
bool authenticated = securityPortal.IsAuthenticated();
```

### 4. Real-Time Analytics Hub (`RealTimeAnalyticsHub.cs`)

**Purpose**: Aggregate metrics from all agents and push to web dashboard.

**Features**:
- **Fleet Aggregation**: Average P-scores, margins, FPS
- **Metrics History**: Store historical data
- **Dashboard Push**: HTTP/WebSocket to Grafana/InfluxDB
- **Real-Time Updates**: Continuous monitoring

**API**:
```csharp
// Get aggregated metrics
AgentMetrics metrics = analyticsHub.GetAggregatedMetrics();

// Get metrics history
List<AgentMetrics> history = analyticsHub.GetMetricsHistory();

// Clear history
analyticsHub.ClearHistory();
```

### 5. Video Streamer (`VideoStreamer.cs`)

**Purpose**: Webcam feed streaming for remote operators.

**Features**:
- **Webcam Integration**: Unity WebCamTexture
- **Multi-Camera Support**: Switch between cameras
- **Picture-in-Picture**: UI overlay for telepresence
- **Permission Handling**: Automatic webcam permission requests

**API**:
```csharp
// Start streaming
videoStreamer.StartStreaming();

// Stop streaming
videoStreamer.StopStreaming();

// Switch camera
videoStreamer.SwitchCamera(1);

// Get available cameras
string[] cameras = videoStreamer.GetAvailableCameras();
```

## Academia Capabilities

### 6. Academic Session Recorder (`AcademicSessionRecorder.cs`)

**Purpose**: Record student sessions with replayable JSON files.

**Features**:
- **Session Tracking**: Student name, start/end time, duration
- **Data Logging**: Custom session data storage
- **JSON Export**: Replayable session files
- **Auto-Save**: Automatic log saving

**API**:
```csharp
// Start session
sessionRecorder.StartSession();

// End session
sessionRecorder.EndSession();

// Add session data
sessionRecorder.AddSessionData("mission_type", "baseline");

// Check if recording
bool recording = sessionRecorder.IsRecording();
```

### 7. Lecture Annotation Tool (`LectureAnnotationTool.cs`)

**Purpose**: Digital sticky notes for professors on 3D scene.

**Features**:
- **3D Annotation**: Place notes in world space
- **Text Input**: UI for annotation text
- **Persistence**: Save/load annotations
- **Visualization**: TextMesh or prefab-based notes

**API**:
```csharp
// Get all annotations
List<Annotation> annotations = annotationTool.GetAnnotations();

// Clear all annotations
annotationTool.ClearAnnotations();
```

### 8. Experiment Workflow Controller (`ExperimentWorkflowController.cs`)

**Purpose**: Orchestrate experiments with crowd simulation and session recording.

**Features**:
- **Experiment Control**: Start/stop/pause experiments
- **Session Sync**: Automatic session recording
- **Crowd Integration**: Connect to crowd simulation
- **Auto-Restart**: Batch experiment support

**API**:
```csharp
// Start experiment
workflowController.StartExperiment();

// Stop experiment
workflowController.StopExperiment();

// Pause experiment
workflowController.PauseExperiment();

// Check status
bool running = workflowController.IsRunning();
bool paused = workflowController.IsPaused();
```

## Dual Mode Manager

### 9. Dual Mode Manager (`DualModeManager.cs`)

**Purpose**: Master controller for switching between Academia and Production modes.

**Features**:
- **Mode Switching**: Academia, Production, or Hybrid
- **Component Management**: Enable/disable components per mode
- **UI Integration**: Mode status display and dropdown
- **No Data Loss**: Components disabled, not destroyed

**Modes**:
- **Academia**: Session Recorder, Annotation Tool, Workflow Controller
- **Production**: Fleet Discovery, Security Portal, Analytics Hub, Video Streamer
- **Hybrid**: Both modes enabled simultaneously

**API**:
```csharp
// Set mode
dualMode.SetMode(DualModeManager.SystemMode.Production);

// Get current mode
SystemMode mode = dualMode.GetCurrentMode();

// Toggle mode
dualMode.ToggleMode();
```

## Integration

### Scene Setup

All dual-mode components are automatically added via `SceneSetupHelper`:
1. **Fleet Discovery Manager**: Added with UI
2. **Mission Profile System**: Added to ROS Manager
3. **Security Portal**: Added with lock screen and login panel
4. **Real-Time Analytics Hub**: Added with metrics display
5. **Video Streamer**: Added to ROS Manager
6. **Academic Session Recorder**: Added to ROS Manager
7. **Lecture Annotation Tool**: Added to ROS Manager
8. **Experiment Workflow Controller**: Added with status UI
9. **Dual Mode Manager**: Added as master controller

### System Verification

`NavlProjectVerifier` now checks for:
- ✅ Fleet Discovery Manager
- ✅ Mission Profile System
- ✅ Security Portal
- ✅ Analytics Hub
- ✅ Video Streamer
- ✅ Session Recorder
- ✅ Annotation Tool
- ✅ Workflow Controller
- ✅ Dual Mode Manager

## Use Cases

### Academia Mode (Newcastle University)

1. **Start Session**:
   - Set student name
   - Start session recording
   - Begin experiment

2. **Run Experiment**:
   - Start workflow controller
   - Crowd simulation activates
   - Session data logged

3. **Add Annotations**:
   - Click in 3D scene
   - Add professor notes
   - Notes persist with replay

4. **End Session**:
   - Stop experiment
   - End session recording
   - Export JSON for grading

### Production Mode (Fleet Deployment)

1. **Authenticate**:
   - Enter production token
   - Security portal unlocks
   - Fleet discovery activates

2. **Discover Fleet**:
   - Auto-detect agents on network
   - Agents spawn dynamically
   - Fleet status displayed

3. **Monitor Analytics**:
   - Real-time metrics aggregation
   - Dashboard push enabled
   - Fleet health monitoring

4. **Video Streaming**:
   - Enable webcam feed
   - Remote operator view
   - Multi-camera support

### Hybrid Mode

- Both Academia and Production enabled
- Useful for research with production fleet
- All capabilities available simultaneously

## Summary

The Dual-Mode Platform provides:

- ✅ **Academia Mode**: Session recording, annotations, experiment workflow
- ✅ **Production Mode**: Fleet discovery, security, analytics, video streaming
- ✅ **Hybrid Mode**: Both modes simultaneously
- ✅ **No Data Loss**: Components disabled, not destroyed
- ✅ **Seamless Switching**: Instant mode changes
- ✅ **Complete Integration**: All previous features preserved

**Key Achievement**: The system now supports **Dual-Personality** operation, seamlessly switching between **Rigorous Academia** (Experiments) and **Hardened Production** (Fleet Deployment) while ensuring **NO capabilities are lost**.

The NAVΛ Dashboard is now a **Complete Platform** capable of:
- **Newcastle Research**: Crowds, Orchestration, Annotation
- **Production**: Fleet Discovery, Auth, Analytics, OTA
- **Dual-Personality**: Seamless switching between Research and Ops modes
- **SOTA Integration**: All previous features (Rust Math, VNC 7D, Swarm-AGI, etc.) preserved

This is the **Thesis-Ready** deliverable for Newcastle University.
