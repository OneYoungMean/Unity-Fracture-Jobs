using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NVBlastECS.Test
{
    [CustomEditor(typeof(NvSetting))]
    public class NvSettingEditor : Editor
    {
        NvSetting controller;
        public void OnEnable()
        {
            controller = target as NvSetting;
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Titlebar("Fracture Setting", Color.green);
            controller.fractureType = (FractureTypes)EditorGUILayout.EnumPopup("fractureType", controller.fractureType);
            switch (controller.fractureType)
            {
                case FractureTypes.Voronoi:
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkCount"), new GUIContent("ChunkCount"), true);
                    }
                    break;
                case FractureTypes.Clustered:
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("clusters"), new GUIContent("Clusters"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sitesPerCluster"), new GUIContent
("SitesPerCluster"), true);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterRadius"), new GUIContent
("ClusterRadius"), true);
                    }
                    break;
                case FractureTypes.Slicing:
                    {

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("slicingConfiguration"), new GUIContent
("Slicing Configuration"), true);
                    }
                    break;
                default:
                    break;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("insideMaterial"), new GUIContent
("Inside Material"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generateCollider"), new GUIContent
("GenerateCollider"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sleepLine"), new GUIContent
("Sleep Line"), true);
            if (controller.generateCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("generateJoint"), new GUIContent
("Generate Joint"), true);
                if (controller.generateJoint)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jointBreakForce"), new GUIContent
("Joint Break Force"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("density"), new GUIContent
        ("Density"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("viscosity"), new GUIContent
        ("Viscosity"), true);
                }
            }


            serializedObject.ApplyModifiedProperties();
        }

        void Titlebar(string text, Color color)
        {
            GUILayout.Space(12);

            var backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(text);
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = backgroundColor;

            GUILayout.Space(3);
        }
    }
}

    
