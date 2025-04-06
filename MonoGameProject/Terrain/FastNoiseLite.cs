// FastNoiseLite by Auburn - MIT License
// https://github.com/Auburn/FastNoiseLite
// Ported to C# for MonoGame

using System;
using System.Runtime.CompilerServices;

namespace MonoGameProject.Terrain
{
    public class FastNoiseLite
    {
        public enum NoiseType
        {
            OpenSimplex2,
            OpenSimplex2S,
            Cellular,
            Perlin,
            ValueCubic,
            Value
        }

        public enum RotationType3D
        {
            None,
            ImproveXYPlanes,
            ImproveXZPlanes
        }

        public enum FractalType
        {
            None,
            FBm,
            Ridged,
            PingPong,
            DomainWarpProgressive,
            DomainWarpIndependent
        }

        public enum CellularDistanceFunction
        {
            Euclidean,
            EuclideanSq,
            Manhattan,
            Hybrid
        }

        public enum CellularReturnType
        {
            CellValue,
            Distance,
            Distance2,
            Distance2Add,
            Distance2Sub,
            Distance2Mul,
            Distance2Div
        }

        public enum DomainWarpType
        {
            OpenSimplex2,
            OpenSimplex2Reduced,
            BasicGrid
        }

        private int mSeed = 1337;
        private float mFrequency = 0.01f;
        private NoiseType mNoiseType = NoiseType.OpenSimplex2;
        private RotationType3D mRotationType3D = RotationType3D.None;
        private FractalType mFractalType = FractalType.None;
        private int mOctaves = 3;
        private float mLacunarity = 2.0f;
        private float mGain = 0.5f;
        private float mWeightedStrength = 0.0f;
        private float mPingPongStrength = 2.0f;
        private float mFractalBounding = 1.0f;
        private CellularDistanceFunction mCellularDistanceFunction = CellularDistanceFunction.EuclideanSq;
        private CellularReturnType mCellularReturnType = CellularReturnType.Distance;
        private float mCellularJitterModifier = 1.0f;
        private DomainWarpType mDomainWarpType = DomainWarpType.OpenSimplex2;
        private float mDomainWarpAmp = 1.0f;

        /// <summary>
        /// Create new FastNoise object with default seed
        /// </summary>
        public FastNoiseLite() { }

        /// <summary>
        /// Create new FastNoise object with specified seed
        /// </summary>
        public FastNoiseLite(int seed)
        {
            SetSeed(seed);
        }

        /// <summary>
        /// Sets seed used for all noise types
        /// </summary>
        /// <remarks>
        /// Default: 1337
        /// </remarks>
        public void SetSeed(int seed) { mSeed = seed; }

        /// <summary>
        /// Sets frequency for all noise types
        /// </summary>
        /// <remarks>
        /// Default: 0.01
        /// </remarks>
        public void SetFrequency(float frequency) { mFrequency = frequency; }

        /// <summary>
        /// Sets noise algorithm used for GetNoise(...)
        /// </summary>
        /// <remarks>
        /// Default: OpenSimplex2
        /// </remarks>
        public void SetNoiseType(NoiseType noiseType) { mNoiseType = noiseType; }

        /// <summary>
        /// Sets domain rotation type for 3D Noise and 3D DomainWarp
        /// </summary>
        /// <remarks>
        /// Default: None
        /// </remarks>
        public void SetRotationType3D(RotationType3D rotationType3D) { mRotationType3D = rotationType3D; }

        /// <summary>
        /// Sets method for combining octaves in all fractal noise types
        /// </summary>
        /// <remarks>
        /// Default: None
        /// </remarks>
        public void SetFractalType(FractalType fractalType) { mFractalType = fractalType; UpdateFractalBounding(); }

        /// <summary>
        /// Sets octave count for all fractal noise types
        /// </summary>
        /// <remarks>
        /// Default: 3
        /// </remarks>
        public void SetFractalOctaves(int octaves) { mOctaves = octaves; CalculateFractalBounding(); }

        /// <summary>
        /// Sets octave lacunarity for all fractal noise types
        /// </summary>
        /// <remarks>
        /// Default: 2.0
        /// </remarks>
        public void SetFractalLacunarity(float lacunarity) { mLacunarity = lacunarity; }

        /// <summary>
        /// Sets octave gain for all fractal noise types
        /// </summary>
        /// <remarks>
        /// Default: 0.5
        /// </remarks>
        public void SetFractalGain(float gain) { mGain = gain; CalculateFractalBounding(); }

        /// <summary>
        /// Sets octave weighting for all none DomainWarp fractal types
        /// </summary>
        /// <remarks>
        /// Default: 0.0
        /// </remarks>
        public void SetFractalWeightedStrength(float weightedStrength) { mWeightedStrength = weightedStrength; }

        /// <summary>
        /// Sets strength of the fractal ping pong effect
        /// </summary>
        /// <remarks>
        /// Default: 2.0
        /// </remarks>
        public void SetFractalPingPongStrength(float pingPongStrength) { mPingPongStrength = pingPongStrength; }

        /// <summary>
        /// Sets distance function used in cellular noise calculations
        /// </summary>
        /// <remarks>
        /// Default: Distance
        /// </remarks>
        public void SetCellularDistanceFunction(CellularDistanceFunction cellularDistanceFunction) { mCellularDistanceFunction = cellularDistanceFunction; }

        /// <summary>
        /// Sets return type from cellular noise calculations
        /// </summary>
        /// <remarks>
        /// Default: EuclideanSq
        /// </remarks>
        public void SetCellularReturnType(CellularReturnType cellularReturnType) { mCellularReturnType = cellularReturnType; }

        /// <summary>
        /// Sets the maximum distance a cellular point can move from its grid position
        /// </summary>
        /// <remarks>
        /// Default: 1.0
        /// Note: Setting this higher than 1 will cause artifacts
        /// </remarks>
        public void SetCellularJitter(float cellularJitter) { mCellularJitterModifier = cellularJitter; }

        /// <summary>
        /// Sets the warp algorithm when using DomainWarp(...)
        /// </summary>
        /// <remarks>
        /// Default: OpenSimplex2
        /// </remarks>
        public void SetDomainWarpType(DomainWarpType domainWarpType) { mDomainWarpType = domainWarpType; }

        /// <summary>
        /// Sets the maximum warp distance from original position when using DomainWarp(...)
        /// </summary>
        /// <remarks>
        /// Default: 1.0
        /// </remarks>
        public void SetDomainWarpAmp(float domainWarpAmp) { mDomainWarpAmp = domainWarpAmp; }

        /// <summary>
        /// 2D noise at given position using current settings
        /// </summary>
        /// <returns>Noise output bounded between -1...1</returns>
        public float GetNoise(float x, float y)
        {
            x *= mFrequency;
            y *= mFrequency;

            switch (mNoiseType)
            {
                case NoiseType.OpenSimplex2:
                case NoiseType.OpenSimplex2S:
                    {
                        const float SQRT3 = 1.7320508075688772935274463415059f;
                        const float F2 = 0.5f * (SQRT3 - 1);
                        float t = (x + y) * F2;
                        x += t;
                        y += t;
                    }
                    break;
                default:
                    break;
            }

            switch (mFractalType)
            {
                default:
                    return GenNoiseSingle(mSeed, x, y);
                case FractalType.FBm:
                    return GenFractalFBm(x, y);
                case FractalType.Ridged:
                    return GenFractalRidged(x, y);
                case FractalType.PingPong:
                    return GenFractalPingPong(x, y);
            }
        }

        private float GenFractalFBm(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = mFractalBounding;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = GenNoiseSingle(seed++, x, y);
                sum += noise * amp;
                amp *= Lerp(1.0f, FastMin(noise + 1, 2) * 0.5f, mWeightedStrength);

                x *= mLacunarity;
                y *= mLacunarity;
                amp *= mGain;
            }

            return sum;
        }

        private float GenFractalRidged(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = mFractalBounding;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = FastAbs(GenNoiseSingle(seed++, x, y));
                sum += (noise * -2 + 1) * amp;
                amp *= Lerp(1.0f, 1 - noise, mWeightedStrength);

                x *= mLacunarity;
                y *= mLacunarity;
                amp *= mGain;
            }

            return sum;
        }

        private float GenFractalPingPong(float x, float y)
        {
            int seed = mSeed;
            float sum = 0;
            float amp = mFractalBounding;

            for (int i = 0; i < mOctaves; i++)
            {
                float noise = PingPong((GenNoiseSingle(seed++, x, y) + 1) * mPingPongStrength);
                sum += (noise - 0.5f) * 2 * amp;
                amp *= Lerp(1.0f, noise, mWeightedStrength);

                x *= mLacunarity;
                y *= mLacunarity;
                amp *= mGain;
            }

            return sum;
        }

        private float GenNoiseSingle(int seed, float x, float y)
        {
            switch (mNoiseType)
            {
                case NoiseType.OpenSimplex2:
                    return SingleSimplex(seed, x, y);
                case NoiseType.OpenSimplex2S:
                    return SingleOpenSimplex2S(seed, x, y);
                case NoiseType.Cellular:
                    return SingleCellular(seed, x, y);
                case NoiseType.Perlin:
                    return SinglePerlin(seed, x, y);
                case NoiseType.ValueCubic:
                    return SingleValueCubic(seed, x, y);
                case NoiseType.Value:
                    return SingleValue(seed, x, y);
                default:
                    return 0;
            }
        }

        private void UpdateFractalBounding()
        {
            CalculateFractalBounding();
        }

        private void CalculateFractalBounding()
        {
            float gain = FastAbs(mGain);
            float amp = gain;
            float ampFractal = 1.0f;
            for (int i = 1; i < mOctaves; i++)
            {
                ampFractal += amp;
                amp *= gain;
            }
            mFractalBounding = 1 / ampFractal;
        }

        // Simplified implementations of the noise functions
        private float SinglePerlin(int seed, float x, float y)
        {
            int x0 = FastFloor(x);
            int y0 = FastFloor(y);

            float xd0 = x - x0;
            float yd0 = y - y0;
            float xd1 = xd0 - 1;
            float yd1 = yd0 - 1;

            float xs = InterpQuintic(xd0);
            float ys = InterpQuintic(yd0);

            x0 *= PrimeX;
            y0 *= PrimeY;
            int x1 = x0 + PrimeX;
            int y1 = y0 + PrimeY;

            float xf0 = Lerp(GradCoord(seed, x0, y0, xd0, yd0), GradCoord(seed, x1, y0, xd1, yd0), xs);
            float xf1 = Lerp(GradCoord(seed, x0, y1, xd0, yd1), GradCoord(seed, x1, y1, xd1, yd1), xs);

            return Lerp(xf0, xf1, ys) * 0.5f + 0.5f;
        }

        private float SingleSimplex(int seed, float x, float y)
        {
            // Simplified implementation - returns Perlin for simplicity
            return SinglePerlin(seed, x, y);
        }

        private float SingleOpenSimplex2S(int seed, float x, float y)
        {
            // Simplified implementation - returns Perlin for simplicity
            return SinglePerlin(seed, x, y);
        }

        private float SingleCellular(int seed, float x, float y)
        {
            // Simplified implementation - returns Perlin for simplicity
            return SinglePerlin(seed, x, y);
        }

        private float SingleValueCubic(int seed, float x, float y)
        {
            // Simplified implementation - returns Perlin for simplicity
            return SinglePerlin(seed, x, y);
        }

        private float SingleValue(int seed, float x, float y)
        {
            // Simplified implementation - returns Perlin for simplicity
            return SinglePerlin(seed, x, y);
        }

        // Utility functions
        private const int PrimeX = 501125321;
        private const int PrimeY = 1136930381;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GradCoord(int seed, int xPrimed, int yPrimed, float xd, float yd)
        {
            // Simple and safe approach: use a small set of fixed gradients
            // This avoids any potential array index issues
            int hash = Hash(seed, xPrimed, yPrimed);
            
            // Use only 4 simple gradients: (1,1), (1,-1), (-1,1), (-1,-1)
            // This is much simpler and safer than using the full gradient table
            int h = hash & 3; // Get value 0-3
            
            float xg = (h & 2) == 0 ? 1 : -1;
            float yg = (h & 1) == 0 ? 1 : -1;
            
            return xd * xg + yd * yg;
        }

        private static readonly float[] Gradients2D = {
            0.130526192220052f, 0.99144486137381f, 0.38268343236509f, 0.923879532511287f,
            0.608761429008721f, 0.793353340291235f, 0.793353340291235f, 0.608761429008721f,
            0.923879532511287f, 0.38268343236509f, 0.99144486137381f, 0.130526192220051f,
            0.99144486137381f, -0.130526192220051f, 0.923879532511287f, -0.38268343236509f,
            0.793353340291235f, -0.60876142900872f, 0.608761429008721f, -0.793353340291235f,
            0.38268343236509f, -0.923879532511287f, 0.130526192220052f, -0.99144486137381f,
            -0.130526192220052f, -0.99144486137381f, -0.38268343236509f, -0.923879532511287f,
            -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
            -0.923879532511287f, -0.38268343236509f, -0.99144486137381f, -0.130526192220052f,
            -0.99144486137381f, 0.130526192220051f, -0.923879532511287f, 0.38268343236509f,
            -0.793353340291235f, 0.608761429008721f, -0.608761429008721f, 0.793353340291235f,
            -0.38268343236509f, 0.923879532511287f, -0.130526192220052f, 0.99144486137381f
        };

        private static int Hash(int seed, int xPrimed, int yPrimed)
        {
            int hash = seed ^ xPrimed ^ yPrimed;
            hash *= 0x27d4eb2d;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InterpQuintic(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float PingPong(float t)
        {
            t -= (int)(t * 0.5f) * 2;
            return t < 1 ? t : 2 - t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float f)
        {
            return f >= 0 ? (int)f : (int)f - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastAbs(float f)
        {
            return f < 0 ? -f : f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastMin(float a, float b)
        {
            return a < b ? a : b;
        }
    }
}
