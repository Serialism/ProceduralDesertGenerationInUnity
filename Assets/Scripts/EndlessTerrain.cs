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


    public LODInfo[] detailLevels;//detailLevels为一个存储LODInfo信息的一个数组
    public static float maxViewDst;

    public Transform viewer;//关联gameObject viewer
    public Material mapMaterial;

    public static Vector2 viewerPosition;

    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2,TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2,TerrainChunk>();//实例化一个（坐标位置：TerrainChunk）的字典
    static List<TerrainChunk> TerrainChunksVisibleLastUpdate = new List<TerrainChunk>();//实例化一个名为TerrainChunkVisibleLastUpdate的列表，用来存储TerrainChunk对象

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length -1].visibleDstThreshold;//直接读取detailLevels里存储的最远的visibleDstThreshold
        chunkSize = MapGenerator.mapChunkSize-1;//241-1 = 240
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);//2
        UpdateVisibleChunks();
        //玄学问题，编译后运行第一次会让地块随着viewer平行移动，第二次运行就能完美运行。
        //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/scale;//将viewer的坐标值赋给viewerPosition
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            //如果viewer的坐标上移动的距离大于一个固定的值，则更新UpdateVisibleChunks()
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks(); 
        }
    }
    void UpdateVisibleChunks()
    {
        //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
        //TerranchunksVisibleLastUpdate这个list起先第一帧的时候没有任何值，长度也为0，长度为0时，不会执行下面的for循环直接跳过
        for (int i = 0;i<TerrainChunksVisibleLastUpdate.Count;i++) //列表TerrainChunksVisibleLastUpdate用于存储所有的TerrainChunk
        {
            //Debug.Log(TerrainChunksVisibleLastUpdate.Count);
            TerrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        TerrainChunksVisibleLastUpdate.Clear();
        //删除TerrainChunkVisibleLastUpdate列表里所有的元素
        //遍历上一帧更新出来的可见的所有的块，将之设置为不可见



        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);//当前viewer所在的chunk的坐标
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) //-2,-1,0,1,2
        {
            for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset,currentChunkCoordY + yOffset);
                //把viewer周围的chunk块的单位坐标一一赋值给viewedChunkCoord

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                //检查terrainChunkDictionary这个字典的键里里有没有当前搜索到的这个chunk的单位坐标
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    //terrainChunkDictionary[viewedChunkCoord]是一个TerrainChunk类
                    //UpdateTerrainChunk()函数是terrainChunk类的函数，功能是计算这个类里的玩家距离chunk的距离，然后比较maxViewDst来判定这个chunk可不可见
                    //计算得到能否可见后，直接就在这个函数里设置了这个chunk是否可见
                   
                    //  if (terrainChunkDictionary[viewedChunkCoord].IsVisible()) //如果这个chunk是被激活的，之前也在字典里，那么就在可见的chunk数组里添加这个chunk
                    //  {
                    //      TerrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);//每一帧，都让所有的chunk不可见一下，然后再把周围的chunk可见出来
                    //  }
                }
                else 
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform,mapMaterial));
                    //transform是一个预定义的变量，无需显式声明即可使用，直接访问游戏对象GameObject的Transform组件
                    //如果没有在键位里找到这个玩家周围的chunk的单位坐标，那么就在字典里添加一个（区块的单位坐标，实例化的一个单位平面）
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
            //coord 是玩家周围的一圈chunk的区块，detailLevels是自己在界面上设定的(lod,threshold)信息，parent是mapgenerator的位置，材质是界面上设定的材质
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position,Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);//将创建的meshObject对象设置为一个mesh平面

            meshObject = new GameObject("Terrain Chunk"+coord);
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3*scale;
            //meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one*scale;
            //Vector3.one是一个（1，1，1）的单位向量
            //创建出来的平面meshObject的默认大小是10，所以需要再除以10f
            //transform组件是来自于mapgenerator的transform组件
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            //detailLevels.Length代表detailLevels这个数组里有多少个这个结构体
            //lodMeshes是一个LODMesh实例化出来的存储LODMesh信息的数组，数组长度等于划分出来的LOD级别的级数

            for (int i = 0;i < detailLevels.Length;i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod,UpdateTerrainChunk);
                //赋值lodMeshes[]，每个i对应的值为LODMesh(detailLevels[i].lod)
            }

            mapGenerator.RequestMapData(position,OnMapDataReceived);
            //更新了meshFilter.mesh = meshData.CreateMesh();
        }
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                //bounds.sqrDistance(viewerPosition)的功能是，计算viewerPosition距离这个bounds的距离的平方
                //再开根号，就是计算玩家距离周围生成的这个地块的距离实际是多少
                bool visible = viewerDstFromNearestEdge <= maxViewDst;
                //如果玩家距离这个区块的距离比玩家的视距小，那么visible是1，说明区块是可见的

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                            //如果玩家距离这个区块的距离比lod的某一个门槛大
                            //比如i=0时，visibleDstThreshold是200
                            //如果玩家距离这个区块的距离比200大，那就设置lodIndex为1
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }

                    }
                    if (lodIndex != previousLODIndex)//如果lodIndex没有重复
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];//得到一个LODMesh类，里面的lod是detailLevels[lodindex].lod
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
        //在MapGenerator里的Update()函数中，每一帧都会执行一批次threadInfo.callback(threadInfo.parameter);
        //也就是OnMapDataReceived(mapData)
        //而mapData已经通过mapData = GenerateMapData()在MapGenerator文件的执行过程中生成了
        //该函数功能总结即为，将在MapGenerator里生成的mapData传递过来，赋值给terrainChunk类里头定义的mapData对象
        {
            this.mapData = mapData;
            //this.mapData引用了之前terrainChunk类里头定义的mapData对象，然后在Mapgenerator里把mapData引用过来，赋值给这个对象
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }    
            //mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);


            //在MapGenerator里的Update执行一轮OnMapDataReceived(mapData)，这里的mapdata已经通过MapData mapData = GenerateMapData()生成出来了
            //随后是mapGenerator.RequestMeshData(mapData, OnMeshDataReceived());
        

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();//返回一个Mesh类型的mesh对象，用meshData创建一个mesh
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
            //SetActive（false）的方式是隐藏，让一个chunk消失，不发生任何互动，但是这个chunk的数据，已然留存着，如果什么时候玩家需要回来的时候，可以见到老的地形
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;//布尔值默认为false
        public bool hasMesh;//布尔值默认为false
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
            //callback 是OnMeshDataReceived() 参数是mapdata
        }

        
        
    }
    [System.Serializable]
    public struct LODInfo 
        { 
            public int lod;
            public float visibleDstThreshold;
        }
}
