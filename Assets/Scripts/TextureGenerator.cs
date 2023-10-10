using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) 
    { 
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;//��ֹ�������곬����Χʱ����ͼ���ظ��������������ǽ�������������Ч���������귶Χ�ڡ�
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
        //������color[]�������ɵ�������������Ǻڰ׵�ͼ�����黹�ǲ�ɫ��ͼ������
        //��ɫ��ͼֱ�����������ֱ������texture����Ϊ�Ѿ���Mapgenerator�ｫ��ֵ�����δ������
        //�ڰ׵�ͼ����������TextureFromHeightMap���в�ֵ�������������������
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap) //���ɺڰ�ɫ��ͼ�ķ���
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        //����һ��һά��color���飬��СΪ��ά�����ϵ����и�������
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
                //����ά������ӳ�䵽һά�������У�һά����ĳ��ȵ���2ά��������и�����������ƽ������ķ�ʽ�����������ڴ��д洢�ͷ��ʸ��Ӹ�Ч
                //Lerp��һ����ֵ���������õ������Բ�ֵ��noisemap[x,y]��ֵΪ��ֵ���ӣ������ֵ����Ϊ0���򷵻���ʼ��ɫ�������ֵ����Ϊ1���򷵻�Ŀ����ɫ

            }
        }
        return TextureFromColourMap(colourMap,width,height);//���������ڰ׵�ͼ���ɵ��������
        
    }
}
