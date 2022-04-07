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

    public void InitParticleDataWithSeaLevel(RenderTexture vTerrianHeight, RenderTexture vTallCellHeight, float vSeaLevel, DynamicParticle voTarget)
    {
    }

    private Vector2Int m_GPUGroupCount;

    private ComputeShader m_ParticleInCellToolsCS;
}
