using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemeshTools
{
    public RemeshTools(Vector2Int vResolutionXZ, float vCellLength)
    {
        m_RemeshTallCellGridCS = Resources.Load<ComputeShader>(Common.RemeshTallCellGridCSPath);
        computeTerrianHeightAndTallCellHeight = m_RemeshTallCellGridCS.FindKernel("computeTerrianHeightAndTallCellHeight");
        smoothTallCellHeight = m_RemeshTallCellGridCS.FindKernel("smoothTallCellHeight");
        enforceDCondition = m_RemeshTallCellGridCS.FindKernel("enforceDCondition");

        m_TargetResolution = vResolutionXZ;
        m_GPUGroupCount.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount2D);
        m_GPUGroupCount.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount2D);
        m_CellLength = vCellLength;
        m_TallCellHeightMaxMinCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        m_TallCellHeightCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~RemeshTools()
    {
        m_TallCellHeightMaxMinCache.Release();
        m_TallCellHeightCache.Release();
    }

    public void InitTallCellMesh(Texture vTerrian, RenderTexture voTerrrianHeight, RenderTexture voTallCellHeight, float vSeaLevel, float vHeightScale = 1.0f, int vGridAbove = 8, int vGridLow = 8, float vBlurSigma = 1.5f, float vBlurRadius = 3.0f)
    {
        m_RemeshTallCellGridCS.SetInts("XZResolution", m_TargetResolution.x, m_TargetResolution.y);
        m_RemeshTallCellGridCS.SetFloat("SeaLevel", vSeaLevel);
        m_RemeshTallCellGridCS.SetFloat("CellLength", m_CellLength);
        m_RemeshTallCellGridCS.SetInt("GridAbove", vGridAbove);
        m_RemeshTallCellGridCS.SetInt("GridLow", vGridLow);
        m_RemeshTallCellGridCS.SetFloat("HeightScale", vHeightScale);
        m_RemeshTallCellGridCS.SetFloat("BlurSigma", vBlurSigma);
        m_RemeshTallCellGridCS.SetFloat("BlurRadius", vBlurRadius);

        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeightAndTallCellHeight, "TerrianTexture_R", vTerrian);
        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeightAndTallCellHeight, "TerrianHeight_RW", voTerrrianHeight);
        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeightAndTallCellHeight, "TallCellHeight_RW", voTallCellHeight);
        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeightAndTallCellHeight, "TallCellHeightMaxMin_RW", m_TallCellHeightMaxMinCache);
        m_RemeshTallCellGridCS.Dispatch(computeTerrianHeightAndTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);

        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeightMaxMin_R", m_TallCellHeightMaxMinCache);
        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeight_R", voTerrrianHeight);
        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeightCache_RW", m_TallCellHeightCache);
        m_RemeshTallCellGridCS.Dispatch(smoothTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);

        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TerrianHeight_R", voTerrrianHeight);
        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TallCellHeight_R", m_TallCellHeightCache);
        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TallCellHeightCache_RW", voTallCellHeight);
        m_RemeshTallCellGridCS.Dispatch(enforceDCondition, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    private Vector2Int m_TargetResolution;
    private Vector2Int m_GPUGroupCount;
    private float m_CellLength;

    private ComputeShader m_RemeshTallCellGridCS;
    private int computeTerrianHeightAndTallCellHeight;
    private int smoothTallCellHeight;
    private int enforceDCondition;

    private RenderTexture m_TallCellHeightMaxMinCache;
    private RenderTexture m_TallCellHeightCache;
}
