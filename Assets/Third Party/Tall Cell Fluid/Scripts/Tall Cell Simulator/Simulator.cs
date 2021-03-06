using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class Simulator
{
    public static int OnlyTallCellParticleTypeIndex {get{ return 3; }}
    public static int IntersectCellParticleTypeIndex { get{ return 1; }}
    public static int OnlyRegularCellParticleTypeIndex { get{ return 0; }}
    public static int ScatterOnlyTallCellParticleArgmentOffset { get{ return 0; }}

    public Simulator(InitializationParameter vParam)
    {
        m_Min = vParam.m_Min;
        m_CellLength = vParam.m_CellLength;
        m_Max = m_Min + (new Vector3((int)vParam.m_ResolutionXZ, (int)vParam.m_RegularCellYCount * 8, (int)vParam.m_ResolutionXZ)) * m_CellLength;

        m_Grid = new Grid(new Vector2Int((int)vParam.m_ResolutionXZ, (int)vParam.m_ResolutionXZ), (int)vParam.m_RegularCellYCount, m_CellLength);
        m_Grid.InitMesh(vParam.m_Terrian, vParam.m_SeaLevel);

        m_Utils = new Utils();
        m_SimulatorGPUCache = new SimulatorGPUCache(vParam.m_MaxParticleCount, new Vector2Int((int)vParam.m_ResolutionXZ, (int)vParam.m_ResolutionXZ));
        m_ParticleSortTools = new ParticleSortTools();
        m_DynamicParticle = new DynamicParticle(vParam.m_MaxParticleCount, m_CellLength / 4.0f);
        m_ParticlePostProcessingTools = new PostProcessingParticle(vParam.m_MaxParticleCount, m_Min, m_CellLength, m_DynamicParticle.Argument, m_SimulatorGPUCache.HashCount, m_SimulatorGPUCache.HashOffset);
        m_ParticleInCellTools = new ParticleInCellTools(m_Min, new Vector2Int((int)vParam.m_ResolutionXZ, (int)vParam.m_ResolutionXZ), m_CellLength, (int)vParam.m_RegularCellYCount);
        m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_Grid.FineGrid, vParam.m_SeaLevel, m_DynamicParticle);

        m_Argument = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] InitArgument = new uint[3] {
            1, 1, 1
        };
        m_Argument.SetData(InitArgument);

        m_Grid.UpdateGridValue();
    }

    public void SetupDataForReconstruction(Simulator2ReconstructionData vData, bool vComputeAnisotropyMatrix)
    {
        vData.ParticleArgumentBuffer = m_DynamicParticle.Argument;
        if(vComputeAnisotropyMatrix)
        {
            vData.PositionBuffer = m_ParticlePostProcessingTools.m_NarrowPositionBuffer;
            vData.AnisotropyBuffer = m_ParticlePostProcessingTools.m_AnisotropyBuffer;
        }
        else
        {
            vData.PositionBuffer = m_DynamicParticle.MainParticle.Position;
            vData.AnisotropyBuffer = null;
        }
        vData.MinPos = m_Min;
        vData.MaxPos = new Vector3(64, 32, 64);  //TODO:
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

        System.Random Rand = new System.Random();
        for(int x = 0; x < Top.width; x++)
        {
            for (int y = 0; y < Top.height; y++)
            {
                //Color TopVelocity = new Color((float)x / Top.width, 0.0f, (float)y / Top.height, 1.0f);
                //Color TopVelocity = new Color((float)x / Top.width, 0.0f, 0.0f, 1.0f);
                Color TopVelocity = new Color(0.0f, -4.0f, 0.0f, 1.0f);
                //Color TopVelocity = new Color(-1.0f, 0.0f, 0.0f, 1.0f);
                //Color TopVelocity = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                //Color TopVelocity = new Color(Random.Range(0.0f, 1.0f), 0.0f, 0.0f, 1.0f);
                Top.SetPixel(x, y, TopVelocity);
                Color BottomVelocity = new Color(0.0f, -4.0f, 0.0f, 1.0f);
                //Color BottomVelocity = new Color(-1.0f, 0.0f, 0.0f, 1.0f);
                //Color BottomVelocity = new Color(1.0f, 0.0f, 0.0f, 1.0f);
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
                    //Color RegularVelocity = new Color((float)x / FineGrid.ResolutionXZ.x, 0.0f, (float)z / FineGrid.ResolutionXZ.y, 1.0f);
                    //Color RegularVelocity = new Color((float)x / FineGrid.ResolutionXZ.x, 0.0f, 0.0f, 1.0f);
                    //Color RegularVelocity = new Color(-1.0f, 0.0f, 0.0f, 1.0f);
                    //Color RegularVelocity = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    Color RegularVelocity = new Color(0.0f, -4.0f, 0.0f, 1.0f);
                    //Color RegularVelocity = new Color(Random.Range(0.0f, 1.0f), 0.0f, 0.0f, 1.0f);
                    Regular.SetPixel(x, y, z, RegularVelocity);
                }
            }
        }
        Regular.Apply();
        m_Utils.CopyFloat4Texture3DToAnother(Regular, FineGrid.Velocity.RegularCellValue);
    }

    uint FrameIndex = 0;
    public void Step(float vTimeStep, RuntimeParameter vRuntimeParam)
    {
        FrameIndex++;
        //if (FrameIndex != 1)
        //{
        //    m_ParticlePostProcessingTools.computeAnisotropyMatrix(m_DynamicParticle.MainParticle.Position, vRuntimeParam.m_PCAIterationNum);
        //    return;
        //}

        Profiler.BeginSample("ParticleInCell");
        __ParticleInCell(vTimeStep, vRuntimeParam);
        Profiler.EndSample();

        Profiler.BeginSample("Remesh");
        m_Grid.Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("UpdateGridValue");
        m_Grid.UpdateGridValue();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        m_Grid.SparseMultiGridRedBlackGaussSeidel(vTimeStep, 1);
        Profiler.EndSample();
    }

    private void __ParticleInCell(float vTimeStep, RuntimeParameter vRuntimeParam)
    {
        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_Grid.FineGrid);
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

        Profiler.BeginSample("Advect");
        m_ParticleInCellTools.Advect(m_DynamicParticle, vTimeStep);
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

        if(vRuntimeParam.m_ComputeAnisotropyMatrix)
        {
            Profiler.BeginSample("ParticlePostProcessing");
            m_ParticleSortTools.SortParticleHashFull(m_DynamicParticle, m_SimulatorGPUCache, m_Min, m_CellLength);
            m_ParticlePostProcessingTools.computeAnisotropyMatrix(m_DynamicParticle.MainParticle.Position, vRuntimeParam.m_PCAIterationNum);
            Profiler.EndSample();
        }
    }

    private Vector3 m_Min;
    private Vector3 m_Max;
    private float m_CellLength;

    private Grid m_Grid;
    private DynamicParticle m_DynamicParticle;
    private ParticleInCellTools m_ParticleInCellTools;
    private ParticleSortTools m_ParticleSortTools;
    private PostProcessingParticle m_ParticlePostProcessingTools;
    private Utils m_Utils;

    private ComputeBuffer m_Argument;
    private SimulatorGPUCache m_SimulatorGPUCache;
}
