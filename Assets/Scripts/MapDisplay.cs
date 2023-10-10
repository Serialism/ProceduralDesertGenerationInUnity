using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public void DrawTexture(Texture2D texture)
    {
        
        textureRender.sharedMaterial.mainTexture = texture;
        //将做成的纹理应用给这个命名为textureRender的对象
        //在inspector面板中已经将该对象关联给了plane
        textureRender.transform.localScale = new Vector3(texture.width,1,texture.height);
        //设置plane的大小以匹配这个地图的大小
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
