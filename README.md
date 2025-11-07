
<center>![Preview](procedural/Assets/Ressources/img.png)</center>


# Procedural Generation Methods for Unity

This project implements three procedural generation algorithms in Unity using C#. Each method generates different types of terrain and environments suitable for game development.

## Overview

This repository contains three procedural generation approaches:

1. **Noise-Based Fractal Generation** - Creates organic terrain with multiple biomes
2. **Binary Space Partitioning (BSP)** - Generates dungeon layouts with rooms and corridors
3. **Cellular Automata** - Produces cave-like environments with natural formations

## Common Features

- Built on Unity's ScriptableObject architecture
- Asynchronous generation using UniTask
- Grid-based system
- Configurable parameters through the Unity Inspector

## 1. Noise Fractal Generation (`NoiseFrac.cs`)

Generates terrain using FastNoiseLite with fractal noise patterns. This method creates realistic landscapes with different biomes based on height values.

### Features
- Multiple biome types: Water, Sand, Grass, Mountains
- Configurable noise parameters (frequency, amplitude)
- Fractal settings (octaves, lacunarity, persistence)
- Adjustable height thresholds for each biome
- Automatic player spawn point

### Parameters
```csharp
// Noise Settings
- NoiseType: OpenSimplex2, Perlin, etc.
- Frequency: 0.02 - 0.05
- Amplitude: -1 to 1

// Fractal Settings
- FractalType: FBm, Ridged, PingPong
- Octaves: 1-8
- Lacunarity: 1-5
- Persistence: 0.1-1

// Biome Heights
- Water Height: -0.4
- Sand Height: -0.1
- Grass Height: 0.25
- Rock Height: 0.55
```

### Use Cases
- Open-world terrain generation
- Island generation
- Heightmap-based landscapes

## 2. Binary Space Partitioning (`BSP.cs`)

Creates interconnected rooms and corridors by recursively dividing the grid space.

### Features
- Recursive room subdivision
- Automatic corridor generation between rooms
- Configurable room sizes and spacing
- L-shaped corridor connections

### Parameters
```csharp
- maxIterations: 4 (depth of subdivision)
- minRoomSize: 4 (minimum room dimensions)
- roomPadding: 2 (spacing between rooms and bounds)
```

### Algorithm Steps
1. Recursively divide grid into smaller rectangles
2. Create rooms in leaf nodes
3. Connect sibling rooms with L-shaped corridors
4. Place ground tiles throughout

### Use Cases
- Dungeon generation
- Building interiors
- Roguelike level layouts

## 3. Cellular Automata (`CellularAutomata.cs`)

Simulates organic cave formation using cellular automaton rules over multiple iterations.

### Features
- Iterative simulation process
- Natural-looking cave formations
- Mountain generation in dense ground areas
- Visual step-by-step generation with delays
- Configurable density and thresholds

### Parameters
```csharp
- maxIterations: 4 (number of simulation steps)
- noise_density: 45 (initial water percentage)
- mountainThreshold: 6 (neighbors needed for mountains)
```

### Algorithm Steps
1. Initialize grid with random noise
2. Apply cellular automaton rules:
   - >4 water neighbors → becomes water
   - <4 water neighbors → becomes ground
   - =4 neighbors → maintains current state
3. Generate mountains in densely-packed ground areas
4. Visualize each iteration with delay

### Use Cases
- Cave systems
- Organic terrain features
- Natural water bodies

## Technical Details

### Dependencies
- **Unity** (2020.3+)
- **UniTask** - For async operations
- **FastNoiseLite** - Noise generation library
- **VTools.Grid** - Custom grid system
- **VTools.ScriptableObjectDatabase** - Template management

### Architecture

Each generator extends `ProceduralGenerationMethod` and implements:
```csharp
protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
```

### Data Structures

**NoiseCells** - Stores terrain type and noise value
```csharp
- IsWater, IsSand, IsGrass, IsMountain
- NoiseValue
```

**Cells** - Stores cellular automata state
```csharp
- IsWater, IsMountain, IsGround
```

**TestBSP** - BSP tree node
```csharp
- Bounds, Room, Children
- Recursive division logic
```

## Usage

1. Create a ScriptableObject asset:
   ```
   Right-click in Project window → Create → Procedural Generation Method → [Method Name]
   ```

2. Configure parameters in the Inspector

3. Assign to your grid generator

4. The generation will run automatically when triggered

- Add visualization tools for parameter tuning
