using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameProject.Terrain
{
    public class TerrainChunk
    {
        private const int VERTICES_PER_TRIANGLE = 3;
        private const int INDICES_PER_QUAD = 6;

        private GraphicsDevice _graphicsDevice;
        private TerrainGenerator _terrainGenerator;
        private BasicEffect _basicEffect;
        private Texture2D[] _textures;
        
        // Vertex data
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private VertexPositionNormalColorTexture[] _vertices;
        private int[] _indices;
        
        // Biome color mapping
        private Dictionary<BiomeType, Color> _biomeColors = new Dictionary<BiomeType, Color>();

        private Vector2 _position;
        private int _size;
        private int _resolution;
        private float _heightScale;
        private BoundingBox _boundingBox;
        private bool _isVisible = true;

        public Vector2 Position => _position;
        public BoundingBox BoundingBox => _boundingBox;
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        public TerrainChunk(GraphicsDevice graphicsDevice, TerrainGenerator terrainGenerator, 
            Vector2 position, int size, int resolution, float heightScale, Effect terrainEffect, Texture2D[] textures)
        {
            _graphicsDevice = graphicsDevice;
            _terrainGenerator = terrainGenerator;
            _position = position;
            _size = size;
            _resolution = resolution;
            _heightScale = heightScale;
            _textures = textures ?? new Texture2D[0]; // Ensure textures is never null
            
            // Create a BasicEffect instead of using a custom shader
            _basicEffect = new BasicEffect(graphicsDevice)
            {
                TextureEnabled = _textures.Length > 0, // Only enable texturing if we have textures
                VertexColorEnabled = true, // We're now using vertex colors for biome visualization
                LightingEnabled = true
            };
            
            // Make sure the effect knows we're using vertex colors
            _basicEffect.VertexColorEnabled = true;
            
            // Set up lighting with stronger values for better visibility
            _basicEffect.DirectionalLight0.Enabled = true;
            _basicEffect.DirectionalLight0.Direction = new Vector3(0.5f, -0.7f, 0.5f); // Less steep angle for better lighting
            _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0.9f); // Slightly warm light
            _basicEffect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.6f); // Stronger ambient light
            
            // Set texture if available
            if (_textures.Length > 0)
            {
                _basicEffect.Texture = _textures[0];
            }
            
            // Initialize biome colors
            InitializeBiomeColors();

            GenerateMesh();
            CalculateBoundingBox();
        }

        private void GenerateMesh()
        {
            try
            {
                // Generate heightmap for this chunk
                float[,] heightMap = _terrainGenerator.GenerateHeightMap(_resolution, _resolution, _position);

                // Create vertices and indices
                CreateVerticesAndIndices(heightMap);

                // Calculate normals
                CalculateNormals();

                // Create vertex and index buffers
                _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalColorTexture), _vertices.Length, BufferUsage.WriteOnly);
                _vertexBuffer.SetData(_vertices);

                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, _indices.Length, BufferUsage.WriteOnly);
                _indexBuffer.SetData(_indices);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateMesh: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to see the error in the console
            }
        }

        private void CreateVerticesAndIndices(float[,] heightMap)
        {
            int vertexCount = _resolution * _resolution;
            int indexCount = (_resolution - 1) * (_resolution - 1) * INDICES_PER_QUAD;

            _vertices = new VertexPositionNormalColorTexture[vertexCount];
            _indices = new int[indexCount];

            // Create vertices
            for (int z = 0; z < _resolution; z++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    int vertexIndex = z * _resolution + x;
                    
                    // Calculate position in world space
                    float worldX = (float)x / (_resolution - 1) * _size;
                    float worldZ = (float)z / (_resolution - 1) * _size;
                    float height = heightMap[x, z] * _heightScale;

                    Vector3 position = new Vector3(worldX, height, worldZ);
                    position.X += _position.X * _size;
                    position.Z += _position.Y * _size;
                    
                    // Get absolute world position for biome determination
                    float absoluteX = position.X;
                    float absoluteZ = position.Z;
                    
                    // Get biome information at this position
                    BiomeInfo biomeInfo = _terrainGenerator.GetBiomeAt(absoluteX, absoluteZ);
                    
                    // Get color based on biome
                    Color vertexColor = GetBiomeColor(biomeInfo);
                    
                    // Apply height-based color variation (darker in valleys, lighter on peaks)
                    float normalizedHeight = height / _heightScale;
                    vertexColor = AdjustColorByHeight(vertexColor, normalizedHeight);
                    
                    // Apply slope-based color variation
                    if (x > 0 && z > 0 && x < _resolution - 1 && z < _resolution - 1)
                    {
                        float heightL = heightMap[x-1, z] * _heightScale;
                        float heightR = heightMap[x+1, z] * _heightScale;
                        float heightU = heightMap[x, z-1] * _heightScale;
                        float heightD = heightMap[x, z+1] * _heightScale;
                        
                        // Calculate approximate slope
                        float slopeX = (heightR - heightL) / (2 * _size / (_resolution - 1));
                        float slopeZ = (heightD - heightU) / (2 * _size / (_resolution - 1));
                        float slope = (float)Math.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                        
                        // Adjust color based on slope (rocky on steep slopes)
                        if (slope > 0.5f && biomeInfo.Type != BiomeType.Ocean)
                        {
                            // Blend with rock color based on slope
                            float rockBlend = MathHelper.Clamp((slope - 0.5f) * 2f, 0f, 0.8f);
                            Color rockColor = new Color(100, 100, 100); // Gray rock
                            vertexColor = Color.Lerp(vertexColor, rockColor, rockBlend);
                        }
                    }

                    // Calculate texture coordinates (u, v)
                    float u = (float)x / (_resolution - 1);
                    float v = (float)z / (_resolution - 1);
                    Vector2 texCoord = new Vector2(u, v);

                    // Create vertex (normal will be calculated later)
                    _vertices[vertexIndex] = new VertexPositionNormalColorTexture(
                        position, Vector3.Up, vertexColor, texCoord);
                }
            }

            // Create indices for triangles
            int index = 0;
            for (int z = 0; z < _resolution - 1; z++)
            {
                for (int x = 0; x < _resolution - 1; x++)
                {
                    int topLeft = z * _resolution + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * _resolution + x;
                    int bottomRight = bottomLeft + 1;

                    // First triangle (top-left, bottom-left, top-right)
                    _indices[index++] = topLeft;
                    _indices[index++] = bottomLeft;
                    _indices[index++] = topRight;

                    // Second triangle (top-right, bottom-left, bottom-right)
                    _indices[index++] = topRight;
                    _indices[index++] = bottomLeft;
                    _indices[index++] = bottomRight;
                }
            }
        }

        private void CalculateNormals()
        {
            // Initialize normals to zero
            for (int i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Normal = Vector3.Zero;
            }

            // Calculate normals for each triangle and accumulate them at vertices
            for (int i = 0; i < _indices.Length; i += VERTICES_PER_TRIANGLE)
            {
                int index1 = _indices[i];
                int index2 = _indices[i + 1];
                int index3 = _indices[i + 2];

                Vector3 side1 = _vertices[index2].Position - _vertices[index1].Position;
                Vector3 side2 = _vertices[index3].Position - _vertices[index1].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                // Accumulate the normal to each vertex of the triangle
                _vertices[index1].Normal += normal;
                _vertices[index2].Normal += normal;
                _vertices[index3].Normal += normal;
            }

            // Normalize all normals
            for (int i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Normal.Normalize();
                
                // Adjust color based on normal (darker on steep slopes)
                float steepness = Vector3.Dot(_vertices[i].Normal, Vector3.Up);
                if (steepness < 0.8f) // Steeper than ~37 degrees
                {
                    float darkening = MathHelper.Lerp(0.6f, 1.0f, steepness);
                    _vertices[i].Color = new Color(
                        (int)(_vertices[i].Color.R * darkening),
                        (int)(_vertices[i].Color.G * darkening),
                        (int)(_vertices[i].Color.B * darkening));
                }
            }
        }

        private void CalculateBoundingBox()
        {
            // Find min and max positions
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var vertex in _vertices)
            {
                min = Vector3.Min(min, vertex.Position);
                max = Vector3.Max(max, vertex.Position);
            }

            _boundingBox = new BoundingBox(min, max);
        }

        public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition)
        {
            if (!_isVisible || _vertexBuffer == null || _indexBuffer == null)
                return;

            try
            {
                // Set vertex and index buffers
                _graphicsDevice.SetVertexBuffer(_vertexBuffer);
                _graphicsDevice.Indices = _indexBuffer;
                
                // Ensure vertex color is enabled for our custom vertex structure
                _basicEffect.VertexColorEnabled = true;

                // Set BasicEffect parameters
                _basicEffect.World = Matrix.Identity;
                _basicEffect.View = view;
                _basicEffect.Projection = projection;
                
                // Set ambient color based on camera height for atmospheric effect
                float atmosphericFactor = MathHelper.Clamp((cameraPosition.Y - 100) / 1000, 0, 0.5f);
                _basicEffect.AmbientLightColor = new Vector3(0.5f + atmosphericFactor, 0.5f + atmosphericFactor, 0.6f + atmosphericFactor);
                
                // Apply fog effect for distance
                _basicEffect.FogEnabled = true;
                _basicEffect.FogColor = new Vector3(0.7f, 0.8f, 0.9f); // Light blue-gray fog
                _basicEffect.FogStart = 500f;
                _basicEffect.FogEnd = 1000f;
                
                // Make sure material color is bright enough to be visible
                _basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
                
                // Make sure wireframe mode is disabled for solid terrain rendering
                _graphicsDevice.RasterizerState = new RasterizerState
                {
                    CullMode = CullMode.CullCounterClockwiseFace,
                    FillMode = FillMode.Solid
                };
                
                // Apply the effect
                foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertices.Length, 0, _indices.Length / VERTICES_PER_TRIANGLE);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Draw: {ex.Message}");
            }
        }



        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
        
        private void InitializeBiomeColors()
        {
            // Define colors for each biome type
            _biomeColors[BiomeType.Ocean] = new Color(0, 75, 168);           // Deep blue
            _biomeColors[BiomeType.Plains] = new Color(120, 180, 70);        // Light green
            _biomeColors[BiomeType.Forest] = new Color(40, 120, 40);         // Dark green
            _biomeColors[BiomeType.Hills] = new Color(110, 150, 90);         // Olive green
            _biomeColors[BiomeType.Mountains] = new Color(120, 120, 120);    // Gray
            _biomeColors[BiomeType.SnowyMountains] = new Color(230, 230, 240); // White
            _biomeColors[BiomeType.Desert] = new Color(220, 200, 110);      // Sand
            _biomeColors[BiomeType.Savanna] = new Color(190, 170, 90);      // Yellow-green
            _biomeColors[BiomeType.Swamp] = new Color(70, 100, 70);         // Dark olive
            _biomeColors[BiomeType.ConiferousForest] = new Color(50, 90, 50); // Dark green
            _biomeColors[BiomeType.SnowyConiferousForest] = new Color(150, 180, 180); // Blue-green
        }
        
        private Color GetBiomeColor(BiomeInfo biomeInfo)
        {
            // Get base color for the biome
            if (_biomeColors.TryGetValue(biomeInfo.Type, out Color baseColor))
            {
                // Apply moisture and temperature variations
                if (biomeInfo.Type != BiomeType.Ocean && biomeInfo.Type != BiomeType.SnowyMountains)
                {
                    // More moisture = darker and more saturated
                    float moistureFactor = biomeInfo.Moisture * 0.4f;
                    baseColor.R = (byte)MathHelper.Clamp(baseColor.R * (1 - moistureFactor), 0, 255);
                    baseColor.G = (byte)MathHelper.Clamp(baseColor.G * (1 - moistureFactor * 0.5f), 0, 255);
                    
                    // Lower temperature = more blue tint
                    float tempFactor = (1 - biomeInfo.Temperature) * 0.3f;
                    baseColor.R = (byte)MathHelper.Clamp(baseColor.R * (1 - tempFactor), 0, 255);
                    baseColor.B = (byte)MathHelper.Clamp(baseColor.B * (1 + tempFactor), 0, 255);
                }
                
                return baseColor;
            }
            
            // Fallback color (green)
            return new Color(0, 255, 0);
        }
        
        private Color AdjustColorByHeight(Color baseColor, float normalizedHeight)
        {
            // Darker in valleys, lighter on peaks
            float heightFactor = MathHelper.Lerp(0.7f, 1.2f, normalizedHeight);
            
            return new Color(
                (byte)MathHelper.Clamp(baseColor.R * heightFactor, 0, 255),
                (byte)MathHelper.Clamp(baseColor.G * heightFactor, 0, 255),
                (byte)MathHelper.Clamp(baseColor.B * heightFactor, 0, 255));
        }
    }
}
