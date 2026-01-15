# Adaptive Safety - Self-Healing & Context-Awareness

## Overview

The **Adaptive Safety** system transitions from "Ironclad Evolution" (Teaching) to **Self-Healing** and **Context-Awareness**. The dashboard now **mutates its own parameters** based on the real world, rather than waiting for manual CSV updates.

## Architecture

### The Adaptive Safety Loop

```
[Environment Perception] → [Risk Assessment] → [Parameter Adaptation] → [Self-Healing] → [Improved Safety]
         ↑                                                                                        ↓
         └────────────────────────────────────────────────────────────────────────────────────────┘
```

## Components

### 1. Environment Profiler (`EnvironmentProfiler.cs`)

**Purpose**: Detects terrain friction (slipperiness), lighting quality, and obstacle density, and auto-adjusts the Safety Alpha (α) in the Ironclad equation.

**Features**:
- **Terrain Analysis**: Raycast-based friction detection
- **Light Quality**: Ambient and main light intensity analysis
- **Obstacle Density**: Overlap sphere detection
- **Adaptive Alpha**: Automatically adjusts VNC safety alpha based on conditions

**Adaptive Logic**:
- **Low Friction (Ice)**: Higher Alpha (Strictness) → Slower, more cautious
- **High Friction (Asphalt)**: Lower Alpha (Speed) → Faster, more aggressive
- **Low Light**: Higher Alpha → More cautious
- **High Density**: Higher Alpha → More cautious

**API**:
```csharp
float friction = envProfiler.GetFriction(); // 0-1
float lightQuality = envProfiler.GetLightQuality(); // 0-1
float density = envProfiler.GetObstacleDensity(); // obstacles/m²
float riskFactor = envProfiler.GetRiskFactor(); // 0-1 (higher = more risky)
```

### 2. Self-Healing Safety (`SelfHealingSafety.cs`)

**Purpose**: Auto-Recovery System. If a collision occurs, the system increases the Safety Margin Buffer dynamically to prevent recurrence.

**Healing Protocol**:
1. **Detect Collision**: OnCollisionEnter triggers healing routine
2. **Pause Robot**: Wait for physics to settle
3. **Analyze Context**: Why did we crash?
   - Low friction → Increase margin
   - High collision count → Reduce speed (fatigue)
   - One-off error → Small margin increase
4. **Apply Healing**: Update margin, reduce speed, or adjust parameters
5. **Visual Feedback**: Healing vector, particles, sound

**Features**:
- Automatic collision detection
- Context-aware recovery
- ROS margin updates
- Reasoning HUD integration
- Visual healing feedback

**API**:
```csharp
void SetMargin(float margin); // Set safety margin manually
float GetCurrentMargin(); // Get current margin
int GetCollisionCount(); // Get collision count
void ResetCollisionCount(); // Reset collision tracking
```

### 3. Enhanced Adaptive VLA Manager

**Enhancement**: Environment-Based Risk Tuning

**New Features**:
- **Risk Appetite Adaptation**: VLA model adapts speed based on environment
- **Speed Modifier**: Automatically reduces speed on slippery/dark terrain
- **Real-Time Tuning**: Continuous adaptation based on environment profiler

**Adaptive Logic**:
- **Slippery Terrain (Friction < 0.8)**: Speed × 0.5 (Cut in half)
- **Low Light (Light < 0.5)**: Speed × 0.8 (Reduce by 20%)
- **Overall Risk Factor**: Speed × (1 - riskFactor × 0.3) (Up to 30% reduction)

**API**:
```csharp
void SetRiskFactor(float factor); // Set speed modifier (0-1)
void ReduceMaxSpeed(float reductionFactor); // Reduce base speed
float GetMaxSpeed(); // Get current max speed
float GetRiskFactor(); // Get current risk factor
```

### 4. Dynamic Zone Manager (`DynamicZoneManager.cs`)

**Purpose**: Context-Aware God Mode. Zones "breathe" (expand/contract) based on Certainty (P) from 7D Rigor.

**Zone Behavior**:
- **High Certainty (P > 70)**: Tight Control (Performance) → Small Radius (2m)
- **Low Certainty (P < 40)**: Large Buffers (Safety) → Large Radius (5m)
- **Medium Certainty**: Smooth interpolation

**Features**:
- Real-time zone radius calculation
- Smooth animation (breathing effect)
- Color-coded visualization
- Automatic margin updates for low certainty

**Visualization**:
- LineRenderer for zone ring
- Particle system for zone particles
- Color coding: Green (High Certainty) → Red (Low Certainty)

**API**:
```csharp
float GetCurrentRadius(); // Get current zone radius
float GetTargetRadius(); // Get target zone radius
void SetZoneRadius(float radius); // Set zone radius manually
```

## Integration

### Component Wiring

All components automatically find their dependencies:
- `EnvironmentProfiler` → `Navl7dRigor`, `Vnc7dVerifier`
- `SelfHealingSafety` → `EnvironmentProfiler`, `UniversalModelManager`, `ReasoningHUD`
- `AdaptiveVlaManager` → `EnvironmentProfiler` (enhanced)
- `DynamicZoneManager` → `NavlConsciousnessRigor`, `SelfHealingSafety`, `Vnc7dVerifier`

### Scene Setup

All adaptive safety components are automatically added via `SceneSetupHelper`:
1. **Environment Profiler**: Added to Real Robot with UI
2. **Self-Healing Safety**: Added to Real Robot with healing status UI
3. **Dynamic Zone Manager**: Added to Real Robot with zone status UI
4. **Enhanced Adaptive VLA**: Already integrated, now uses environment data

## Adaptive Safety Workflow

### 1. Environment Perception
- `EnvironmentProfiler` continuously analyzes terrain, light, and obstacles
- Calculates risk factor and adaptive alpha
- Updates VNC verifier safety alpha

### 2. Risk Assessment
- `AdaptiveVlaManager` evaluates environment risk
- Adjusts speed modifier based on conditions
- Updates model risk factor

### 3. Self-Healing
- `SelfHealingSafety` monitors collisions
- Analyzes context (terrain, fatigue, one-off)
- Applies appropriate healing (margin increase, speed reduction)

### 4. Dynamic Zones
- `DynamicZoneManager` monitors P-score
- Adjusts zone radius based on certainty
- Expands zones in low certainty scenarios

## Benefits

### 1. Self-Healing
- Robot recovers from collisions automatically
- Learns from mistakes without manual intervention
- Context-aware recovery strategies

### 2. Context-Awareness
- Adapts to terrain conditions automatically
- Adjusts to lighting conditions
- Responds to obstacle density

### 3. Real-Time Adaptation
- No offline training required
- Continuous parameter tuning
- Immediate response to environment changes

### 4. Mathematical Safety
- VNC alpha automatically adjusted
- Safety margins dynamically updated
- Zones adapt to certainty levels

## Example Scenarios

### Scenario 1: Slippery Terrain
1. Robot enters icy area
2. `EnvironmentProfiler` detects low friction (0.4)
3. Safety alpha increases (5.0 → 8.0)
4. `AdaptiveVlaManager` reduces speed (×0.5)
5. Robot moves slower and more cautiously

### Scenario 2: Collision Recovery
1. Robot collides with obstacle
2. `SelfHealingSafety` detects collision
3. Analyzes: Low friction detected
4. Increases margin buffer (1.0m → 1.2m)
5. Resumes operation with larger safety margin

### Scenario 3: Low Certainty
1. P-score drops below 40
2. `DynamicZoneManager` detects low certainty
3. Expands zone radius (2m → 5m)
4. Updates safety margin to match zone
5. Robot enters "Safe Mode"

## Summary

The **Adaptive Safety** system transforms the NAVΛ Dashboard into a **Self-Healing, Context-Aware** platform:

- ✅ **Environment Perception**: Automatic terrain/light/obstacle detection
- ✅ **Risk Assessment**: Real-time risk factor calculation
- ✅ **Parameter Adaptation**: Automatic alpha and speed tuning
- ✅ **Self-Healing**: Automatic collision recovery
- ✅ **Dynamic Zones**: Context-aware safety boundaries

**Key Achievement**: The system now **mutates its own parameters** based on the real world, achieving true **Adaptive Safety**.
