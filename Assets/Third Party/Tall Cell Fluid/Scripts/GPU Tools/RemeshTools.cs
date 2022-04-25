using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemeshTools
{
    public RemeshTools(Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
    {
        m_RemeshToolsCS = Resources.Load<ComputeShader>(Common.RemeshToolsCSPath);
        computeTerrianHeight = m_RemeshToolsCS.FindKernel("computeTerrianHeight");
        computeH1H2WithSeaLevel = m_RemeshToolsCS.FindKernel("computeH1H2WithSeaLevel");
        computeTallCellHeight = m_RemeshToolsCS.FindKernel("computeTallCellHeight");
        smoothTallCellHeight = m_RemeshToolsCS.FindKernel("smoothTallCellHeight");
        enforceDCondition = m_RemeshToolsCS.FindKernel("enforceDCondition");
        updateRegularCellVelocity = m_RemeshToolsCS.FindKernel("updateRegularCellVelocity");
        updateTallCellVelocity = m_RemeshToolsCS.FindKernel("updateTallCellVelocity");
        updateSolidInfos = m_RemeshToolsCS.FindKernel("updateSolidInfos");

        UpdateGlobalParma(vResolutionXZ, vCellLength, vRegularCellYCount);
    }

    public void UpdateGlobalParma(Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
    {
        m_RemeshToolsCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_RemeshToolsCS.SetFloat("CellLength", vCellLength);
        m_RemeshToolsCS.SetInt("ConstantCellNum", vRegularCellYCount);

        m_GPUGroupCount.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount2D);
        m_GPUGroupCount.y = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount2D);

        m_GPUGroupCountForRegularCell.x = Mathf.CeilToInt((float)vResolutionXZ.x / Common.ThreadCount3D);
        m_GPUGroupCountForRegularCell.y = Mathf.CeilToInt((float)vRegularCellYCount / Common.ThreadCount3D);
        m_GPUGroupCountForRegularCell.z = Mathf.CeilToInt((float)vResolutionXZ.y / Common.ThreadCount3D);
        
    }

    public void ComputeTerrianHeight(Texture vTerrian, RenderTexture voTerrianHeight, float vHeightScale = 1.0f)
    {
        m_RemeshToolsCS.SetFloat("HeightScale", vHeightScale);
        m_RemeshToolsCS.SetTexture(computeTerrianHeight, "TerrianTexture_R", vTerrian);
        m_RemeshToolsCS.SetTexture(computeTerrianHeight, "TerrianHeight_RW", voTerrianHeight);
        m_RemeshToolsCS.Dispatch(computeTerrianHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void ComputeH1H2WithSeaLevel(RenderTexture vTerrianHeight, RenderTexture voWaterSurfaceH1H2, float vSeaLevel = 0)
    {
        m_RemeshToolsCS.SetFloat("SeaLevel", vSeaLevel);
        m_RemeshToolsCS.SetTexture(computeH1H2WithSeaLevel, "TerrianHeight_R", vTerrianHeight);
        m_RemeshToolsCS.SetTexture(computeH1H2WithSeaLevel, "WaterSurfaceH1H2_RW", voWaterSurfaceH1H2);
        m_RemeshToolsCS.Dispatch(computeH1H2WithSeaLevel, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void ComputeTallCellHeight(RenderTexture vTerrianHeight, RenderTexture vWaterSurfaceH1H2, RenderTexture voTallCellHeightMaxMin, RenderTexture voTallCellHeight, 
        int vGridAbove = 8, int vGridLow = 8, int vD = 6)
    {
        m_RemeshToolsCS.SetInt("GridAbove", vGridAbove);
        m_RemeshToolsCS.SetInt("GridLow", vGridLow);
        m_RemeshToolsCS.SetInt("D", vD);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TerrianHeight_R", vTerrianHeight);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "WaterSurfaceH1H2_R", vWaterSurfaceH1H2);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TallCellHeightMaxMin_RW", voTallCellHeightMaxMin);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TallCellHeight_RW", voTallCellHeight);
        m_RemeshToolsCS.Dispatch(computeTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void SmoothTallCellHeight(RenderTexture vTallCellHeightMaxMin, RenderTexture vFrontTallCellHeight, RenderTexture voBackTallCellHeight, 
        float vBlurSigma = 1.5f, float vBlurRadius = 3.0f)
    {
        m_RemeshToolsCS.SetFloat("BlurSigma", vBlurSigma);
        m_RemeshToolsCS.SetFloat("BlurRadius", vBlurRadius);
        m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeightMaxMin_R", vTallCellHeightMaxMin);
        m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeight_R", vFrontTallCellHeight);
        m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeightCache_RW", voBackTallCellHeight);
        m_RemeshToolsCS.Dispatch(smoothTallCellHeight, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void EnforceDCondition(RenderTexture vTerrianHeight, RenderTexture vFrontTallCellHeight, RenderTexture voBackTallCellHeight)
    {
        m_RemeshToolsCS.SetTexture(enforceDCondition, "TerrianHeight_R", vTerrianHeight);
        m_RemeshToolsCS.SetTexture(enforceDCondition, "TallCellHeight_R", vFrontTallCellHeight);
        m_RemeshToolsCS.SetTexture(enforceDCondition, "TallCellHeightCache_RW", voBackTallCellHeight);
        m_RemeshToolsCS.Dispatch(enforceDCondition, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void UpdateFineGridVelocity(RenderTexture vTerrrianHeight, RenderTexture vLastFrameTallcellHeight, GridValuePerLevel vLastFrameVelocity, RenderTexture vTallcellHeight, GridValuePerLevel vVelocity)
    {
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "TerrrianHeight", vTerrrianHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellHeight", vLastFrameTallcellHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "TallCellHeight", vTallcellHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcRegularCellVelocity", vLastFrameVelocity.RegularCellValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellTopVelocity", vLastFrameVelocity.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellBottomVelocity", vLastFrameVelocity.TallCellBottomValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "RegularCellVelocity", vVelocity.RegularCellValue);
        m_RemeshToolsCS.Dispatch(updateRegularCellVelocity, m_GPUGroupCountForRegularCell.x, m_GPUGroupCountForRegularCell.y, m_GPUGroupCountForRegularCell.z);

        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TerrrianHeight", vTerrrianHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellHeight", vLastFrameTallcellHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellHeight", vTallcellHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcRegularCellVelocity", vLastFrameVelocity.RegularCellValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellTopVelocity", vLastFrameVelocity.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellBottomVelocity", vLastFrameVelocity.TallCellBottomValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellTopVelocity", vVelocity.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellTopVelocity", vVelocity.TallCellBottomValue);
        m_RemeshToolsCS.Dispatch(updateTallCellVelocity, m_GPUGroupCount.x, m_GPUGroupCount.y, 1);
    }

    public void UpdateSolidInfos(RenderTexture vTerrianHeight, RenderTexture vRigidBodyPercentage, RenderTexture vRigidBodyVelocity)
    {
        GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>().UploadRigidBodyDataToGPU(m_RemeshToolsCS, updateSolidInfos);
        m_RemeshToolsCS.SetTexture(updateSolidInfos, "TallCellHeight_R", vTerrianHeight);
        m_RemeshToolsCS.SetTexture(updateSolidInfos, "OutRegularCellRigidBodyPercentage", vRigidBodyPercentage);
        m_RemeshToolsCS.SetTexture(updateSolidInfos, "OutRegularCellRigidbodyVelocity", vRigidBodyVelocity);
        m_RemeshToolsCS.Dispatch(updateSolidInfos, m_GPUGroupCountForRegularCell.x, m_GPUGroupCountForRegularCell.y, m_GPUGroupCountForRegularCell.z);
    }

    private Vector2Int m_GPUGroupCount;
    private Vector3Int m_GPUGroupCountForRegularCell;

    private ComputeShader m_RemeshToolsCS;
    private int computeTerrianHeight;
    private int computeH1H2WithSeaLevel;
    private int computeTallCellHeight;
    private int smoothTallCellHeight;
    private int enforceDCondition;
    private int updateSolidInfos;
    private int updateRegularCellVelocity;
    private int updateTallCellVelocity;
}
