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
        m_Max = vMin + (new Vector3(vResolutionXZ.x, vRegularCellYCount * 32, vResolutionXZ.y)) * vCellLength;

        m_Grid = new Grid(vResolutionXZ, vRegularCellYCount, vCellLength);
        m_Grid.InitMesh(vTerrian, vSeaLevel);

        m_Utils = new Utils();
        m_SimulatorGPUCache = new SimulatorGPUCache(vMaxParticleCount, vResolutionXZ);
        m_ParticleSortTools = new ParticleSortTools();
        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 4.0f);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellYCount);
        m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_Grid.FineGrid, vSeaLevel, m_DynamicParticle);

        m_Argument = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] InitArgument = new uint[3] {
                1, 1, 1
        };
        m_Argument.SetData(InitArgument);

        m_Grid.UpdateGridValue();
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

    public void VisualGrid(VisualGridInfo VisualGridInfo)
    {
        m_Grid.VisualGrid(VisualGridInfo, m_Min);
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
                Color TopVelocity = new Color((float)x / Top.width, 0.0f, (float)y / Top.height, 1.0f);
                //Color TopVelocity = new Color((float)x / Top.width, 0.0f, 0.0f, 1.0f);
                //Color TopVelocity = new Color(Random.Range(0.0f, 1.0f), 0.0f, 0.0f, 1.0f);
                Top.SetPixel(x, y, TopVelocity);
                Color BottomVelocity = new Color(0.8f, 0, 0, 1.0f);
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
                    Color RegularVelocity = new Color((float)x / FineGrid.ResolutionXZ.x, 0.0f, (float)z / FineGrid.ResolutionXZ.y, 1.0f);
                    //Color RegularVelocity = new Color((float)x / FineGrid.ResolutionXZ.x, 0.0f, 0.0f, 1.0f);
                    //Color RegularVelocity = new Color(Random.Range(0.0f, 1.0f), 0.0f, 0.0f, 1.0f);
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
        __ParticleInCell();
        Profiler.EndSample();

        Profiler.BeginSample("Remesh");
        m_Grid.Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateGridValue");
        m_Grid.UpdateGridValue();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        m_Grid.SparseMultiGridRedBlackGaussSeidel();
        Profiler.EndSample();
    }

    private void __ParticleInCell()
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

        m_Utils.UpdateArgment(m_Argument, m_DynamicParticle.Argument, DynamicParticle.DifferParticleXGridCountArgumentOffset + OnlyTallCellParticleTypeIndex * 3, ScatterOnlyTallCellParticleArgmentOffset);

        Profiler.BeginSample("GatherGridToOnlyRegularParticle");
        m_ParticleInCellTools.GatherGridToOnlyRegularParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToIntersectCellParticle");
        m_ParticleInCellTools.GatherGridToIntersectCellParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("GatherGridToOnlyTallCellParticle");
        m_ParticleInCellTools.GatherGridToOnlyTallCellParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        //TODO: advect particle

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
        m_ParticleInCellTools.ScatterParticleToRegularGrid(m_DynamicParticle, m_Grid);
        Profiler.EndSample();

        //TODO: update mark for fine level
    }

    private Vector3 m_Min;
    private Vector3 m_Max;
    private float m_CellLength;

    private Grid m_Grid;
    private DynamicParticle m_DynamicParticle;
    private ParticleInCellTools m_ParticleInCellTools;
    private ParticleSortTools m_ParticleSortTools;
    private Utils m_Utils;

    private ComputeBuffer m_Argument;

    private SimulatorGPUCache m_SimulatorGPUCache;
}
