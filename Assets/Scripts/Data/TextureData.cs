using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public Color[] baseColours;
    [Range(0, 1)]
    public float[] baseStartHeights;
    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);

        material.SetInt("baseColorCount", baseColours.Length);
        material.SetColorArray("baseColours", baseColours);
        material.SetFloatArray("baseStartHeights", baseStartHeights);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }
}
