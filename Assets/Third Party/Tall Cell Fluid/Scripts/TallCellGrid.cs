using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallCellGridLayer
{
    public TallCellGridLayer(Vector2Int vResolutionXZ, int vRegularCellCount, float vCellLength)
    {
        m_CellLength = vCellLength;
        m_ResolutionXZ = vResolutionXZ;
        m_RegularCellCount = vRegularCellCount;

        m_Terrrian = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
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

        m_Velocity = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vRegularCellCount + 2, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~TallCellGridLayer()
    {
        m_Terrrian.Release();
        m_TallCellHeight.Release();
        m_Velocity.Release();
    }

    private Vector2Int m_ResolutionXZ;
    private int m_RegularCellCount;
    private float m_CellLength;

    private RenderTexture m_Terrrian;
    private RenderTexture m_TallCellHeight;
    private RenderTexture m_Velocity;
}

public class TallCellGrid
{
    public TallCellGrid(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellCount, Vector3 vMin, float vCellLength, float vSeaLevel)
    {
        m_TallCellSolverCS = Resources.Load<ComputeShader>("Shaders/TallCellSolver");

        m_Terrian = vTerrian;
        m_Min = vMin;
        m_HierarchicalLevel = (int)Mathf.Min(
            Mathf.Log(vResolutionXZ.x, 2),
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellCount, 2)));

        for(int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int LayerRegularCellCount = vRegularCellCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength / Mathf.Pow(2, i);
            TallCellGridLayer Temp = new TallCellGridLayer(LayerResolutionXZ, LayerRegularCellCount, CellLength);
            m_TallCellGridLayers.Add(Temp);
        }
    }

    public void Step(float vTimeStep)
    {
        ExtrapolationVelocity();
        Advect();
        Remesh();
        SolveLiner();
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
        //modifie 2D Texture TerrianHeight and TallCellHeight (fine level)
        //transfer data from old to new (fine level)

        //down sample TerrianHeight and TallCellHeight to coarse level
        //down sample velocity to coarse level

        //update mark for each level
    }

    private void SolveLiner()
    {
        //generate mark using particle and solid
        //construct A matrix for each level
    }

    private Texture m_Terrian;
    private Vector3 m_Min;
    private int m_HierarchicalLevel;
    private List<TallCellGridLayer> m_TallCellGridLayers;

    private ComputeShader m_TallCellSolverCS;
}
