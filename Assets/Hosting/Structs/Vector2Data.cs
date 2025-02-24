using System;

using UnityEngine;

namespace Hosting.Structs
{
    [Serializable]
    public struct Vector2Data
    {
        public float x;
        public float y;

        public Vector2Data(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        
        public static implicit operator Vector2(Vector2Data b)
        {
            return new Vector2(b.x, b.y).normalized;
        }
    }
}