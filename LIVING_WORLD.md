# Living World System - NAVΛ Dashboard

## Overview

The NAVΛ Dashboard has been upgraded with **Living World** attributes that make the environment feel **Dynamic, Reactive, and Interactive**:

- ✅ **Dynamic World Controller** - Day/night cycles, weather effects, reactive agents
- ✅ **Physics Interaction System** - Doors, buttons, levers, physical interactions
- ✅ **Procedural Audio Manager** - Dynamic sound generation based on events
- ✅ **Live Texture Painter** - Real-time texture painting for data collection

## Architecture

### Core Modules

1. **Dynamic World Controller** (`DynamicWorldController.cs`)
   - Day/night cycle management
   - Weather system (rain, fog, wind)
   - Reactive agent system (crowds that avoid robot)
   - Environmental state updates

2. **Physics Interaction System** (`PhysicsInteractionSystem.cs`)
   - Raycast-based interaction detection
   - Door animation system
   - Button press mechanics
   - Lever toggling
   - Visual highlighting

3. **Procedural Audio Manager** (`ProceduralAudioManager.cs`)
   - Event-based sound generation
   - 3D spatial audio
   - Weather audio (rain, wind)
   - Footstep sounds
   - Impact/collision sounds

4. **Live Texture Painter** (`LiveTexturePainter.cs`)
   - Real-time texture painting
   - Brush system with size control
   - Color presets (mud, dirt, oil, rust)
   - Texture export for data collection

## Usage

### Dynamic World Setup

```csharp
// Get dynamic world controller
DynamicWorldController world = GetComponent<DynamicWorldController>();

// Set weather
world.SetRainIntensity(0.8f);
world.SetFogDensity(0.5f);

// Get environmental modifiers
float friction = world.GetFrictionModifier();
float visibility = world.GetVisibilityModifier();
```

### Physics Interactions

```csharp
// Get interaction system
PhysicsInteractionSystem interaction = GetComponent<PhysicsInteractionSystem>();

// Check if object is interactable
bool canInteract = interaction.IsInteractable(doorObject);

// Get interaction state
InteractionState state = interaction.GetInteractionState(doorObject);
```

### Procedural Audio

```csharp
// Get audio manager
ProceduralAudioManager audio = GetComponent<ProceduralAudioManager>();

// Play footstep
audio.PlayFootstep(agentObject, position);

// Play rain
audio.PlayRain(rainIntensity);

// Play impact
audio.PlayImpact(position, intensity);

// Update weather audio
audio.UpdateWeatherAudio(rainIntensity, windIntensity);
```

### Texture Painting

```csharp
// Get texture painter
LiveTexturePainter painter = GetComponent<LiveTexturePainter>();

// Set color
painter.SetColor(Color.brown); // Mud

// Set brush size
painter.SetBrushSize(1.0f);

// Clear paint
painter.ClearPaint(renderer);

// Export texture
painter.ExportTexture(renderer, "Assets/Painted/painted_texture.png");
```

## Workflow

### Setting Up Living World

1. **Add Components:**
   ```csharp
   // Add to scene root
   GameObject worldObj = new GameObject("DynamicWorld");
   worldObj.AddComponent<DynamicWorldController>();
   worldObj.AddComponent<ProceduralAudioManager>();
   worldObj.AddComponent<PhysicsInteractionSystem>();
   worldObj.AddComponent<LiveTexturePainter>();
   ```

2. **Configure Layers:**
   - Create "Interactable" layer
   - Assign to doors, buttons, levers
   - Set in PhysicsInteractionSystem

3. **Add Agents:**
   - Tag objects as "Agent"
   - Place in scene
   - Agents will automatically react to robot

4. **Add Interactables:**
   - Tag doors as "Door"
   - Tag buttons as "Button"
   - Tag levers as "Lever"

### Testing Interactions

1. **Test Doors:**
   - Click on door (or press E)
   - Door should animate open/close
   - Visual feedback provided

2. **Test Buttons:**
   - Click on button
   - Button should press down
   - Trigger events if configured

3. **Test Weather:**
   - Adjust rain intensity slider
   - Rain particles should appear
   - Audio should play
   - Friction should decrease

4. **Test Painting:**
   - Hold Ctrl + Left Mouse
   - Paint on objects
   - Export painted textures

## Integration

### With Existing Systems

1. **Universal Model Manager:**
   - Listen to weather state
   - Adjust speed based on wetness
   - Enable night mode

2. **Adaptive VLA Manager:**
   - Consider visibility in decisions
   - Adjust confidence based on weather
   - React to crowd density

3. **Environment Profiler:**
   - Monitor friction changes
   - Track visibility
   - Log weather events

## Best Practices

1. **Performance:**
   - Limit agent count (50-100 max)
   - Use object pooling for decals
   - Optimize audio sources

2. **Weather Effects:**
   - Gradually transition weather
   - Use particle systems efficiently
   - Cache audio clips

3. **Interactions:**
   - Use layers for filtering
   - Implement cooldowns
   - Provide visual feedback

4. **Texture Painting:**
   - Limit texture resolution
   - Use texture compression
   - Export only when needed

## Troubleshooting

### Agents Not Reacting

**Solution:**
1. Check agent tags ("Agent")
2. Verify robot GameObject name/tag
3. Check DynamicWorldController enabled

### Interactions Not Working

**Solution:**
1. Check layer masks
2. Verify interaction distance
3. Check object tags
4. Ensure colliders present

### Audio Not Playing

**Solution:**
1. Check AudioListener present
2. Verify audio sources assigned
3. Check volume settings
4. Ensure clips loaded

### Painting Not Working

**Solution:**
1. Check paint layer mask
2. Verify object has Renderer
3. Check painting enabled
4. Ensure Ctrl key held

## Next Steps

1. **Advanced Crowd AI:**
   - Pathfinding for agents
   - Group behaviors
   - Social dynamics

2. **Enhanced Weather:**
   - Snow, storms
   - Dynamic weather transitions
   - Weather forecasting

3. **More Interactions:**
   - Switches, dials
   - Containers, drawers
   - Complex mechanisms

4. **Advanced Painting:**
   - Brush textures
   - Layer system
   - Undo/redo

## License

NAVΛ Dashboard - Living World System
© 2024 Newcastle University
