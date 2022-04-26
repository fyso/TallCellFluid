using UnityEngine;
using DParticle;
using UnityEngine.Profiling;
using System.Collections.Generic;

public class Simulator
{
    public static int OnlyTallCellParticleTypeIndex {get{ return 3; }} // TODO: magic number

    public static int RegularCellParticleTypeIndex { get{ return 0; } } // TODO: magic number

    public Simulator(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellYCount, Vector3 vMin, float vCellLength, float vSeaLevel, int vMaxParticleCount)
    {
        m_Min = vMin;
        m_CellLength = vCellLength;
        m_Max = vMin + (new Vector3(vResolutionXZ.x, vRegularCellYCount * 32, vResolutionXZ.y)) * vCellLength;

        m_Grid = new Grid(vResolutionXZ, vRegularCellYCount, vCellLength);
        m_Grid.InitMesh(vTerrian, vSeaLevel);

        m_Utils = new Utils();
        m_SimulatorGPUCache = new SimulatorGPUCache(vMaxParticleCount);
        m_ParticleSortTools = new ParticleSortTools();
        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 4.0f);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellYCount);
        m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_Grid.FineGrid, vSeaLevel, m_DynamicParticle);

        m_Grid.UpdateGridValue();
    }

    public void VisualParticle()
    {
        m_DynamicParticle.VisualParticle();
    }
    
    public void VisualGrid(VisualGridInfo VisualGridInfo)
    {
        m_Grid.VisualGrid(VisualGridInfo);
    }

    public void DebugGridShape()
    {
        GridPerLevel FineGrid = m_Grid.FineGrid;
        Texture2D TerrianHeight = Common.CopyRenderTextureToCPU(FineGrid.TerrainHeight);
        Texture2D TallCellHeight = Common.CopyRenderTextureToCPU(FineGrid.TallCellHeight);

        for (int i = 0; i < FineGrid.ResolutionXZ.x; i++)
        {
            for (int j = 0; j < FineGrid.ResolutionXZ.y; j++)
            {
                float CurrTerrianHeight = TerrianHeight.GetPixel(i, j).r;
                float CurrTallCellHeight = TallCellHeight.GetPixel(i, j).r;

                Gizmos.color = new Color(0.0f, 0.0f, 1.0f);
                Vector3 RegularCellCenter = m_Min + new Vector3(i * FineGrid.CellLength, CurrTallCellHeight + CurrTerrianHeight + 0.5f * FineGrid.CellLength, j * FineGrid.CellLength);
                for (int k = 0; k < FineGrid.RegularCellYCount; k++)
                {
                    Gizmos.DrawWireCube(RegularCellCenter, new Vector3(FineGrid.CellLength, FineGrid.CellLength, FineGrid.CellLength));
                    RegularCellCenter.y += FineGrid.CellLength;
                }

                Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
                Vector3 TallCellCenter = m_Min + new Vector3(i * FineGrid.CellLength, CurrTallCellHeight * 0.5f + CurrTerrianHeight, j * FineGrid.CellLength);
                Gizmos.DrawWireCube(TallCellCenter, new Vector3(FineGrid.CellLength, CurrTallCellHeight, FineGrid.CellLength));

                Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
                Vector3 TerrianCellCenter = m_Min + new Vector3(i * FineGrid.CellLength, CurrTerrianHeight * 0.5f, j * FineGrid.CellLength);
                Gizmos.DrawWireCube(TerrianCellCenter, new Vector3(FineGrid.CellLength, CurrTerrianHeight, FineGrid.CellLength));
            }
        }
    }

    public void GenerateRandomVelicty()
    {
        GridPerLevel FineGrid = m_Grid.FineGrid;
        Texture2D Top = new Texture2D(FineGrid.ResolutionXZ.x, FineGrid.ResolutionXZ.y, TextureFormat.RGBAFloat, false);
        Texture2D Bottom = new Texture2D(FineGrid.ResolutionXZ.x, FineGrid.ResolutionXZ.y, TextureFormat.RGBAFloat, false);
        if (Top.width != Bottom.width || Top.height != Bottom.height)
            Debug.LogError("unmatched top and bottom data!");

        System.Random Rand = new System.Random();
        for(int x = 0; x < Top.width; x++)
        {
            for (int y = 0; y < Top.height; y++)
            {
                Color TopVelocity = new Color((float)x / Top.width, 0.0f, 0.0f, 1.0f);
                Top.SetPixel(x, y, TopVelocity);
                Color BottomVelocity = new Color(0.0f, 0, 0, 1.0f);
                Bottom.SetPixel(x, y, BottomVelocity);
            }
        }
        Top.Apply();
        Bottom.Apply();
        m_Utils.CopyFloat4Texture2DToAnother(Top, FineGrid.Velocity.TallCellTopValue);
        m_Utils.CopyFloat4Texture2DToAnother(Bottom, FineGrid.Velocity.TallCellBottomValue);

        Texture3D Regular = new Texture3D(FineGrid.ResolutionXZ.x, FineGrid.RegularCellYCount, FineGrid.ResolutionXZ.y, TextureFormat.RGBAFloat, 0);
        for (int x = 0; x < FineGrid.ResolutionXZ.x; x++)
        {
            for (int y = 0; y < FineGrid.RegularCellYCount; y++)
            {
                for (int z = 0; z < FineGrid.ResolutionXZ.y; z++)
                {
                    Color RegularVelocity = new Color((float)x / Top.width, 0.0f, 0.0f, 1.0f);
                    Regular.SetPixel(x, y, z, RegularVelocity);
                }
            }
        }
        Regular.Apply();
        m_Utils.CopyFloat4Texture3DToAnother(Regular, FineGrid.Velocity.RegularCellValue);
    }

    public void Step(float vTimeStep)
    {
        Profiler.BeginSample("ParticleInCell");
        ParticleInCell();
        Profiler.EndSample();

        Profiler.BeginSample("Remesh");
        m_Grid.Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateGridValue");
        m_Grid.UpdateGridValue();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        SparseMultiGridRedBlackGaussSeidel();
        Profiler.EndSample();
    }

    private void ParticleInCell()
    {
        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("DeleteParticleOutofRange");
        m_DynamicParticle.DeleteParticleOutofRange(m_Min, m_Max, m_CellLength);
        Profiler.EndSample();

        Profiler.BeginSample("OrganizeParticle");
        m_DynamicParticle.OrganizeParticle();
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToParticle");
        m_ParticleInCellTools.GatherGridToParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        //TODO: advect particle

        //Profiler.BeginSample("ZSortOnlyTallCellparticle");
        //m_ParticleSortTools.SortOnlyTallCellParticle(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength);
        //Profiler.EndSample();

        //Profiler.BeginSample("OnlyTallCellParticleToGrid");
        //m_ParticleInCellTools.ScatterOnlyTallCellParticleToGrid(m_DynamicParticle, m_Grid, m_SimulatorGPUCache);
        //Profiler.EndSample();

        //Profiler.BeginSample("ZSortOtherParticle");
        //m_ParticleSortTools.SortRegularCellParticle(m_DynamicParticle, m_Grid, m_SimulatorGPUCache, m_Min, m_CellLength);
        //Profiler.EndSample();

        //Profiler.BeginSample("SortRegularCellParticle");
        //m_ParticleInCellTools.ScatterRegularParticleToGrid(m_DynamicParticle, m_Grid);
        //Profiler.EndSample();

        //TODO: update mark for fine level
    }

    private void SparseMultiGridRedBlackGaussSeidel()
    {
        //multi grid gauss-seidel
    }

    private Vector3 m_Min;
    private Vector3 m_Max;
    private float m_CellLength;

    private Grid m_Grid;
    private DynamicParticle m_DynamicParticle;
    private ParticleInCellTools m_ParticleInCellTools;
    private ParticleSortTools m_ParticleSortTools;
    private Utils m_Utils;

    private SimulatorGPUCache m_SimulatorGPUCache;
}
