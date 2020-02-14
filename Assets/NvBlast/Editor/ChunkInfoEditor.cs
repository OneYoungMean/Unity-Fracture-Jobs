using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChunkInfo))]
public class ChunkInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ChunkInfo ci = (ChunkInfo)target;

        MeshFilter mf = ci.gameObject.GetComponent<MeshFilter>();
        MeshRenderer mr = ci.gameObject.GetComponent<MeshRenderer>();

        Bounds b = mr.bounds;

        GUILayout.Label(ci.startBounds.ToString());

        GUILayout.Label(b.ToString());
        GUILayout.Label("Min:" + b.min.ToString());
        GUILayout.Label("Max:" + b.max.ToString());
        GUILayout.Label("Size:" + b.size.ToString());
    }
}