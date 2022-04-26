using UnityEngine;
using UnityEngine.Profiling;

public class GridValuePerLevel
{
    public RenderTexture RegularCellValue { get { return m_RegularCellValue; } }
    public RenderTexture TallCellTopValue { get { return m_TallCellTopValue; } }
    public RenderTexture TallCellBottomValue { get { return m_TallCellBottomValue; } }

    public GridValuePerLevel(Vector2Int vResolutionXZ, int vRegularCellYCount, RenderTextureFormat vDataType)
    {
        m_RegularCellValue = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, vDataType)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellTopValue = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellBottomValue = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~GridValuePerLevel()
    {
        m_RegularCellValue.Release();
        m_TallCellTopValue.Release();
        m_TallCellBottomValue.Release();
    }

    private RenderTexture m_RegularCellValue;
    private RenderTexture m_TallCellTopValue;
    private RenderTexture m_TallCellBottomValue;
}

public class GridPerLevel
{
    public RenderTexture TerrainHeight { get { return m_TerrrianHeight; } }
    public RenderTexture TallCellHeight {  get { return m_TallCellHeight; } set {  m_TallCellHeight = value; } }
    public RenderTexture RegularCellMark { get { return m_RegularCellMark; } }
    public GridValuePerLevel Velocity { get { return m_Velocity; } set { m_Velocity = value; } }
    public GridValuePerLevel Pressure { get { return m_Pressure; } }
    public GridValuePerLevel RigidBodyPercentage { get { return m_RigidBodyPercentage; } }
    public GridValuePerLevel RigidBodyVelocity { get { return m_RigidBodyVelocity; } }
    public Vector2Int ResolutionXZ { get { return m_ResolutionXZ; } }
    public int RegularCellYCount { get { return m_RegularCellYCount; } }

    public float CellLength { get { return m_CellLength; } }

    public GridPerLevel(Vector2Int vResolutionXZ, int vRegularCellYCount, float vCellLength, bool isFine = false)
    {
        m_CellLength = vCellLength;
        m_ResolutionXZ = vResolutionXZ;
        m_RegularCellYCount = vRegularCellYCount;

        m_TerrrianHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_RegularCellMark = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.R8)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_RigidBodyPercentage = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);

        // No downsampling required
        if (isFine)
        {
            m_Velocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);
            m_Pressure = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
            m_RigidBodyVelocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);
        }
    }

    ~GridPerLevel()
    {
        m_TerrrianHeight.Release();
        m_TallCellHeight.Release();
        m_RegularCellMark.Release();
    }

    private Vector2Int m_ResolutionXZ;
    private int m_RegularCellYCount;
    private float m_CellLength;

    private RenderTexture m_TerrrianHeight;
    private RenderTexture m_TallCellHeight;
    private RenderTexture m_RegularCellMark;
    private GridValuePerLevel m_Velocity;
    private GridValuePerLevel m_Pressure;
    private GridValuePerLevel m_RigidBodyPercentage;
    private GridValuePerLevel m_RigidBodyVelocity;
}

public class Grid
{
    public Grid(Vector2Int vResolutionXZ, int vRegularCellYCount, float vCellLength)
    {
        m_VisualGridMaterial = Resources.Load<Material>("Materials/VisualGrid");
        int[] BoxIndex = new int[36] { 0, 2, 3, 0, 3, 1, 8, 4, 5, 8, 5, 9, 10, 6, 7, 10, 7, 11, 12, 13, 14, 12, 14, 15, 16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23 };
        //m_BoxIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 36, 4);
        //m_BoxIndexBuffer.SetData(BoxIndex);

        m_HierarchicalLevel = (int)Mathf.Min(
            Mathf.Log(vResolutionXZ.x, 2),
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellYCount, 2))) + 1;
        m_GridData = new GridPerLevel[m_HierarchicalLevel];
        for (int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int RegularCellYCount = vRegularCellYCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength * Mathf.Pow(2, i);
            m_GridData[i] = new GridPerLevel(LayerResolutionXZ, RegularCellYCount, CellLength, i == 0);
        }

        m_RemeshTools = new RemeshTools(vResolutionXZ, vCellLength, vRegularCellYCount);
        m_GPUCache = new GridGPUCache(vResolutionXZ, vCellLength, vRegularCellYCount);
        __InitDownSampleTools();
    }

    public GridPerLevel FineGrid { get { return m_GridData[0]; } }

    public GridGPUCache GPUCache { get { return m_GPUCache; } }

    public void VisualGrid(VisualGridInfo VisualGridInfo)
    {
        Profiler.BeginSample("VisualGrid");
        m_VisualGridMaterial.SetPass(0);
        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0,0,0), Quaternion.Euler(0, 0, 0), new Vector3(1, 1, 1));
        //Graphics.DrawMeshNow(VisualGridInfo.mesh, matrix);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 36, 1);
        Profiler.EndSample();
    }

    public void InitMesh(Texture vTerrian, float vSeaLevel)
    {
        m_RemeshTools.ComputeTerrianHeight(vTerrian, FineGrid.TerrainHeight, 20.0f);
        __DownSampleTerrainHeight();

        Profiler.BeginSample("init fine level tallcell grid");
        m_RemeshTools.ComputeH1H2WithSeaLevel(FineGrid.TerrainHeight, m_GPUCache.H1H2Cahce, vSeaLevel);
        __ComputeTallCellHeightFromH1H2();
        Profiler.EndSample();

        Profiler.BeginSample("down sample height");
        __DownSampleTallCellHeight();
        Profiler.EndSample();
    }

    public void Remesh(bool vIsInit = false)
    {
        __SwapFineGridVelocityWithCache();

        Profiler.BeginSample("update fine level tallcell grid");
        //TODO: ComputeH1H2WithParticle
        __ComputeTallCellHeightFromH1H2();
        Profiler.EndSample();

        Profiler.BeginSample("down sample height");
        __DownSampleTallCellHeight();
        Profiler.EndSample();
    }

    public void UpdateGridValue()
    {
        Profiler.BeginSample("update fine grid velocity");
        m_RemeshTools.UpdateFineGridVelocity(
            m_GPUCache.LastFrameTallCellHeightCache,
            m_GPUCache.LastFrameVelocityCache,
            FineGrid.TallCellHeight,
            FineGrid.Velocity) ;
        Profiler.EndSample();

        Profiler.BeginSample("update rigidbody");
        m_RemeshTools.UpdateSolidInfos(FineGrid.TerrainHeight, FineGrid.TallCellHeight, FineGrid.RigidBodyPercentage, FineGrid.RigidBodyVelocity);
        Profiler.EndSample();

        //TODO: update water mark

        Profiler.BeginSample("down sample");
        __DownSampleValue();
        Profiler.EndSample();
    }

    #region DownSample
    private ComputeShader m_DownsampleCS;
    private int downSampleTerrainHeight;
    private int downSampleTallCellHeight;
    private int m_DownSampleRegularCellKernelIndex;
    private int m_DownSampleTallCellKernelIndex;

    private void __InitDownSampleTools()
    {
        m_DownsampleCS = Resources.Load<ComputeShader>(Common.DownsampleToolsCSPath);
        downSampleTerrainHeight = m_DownsampleCS.FindKernel("downSampleTerrainHeight");
        downSampleTallCellHeight = m_DownsampleCS.FindKernel("downSampleTallCellHeight");
        m_DownSampleRegularCellKernelIndex = m_DownsampleCS.FindKernel("downSampleRegularCell");
        m_DownSampleTallCellKernelIndex = m_DownsampleCS.FindKernel("downSampleTallCell");
    }

    private void __DownSampleTallCellHeight(int vSrcLevel, int LeftLevel)
    {
        m_DownsampleCS.SetTexture(downSampleTallCellHeight, "SrcTex", m_GridData[vSrcLevel].TallCellHeight);
        m_DownsampleCS.SetTexture(downSampleTallCellHeight, "SrcTerrain", m_GridData[vSrcLevel].TerrainHeight);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(downSampleTallCellHeight, "OutMip" + i, m_GridData[vSrcLevel + i].TallCellHeight);
        }
        m_DownsampleCS.Dispatch(downSampleTallCellHeight, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);
    }

    private void __DownSampleTallCellHeight()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            __DownSampleTallCellHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    private void __DownSampleTerrainHeight(int vSrcLevel, int LeftLevel)
    {
        m_DownsampleCS.SetTexture(downSampleTerrainHeight, "SrcTex", m_GridData[vSrcLevel].TerrainHeight);
        m_DownsampleCS.SetInts("SrcResolution", m_GridData[vSrcLevel].ResolutionXZ.x, m_GridData[vSrcLevel].ResolutionXZ.y);
        m_DownsampleCS.SetInt("NumMipLevels", LeftLevel);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(downSampleTerrainHeight, "OutMip" + i, m_GridData[vSrcLevel + i].TerrainHeight);
        }
        m_DownsampleCS.Dispatch(downSampleTerrainHeight, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);
    }
    
    private void __DownSampleTerrainHeight()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            __DownSampleTerrainHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    private void __DownSampleValue()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            if (i < 4) m_DownsampleCS.SetInt("SaveMoreAir", 1);
            else m_DownsampleCS.SetInt("SaveMoreAir", 0);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTerrainHeight", m_GridData[i + 1].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTallCellHeight", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TerrainHeight", m_GridData[i].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TallCellHeight", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularMark", m_GridData[i].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularCellRigidBodyPercentage", m_GridData[i].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularMark", m_GridData[i + 1].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularCellRigidBodyPercentage", m_GridData[i + 1].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleRegularCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 4, 1),
                Mathf.Max(m_GridData[i + 1].RegularCellYCount / 4, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 4, 1));
        }

        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTerrainHeight", m_GridData[i + 1].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTallCellHeight", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TerrainHeight", m_GridData[i].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TallCellHeight", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularCellRigidBodyPercentage", m_GridData[i].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellTop", m_GridData[i].RigidBodyPercentage.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellBottom", m_GridData[i].RigidBodyPercentage.TallCellBottomValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellTop", m_GridData[i + 1].RigidBodyPercentage.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellBottom", m_GridData[i + 1].RigidBodyPercentage.TallCellBottomValue);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleTallCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 8, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 8, 1),
                1);
        }
    }
    #endregion

    private void __ComputeTallCellHeightFromH1H2()
    {
        m_RemeshTools.ComputeTallCellHeight(FineGrid.TerrainHeight, m_GPUCache.H1H2Cahce, m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GPUCache.BackTallCellHeightCahce, FineGrid.TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(FineGrid.TerrainHeight, m_GPUCache.BackTallCellHeightCahce, FineGrid.TallCellHeight);
    }

    private void __SwapFineGridVelocityWithCache()
    {
        RenderTexture LastFrameTallCellHeightCache = m_GPUCache.LastFrameTallCellHeightCache;
        GridValuePerLevel LastFrameVelocityCache = m_GPUCache.LastFrameVelocityCache;

        m_GPUCache.LastFrameTallCellHeightCache = FineGrid.TallCellHeight;
        m_GPUCache.LastFrameVelocityCache = FineGrid.Velocity;
        FineGrid.TallCellHeight = LastFrameTallCellHeightCache;
        FineGrid.Velocity = LastFrameVelocityCache;
    }

    private int m_HierarchicalLevel;
    private GridPerLevel[] m_GridData;
    private RemeshTools m_RemeshTools;

    private GridGPUCache m_GPUCache;

    private Material m_VisualGridMaterial;
    //GraphicsBuffer m_BoxIndexBuffer;
}
