using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameProject.Terrain
{
    public class TerrainManager
    {
        private GraphicsDevice _graphicsDevice;
        private TerrainGenerator _terrainGenerator;
        private Texture2D[] _textures;
        
        private Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
        private Queue<TerrainChunk> _chunksToRemove = new Queue<TerrainChunk>();
        
        private int _chunkSize;
        private int _chunkResolution;
        private float _heightScale;
        private int _viewDistance;
        private Vector2 _lastUpdatePosition;
        
        public TerrainManager(GraphicsDevice graphicsDevice, TerrainGenerator terrainGenerator, 
            Effect terrainEffect, Texture2D[] textures, int chunkSize = 100, int chunkResolution = 65, 
            float heightScale = 50f, int viewDistance = 3)
        {
            _graphicsDevice = graphicsDevice;
            _terrainGenerator = terrainGenerator;
            _textures = textures;
            _chunkSize = chunkSize;
            _chunkResolution = chunkResolution;
            _heightScale = heightScale;
            _viewDistance = viewDistance;
            _lastUpdatePosition = new Vector2(float.MaxValue);
        }

        public void Update(Vector3 cameraPosition)
        {
            // Convert camera position to chunk coordinates
            Vector2 currentChunkCoord = GetChunkCoordFromPosition(cameraPosition);
            
            // Only update chunks if camera has moved to a different chunk
            if (currentChunkCoord != _lastUpdatePosition)
            {
                UpdateVisibleChunks(currentChunkCoord);
                _lastUpdatePosition = currentChunkCoord;
            }
            
            // Remove any chunks that were marked for removal
            while (_chunksToRemove.Count > 0)
            {
                TerrainChunk chunk = _chunksToRemove.Dequeue();
                chunk.Dispose();
            }
        }

        private void UpdateVisibleChunks(Vector2 currentChunkCoord)
        {
            // Mark all chunks as not visible initially
            foreach (var chunk in _chunks.Values)
            {
                chunk.IsVisible = false;
            }
            
            // Determine which chunks should be visible
            for (int xOffset = -_viewDistance; xOffset <= _viewDistance; xOffset++)
            {
                for (int zOffset = -_viewDistance; zOffset <= _viewDistance; zOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(
                        currentChunkCoord.X + xOffset,
                        currentChunkCoord.Y + zOffset);
                    
                    // Skip chunks that are too far (use distance squared for efficiency)
                    float sqrDst = (viewedChunkCoord - currentChunkCoord).LengthSquared();
                    if (sqrDst > _viewDistance * _viewDistance)
                        continue;
                    
                    // If chunk exists, mark it as visible
                    if (_chunks.TryGetValue(viewedChunkCoord, out TerrainChunk chunk))
                    {
                        chunk.IsVisible = true;
                    }
                    else
                    {
                        // Create new chunk
                        chunk = new TerrainChunk(
                            _graphicsDevice,
                            _terrainGenerator,
                            viewedChunkCoord,
                            _chunkSize,
                            _chunkResolution,
                            _heightScale,
                            null, // We're not using custom shader anymore
                            _textures);
                        
                        _chunks.Add(viewedChunkCoord, chunk);
                    }
                }
            }
            
            // Mark chunks for removal if they're too far from the camera
            List<Vector2> chunksToRemove = new List<Vector2>();
            foreach (var entry in _chunks)
            {
                Vector2 coord = entry.Key;
                TerrainChunk chunk = entry.Value;
                
                if (!chunk.IsVisible)
                {
                    float sqrDst = (coord - currentChunkCoord).LengthSquared();
                    if (sqrDst > (_viewDistance + 2) * (_viewDistance + 2))
                    {
                        chunksToRemove.Add(coord);
                        _chunksToRemove.Enqueue(chunk);
                    }
                }
            }
            
            // Remove far chunks from the dictionary
            foreach (var coord in chunksToRemove)
            {
                _chunks.Remove(coord);
            }
        }

        public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition)
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk.IsVisible)
                {
                    chunk.Draw(view, projection, cameraPosition);
                }
            }
        }

        private Vector2 GetChunkCoordFromPosition(Vector3 position)
        {
            int x = (int)Math.Floor(position.X / _chunkSize);
            int z = (int)Math.Floor(position.Z / _chunkSize);
            return new Vector2(x, z);
        }

        public float GetHeightAt(float x, float z)
        {
            return _terrainGenerator.GetHeightAt(x, z);
        }

        public void Dispose()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.Dispose();
            }
            _chunks.Clear();
            
            while (_chunksToRemove.Count > 0)
            {
                _chunksToRemove.Dequeue().Dispose();
            }
        }
    }
}
