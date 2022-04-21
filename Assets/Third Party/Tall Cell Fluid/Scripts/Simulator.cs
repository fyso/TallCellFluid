using System.Collections.Generic;
using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class Simulator
{
    public DynamicParticle DynamicParticle { get { return m_DynamicParticle; } } //TODO: Cannot return directly, breaking encapsulation.
    public GridPerLevel FineGrid { get { return m_Grid.FineGrid; } }

    public Simulator(Texture vTerrian, Vector2Int vResolutionXZ, int vRegularCellYCount, Vector3 vMin, float vCellLength, float vSeaLevel, int vMaxParticleCount)
    {
        m_Min = vMin;
        m_CellLength = vCellLength;
        m_Max = vMin + (new Vector3(vResolutionXZ.x, vRegularCellYCount * 32, vResolutionXZ.y)) * vCellLength;

        m_Grid = new Grid(vResolutionXZ, vRegularCellYCount, vCellLength);
        m_Grid.InitMesh(vTerrian, vSeaLevel);

        m_DynamicParticle = new DynamicParticle(vMaxParticleCount, vCellLength / 4.0f);
        m_ParticleInCellTools = new ParticleInCellTools(vMin, vResolutionXZ, vCellLength, vRegularCellYCount);
        m_ParticleInCellTools.InitParticleDataWithSeaLevel(m_Grid.FineGrid, vSeaLevel, m_DynamicParticle);

        m_Grid.UpdateGridValue();
    }

    public void Step(float vTimeStep)
    {
        Profiler.BeginSample("Remesh");
        m_Grid.Remesh();
        Profiler.EndSample();

        Profiler.BeginSample("Advect");
        Advect();
        Profiler.EndSample();

        Profiler.BeginSample("Remesh");
        m_Grid.UpdateGridValue();
        Profiler.EndSample();

        Profiler.BeginSample("SparseMultiGridRedBlackGaussSeidel");
        SparseMultiGridRedBlackGaussSeidel();
        Profiler.EndSample();
    }

    //TODO: move Advect() to DynamicParticle if possible, and replace comments with more accurate function name.
    private void Advect()
    {
        //split particle by cell type
        Profiler.BeginSample("MarkParticleWtihCellType");
        m_ParticleInCellTools.MarkParticleWtihCellType(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        Profiler.BeginSample("DeleteParticleOutofRange");
        m_DynamicParticle.DeleteParticleOutofRange(m_Min, m_Max, m_CellLength);
        Profiler.EndSample();

        Profiler.BeginSample("OrganizeParticle");
        m_DynamicParticle.OrganizeParticle();
        Profiler.EndSample();

        //grid to particle using fine level
        Profiler.BeginSample("Gather Grid To Particle");
        m_ParticleInCellTools.GatherGridToParticle(m_DynamicParticle, m_Grid.FineGrid);
        Profiler.EndSample();

        //TODO: advect particle

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

    private Vector3 m_Min;
    private Vector3 m_Max;
    private float m_CellLength;

    private Grid m_Grid;

    //TODO: move ParticleInCellTools into DynamicParticle
    private DynamicParticle m_DynamicParticle;
    private ParticleInCellTools m_ParticleInCellTools;
}
