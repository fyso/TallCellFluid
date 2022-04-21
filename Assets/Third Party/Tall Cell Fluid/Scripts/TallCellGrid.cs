using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class GridValuePerLevel
{
    public RenderTexture RegularCellValue { get { return m_RegularCellValue; } }
    public RenderTexture TallCellTopValue { get { return m_TallCellTopValue; } }
    public RenderTexture TallCellBottomValue { get { return m_TallCellBottomValue; } }

    public GridValuePerLevel(Vector2Int vResolutionXZ, int vRegularCellCount, RenderTextureFormat vDataType)
    {
        m_RegularCellValue = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vRegularCellCount,
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
    public GridValuePerLevel Velocity { get { return m_Velocity; } }
    public GridValuePerLevel Pressure { get { return m_Pressure; } }
    public Vector2Int ResolutionXZ { get { return m_ResolutionXZ; } }
    public float CellLength { get { return m_CellLength; } }

    public GridPerLevel(Vector2Int vResolutionXZ, int vRegularCellCount, float vCellLength)
    {
        m_CellLength = vCellLength;
        m_ResolutionXZ = vResolutionXZ;
        m_RegularCellCount = vRegularCellCount;

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

        m_Velocity = new GridValuePerLevel(vResolutionXZ, vRegularCellCount, RenderTextureFormat.ARGBFloat);
        m_Pressure = new GridValuePerLevel(vResolutionXZ, vRegularCellCount, RenderTextureFormat.RFloat);
    }

    ~GridPerLevel()
    {
        m_TerrrianHeight.Release();
        m_TallCellHeight.Release();
    }

    private Vector2Int m_ResolutionXZ;
    private int m_RegularCellCount;
    private float m_CellLength;

    private RenderTexture m_TerrrianHeight;
    private RenderTexture m_TallCellHeight;
    private GridValuePerLevel m_Velocity;
    private GridValuePerLevel m_Pressure;
}

public class TallCellGrid
{
    public List<GridPerLevel> GridData { get { return m_GridData; } }
    public DynamicParticle DynamicParticle { get { return m_DynamicParticle; } }

    public TallCellGrid(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellCount, Vector3 vMin, float vCellLength, float vSeaLevel, int vMaxParticleCount)
    {
        m_SeaLevel = vSeaLevel;
        m_Terrian = vTerrian;
        m_Min = vMin;
        m_CellLength = vCellLength;
        m_Max = vMin + (new Vector3(vResolutionXZ.x, vRegularCellCount * 32, vResolutionXZ.y)) * vCellLength;
        m_HierarchicalLevel = (int)Mathf.Min(
            Mathf.Log(vResolutionXZ.x, 2),
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellCount, 2))) + 1;
        m_GridData = new List<GridPerLevel>();

        for (int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int LayerRegularCellCount = vRegularCellCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength * Mathf.Pow(2, i);
            GridPerLevel Temp = new GridPerLevel(LayerResolutionXZ, LayerRegularCellCount, CellLength);
            m_GridData.Add(Temp);
        }

        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 8.0f);

        m_RemeshTools = new RemeshTools(vResolutionXZ, vCellLength, vRegularCellCount);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellCount);

        m_GPUCache = new TallCellGridGPUCache(vResolutionXZ, vCellLength, vRegularCellCount);

        m_ReductionCS = Resources.Load<ComputeShader>(Common.ReductionToolsCSPath);
        DownSampleWithFourLevelsKernelIndex = m_ReductionCS.FindKernel("downSampleWithFourLevels");
    }

    public void Step(float vTimeStep)
    {
        Profiler.BeginSample("Remesh");
        Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("ExtrapolationVelocity");
        ExtrapolationVelocity();
        Profiler.EndSample();

        Profiler.BeginSample("Advect");
        Advect();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        SparseMultiGridRedBlackGaussSeidel();
        Profiler.EndSample();
    }


    private void DownSampleWithFourLevels(int vSrcLevel, int LeftLevel)
    {
        //Terrain
        m_ReductionCS.SetTexture(DownSampleWithFourLevelsKernelIndex, "SrcTex", GridData[vSrcLevel].TerrrianHeight);
        m_ReductionCS.SetInts("SrcResolution", GridData[vSrcLevel].ResolutionXZ.x, GridData[vSrcLevel].ResolutionXZ.y);//TODO:是否同步？
        m_ReductionCS.SetInt("NumMipLevels", LeftLevel);//TODO:是否同步？
        m_ReductionCS.SetTexture(DownSampleWithFourLevelsKernelIndex, "SrcTex", GridData[vSrcLevel].TerrrianHeight);
        for(int i = 1; i < LeftLevel; i++)
        {
            m_ReductionCS.SetTexture(DownSampleWithFourLevelsKernelIndex, "outMip" + i, GridData[vSrcLevel + i].TerrrianHeight);
        }
        //m_ReductionCS.Dispatch(DownSampleWithFourLevelsKernelIndex, 8, 8, 1);
    }

    private void Remesh()
    {
        if (!m_IsInit)
        {
            m_RemeshTools.ComputeTerrianHeight(m_Terrian, m_GridData[0].TerrrianHeight, 10.0f);
            m_RemeshTools.ComputeH1H2WithSeaLevel(m_GridData[0].TerrrianHeight, m_GPUCache.H1H2Cahce, m_SeaLevel);
        }
        else
        {
            //ComputeH1H2WithMark
        }

        Profiler.BeginSample("generate fine level tallcellgrid");
        m_RemeshTools.ComputeTallCellHeight(m_GridData[0].TerrrianHeight, m_GPUCache.H1H2Cahce, m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GPUCache.BackTallCellHeightCahce, m_GridData[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GridData[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(m_GridData[0].TerrrianHeight, m_GPUCache.BackTallCellHeightCahce, m_GridData[0].TallCellHeight);
        Profiler.EndSample();

        if (!m_IsInit)
            m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_GridData[0], m_SeaLevel, m_DynamicParticle);

        //update mark for each level

        //transfer data from old to new (fine level)

        //down sample TerrianHeight and TallCellHeight to coarse level
        //TODO：只支持2的指数量的分辨率
        Profiler.BeginSample("DownSampleWithFourLevels");
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            DownSampleWithFourLevels(i, m_HierarchicalLevel - i - 1);
        }
        Profiler.EndSample();

        if (!m_IsInit) m_IsInit = true;
    }

    private void ExtrapolationVelocity()
    {
        //narrowband extrapolation
        //down sample TerrianHeight and TallCellHeight to coarse level
        //V-style extrapolation
    }

    private void Advect()
    {
        //split particle by cell type
        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_GridData[0]);
        Profiler.EndSample();

        Profiler.BeginSample("DeleteParticleOutofRange");
        m_DynamicParticle.DeleteParticleOutofRange(m_Min, m_Max, m_CellLength);
        Profiler.EndSample();

        Profiler.BeginSample("OrganizeParticle");
        m_DynamicParticle.OrganizeParticle();
        Profiler.EndSample();

        //grid to particle using fine level
        Profiler.BeginSample("Gather Grid To Particle");
        m_ParticleInCellTools.GatherGridToParticle(m_DynamicParticle, m_GridData[0]);
        Profiler.EndSample();

        //advect particle

        //generate OnlyTallCell particle count and offset info
        Profiler.BeginSample("ZSort OnlyTallCell particle");
        m_DynamicParticle.ZSort(m_Min, m_CellLength, true, 3);
        Profiler.EndSample();

        //Profiler.BeginSample("Gather OnlyTallCell Particle To Grid");
        //m_ParticleInCellTools.GatherOnlyTallCellParticleToGrid(m_DynamicParticle, m_GridData[0]);
        //Profiler.EndSample();

        //OnlyTallCell Particle to grid using fine level
    }

    private void SparseMultiGridRedBlackGaussSeidel()
    {
        //multi grid gauss-seidel
    }

    private bool m_IsInit = false;
    private Vector3 m_Min;
    private Vector3 m_Max;
    private float m_SeaLevel;
    private float m_CellLength;
    private Texture m_Terrian;
    private int m_HierarchicalLevel;
    private List<GridPerLevel> m_GridData;
    private DynamicParticle m_DynamicParticle;

    private RemeshTools m_RemeshTools;
    private ParticleInCellTools m_ParticleInCellTools;
    private ComputeShader m_ReductionCS;
    private int DownSampleWithFourLevelsKernelIndex;

    //Temp cache
    private TallCellGridGPUCache m_GPUCache;
}
