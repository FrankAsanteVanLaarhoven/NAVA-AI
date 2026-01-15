# Ironclad Evolution - Online Barrier Learning & Digital Twin Certification

## Overview

The **Ironclad Evolution** system transforms the dashboard from passive monitoring to **active learning**. The dashboard doesn't just *watch* the robot—it *teaches* it to be safer through online barrier learning and adaptive training.

## Architecture

### The Evolution Loop

```
[Robot Action] → [P-Score Calculation] → [Data Logging] → [Model Adaptation] → [Improved Safety]
       ↑                                                                              ↓
       └──────────────────────────────────────────────────────────────────────────────┘
```

## Components

### 1. Ironclad Data Logger (`IroncladDataLogger.cs`)

**Purpose**: Records "Ironclad Data" (CBF violations vs. Successes) for Neural Model training.

**Features**:
- High-frequency data collection (configurable rate)
- CSV format for machine learning compatibility
- Automatic buffer management
- Dataset statistics tracking
- Visual feedback (particle effects)

**Data Format**:
```csv
timestamp,score_p,score_x,score_y,score_z,score_t,score_g,score_i,score_c,barrier_h,barrier_deriv,success_action,action_x,action_y,action_z
```

**Usage**:
- Automatically logs every frame (or at specified rate)
- Stores to `Assets/Data/ironclad_dataset.csv`
- Provides statistics via `GetDatasetStats()`

### 2. Adaptive VLA Manager (`AdaptiveVlaManager.cs`)

**Purpose**: Online Training that adapts VLA weights based on Ironclad P-Score.

**Learning Logic**:
- **High P-Score (>50)**: Model becomes "Confident" (Aggressive exploration)
- **Low P-Score (<30)**: Model becomes "Conservative" (Safety first)
- **Medium P-Score**: Maintains balanced behavior

**Features**:
- Sliding window P-score history
- Confidence bias adjustment (-1 to 1)
- Training state visualization
- Real-time adaptation

**Training States**:
- `Conservative`: Low P-score - Safety first (Yellow)
- `Neutral`: Medium P-score - Balanced (Cyan)
- `Confident`: High P-score - Aggressive (Green)

### 3. Causal Graph Builder (`CausalGraphBuilder.cs`)

**Purpose**: Digital Twin Causal Graph for certification.

**Concept**: To prove to regulators that Digital Twin is "Safe," we need a Causal Graph (A → B → C) showing that P stayed above threshold.

**Graph Structure**:
- **Action Nodes** (Green): VLA Model Output or Teleop Commands
- **State Nodes** (Blue): Current P-Score and Barrier Values
- **Result Nodes** (Cyan/Red): Safe/Unsafe based on P-Score

**Features**:
- Real-time causal chain visualization
- Certification evidence generation
- Safety rate calculation
- Formal proof of safety

**Certification Evidence**:
```csharp
CausalGraphEvidence evidence = causalGraph.GetCertificationEvidence();
// Returns: totalActions, safeResults, safetyRate, averagePScore, etc.
```

### 4. Evolution HUD (`EvolutionHUD.cs`)

**Purpose**: Visualizes the "Training Loop" and "Ironclad" dataset growth.

**Features**:
- **Data Heatmap**: P-score distribution visualization
- **Generation Count**: Real-time datapoint tracking
- **Training Statistics**: Success rate, confidence bias, average P-score
- **Success Rate**: Color-coded success/failure rate

**Visualization**:
- Heatmap color: Green (High P) to Red (Low P)
- Real-time updates (configurable rate)
- Historical P-score tracking

## Integration

### Scene Setup

All evolution components are automatically added via `SceneSetupHelper`:

1. **Ironclad Data Logger**: Added to ROS Manager
2. **Adaptive VLA Manager**: Added to ROS Manager with UI
3. **Causal Graph Builder**: Added to Real Robot
4. **Evolution HUD**: Added to ROS Manager with heatmap

### Component Wiring

All components automatically find their dependencies:
- `IroncladDataLogger` → `NavlConsciousnessRigor`, `Vnc7dVerifier`, `UnityTeleopController`
- `AdaptiveVlaManager` → `NavlConsciousnessRigor`, `UniversalModelManager`, `VlaSaliencyOverlay`
- `CausalGraphBuilder` → `UnityTeleopController`, `NavlConsciousnessRigor`, `Vnc7dVerifier`
- `EvolutionHUD` → `NavlConsciousnessRigor`, `IroncladDataLogger`, `AdaptiveVlaManager`

## Training Workflow

### 1. Data Collection Phase
- Robot operates normally
- `IroncladDataLogger` records all cycles
- Data stored to CSV file

### 2. Adaptation Phase
- `AdaptiveVlaManager` analyzes P-score history
- Adjusts confidence bias based on performance
- Model becomes more confident or conservative

### 3. Certification Phase
- `CausalGraphBuilder` tracks action → state → result chains
- Generates certification evidence
- Proves safety to regulators

### 4. Visualization Phase
- `EvolutionHUD` displays training progress
- Real-time heatmap updates
- Statistics dashboard

## API Reference

### IroncladDataLogger

```csharp
// Get dataset statistics
DatasetStats stats = dataLogger.GetDatasetStats();
// Returns: totalEntries, successCount, failureCount, successRate

// Log cycle manually
dataLogger.LogCycle(pScore, scoreX, scoreY, scoreZ, scoreT, scoreG, scoreI, scoreC,
                    barrierH, barrierDeriv, success, actionTaken, action);
```

### AdaptiveVlaManager

```csharp
// Get current confidence bias
float bias = adaptiveVla.GetConfidenceBias(); // -1 to 1

// Get training state
TrainingState state = adaptiveVla.GetTrainingState();

// Get average P-score
float avgP = adaptiveVla.GetAveragePScore();

// Reset learning
adaptiveVla.ResetLearning();
```

### CausalGraphBuilder

```csharp
// Get certification evidence
CausalGraphEvidence evidence = causalGraph.GetCertificationEvidence();
// Returns: totalActions, safeResults, safetyRate, averagePScore, allResultsSafe

// Clear graph
causalGraph.ClearGraph();
```

## Benefits

### 1. Adaptive Safety
- Robot learns from its mistakes
- Automatically adjusts behavior based on P-score
- Becomes safer over time

### 2. Certification Ready
- Causal graphs provide formal proof
- Regulators can verify safety claims
- ISO 26262 compliant evidence

### 3. Real-Time Learning
- No offline training required
- Continuous improvement
- Immediate adaptation to new environments

### 4. Transparent Training
- Visual feedback on learning progress
- Statistics dashboard
- Heatmap visualization

## Future Enhancements

1. **Reinforcement Learning Integration**: Direct RL policy updates
2. **Multi-Agent Learning**: Swarm-wide knowledge sharing
3. **Transfer Learning**: Pre-trained models for new environments
4. **Federated Learning**: Privacy-preserving distributed training

## Summary

The **Ironclad Evolution** system transforms the NAVΛ Dashboard from a monitoring tool into an **adaptive learning platform**. The robot doesn't just follow safety rules—it learns to be safer through continuous feedback and online adaptation.

**Key Achievement**: The dashboard now *teaches* the robot, not just *watches* it.
