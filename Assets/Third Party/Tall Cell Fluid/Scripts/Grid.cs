using UnityEngine;

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
    public RenderTexture TerrrianHeight { get { return m_TerrrianHeight; } }
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

    public void RestCache()
    {
        Graphics.SetRenderTarget(GPUCache.TallCellScalarCahce1);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.TallCellScalarCahce2);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.TallCellVectorCahce1);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.TallCellVectorCahce2);
        GL.Clear(false, true, new Color(0, 0, 0, 0));

        Graphics.SetRenderTarget(GPUCache.RegularCellScalarCahce);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.RegularCellVectorXCache);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.RegularCellVectorYCache);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
        Graphics.SetRenderTarget(GPUCache.RegularCellVectorZCache);
        GL.Clear(false, true, new Color(0, 0, 0, 0));
    }

    public void InitMesh(Texture vTerrian, float vSeaLevel)
    {
        m_RemeshTools.ComputeTerrianHeight(vTerrian, FineGrid.TerrrianHeight, 20.0f);

        UnityEngine.Profiling.Profiler.BeginSample("init fine level tallcell grid");
        m_RemeshTools.ComputeH1H2WithSeaLevel(FineGrid.TerrrianHeight, m_GPUCache.H1H2Cahce, vSeaLevel);
        __ComputeTallCellHeightFromH1H2();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("down sample height");
        __DownSampleHeight();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void Remesh(bool vIsInit = false)
    {
        __SwapFineGridVelocityWithCache();

        UnityEngine.Profiling.Profiler.BeginSample("update fine level tallcell grid");
        //TODO: ComputeH1H2WithParticle
        __ComputeTallCellHeightFromH1H2();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("down sample height");
        __DownSampleHeight();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void UpdateGridValue()
    {
        UnityEngine.Profiling.Profiler.BeginSample("update fine grid velocity");
        m_RemeshTools.UpdateFineGridVelocity(
            m_GPUCache.LastFrameTallCellHeightCache,
            m_GPUCache.LastFrameVelocityCache,
            FineGrid.TallCellHeight,
            FineGrid.Velocity) ;
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("update rigidbody");
        //m_RemeshTools.UpdateSolidInfos(FineGrid.TerrrianHeight, FineGrid.TallCellHeight, FineGrid.RigidBodyPercentage, FineGrid.RigidBodyVelocity);
        UnityEngine.Profiling.Profiler.EndSample();

        //TODO: update water mark

        UnityEngine.Profiling.Profiler.BeginSample("down sample");
        __DownSampleValue();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    #region DownSample
    private ComputeShader m_DownsampleCS;
    private int m_ReductionKernelIndex;
    private int m_DownSampleRegularCellKernelIndex;
    //private int m_DownSampleTallCellKernelIndex;

    private void __InitDownSampleTools()
    {
        m_DownsampleCS = Resources.Load<ComputeShader>(Common.DownsampleToolsCSPath);
        m_ReductionKernelIndex = m_DownsampleCS.FindKernel("reduction");
        m_DownSampleRegularCellKernelIndex = m_DownsampleCS.FindKernel("downSampleRegularCell");
        //m_DownSampleTallCellKernelIndex = m_DownsampleCS.FindKernel("downSampleTallCell");
    }

    private void __DownSampleHeight(int vSrcLevel, int LeftLevel)
    {
        //Terrain
        m_DownsampleCS.SetTexture(m_ReductionKernelIndex, "SrcTex", m_GridData[vSrcLevel].TerrrianHeight);
        m_DownsampleCS.SetInts("SrcResolution", m_GridData[vSrcLevel].ResolutionXZ.x, m_GridData[vSrcLevel].ResolutionXZ.y);
        m_DownsampleCS.SetInt("NumMipLevels", LeftLevel);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(m_ReductionKernelIndex, "OutMip" + i, m_GridData[vSrcLevel + i].TerrrianHeight);
        }
        m_DownsampleCS.EnableKeyword("_REDUCTION_MAX");
        m_DownsampleCS.DisableKeyword("_REDUCTION_MIN");
        m_DownsampleCS.Dispatch(m_ReductionKernelIndex, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);

        //TallCell
        m_DownsampleCS.SetTexture(m_ReductionKernelIndex, "SrcTex", m_GridData[vSrcLevel].TallCellHeight);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(m_ReductionKernelIndex, "OutMip" + i, m_GridData[vSrcLevel + i].TallCellHeight);
        }
        m_DownsampleCS.EnableKeyword("_REDUCTION_MAX");
        m_DownsampleCS.DisableKeyword("_REDUCTION_MIN");
        m_DownsampleCS.Dispatch(m_ReductionKernelIndex, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);
    }

    private void __DownSampleHeight()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            __DownSampleHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    private void __DownSampleValue()
    {
        //down sample regular cell Mark/RigidBodyPercentage to coarse level
        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            if (i < 4) m_DownsampleCS.SetInt("SaveMoreAir", 1);
            else m_DownsampleCS.SetInt("SaveMoreAir", 0);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTallCell", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TallCell", m_GridData[i].TallCellHeight);
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

        //TODO: down sample tall cell RigidBodyPercentage to coarse level
        //for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        //{
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TallCell", m_GridData[i].TallCellHeight);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTallCell", m_GridData[i + 1].TallCellHeight);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "Terrain", m_GridData[i].TerrrianHeight);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTerrain", m_GridData[i + 1].TerrrianHeight);
        //    m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularMark", m_GridData[i].RegularCellMark);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularCellVelocity", m_GridData[i].Velocity.RegularCellValue);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellTop", m_GridData[i].Velocity.TallCellTopValue);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellBottom", m_GridData[i].Velocity.TallCellBottomValue);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellTop", m_GridData[i + 1].Velocity.TallCellTopValue);
        //    m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellBottom", m_GridData[i + 1].Velocity.TallCellBottomValue);
        //    m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
        //    m_DownsampleCS.Dispatch(m_DownSampleTallCellKernelIndex,
        //        Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 8, 1),
        //        Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 8, 1),
        //        1);
        //}
    }
    #endregion

    private void __ComputeTallCellHeightFromH1H2()
    {
        m_RemeshTools.ComputeTallCellHeight(FineGrid.TerrrianHeight, m_GPUCache.H1H2Cahce, m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GPUCache.BackTallCellHeightCahce, FineGrid.TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, FineGrid.TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(FineGrid.TerrrianHeight, m_GPUCache.BackTallCellHeightCahce, FineGrid.TallCellHeight);
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
}
