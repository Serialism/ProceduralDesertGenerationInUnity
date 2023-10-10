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
        //mesh网格的细节精度，mesh节点是一格一格的还是两格两格的，还是四格四格的
        int verticesPerline = (width - 1) / meshsimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerline, verticesPerline);
        int vertexIndex = 0;

        for (int y=0;y < height; y+=meshsimplificationIncrement)
        {
            for (int x=0;x < width; x+=meshsimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightmap[x,y])* heightMultiplier,topLeftZ-y );
                //将地图上各个点的数据压缩存储入meshData.vertices[]的一维数组里，然后vertex++继续存储下一个相邻的点的信息
                //通过两个二分之一的width和height的值，将整张图的中心映射到虚构的坐标轴的原点上
                //heightCurve.Evaluate(heightmap[x,y])* heightMultiplier为该网格点的高度



                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                //UVs（也称为纹理坐标或纹理映射坐标）是一组二维向量，用来指定3D模型上每个顶点对应的纹理坐标。
                //UV坐标决定了纹理如何映射到模型的表面，允许您将纹理贴图精确地放置在模型上。
                //通常，UV坐标的范围是从(0, 0)到(1, 1)，其中(0, 0)表示纹理的左下角，(1, 1)表示纹理的右上角。
                //通过调整UV坐标，您可以控制纹理在模型上的平铺、缩放和旋转等效果。
                
                if (x < width - 1 && y < height - 1)//如果遍历的点搜索到地图边缘上的点了，那么就不用再朝边缘外面去遍历了
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerline + 1, vertexIndex + verticesPerline);
                    meshData.AddTriangle(vertexIndex + verticesPerline + 1,vertexIndex, vertexIndex + 1 );
                    //由于是一维数组，所以与当前vertexIndex相邻的两个点的vertexIndex的值为vertexIndex+1和vertexIndex+w
                    //以vertexIndex为左上角的顶点，可以构造一个正方形，将该正方形分成两个三角形，添加进入mesh.Data
                    //两个三角形的顶点分别是:
                    //（vertexIndex，vertexIndex + width + 1, vertexIndex + width）和(vertexIndex, vertexIndex + 1, vertexIndex + width + 1)
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
        //每个小矩形能分解为两个三角形，而每个三角形需要三个点来确定，因此是乘以6
    }
    public void AddTriangle(int a, int b, int c) 
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex+=3;
    }
    public Mesh CreateMesh()//用UnityEngine里的mesh类来创建一个方法
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();//重新计算网格中的法线
        return mesh;
    }
}

