using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DParticle;

public class ParticleInCellTools
{
    public ParticleInCellTools()
    {
        m_ParticleInCellToolsCS = Resources.Load<ComputeShader>(Common.ParticleInCellToolsCSPath);
    }

    public void InitParticleDataWithSeaLevel(TallCellGridLayer vFineLayer, float vSeaLevel, DynamicParticle voTarget)
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
                    voFilter.Add(1);
                }
            }
        }
    }

    private Vector2Int m_GPUGroupCount;
    private ComputeShader m_ParticleInCellToolsCS;
}
