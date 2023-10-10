using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Threading;
using static UnityEngine.EventSystems.EventTrigger;
using static Noise;

public class MapGenerator : MonoBehaviour
{
    [Range(0f, 1f)]
    public float seaLevel;

    [Range(0f, 1f)]
    public float randomness;

    public int cellScale = 40;
    public enum DrawMode { NoiseMap,ColourMap,Mesh,VoronoiNoiseMap,DrawVoronoiMesh };//创建绘制模式
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public const int mapChunkSize = 241;

    [Range(0,6)]
    public int editorPreviewLOD;


   
    public float noiseScale;

    public int octaves;
    [Range(0,1)]    
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;
    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue <MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    //MapThreadInfo<MapData> 是一个泛型结构体，其中泛型参数 T 被指定为 MapData。
    //这意味着 MapThreadInfo 结构体的实例将包含两个字段，一个是 callback，类型为 Action<MapData>，另一个是 parameter，类型为 MapData。
    //在这种情况下，MapThreadInfo 结构体用于存储一个回调函数（Action<MapData>）以及一个 MapData 类型的参数。

    public void DrawMapInEditor() 
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();

        
        

        MapData voronoiMapData = GenerateVoronoiNoiseMapData(Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), 
                            TextureGenerator.TextureFromColourMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.VoronoiNoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(voronoiMapData.heightMap));
             
        }
        else if (drawMode == DrawMode.DrawVoronoiMesh)
        {
            
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(voronoiMapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), 
                            TextureGenerator.TextureFromColourMap(voronoiMapData.colorMap, mapChunkSize, mapChunkSize));
            
        }
    }

    public void RequestMapData(Vector2 centre,Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
            //这里的callback是EndlessTerrain里的OnMapDataReceived函数
            //执行的内容是：mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
            //MapDataThread(OnMapDataReceived())
            //
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
        //这是一个方法定义，它接受一个Action<MapData>类型的回调函数作为参数。
        //这意味着您可以将一个处理MapData类型数据的回调函数传递给这个方法。
        //此处的callback是OnMapDataReceived()函数
    {
        MapData mapData = GenerateVoronoiNoiseMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));
            //mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(OnMapDataReceived(),mapData));
            //随后在Update里执行一轮所有的OnMapDataReceived(mapData)
        }

    }

    public void RequestMeshData(MapData mapdata,int lod,Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapdata,lod, callback);
        };
        new Thread(threadStart).Start();//这里的callback是EndlessTerrain里的OnMeshDataReceived()，mapdata是生成出来的
    }

    void MeshDataThread(MapData mapData,int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)//用于实现多线程中的互斥操作，确保在多个线程同时访问mapDataThreadInfoQueue时，只有一个线程可以执行下面的代码块
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
            //生成meshDataThreadQueue列，里面的元素是MapThreadInfo<MeshData>(callback,meshData)，这里的callback是EndlessTerrain里的OnMeshDataReceived()
            //meshData等于MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count>0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                //Deque()函数的功能为从列表中移出并返回头部元素
               
                threadInfo.callback(threadInfo.parameter);
                //threadInfo.callback是访问thread对象里的回调函数
                //threadInfo.parameter是访问thread对象里的参数，这里的parameter是一个MapData类型的数据
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0;i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
                //callback是OnMeshDataReceived()
                //parameter是通过
                //MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
                //生成出来的meshData，具备:
                //public Vector3[] vertices;
                //public int[] triangles;
                //public Vector2[] uvs;
                //int triangleIndex;
                //这四个值
}
        }
    }


    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize,seed, noiseScale,octaves, persistance,lacunarity,centre+offset, normalizeMode,seaLevel);

        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        { 
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x,y];
                //Debug.Log("currentHeight:" + "(" + x + "," + y + ")" + "=" + currentHeight);
                for (int i = 0;i<regions.Length;i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].color;

                        //不用lerp插值，直接判断currentHeight是否过设定的height高度来指定各点的颜色
                    }

                    else 
                    { 
                        break; 
                    }

                }
            }
        }
        return new MapData(noiseMap, colourMap);
    }
    MapData GenerateVoronoiNoiseMapData(Vector2 centre)
    {
        float[,] voronoiNoiseMap = VoronoiNoise.GenerateVoronoiNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, centre + offset, randomness,cellScale,
            octaves, persistance, lacunarity, normalizeMode, seaLevel
            );
        
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = voronoiNoiseMap[x, y];
                //Debug.Log("currentHeight:"+"("+x+","+y+")" + "=" + currentHeight);
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].color;

                        //不用lerp插值，直接判断currentHeight是否过设定的height高度来指定各点的颜色
                    }

                    else
                    {
                        break;
                    }

                }
            }
        }


        
        return new MapData(voronoiNoiseMap, colourMap);
    }

    

    void OnValidate() 
    {
        
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0) 
        { 
            octaves = 0;
        }

    }
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback,T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
    //这个结构体用于封装一个回调方法（callback）和一个参数（parameter），以便稍后在某个线程或上下文中执行这个回调方法，
    //并将参数传递给它。这种模式通常用于多线程编程，可以在不同的线程上执行回调方法，并将参数传递给这些方法以完成特定的任务。


}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public Color color;
    public float height;
    
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


