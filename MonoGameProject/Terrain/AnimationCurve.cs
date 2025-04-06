using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoGameProject.Terrain
{
    /// <summary>
    /// A simplified version of Unity's AnimationCurve for interpolation
    /// </summary>
    public class AnimationCurve
    {
        private List<Keyframe> _keys = new List<Keyframe>();

        public AnimationCurve()
        {
        }

        public AnimationCurve(params Keyframe[] keys)
        {
            _keys.AddRange(keys);
            _keys = _keys.OrderBy(k => k.Time).ToList();
        }

        public void AddKey(float time, float value)
        {
            _keys.Add(new Keyframe(time, value));
            _keys = _keys.OrderBy(k => k.Time).ToList();
        }

        public float Evaluate(float time)
        {
            if (_keys.Count == 0)
                return 0;

            if (_keys.Count == 1)
                return _keys[0].Value;

            // Clamp time to the valid range
            time = Math.Clamp(time, _keys[0].Time, _keys[_keys.Count - 1].Time);

            // Find the two keyframes to interpolate between
            for (int i = 0; i < _keys.Count - 1; i++)
            {
                if (time >= _keys[i].Time && time <= _keys[i + 1].Time)
                {
                    float t = (time - _keys[i].Time) / (_keys[i + 1].Time - _keys[i].Time);
                    return Lerp(_keys[i].Value, _keys[i + 1].Value, t);
                }
            }

            // Fallback
            return _keys[_keys.Count - 1].Value;
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }

    public struct Keyframe
    {
        public float Time { get; set; }
        public float Value { get; set; }

        public Keyframe(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }
}
