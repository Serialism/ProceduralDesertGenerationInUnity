using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
public static class Noise
{
    public enum NormalizeMode {Local,Global};
    

    public static float[,] GenerateNoiseMap(
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
        
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];



        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency = 1;
        for (int i = 0; i < octaves; i++) 
        {
            float offsetX = prng.Next(-100000,100000)+offset.x;
            float offsetY = prng.Next(-100000,100000)-offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= frequency;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
            
                for (int i = 0; i < octaves;i++) 
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;//��Perlin����ֵ��������ӳ�䣬��[0��1]ӳ�䵽[-1��1]
                    noiseHeight += perlinValue*amplitude;//noiseHeight = �ϵ�noiseHeight���������ɵ�noiseHeight�������

                    amplitude *= persistance;//��һ��amplitude˥���ĳ̶�
                    frequency *= lacunarity;//��һ��frequency�Ӿ�ĳ̶ȣ�Ƶ��Խ�����ɵ�С�͵�òԽ�ܼ�
                    //persistance��lacunarity����С��1����
                    //Debug.Log(noiseHeight);
                }
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
                
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            //Debug.Log(noiseMap[1, 0]);
            //Debug.Log(noiseMap[100, 200]);
            //Debug.Log(noiseMap[200, 300]);
            //Debug.Log(maxNoiseHeight);
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    //noiseMap[x, y] = 5*Mathf.InverseLerp(0, maxPossibleHeight, noiseMap[x, y]);

                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    //Debug.Log(maxPossibleHeight);
                   
                    float normalizedHeight = (noiseMap[x, y] + seaLevel) / (maxPossibleHeight / 0.9f);//���Ե��ں�ƽ��
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
                }
            }
            
        }
        //Debug.Log(noiseMap[1,0]);
        //Debug.Log(noiseMap[100, 200]);
        //Debug.Log(noiseMap[200, 300]);
        return noiseMap;
    }
}
//Mathf.Approximately(sampleX, 250) && Mathf.Approximately(sampleY, 250)
