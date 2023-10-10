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
    public enum DrawMode { NoiseMap,ColourMap,Mesh,VoronoiNoiseMap,DrawVoronoiMesh };//��������ģʽ
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
    //MapThreadInfo<MapData> ��һ�����ͽṹ�壬���з��Ͳ��� T ��ָ��Ϊ MapData��
    //����ζ�� MapThreadInfo �ṹ���ʵ�������������ֶΣ�һ���� callback������Ϊ Action<MapData>����һ���� parameter������Ϊ MapData��
    //����������£�MapThreadInfo �ṹ�����ڴ洢һ���ص�������Action<MapData>���Լ�һ�� MapData ���͵Ĳ�����

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
            //�����callback��EndlessTerrain���OnMapDataReceived����
            //ִ�е������ǣ�mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
            //MapDataThread(OnMapDataReceived())
            //
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
        //����һ���������壬������һ��Action<MapData>���͵Ļص�������Ϊ������
        //����ζ�������Խ�һ������MapData�������ݵĻص��������ݸ����������
        //�˴���callback��OnMapDataReceived()����
    {
        MapData mapData = GenerateVoronoiNoiseMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));
            //mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(OnMapDataReceived(),mapData));
            //�����Update��ִ��һ�����е�OnMapDataReceived(mapData)
        }

    }

    public void RequestMeshData(MapData mapdata,int lod,Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapdata,lod, callback);
        };
        new Thread(threadStart).Start();//�����callback��EndlessTerrain���OnMeshDataReceived()��mapdata�����ɳ�����
    }

    void MeshDataThread(MapData mapData,int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)//����ʵ�ֶ��߳��еĻ��������ȷ���ڶ���߳�ͬʱ����mapDataThreadInfoQueueʱ��ֻ��һ���߳̿���ִ������Ĵ����
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
            //����meshDataThreadQueue�У������Ԫ����MapThreadInfo<MeshData>(callback,meshData)�������callback��EndlessTerrain���OnMeshDataReceived()
            //meshData����MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count>0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                //Deque()�����Ĺ���Ϊ���б����Ƴ�������ͷ��Ԫ��
               
                threadInfo.callback(threadInfo.parameter);
                //threadInfo.callback�Ƿ���thread������Ļص�����
                //threadInfo.parameter�Ƿ���thread������Ĳ����������parameter��һ��MapData���͵�����
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0;i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
                //callback��OnMeshDataReceived()
                //parameter��ͨ��
                //MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
                //���ɳ�����meshData���߱�:
                //public Vector3[] vertices;
                //public int[] triangles;
                //public Vector2[] uvs;
                //int triangleIndex;
                //���ĸ�ֵ
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

                        //����lerp��ֵ��ֱ���ж�currentHeight�Ƿ���趨��height�߶���ָ���������ɫ
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

                        //����lerp��ֵ��ֱ���ж�currentHeight�Ƿ���趨��height�߶���ָ���������ɫ
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
    //����ṹ�����ڷ�װһ���ص�������callback����һ��������parameter�����Ա��Ժ���ĳ���̻߳���������ִ������ص�������
    //�����������ݸ���������ģʽͨ�����ڶ��̱߳�̣������ڲ�ͬ���߳���ִ�лص������������������ݸ���Щ����������ض�������


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


