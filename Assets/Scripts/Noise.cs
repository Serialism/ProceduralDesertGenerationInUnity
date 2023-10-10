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

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;//将Perlin噪声值进行线性映射，从[0，1]映射到[-1，1]
                    noiseHeight += perlinValue*amplitude;//noiseHeight = 老的noiseHeight加上新生成的noiseHeight乘以振幅

                    amplitude *= persistance;//下一轮amplitude衰减的程度
                    frequency *= lacunarity;//下一轮frequency加剧的程度，频率越大，生成的小型地貌越密集
                    //persistance和lacunarity都是小于1的数
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
                   
                    float normalizedHeight = (noiseMap[x, y] + seaLevel) / (maxPossibleHeight / 0.9f);//可以调节海平面
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
