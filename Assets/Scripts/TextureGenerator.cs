using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) 
    { 
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;//防止纹理坐标超出范围时出现图像重复或镜像的情况，而是将它们限制在有效的纹理坐标范围内。
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
        //返回用color[]数组做成的纹理对象，无论是黑白地图的数组还是彩色地图的数组
        //彩色地图直接用这个函数直接生成texture，因为已经在Mapgenerator里将数值分两段处理过了
        //黑白地图的数组先用TextureFromHeightMap进行插值再用这个方法生成纹理
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap) //生成黑白色地图的方法
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        //生成一个一维的color数组，大小为二维数组上的所有格子数量
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
                //将二维的数组映射到一维的数组中，一维数组的长度等于2维数组的所有格子数量，扁平化数组的方式可以让它在内存中存储和访问更加高效
                //Lerp是一个插值函数，采用的是线性插值，noisemap[x,y]的值为插值因子，如果插值因子为0，则返回起始颜色，如果插值因子为1，则返回目标颜色

            }
        }
        return TextureFromColourMap(colourMap,width,height);//返回用这块黑白地图做成的纹理对象
        
    }
}
