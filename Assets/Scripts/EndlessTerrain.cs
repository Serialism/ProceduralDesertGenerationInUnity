using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


public class EndlessTerrain : MonoBehaviour
{
    const float scale = 1f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;


    public LODInfo[] detailLevels;//detailLevelsΪһ���洢LODInfo��Ϣ��һ������
    public static float maxViewDst;

    public Transform viewer;//����gameObject viewer
    public Material mapMaterial;

    public static Vector2 viewerPosition;

    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2,TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2,TerrainChunk>();//ʵ����һ��������λ�ã�TerrainChunk�����ֵ�
    static List<TerrainChunk> TerrainChunksVisibleLastUpdate = new List<TerrainChunk>();//ʵ����һ����ΪTerrainChunkVisibleLastUpdate���б������洢TerrainChunk����

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length -1].visibleDstThreshold;//ֱ�Ӷ�ȡdetailLevels��洢����Զ��visibleDstThreshold
        chunkSize = MapGenerator.mapChunkSize-1;//241-1 = 240
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);//2
        UpdateVisibleChunks();
        //��ѧ���⣬��������е�һ�λ��õؿ�����viewerƽ���ƶ����ڶ������о����������С�
        //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/scale;//��viewer������ֵ����viewerPosition
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            //���viewer���������ƶ��ľ������һ���̶���ֵ�������UpdateVisibleChunks()
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks(); 
        }
    }
    void UpdateVisibleChunks()
    {
        //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
        //TerranchunksVisibleLastUpdate���list���ȵ�һ֡��ʱ��û���κ�ֵ������ҲΪ0������Ϊ0ʱ������ִ�������forѭ��ֱ������
        for (int i = 0;i<TerrainChunksVisibleLastUpdate.Count;i++) //�б�TerrainChunksVisibleLastUpdate���ڴ洢���е�TerrainChunk
        {
            //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
            TerrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        TerrainChunksVisibleLastUpdate.Clear();
        //ɾ��TerrainChunkVisibleLastUpdate�б������е�Ԫ��
        //������һ֡���³����Ŀɼ������еĿ飬��֮����Ϊ���ɼ�



        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);//��ǰviewer���ڵ�chunk������
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) //-2,-1,0,1,2
        {
            for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset,currentChunkCoordY + yOffset);
                //��viewer��Χ��chunk��ĵ�λ����һһ��ֵ��viewedChunkCoord

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                //���terrainChunkDictionary����ֵ�ļ�������û�е�ǰ�����������chunk�ĵ�λ����
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    //terrainChunkDictionary[viewedChunkCoord]��һ��TerrainChunk��
                    //UpdateTerrainChunk()������terrainChunk��ĺ����������Ǽ�������������Ҿ���chunk�ľ��룬Ȼ��Ƚ�maxViewDst���ж����chunk�ɲ��ɼ�
                    //����õ��ܷ�ɼ���ֱ�Ӿ���������������������chunk�Ƿ�ɼ�
                   
                    //  if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) //������chunk�Ǳ�����ģ�֮ǰҲ���ֵ����ô���ڿɼ���chunk������������chunk
                    //  {
                    //      TerrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);//ÿһ֡���������е�chunk���ɼ�һ�£�Ȼ���ٰ���Χ��chunk�ɼ�����
                    //  }
                }
                else 
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform,mapMaterial));
                    //transform��һ��Ԥ����ı�����������ʽ��������ʹ�ã�ֱ�ӷ�����Ϸ����GameObject��Transform���
                    //���û���ڼ�λ���ҵ���������Χ��chunk�ĵ�λ���꣬��ô�����ֵ������һ��������ĵ�λ���꣬ʵ������һ����λƽ�棩
                }
            }
        }
        
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord,int size, LODInfo[] detailLevels,Transform parent,Material material)
            //coord �������Χ��һȦchunk�����飬detailLevels���Լ��ڽ������趨��(lod,threshold)��Ϣ��parent��mapgenerator��λ�ã������ǽ������趨�Ĳ���
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position,Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);//��������meshObject��������Ϊһ��meshƽ��

            meshObject = new GameObject("Terrain Chunk"+coord);
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3*scale;
            //meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one*scale;
            //Vector3.one��һ����1��1��1���ĵ�λ����
            //����������ƽ��meshObject��Ĭ�ϴ�С��10��������Ҫ�ٳ���10f
            //transform�����������mapgenerator��transform���
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            //detailLevels.Length����detailLevels����������ж��ٸ�����ṹ��
            //lodMeshes��һ��LODMeshʵ���������Ĵ洢LODMesh��Ϣ�����飬���鳤�ȵ��ڻ��ֳ�����LOD����ļ���

            for (int i = 0;i < detailLevels.Length;i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod,UpdateTerrainChunk);
                //��ֵlodMeshes[]��ÿ��i��Ӧ��ֵΪLODMesh(detailLevels[i].lod)
            }

            mapGenerator.RequestMapData(position,OnMapDataReceived);
            //������meshFilter.mesh = meshData.CreateMesh();
        }
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                //bounds.sqrDistance(viewerPosition)�Ĺ����ǣ�����viewerPosition�������bounds�ľ����ƽ��
                //�ٿ����ţ����Ǽ�����Ҿ�����Χ���ɵ�����ؿ�ľ���ʵ���Ƕ���
                bool visible = viewerDstFromNearestEdge <= maxViewDst;
                //�����Ҿ����������ľ������ҵ��Ӿ�С����ôvisible��1��˵�������ǿɼ���

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                            //�����Ҿ����������ľ����lod��ĳһ���ż���
                            //����i=0ʱ��visibleDstThreshold��200
                            //�����Ҿ����������ľ����200���Ǿ�����lodIndexΪ1
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }

                    }
                    if (lodIndex != previousLODIndex)//���lodIndexû���ظ�
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];//�õ�һ��LODMesh�࣬�����lod��detailLevels[lodindex].lod
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    TerrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }

            
        }

        void OnMapDataReceived(MapData mapData)
        //��MapGenerator���Update()�����У�ÿһ֡����ִ��һ����threadInfo.callback(threadInfo.parameter);
        //Ҳ����OnMapDataReceived(mapData)
        //��mapData�Ѿ�ͨ��mapData = GenerateMapData()��MapGenerator�ļ���ִ�й�����������
        //�ú��������ܽἴΪ������MapGenerator�����ɵ�mapData���ݹ�������ֵ��terrainChunk����ͷ�����mapData����
        {
            this.mapData = mapData;
            //this.mapData������֮ǰterrainChunk����ͷ�����mapData����Ȼ����Mapgenerator���mapData���ù�������ֵ���������
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }    
            //mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);


            //��MapGenerator���Updateִ��һ��OnMapDataReceived(mapData)�������mapdata�Ѿ�ͨ��MapData mapData = GenerateMapData()���ɳ�����
            //�����mapGenerator.RequestMeshData(mapData, OnMeshDataReceived());
        

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();//����һ��Mesh���͵�mesh������meshData����һ��mesh
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
            //SetActive��false���ķ�ʽ�����أ���һ��chunk��ʧ���������κλ������������chunk�����ݣ���Ȼ�����ţ����ʲôʱ�������Ҫ������ʱ�򣬿��Լ����ϵĵ���
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;//����ֵĬ��Ϊfalse
        public bool hasMesh;//����ֵĬ��Ϊfalse
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,lod, OnMeshDataReceived);
            //callback ��OnMeshDataReceived() ������mapdata
        }

        
        
    }
    [System.Serializable]
    public struct LODInfo 
        { 
            public int lod;
            public float visibleDstThreshold;
        }
}
