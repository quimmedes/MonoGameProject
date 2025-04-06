using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameProject.Terrain
{
    public class TerrainGenerator
    {
        // Primary terrain noise
        private readonly FastNoiseLite _terrainNoise;
        private readonly FastNoiseLite _detailNoise;
        private readonly FastNoiseLite _moistureNoise;
        private readonly FastNoiseLite _temperatureNoise;
        
        // Terrain parameters
        private readonly int _seed;
        private readonly float _scale;
        private readonly int _octaves;
        private readonly float _persistence;
        private readonly float _lacunarity;
        private readonly Vector2 _offset;
        private readonly float _heightMultiplier;
        private readonly AnimationCurve _heightCurve;
        
        // Biome and feature parameters
        private readonly float _mountainThreshold = 0.65f;
        private readonly float _hillThreshold = 0.45f;
        private readonly float _plainThreshold = 0.25f;
        private readonly float _waterThreshold = 0.18f;
        
        // Cache for heightmaps to avoid recalculating for the same position
        private Dictionary<Vector2, float[,]> _heightMapCache = new Dictionary<Vector2, float[,]>();
        private int _maxCacheSize = 20; // Limit cache size to avoid memory issues

        public TerrainGenerator(int seed, float scale = 150f, int octaves = 8, 
            float persistence = 0.55f, float lacunarity = 2.2f, float heightMultiplier = 150f)
        {
            _seed = seed;
            _scale = scale;
            _octaves = octaves;
            _persistence = persistence;
            _lacunarity = lacunarity;
            _heightMultiplier = heightMultiplier;
            _offset = new Vector2(0, 0);

            // Initialize main terrain noise generator
            _terrainNoise = new FastNoiseLite(seed);
            _terrainNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _terrainNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _terrainNoise.SetFractalOctaves(octaves);
            _terrainNoise.SetFractalLacunarity(lacunarity);
            _terrainNoise.SetFractalGain(persistence);
            
            // Initialize detail noise for small terrain features
            _detailNoise = new FastNoiseLite(seed + 1);
            _detailNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            _detailNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _detailNoise.SetFractalOctaves(octaves + 2); // More detail
            _detailNoise.SetFrequency(0.05f);
            
            // Initialize moisture noise for biome determination
            _moistureNoise = new FastNoiseLite(seed + 2);
            _moistureNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            _moistureNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _moistureNoise.SetFractalOctaves(4);
            _moistureNoise.SetFrequency(0.003f); // Larger features for moisture
            
            // Initialize temperature noise for biome variation
            _temperatureNoise = new FastNoiseLite(seed + 3);
            _temperatureNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            _temperatureNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _temperatureNoise.SetFractalOctaves(3);
            _temperatureNoise.SetFrequency(0.002f); // Even larger features for temperature
            
            // Initialize height curve for more realistic terrain
            _heightCurve = new AnimationCurve();
            _heightCurve.AddKey(0f, 0f);          // Deep ocean
            _heightCurve.AddKey(_waterThreshold - 0.05f, 0.1f);  // Ocean shelf
            _heightCurve.AddKey(_waterThreshold, 0.2f);  // Coastline
            _heightCurve.AddKey(_plainThreshold, 0.3f);  // Plains
            _heightCurve.AddKey(_hillThreshold, 0.5f);   // Hills
            _heightCurve.AddKey(_mountainThreshold, 0.7f); // Mountain base
            _heightCurve.AddKey(0.8f, 0.85f);     // Mountain
            _heightCurve.AddKey(0.9f, 0.95f);     // High mountain
            _heightCurve.AddKey(1f, 1f);          // Peak
        }

        public float[,] GenerateHeightMap(int width, int height, Vector2 chunkPosition)
        {
            // Check if this heightmap is already in the cache
            if (_heightMapCache.TryGetValue(chunkPosition, out float[,] cachedHeightMap))
            {
                return cachedHeightMap;
            }
            
            float[,] heightMap = new float[width, height];
            float[,] moistureMap = new float[width, height];
            float[,] temperatureMap = new float[width, height];
            
            // Generate base terrain, moisture, and temperature maps
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate world positions
                    float worldX = x + chunkPosition.X * (width - 1);
                    float worldY = y + chunkPosition.Y * (height - 1);
                    
                    // Get base terrain height using fractal noise
                    float baseHeight = GenerateTerrainHeight(worldX, worldY);
                    
                    // Add detail noise for small terrain features
                    float detailValue = _detailNoise.GetNoise(worldX * 0.1f, worldY * 0.1f) * 0.1f;
                    baseHeight += detailValue;
                    
                    // Clamp and normalize height to 0-1 range
                    baseHeight = Math.Max(0, Math.Min(1, (baseHeight + 1) * 0.5f));
                    
                    // Apply height curve for more realistic terrain
                    float finalHeight = _heightCurve.Evaluate(baseHeight);
                    
                    // Generate moisture and temperature values for biome determination
                    float moisture = (_moistureNoise.GetNoise(worldX, worldY) + 1) * 0.5f;
                    float temperature = (_temperatureNoise.GetNoise(worldX, worldY) + 1) * 0.5f;
                    
                    // Adjust temperature based on height (higher = colder)
                    temperature = Math.Max(0, temperature - (finalHeight * 0.3f));
                    
                    // Store values
                    heightMap[x, y] = finalHeight;
                    moistureMap[x, y] = moisture;
                    temperatureMap[x, y] = temperature;
                }
            }
            
            // Apply erosion and other terrain features
            ApplyTerrainFeatures(heightMap, moistureMap, temperatureMap, width, height);
            
            // Add to cache if not full
            if (_heightMapCache.Count < _maxCacheSize)
            {
                _heightMapCache[chunkPosition] = heightMap;
            }
            else if (_heightMapCache.Count == _maxCacheSize)
            {
                // Clear cache when it gets too full
                _heightMapCache.Clear();
                _heightMapCache[chunkPosition] = heightMap;
            }

            return heightMap;
        }
        
        private float GenerateTerrainHeight(float x, float y)
        {
            // Use the configured noise to generate the base terrain height
            return _terrainNoise.GetNoise(x / _scale, y / _scale);
        }
        
        private void ApplyTerrainFeatures(float[,] heightMap, float[,] moistureMap, float[,] temperatureMap, int width, int height)
        {
            // Apply simple erosion by smoothing peaks and valleys
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float currentHeight = heightMap[x, y];
                    
                    // Skip water areas
                    if (currentHeight < _waterThreshold)
                        continue;
                    
                    // Calculate average of neighboring heights
                    float avgHeight = (
                        heightMap[x-1, y] + heightMap[x+1, y] + 
                        heightMap[x, y-1] + heightMap[x, y+1]) / 4.0f;
                    
                    // Erode peaks (higher than neighbors)
                    if (currentHeight > avgHeight)
                    {
                        // More erosion on higher slopes
                        float erosionFactor = 0.2f * (currentHeight - avgHeight) / currentHeight;
                        heightMap[x, y] = currentHeight - (currentHeight - avgHeight) * erosionFactor;
                    }
                    
                    // Add sediment to valleys (lower than neighbors)
                    if (currentHeight < avgHeight && currentHeight > _waterThreshold)
                    {
                        float sedimentFactor = 0.1f;
                        heightMap[x, y] = currentHeight + (avgHeight - currentHeight) * sedimentFactor;
                    }
                }
            }
            
            // Create river valleys based on moisture
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // Only create rivers in high moisture areas
                    if (moistureMap[x, y] > 0.7f && heightMap[x, y] > _waterThreshold && heightMap[x, y] < _mountainThreshold)
                    {
                        // Check if this could be a river path (lower than some neighbors)
                        bool hasHigherNeighbor = 
                            heightMap[x-1, y] > heightMap[x, y] || 
                            heightMap[x+1, y] > heightMap[x, y] || 
                            heightMap[x, y-1] > heightMap[x, y] || 
                            heightMap[x, y+1] > heightMap[x, y];
                            
                        if (hasHigherNeighbor && _detailNoise.GetNoise(x * 0.5f, y * 0.5f) > 0.7f)
                        {
                            // Create a river valley by lowering the terrain
                            heightMap[x, y] = Math.Max(_waterThreshold - 0.02f, heightMap[x, y] - 0.1f);
                        }
                    }
                }
            }
        }

        public float GetHeightAt(float x, float z)
        {
            // Calculate world position
            float worldX = x;
            float worldZ = z;
            
            // Get base terrain height
            float baseHeight = GenerateTerrainHeight(worldX, worldZ);
            
            // Add detail noise
            float detailValue = _detailNoise.GetNoise(worldX * 0.1f, worldZ * 0.1f) * 0.1f;
            baseHeight += detailValue;
            
            // Normalize to 0-1 range
            baseHeight = Math.Max(0, Math.Min(1, (baseHeight + 1) * 0.5f));
            
            // Apply height curve
            float finalHeight = _heightCurve.Evaluate(baseHeight);
            
            // Apply height multiplier
            return finalHeight * _heightMultiplier;
        }
        
        // Get biome information at a specific world position
        public BiomeInfo GetBiomeAt(float x, float z)
        {
            // Get height normalized to 0-1
            float height = GetHeightAt(x, z) / _heightMultiplier;
            
            // Get moisture and temperature
            float moisture = (_moistureNoise.GetNoise(x, z) + 1) * 0.5f;
            float temperature = (_temperatureNoise.GetNoise(x, z) + 1) * 0.5f;
            
            // Adjust temperature based on height (higher = colder)
            temperature = Math.Max(0, temperature - (height * 0.3f));
            
            // Determine biome type
            BiomeType biomeType;
            
            if (height < _waterThreshold)
            {
                biomeType = BiomeType.Ocean;
            }
            else if (height < _plainThreshold)
            {
                if (moisture > 0.7f)
                    biomeType = BiomeType.Swamp;
                else if (moisture > 0.4f)
                    biomeType = BiomeType.Forest;
                else
                    biomeType = BiomeType.Plains;
            }
            else if (height < _hillThreshold)
            {
                if (moisture > 0.6f)
                    biomeType = BiomeType.Forest;
                else if (moisture > 0.3f)
                    biomeType = BiomeType.Hills;
                else
                    biomeType = BiomeType.Savanna;
            }
            else if (height < _mountainThreshold)
            {
                if (temperature < 0.3f)
                    biomeType = BiomeType.SnowyConiferousForest;
                else if (moisture > 0.5f)
                    biomeType = BiomeType.ConiferousForest;
                else
                    biomeType = BiomeType.Mountains;
            }
            else
            {
                if (temperature < 0.2f)
                    biomeType = BiomeType.SnowyMountains;
                else
                    biomeType = BiomeType.Mountains;
            }
            
            return new BiomeInfo
            {
                Type = biomeType,
                Height = height,
                Moisture = moisture,
                Temperature = temperature
            };
        }
    }
    
    // Biome types for terrain classification
    public enum BiomeType
    {
        Ocean,
        Plains,
        Forest,
        Hills,
        Mountains,
        SnowyMountains,
        Desert,
        Savanna,
        Swamp,
        ConiferousForest,
        SnowyConiferousForest
    }
    
    // Biome information structure
    public struct BiomeInfo
    {
        public BiomeType Type;
        public float Height;
        public float Moisture;
        public float Temperature;
    }
}
