using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;
using UnityEngine.Profiling;

public class ParticleInCellTools
{
    public ParticleInCellTools(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_ParticleInCellToolsCS = Resources.Load<ComputeShader>(Common.ParticleInCellToolsCSPath);
        markParticleByCellType = m_ParticleInCellToolsCS.FindKernel("markParticleByCellType");
        gatherGridToParticle = m_ParticleInCellToolsCS.FindKernel("gatherGridToParticle");
        scatterOnlyTallCellParticleToGrid_Pass1 = m_ParticleInCellToolsCS.FindKernel("scatterOnlyTallCellParticleToGrid_Pass1");
        scatterOnlyTallCellParticleToGrid_Pass2 = m_ParticleInCellToolsCS.FindKernel("scatterOnlyTallCellParticleToGrid_Pass2");
        gatherOnlyTallCellParticleToGrid = m_ParticleInCellToolsCS.FindKernel("gatherOnlyTallCellParticleToGrid");
        UpdateGlobalParma(vMin, vResolutionXZ, vCellLength, vConstantCellNum);
    }

    public void UpdateGlobalParma(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_ParticleInCellToolsCS.SetFloats("Min", vMin.x, vMin.y, vMin.z);
        m_ParticleInCellToolsCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_ParticleInCellToolsCS.SetFloat("CellLength", vCellLength);
        m_ParticleInCellToolsCS.SetInt("ConstantCellNum", vConstantCellNum);

        m_GPUGroupCount2D.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount2D);
        m_GPUGroupCount2D.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount2D);

        m_GPUGroupCount3D.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount3D);
        m_GPUGroupCount3D.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount3D);
        m_GPUGroupCount3D.z = Mathf.CeilToInt((float)vConstantCellNum / Common.ThreadCount3D);
    }

    public void MarkParticleWtihCellType(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetInt("ParticleCountOffset", DynamicParticle.ParticleCountArgumentOffset);

        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticleIndrectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(markParticleByCellType, "ParticleFilter_RW", vParticle.MainParticle.Filter);
        m_ParticleInCellToolsCS.SetTexture(markParticleByCellType, "TerrianHeight_R", vTargetLevel.TerrrianHeight);
        m_ParticleInCellToolsCS.SetTexture(markParticleByCellType, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.DispatchIndirect(markParticleByCellType, vParticle.Argument);
    }

    public void GatherGridToParticle(DynamicParticle vParticle, GridPerLevel vTargetLevel)
    {
        m_ParticleInCellToolsCS.SetInt("ParticleCountOffset", DynamicParticle.ParticleCountArgumentOffset);

        m_ParticleInCellToolsCS.SetBuffer(gatherGridToParticle, "ParticleIndrectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToParticle, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToParticle, "ParticleVelocity_RW", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetBuffer(gatherGridToParticle, "ParticleFilter_RW", vParticle.MainParticle.Filter);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToParticle, "TerrianHeight_R", vTargetLevel.TerrrianHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToParticle, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToParticle, "TopCellVelocity_R", vTargetLevel.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToParticle, "BottomCellVelocity_R", vTargetLevel.Velocity.TallCellBottomValue);
        m_ParticleInCellToolsCS.SetTexture(gatherGridToParticle, "RegularCellVelocity_R", vTargetLevel.Velocity.RegularCellValue);
        m_ParticleInCellToolsCS.DispatchIndirect(gatherGridToParticle, vParticle.Argument);
    }

    public void GatherOnlyTallCellParticleToGrid(DynamicParticle vParticle, GridPerLevel vTargetLevel, SimulatorGPUCache vCache)
    {
        m_ParticleInCellToolsCS.SetBuffer(gatherOnlyTallCellParticleToGrid, "HashGridCellParticleCount_R", vCache.HashCount);
        m_ParticleInCellToolsCS.SetBuffer(gatherOnlyTallCellParticleToGrid, "HashGridCellParticleOffset_R", vCache.HashOffset);
        m_ParticleInCellToolsCS.SetBuffer(gatherOnlyTallCellParticleToGrid, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(gatherOnlyTallCellParticleToGrid, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(gatherOnlyTallCellParticleToGrid, "TerrianHeight_R", vTargetLevel.TerrrianHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherOnlyTallCellParticleToGrid, "TallCellHeight_R", vTargetLevel.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(gatherOnlyTallCellParticleToGrid, "TopCellVelocity_RW", vTargetLevel.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(gatherOnlyTallCellParticleToGrid, "BottomCellVelocity_RW", vTargetLevel.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.Dispatch(gatherOnlyTallCellParticleToGrid, Mathf.CeilToInt(vTargetLevel.ResolutionXZ.x / Common.ThreadCount2D), Mathf.CeilToInt(vTargetLevel.ResolutionXZ.y / Common.ThreadCount2D), 1);
    }

    public void ScatterOnlyTallCellParticleToGrid(DynamicParticle vParticle, Grid vTargetGrid, SimulatorGPUCache vCache)
    {
        m_ParticleInCellToolsCS.SetInt("ParticleCountOffset", DynamicParticle.ParticleCountArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("ParticleCountArgumentIndex", DynamicParticle.ParticleCountArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("ParticleOffsetArgumentIndex", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        m_ParticleInCellToolsCS.SetInt("OnlyTallCellParticleType", Simulator.OnlyTallCellParticleTypeIndex);

        Profiler.BeginSample("Pass1");
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyTallCellParticleToGrid_Pass1, "ParticleIndrectArgment_R", vParticle.Argument);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyTallCellParticleToGrid_Pass1, "ParticlePosition_R", vParticle.MainParticle.Position);
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyTallCellParticleToGrid_Pass1, "ParticleVelocity_R", vParticle.MainParticle.Velocity);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "TerrianHeight_R", vTargetGrid.FineGrid.TerrrianHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "XSum_RW", vTargetGrid.GPUCache.TallCellHeightSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "XXSum_RW", vTargetGrid.GPUCache.TallCellPow2HeightSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "YSum_R_RW", vTargetGrid.GPUCache.TallCellVelocityXSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "YSum_G_RW", vTargetGrid.GPUCache.TallCellVelocityYSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "YSum_B_RW", vTargetGrid.GPUCache.TallCellVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "XYSum_R_RW", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "XYSum_G_RW", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass1, "XYSum_B_RW", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.DispatchIndirect(scatterOnlyTallCellParticleToGrid_Pass1, vParticle.Argument, ((uint)DynamicParticle.DifferParticleXGridCountArgumentOffset + (uint)Simulator.OnlyTallCellParticleTypeIndex * 3) * sizeof(uint));
        Profiler.EndSample();

        Profiler.BeginSample("Pass2");
        m_ParticleInCellToolsCS.SetBuffer(scatterOnlyTallCellParticleToGrid_Pass2, "HashGridCellParticleCount_R", vCache.HashCount);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "TerrianHeight_R", vTargetGrid.FineGrid.TerrrianHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "TallCellHeight_R", vTargetGrid.FineGrid.TallCellHeight);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "XSum_R", vTargetGrid.GPUCache.TallCellHeightSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "XXSum_R", vTargetGrid.GPUCache.TallCellPow2HeightSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "YSum_R_R", vTargetGrid.GPUCache.TallCellVelocityXSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "YSum_G_R", vTargetGrid.GPUCache.TallCellVelocityYSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "YSum_B_R", vTargetGrid.GPUCache.TallCellVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "XYSum_R_R", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "XYSum_G_R", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "XYSum_B_R", vTargetGrid.GPUCache.TallCellHeightVelocityZSumCahce);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "TopCellVelocity_RW", vTargetGrid.FineGrid.Velocity.TallCellTopValue);
        m_ParticleInCellToolsCS.SetTexture(scatterOnlyTallCellParticleToGrid_Pass2, "BottomCellVelocity_RW", vTargetGrid.FineGrid.Velocity.TallCellBottomValue);
        m_ParticleInCellToolsCS.Dispatch(scatterOnlyTallCellParticleToGrid_Pass2, Mathf.CeilToInt(vTargetGrid.FineGrid.ResolutionXZ.x / Common.ThreadCount2D), Mathf.CeilToInt(vTargetGrid.FineGrid.ResolutionXZ.y / Common.ThreadCount2D), 1);
        Profiler.EndSample();
}

    public void InitParticleDataWithSeaLevel(GridPerLevel vFineLayer, float vSeaLevel, DynamicParticle voTarget)
    {
        Texture2D TerrianHeight = Common.CopyRenderTextureToCPU(vFineLayer.TerrrianHeight);
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
        System.Random Rand = new System.Random();
        float Step = vCellLength / vParticleInCellRes;
        for(int x = 0; x < vParticleInCellRes; x++)
        {
            for (int y = 0; y < vParticleInCellRes; y++)
            {
                for (int z = 0; z < vParticleInCellRes; z++)
                {
                    Vector3 SubCellMin = vCellMin + new Vector3(x, y, z) * Step;
                    if (SubCellMin.y > vTop)
                        continue;

                    Vector3 Podition = SubCellMin + new Vector3(Step * (float)Rand.NextDouble(), Step * (float)Rand.NextDouble(), Step * (float)Rand.NextDouble());
                    voPosition.Add(Podition);
                    voVelocity.Add(new Vector3(0, 0, 0));
                    voFilter.Add(0);
                }
            }
        }
    }

    private Vector2Int m_GPUGroupCount2D;
    private Vector3Int m_GPUGroupCount3D;
    private ComputeShader m_ParticleInCellToolsCS;
    private int markParticleByCellType;
    private int gatherGridToParticle;
    private int scatterOnlyTallCellParticleToGrid_Pass1;
    private int scatterOnlyTallCellParticleToGrid_Pass2;
    private int gatherOnlyTallCellParticleToGrid;
}
