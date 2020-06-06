using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public float noiseScale;
    public int octaves;
    public float lacunarity;
    [Range(0,1)]
    public float persistance;
    public int seed;
    public Vector2 offset;
    public Noise.NormalizeMode normalizeMode;

    
    protected override void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        
        base.OnValidate();
    }
}
