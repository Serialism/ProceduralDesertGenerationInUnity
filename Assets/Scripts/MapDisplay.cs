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
        //�����ɵ�����Ӧ�ø��������ΪtextureRender�Ķ���
        //��inspector������Ѿ����ö����������plane
        textureRender.transform.localScale = new Vector3(texture.width,1,texture.height);
        //����plane�Ĵ�С��ƥ�������ͼ�Ĵ�С
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
