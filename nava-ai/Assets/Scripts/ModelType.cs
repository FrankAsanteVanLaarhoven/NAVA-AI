using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Universal model types supported by the dashboard
/// </summary>
public enum ModelType
{
    VLA,        // Vision-Language-Action (Visual Attention)
    VLM,        // Vision-Language Model (Text Reasoning)
    RL,         // Reinforcement Learning (Q-Values)
    SSM,        // State Space Model
    SafeVLA,    // Safe VLA with constraints
    AGI,        // Artificial General Intelligence
    Quadrotor,  // Drone/Aerial vehicle
    Humanoid    // Humanoid robot
}

/// <summary>
/// Robot form factor types
/// </summary>
public enum RobotFormFactor
{
    Ground,     // Ground-based (wheeled/legged)
    Aerial,     // Flying (quadrotor, fixed-wing)
    Humanoid,   // Humanoid (bipedal)
    Manipulator // Arm/manipulator
}

/// <summary>
/// Formation shapes for swarm control
/// </summary>
public enum FormationShape
{
    Line,
    V,
    Circle,
    Grid,
    Diamond,
    Custom
}
