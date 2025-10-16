# FitzHughNagumo
# FitzHugh-Nagumo 3D Visualization

A real-time 3D visualization of the FitzHugh-Nagumo neuronal model using WPF (Windows Presentation Foundation). This application displays the phase space trajectory of neural dynamics in an interactive 3D environment.

## Overview

The FitzHugh-Nagumo model is a simplified version of the Hodgkin-Huxley model that describes the activation and deactivation dynamics of a spiking neuron. This visualization shows how the membrane potential (V) and recovery variable (W) evolve over time (T) in a 3D phase space.

### Mathematical Model

The FitzHugh-Nagumo equations:

```
dV/dt = V - V³/3 - W + I
dW/dt = ε(V + a - bW)
```

Where:
- **V**: Membrane potential (voltage)
- **W**: Recovery variable (represents combined effects of ion channels)
- **I**: External stimulus current
- **ε**: Time scale separation parameter (controls speed of W relative to V)
- **a, b**: Shape parameters of the nullclines

## Features

### Interactive 3D Visualization
- **Real-time trajectory rendering** as a colored tube in 3D space
- **Smooth camera controls**: drag to rotate, scroll to zoom
- **Color-coded axes**: Red (V), Green (W), Blue (T)
- **Gradient-colored trajectory**: visualizes temporal progression
- **Grid plane** for spatial reference

### Dynamic Parameter Control
- **Current (I)**: Adjust external stimulus (0 to 2.0)
- **Epsilon (ε)**: Control time scale separation (0.01 to 0.3)
- **Parameter a**: Modify nullcline shape (0.1 to 1.5)
- **Reset button**: Clear trajectory and restart simulation

### Visual Elements
- 3D coordinate system with labeled axes
- Semi-transparent legend overlay
- Grid plane at base
- Professional lighting from multiple angles

- <img width="592" height="347" alt="image" src="https://github.com/user-attachments/assets/9b3abcf4-4bb7-4b50-b9f4-95b21cf31f91" />


## Requirements

- **Windows OS** (Windows 7 or higher)
- **.NET Framework 4.7.2** or higher (or .NET 6/7/8 for modern WPF)
- **Visual Studio 2019/2022** (Community Edition or higher)

### Basic Controls

**Mouse Controls:**
- **Left-click + Drag**: Rotate the 3D view around the model
- **Mouse Wheel Up**: Zoom in
- **Mouse Wheel Down**: Zoom out

**Parameter Sliders:**
- Adjust **Current (I)** to change external stimulus
- Modify **ε (epsilon)** to alter time scale dynamics
- Change **a** to reshape the phase space behavior

**Reset Button:**
- Clears the trajectory and restarts from initial conditions

### Exploring Dynamics

Try these parameter combinations to see different behaviors:

#### 1. **Stable Limit Cycle** (Default)
- I = 0.5
- ε = 0.08
- a = 0.7
- *Result*: Beautiful spiral converging to periodic oscillation

#### 2. **Fast Spiking**
- I = 1.2
- ε = 0.15
- a = 0.7
- *Result*: Rapid oscillations with tight spiral

#### 3. **Relaxation Oscillations**
- I = 0.8
- ε = 0.03
- a = 0.7
- *Result*: Slow-fast dynamics with sudden jumps

#### 4. **Excitability**
- I = 0.2
- ε = 0.08
- a = 0.7
- *Result*: Damped oscillations returning to rest state

#### 5. **Bistability**
- I = 0.0
- ε = 0.08
- a = 0.7
- *Result*: System approaches stable fixed point

## Understanding the Visualization

### Axes
- **Red Axis (V)**: Represents the membrane potential of the neuron
- **Green Axis (W)**: Represents the recovery variable (slow dynamics)
- **Blue Axis (T)**: Represents time progression

### Trajectory Color
The trajectory uses a gradient from **blue → cyan → yellow** to show temporal progression, with newer sections appearing more yellow.

### Phase Space Interpretation
- The 3D curve shows how the system state (V, W) evolves over time
- **Spirals** indicate oscillatory behavior (action potentials)
- **Convergence** to a point indicates stable equilibrium
- **Limit cycles** appear as closed loops in the V-W plane

## Technical Details

### Numerical Integration
- **Method**: 4th-order Runge-Kutta (RK4) for accuracy
- **Time step**: dt = 0.02
- **Steps per frame**: 3 (for faster visualization)

### Rendering
- **Tube segments**: 8-sided polygons for trajectory
- **Maximum points**: 8000 (oldest points removed)
- **Mesh optimization**: Dynamic vertex/index buffers

### Performance
- **Frame rate**: ~60 FPS
- **Update rate**: ~16ms per frame
- **Simulation speed**: 3× real-time

## Code Structure

```
MainWindow.cs
├── Initialization
│   ├── InitializeScene()      // Setup 3D viewport
│   ├── InitializeTimer()      // Animation loop
│   └── CreateControlPanel()   // UI controls
├── Simulation
│   ├── Timer_Tick()           // RK4 integration
│   └── UpdateTrajectoryMesh() // Geometry update
├── Rendering
│   ├── AddTubeSegment()       // Trajectory tubes
│   ├── AddAxisArrow()         // Coordinate axes
│   └── AddGridPlane()         // Reference grid
└── Interaction
    ├── Camera controls        // Mouse interaction
    └── Parameter updates      // Slider callbacks
```

## Troubleshooting

### Application won't compile
- Ensure you're using a WPF project (not WinForms or Console)
- Check that .NET Framework 4.7.2+ is installed
- Verify all `using` statements are present

### 3D view appears black
- Check that DirectX is properly installed on your system
- Update your graphics drivers
- Try running as Administrator

### Slow performance
- Reduce `maxPoints` from 8000 to 4000
- Decrease `stepsPerFrame` from 3 to 1
- Lower tube segment count from 8 to 6

### Camera controls not working
- Ensure mouse events are properly attached to viewport
- Check that the viewport is receiving focus

## Extensions & Modifications

### Adding More Parameters
To add slider for parameter `b`:
```csharp
AddSlider(panel, "b:", 0.1, 1.5, b, (v) => { b = v; });
```

### Changing Color Scheme
Modify the gradient in `InitializeScene()`:
```csharp
gradientBrush.GradientStops.Add(new GradientStop(Colors.Purple, 0));
gradientBrush.GradientStops.Add(new GradientStop(Colors.Orange, 1));
```

### Export Trajectory Data
Add this method to save points to CSV:
```csharp
private void ExportData()
{
    var csv = "V,W,T\n" + string.Join("\n", 
        trajectoryPoints.Select(p => $"{p.X},{p.Y},{p.Z}"));
    System.IO.File.WriteAllText("trajectory.csv", csv);
}
```

## Scientific Background

The FitzHugh-Nagumo model (1961-1962) is a cornerstone of computational neuroscience. It captures the essential dynamics of neuronal excitability while being simple enough for mathematical analysis.

**Key Concepts:**
- **Excitability**: Small perturbations can trigger large responses
- **Refractoriness**: Recovery period after activation
- **Threshold behavior**: All-or-none responses
- **Oscillations**: Rhythmic firing patterns

**Applications:**
- Understanding action potential generation
- Cardiac arrhythmia modeling
- Network synchronization studies
- Bifurcation analysis in dynamical systems



## References

1. FitzHugh, R. (1961). "Impulses and Physiological States in Theoretical Models of Nerve Membrane"
2. Nagumo, J., et al. (1962). "An Active Pulse Transmission Line Simulating Nerve Axon"
3. Izhikevich, E. M. (2007). "Dynamical Systems in Neuroscience"

## License

This code is provided as-is for educational purposes. Feel free to modify and distribute.

## Author

Created as an educational visualization of the FitzHugh-Nagumo neuronal model.

---

**Enjoy exploring the fascinating dynamics of neural oscillations in 3D!** 🧠✨
