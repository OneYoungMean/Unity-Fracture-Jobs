using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Jobs;

namespace NVBlastECS
{
    using NVBlastECS.Test;
    public class NvController : MonoBehaviour
    {

        private FractureTypes fractureType = FractureTypes.Voronoi;

        public Material insideMaterial;
        public Material outsideMaterial;
        public NvSetting setting;
        public bool enable;

        private bool isInitialize;
        private bool isGenerate;
        private bool isActive;
        private bool isCloseChunkCollider=true;
        private MeshRenderer render;
        private Collider[] collider;
        private NvFractureRead nvFractureRead;
        private NvFracture nvFracture;
        private GameObject chunkCollection;
        private GameObject[] allChunk;
        private MeshCollider[] allChunkCollider;
        private Rigidbody[] allChunkRigidbody;
        private MeshRenderer[] allChunkRender;
        private Vector3[] allChunkPosition;
        private int chunkCount;


        void Start()
        {

            if (gameObject.GetComponent<MeshFilter>() == null)
                return;
            isInitialize = true;
            nvFracture = NvFracture.GetNvFractureInstance();
            collider = GetComponents<Collider>();
            if (outsideMaterial == null)
            {
                render = gameObject.GetComponent<MeshRenderer>();
                outsideMaterial = render.sharedMaterial;
            }
            if (setting == null)
            {
                setting = Resources.Load<NvSetting>("NvSetting/NvFractureSetting");
            }
            if (setting != null)
            {
                nvFractureRead = setting.ToNvFractureRead();
                if (insideMaterial == null)
                {
                    insideMaterial = setting.insideMaterial;
                }
            }

            if (nvFractureRead.chunkCount == 0)
            {
                nvFractureRead = NvFractureRead.GetDefaultFractureRead();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!isGenerate&&isInitialize)
            {
                isGenerate = true;

                Mesh ms = new Mesh();
                MeshFilter mf = gameObject.GetComponent<MeshFilter>();

                if (mf == null)
                {
                    SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (smr == null) return;
                    else
                    {
                        smr.BakeMesh(ms);
                    }
                }
                else
                {
                    ms = mf.sharedMesh;
                }
                if (ms == null)
                    return;

                NvFracture.NvMeshGenerate(ref nvFractureRead, ms.vertices, ms.normals, ms.uv, ms.vertexCount, ms.GetIndices(0), (int)ms.GetIndexCount(0));//OYM：这个方法很迷就是了....传入dll,返回一个带intPtr的类
                nvFracture.EnQueue(nvFractureRead, this);
            }
            if (chunkCollection == null) return;

            if (enable&&!isActive)
            {
                Open();
            }
            else if(!enable&&isActive)
            {
                Close();
            }

        }

        public void Open()
        {
            if (!isCloseChunkCollider)
            {

                    isCloseChunkCollider = !isCloseChunkCollider;
                    for (int i = 1; i < allChunk.Length; i++)
                    {
                        allChunkCollider[i].enabled = true;
                    }
                
            }
            isActive = true;
            chunkCollection.SetActive(true);
            render.enabled = false;
            for (int i = 0; i < collider.Length; i++)
            {
                collider[i].enabled = false;
            }
            for (int i = 1; i < allChunk.Length; i++)
            {
                allChunkRigidbody[i].isKinematic = false;
            }
            for (int i = 1; i < allChunk.Length; i++)
            {
                allChunkRender[i].enabled = true;
            }
        }
        public void Close()
        {
            if (isCloseChunkCollider)
            {
                isCloseChunkCollider = !isCloseChunkCollider;
                for (int i = 1; i < allChunk.Length; i++)
                {
                    allChunkCollider[i].enabled = false;
                }
            }
            if (SleepCheck())
            {
                isActive = false;
                chunkCollection.SetActive(false);
                render.enabled = true;
                for (int i = 1; i < collider.Length; i++)
                {
                    collider[i].enabled = true;
                }
            }
        }
        public void ResetChunk()
        {
            for (int i = 1; i < allChunk.Length; i++)
            {
                allChunk[i].transform.localPosition = allChunkPosition[i];
                allChunk[i].transform.localRotation = Quaternion.identity;
            }
        }
        private  bool SleepCheck()
        {

            bool result = true;
            for (int i = 1; i < allChunk.Length; i++)
            {
                if (allChunkRender[i].enabled)
                {
                    if (allChunk[i].transform.position.y < setting.sleepLine)
                    {
                        allChunk[i].transform.localPosition = allChunkPosition[i];
                        allChunk[i].transform.localRotation = Quaternion.identity;
                        allChunkRender[i].enabled = false;
                        allChunkRigidbody[i].isKinematic = true;

                    }
                    else
                    {
                        result = false;
                    }
                }

            }
            return result;
        }

        public void SetChunkData()
        {
            if (nvFractureRead.nvFractureToolPtr == IntPtr.Zero) return;

            chunkCount = NvFracture._FractureTool_getChunkCount(nvFractureRead.nvFractureToolPtr);

            if (chunkCount <= 1) return;
            allChunk = new GameObject[chunkCount];
            allChunkPosition = new Vector3[chunkCount];
            allChunkCollider = new MeshCollider[chunkCount];
            allChunkRigidbody = new Rigidbody[chunkCount];
            allChunkRender = new MeshRenderer[chunkCount];
            chunkCollection = new GameObject(gameObject.name + " CHUNKS Collection");
            Transform chunkCollectionTransform = chunkCollection.transform;
            chunkCollectionTransform.parent = transform;
            chunkCollectionTransform.localPosition = Vector3.zero;
            chunkCollectionTransform.localRotation = Quaternion.identity;
            chunkCollectionTransform.localScale = Vector3.one;

            var radiusArray = new float[chunkCount];
            for (int j0 = 1; j0 < chunkCount; j0++)//OYM：创建破裂的网格
            {
                GameObject chunk = new GameObject(gameObject.name + " Chunk " + j0);

                allChunk[j0] = chunk;
                chunk.transform.parent = chunkCollectionTransform;
                chunk.transform.localRotation = Quaternion.identity;
                chunk.transform.localScale = Vector3.one;

                MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
                allChunkRender[j0] = chunk.AddComponent<MeshRenderer>();
                allChunkRender[j0].sharedMaterials = new Material[] { outsideMaterial, insideMaterial == null ? outsideMaterial : insideMaterial };

                IntPtr outside = NvFracture._FractureTool_getChunkMesh(nvFractureRead.nvFractureToolPtr, j0, false);
                IntPtr inside = NvFracture._FractureTool_getChunkMesh(nvFractureRead.nvFractureToolPtr, j0, true);

                Vector3[] vertices = NvFracture.getVertices(outside);

                Vector3 average = Vector3.zero;
                for (int k1 = 0; k1 < vertices.Length; k1++)
                {
                    average += vertices[k1];
                }
                average /= vertices.Length;
                allChunkPosition[j0] = average;
                for (int k1 = 0; k1 < vertices.Length; k1++)
                {
                    vertices[k1] -= average;
                }
                chunk.transform.localPosition = average;
                float min = float.MaxValue;
                for (int k1 = 0; k1 < vertices.Length; k1++)
                {
                    float sqrRadius = (vertices[k1] - average).sqrMagnitude;

                    if (sqrRadius < min)
                    {
                        min = sqrRadius;
                    }
                }
                radiusArray[j0] = Mathf.Lerp(0, Mathf.Sqrt(min), setting.viscosity);

                Mesh m = new Mesh
                {
                    subMeshCount = 2,
                    vertices = vertices,
                    normals = NvFracture.getNormals(outside),
                    uv = NvFracture.getUVs(outside),
                };
                m.SetIndices(NvFracture.getIndexes(outside), MeshTopology.Triangles, 0, true);
                m.SetIndices(NvFracture.getIndexes(inside), MeshTopology.Triangles, 1);
                m.RecalculateBounds();
                meshFilter.sharedMesh = m;

                if (setting.generateCollider)
                {
                    allChunkRigidbody[j0] = chunk.AddComponent<Rigidbody>();
                    float volume = m.bounds.size.x * m.bounds.size.y * m.bounds.size.z;
                    allChunkRigidbody[j0].mass = setting.density * volume;
                    allChunkCollider[j0] = chunk.AddComponent<MeshCollider>();
                    allChunkCollider[j0].convex = true;
                }
            }
            for (int j0 = 1; j0 < chunkCount; j0++)
            {
                for (int k1 = j0 + 1; k1 < chunkCount; k1++)
                {
                    float rate = (radiusArray[j0] + radiusArray[k1]) / (allChunkRigidbody[j0].transform.position - allChunkRigidbody[k1].transform.position).magnitude;
                    rate = rate * rate - 1;
                    if (rate > 0)
                    {
                        var joint = allChunkRigidbody[j0].gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = allChunkRigidbody[k1];
                        joint.breakForce = setting.jointBreakForce * rate * allChunkRigidbody[j0].mass * allChunkRigidbody[k1].mass;//OYM：G(m1*m2/R^2)
                        joint.breakTorque = joint.breakForce;
                    }
                }
            }
            chunkCollection.SetActive(false);
        }
    }
}

