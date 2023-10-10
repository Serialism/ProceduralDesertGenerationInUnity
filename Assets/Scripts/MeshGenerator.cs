using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightmap,float heightMultiplier,AnimationCurve _heightCurve,int levelOfDetail) 
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshsimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;
        //mesh�����ϸ�ھ��ȣ�mesh�ڵ���һ��һ��Ļ�����������ģ������ĸ��ĸ��
        int verticesPerline = (width - 1) / meshsimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerline, verticesPerline);
        int vertexIndex = 0;

        for (int y=0;y < height; y+=meshsimplificationIncrement)
        {
            for (int x=0;x < width; x+=meshsimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightmap[x,y])* heightMultiplier,topLeftZ-y );
                //����ͼ�ϸ����������ѹ���洢��meshData.vertices[]��һά�����Ȼ��vertex++�����洢��һ�����ڵĵ����Ϣ
                //ͨ����������֮һ��width��height��ֵ��������ͼ������ӳ�䵽�鹹���������ԭ����
                //heightCurve.Evaluate(heightmap[x,y])* heightMultiplierΪ�������ĸ߶�



                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                //UVs��Ҳ��Ϊ�������������ӳ�����꣩��һ���ά����������ָ��3Dģ����ÿ�������Ӧ���������ꡣ
                //UV����������������ӳ�䵽ģ�͵ı��棬��������������ͼ��ȷ�ط�����ģ���ϡ�
                //ͨ����UV����ķ�Χ�Ǵ�(0, 0)��(1, 1)������(0, 0)��ʾ��������½ǣ�(1, 1)��ʾ��������Ͻǡ�
                //ͨ������UV���꣬�����Կ���������ģ���ϵ�ƽ�̡����ź���ת��Ч����
                
                if (x < width - 1 && y < height - 1)//��������ĵ���������ͼ��Ե�ϵĵ��ˣ���ô�Ͳ����ٳ���Ե����ȥ������
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerline + 1, vertexIndex + verticesPerline);
                    meshData.AddTriangle(vertexIndex + verticesPerline + 1,vertexIndex, vertexIndex + 1 );
                    //������һά���飬�����뵱ǰvertexIndex���ڵ��������vertexIndex��ֵΪvertexIndex+1��vertexIndex+w
                    //��vertexIndexΪ���ϽǵĶ��㣬���Թ���һ�������Σ����������ηֳ����������Σ���ӽ���mesh.Data
                    //���������εĶ���ֱ���:
                    //��vertexIndex��vertexIndex + width + 1, vertexIndex + width����(vertexIndex, vertexIndex + 1, vertexIndex + width + 1)
                }
                vertexIndex++;
            }
        }
        return meshData;
    }
}
public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;  

    public MeshData(int meshWidth, int meshHeight) 
    { 
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        //ÿ��С�����ֽܷ�Ϊ���������Σ���ÿ����������Ҫ��������ȷ��������ǳ���6
    }
    public void AddTriangle(int a, int b, int c) 
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex+=3;
    }
    public Mesh CreateMesh()//��UnityEngine���mesh��������һ������
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();//���¼��������еķ���
        return mesh;
    }
}

