using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemeshTools
{
    public RemeshTools(Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_RemeshTallCellGridCS = Resources.Load<ComputeShader>(Common.RemeshToolsCSPath);
        computeTerrianHeight = m_RemeshTallCellGridCS.FindKernel("computeTerrianHeight");
        computeH1H2WithSeaLevel = m_RemeshTallCellGridCS.FindKernel("computeH1H2WithSeaLevel");
        computeTallCellHeight = m_RemeshTallCellGridCS.FindKernel("computeTallCellHeight");
        smoothTallCellHeight = m_RemeshTallCellGridCS.FindKernel("smoothTallCellHeight");
        enforceDCondition = m_RemeshTallCellGridCS.FindKernel("enforceDCondition");

        UpdateGlobalParma(vResolutionXZ, vCellLength, vConstantCellNum);
    }

    public void UpdateGlobalParma(Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        m_RemeshTallCellGridCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_RemeshTallCellGridCS.SetFloat("CellLength", vCellLength);
        m_RemeshTallCellGridCS.SetInt("ConstantCellNum", vConstantCellNum);

        m_GPUGroupCount.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount2D);
        m_GPUGroupCount.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount2D);
    }

    public void ComputeTerrianHeight(Texture vTerrian, RenderTexture voTerrianHeight, float vHeightScale = 1.0f)
    {
        m_RemeshTallCellGridCS.SetFloat("HeightScale", vHeightScale);
        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeight, "TerrianTexture_R", vTerrian);
        m_RemeshTallCellGridCS.SetTexture(computeTerrianHeight, "TerrianHeight_RW", voTerrianHeight);
        m_RemeshTallCellGridCS.Dispatch(computeTerrianHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void ComputeH1H2WithSeaLevel(RenderTexture vTerrianHeight, RenderTexture voWaterSurfaceH1H2, float vSeaLevel = 0)
    {
        m_RemeshTallCellGridCS.SetFloat("SeaLevel", vSeaLevel);
        m_RemeshTallCellGridCS.SetTexture(computeH1H2WithSeaLevel, "TerrianHeight_R", vTerrianHeight);
        m_RemeshTallCellGridCS.SetTexture(computeH1H2WithSeaLevel, "WaterSurfaceH1H2_RW", voWaterSurfaceH1H2);
        m_RemeshTallCellGridCS.Dispatch(computeH1H2WithSeaLevel, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void ComputeTallCellHeight(RenderTexture vTerrianHeight, RenderTexture vWaterSurfaceH1H2, RenderTexture voTallCellHeightMaxMin, RenderTexture voTallCellHeight, 
        int vGridAbove = 8, int vGridLow = 8, int vD = 6)
    {
        m_RemeshTallCellGridCS.SetInt("GridAbove", vGridAbove);
        m_RemeshTallCellGridCS.SetInt("GridLow", vGridLow);
        m_RemeshTallCellGridCS.SetInt("D", vD);
        m_RemeshTallCellGridCS.SetTexture(computeTallCellHeight, "TerrianHeight_R", vTerrianHeight);
        m_RemeshTallCellGridCS.SetTexture(computeTallCellHeight, "WaterSurfaceH1H2_R", vWaterSurfaceH1H2);
        m_RemeshTallCellGridCS.SetTexture(computeTallCellHeight, "TallCellHeightMaxMin_RW", voTallCellHeightMaxMin);
        m_RemeshTallCellGridCS.SetTexture(computeTallCellHeight, "TallCellHeight_RW", voTallCellHeight);
        m_RemeshTallCellGridCS.Dispatch(computeTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void SmoothTallCellHeight(RenderTexture vTallCellHeightMaxMin, RenderTexture vFrontTallCellHeight, RenderTexture voBackTallCellHeight, 
        float vBlurSigma = 1.5f, float vBlurRadius = 3.0f)
    {
        m_RemeshTallCellGridCS.SetFloat("BlurSigma", vBlurSigma);
        m_RemeshTallCellGridCS.SetFloat("BlurRadius", vBlurRadius);
        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeightMaxMin_R", vTallCellHeightMaxMin);
        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeight_R", vFrontTallCellHeight);
        m_RemeshTallCellGridCS.SetTexture(smoothTallCellHeight, "TallCellHeightCache_RW", voBackTallCellHeight);
        m_RemeshTallCellGridCS.Dispatch(smoothTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void EnforceDCondition(RenderTexture vTerrianHeight,RenderTexture vFrontTallCellHeight, RenderTexture voBackTallCellHeight)
    {
        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TerrianHeight_R", vTerrianHeight);
        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TallCellHeight_R", vFrontTallCellHeight);
        m_RemeshTallCellGridCS.SetTexture(enforceDCondition, "TallCellHeightCache_RW", voBackTallCellHeight);
        m_RemeshTallCellGridCS.Dispatch(enforceDCondition, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    private Vector2Int m_GPUGroupCount;

    private ComputeShader m_RemeshTallCellGridCS;
    private int computeTerrianHeight;
    private int computeH1H2WithSeaLevel;
    private int computeTallCellHeight;
    private int smoothTallCellHeight;
    private int enforceDCondition;
}
