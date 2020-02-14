using UnityEditor;
using UnityEngine;
public class CEditorTestFracture : EditorWindow
{
    private enum FractureTypes
    {
        Voronoi,
        Clustered,
        Slicing,
        Skinned,
        Plane,
        Cutout
    }

    [MenuItem("Test/Fracture")]
    public static void OpenEditor()
    {
        EditorWindow.GetWindow<CEditorTestFracture>("Fracture");
    }

    private FractureTypes fractureType = FractureTypes.Voronoi;

    public GameObject point;
    public GameObject source;
    public Material insideMaterial;
    public bool islands = false;
    public bool previewColliders = false;
    public float previewDistance = 0.5f;
    public int totalChunks = 5;
    public int seed = 0;

    //TODO: serialize
    //public SlicingConfiguration sliceConf;

    Vector3Int slices = Vector3Int.one;
    float offset_variations = 0;
    float angle_variations = 0;
    float amplitude = 0;
    float frequency = 1;
    int octaveNumber = 1;
    int surfaceResolution = 2;

    public int clusters = 5;
    public int sitesPerCluster = 5;
    public float clusterRadius = 1;

    private void OnEnable()
    {
        point = (GameObject)Resources.Load("Point");
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    protected void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label("PLAY MODE ACTIVE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));//OYM：Play模式屏蔽了
            return;
        }

        GUILayout.Label("OPTIONS", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));
        if (GUILayout.Button("Clean Up Objects")) CleanUp();//OYM：清理Object

        GUILayout.Space(20);
        source = EditorGUILayout.ObjectField("Source", source, typeof(GameObject), true) as GameObject;//OYM：源文件

        if (Selection.activeGameObject != null)
        {
            //hack to not select preview chunks OR Points OR Destructible :)
            if (Selection.activeGameObject.GetComponent<ChunkInfo>() == null && Selection.activeGameObject.hideFlags != HideFlags.NotEditable && Selection.activeGameObject.GetComponent<Destructible>() == null)
            {
                if (Selection.activeGameObject.GetComponent<MeshFilter>() != null) source = Selection.activeGameObject;
                if (Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)//OYM：这个操作还是一如既往的迷啊...

                {
                    source = Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>().gameObject;
                }
            }
        }

        if (!source) return;
        //OYM：没有就不管
        insideMaterial = (Material)EditorGUILayout.ObjectField("Inside Material", insideMaterial, typeof(Material), false);

        if (!insideMaterial) return;

        fractureType = (FractureTypes)EditorGUILayout.EnumPopup("Fracture Type", fractureType);//OYM：获取type

        EditorGUILayout.BeginHorizontal();
        islands = EditorGUILayout.Toggle("Islands", islands);
        previewColliders = EditorGUILayout.Toggle("Preview Colliders", previewColliders);
        EditorGUILayout.EndHorizontal();

        seed = EditorGUILayout.IntSlider("Seed", seed, 0, 25);

        EditorGUI.BeginChangeCheck();
        previewDistance = EditorGUILayout.Slider("Preview", previewDistance, 0, 5);
        //OYM：获取各种属性
        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }

        bool canCreate = false;

        if (fractureType == FractureTypes.Voronoi) canCreate = GUI_Voronoi();//OYM：单纯的裂开
        if (fractureType == FractureTypes.Clustered) canCreate = GUI_Clustered();//OYM：不知道,或许是在某一范围内炸开?
        if (fractureType == FractureTypes.Slicing) canCreate = GUI_Slicing();//OYM：切片
        if (fractureType == FractureTypes.Skinned) canCreate = GUI_Skinned();//OYM：切关节
        if (fractureType == FractureTypes.Plane) canCreate = GUI_Plane();
        if (fractureType == FractureTypes.Cutout) canCreate = GUI_Cutout();

        if (canCreate)//OYM：反正就是只要是这几个界面都可以
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Chunks"))
            {
                _createPreview(false);
            }

            if (GUILayout.Button("Create Prefab"))
            {
                _createPreview(true);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void _createPreview(bool makePrefab)
    {
        float startTime = System.Environment.TickCount;//OYM：计时

        NvBlastExtUnity.setSeed(seed);//OYM：设置中子

        CleanUp();//OYM：清除上一次生成的point与Chunk

        GameObject cs = new GameObject("CHUNKS");//OYM：新建一个chunk出来
        cs.transform.position = Vector3.zero;
        cs.transform.rotation = Quaternion.identity;
        cs.transform.localScale = Vector3.one;

        Mesh ms = null;

        Material[] mats = new Material[2];
        mats[1] = insideMaterial;
        //OYM：似曾相识啊
        MeshFilter mf = source.GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = source.GetComponent<SkinnedMeshRenderer>();

        if (mf != null)
        {
            mats[0] = source.GetComponent<MeshRenderer>().sharedMaterial;
            ms = source.GetComponent<MeshFilter>().sharedMesh;
        }
        if (smr != null)
        {
            mats[0] = smr.sharedMaterial;
            smr.gameObject.transform.position = Vector3.zero;
            smr.gameObject.transform.rotation = Quaternion.identity;
            smr.gameObject.transform.localScale = Vector3.one;
            ms = new Mesh();
            smr.BakeMesh(ms);
            //ms = smr.sharedMesh;
        }

        if (ms == null) return;
        NvMesh mymesh = new NvMesh(ms.vertices, ms.normals, ms.uv, ms.vertexCount, ms.GetIndices(0), (int)ms.GetIndexCount(0));

        //NvMeshCleaner cleaner = new NvMeshCleaner();
        //cleaner.cleanMesh(mymesh);
        //OYM：设置是否为mesh
        NvFractureTool fractureTool = new NvFractureTool();
        fractureTool.setRemoveIslands(islands);
        fractureTool.setSourceMesh(mymesh);

        Debug.Log("sittingTime =" + (System.Environment.TickCount - startTime).ToString());

        if (fractureType == FractureTypes.Voronoi) _Voronoi(fractureTool, mymesh);//OYM：这两个方法差不多快
        if (fractureType == FractureTypes.Clustered) _Clustered(fractureTool, mymesh);
        if (fractureType == FractureTypes.Slicing) _Slicing(fractureTool, mymesh);
        if (fractureType == FractureTypes.Skinned) _Skinned(fractureTool, mymesh);
        if (fractureType == FractureTypes.Plane) _Plane(fractureTool, mymesh);
        if (fractureType == FractureTypes.Cutout) _Cutout(fractureTool, mymesh);

        fractureTool.finalizeFracturing();

        NvLogger.Log("Chunk Count: " + fractureTool.getChunkCount());

        Debug.Log("fractureTime =" + (System.Environment.TickCount - startTime).ToString());

        if (makePrefab)//OYM：创建文件夹(很明显我不需要搞这些)
        {
            if (!AssetDatabase.IsValidFolder("Assets/NvBlast Prefabs")) AssetDatabase.CreateFolder("Assets", "NvBlast Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/NvBlast Prefabs/Meshes")) AssetDatabase.CreateFolder("Assets/NvBlast Prefabs", "Meshes");
            if (!AssetDatabase.IsValidFolder("Assets/NvBlast Prefabs/Fractured")) AssetDatabase.CreateFolder("Assets/NvBlast Prefabs", "Fractured");

            FileUtil.DeleteFileOrDirectory("Assets/NvBlast Prefabs/Meshes/" + source.name);
            AssetDatabase.Refresh();
            AssetDatabase.CreateFolder("Assets/NvBlast Prefabs/Meshes", source.name);
        }
        Debug.Log("GenerateTime=" + (System.Environment.TickCount - startTime).ToString());
        for (int i = 1; i < fractureTool.getChunkCount(); i++)
        {
            GameObject ck = new GameObject("Chunk" + i);
            ck.transform.parent = cs.transform;
            ck.transform.position = Vector3.zero;
            ck.transform.rotation = Quaternion.identity;

            MeshFilter ckmf = ck.AddComponent<MeshFilter>();
            MeshRenderer ckmr = ck.AddComponent<MeshRenderer>();

            ckmr.sharedMaterials = mats;

            NvMesh outside = fractureTool.getChunkMesh(i, false);
            NvMesh inside = fractureTool.getChunkMesh(i, true);

            Mesh m = outside.toUnityMesh();
            m.subMeshCount = 2;
            m.SetIndices(inside.getIndexes(), MeshTopology.Triangles, 1);
            ckmf.sharedMesh = m;

            if (makePrefab)
            {
                AssetDatabase.CreateAsset(m, "Assets/NvBlast Prefabs/Meshes/" + source.name + "/Chunk" + i + ".asset");
            }

            if (!makePrefab) ck.AddComponent<ChunkInfo>();

            if (makePrefab || previewColliders)
            {
                ck.AddComponent<Rigidbody>();
                MeshCollider mc = ck.AddComponent<MeshCollider>();
                mc.inflateMesh = true;
                mc.convex = true;
            }
        }
        Debug.Log("GenerateTimeA=" + (System.Environment.TickCount - startTime).ToString());
        if (makePrefab)
        {
            GameObject p = PrefabUtility.CreatePrefab("Assets/NvBlast Prefabs/Fractured/" + source.name + "_fractured.prefab", cs);

            GameObject fo;

            bool skinnedMesh = false;
            if (source.GetComponent<SkinnedMeshRenderer>() != null) skinnedMesh = true;

            if (skinnedMesh)
                fo = Instantiate(source.transform.root.gameObject);
            else
                fo = Instantiate(source);

            Destructible d = fo.AddComponent<Destructible>();
            d.fracturedPrefab = p;

            bool hasCollider = false;
            if (fo.GetComponent<MeshCollider>() != null) hasCollider = true;
            if (fo.GetComponent<BoxCollider>() != null) hasCollider = true;
            if (fo.GetComponent<SphereCollider>() != null) hasCollider = true;
            if (fo.GetComponent<CapsuleCollider>() != null) hasCollider = true;

            if (!hasCollider)
            {
                BoxCollider bc = fo.AddComponent<BoxCollider>();
                if (skinnedMesh)
                {
                    Bounds b = source.GetComponent<SkinnedMeshRenderer>().bounds;
                    bc.center = new Vector3(0,.5f,0);
                    bc.size = b.size;
                }
            }

            PrefabUtility.CreatePrefab("Assets/NvBlast Prefabs/" + source.name + ".prefab", fo);
            DestroyImmediate(fo);
        }

        cs.transform.rotation = source.transform.rotation;

        UpdatePreview();
        Debug.Log("GenerateTimeB=" + (System.Environment.TickCount - startTime).ToString());
    }

    private void _Cutout(NvFractureTool fractureTool, NvMesh mesh)
    {
    }

    private void _Plane(NvFractureTool fractureTool, NvMesh mesh)
    {
    }

    private void _Skinned(NvFractureTool fractureTool, NvMesh mesh)
    {
        SkinnedMeshRenderer smr = source.GetComponent<SkinnedMeshRenderer>();
        NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(mesh);
        sites.boneSiteGeneration(smr);
        fractureTool.voronoiFracturing(0, sites);
    }

    private void _Slicing(NvFractureTool fractureTool, NvMesh mesh)
    {
        SlicingConfiguration conf = new SlicingConfiguration();
        conf.slices = slices;
        conf.offset_variations = offset_variations;
        conf.angle_variations = angle_variations;

        conf.noise.amplitude = amplitude;
        conf.noise.frequency = frequency;
        conf.noise.octaveNumber = octaveNumber;
        conf.noise.surfaceResolution = surfaceResolution;

        fractureTool.slicing(0, conf, false);
    }

    private void _Clustered(NvFractureTool fractureTool, NvMesh mesh)
    {
        NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(mesh);
        sites.clusteredSitesGeneration(clusters, sitesPerCluster, clusterRadius);
        fractureTool.voronoiFracturing(0, sites);
    }

    private void _Voronoi(NvFractureTool fractureTool, NvMesh mesh)
    {
        NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(mesh);
        sites.uniformlyGenerateSitesInMesh(totalChunks);
        fractureTool.voronoiFracturing(0, sites);
    }

    private bool GUI_Voronoi()
    {
        GUILayout.Space(20);
        GUILayout.Label("VORONOI FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        totalChunks = EditorGUILayout.IntSlider("Total Chunks", totalChunks, 2, 100);
        //OYM：voronoi就这一个属性
        if (GUILayout.Button("Visualize Points"))
        {
            _Visualize();
        }
        return true;
    }

    private bool GUI_Clustered()
    {
        GUILayout.Space(20);
        GUILayout.Label("CLUSTERED VORONOI FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        clusters = EditorGUILayout.IntField("Clusters", clusters);
        sitesPerCluster = EditorGUILayout.IntField("Sites", sitesPerCluster);
        clusterRadius = EditorGUILayout.FloatField("Radius", clusterRadius);
        //OYM：三个属性(也不知道是做啥的)
        if (GUILayout.Button("Visualize Points"))
        {
            _Visualize();
        }

        return true;
    }

    private bool GUI_Skinned()
    {
        GUILayout.Space(20);
        GUILayout.Label("SKINNED MESH VORONOI FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        if (source.GetComponent<SkinnedMeshRenderer>() == null)
        {
            EditorGUILayout.HelpBox("Skinned Mesh Not Selected", MessageType.Error);
            return false;
        }

        if (source.transform.root.position != Vector3.zero)
        {
            EditorGUILayout.HelpBox("Root must be at 0,0,0 for Skinned Meshes", MessageType.Info);
            if (GUILayout.Button("FIX"))
            {
                source.transform.root.position = Vector3.zero;
                source.transform.root.rotation = Quaternion.identity;
                source.transform.root.localScale = Vector3.one;
            }

            return false;
        }

        if (GUILayout.Button("Visualize Points"))
        {
            _Visualize();
        }

        return true;
    }

    private bool GUI_Slicing()
    {
        GUILayout.Space(20);
        GUILayout.Label("SLICING FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        slices = EditorGUILayout.Vector3IntField("Slices", slices);
        offset_variations = EditorGUILayout.Slider("Offset", offset_variations, 0, 1);
        angle_variations = EditorGUILayout.Slider("Angle", angle_variations, 0, 1);

        GUILayout.BeginHorizontal();
        amplitude = EditorGUILayout.FloatField("Amplitude", amplitude);
        frequency = EditorGUILayout.FloatField("Frequency", frequency);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        octaveNumber = EditorGUILayout.IntField("Octave", octaveNumber);
        surfaceResolution = EditorGUILayout.IntField("Resolution", surfaceResolution);
        GUILayout.EndHorizontal();
        //OYM：太长不看
        return true;
    }

    private bool GUI_Plane()
    {
        GUILayout.Space(20);
        GUILayout.Label("PLANE FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        GUILayout.Label("Coming Soon...");
        return false;
    }

    private bool GUI_Cutout()
    {
        GUILayout.Space(20);
        GUILayout.Label("CUTOUT FRACTURE", GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(32));

        GUILayout.Label("Coming Soon...");
        return false;
    }

    private void _Visualize()
    {
        NvBlastExtUnity.setSeed(seed);

        CleanUp();
        if (source == null) return;

        GameObject ps = new GameObject("POINTS");
        ps.transform.position = Vector3.zero;
        ps.transform.rotation = Quaternion.identity;
        ps.transform.localScale = Vector3.one;

        Mesh ms = null;

        MeshFilter mf = source.GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = source.GetComponent<SkinnedMeshRenderer>();

        if (mf != null)
        {
            ms = source.GetComponent<MeshFilter>().sharedMesh;
        }
        if (smr != null)
        {
            smr.gameObject.transform.position = Vector3.zero;
            smr.gameObject.transform.rotation = Quaternion.identity;
            smr.gameObject.transform.localScale = Vector3.one;
            ms = new Mesh();
            smr.BakeMesh(ms);
            //ms = smr.sharedMesh;
            //OYM：储存mesh
        }

        if (ms == null) return;

        NvMesh mymesh = new NvMesh(ms.vertices, ms.normals, ms.uv, ms.vertexCount, ms.GetIndices(0), (int)ms.GetIndexCount(0));//OYM：这里是把mesh丢给一个dll并返回一个intptr

        NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(mymesh);
        //OYM：根据IntPtr获取voronoiSites
        if (fractureType == FractureTypes.Voronoi) sites.uniformlyGenerateSitesInMesh(totalChunks);//OYM：裂开
        if (fractureType == FractureTypes.Clustered) sites.clusteredSitesGeneration(clusters, sitesPerCluster, clusterRadius);//OYM：拥挤
        if (fractureType == FractureTypes.Skinned) sites.boneSiteGeneration(smr);//OYM：骨骼

        Vector3[] vs = sites.getSites();//OYM：获取..我也不知道啥

        for (int i = 0; i < vs.Length; i++)
        {
            GameObject po = Instantiate(point, vs[i], Quaternion.identity, ps.transform);//OYM：把这些点标记出来
            po.hideFlags = HideFlags.NotEditable;//OYM：不可车裂
        }

        ps.transform.rotation = source.transform.rotation;//OYM：把坐标拷走?
        ps.transform.position = source.transform.position;
    }

    private void CleanUp()
    {
        GameObject.DestroyImmediate(GameObject.Find("POINTS"));
        GameObject.DestroyImmediate(GameObject.Find("CHUNKS"));
    }

    private void UpdatePreview()
    {
        GameObject cs = GameObject.Find("CHUNKS");
        if (cs == null) return;

        Transform[] ts = cs.transform.GetComponentsInChildren<Transform>();

        foreach (Transform t in ts)
        {
            ChunkInfo ci = t.GetComponent<ChunkInfo>();
            if (ci != null)
            {
                ci.UpdatePreview(previewDistance);
            }
        }
    }
}