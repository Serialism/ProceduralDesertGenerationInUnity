using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using static Noise;


public static class VoronoiNoise
{
    //public static List<Vector2> cellPointGenerate(int cellScale, int mapChunkSize, float randomness)
    //{

    //    //Vector3 cellLocation = new Vector3 ();
    //    Vector2 cellPointCoord = new Vector2();
    //    int cellLength = (int)(mapChunkSize / cellScale);//240/20=12
    //    List<Vector2> cellPointMap = new List<Vector2>();

    //    List<Vector2> pointCoord = new List<Vector2>();

    //    for (int i = -mapChunkSize/2; i < mapChunkSize / 2; i += cellScale)
    //    {
    //        for (int j = -mapChunkSize / 2; j < mapChunkSize / 2; j += cellScale)
    //        {
    //            Vector2 cellLocation = new Vector2(i, j);
    //            cellPointCoord = cellLocation;
    //            cellPointMap.Add(cellPointCoord);
    //        }
    //    }
    //    return cellPointMap;
    //}
    public static float[,] baseNoiseGenerate(
        int mapWidth,
        int mapHeight,
        int seed,
        float scale,
        int octaves,
        float persistance,
        float lacunarity,
        Vector2 offset,
        NormalizeMode normalizeMode,
        float seaLevel
        ) 
    {
        float[,] baseNoise = Noise.GenerateNoiseMap(
        mapWidth,
        mapHeight,
        seed,
        scale,
        octaves,
        persistance,
        lacunarity,
        offset,
        normalizeMode,
        seaLevel
        );

        return baseNoise;

    }








    public static Vector2 CalculatePointRegion(int cellScale,Vector2 cood)
    {
        Vector2 regionCoord = new Vector2();
        regionCoord.x = (int)Math.Floor(cood.x / cellScale);
        regionCoord.y = (int)Math.Floor(cood.y / cellScale);
        return regionCoord;
    }

    public static Vector2 cellPointDisturbance(Vector2 regionCoord,int cellScale)
    {
        Vector2 disturbanceCoord = new Vector2();

        int a = (int)regionCoord.x;
        int b = (int)regionCoord.y;

        //int seed1 = (int)((a * b - 2.5) / (a + b + 1) + a + b);
        //int seed2 = (int)((a * b + 2.5) / (a - b - 1) + a - b);

        int seed1 = a * 10 + b - 2;
        int seed2 = b * 240 + a + 1;

        System.Random random1 = new System.Random(seed1);
        System.Random random2 = new System.Random(seed2);

        float randomValue1 = cellScale * (float)random1.NextDouble();
        float randomValue2 = cellScale * (float)random2.NextDouble();

        disturbanceCoord.x = randomValue1+cellScale*regionCoord.x;
        disturbanceCoord.y = randomValue2+cellScale*regionCoord.y;

        return disturbanceCoord;
    }





    public static float[,] GenerateVoronoiNoiseMap(int mapWidth, int mapHeight, int seed, float scale, Vector2 offset, float randomness,int cellScale,
        int octaves,float persistance,float lacunarity, NormalizeMode normalizeMode, float seaLevel
        )
    {
        float[,] baseNoise = baseNoiseGenerate(
        mapWidth,mapHeight,
        seed,
        80,   //noiseScale
        4,  //octaves
        0.5f,  //persistance
        1,  //lacunarity
        offset,
        normalizeMode,
        1   //sealevel
        );


        //返回的是一个float[，]类型的数据
        float[,] VoronoiNoiseMap = new float[mapWidth, mapHeight];
        Vector2 regionCoord = new Vector2();
        Vector2 pointCoord = new Vector2();
        Vector2 disturbanceCoord = new Vector2();
        List<Vector2> nineNoisePointMap = new List<Vector2>();
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float value = baseNoise[x, y] * 140;
                //在此处生成干扰

                int a = x;
                int b = y;

                int seed3 = 10 * a + b + 1;
                int seed4 = 240 * a + b + 2;

                System.Random random3 = new System.Random(seed3);
                System.Random random4 = new System.Random(seed4);

                float randomValue3 = (float)random3.NextDouble();
                float randomValue4 = (float)random4.NextDouble();

                float heightValue = 20*baseNoise[x, y];

                Vector3 vec3 = new Vector3();
                vec3.x = x + offset.x + (int)30 * (Mathf.PerlinNoise((x + 0.1f * randomValue3) / 40, 0) * 2 - 1);
                // +(int)30*(Mathf.PerlinNoise((x + 0.1f*randomValue3)/40, 0) * 2 - 1);
                //vec3.y = (int)20*(Mathf.PerlinNoise((y + randomValue4)/40, 0) * 2 - 1);

                //vec3.x = x;
                vec3.y = y - offset.y;

                vec3.z = heightValue;

                
                nineNoisePointMap.Clear();
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        pointCoord.x = vec3.x + cellScale * i;
                        pointCoord.y = vec3.y + cellScale * j;
                        regionCoord = CalculatePointRegion(cellScale, pointCoord);
                        disturbanceCoord = cellPointDisturbance(regionCoord, cellScale);
                        //Debug.Log("九宫格噪声数据打印" + ((i + 1) * 3 + (j + 2)) + ", " + "(" + x + ", " + y + "), (" + regionCoord.x + ", " + regionCoord.y + "), (" + disturbanceCoord.x + ", " + disturbanceCoord.y + ")");
                        nineNoisePointMap.Add(disturbanceCoord);
                        //生成新的一张九宫格里的九个noise，存入数组
                    }
                }
                VoronoiNoiseMap[x, y] = CalculateMinDistanceFromPointToNineNoisePoint3D(nineNoisePointMap, vec3) / (cellScale);
                //var result = CalculateMinDistanceFromPointToNineNoisePoint2D(nineNoisePointMap, x, y);
                //VoronoiNoiseMap[x, y] = result.Item1 / 20;
                //Vector2 nearestNoisePointCoord = result.Item2;

                //Vector3 basePointNoise = new Vector3();

                //float xOffset = (float)Math.Cos(seed1/2);
                //float yOffset = (float)Math.Sin(seed2/2);

                //basePointNoise.x = x;
                //basePointNoise.y = y;

                //basePointNoise.x = x;
                //basePointNoise.y = y;
                //basePointNoise.z = baseNoise[x, y]*140;

                //VoronoiNoiseMap[x, y] = CalculateMinDistanceFromPointToNineNoisePoint3D(nineNoisePointMap, basePointNoise)/cellScale;

                //Debug.Log("当前这个点四周的九宫格为：" + Environment.NewLine
                //                              + nineNoisePointMap[6] + "," + nineNoisePointMap[7] + "," + nineNoisePointMap[8] + Environment.NewLine
                //                              + nineNoisePointMap[3] + "," + nineNoisePointMap[4] + "," + nineNoisePointMap[5] + Environment.NewLine
                //                              + nineNoisePointMap[0] + "," + nineNoisePointMap[1] + "," + nineNoisePointMap[2] + Environment.NewLine
                //         + "nineNOiseMap数组的数据数量为：" + nineNoisePointMap.Count + Environment.NewLine
                //         + "当前这个点的坐标为：" + "("+x+", "+y+")" + Environment.NewLine 
                //         + "当前这个点的最小距离是：" + result.Item1);

                //Debug.Log(VoronoiNoiseMap[x, y] + "(" +x + "," + y + ")" + ","+ nearestNoisePointCoord);
            }
        }

        //List<Vector2> cellPointMap = cellPointGenerate(cellScale, mapHeight, randomness);
        //Debug.Log(cellPointMap.Count);
        //for (int i = 0; i < cellPointMap.Count; i++)
        //{

        //}

        return VoronoiNoiseMap;
    }

    static float CalculateMinDistanceFromPointToNineNoisePoint3D(List<Vector2> nineNoisePointMap,Vector3 basePointNoise)
    {
        float minDistance = 8000;
        //Vector3 pointCoord = new Vector3();
        for (int i = 0; i < nineNoisePointMap.Count; i++)
        {
            Vector3 cellPoint3D = new Vector3();
            cellPoint3D.x = nineNoisePointMap[i].x;
            cellPoint3D.y = nineNoisePointMap[i].y;
            cellPoint3D.z = 0;
            float distanceToPOint = EuclideanDistance3D(cellPoint3D, basePointNoise);
        
            if (distanceToPOint < minDistance)
            {
                minDistance = distanceToPOint;

            }
        }

        return minDistance;

    }
    static (float,Vector2) CalculateMinDistanceFromPointToNineNoisePoint2D(List<Vector2> nineNoisePointMap,int x,int y)
    {
        float minDistance = 800;
        Vector2 pointCoord = new Vector2();
        Vector2 disturbanceCoord = new Vector2();
        pointCoord.x = x;
        pointCoord.y = y;
        for (int i = 0;i < nineNoisePointMap.Count; i++)
        {
            float distanceToPOint = EuclideanDistance2D(nineNoisePointMap[i], pointCoord);
            if (distanceToPOint < minDistance) 
            { 
                minDistance = distanceToPOint;
                disturbanceCoord = nineNoisePointMap[i];
            }
        }
        return (minDistance,disturbanceCoord);
    }

  //——————————————————————————————————————2维和3维的点距离算法——————————————————————————————————

    static float EuclideanDistance3D(Vector3 Coord1, Vector3 Coord2)
    {

        float deltaX = Coord1.x - Coord2.x;
        float deltaY = Coord1.y - Coord2.y;
        float deltaZ = Coord1.z - Coord2.z;

        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

    }
    static float EuclideanDistance2D(Vector2 Coord1, Vector2 Coord2)
    {
        float deltaX = Coord1.x - Coord2.x;
        float deltaY = Coord1.y - Coord2.y;
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);


    }

}
//——————————————————————————————————之前的GenerateVoronoiNoiseMap代码——————————————————————————————

//public static float[,] GenerateVoronoiNoiseMap(int mapWidth, int mapHeight, int seed, float scale, Vector2 offset, float randomness, int cellScale)
//{

//    float[,] VoronoiNoiseMap = new float[mapWidth, mapHeight];

//    float[,] preNoiseMap = GeneratePreNoiseMap(mapWidth, mapHeight);

//    List<Vector3> cellPointMap = cellPointGenerate(cellScale, mapWidth, randomness);

//    for (int y = 0; y < mapHeight; y++)
//    {
//        for (int x = 0; x < mapWidth; x++)
//        {

//            Vector3 preNoiseMapCoord = new Vector3(x, y, preNoiseMap[x, y]);
//            float distance = CalculateMinDistance(preNoiseMapCoord, scale, randomness, cellPointMap);
//            VoronoiNoiseMap[x, y] = distance;
//            Debug.Log(VoronoiNoiseMap[x, y] + distance);
//        }
//    }

//    return VoronoiNoiseMap;
//}