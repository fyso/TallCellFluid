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
    public RenderTexture TallCellHeight { get { return m_TallCellHeight; } }
    public RenderTexture RegularCellMark { get { return m_RegularCellMark; } }
    public GridValuePerLevel Velocity { get { return m_Velocity; } }
    public GridValuePerLevel Pressure { get { return m_Pressure; } }
    public GridValuePerLevel RigidBodyPercentage { get { return m_RigidBodyPercentage; } }
    public GridValuePerLevel RigidBodyVelocity { get { return m_RigidBodyVelocity; } }
    public Vector2Int ResolutionXZ { get { return m_ResolutionXZ; } }
    public int RegularCellYCount { get { return m_RegularCellYCount; } }

    public float CellLength { get { return m_CellLength; } }

    public void ClearMark(int clearValue = 0)
    {
        Graphics.SetRenderTarget(m_RegularCellMark);
        GL.Clear(true, true, new Color(clearValue, clearValue, clearValue, clearValue));
    }

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
        ClearMark(1);

        m_Velocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);
        m_Pressure = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        m_RigidBodyPercentage = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        if(isFine) m_RigidBodyVelocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat); // No downsampling required
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
    public GridGPUCache GPUCache { get { return m_GPUCache; } }

    #region DownSample
    private ComputeShader m_DownsampleCS;
    private int m_ReductionKernelIndex;
    private int m_DownSampleRegularCellKernelIndex;
    private int m_DownSampleTallCellKernelIndex;

    private void InitDownSampleTools()
    {
        m_DownsampleCS = Resources.Load<ComputeShader>(Common.DownsampleToolsCSPath);
        m_ReductionKernelIndex = m_DownsampleCS.FindKernel("reduction");
        m_DownSampleRegularCellKernelIndex = m_DownsampleCS.FindKernel("downSampleRegularCell");
        m_DownSampleTallCellKernelIndex = m_DownsampleCS.FindKernel("downSampleTallCell");
    }

    private void UpdateHeight(int vSrcLevel, int LeftLevel)
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

    private void DownSample()
    {
        //down sample regular cell velocity/Mark to coarse level
        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            if (i < 4) m_DownsampleCS.SetInt("SaveMoreAir", 1);
            else m_DownsampleCS.SetInt("SaveMoreAir", 0);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTallCell", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TallCell", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularMark", m_GridData[i].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularCell", m_GridData[i].Velocity.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularCell", m_GridData[i + 1].Velocity.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularMark", m_GridData[i + 1].RegularCellMark);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleRegularCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 4, 1),
                Mathf.Max(m_GridData[i + 1].RegularCellYCount / 4, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 4, 1));
        }

        //down sample tall cell velocity to coarse level
        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TallCell", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTallCell", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "Terrain", m_GridData[i].TerrrianHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTerrain", m_GridData[i + 1].TerrrianHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularMark", m_GridData[i].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularCell", m_GridData[i].Velocity.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellTop", m_GridData[i].Velocity.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellBottom", m_GridData[i].Velocity.TallCellBottomValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellTop", m_GridData[i + 1].Velocity.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellBottom", m_GridData[i + 1].Velocity.TallCellBottomValue);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleTallCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 8, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 8, 1),
                1);
        }
    }
    #endregion

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
        InitDownSampleTools();
    }

    public void InitMesh(Texture vTerrian, float vSeaLevel)
    {
        m_RemeshTools.ComputeTerrianHeight(vTerrian, m_GridData[0].TerrrianHeight, 20.0f);
        m_RemeshTools.ComputeH1H2WithSeaLevel(m_GridData[0].TerrrianHeight, m_GPUCache.H1H2Cahce, vSeaLevel);

        Remesh(true);
    }

    public void Remesh(bool vIsInit = false)
    {
        if (!vIsInit)
        {
            //TODO: ComputeH1H2WithMark
        }

        UnityEngine.Profiling.Profiler.BeginSample("generate fine level tallcellgrid");
        m_RemeshTools.ComputeTallCellHeight(m_GridData[0].TerrrianHeight, m_GPUCache.H1H2Cahce, m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GPUCache.BackTallCellHeightCahce, m_GridData[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(m_GridData[0].TerrrianHeight, m_GPUCache.BackTallCellHeightCahce, m_GridData[0].TallCellHeight);
        UnityEngine.Profiling.Profiler.EndSample();

        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            UpdateHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    public void UpdateRigidbody()
    {
        //TODO: tall cell has not been considered
        m_RemeshTools.UpdateSolidInfos(FineGrid.TallCellHeight, FineGrid.RigidBodyPercentage.RegularCellValue, FineGrid.RigidBodyVelocity.RegularCellValue);
    }

    public void UpdateGridValue()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Update Rigidbody");
        UpdateRigidbody();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("DownSample");
        DownSample();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public GridPerLevel FineGrid { get { return m_GridData[0]; } }

    private int m_HierarchicalLevel;
    private GridPerLevel[] m_GridData;
    private RemeshTools m_RemeshTools;

    private GridGPUCache m_GPUCache;
}
