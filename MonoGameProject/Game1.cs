using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameProject.Terrain;
using System;
using System.Collections.Generic;

namespace MonoGameProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        // 3D objects
        private VertexPositionColor[] _triangleVertices;
        private VertexPositionNormalColorTexture[] _terrainVertices;
        private int[] _terrainIndices;
        private VertexBuffer _terrainVertexBuffer;
        private IndexBuffer _terrainIndexBuffer;
        private BasicEffect _basicEffect;
        
        // Terrain
        private TerrainManager _terrainManager;
        private TerrainGenerator _terrainGenerator;
        private Texture2D _terrainTexture;
        private Texture2D[] _terrainTextures;
        
        // First-person camera
        private Matrix _view;
        private Matrix _projection;
        private Vector3 _cameraPosition;
        private Vector3 _cameraDirection;
        private Vector3 _cameraUp;
        private float _cameraSpeed = 5.0f;  // Increased for better terrain navigation
        private float _cameraYaw = 0f;   // Rotation around Y axis (left/right)
        private float _cameraPitch = 0f;  // Rotation around X axis (up/down)
        private float _mouseSensitivity = 0.003f;
        private Point _lastMousePosition;
        
        // Input tracking
        private KeyboardState _previousKeyboardState;
        
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;  // Hide mouse cursor by default for better camera control
            
            // Set resolution
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // Create a simple colored triangle for debug purposes
            _triangleVertices = new VertexPositionColor[3];
            
            // Define the triangle vertices with positions and colors
            _triangleVertices[0] = new VertexPositionColor(new Vector3(0, 10, 0), Color.Red);
            _triangleVertices[1] = new VertexPositionColor(new Vector3(10, -10, 0), Color.Green);
            _triangleVertices[2] = new VertexPositionColor(new Vector3(-10, -10, 0), Color.Blue);
            
            // Set up first-person camera with a higher starting position to see more terrain
            _cameraPosition = new Vector3(0, 200, 300);  // Starting much higher to see more terrain
            _cameraDirection = new Vector3(0, 0.3f, -1);  // Looking down toward terrain (positive Y)
            _cameraUp = Vector3.Up;            // Up direction
            _cameraYaw = 0f;
            _cameraPitch = 0.3f;  // Looking down toward terrain (positive pitch)
            _view = Matrix.CreateLookAt(_cameraPosition, _cameraPosition + _cameraDirection, _cameraUp);
            
            // Center the mouse and store its position
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            _lastMousePosition = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            _projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(70),  // Wider field of view for better terrain visibility
                GraphicsDevice.Viewport.AspectRatio,
                0.1f,  // near clipping plane
                2000f); // far clipping plane (increased for terrain viewing distance)
            
            // Create and configure the basic effect for rendering
            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.VertexColorEnabled = true;
            
            // Terrain will be created in LoadContent after textures are loaded
            
            base.Initialize();
        }
        


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Create a basic effect for our 3D rendering
            _basicEffect = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = true
            };
            
            // Create a simple test terrain
            CreateSimpleTestTerrain();
            
            // Set up a simple directional light
            _basicEffect.DirectionalLight0.Enabled = true;
            _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, -1));
            _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
            _basicEffect.AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f);
            
            // Create a checkerboard texture for the terrain
            _terrainTexture = CreateCheckerboardTexture(256, 256, 32, Color.ForestGreen, Color.DarkGreen);
            _terrainTextures = new Texture2D[] { _terrainTexture };
            
            // Ensure the BasicEffect is set up for textures
            _basicEffect.TextureEnabled = true;
            _basicEffect.Texture = _terrainTexture;
            
            // Create terrain after textures are loaded
            CreateTerrain();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Exit on Escape
            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();
                
            // Toggle mouse capture with Tab key
            if (keyboardState.IsKeyDown(Keys.Tab) && !_previousKeyboardState.IsKeyDown(Keys.Tab))
            {
                IsMouseVisible = !IsMouseVisible;
                
                // Display instructions when toggling mouse visibility
                Console.WriteLine(IsMouseVisible ? 
                    "Mouse cursor visible - press Tab to capture mouse for camera control" : 
                    "Mouse captured for camera control - press Tab to free cursor");
            }
            
            // Mouse camera rotation
            // Calculate mouse movement delta
            int deltaX = mouseState.X - _lastMousePosition.X;
            int deltaY = mouseState.Y - _lastMousePosition.Y;
            
            // Only rotate camera if there was actual mouse movement
            if (deltaX != 0 || deltaY != 0)
            {
                // Update camera orientation
                _cameraYaw -= deltaX * _mouseSensitivity;
                _cameraPitch -= deltaY * _mouseSensitivity;
                
                // Normalize yaw to allow full 360-degree rotation
                while (_cameraYaw > MathHelper.TwoPi)
                    _cameraYaw -= MathHelper.TwoPi;
                while (_cameraYaw < 0)
                    _cameraYaw += MathHelper.TwoPi;
                
                // Clamp pitch to avoid flipping
                _cameraPitch = MathHelper.Clamp(_cameraPitch, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);
                
                // Reset mouse position to center of screen to allow continuous rotation
                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                _lastMousePosition = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            }
            
            // Calculate new camera direction based on yaw and pitch
            // Inverting the pitch calculation to fix the upside-down view
            _cameraDirection = new Vector3(
                (float)Math.Cos(_cameraPitch) * (float)Math.Sin(_cameraYaw),
                (float)-Math.Sin(_cameraPitch),  // Inverting the Y component
                (float)Math.Cos(_cameraPitch) * (float)Math.Cos(_cameraYaw)
            );
            _cameraDirection.Normalize();
            
            // Camera movement
            Vector3 cameraMovement = Vector3.Zero;
            
            // Forward/backward movement along camera direction
            if (keyboardState.IsKeyDown(Keys.W))
                cameraMovement += _cameraDirection;
            else if (keyboardState.IsKeyDown(Keys.S))
                cameraMovement -= _cameraDirection;
                
            // Left/right movement perpendicular to camera direction
            Vector3 right = Vector3.Cross(_cameraDirection, Vector3.Up);
            right.Normalize();
            
            if (keyboardState.IsKeyDown(Keys.A))
                cameraMovement -= right;
            else if (keyboardState.IsKeyDown(Keys.D))
                cameraMovement += right;
                
            // Up/down movement along world up axis
            if (keyboardState.IsKeyDown(Keys.Space))
                cameraMovement.Y += 1;
            else if (keyboardState.IsKeyDown(Keys.LeftShift))
                cameraMovement.Y -= 1;
            
            // Apply movement if any keys were pressed
            if (cameraMovement != Vector3.Zero)
            {
                // Normalize the movement vector if we're moving in multiple directions
                if (cameraMovement.Length() > 1)
                    cameraMovement.Normalize();
                
                // Scale movement by speed and delta time
                cameraMovement *= _cameraSpeed * 100.0f * deltaTime;
                
                // Apply the movement
                _cameraPosition += cameraMovement;
            }
            
            // Update the view matrix
            _view = Matrix.CreateLookAt(_cameraPosition, _cameraPosition + _cameraDirection, Vector3.Up);
            
            // Update terrain chunks based on camera position
            if (_terrainManager != null)
            {
                _terrainManager.Update(_cameraPosition);
            }
            
            // Rotate the triangle
            float rotationAmount = (float)gameTime.TotalGameTime.TotalSeconds;
            Matrix rotation = Matrix.CreateRotationY(rotationAmount);
            
            // Update the effect matrices
            _basicEffect.World = rotation;
            _basicEffect.View = _view;
            _basicEffect.Projection = _projection;
            
            // Store current keyboard state for next frame
            _previousKeyboardState = keyboardState;
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen with a sky blue color for better atmosphere
            GraphicsDevice.Clear(new Color(135, 206, 235));
            
            // Set render states for 3D drawing
            GraphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = true // Enable anti-aliasing if supported
            };
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            
            // Draw the terrain first
            DrawTerrain();
            
            // Position the triangle in front of the camera so it's visible
            Vector3 trianglePosition = new Vector3(
                _cameraPosition.X + _cameraDirection.X * 10,
                _cameraPosition.Y + _cameraDirection.Y * 10,
                _cameraPosition.Z + _cameraDirection.Z * 10);
                
            Matrix triangleWorld = Matrix.CreateTranslation(trianglePosition) * 
                                  Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds);
            
            // Draw the triangle (commented out to focus on terrain)
            /*
            _basicEffect.World = triangleWorld;
            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _triangleVertices, 0, 1);
            }
            */
            
            // Draw a white crosshair in the center of the screen
            _spriteBatch.Begin();
            // Horizontal line
            _spriteBatch.Draw(CreateWhitePixel(), new Rectangle(
                GraphicsDevice.Viewport.Width / 2 - 10, 
                GraphicsDevice.Viewport.Height / 2, 
                20, 1), Color.White);
            // Vertical line
            _spriteBatch.Draw(CreateWhitePixel(), new Rectangle(
                GraphicsDevice.Viewport.Width / 2, 
                GraphicsDevice.Viewport.Height / 2 - 10, 
                1, 20), Color.White);
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }
        
        private void DrawTerrain()
        {
            // First try to draw the simple test terrain
            if (_terrainVertexBuffer != null && _terrainIndexBuffer != null)
            {
                // Set the vertex and index buffers
                GraphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
                GraphicsDevice.Indices = _terrainIndexBuffer;
                
                // Configure the basic effect for drawing
                _basicEffect.World = Matrix.Identity;
                _basicEffect.View = _view;
                _basicEffect.Projection = _projection;
                _basicEffect.VertexColorEnabled = true;
                _basicEffect.LightingEnabled = true;
                
                // Draw the test terrain
                foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,                          // vertex buffer offset
                        0,                          // base vertex index
                        _terrainVertices.Length,    // number of vertices
                        0,                          // start index in index buffer
                        _terrainIndices.Length / 3  // number of primitives
                    );
                }
            }
            
            // Then try the terrain manager if available
            if (_terrainManager != null)
            {
                _terrainManager.Draw(_view, _projection, _cameraPosition);
            }
        }
        
        private Texture2D CreateWhitePixel()
        {
            Texture2D texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            return texture;
        }
        
        private void CreateTerrain()
        {
            try
            {
                // Initialize terrain generator with a random seed for variety
                int seed = new Random().Next(10000);
                _terrainGenerator = new TerrainGenerator(
                    seed,
                    scale: 100f,           // Scale of the noise (smaller = more detailed terrain)
                    octaves: 6,            // Number of noise layers
                    persistence: 0.65f,     // How much each octave contributes (increased for more pronounced features)
                    lacunarity: 2.5f,       // How frequency increases with each octave (increased for more variation)
                    heightMultiplier: 250f  // Maximum height of the terrain (increased for more dramatic terrain)
                );
                
                // Initialize terrain manager
                _terrainManager = new TerrainManager(
                    GraphicsDevice,
                    _terrainGenerator,
                    null,                   // terrainEffect: Using null since we're using BasicEffect in TerrainChunk
                    _terrainTextures,       // textures: Our array of terrain textures
                    200,                    // chunkSize: Size of each terrain chunk in world units (increased for better visibility)
                    64,                     // chunkResolution: Resolution of the mesh for each chunk
                    250f,                   // heightScale: Maximum height of the terrain (increased to match generator)
                    6                       // viewDistance: How many chunks to render in each direction (increased for better visibility)
                );
                
                // Log success
                System.Diagnostics.Debug.WriteLine("Terrain created successfully");
            }
            catch (Exception ex)
            {
                // Log any errors during terrain creation
                System.Diagnostics.Debug.WriteLine($"Error creating terrain: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void CreateSimpleTestTerrain()
        {
            // Create a simple grid terrain for testing
            int terrainSize = 10; // 10x10 grid
            float gridSpacing = 50.0f; // 50 units between grid points
            
            // Calculate vertices and indices
            _terrainVertices = new VertexPositionNormalColorTexture[(terrainSize + 1) * (terrainSize + 1)];
            _terrainIndices = new int[terrainSize * terrainSize * 6]; // 2 triangles per grid cell, 3 indices per triangle
            
            // Create vertices
            for (int z = 0; z <= terrainSize; z++)
            {
                for (int x = 0; x <= terrainSize; x++)
                {
                    int index = z * (terrainSize + 1) + x;
                    
                    // Position at grid point with simple height variation
                    float height = (float)Math.Sin(x * 0.5f) * 20.0f + (float)Math.Cos(z * 0.5f) * 20.0f;
                    Vector3 position = new Vector3(x * gridSpacing - (terrainSize * gridSpacing / 2), height, z * gridSpacing - (terrainSize * gridSpacing / 2));
                    
                    // Color based on height
                    float colorValue = (height + 20.0f) / 40.0f; // Normalize to 0-1 range
                    Color color = new Color(0.0f, colorValue + 0.3f, 0.0f); // Green with height variation
                    
                    // Calculate texture coordinates
                    Vector2 texCoord = new Vector2((float)x / terrainSize, (float)z / terrainSize);
                    
                    // Create vertex with normal pointing up and texture coordinates
                    _terrainVertices[index] = new VertexPositionNormalColorTexture(position, Vector3.Up, color, texCoord);
                }
            }
            
            // Create indices for triangles
            int indexCount = 0;
            for (int z = 0; z < terrainSize; z++)
            {
                for (int x = 0; x < terrainSize; x++)
                {
                    int topLeft = z * (terrainSize + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * (terrainSize + 1) + x;
                    int bottomRight = bottomLeft + 1;
                    
                    // First triangle (top-left, bottom-left, top-right)
                    _terrainIndices[indexCount++] = topLeft;
                    _terrainIndices[indexCount++] = bottomLeft;
                    _terrainIndices[indexCount++] = topRight;
                    
                    // Second triangle (top-right, bottom-left, bottom-right)
                    _terrainIndices[indexCount++] = topRight;
                    _terrainIndices[indexCount++] = bottomLeft;
                    _terrainIndices[indexCount++] = bottomRight;
                }
            }
            
            // Create vertex and index buffers
            _terrainVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColorTexture), _terrainVertices.Length, BufferUsage.WriteOnly);
            _terrainVertexBuffer.SetData(_terrainVertices);
            
            _terrainIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, _terrainIndices.Length, BufferUsage.WriteOnly);
            _terrainIndexBuffer.SetData(_terrainIndices);
        }
        
        private Texture2D CreateCheckerboardTexture(int width, int height, int checkSize, Color color1, Color color2)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isColor1 = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    data[y * width + x] = isColor1 ? color1 : color2;
                }
            }
            
            texture.SetData(data);
            return texture;
        }
        

        
        protected override void UnloadContent()
        {
            // Clean up resources
            base.UnloadContent();
        }
    }
}
