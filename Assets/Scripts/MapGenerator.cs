using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode{HEIGHT_MAP, COLOR_MAP, MESH, FALLOFF_MAP}
    
    public DrawMode drawMode;
    
    public TerrainData terrainData;
    public NoiseData noiseData;

    public float globalNormalizeModeFactor = 1f;
    [Range(0,6)]
    public int editorPreviewlod;
    
    
    public bool autoUpdate;
    public TerrainType[] regions;
    
    static MapGenerator instance;

    float[,] falloffMap;
    Queue<MapThreadInfo<MapData>> mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFallOffMap(mapChunkSize);
        
    }

    void OnValuesUpdated()
    {
        if(!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    public static int mapChunkSize
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }
            if (instance.terrainData.useFlatShading)
            {
                return 95;
            }
            else{
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenrateMapData(Vector2.zero);
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        switch(drawMode)
        {
            case DrawMode.HEIGHT_MAP:
                mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));

            break;
            case DrawMode.COLOR_MAP:
                mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
            break;
            case DrawMode.MESH:
                mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewlod, terrainData.useFlatShading), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
            break;
            case DrawMode.FALLOFF_MAP:
                mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFallOffMap(mapChunkSize)));
                break;
        }
    }



    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }
    
    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenrateMapData(center);

        lock(mapThreadInfoQueue)
        {
            mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };
        
        new Thread(threadStart).Start();
    }

    public void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock(meshThreadInfoQueue)
        {
            meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if(mapThreadInfoQueue.Count > 0)
        {
            for (int i=0; i < mapThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> mapThreadInfo = mapThreadInfoQueue.Dequeue();
                mapThreadInfo.callback(mapThreadInfo.parameter);
            }
        }

        if(meshThreadInfoQueue.Count > 0)
        {
            for (int i=0; i < meshThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> mapThreadInfo = meshThreadInfoQueue.Dequeue();
                mapThreadInfo.callback(mapThreadInfo.parameter);
            }
        }
    }

    MapData GenrateMapData(Vector2 center)
    {
        float[,] heightMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.lacunarity, noiseData.persistance, center + noiseData.offset, noiseData.normalizeMode, globalNormalizeModeFactor);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for(int y=0; y < mapChunkSize; y++)
        {
            for(int x=0; x<mapChunkSize; x++)
            {
                if (terrainData.useFalloff)
                {
                    heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = heightMap[x, y];
                foreach(TerrainType region in regions)
                {
                    if(currentHeight >= region.height)
                    {
                        colorMap[y * mapChunkSize + x] = region.color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(heightMap, colorMap);
        
    }
    void OnValidate()
    {
        if(terrainData != null)
        {
            terrainData.onValuesUpdated -= OnValuesUpdated;
            terrainData.onValuesUpdated += OnValuesUpdated;
        }
        if(noiseData != null)
        {
            noiseData.onValuesUpdated -= OnValuesUpdated;
            noiseData.onValuesUpdated += OnValuesUpdated;
        }

        falloffMap = FalloffGenerator.GenerateFallOffMap(mapChunkSize);
    }

    public struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public float height;
    public Color color;
    public string name;
    
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap; 
    }
}

