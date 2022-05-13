using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;
using UnityEngine.Profiling;
using System;

//TODO: too many tall cell particles heavily solw down the speed of p2g/g2p, so we choose to
//delete the only tall cell particle after least squares method (we need choose a new least squares method)
public class ParticleInCellTools
{
    public ParticleInCellTools(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
    {
        m_ParticleInCellToolsCS = Resources.Load<ComputeShader>(Common.ParticleInCellToolsCSPath);
        advect = m_ParticleInCellToolsCS.FindKernel("advect");
        markParticleByCellType = m_ParticleInCellToolsCS.FindKernel("markParticleByCellType");
        gatherGridToOnlyRegularParticle = m_ParticleInCellToolsCS.FindKernel("gatherGridToOnlyRegularParticle");
        gatherGridToIntersectCellParticle = m_ParticleInCellToolsCS.FindKernel("gatherGridToIntersectCellParticle");
        gatherGridToOnlyTallCellParticle = m_ParticleInCellToolsCS.FindKernel("gatherGridToOnlyTallCellParticle");
        scatterParticleToTallCellGrid_Pass1 = m_ParticleInCellToolsCS.FindKernel("scatterParticleToTallCellGrid_Pass1");
        scatterParticleToTallCellGrid_Pass2 = m_ParticleInCellToolsCS.FindKernel("scatterParticleToTallCellGrid_Pass2");
        scatterOnlyRegularParticleToGrid_Pass1 = m_ParticleInCellToolsCS.FindKernel("scatterOnlyRegularParticleToGrid_Pass1");
        scatterOnlyRegularParticleToGrid_Pass2 = m_ParticleInCellToolsCS.FindKernel("scatterOnlyRegularParticleToGrid_Pass2");
        computeH1H2WithParticle_Pass1 = m_ParticleInCellToolsCS.FindKernel("computeH1H2WithParticle_Pass1");
        computeH1H2WithParticle_Pass2 = m_ParticleInCellToolsCS.FindKernel("computeH1H2WithParticle_Pass2");
        UpdateGlobalParma(vMin, vResolutionXZ, vCellLength, vRegularCellYCount);
    }

    public void Advect(DynamicParticle vParticle, float vTimeStep)
    {
        m_ParticleInCellToolsCS.SetFloat("TimeStep", vTimeStep);
        m_ParticleInCellToolsCS.SetBuffer(advect, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(advect, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetBuffer(advect, "ParticlePosition_RW", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.DispatchIndirect(advect, vParticle.Argument);
    }

    public void UpdateGlobalParma(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
    {
        m_ParticleInCellToolsCS.SetFloats("Min", vMin.x, vMin.y, vMin.z);
        m_ParticleInCellToolsCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_ParticleInCellToolsCS.SetFloat("CellLength", vCellLength);
        m_ParticleInCellToolsCS.SetInt("ConstantCellNum", vRegularCellYCount);

        m_ParticleInCellToolsCS.SetInt("ParticleCountOffset", DynamicParticle.ParticleCountArgumentOffset);

        m_ParticleInCellToolsCS.SetInt("ParticleCountArgumentOffset", DynamicParticle.ParticleCountArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("DifferParticleSplitPointArgumentOffset", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("DifferParticleCountArgumentOffset", DynamicParticle.DifferParticleCountArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("OnlyRegularCellParticleType", Simulator.OnlyRegularCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetInt("IntersectCellParticleType", Simulator.IntersectCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetInt("OnlyTallCellParticleType", Simulator.OnlyTallCellParticleTypeIndex);

        OnlyRegularCellParticleArgumentOffset = ((uint)DynamicParticle.DifferParticleXGridCountArgumentOffset + (uint)Simulator.OnlyRegularCellParticleTypeIndex * 3) * sizeof(uint);
        IntersectCellParticleArgumentOffset = ((uint)DynamicParticle.DifferParticleXGridCountArgumentOffset + (uint)Simulator.IntersectCellParticleTypeIndex * 3) * sizeof(uint);
        OnlyTallCellParticleArgumentOffset = ((uint)DynamicParticle.DifferParticleXGridCountArgumentOffset + (uint)Simulator.OnlyTallCellParticleTypeIndex * 3) * sizeof(uint);
    }

    public void MarkParticleWtihCellType(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticleFilter_RW", vParticle.MainParticle.Filter);
        m_ParticleInCellToolsCS.SetTexture(markParticleByCellType, "TerrianHeight_R", vTargetLevel.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(markParticleByCellType, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.DispatchIndirect(markParticleByCellType, vParticle.Argument);
    }

    public void GatherGridToOnlyRegularParticle(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyRegularParticle, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyRegularParticle, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyRegularParticle, "ParticleVelocity_RW", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyRegularParticle, "TerrianHeight_R", vTargetLevel.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyRegularParticle, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyRegularParticle, "RegularCellVelocity_R", vTargetLevel.Velocity.RegularCellValue);
        m_ParticleInCellToolsCS.DispatchIndirect(gatherGridToOnlyRegularParticle, vParticle.Argument, OnlyRegularCellParticleArgumentOffset);
    }
    
    public void GatherGridToIntersectCellParticle(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToIntersectCellParticle, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToIntersectCellParticle, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToIntersectCellParticle, "ParticleVelocity_RW", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToIntersectCellParticle, "TerrianHeight_R", vTargetLevel.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToIntersectCellParticle, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToIntersectCellParticle, "RegularCellVelocity_R", vTargetLevel.Velocity.RegularCellValue);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToIntersectCellParticle, "TopCellVelocity_R", vTargetLevel.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToIntersectCellParticle, "BottomCellVelocity_R", vTargetLevel.Velocity.TallCellBottomValue);
        m_ParticleInCellToolsCS.DispatchIndirect(gatherGridToIntersectCellParticle, vParticle.Argument, IntersectCellParticleArgumentOffset);
    }

    public void GatherGridToOnlyTallCellParticle(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyTallCellParticle, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyTallCellParticle, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToOnlyTallCellParticle, "ParticleVelocity_RW", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyTallCellParticle, "TerrianHeight_R", vTargetLevel.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyTallCellParticle, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyTallCellParticle, "TopCellVelocity_R", vTargetLevel.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToOnlyTallCellParticle, "BottomCellVelocity_R", vTargetLevel.Velocity.TallCellBottomValue);
        m_ParticleInCellToolsCS.DispatchIndirect(gatherGridToOnlyTallCellParticle, vParticle.Argument, OnlyTallCellParticleArgumentOffset);
    }

    public void ScatterParticleToTallCellGrid(DynamicParticle vParticle, Grid vTargetGrid)
    {
        Profiler.BeginSample("Pass1");
        m_ParticleInCellToolsCS.SetInt("TargetParticleType", Simulator.OnlyTallCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "ParticleCount_RW", vTargetGrid.GPUCache.TallCellParticleCountCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XSum_RW", vTargetGrid.GPUCache.TallCellScalarCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XXSum_RW", vTargetGrid.GPUCache.TallCellScalarCahce2);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "YSum_RW", vTargetGrid.GPUCache.TallCellVectorCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XYSum_RW", vTargetGrid.GPUCache.TallCellVectorCahce2);
        m_ParticleInCellToolsCS.DispatchIndirect(scatterParticleToTallCellGrid_Pass1, vParticle.Argument, OnlyTallCellParticleArgumentOffset);
        Profiler.EndSample();

        Profiler.BeginSample("Pass2");
        m_ParticleInCellToolsCS.SetInt("TargetParticleType", Simulator.IntersectCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(scatterParticleToTallCellGrid_Pass1, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "ParticleCount_RW", vTargetGrid.GPUCache.TallCellParticleCountCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XSum_RW", vTargetGrid.GPUCache.TallCellScalarCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XXSum_RW", vTargetGrid.GPUCache.TallCellScalarCahce2);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "YSum_RW", vTargetGrid.GPUCache.TallCellVectorCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass1, "XYSum_RW", vTargetGrid.GPUCache.TallCellVectorCahce2);
        m_ParticleInCellToolsCS.DispatchIndirect(scatterParticleToTallCellGrid_Pass1, vParticle.Argument, IntersectCellParticleArgumentOffset);
        Profiler.EndSample();

        Profiler.BeginSample("Pass3");
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "ParticleCount_R", vTargetGrid.GPUCache.TallCellParticleCountCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "XSum_R", vTargetGrid.GPUCache.TallCellScalarCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "XXSum_R", vTargetGrid.GPUCache.TallCellScalarCahce2);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "YSum_R", vTargetGrid.GPUCache.TallCellVectorCahce1);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "XYSum_R", vTargetGrid.GPUCache.TallCellVectorCahce2);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "TopCellVelocity_RW", vTargetGrid.FineGrid.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(scatterParticleToTallCellGrid_Pass2, "BottomCellVelocity_RW", vTargetGrid.FineGrid.Velocity.TallCellBottomValue);
        m_ParticleInCellToolsCS.Dispatch(scatterParticleToTallCellGrid_Pass2, Mathf.CeilToInt(((float)vTargetGrid.FineGrid.ResolutionXZ.x) / Common.ThreadCount2D), Mathf.CeilToInt(((float)vTargetGrid.FineGrid.ResolutionXZ.y) / Common.ThreadCount2D), 1);
        Profiler.EndSample();
    }

    public void ScatterParticleToRegularGrid(DynamicParticle vParticle, Grid vTargetGrid)
    {
        Profiler.BeginSample("Pass1");
        m_ParticleInCellToolsCS.SetInt("TargetParticleType", Simulator.OnlyRegularCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeight_RW", vTargetGrid.GPUCache.RegularCellScalarCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_R_RW", vTargetGrid.GPUCache.RegularCellVectorXCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_G_RW", vTargetGrid.GPUCache.RegularCellVectorYCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_B_RW", vTargetGrid.GPUCache.RegularCellVectorZCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellMark_RW", vTargetGrid.FineGrid.RegularCellMark);
        m_ParticleInCellToolsCS.DispatchIndirect(scatterOnlyRegularParticleToGrid_Pass1, vParticle.Argument, OnlyRegularCellParticleArgumentOffset);
        Profiler.EndSample();

        Profiler.BeginSample("Pass1");
        m_ParticleInCellToolsCS.SetInt("TargetParticleType", Simulator.IntersectCellParticleTypeIndex);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyRegularParticleToGrid_Pass1, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeight_RW", vTargetGrid.GPUCache.RegularCellScalarCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_R_RW", vTargetGrid.GPUCache.RegularCellVectorXCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_G_RW", vTargetGrid.GPUCache.RegularCellVectorYCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellWeightedVelocity_B_RW", vTargetGrid.GPUCache.RegularCellVectorZCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass1, "RegularCellMark_RW", vTargetGrid.FineGrid.RegularCellMark);
        m_ParticleInCellToolsCS.DispatchIndirect(scatterOnlyRegularParticleToGrid_Pass1, vParticle.Argument, IntersectCellParticleArgumentOffset);
        Profiler.EndSample();

        Profiler.BeginSample("Pass2");
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass2, "RegularCellWeight_R", vTargetGrid.GPUCache.RegularCellScalarCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass2, "RegularCellWeightedVelocity_R_R", vTargetGrid.GPUCache.RegularCellVectorXCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass2, "RegularCellWeightedVelocity_G_R", vTargetGrid.GPUCache.RegularCellVectorYCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass2, "RegularCellWeightedVelocity_B_R", vTargetGrid.GPUCache.RegularCellVectorZCache);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyRegularParticleToGrid_Pass2, "RegularCellVelocity_RW", vTargetGrid.FineGrid.Velocity.RegularCellValue);
        m_ParticleInCellToolsCS.Dispatch(scatterOnlyRegularParticleToGrid_Pass2, Mathf.CeilToInt(((float)vTargetGrid.FineGrid.ResolutionXZ.x) / Common.ThreadCount3D), Mathf.CeilToInt(((float)vTargetGrid.FineGrid.RegularCellYCount) / Common.ThreadCount3D), Mathf.CeilToInt((float) vTargetGrid.FineGrid.ResolutionXZ.y / Common.ThreadCount3D));
        Profiler.EndSample();
    }

    public void ComputeH1H2WithParticle(DynamicParticle vParticle, Grid vTargetGrid, SimulatorGPUCache vCache)
    {
        Profiler.BeginSample("Pass1");
        m_ParticleInCellToolsCS.SetBuffer(computeH1H2WithParticle_Pass1, "ParticleIndirectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(computeH1H2WithParticle_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass1, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass1, "WaterSurfaceMin_RW", vCache.WaterSurfaceMinInterlockedCahce);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass1, "WaterSurfaceMax_RW", vCache.WaterSurfaceMaxInterlockedCahce);
        m_ParticleInCellToolsCS.DispatchIndirect(computeH1H2WithParticle_Pass1, vParticle.Argument);
        Profiler.EndSample();

        Profiler.BeginSample("Pass2");
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass2, "TerrianHeight_R", vTargetGrid.FineGrid.TerrainHeight);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass2, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass2, "WaterSurfaceMin_R", vCache.WaterSurfaceMinInterlockedCahce);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass2, "WaterSurfaceMax_R", vCache.WaterSurfaceMaxInterlockedCahce);
        m_ParticleInCellToolsCS.SetTexture(computeH1H2WithParticle_Pass2, "WaterSurfaceH1H2_RW", vTargetGrid.GPUCache.H1H2Cahce);
        m_ParticleInCellToolsCS.Dispatch(computeH1H2WithParticle_Pass2, Mathf.CeilToInt(((float)vTargetGrid.FineGrid.ResolutionXZ.x) / Common.ThreadCount2D), Mathf.CeilToInt(((float)vTargetGrid.FineGrid.ResolutionXZ.y) / Common.ThreadCount2D), 1);
        Profiler.EndSample();
    }

    public void InitParticleDataWithSeaLevel(GridPerLevel vFineLayer, float vSeaLevel, DynamicParticle voTarget)
    {
        Texture2D TerrianHeight = Common.CopyRenderTextureToCPU(vFineLayer.TerrainHeight);
        Texture2D TallCellHeight = Common.CopyRenderTextureToCPU(vFineLayer.TallCellHeight);

        List<Vector3> Position = new List<Vector3>();
        List<Vector3> Velocity = new List<Vector3>();
        List<int> Filter = new List<int>();
        int ParticleInCellRes = Mathf.FloorToInt(vFineLayer.CellLength / (voTarget.Radius * 2.0f));
        for (int x = 0; x < vFineLayer.ResolutionXZ.x; x++)
        {
            for (int z = 0; z < vFineLayer.ResolutionXZ.y; z++)
            {
                float CurrTerrianHeight = TerrianHeight.GetPixel(x, z).r;
                float CurrTallCellHeight = TallCellHeight.GetPixel(x, z).r;

                //add particle into tall cell
                int TallCellSlice = Mathf.CeilToInt(CurrTallCellHeight / vFineLayer.CellLength);
                Vector3 TallCellSliceMin = new Vector3(x * vFineLayer.CellLength, CurrTerrianHeight, z * vFineLayer.CellLength);
                for (int c = 0; c < TallCellSlice; c++)
                {
                    //if (!(TallCellSliceMin.y > CurrTerrianHeight + vFineLayer.CellLength * 3 && TallCellSliceMin.y < CurrTerrianHeight + CurrTallCellHeight - vFineLayer.CellLength * 3))
                        addParticleInCell(TallCellSliceMin, vFineLayer.CellLength, ParticleInCellRes, CurrTerrianHeight + CurrTallCellHeight, ref Position, ref Velocity, ref Filter);
                    TallCellSliceMin.y += vFineLayer.CellLength;
                }

                //add particle into regular cell
                float RegularCellHeight = vSeaLevel - CurrTallCellHeight - CurrTerrianHeight;
                int RegularCellSlice = Mathf.CeilToInt(RegularCellHeight / vFineLayer.CellLength);
                Vector3 RegularCellSliceMin = new Vector3(x * vFineLayer.CellLength, CurrTerrianHeight + CurrTallCellHeight, z * vFineLayer.CellLength);
                for (int c = 0; c < RegularCellSlice; c++)
                {
                    addParticleInCell(RegularCellSliceMin, vFineLayer.CellLength, ParticleInCellRes, vSeaLevel, ref Position, ref Velocity, ref Filter);
                    RegularCellSliceMin.y += vFineLayer.CellLength;
                }
            }
        }
        voTarget.SetData(Position, Velocity, Filter, Position.Count);
    }

    private void addParticleInCell(Vector3 vCellMin, float vCellLength, int vParticleInCellRes, float vTop, ref List<Vector3> voPosition, ref List<Vector3> voVelocity, ref List<int> voFilter)
    {
        float Step = vCellLength / vParticleInCellRes;
        for (int x = 0; x < vParticleInCellRes; x++)
        {
            for (int y = 0; y < vParticleInCellRes; y++)
            {
                for (int z = 0; z < vParticleInCellRes; z++)
                {
                    Vector3 SubCellMin = vCellMin + new Vector3(x, y, z) * Step;
                    if (SubCellMin.y > vTop)
                        continue;

                    Vector3 Position = SubCellMin + new Vector3(Step * UnityEngine.Random.Range(0.0f, 1.0f), Step * UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
                    voPosition.Add(Position);
                    voVelocity.Add(new Vector3(0, 0, 0));
                    voFilter.Add(0);
                }
            }
        }
    }

    private ComputeShader m_ParticleInCellToolsCS;
    private int advect;
    private int markParticleByCellType;
    private int gatherGridToOnlyRegularParticle;
    private int gatherGridToIntersectCellParticle;
    private int gatherGridToOnlyTallCellParticle;
    private int scatterParticleToTallCellGrid_Pass1;
    private int scatterParticleToTallCellGrid_Pass2;
    private int scatterOnlyRegularParticleToGrid_Pass1;
    private int scatterOnlyRegularParticleToGrid_Pass2;
    private int computeH1H2WithParticle_Pass1;
    private int computeH1H2WithParticle_Pass2;

    private uint OnlyRegularCellParticleArgumentOffset;
    private uint IntersectCellParticleArgumentOffset;
    private uint OnlyTallCellParticleArgumentOffset;
}
