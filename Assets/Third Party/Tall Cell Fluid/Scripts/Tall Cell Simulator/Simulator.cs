using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class Simulator
{
    public static int OnlyTallCellParticleTypeIndex {get{ return 3; }}
    public static int IntersectCellParticleTypeIndex { get{ return 1; }}
    public static int OnlyRegularCellParticleTypeIndex { get{ return 0; }}
    public static int ScatterOnlyTallCellParticleArgmentOffset { get{ return 0; }}

    public Simulator(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellYCount, Vector3 vMin, float vCellLength, float vSeaLevel, int vMaxParticleCount)
    {
        m_Min = vMin;
        m_CellLength = vCellLength;

        m_Grid = new Grid(vResolutionXZ, vRegularCellYCount, vCellLength);

        m_Utils = new Utils();
        m_SimulatorGPUCache = new SimulatorGPUCache(vMaxParticleCount, vResolutionXZ);
        m_ParticleSortTools = new ParticleSortTools();
        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 4.0f);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellYCount);

        m_Grid.InitMesh(vTerrian, vSeaLevel);
        m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_Grid.FineGrid, vSeaLevel, m_DynamicParticle);
        m_Grid.UpdateGridValue();
    }

    public void SetupParticleDataForReconstruction(ParticleData vParticleData)
    {
        vParticleData.PositionBuffer = m_DynamicParticle.MainParticle.Position;
        vParticleData.ArgumentBuffer = m_DynamicParticle.Argument;
    }

    public void VisualParticle(Material vMaterial)
    {
        Profiler.BeginSample("VisualParticle");
        vMaterial.SetPass(0);
        vMaterial.SetBuffer("_particlePositionBuffer", m_DynamicParticle.MainParticle.Position);
        vMaterial.SetBuffer("_particleVelocityBuffer", m_DynamicParticle.MainParticle.Velocity);
        vMaterial.SetBuffer("_particleFilterBuffer", m_DynamicParticle.MainParticle.Filter);
        Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_DynamicParticle.Argument, 12);
        Profiler.EndSample();
    }

    public void VisualGrid(VisualGridInfo vVisualGridInfo)
    {
        m_Grid.VisualGrid(vVisualGridInfo, m_Min);
    }

    public void GenerateRandomVelicty()
    {
        GridPerLevel FineGrid = m_Grid.FineGrid;
        Texture2D Top = new Texture2D(FineGrid.ResolutionXZ.x, FineGrid.ResolutionXZ.y, TextureFormat.RGBAFloat, false);
        Texture2D Bottom = new Texture2D(FineGrid.ResolutionXZ.x, FineGrid.ResolutionXZ.y, TextureFormat.RGBAFloat, false);
        if (Top.width != Bottom.width || Top.height != Bottom.height)
            Debug.LogError("unmatched top and bottom data!");

        for(int x = 0; x < Top.width; x++)
        {
            for (int y = 0; y < Top.height; y++)
            {
                Color TopVelocity = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                Color BottomVelocity = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                //Color TopVelocity = new Color(x / (float)Top.width * 10.0f, 0.0f, 0.0f, 1.0f);
                //Color BottomVelocity = new Color(x / (float)Top.width * 10.0f, 0.0f, 0.0f, 1.0f);
                Top.SetPixel(x, y, TopVelocity);
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
                    Color RegularVelocity = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                    //Color RegularVelocity = new Color(x / (float)Top.width * 10.0f, 0.0f, 0.0f, 1.0f);
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
        __ParticleInCell(vTimeStep);
        Profiler.EndSample();

        Profiler.BeginSample("Remesh");
        m_Grid.Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateGridValue");
        m_Grid.UpdateGridValue();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        m_Grid.SparseMultiGridRedBlackGaussSeidel(vTimeStep, 128);
        Profiler.EndSample();
    }

    //Send velocity from grid to particle, advect and then send velocity from particle to grid. During this phase, we will get water mark, air mark of grid
    private void __ParticleInCell(float vTimeStep)
    {
        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("OrganizeParticle");
        m_DynamicParticle.OrganizeParticle();
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToOnlyRegularParticle");
        m_ParticleInCellTools.GatherGridToOnlyRegularParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToIntersectCellParticle");
        m_ParticleInCellTools.GatherGridToIntersectCellParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToOnlyTallCellParticle");
        m_ParticleInCellTools.GatherGridToOnlyTallCellParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("Advect");
        m_ParticleInCellTools.Advect(m_DynamicParticle, vTimeStep);
        Profiler.EndSample();

        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("OrganizeParticle");
        m_DynamicParticle.OrganizeParticle();
        Profiler.EndSample();

        Profiler.BeginSample("ClearCache");
        m_Grid.RestCache();
        Profiler.EndSample();

        Profiler.BeginSample("ZSortIntersectCellParticleHashTallCell");
        m_ParticleSortTools.SortParticleHashTallCell(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength, IntersectCellParticleTypeIndex);
        Profiler.EndSample();

        Profiler.BeginSample("ZSortOnlyTallCellparticleHashTallCell");
        m_ParticleSortTools.SortParticleHashTallCell(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength, OnlyTallCellParticleTypeIndex);
        Profiler.EndSample();

        Profiler.BeginSample("ScatterParticleToTallCellGrid");
        m_ParticleInCellTools.ScatterParticleToTallCellGrid(m_DynamicParticle, m_Grid);
        Profiler.EndSample();

        Profiler.BeginSample("ZSortRegularCellParticleHashRegular");
        m_ParticleSortTools.SortParticleHashRegular(m_DynamicParticle, m_Grid, m_SimulatorGPUCache, m_Min, m_CellLength, OnlyRegularCellParticleTypeIndex);
        Profiler.EndSample();

        Profiler.BeginSample("ZSortIntersectCellParticleHashRegular");
        m_ParticleSortTools.SortParticleHashRegular(m_DynamicParticle, m_Grid, m_SimulatorGPUCache, m_Min, m_CellLength, IntersectCellParticleTypeIndex);
        Profiler.EndSample();

        Profiler.BeginSample("ScatterParticleToRegularGrid");
        m_Utils.ClearIntTexture3D(m_Grid.FineGrid.RegularCellMark);
        m_ParticleInCellTools.ScatterParticleToRegularGrid(m_DynamicParticle, m_Grid);
        Profiler.EndSample();

        Profiler.BeginSample("ComputeH1H2WithParticle");
        m_Utils.ClearIntTexture2D(m_SimulatorGPUCache.WaterSurfaceMaxInterlockedCahce, int.MaxValue);
        m_Utils.ClearIntTexture2D(m_SimulatorGPUCache.WaterSurfaceMinInterlockedCahce, int.MinValue);
        m_ParticleSortTools.SortParticleHashTallCell(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength, -1);
        m_ParticleInCellTools.ComputeH1H2WithParticle(m_DynamicParticle, m_Grid, m_SimulatorGPUCache);
        Profiler.EndSample();

        Profiler.BeginSample("ParticlePostProcessing");
        m_ParticleSortTools.SortParticleHashFull(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength);
        Profiler.EndSample();
    }

    private Vector3 m_Min;
    private float m_CellLength;

    private Grid m_Grid;
    private DynamicParticle m_DynamicParticle;
    private ParticleInCellTools m_ParticleInCellTools;
    private ParticleSortTools m_ParticleSortTools;
    private Utils m_Utils;

    private SimulatorGPUCache m_SimulatorGPUCache;
}