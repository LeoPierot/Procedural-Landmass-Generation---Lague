using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise  
{
    public enum NormalizeMode
    {
        Local, Global
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float lacunarity, float persistance, Vector2 offset, NormalizeMode normalizeMode, float globalNormalizeModeFactor)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0f;

        for(int i = 0; i < octaves; i++)
        {
            float offsetx = prng.Next(-100000, 100000) + offset.x;
            float offsety = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets [i] = new Vector2 (offsetx, offsety);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
            scale = 0.0001f;
        
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth/2;
        float halfHeight = mapHeight/2;

        for (int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = frequency * ((x - halfWidth + octaveOffsets[i].x) / scale) ;
                    float sampleY = frequency * ((y - halfHeight + octaveOffsets[i].y) / scale) ;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;

            }
        }

        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / globalNormalizeModeFactor) ;
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        
        

        return noiseMap;
    } 
}
