using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;

public class ParticleInCellTools
{
    public ParticleInCellTools(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_ParticleInCellToolsCS = Resources.Load<ComputeShader>(Common.ParticleInCellToolsCSPath);
        scatterParticleToGrid_Paas1 = m_ParticleInCellToolsCS.FindKernel("scatterParticleToGrid_Paas1");
        scatterParticleToGrid_Paas2 = m_ParticleInCellToolsCS.FindKernel("scatterParticleToGrid_Paas2");
        scatterParticleToGrid_Paas3 = m_ParticleInCellToolsCS.FindKernel("scatterParticleToGrid_Paas3");
        UpdateGlobalParma(vMin, vResolutionXZ, vCellLength, vConstantCellNum);
    }

    public void UpdateGlobalParma(Vector3 vMin, Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_ParticleInCellToolsCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_ParticleInCellToolsCS.SetFloat("CellLength", vCellLength);
        m_ParticleInCellToolsCS.SetInt("ConstantCellNum", vConstantCellNum);

        m_GPUGroupCount2D.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount2D);
        m_GPUGroupCount2D.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount2D);

        m_GPUGroupCount3D.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount3D);
        m_GPUGroupCount3D.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount3D);
        m_GPUGroupCount3D.z = Mathf.CeilToInt((float)vConstantCellNum / Common.ThreadCount3D);
    }

    public void scatterParticleToGrid(DynamicParticle vInputParticle, GridPerLevel voTargetLayer, TallCellGridGPUCache vCache)
    {
        //m_ParticleInCellToolsCS.SetBuffer(scatterParticleToGrid_Paas1, "ParticleIndrectArgment_R", vInputParticle.Argument);
        //m_ParticleInCellToolsCS.SetBuffer(scatterParticleToGrid_Paas1, "ParticlePosition_R", vInputParticle.MainParticle.Position);
        //m_ParticleInCellToolsCS.SetBuffer(scatterParticleToGrid_Paas1, "ParticleVelocity_R", vInputParticle.MainParticle.Velocity);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TerrianHeight_R", voTargetLayer.TerrrianHeight);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellHeight_R", voTargetLayer.TallCellHeight);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellPow2HeightSum_RW", vCache.TallCellPow2HeightSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellHeightSum_RW", vCache.TallCellHeightSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellHeightVelocityXSum_RW", vCache.TallCellHeightVelocityXSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellHeightVelocityYSum_RW", vCache.TallCellHeightVelocityYSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellHeightVelocityZSum_RW", vCache.TallCellHeightVelocityZSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellVelocityXSum_RW", vCache.TallCellVelocityXSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellVelocityYSum_RW", vCache.TallCellVelocityYSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellVelocityZSum_RW", vCache.TallCellVelocityZSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "TallCellParticleCount_RW", vCache.TallCellParticleCountCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "RegularCellWeightTempCache_RW", vCache.RegularCellWeightTempCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "RegularCellVelocityXTempCache_RW", vCache.RegularCellVelocityXTempCache);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "RegularCellVelocityYTempCache_RW", vCache.RegularCellVelocityYTempCache);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas1, "RegularCellVelocityZTempCache_RW", vCache.RegularCellVelocityZTempCache);
        //m_ParticleInCellToolsCS.SetBuffer(scatterParticleToGrid_Paas1, "ParticleFilter_RW", vInputParticle.MainParticle.Filter);
        //m_ParticleInCellToolsCS.DispatchIndirect(scatterParticleToGrid_Paas1, vInputParticle.Argument);

        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas2, "RegularCellWeightTempCache_R", vCache.RegularCellWeightTempCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas2, "RegularCellVelocityXTempCache_R", vCache.RegularCellVelocityXTempCache);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas2, "RegularCellVelocityYTempCache_R", vCache.RegularCellVelocityYTempCache);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas2, "RegularCellVelocityZTempCache_R", vCache.RegularCellVelocityZTempCache);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas2, "RegularCellVelocity_R", voTargetLayer.Velocity.UpperUniform);
        //m_ParticleInCellToolsCS.Dispatch(scatterParticleToGrid_Paas1, m_GPUGroupCount3D.x, m_GPUGroupCount3D.y, m_GPUGroupCount3D.z);

        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellHeight_R", voTargetLayer.TallCellHeight);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellPow2HeightSum_R", vCache.TallCellPow2HeightSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellHeightSum_R", vCache.TallCellHeightSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellHeightVelocityXSum_R", vCache.TallCellHeightVelocityXSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellHeightVelocityYSum_R", vCache.TallCellHeightVelocityYSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellHeightVelocityZSum_R", vCache.TallCellHeightVelocityZSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellVelocityXSum_R", vCache.TallCellVelocityXSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellVelocityYSum_R", vCache.TallCellVelocityYSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellVelocityZSum_R", vCache.TallCellVelocityZSumCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TallCellParticleCount_R", vCache.TallCellParticleCountCahce);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "TopCellVelocity_RW", voTargetLayer.Velocity.Top);
        //m_ParticleInCellToolsCS.SetTexture(scatterParticleToGrid_Paas3, "BottomCellVelocity_RW", voTargetLayer.Velocity.Bottom);
        //m_ParticleInCellToolsCS.Dispatch(scatterParticleToGrid_Paas1, m_GPUGroupCount2D.x, m_GPUGroupCount2D.y, 1);
    }

    public void InitParticleDataWithSeaLevel(GridPerLevel vFineLayer, float vSeaLevel, DynamicParticle voTarget)
    {
        Texture2D TerrianHeight = Common.CopyRenderTextureToCPU(vFineLayer.TerrrianHeight);
        Texture2D TallCellHeight = Common.CopyRenderTextureToCPU(vFineLayer.TallCellHeight);

        List<Vector3> Position = new List<Vector3>();
        List<Vector3> Velocity = new List<Vector3>();
        List<int> Filter = new List<int>();
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
                    addParticleInCell(TallCellSliceMin, vFineLayer.CellLength, 2, CurrTerrianHeight + CurrTallCellHeight, ref Position, ref Velocity, ref Filter);
                    TallCellSliceMin.y += vFineLayer.CellLength;
                }

                //add particle into regular cell
                float RegularCellHeight = vSeaLevel - CurrTallCellHeight - CurrTerrianHeight;
                int RegularCellSlice = Mathf.CeilToInt(RegularCellHeight / vFineLayer.CellLength);
                Vector3 RegularCellSliceMin = new Vector3(x * vFineLayer.CellLength, CurrTerrianHeight + CurrTallCellHeight, z * vFineLayer.CellLength);
                for (int c = 0; c < RegularCellSlice; c++)
                {
                    addParticleInCell(RegularCellSliceMin, vFineLayer.CellLength, 2, vSeaLevel, ref Position, ref Velocity, ref Filter);
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
    private int scatterParticleToGrid_Paas1;
    private int scatterParticleToGrid_Paas2;
    private int scatterParticleToGrid_Paas3;
}
