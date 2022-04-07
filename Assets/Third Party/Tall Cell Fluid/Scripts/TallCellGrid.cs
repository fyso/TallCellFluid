using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;

public class TallCellGridLayerData3D
{
    private RenderTexture m_UpperUniform;
    private RenderTexture m_Top;
    private RenderTexture m_Bottom;

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
}

public class TallCellGridLayer
{
    public RenderTexture TerrrianHeight { get { return m_TerrrianHeight; } }
    public RenderTexture TallCellHeight { get { return m_TallCellHeight; } }

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
    public TallCellGrid(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellCount, Vector3 vMin, float vCellLength, float vSeaLevel, int vMaxParticleCount)
    {
        m_SeaLevel = vSeaLevel;
        m_Terrian = vTerrian;
        m_Min = vMin;
        m_HierarchicalLevel = (int)Mathf.Min(
            Mathf.Log(vResolutionXZ.x, 2),
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellCount, 2)));
        m_TallCellGridLayers = new List<TallCellGridLayer>();

        for (int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int LayerRegularCellCount = vRegularCellCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength / Mathf.Pow(2, i);
            TallCellGridLayer Temp = new TallCellGridLayer(LayerResolutionXZ, LayerRegularCellCount, CellLength);
            m_TallCellGridLayers.Add(Temp);
        }

        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 8.0f);

        m_RemeshTools = new RemeshTools(vResolutionXZ, vCellLength, vRegularCellCount);

        m_H1H2Cahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_MaxMinCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_BackTallCellHeightCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~TallCellGrid()
    {
        m_H1H2Cahce.Release();
        m_MaxMinCahce.Release();
        m_BackTallCellHeightCahce.Release();
    }

    public void Init(float vSeaLevel)
    {
    }

    public void Step(float vTimeStep)
    {
        Remesh();
        ExtrapolationVelocity();
        Advect();
        SparseMultiGridRedBlackGaussSeidel();
    }

    private void ExtrapolationVelocity()
    {
        //narrowband extrapolation
        //down sample TerrianHeight and TallCellHeight to coarse level
        //V-style extrapolation
    }

    private void Advect()
    {
        //grid to particle using fine level
        //advect particle
        //Particle to grid using fine level
    }

    private void Remesh()
    {
        if(!IsInit)
        {
            m_RemeshTools.ComputeTerrianHeight(m_Terrian, m_TallCellGridLayers[0].TerrrianHeight, 10.0f);
            m_RemeshTools.ComputeH1H2WithSeaLevel(m_TallCellGridLayers[0].TerrrianHeight, m_H1H2Cahce, m_SeaLevel);
            IsInit = true;
        }
        else
        {
            //ComputeH1H2WithMark
        }

        m_RemeshTools.ComputeTallCellHeight(m_TallCellGridLayers[0].TerrrianHeight, m_H1H2Cahce, m_MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight, m_BackTallCellHeightCahce);
        m_RemeshTools.SmoothTallCellHeight(m_MaxMinCahce, m_BackTallCellHeightCahce, m_TallCellGridLayers[0].TallCellHeight);
        m_RemeshTools.SmoothTallCellHeight(m_MaxMinCahce, m_TallCellGridLayers[0].TallCellHeight, m_BackTallCellHeightCahce);
        m_RemeshTools.EnforceDCondition(m_TallCellGridLayers[0].TerrrianHeight, m_BackTallCellHeightCahce, m_TallCellGridLayers[0].TallCellHeight);

        //transfer data from old to new (fine level)

        //down sample TerrianHeight and TallCellHeight to coarse level
        //add Particle and update mark for each level
    }

    private void SparseMultiGridRedBlackGaussSeidel()
    {
        //multi grid gauss-seidel
    }

    private bool IsInit = false;
    private float m_SeaLevel;
    private Texture m_Terrian;
    private Vector3 m_Min;
    private int m_HierarchicalLevel;
    private List<TallCellGridLayer> m_TallCellGridLayers;
    private DynamicParticle m_DynamicParticle;

    private RemeshTools m_RemeshTools;

    private RenderTexture m_H1H2Cahce;
    private RenderTexture m_MaxMinCahce;
    private RenderTexture m_BackTallCellHeightCahce;
}
