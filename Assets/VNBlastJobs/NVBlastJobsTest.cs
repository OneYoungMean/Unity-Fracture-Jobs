using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace NVBlastECS.Test
{
    public enum FractureTypes
    {
        Voronoi,
        Clustered,
        Slicing,
        Skinned
    }

    public unsafe class NvFracture : MonoBehaviour
    {
        private Queue<NvFractureRead> nvFractureReadList;
        private Queue<NvController> nvDebugList;
        private NativeArray<NvFractureRead> nvFractureReadArray;
        private NvFractureGenerate nvFractureGenerate;
        private static NvFracture instance;
        private JobHandle Hjob;
        public static NvFracture GetNvFractureInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("NvFractureInstance");
                instance = go.AddComponent<NvFracture>();
                instance.nvDebugList = new Queue<NvController>();
                instance.nvFractureReadList = new Queue<NvFractureRead>();
                instance.nvFractureGenerate = new NvFractureGenerate();

                DontDestroyOnLoad(go);
            }
            return instance;
        }

        public void Update()
        {

            if (!Hjob.IsCompleted) return;
            if (nvFractureReadArray.IsCreated)
            {
                for (int i = 0; i < nvFractureReadArray.Length; i++)
                {
                    nvDebugList.Dequeue().SetChunkData();
                }
                nvFractureReadArray.Dispose(Hjob);
            }

            if (nvFractureReadList.Count != 0)
            {
                nvFractureReadArray = new NativeArray<NvFractureRead>(nvFractureReadList.ToArray(), Allocator.Persistent);
                nvFractureGenerate.nvFractureReads = (NvFractureRead*)nvFractureReadArray.GetUnsafeReadOnlyPtr();
                Hjob = nvFractureGenerate.Schedule(nvFractureReadArray.Length, 16);
                nvFractureReadList.Clear();
            }
        }

        public void OnDestroy()
        {
            if (nvFractureReadArray.IsCreated && Hjob.IsCompleted)
            {
                nvFractureReadArray.Dispose();
            }
        }
        public void EnQueue(NvFractureRead nvFractureRead, NvController nVDebug)
        {
            nvDebugList.Enqueue(nVDebug);
            nvFractureReadList.Enqueue(nvFractureRead);
        }

        public static void NvMeshGenerate(ref NvFractureRead nvFractureRead, Vector3[] positions, Vector3[] normals, Vector2[] uv, Int32 verticesCount, Int32[] indices, Int32 indicesCount)
        {
            nvFractureRead.nvFractureToolPtr = NvBlastExtAuthoringCreateFractureTool();
            nvFractureRead.nvMeshPtr = NvBlastExtAuthoringCreateMesh(positions, normals, uv, verticesCount, indices, indicesCount);
            _FractureTool_setSourceMesh(nvFractureRead.nvFractureToolPtr, nvFractureRead.nvMeshPtr);
            if (nvFractureRead.fractureType == FractureTypes.Voronoi || nvFractureRead.fractureType == FractureTypes.Clustered)
            {
                nvFractureRead.nvVoronoiSitesGeneratorPtr = _VoronoiSitesGenerator_Create(nvFractureRead.nvMeshPtr);
            }
        }

        public static Vector3[] getVertices(IntPtr nvMeshPtr)
        {
            Vector3[] v = new Vector3[getVerticesCount(nvMeshPtr)];
            _Mesh_getVertices(nvMeshPtr, v);
            return v;
        }
        public static Vector3[] getNormals(IntPtr nvMeshPtr)
        {
            Vector3[] v = new Vector3[getVerticesCount(nvMeshPtr)];
            _Mesh_getNormals(nvMeshPtr, v);
            return v;
        }
        public static Vector2[] getUVs(IntPtr nvMeshPtr)
        {
            Vector2[] v = new Vector2[getVerticesCount(nvMeshPtr)];
            _Mesh_getUVs(nvMeshPtr, v);
            return v;
        }
        public static int[] getIndexes(IntPtr ptr)
        {
            int[] v = new int[getIndexesCount(ptr)];
            _Mesh_getIndexes(ptr, v);
            return v;
        }

        public static int getVerticesCount(IntPtr ptr)
        {
            return _Mesh_getVerticesCount(ptr);
        }

        public static int getIndexesCount(IntPtr ptr)
        {
            return _Mesh_getIndexesCount(ptr);
        }

        //Unity Helper Functions

        #region LowLevel
        [DllImport("NvBlastExtUnity_x64")]
        private static extern void setSeed(int seed);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _Mesh_Release(IntPtr mesh);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _Mesh_getVertices(IntPtr mesh, [In, Out] Vector3[] arr);//OYM：extern表示Dll

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _Mesh_getNormals(IntPtr mesh, [In, Out] Vector3[] arr);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _Mesh_getIndexes(IntPtr mesh, [In, Out] int[] arr);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _Mesh_getUVs(IntPtr mesh, [In, Out] Vector2[] arr);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern int _Mesh_getVerticesCount(IntPtr mesh);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern int _Mesh_getIndexesCount(IntPtr mesh);

        [DllImport("NvBlastExtAuthoring_x64")]
        private static extern IntPtr NvBlastExtAuthoringCreateMesh(Vector3[] positions, Vector3[] normals, Vector2[] uv, Int32 verticesCount, Int32[] indices, Int32 indicesCount);


        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _FractureTool_Release(IntPtr tool);

        [DllImport("NvBlastExtAuthoring_x64")]
        private static extern IntPtr NvBlastExtAuthoringCreateFractureTool();

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _FractureTool_setSourceMesh(IntPtr tool, IntPtr mesh);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _FractureTool_setRemoveIslands(IntPtr tool, bool remove);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern bool _FractureTool_voronoiFracturing(IntPtr tool, int chunkId, IntPtr vsg);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern bool _FractureTool_slicing(IntPtr tool, int chunkId, [Out] SlicingConfiguration conf, bool replaceChunk);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _FractureTool_finalizeFracturing(IntPtr tool);

        [DllImport("NvBlastExtUnity_x64")]
        public static extern int _FractureTool_getChunkCount(IntPtr tool);

        [DllImport("NvBlastExtUnity_x64")]
        public static extern IntPtr _FractureTool_getChunkMesh(IntPtr tool, int chunkId, bool
            inside);


        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _VoronoiSitesGenerator_Release(IntPtr site);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern IntPtr _VoronoiSitesGenerator_Create(IntPtr mesh);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern IntPtr _NvVoronoiSitesGenerator_uniformlyGenerateSitesInMesh(IntPtr tool, int count);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern IntPtr _NvVoronoiSitesGenerator_addSite(IntPtr tool, [In] Vector3 site);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern bool _NvVoronoiSitesGenerator_clusteredSitesGeneration(IntPtr tool, int numberOfClusters, int sitesPerCluster, float clusterRadius);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern int _NvVoronoiSitesGenerator_getSitesCount(IntPtr tool);

        [DllImport("NvBlastExtUnity_x64")]
        private static extern void _NvVoronoiSitesGenerator_getSites(IntPtr tool, [In, Out] Vector3[] arr);
        #endregion
        //Unity Specific
        public static void boneSiteGeneration(IntPtr nvVoronoiSitesGeneratorPtr, SkinnedMeshRenderer smr)
        {
            if (smr == null)
            {
                Debug.Log("No Skinned Mesh Renderer");
                return;
            }

            Animator anim = smr.transform.root.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.Log("Missing Animator");
                return;
            }

            if (anim.GetBoneTransform(HumanBodyBones.LeftHand)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.LeftHand).position);
            if (anim.GetBoneTransform(HumanBodyBones.RightHand)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.RightHand).position);

            if (anim.GetBoneTransform(HumanBodyBones.Chest)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.Chest).position);
            if (anim.GetBoneTransform(HumanBodyBones.Spine)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.Spine).position);
            if (anim.GetBoneTransform(HumanBodyBones.Hips)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.Hips).position);

            if (anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position);
            if (anim.GetBoneTransform(HumanBodyBones.RightUpperLeg)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position);

            if (anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position);
            if (anim.GetBoneTransform(HumanBodyBones.RightLowerLeg)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.RightLowerLeg).position);

            if (anim.GetBoneTransform(HumanBodyBones.LeftFoot)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.LeftFoot).position);
            if (anim.GetBoneTransform(HumanBodyBones.RightFoot)) _NvVoronoiSitesGenerator_addSite(nvVoronoiSitesGeneratorPtr, anim.GetBoneTransform(HumanBodyBones.RightFoot).position);

        }

        public struct NvFractureGenerate : IJobParallelFor
        {
            [ReadOnly, NativeDisableUnsafePtrRestriction]
            internal NvFractureRead* nvFractureReads;
            public void TryExcute(int index, int _ = 0)
            {
                for (int i = 0; i < index; i++)
                {
                    Execute(i);
                }
            }
            public void Execute(int index)
            {
                var nvFractureRead = nvFractureReads + index;
                _FractureTool_setRemoveIslands(nvFractureRead->nvFractureToolPtr, nvFractureRead->islands);
                switch (nvFractureRead->fractureType)
                {
                    case FractureTypes.Voronoi:
                        {
                            _NvVoronoiSitesGenerator_uniformlyGenerateSitesInMesh(nvFractureRead->nvVoronoiSitesGeneratorPtr, nvFractureRead->chunkCount);
                            _FractureTool_voronoiFracturing(nvFractureRead->nvFractureToolPtr, 0, nvFractureRead->nvVoronoiSitesGeneratorPtr);
                            break;
                        }
                    case FractureTypes.Clustered:
                        {
                            _NvVoronoiSitesGenerator_clusteredSitesGeneration(nvFractureRead->nvVoronoiSitesGeneratorPtr, nvFractureRead->clusters, nvFractureRead->sitesPerCluster, nvFractureRead->clusterRadius);
                            _FractureTool_voronoiFracturing(nvFractureRead->nvFractureToolPtr, 0, nvFractureRead->nvVoronoiSitesGeneratorPtr);
                            break;
                        }
                    case FractureTypes.Slicing:
                        {
                            _FractureTool_slicing(nvFractureRead->nvFractureToolPtr, 0, nvFractureRead->slicingConfiguration, false);
                            break;
                        }
                    case FractureTypes.Skinned:
                        {
                            return;
                        }
                }
                _FractureTool_finalizeFracturing(nvFractureRead->nvFractureToolPtr);
            }
        }
    }
}

