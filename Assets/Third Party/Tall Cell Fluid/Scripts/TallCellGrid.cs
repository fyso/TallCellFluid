using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class TallCellGridLayerData3D
{
    public RenderTexture UpperUniform { get { return m_UpperUniform; } }
    public RenderTexture Top { get { return m_Top; } }
    public RenderTexture Bottom { get { return m_Bottom; } }

    public TallCellGridLayerData3D(Vector2Int vResolutionXZ, int vRegularCellCount, RenderTextureFormat vDataType)
    {
        m_UpperUniform = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vRegularCellCount, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        m_Top = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_Bottom = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~TallCellGridLayerData3D()
    {
        m_UpperUniform.Release();
        m_Top.Release();
        m_Bottom.Release();
    }

    private RenderTexture m_UpperUniform;
    private RenderTexture m_Top;
    private RenderTexture m_Bottom;
}

public class TallCellGridLayer
{
    public RenderTexture TerrrianHeight { get { return m_TerrrianHeight; } }
    public RenderTexture TallCellHeight { get { return m_TallCellHeight; } }
    public TallCellGridLayerData3D Velocity { get { return m_Velocity; } }
    public TallCellGridLayerData3D Pressure { get { return m_Pressure; } }
    public Vector2Int ResolutionXZ { get { return m_ResolutionXZ; } }
    public float CellLength { get { return m_CellLength; } }

    public TallCellGridLayer(Vector2Int vResolutionXZ, int vRegularCellCount, float vCellLength)
    {
        m_CellLength = vCellLength;
        m_ResolutionXZ = vResolutionXZ;
        m_RegularCellCount = vRegularCellCount;

        m_TerrrianHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_Velocity = new TallCellGridLayerData3D(vResolutionXZ, vRegularCellCount, RenderTextureFormat.ARGBFloat);
        m_Pressure = new TallCellGridLayerData3D(vResolutionXZ, vRegularCellCount, RenderTextureFormat.RFloat);
    }

    ~TallCellGridLayer()
    {
        m_TerrrianHeight.Release();
        m_TallCellHeight.Release();
    }

    private Vector2Int m_ResolutionXZ;
    private int m_RegularCellCount;
    private float m_CellLength;

    private RenderTexture m_TerrrianHeight;
    private RenderTexture m_TallCellHeight;
    private TallCellGridLayerData3D m_Velocity;
    private TallCellGridLayerData3D m_Pressure;
}

public class TallCellGrid
{
    public List<TallCellGridLayer> TallCellGridLayers { get { return m_TallCellGridLayers; } }
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
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellCount, 2)));
        m_TallCellGridLayers = new List<TallCellGridLayer>();

        for (int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int LayerRegularCellCount = vRegularCellCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength * Mathf.Pow(2, i);
            TallCellGridLayer Temp = new TallCellGridLayer(LayerResolutionXZ, LayerRegularCellCount, CellLength);
            m_TallCellGridLayers.Add(Temp);
        }

        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 8.0f);

        m_RemeshTools = new RemeshTools(vResolutionXZ, vCellLength, vRegularCellCount);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellCount);

        m_GPUCache = new TallCellGridGPUCache(vResolutionXZ, vCellLength, vRegularCellCount);
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

    private void Remesh()
    {
        if (!m_IsInit)
        {
            m_RemeshTools.ComputeTerrianHeight(m_Terrian, m_TallCellGridLayers[0].TerrrianHeight, 10.0f);
            m_RemeshTools.ComputeH1H2WithSeaLevel(m_TallCellGridLayers[0].TerrrianHeight, m_GPUCache.H1H2Cahce, m_SeaLevel);
        }
        else
        {
            //ComputeH1H2WithMark
        }

        m_RemeshTools.ComputeTallCellHeight(m_TallCellGridLayers[0].TerrrianHeight, m_GPUCache.H1H2Cahce, m_GPUCache.MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_GPUCache.BackTallCellHeightCahce, m_TallCellGridLayers[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_GPUCache.MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight, m_GPUCache.BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(m_TallCellGridLayers[0].TerrrianHeight, m_GPUCache.BackTallCellHeightCahce, m_TallCellGridLayers[0].TallCellHeight);

        if (!m_IsInit)
            m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_TallCellGridLayers[0], m_SeaLevel, m_DynamicParticle);

        //update mark for each level
        m_ParticleInCellTools.scatterParticleToGrid(m_DynamicParticle, m_TallCellGridLayers[0], m_GPUCache);

        //transfer data from old to new (fine level)
        //down sample TerrianHeight and TallCellHeight to coarse level

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
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_TallCellGridLayers[0]);
        m_DynamicParticle.DeleteParticleOutofRange(m_Min, m_Max, m_CellLength);
        m_DynamicParticle.OrganizeParticle();

        //generate cell's particle count and offset info
        m_DynamicParticle.ZSort(m_Min, m_CellLength);

        //grid to particle using fine level
        //advect particle
        //Particle to grid using fine level
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
    private List<TallCellGridLayer> m_TallCellGridLayers;
    private DynamicParticle m_DynamicParticle;

    private RemeshTools m_RemeshTools;
    private ParticleInCellTools m_ParticleInCellTools;

    //Temp cache
    private TallCellGridGPUCache m_GPUCache;
}
