# Research-Grade Platform - NAVΛ Dashboard

## Overview

The NAVΛ Dashboard has been upgraded to a **Research-Grade Platform** capable of running:
- ✅ **Franka Panda** manipulation tasks
- ✅ **Trossen ALOHA** research benchmarks
- ✅ **Curriculum Learning** with domain adaptation
- ✅ **Episodic RL** with standardized data collection
- ✅ **Low-level manipulation control** for fine-grained research

## Architecture

### Core Modules

1. **Research Episode Manager** (`ResearchEpisodeManager.cs`)
   - Manages episodic RL (N=500 standard)
   - Tracks rewards, success/failure
   - Exports data to CSV/JSON (for Franka/Mujoco plots)
   - Auto-reset between episodes

2. **Benchmark Importer** (`BenchmarkImporter.cs`)
   - Loads standardized research environments
   - Supports Unity Packages and Scene files
   - Compatible with Franka Kitchen, Trossen Office

3. **Manipulation Controller** (`ManipulationController.cs`)
   - Fine-grained gripper control
   - Franka Panda aperture control
   - Independent finger positioning
   - Hardware bridge to ROS

4. **Curriculum Runner** (`CurriculumRunner.cs`)
   - Progressive difficulty (Simple -> Hard -> Complex)
   - Domain adaptation
   - Auto-advance on success
   - Repeat on failure

## Usage

### Running a Research Episode

```csharp
// Get episode manager
ResearchEpisodeManager episodeManager = GetComponent<ResearchEpisodeManager>();

// Start episode
episodeManager.StartEpisode();

// Log transitions (during episode)
episodeManager.LogTransition("state_name", reward, "details");

// Complete episode
episodeManager.CompleteEpisode(finalReward, "SUCCESS");
```

### Loading Research Environment

```csharp
// Get benchmark importer
BenchmarkImporter importer = GetComponent<BenchmarkImporter>();

// Set environment
importer.environmentName = "Franka Kitchen";
importer.assetBundlePath = "Assets/Environments/Franka_kitchen.unitypackage";

// Load
importer.LoadEnvironment();
```

### Fine Manipulation Control

```csharp
// Get manipulation controller
ManipulationController manip = GetComponent<ManipulationController>();

// Franka-style grip (0.0 = open, 1.0 = closed)
manip.SetFrankaGrip(0.5f);

// Individual fingers
manip.SetFingerPositions(0.3f, 0.7f);

// Open/Close
manip.OpenGripper();
manip.CloseGripper();
```

### Running Curriculum Learning

```csharp
// Get curriculum runner
CurriculumRunner curriculum = GetComponent<CurriculumRunner>();

// Start curriculum
curriculum.StartCurriculum();

// Curriculum will automatically:
// 1. Load Simple Push task
// 2. Run episodes
// 3. Advance to Complex Stack on success
// 4. Advance to Franka Kitchen on success
```

## Workflow

### Standard Research Workflow

1. **Setup Environment:**
   ```csharp
   BenchmarkImporter importer = GetComponent<BenchmarkImporter>();
   importer.environmentName = "Franka Kitchen";
   importer.LoadEnvironment();
   ```

2. **Configure Episode Manager:**
   ```csharp
   ResearchEpisodeManager episodeManager = GetComponent<ResearchEpisodeManager>();
   episodeManager.maxEpisodes = 500;
   episodeManager.episodeTimeout = 60.0f;
   episodeManager.successThreshold = 0.8f;
   ```

3. **Run Episodes:**
   ```csharp
   episodeManager.StartEpisode();
   // ... perform task ...
   episodeManager.CompleteEpisode(reward, "SUCCESS");
   ```

4. **Export Data:**
   ```csharp
   episodeManager.ExportLogs();
   // Exports to Assets/Research/episode_data.json and .csv
   ```

### Curriculum Learning Workflow

1. **Define Curriculum:**
   ```csharp
   CurriculumRunner curriculum = GetComponent<CurriculumRunner>();
   // Add tasks in order of difficulty
   ```

2. **Start Curriculum:**
   ```csharp
   curriculum.StartCurriculum();
   ```

3. **Monitor Progress:**
   - Check UI for current stage
   - Review episode statistics
   - Export data after completion

## Data Export

### Episode Data Format

**JSON:**
```json
{
  "episodes": [
    {
      "timestamp": "2024-01-01T12:00:00Z",
      "episode": 1,
      "state": "COMPLETE",
      "reward": 0.85,
      "outcome": "SUCCESS",
      "duration": 45.2,
      "success": true
    }
  ]
}
```

**CSV:**
```csv
timestamp,episode,state,reward,outcome,duration,success
2024-01-01T12:00:00Z,1,COMPLETE,0.85,SUCCESS,45.2,true
```

### Export Location

- **JSON:** `Assets/Research/episode_data.json`
- **CSV:** `Assets/Research/episode_data.csv`
- **Log:** `Assets/Research/research_log.csv`

## Hardware Integration

### Franka Panda

**ROS Integration:**
```csharp
ManipulationController manip = GetComponent<ManipulationController>();
manip.sendToHardware = true; // Enable ROS bridge
manip.SetFrankaGrip(0.5f); // Sends to /franka/gripper/command
```

**Required ROS Topics:**
- `/franka/gripper/command` (std_msgs/Float64)
- `/franka/joint_states` (sensor_msgs/JointState)

### Trossen ALOHA

**MuJoCo Compatibility:**
- Use `BenchmarkImporter` to load Trossen environments
- Fallback to Unity Scene if MuJoCo not available
- Manipulation controller works with ALOHA gripper

## Best Practices

1. **Episode Management:**
   - Use standard N=500 episodes for research
   - Set appropriate timeout (60s for simple, 120s for complex)
   - Enable auto-reset for efficiency

2. **Data Collection:**
   - Export after each run
   - Use consistent naming conventions
   - Include metadata (environment, date, parameters)

3. **Curriculum Design:**
   - Start with simple tasks (difficulty 1.0)
   - Gradually increase difficulty
   - Set realistic success thresholds

4. **Hardware Control:**
   - Test in simulation first
   - Enable hardware bridge only when ready
   - Monitor gripper state continuously

## Troubleshooting

### Environment Not Loading

**Solution:**
1. Check file paths in `BenchmarkImporter`
2. Verify Unity Package exists
3. Try fallback scene loading

### Episodes Not Completing

**Solution:**
1. Check timeout settings
2. Verify `CompleteEpisode()` is called
3. Review reward thresholds

### Curriculum Not Advancing

**Solution:**
1. Check success thresholds
2. Verify episode completion
3. Review `ShouldAdvance()` logic

## Next Steps

1. **Add More Benchmarks:**
   - Import Franka Kitchen Unity Package
   - Add Trossen ALOHA scenes
   - Create custom research environments

2. **Enhance Data Analysis:**
   - Add plotting tools
   - Integrate with research notebooks
   - Export to standard formats (HDF5, etc.)

3. **Hardware Integration:**
   - Complete ROS bridge
   - Add MuJoCo support
   - Test with real robots

## License

NAVΛ Dashboard - Research-Grade Platform
© 2024 Newcastle University
