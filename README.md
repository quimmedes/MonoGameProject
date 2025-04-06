# MonoGame Procedural Terrain Generator

A 3D terrain generation system built with MonoGame, featuring procedural terrain generation with biome-based coloring and dynamic terrain loading.

## Features

### Terrain Generation
- Procedural terrain generation using FastNoiseLite for realistic height maps
- 11 different biome types:
  - Ocean
  - Plains
  - Forest
  - Hills
  - Mountains
  - Snowy Mountains
  - Desert
  - Savanna
  - Swamp
  - Coniferous Forest
  - Snowy Coniferous Forest
- Dynamic terrain chunk loading based on camera position
- Realistic lighting with ambient and directional light

### Camera System
- First-person camera controls
- WASD keys for movement
- Mouse look for camera rotation
- Space/Shift for vertical movement (up/down)
- Adjustable camera speed and sensitivity

### Graphics
- Modern 3D rendering with MonoGame
- Vertex color and normal mapping for terrain visualization
- Texture support for terrain features
- Anti-aliasing and depth testing
- Dynamic terrain LOD (Level of Detail) system

## Controls

- **Movement**:
  - W: Move forward
  - S: Move backward
  - A: Strafe left
  - D: Strafe right
  - Space: Move up
  - Shift: Move down
  
- **Camera**:
  - Mouse: Look around
  - Mouse sensitivity can be adjusted in the settings

## Technical Details

### Implementation
- Written in C# using MonoGame Framework
- Custom vertex structures for efficient terrain rendering
- Chunk-based terrain system for better performance
- Biome classification based on height, moisture, and temperature

### Performance Optimizations
- Dynamic chunk loading/unloading
- Efficient vertex and index buffer management
- View frustum culling
- Distance-based detail scaling

## Getting Started

### Prerequisites
- .NET Core SDK
- MonoGame Framework

### Building and Running
1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution using `dotnet build`
4. Run the project using `dotnet run`

## Project Structure

- `Game1.cs`: Main game loop and initialization
- `Terrain/`
  - `TerrainGenerator.cs`: Procedural terrain generation logic
  - `TerrainChunk.cs`: Individual terrain chunk management
  - `TerrainManager.cs`: Overall terrain system management
  - `VertexPositionNormalColorTexture.cs`: Custom vertex structure

## Future Enhancements

- [ ] Additional biome types
- [ ] Texture blending between biomes
- [ ] Vegetation system
- [ ] Weather effects
- [ ] Day/night cycle
- [ ] Save/load terrain functionality

## Code Examples

### Initializing the Terrain Generator
```csharp
// Create a new terrain generator with custom parameters
var terrainGenerator = new TerrainGenerator(
    seed: 12345,            // Random seed for consistent generation
    heightScale: 250f,       // Maximum terrain height
    frequency: 0.01f,        // Noise frequency for terrain features
    octaves: 6              // Number of noise octaves for detail
);
```

### Setting Up the Terrain Manager
```csharp
// Initialize the terrain manager with desired parameters
_terrainManager = new TerrainManager(
    graphicsDevice: GraphicsDevice,
    generator: _terrainGenerator,
    effect: null,              // Using BasicEffect in TerrainChunk
    textures: _terrainTextures,
    chunkSize: 200,            // Size of each terrain chunk
    resolution: 64,            // Mesh resolution per chunk
    heightScale: 250f,         // Maximum terrain height
    viewDistance: 6            // Number of chunks to render
);
```

### Creating a Custom Vertex Structure
```csharp
[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionNormalColorTexture : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Color Color;
    public Vector2 TexCoord;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    );

    public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color color, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        Color = color;
        TexCoord = texCoord;
    }
}
```

### Implementing Camera Controls
```csharp
// Update camera rotation based on mouse movement
Point currentMouseState = Mouse.GetState().Position;
if (currentMouseState != _lastMousePosition)
{
    float deltaX = currentMouseState.X - _lastMousePosition.X;
    float deltaY = currentMouseState.Y - _lastMousePosition.Y;
    
    _cameraYaw += deltaX * _mouseSensitivity;
    _cameraPitch -= deltaY * _mouseSensitivity;
    _cameraPitch = MathHelper.Clamp(_cameraPitch, -MathHelper.PiOver2, MathHelper.PiOver2);
    
    // Update camera direction
    _cameraDirection = new Vector3(
        (float)(Math.Cos(_cameraPitch) * Math.Sin(_cameraYaw)),
        (float)Math.Sin(_cameraPitch),
        (float)(Math.Cos(_cameraPitch) * Math.Cos(_cameraYaw))
    );
}
```

### Generating Biome Colors
```csharp
public Color GetBiomeColor(BiomeType biome, float height, float moisture)
{
    switch (biome)
    {
        case BiomeType.Ocean:
            return new Color(0, 50, 200);  // Deep blue
        case BiomeType.Plains:
            return new Color(100, 180, 50); // Light green
        case BiomeType.Forest:
            return new Color(0, 100, 0);    // Dark green
        case BiomeType.Mountains:
            return new Color(100, 100, 100); // Gray
        // Add more biome colors...
        default:
            return Color.White;
    }
}
```

## Contributing

Feel free to contribute to this project by submitting issues or pull requests. All contributions are welcome!

## License

This project is licensed under the MIT License - see the LICENSE file for details.
