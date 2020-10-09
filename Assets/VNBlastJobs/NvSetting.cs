using NVBlastECS.Test;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NVBlastECS.Test
{
    [CreateAssetMenu(fileName ="NvFractureSetting",menuName ="NvFracture")]
    public class NvSetting : ScriptableObject
    {
        public FractureTypes fractureType;

        public Vector3Int slices;

        public SlicingConfiguration slicingConfiguration;

        public int chunkCount;

        public int clusters;
        public int sitesPerCluster;
        public float clusterRadius;
        public float sleepLine=-2;
        public bool generateCollider=true;
        public bool generateJoint=true;
        public float jointBreakForce=20;
        public float density=4;
        [Range(0,2)]
        public float viscosity=1;

        public bool islands;

        public Material insideMaterial;
        public NvFractureRead ToNvFractureRead()
        {
            return new NvFractureRead
            {
                fractureType = fractureType,
                slices = slices,
                slicingConfiguration = slicingConfiguration,
                chunkCount = chunkCount,
                clusters = clusters,
                sitesPerCluster = sitesPerCluster,
                clusterRadius = clusterRadius,
                islands = islands,
            };

        }
    }
    [System.Serializable]
    public struct NoiseConfiguration
    {
        [Range(0, 1)]
        public float amplitude;//0 - disabled
        [Range(0, 1)]
        public float frequency;//:1
        public int octaveNumber;//:1
        public int surfaceResolution;//:1
    }
    [System.Serializable]
    public struct SlicingConfiguration
    {
        public Vector3Int slices;
        [Range(0, 1)]
        public float offset_variations;//0-1:0
        [Range(0, 1)]
        public float angle_variations;//0-1:0
        public NoiseConfiguration noise;
    }

    public struct NvFractureRead
    {
        public FractureTypes fractureType;
        public Vector3Int slices;

        public SlicingConfiguration slicingConfiguration;

        public int chunkCount;

        public int clusters;
        public int sitesPerCluster;
        public float clusterRadius;

        public bool islands;
        internal IntPtr nvMeshPtr;
        internal IntPtr nvFractureToolPtr;
        internal IntPtr nvVoronoiSitesGeneratorPtr;

        public static NvFractureRead GetDefaultFractureRead()
        {
            return new NvFractureRead
            {
                fractureType = FractureTypes.Voronoi,
                slices = Vector3Int.zero,
                slicingConfiguration = new SlicingConfiguration
                {
                    offset_variations = 0,
                    angle_variations = 0,
                    noise = new NoiseConfiguration
                    {
                        amplitude = 0,
                        frequency = 1,
                        octaveNumber = 1,
                        surfaceResolution = 2,
                    }
                },
                chunkCount = 25,

                clusters = 5,
                sitesPerCluster = 5,
                clusterRadius = 1,

                islands = false,

                nvMeshPtr = IntPtr.Zero,
                nvFractureToolPtr = IntPtr.Zero,
                nvVoronoiSitesGeneratorPtr = IntPtr.Zero,

            };
        }
    }
}

