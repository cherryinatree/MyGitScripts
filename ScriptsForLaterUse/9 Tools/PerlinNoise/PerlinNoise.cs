using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GamingTools
{
    public class PerlinNoise
    {
        private float amplitude = 1f;
        private float frequency = 1f;
        private float noiseHeight = 0f;

        public PerlinNoise(float amp, float freq, float noi)
        {
            amplitude = amp;
            frequency = freq;
            noiseHeight = noi;
        }
        public PerlinNoise()
        {
        }

        public float Noise(float width, float height, float maxWidth, float maxHeight, float scale,
            float offsetX, float offsetY, float heightScale, float lacunarity, float persistance, float octaves)
        {
            float y = 0;
            for (int i = 0; i < octaves; i++)
            {
                float xCoord = (width / scale) * frequency + offsetX;
                float yCoord = (height / scale) * frequency + offsetY;

                y = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                noiseHeight += y * amplitude;
                amplitude *= persistance;
                frequency *= lacunarity;

            }

            return y * heightScale;
        }
    }
}
