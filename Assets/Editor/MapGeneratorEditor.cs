using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI() 
    {
        MapGenerator mapGen = (MapGenerator)target;
        if (DrawDefaultInspector())//DrawDefaultInspector()函数会返回一个bool值，用于指示用户是否更改了inspector面板里的属性
        {
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }
        
        if (GUILayout.Button("Generate")) 
        {
            mapGen.DrawMapInEditor();  
        }
    }
}
